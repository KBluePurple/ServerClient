using DummyClient;
using ServerCore;

internal class PacketHandler
{
    // 게임에 누군가 들어왔을 때
    public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
    {
        // 서버에서 보내준 패킷을 S_BroadcastEnterGame으로 형변환
        var pkt = packet as S_BroadcastEnterGame;

        // 플레이어 매니저에게 플레이어가 들어왔다고 알려줌
        PlayerManager.Instance.EnterGame(pkt);
    }

    // 게임에 누군가 나갔을 때
    public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet)
    {
        // 서버에서 보내준 패킷을 S_BroadcastLeaveGame으로 형변환
        var pkt = packet as S_BroadcastLeaveGame;

        // 플레이어 매니저에게 플레이어가 나갔다고 알려줌
        PlayerManager.Instance.LeaveGame(pkt);
    }

    // 플레이어 리스트를 받아올 때
    public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    {
        // 서버에서 보내준 패킷을 S_PlayerList으로 형변환
        var pkt = packet as S_PlayerList;

        // 플레이어 매니저에게 플레이어 리스트를 넘겨줌
        PlayerManager.Instance.Add(pkt);
    }

    // 누군가가 움직였을 때
    public static void S_BroadcastMoveHandler(PacketSession session, IPacket packet)
    {
        // 서버에서 보내준 패킷을 S_BroadcastMove으로 형변환
        var pkt = packet as S_BroadcastMove;
        
        // 플레이어 매니저에게 플레이어가 움직였다고 알려줌
        PlayerManager.Instance.Move(pkt);
    }
}