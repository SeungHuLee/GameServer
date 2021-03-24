using ServerCore;
using System;
using System.Net;
using System.Text;
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
        public string name;

        public PlayerInfoReq()
        {
            this.packetID = (ushort)PacketID.PlayerInfoReq;
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort usedByteCount = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);


            usedByteCount += sizeof(ushort);

            usedByteCount += sizeof(ushort);
            this.playerID = BitConverter.ToInt64(span.Slice(usedByteCount, span.Length - usedByteCount));
            usedByteCount += sizeof(long);

            // string length, string
            ushort nameLength = BitConverter.ToUInt16(span.Slice(usedByteCount, span.Length - usedByteCount));
            usedByteCount += sizeof(ushort);

            this.name = Encoding.Unicode.GetString(span.Slice(usedByteCount, nameLength));
        }

        public override ArraySegment<byte> Write()
        {
            // Serialize
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);
            ushort usedByteCount = 0;
            bool success = true;

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            usedByteCount += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(span.Slice(usedByteCount, span.Length - usedByteCount), packetID);
            usedByteCount += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(span.Slice(usedByteCount, span.Length - usedByteCount), playerID);
            usedByteCount += sizeof(long);

            // string length, string
            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + usedByteCount + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(span.Slice(usedByteCount, span.Length - usedByteCount), nameLength);
            usedByteCount += sizeof(ushort);
            usedByteCount += nameLength;

            // size
            success &= BitConverter.TryWriteBytes(span, usedByteCount);

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
                    Console.WriteLine($"PlayerInfoReq - PlayerID: {packet.playerID}, Name: {packet.name}");
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
