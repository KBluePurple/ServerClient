using System;
using System.Net;
using DummyClient;
using ServerCore;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    // 서버 세션
    private readonly ServerSession _session = new();

    private void Start()
    {
        // 호스트 이름을 가져온다.
        var host = Dns.GetHostName();
        // 호스트 이름으로 IP 주소를 가져온다.
        var ipHost = Dns.GetHostEntry(host);
        var ipAddr = ipHost.AddressList[0];
        var endPoint = new IPEndPoint(ipAddr, 7777);

        // 커넥터 생성
        var connector = new Connector();
        // 접속
        connector.Connect(endPoint, () => _session);
    }

    private void Update()
    {
        // 쌓인 패킷을 모두 처리한다.
        var list = PacketQueue.Instance.PopAll();
        foreach (var packet in list)
            PacketManager.Instance.HandlePacket(_session, packet);
    }

    public void Send(ArraySegment<byte> sendBuff)
    {
        // 패킷을 보낸다.
        _session.Send(sendBuff);
    }
}