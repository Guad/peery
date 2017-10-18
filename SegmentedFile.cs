using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Peery
{
    public class SegmentedFile : IDisposable
    {
        private Stream _stream;

        private const int PacketLength = 1024;
        private const bool AlwaysFlush = false;

        public static long BufferFlushInterval;

        public long BytesToTransfer;
        public long BytesTransferred;
        public long Length;
        public long LastBufferFlush;

        public long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public SegmentedFile(Stream stream)
        {
            _stream = stream;
            Length = stream.Length;
            BytesToTransfer = Length - Position;
        }

        public async Task<byte[]> SendSegmentAsync()
        {
            if (Position >= Length)
            {
                // we're done sending
                return null;
            }
            else
            {
                int len = (int) Math.Min(PacketLength, Length - Position);

                byte[] data = new byte[len];

                await _stream.ReadAsync(data, 0, len);

                BytesTransferred += len;
                
                return data;
            }
        }

        public async Task ReceiveSegmentAsync(byte[] segment)
        {
            await _stream.WriteAsync(segment, 0, segment.Length);
            BytesTransferred += segment.Length;

            if (Position - LastBufferFlush > BufferFlushInterval)
            {
                await _stream.FlushAsync();
                LastBufferFlush = Position;
            }
        }
        
        public void Dispose()
        {
            _stream?.Flush();
            _stream?.Dispose();
        }
    }
}