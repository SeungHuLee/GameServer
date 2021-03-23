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

            PlayerInfoReq packet = new PlayerInfoReq() { playerID = 1001 };

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
