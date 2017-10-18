using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Peery
{
    public class FileReceiver : IFileExchange
    {
        private int _port;
        private UdpClient _socket;
        private int _password;
        private IPAddress _client;
        private SegmentedFile _file;
        private string _path;

        public FileReceiver(int port, string path)
        {
            _port = port;
            _path = path;

            _password = new Random().Next(10000, 100000);
            Console.WriteLine("Your PIN code is " + _password);
        }

        public bool Finished { get; private set; }
        public bool Verbose { get; set; }
        public SegmentedFile File => _file;

        public async Task Start()
        {
            _socket = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
            _socket.AllowNatTraversal(true);

            VerboseLog("Awaiting connections");
        }

        public async Task Pulse()
        {
            try
            {
                UdpReceiveResult result = await _socket.ReceiveAsync();
                await HandlePacket(result);

                VerboseLog($"Ack: {_file.AcknowledgedBytes}, Len: {_file.Length} Total: {_file.BytesToTransfer} Transferred: {_file.BytesTransferred}");
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection closed by remote peer");
                Finished = true;
            }
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
            if (_client == null && (PacketType)result.Buffer[0] == PacketType.SetFileInfo)
            {
                int pinCode = BitConverter.ToInt32(result.Buffer, 1);
                long length = BitConverter.ToInt64(result.Buffer, 5);
                long position = BitConverter.ToInt64(result.Buffer, 13);

                VerboseLog("New connection from " + result.RemoteEndPoint.Address + ":" + result.RemoteEndPoint.Port);
                VerboseLog($"File len: {length} with starting pos {position}");
                VerboseLog("Payload: " + string.Join(",", result.Buffer));

                if (pinCode == _password)
                {
                    _client = result.RemoteEndPoint.Address;

                    _file = new SegmentedFile(System.IO.File.OpenWrite(_path));
                    _file.Position = position;
                    _file.Length = length;
                    _file.BytesToTransfer = _file.Length - _file.Position;

                    _socket.Connect(result.RemoteEndPoint);

                    VerboseLog("Connection successful!");
                }
            }
            else if (Equals(_client, result.RemoteEndPoint.Address))
            {
                // Assume it's file data
                FileSegment segment = FileSegment.BinaryDeserialize(result.Buffer);

                if (segment != null)
                {
                    VerboseLog("Receiving segment " + segment.Position + " with len " + segment.Length);

                    await _file.ReceiveSegmentAsync(segment);
                    // Acknowledge
                    byte[] ackPacket = new byte[9];
                    ackPacket[0] = (byte) PacketType.Ack;
                    Array.Copy(BitConverter.GetBytes(segment.Position), 0, ackPacket, 1, 8);

                    await _socket.SendAsync(ackPacket, ackPacket.Length);
                    
                    if (_file.BytesToTransfer - _file.BytesTransferred == 0)
                        Finished = true;
                }
            }
        }

        private void VerboseLog(string text)
        {
            if (Verbose) Console.WriteLine(text);
        }
    }
}