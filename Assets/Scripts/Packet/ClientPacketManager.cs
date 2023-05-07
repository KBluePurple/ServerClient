using System;
using System.Collections.Generic;
using ServerCore;

public class PacketManager
{
    // 패킷을 헨들링할 함수를 등록
    private readonly Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new();

    // 패킷을 만들 함수를 등록
    private readonly Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new();

    private PacketManager()
    {
        Register();
    }

    private void Register()
    {
        // 패킷들을 생성하는 함수와 헨들러들을 등록
        _makeFunc.Add((ushort)PacketID.S_BroadcastEnterGame, MakePacket<S_BroadcastEnterGame>);
        _handler.Add((ushort)PacketID.S_BroadcastEnterGame, PacketHandler.S_BroadcastEnterGameHandler);
        _makeFunc.Add((ushort)PacketID.S_BroadcastLeaveGame, MakePacket<S_BroadcastLeaveGame>);
        _handler.Add((ushort)PacketID.S_BroadcastLeaveGame, PacketHandler.S_BroadcastLeaveGameHandler);
        _makeFunc.Add((ushort)PacketID.S_PlayerList, MakePacket<S_PlayerList>);
        _handler.Add((ushort)PacketID.S_PlayerList, PacketHandler.S_PlayerListHandler);
        _makeFunc.Add((ushort)PacketID.S_BroadcastMove, MakePacket<S_BroadcastMove>);
        _handler.Add((ushort)PacketID.S_BroadcastMove, PacketHandler.S_BroadcastMoveHandler);
    }

    // 패킷을 받았을 때
    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer,
        Action<PacketSession, IPacket> onRecvCallback = null)
    {
        // 패킷의 위치
        ushort count = 0;
        // 패킷의 크기를 읽어옴
        var size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        // 패킷의 위치를 옮김
        count += 2;
        // 패킷의 아이디를 읽어옴
        var id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        // 패킷의 위치를 옮김
        count += 2;

        // 패킷을 만드는 함수를 가져옴
        if (!_makeFunc.TryGetValue(id, out var func)) return;
        
        // 패킷을 만들고
        var packet = func.Invoke(session, buffer);
        
        // 패킷을 처리할 함수를 가져옴
        if (onRecvCallback != null)
            // 패킷을 처리할 함수를 실행
            onRecvCallback.Invoke(session, packet);
        else
            // 패킷을 처리할 함수가 없으면 기본 처리
            HandlePacket(session, packet);
    }

    // 패킷을 만드는 함수
    private static T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {
        // 패킷을 만들고
        var pkt = new T();
        // 패킷의 버퍼를 통해 패킷을 읽어옴
        pkt.Read(buffer);
        // 패킷을 리턴
        return pkt;
    }

    // 패킷을 처리하는 함수
    public void HandlePacket(PacketSession session, IPacket packet)
    {
        // 패킷을 처리할 함수를 가져옴
        if (_handler.TryGetValue(packet.Protocol, out var action))
            // 패킷을 처리할 함수를 실행
            action.Invoke(session, packet);
    }

    #region Singleton

    public static PacketManager Instance { get; } = new();

    #endregion
}