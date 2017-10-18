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
        private TcpClient _socket;
        private NetworkStream _stream;
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
        private bool _gracefully;
        public bool Verbose { get; set; }
        public SegmentedFile File => _file;


        public async Task Start()
        {
            _socket = new TcpClient();

            VerboseLog("Starting connection to " + _address + ":" + _port);

            _socket.Connect(new IPEndPoint(_address, _port));
            _stream = _socket.GetStream();

            _file = new SegmentedFile(System.IO.File.OpenRead(_path));

            long pos;
            if (TryLoadState(out pos))
            {
                _file.Position = pos;
            }

            byte[] fileInfo = new byte[1 + 4 + 8 + 8];

            fileInfo[0] = (byte) PacketType.SetFileInfo;

            VerboseLog("Sending file with length " + _file.Length);

            Array.Copy(BitConverter.GetBytes(_code), 0, fileInfo, 1, 4);
            Array.Copy(BitConverter.GetBytes(_file.Length), 0, fileInfo, 5, 8);
            Array.Copy(BitConverter.GetBytes(_file.Position), 0, fileInfo, 13, 8);

            VerboseLog("Sending welcome packet: " + string.Join(", ", fileInfo));
            await _stream.WriteAsync(fileInfo, 0, fileInfo.Length);
        }

        public async Task Pulse()
        {
            byte[] segment = await _file.SendSegmentAsync();

            if (segment != null)
            {
                VerboseLog("Sending packet of length " + segment.Length);
                try
                {
                    await _stream.WriteAsync(segment, 0, segment.Length);
                }
                catch (IOException)
                {
                    Console.WriteLine("\nConnection closed by remote peer");
                    Finished = true;
                    return;
                }
            }

            if (_file.Position >= _file.Length)
            {
                _gracefully = true;
                Finished = true;
            }
        }

        public void Stop()
        {
            VerboseLog("Finished");

            if (!_gracefully)
                SaveState();
            else if (System.IO.File.Exists(_path + ".peery"))
                System.IO.File.Delete(_path + ".peery");

            _file?.Dispose();
            _socket?.Close();
            _socket?.Dispose();
        }

        private void SaveState()
        {
            string path = _path + ".peery";

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            System.IO.File.WriteAllBytes(path, BitConverter.GetBytes(_file.Position));
        }

        private bool TryLoadState(out long pos)
        {
            string path = _path + ".peery";

            if (System.IO.File.Exists(path))
            {
                pos = BitConverter.ToInt64(System.IO.File.ReadAllBytes(path), 0);

                System.IO.File.Delete(path);

                return true;
            }

            pos = 0;

            return false;
        }
        
        private void VerboseLog(string text)
        {
            if (Verbose) Console.WriteLine(text);
        }
    }
}