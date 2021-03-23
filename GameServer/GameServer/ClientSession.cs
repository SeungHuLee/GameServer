using ServerCore;
using System;
using System.Net;
using System.Threading;

namespace GameServer
{
    public abstract class Packet
    {
        public ushort size;
        public ushort packetID;

        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> segment);
    }

    public class PlayerInfoReq : Packet
    {
        public long playerID;

        public PlayerInfoReq()
        {
            this.packetID = (ushort)PacketID.PlayerInfoReq;
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort usedByteCount = 0;

            // ushort size = BitConverter.ToUInt16(segment.Array, segment.Offset + usedByteCount);
            usedByteCount += 2;
            // ushort packetID = BitConverter.ToUInt16(segment.Array, segment.Offset + usedByteCount);
            usedByteCount += 2;
            this.playerID = BitConverter.ToInt64(new ReadOnlySpan<byte>(segment.Array, segment.Offset + usedByteCount, segment.Count - usedByteCount));
            usedByteCount += 8;
        }

        public override ArraySegment<byte> Write()
        {
            // Serialize
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);
            ushort usedByteCount = 0;
            bool success = true;

            usedByteCount += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(segment.Array, segment.Offset + usedByteCount, segment.Count - usedByteCount), packetID);
            usedByteCount += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(segment.Array, segment.Offset + usedByteCount, segment.Count - usedByteCount), playerID);
            usedByteCount += 8;

            success &= BitConverter.TryWriteBytes(new Span<byte>(segment.Array, segment.Offset, segment.Count), usedByteCount);

            if (!success) { return null; }
            return SendBufferHelper.Close(usedByteCount);
        }
    }

    public class PlayerInfoOK : Packet
    {
        public int hp;
        public int attack;

        public override void Read(ArraySegment<byte> segment)
        {
            throw new NotImplementedException();
        }

        public override ArraySegment<byte> Write()
        {
            throw new NotImplementedException();
        }
    }

    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOK = 2
    }

    class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Connected: {endPoint}");

            // Packet packet = new Packet() { size = 100, packetID = 10 };

            // ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            // byte[] buffer = BitConverter.GetBytes(packet.size);
            // byte[] buffer2 = BitConverter.GetBytes(packet.packetID);
            // 
            // Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
            // Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
            // 
            // ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

            // Send(sendBuff);

            Thread.Sleep(5000);

            Disconnect();
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            // Deserialize
            ushort usedByteCount = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + usedByteCount);
            usedByteCount += 2;
            ushort packetID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + usedByteCount);
            usedByteCount += 2;

            switch ((PacketID)packetID)
            {
                case PacketID.PlayerInfoReq:
                {
                    PlayerInfoReq packet = new PlayerInfoReq();
                    packet.Read(buffer);
                    Console.WriteLine($"PlayerInfoReq - PlayerID: {packet.playerID}");
                    break;
                }
                default:
                    break;
            }

            Console.WriteLine($"OnReceivePacket- Size: {size} ID: {packetID}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"Disconnected: {endPoint}");
        }
    }
}
