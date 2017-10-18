using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Peery
{
    public class FileSender : IFileExchange
    {
        private IPAddress _address;
        private int _port;
        private UdpClient _socket;
        private SegmentedFile _file;
        private string _path;
        private int _code;

        public FileSender(IPAddress address, int port, string path, int code)
        {
            _port = port;
            _path = path;
            _code = code;
            _address = address;
        }

        public bool Finished { get; private set; }
        public bool Verbose { get; set; }
        public SegmentedFile File => _file;


        public async Task Start()
        {
            _socket = new UdpClient();
            _socket.AllowNatTraversal(true);

            VerboseLog("Starting connection to " + _address + ":" + _port);

            _socket.Connect(new IPEndPoint(_address, _port));

            _file = new SegmentedFile(System.IO.File.OpenRead(_path));

            byte[] fileInfo = new byte[1 + 4 + 8 + 8];

            fileInfo[0] = (byte) PacketType.SetFileInfo;

            VerboseLog("Sending file with length " + _file.Length);

            Array.Copy(BitConverter.GetBytes(_code), 0, fileInfo, 1, 4);
            Array.Copy(BitConverter.GetBytes(_file.Length), 0, fileInfo, 5, 8);
            Array.Copy(BitConverter.GetBytes(_file.Position), 0, fileInfo, 13, 8);

            VerboseLog("Sending welcome packet: " + string.Join(", ", fileInfo));
            await _socket.SendAsync(fileInfo, fileInfo.Length);
        }

        public async Task Pulse()
        {
            // Assume we're connected
            if (_socket.Available > 0)
            {
                try
                {
                    UdpReceiveResult result = await _socket.ReceiveAsync();
                    await HandlePacket(result);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Connection closed by remote peer");
                    Finished = true;
                    return;
                }
            }

            FileSegment segment = await _file.SendSegmentAsync();

            if (segment != null)
            {
                byte[] payload = segment.BinarySerialize();
                await _socket.SendAsync(payload, payload.Length);
            }

            if (_file.Position >= _file.Length && _file.PendingSegments == 0)
                Finished = true;
        }

        public void Stop()
        {
            VerboseLog("Finished");

            _file?.Dispose();
            _socket?.Close();
            _socket?.Dispose();
        }

        private async Task HandlePacket(UdpReceiveResult result)
        {
            if (!Equals(result.RemoteEndPoint.Address, _address))
                return;

            if (result.Buffer[0] == (int) PacketType.Ack && result.Buffer.Length == 9)
            {
                long pos = BitConverter.ToInt64(result.Buffer, 1);
                VerboseLog("Received ACK for " + pos);
                _file.Acknowledge(pos);
            }
        }

        private void VerboseLog(string text)
        {
            if (Verbose) Console.WriteLine(text);
        }
    }
}