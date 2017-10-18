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
        private TcpListener _socket;
        private TcpClient _client;
        private NetworkStream _clientStream;
        private int _password;
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
            _socket = new TcpListener(new IPEndPoint(IPAddress.Any, _port));
            _socket.AllowNatTraversal(true);
            _socket.Start();
            VerboseLog("Awaiting connections");
        }

        public async Task Pulse()
        {
            try
            {
                if (_socket.Pending())
                    await AcceptClient();

                if (_client != null && _client.Available > 0)
                {
                    await HandlePacket();
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("\nConnection closed by remote peer");
                Finished = true;
            }
        }

        public void Stop()
        {
            VerboseLog("Finished");

            _file?.Dispose();
            _client?.Close();
            _client?.Dispose();
            _socket?.Stop();
        }

        private async Task AcceptClient()
        {
            TcpClient cl = await _socket.AcceptTcpClientAsync();

            if (_client != null && IsClientConnected())
            {
                cl.Close();
                return;
            }

            byte[] buffer = new byte[1 + 4 + 8 + 8];

            await cl.GetStream().ReadAsync(buffer, 0, buffer.Length);

            if (buffer[0] == (byte) PacketType.SetFileInfo)
            {
                int pinCode = BitConverter.ToInt32(buffer, 1);
                long length = BitConverter.ToInt64(buffer, 5);
                long position = BitConverter.ToInt64(buffer, 13);

                VerboseLog("New connection from " + cl.Client.RemoteEndPoint);
                VerboseLog($"File len: {length} with starting pos {position}");
                VerboseLog("Payload: " + string.Join(",", buffer));

                if (pinCode == _password)
                {
                    _client = cl;
                    _clientStream = cl.GetStream();

                    if (_file == null)
                        _file = new SegmentedFile(System.IO.File.OpenWrite(_path));
                    _file.Position = position;
                    _file.Length = length;
                    _file.BytesTransferred = 0;
                    _file.BytesToTransfer = _file.Length - position;

                    VerboseLog("Connection successful!");
                }
            }
        }

        private async Task HandlePacket()
        {
            VerboseLog("Available to read: " + _client.Available);

            byte[] buffer = new byte[_client.Available];

            await _clientStream.ReadAsync(buffer, 0, buffer.Length);

            VerboseLog("Receiving segment with len " + buffer.Length);

            await _file.ReceiveSegmentAsync(buffer);

            if (_file.BytesToTransfer - _file.BytesTransferred == 0)
                Finished = true;
        }

        private void VerboseLog(string text)
        {
            if (Verbose) Console.WriteLine(text);
        }

        private bool IsClientConnected()
        {
            if (_client == null) return false;
            if (!_client.Connected) return false;

            if (_client.Client.Poll(0, SelectMode.SelectWrite) && !_client.Client.Poll(0, SelectMode.SelectError))
            {
                byte[] buf = new byte[1];
                if (_client.Client.Receive(buf, SocketFlags.Peek) == 0)
                    return false;
                return true;
            }

            return false;
        }
    }
}