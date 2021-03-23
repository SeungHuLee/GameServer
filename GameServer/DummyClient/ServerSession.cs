using ServerCore;
using System;
using System.Net;
using System.Text;

namespace DummyClient
{
    public class Packet
    {
        public ushort size;
        public ushort packetID;
    }

    public class PlayerInfoReq : Packet
    {
        public long playerID;
    }

    public class PlayerInfoOK : Packet
    {
        public int hp;
        public int attack;
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

            PlayerInfoReq packet = new PlayerInfoReq() { packetID = (ushort)PacketID.PlayerInfoReq, playerID = 1001 };

            {
                ArraySegment<byte> segment = SendBufferHelper.Open(4096);
                ushort usedByteCount = 0;
                bool success = true;

                
                usedByteCount += 2;
                success &= BitConverter.TryWriteBytes(new Span<byte>(segment.Array, segment.Offset + usedByteCount, segment.Count - usedByteCount), packet.packetID);
                usedByteCount += 2;
                success &= BitConverter.TryWriteBytes(new Span<byte>(segment.Array, segment.Offset + usedByteCount, segment.Count - usedByteCount), packet.playerID);
                usedByteCount += 8;

                success &= BitConverter.TryWriteBytes(new Span<byte>(segment.Array, segment.Offset, segment.Count), usedByteCount);


                ArraySegment<byte> sendBuff = SendBufferHelper.Close(usedByteCount);

                if (success) { Send(sendBuff); }
            }
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
