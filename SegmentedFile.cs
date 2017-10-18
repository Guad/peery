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

        private Dictionary<long, FileSegment> _segments;

        private const int MaxPendingSegments = 10;
        private const int PacketLength = 512;
        private const bool AlwaysFlush = true;

        public long AcknowledgedBytes;
        public long BytesToTransfer;
        public long BytesTransferred;
        public long Length;
        public long Position;

        public int PendingSegments => _segments.Count;

        public SegmentedFile(Stream stream)
        {
            _stream = stream;
            _segments = new Dictionary<long, FileSegment>();

            Length = stream.Length;
            Position = stream.Position;
            BytesToTransfer = Length - Position;
        }

        public async Task<FileSegment> SendSegmentAsync()
        {
            if (_segments.Count > MaxPendingSegments || Position == Length)
            {
                // Too many unacknowledged segments
                // Or we're done sending
                // Send oldest first
                if (_segments.Count > 0)
                {
                    // Slow down, client may not be able to catch up
                    await Task.Delay(100);

                    return _segments.First().Value;
                }
                return null;
            }
            else
            {
                int len = (int) Math.Min(PacketLength, Length - Position);

                // Send new segment
                FileSegment segment = new FileSegment();
                segment.Length = len;
                segment.Position = Position;
                segment.Data = new byte[len];

                await _stream.ReadAsync(segment.Data, 0, len);

                _segments.Add(Position, segment);

                Position += len;
                BytesTransferred += len;
                
                return segment;
            }
        }

        public async Task ReceiveSegmentAsync(FileSegment segment)
        {
            _stream.Position = segment.Position;
            await _stream.WriteAsync(segment.Data, 0, segment.Length);
            BytesTransferred += segment.Length;
            if (segment.Position > Position)
                Position = segment.Position;

            if (AlwaysFlush)
                await _stream.FlushAsync();
        }

        public void Acknowledge(long pos)
        {
            if (_segments.ContainsKey(pos))
                AcknowledgedBytes += _segments[pos].Length;

            _segments.Remove(pos);
        }

        public void Dispose()
        {
            _stream?.Flush();
            _stream?.Dispose();
        }
    }
}