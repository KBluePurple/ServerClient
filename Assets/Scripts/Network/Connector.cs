using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class Connector
    {
        // 세션 팩토리
        private Func<Session> _sessionFactory;

        // 접속
        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                // 소켓 생성
                var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // 세션 팩토리 설정
                _sessionFactory = sessionFactory;

                // 이벤트 인자 생성
                var args = new SocketAsyncEventArgs();
                // 연결이 완료되면 호출될 콜백 메서드 설정
                args.Completed += OnConnectCompleted;
                // 연결할 서버 주소 설정
                args.RemoteEndPoint = endPoint;
                // 소켓을 넘겨준다.
                args.UserToken = socket;

                // 비동기 연결 요청
                RegisterConnect(args);
            }
        }

        private void RegisterConnect(SocketAsyncEventArgs args)
        {
            // 소켓 가져오기
            var socket = args.UserToken as Socket;
            if (socket == null)
                return;

            // 비동기 연결 요청
            var pending = socket.ConnectAsync(args);
            if (pending == false)
                OnConnectCompleted(null, args);
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            // 연결된 소켓 가져오기
            if (args.SocketError == SocketError.Success)
            {
                // 세션 불러오기
                var session = _sessionFactory.Invoke();
                // 세션 시작
                session.Start(args.ConnectSocket);
                // 세션에 접속 완료 알림
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"OnConnectCompleted Fail: {args.SocketError}");
            }
        }
    }
}