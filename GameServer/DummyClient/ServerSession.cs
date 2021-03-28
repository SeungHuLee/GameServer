using ServerCore;
using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

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

        public List<SkillInfo> skills = new List<SkillInfo>();

        public struct SkillInfo
        {
            public int id;
            public ushort level;
            public float duration;

            public void Read(ReadOnlySpan<byte> span, ref ushort count)
            {
                this.id = BitConverter.ToInt32(span.Slice(count, span.Length - count));
                count += sizeof(int);
                this.level = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
                count += sizeof(ushort);
                this.duration = BitConverter.ToSingle(span.Slice(count, span.Length - count));
                count += sizeof(float);
            }

            public bool Write(Span<byte> span, ref ushort count)
            {
                bool success = true;

                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), id);
                count += sizeof(int);
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), level);
                count += sizeof(ushort);
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), duration);
                count += sizeof(float);

                return success;
            }
        }

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
            usedByteCount += nameLength;

            // skills
            ushort skillLength = BitConverter.ToUInt16(span.Slice(usedByteCount, span.Length - usedByteCount));
            usedByteCount += sizeof(ushort);

            skills.Clear();
            for (int i = 0; i < skillLength; i++)
            {
                SkillInfo skill = new SkillInfo();
                skill.Read(span, ref usedByteCount);
                skills.Add(skill);
            }
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

            // Skill List
            success &= BitConverter.TryWriteBytes(span.Slice(usedByteCount, span.Length - usedByteCount), (ushort)skills.Count);
            usedByteCount += sizeof(ushort);

            foreach (var skill in skills)
            {
                // TODO
                success &= skill.Write(span, ref usedByteCount);
            }

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
            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 101, level = 1, duration = 3.75f });
            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 201, level = 2, duration = 2.75f });
            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 301, level = 3, duration = 1.75f });
            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 401, level = 4, duration = 1.5f });
            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 501, level = 5, duration = 0.75f });

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
