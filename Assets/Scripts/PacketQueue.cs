using System.Collections.Generic;

public class PacketQueue
{
    // 락 오브젝트
    private readonly object _lock = new();

    // 패킷 큐
    private readonly Queue<IPacket> _packetQueue = new();

    // 패킷 큐에 대한 싱글톤
    public static PacketQueue Instance { get; } = new();

    // 패킷 큐에 패킷을 넣는다.
    public void Push(IPacket packet)
    {
        // 락을 걸고 큐에 패킷을 넣는다.
        lock (_lock)
        {
            // 큐에 패킷을 넣는다.
            _packetQueue.Enqueue(packet);
        }
    }

    // 패킷 큐에서 패킷을 꺼낸다.
    public IPacket Pop()
    {
        // 락을 걸고 큐에서 패킷을 꺼낸다.
        lock (_lock)
        {
            // 큐에서 패킷을 꺼낸다.
            return _packetQueue.Count == 0 ? null : _packetQueue.Dequeue();
        }
    }

    // 패킷 큐에서 모든 패킷을 꺼낸다.
    public List<IPacket> PopAll()
    {
        // 패킷을 담을 리스트
        var list = new List<IPacket>();

        // 락을 걸고 큐에서 모든 패킷을 꺼낸다.
        lock (_lock)
        {
            // 큐에서 모든 패킷을 꺼낸다.
            while (_packetQueue.Count > 0)
                list.Add(_packetQueue.Dequeue());
        }

        // 패킷 리스트를 반환한다.
        return list;
    }
}