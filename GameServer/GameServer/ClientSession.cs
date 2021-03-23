using ServerCore;
using System;
using System.Net;
using System.Threading;

namespace GameServer
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
            ushort usedByteCount = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + usedByteCount);
            usedByteCount += 2;
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + usedByteCount);
            usedByteCount += 2;

            switch ((PacketID)id)
            {
                case PacketID.PlayerInfoReq:
                {
                    long playerID = BitConverter.ToInt64(buffer.Array, buffer.Offset + usedByteCount);
                    usedByteCount += 8;
                    Console.WriteLine($"PlayerInfoReq - PlayerID: {playerID}");
                    break;
                }
                default:
                    break;
            }

            Console.WriteLine($"OnReceivePacket- Size: {size} ID: {id}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"Disconnected: {endPoint}");
        }
    }
}
