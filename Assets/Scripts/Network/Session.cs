using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    // 패킷을 가지는 세션
    public abstract class PacketSession : Session
    {
        // 헤더 사이즈는 패킷 사이즈를 담는다
        private const int HeaderSize = 2;

        // 패킷을 받았을 때 호출되는 함수
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            // 처리된 데이터 길이
            var processLen = 0;
            // 패킷의 길이
            var packetCount = 0;

            // 패킷이 조립이 되었는지 확인
            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize)
                    break;

                // 패킷이 완전체로 도착했는지 확인
                var dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                    break;

                // 여기까지 왔으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
                packetCount++;

                // 처리한 데이터 길이를 더해준다
                processLen += dataSize;
                // 버퍼에서 처리한 만큼 빼준다
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            if (packetCount > 1)
                Console.WriteLine($"패킷 모아보내기 : {packetCount}");
            
            // 처리된 데이터 길이를 리턴
            return processLen;
        }

        // 패킷을 보낼 때 호출되는 함수
        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    // 세션
    public abstract class Session
    {
        // 연결 상태를 나타내는 변수
        private int _disconnected;

        // 락 오브젝트
        private readonly object _lock = new();
        // 보낼 패킷을 담는 리스트
        private readonly List<ArraySegment<byte>> _pendingList = new();
        // 받을 인자
        private readonly SocketAsyncEventArgs _recvArgs = new();

        // 패킷을 받을 버퍼
        private readonly RecvBuffer _recvBuffer = new(65535);
        // 보내는 인자
        private readonly SocketAsyncEventArgs _sendArgs = new();
        // 보낼 패킷을 담는 큐
        private readonly Queue<ArraySegment<byte>> _sendQueue = new();
        // 소켓
        private Socket _socket;

        // 연결이 되었을 때 호출되는 함수
        public abstract void OnConnected(EndPoint endPoint);
        // 패킷을 받았을 때 호출되는 함수
        public abstract int OnRecv(ArraySegment<byte> buffer);
        // 패킷을 보냈을 때 호출되는 함수
        public abstract void OnSend(int numOfBytes);
        // 연결이 끊겼을 때 호출되는 함수
        public abstract void OnDisconnected(EndPoint endPoint);

        // 모든 데이터를 지우는 함수
        private void Clear()
        {
            lock (_lock)
            {
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        public void Start(Socket socket)
        {
            // 초기화
            _socket = socket;

            // 받을 인자 초기화
            _recvArgs.Completed += OnRecvCompleted;
            _sendArgs.Completed += OnSendCompleted;

            // 등록
            RegisterRecv();
        }

        // 패킷 전송
        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            // 보낼 패킷이 없으면 리턴
            if (sendBuffList.Count == 0)
                return;

            lock (_lock)
            {
                // 보낼 패킷을 담는다
                foreach (var sendBuff in sendBuffList)
                    _sendQueue.Enqueue(sendBuff);

                // 보낼 패킷이 없으면 다시 대기
                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                // 보낼 패킷을 담는다
                _sendQueue.Enqueue(sendBuff);
                // 보낼 패킷이 없으면 다시 대기
                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            // 이미 끊겼으면 리턴
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            // 끊겼을 때 호출
            OnDisconnected(_socket.RemoteEndPoint);
            // 소켓 종료
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            Clear();
        }

        #region 네트워크 통신

        private void RegisterSend()
        {
            // 이미 끊겼으면 리턴
            if (_disconnected == 1)
                return;

            // 보낼 패킷이 없을 때까지 큐에서 꺼내서 보낼 리스트에 담는다
            while (_sendQueue.Count > 0)
            {
                var buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);
            }

            // 전송 인자에 보낼 패킷 리스트를 담는다
            _sendArgs.BufferList = _pendingList;

            try
            {
                // 비동기 전송 시작
                var pending = _socket.SendAsync(_sendArgs);
                if (pending == false)
                    OnSendCompleted(null, _sendArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Failed {e}");
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                // 전송이 완료되었으면
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                    try
                    {
                        // 보낼 리스트에서 전송 완료한 패킷을 제거한다
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        // OnSend 호출
                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                            // 다음 패킷 전송
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                else
                    // 전송이 실패했으면
                    Disconnect();
            }
        }

        private void RegisterRecv()
        {
            // 이미 끊겼으면 리턴
            if (_disconnected == 1)
                return;

            // 받는 버퍼를 비운다
            _recvBuffer.Clean();
            var segment = _recvBuffer.WriteSegment;
            // 받을 인자에 버퍼를 담는다
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                // 비동기 수신 시작
                var pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false)
                    OnRecvCompleted(null, _recvArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterRecv Failed {e}");
            }
        }

        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            // 받기 완료
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                try
                {
                    // Write 커서 이동
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    var processLen = OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }

                    // Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                }
            else
                Disconnect();
        }

        #endregion
    }
}