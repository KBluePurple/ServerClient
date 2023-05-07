using System;
using System.Collections.Generic;
using ServerCore;
using UnityEngine;

// 엄 설명할 필요 없다고 생각합니다 (아마..?)

public enum PacketID
{
    S_BroadcastEnterGame = 1,
    C_LeaveGame = 2,
    S_BroadcastLeaveGame = 3,
    S_PlayerList = 4,
    C_Move = 5,
    S_BroadcastMove = 6
}

public interface IPacket
{
    ushort Protocol { get; }
    void Read(ArraySegment<byte> segment);
    ArraySegment<byte> Write();
}


public class S_BroadcastEnterGame : IPacket
{
    public int playerId;
    public float posX;
    public float posY;
    public float posZ;

    public ushort Protocol => (ushort)PacketID.S_BroadcastEnterGame;

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;
        count += sizeof(ushort);
        count += sizeof(ushort);
        playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
        count += sizeof(int);
        posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
        posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
        posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        var segment = SendBufferHelper.Open(4096);
        ushort count = 0;

        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastEnterGame), 0, segment.Array,
            segment.Offset + count, sizeof(ushort));
        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes(playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
        count += sizeof(int);
        Array.Copy(BitConverter.GetBytes(posX), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);
        Array.Copy(BitConverter.GetBytes(posY), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);
        Array.Copy(BitConverter.GetBytes(posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);

        Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

        return SendBufferHelper.Close(count);
    }
}

public class C_LeaveGame : IPacket
{
    public ushort Protocol => (ushort)PacketID.C_LeaveGame;

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;
        count += sizeof(ushort);
        count += sizeof(ushort);
    }

    public ArraySegment<byte> Write()
    {
        var segment = SendBufferHelper.Open(4096);
        ushort count = 0;

        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_LeaveGame), 0, segment.Array, segment.Offset + count,
            sizeof(ushort));
        count += sizeof(ushort);


        Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

        return SendBufferHelper.Close(count);
    }
}

public class S_BroadcastLeaveGame : IPacket
{
    public int playerId;

    public ushort Protocol => (ushort)PacketID.S_BroadcastLeaveGame;

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;
        count += sizeof(ushort);
        count += sizeof(ushort);
        playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
        count += sizeof(int);
    }

    public ArraySegment<byte> Write()
    {
        var segment = SendBufferHelper.Open(4096);
        ushort count = 0;

        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastLeaveGame), 0, segment.Array,
            segment.Offset + count, sizeof(ushort));
        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes(playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
        count += sizeof(int);

        Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

        return SendBufferHelper.Close(count);
    }
}

public class S_PlayerList : IPacket
{
    public List<Player> players = new();

    public ushort Protocol => (ushort)PacketID.S_PlayerList;

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;
        count += sizeof(ushort);
        count += sizeof(ushort);
        players.Clear();
        var playerLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
        count += sizeof(ushort);
        for (var i = 0; i < playerLen; i++)
        {
            var player = new Player();
            player.Read(segment, ref count);
            players.Add(player);
        }
    }

    public ArraySegment<byte> Write()
    {
        var segment = SendBufferHelper.Open(4096);
        ushort count = 0;

        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_PlayerList), 0, segment.Array, segment.Offset + count,
            sizeof(ushort));
        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)players.Count), 0, segment.Array, segment.Offset + count,
            sizeof(ushort));
        count += sizeof(ushort);
        foreach (var player in players)
            player.Write(segment, ref count);

        Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

        return SendBufferHelper.Close(count);
    }

    public class Player
    {
        public bool isSelf;
        public int playerId;
        public float posX;
        public float posY;
        public float posZ;

        public void Read(ArraySegment<byte> segment, ref ushort count)
        {
            isSelf = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
            count += sizeof(bool);
            playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
            count += sizeof(int);
            posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
            count += sizeof(float);
            posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
            count += sizeof(float);
            posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
            count += sizeof(float);
        }

        public bool Write(ArraySegment<byte> segment, ref ushort count)
        {
            var success = true;
            Array.Copy(BitConverter.GetBytes(isSelf), 0, segment.Array, segment.Offset + count, sizeof(bool));
            count += sizeof(bool);
            Array.Copy(BitConverter.GetBytes(playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
            count += sizeof(int);
            Array.Copy(BitConverter.GetBytes(posX), 0, segment.Array, segment.Offset + count, sizeof(float));
            count += sizeof(float);
            Array.Copy(BitConverter.GetBytes(posY), 0, segment.Array, segment.Offset + count, sizeof(float));
            count += sizeof(float);
            Array.Copy(BitConverter.GetBytes(posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
            count += sizeof(float);
            return success;
        }
    }
}

public class C_Move : IPacket
{
    public float posX;
    public float posY;
    public float posZ;

    public C_Move(Vector3 position)
    {
        posX = position.x;
        posY = position.y;
        posZ = position.z;
    }

    public ushort Protocol => (ushort)PacketID.C_Move;

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;
        count += sizeof(ushort);
        count += sizeof(ushort);
        posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
        posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
        posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        var segment = SendBufferHelper.Open(4096);
        ushort count = 0;

        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Move), 0, segment.Array, segment.Offset + count,
            sizeof(ushort));
        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes(posX), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);
        Array.Copy(BitConverter.GetBytes(posY), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);
        Array.Copy(BitConverter.GetBytes(posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);

        Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

        return SendBufferHelper.Close(count);
    }
}

public class S_BroadcastMove : IPacket
{
    public int playerId;
    public float posX;
    public float posY;
    public float posZ;

    public ushort Protocol => (ushort)PacketID.S_BroadcastMove;

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;
        count += sizeof(ushort);
        count += sizeof(ushort);
        playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
        count += sizeof(int);
        posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
        posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
        posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
        count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        var segment = SendBufferHelper.Open(4096);
        ushort count = 0;

        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastMove), 0, segment.Array, segment.Offset + count,
            sizeof(ushort));
        count += sizeof(ushort);
        Array.Copy(BitConverter.GetBytes(playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
        count += sizeof(int);
        Array.Copy(BitConverter.GetBytes(posX), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);
        Array.Copy(BitConverter.GetBytes(posY), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);
        Array.Copy(BitConverter.GetBytes(posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
        count += sizeof(float);

        Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

        return SendBufferHelper.Close(count);
    }
}