using ServerCore;
using System;
using System.Net;
using System.Text;

namespace DummyClient
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

    class ServerSession : Session
    {
        static unsafe void ToBytes(byte[] array, int offset, ulong value)
        {
            fixed (byte* ptr = &array[offset])
            {
                *(ulong*)ptr = value;
            }
        }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Connected: {endPoint}");

            PlayerInfoReq packet = new PlayerInfoReq() { playerID = 1001, name = "SeungHu" };

            ArraySegment<byte> sendBuff = packet.Write();

            if (sendBuff != null) { Send(sendBuff); }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"Disconnected: {endPoint}");
        }

        public override int OnReceive(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server] {recvData}");

            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
