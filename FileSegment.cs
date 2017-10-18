using System;

namespace Peery
{
    public class FileSegment
    {
        public long Position { get; set; }
        public int Length { get; set; }
        public byte[] Data { get; set; }

        public byte[] BinarySerialize()
        {
            byte[] bytes = new byte[8 + 4 + Data.Length];

            Array.Copy(BitConverter.GetBytes(Position), 0, bytes, 0, 8);
            Array.Copy(BitConverter.GetBytes(Length), 0, bytes, 8, 4);
            Array.Copy(Data, 0, bytes, 8 + 4, Data.Length);

            return bytes;
        }

        public static FileSegment BinaryDeserialize(byte[] bytes)
        {
            if (bytes.Length < 8 + 4)
                return null;

            FileSegment seg = new FileSegment();

            seg.Position = BitConverter.ToInt64(bytes, 0);
            seg.Length = BitConverter.ToInt32(bytes, 8);
            seg.Data = new byte[seg.Length];
            
            if (seg.Length > 0)
                Array.Copy(bytes, 8 + 4, seg.Data, 0, seg.Length);

            return seg;
        }
    }
}