using System.Net;

namespace Peery
{
    public class CommandlineArguments
    {
        public CommandlineArguments()
        {
            Port = 45554;
            BufferSize = 50 * 1024 * 1024;
        }

        //[Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        //[Option('p', "port", Default = 45554, HelpText = "Select what port to use to connect to the target machine")]
        public int Port { get; set; }

        //[Option('r', "receive", Default = false, HelpText = "Receive a file and wait for connections")]
        public bool Receiving { get; set; }

        //[Option('h', "host", HelpText = "IP of the target machine")]
        public string Host { get; set; }

        //[Value(0, HelpText = "Path to the file to send or receive.", Required = true)]
        public string File { get; set; }
        
        //[Option('b', "buffer", HelpText = "Size of the local disk buffer, in bytes.", Default = 50 * 1024 * 1024)]
        public long BufferSize { get; set; }

        public bool Help { get; set; }

        /*
        [Value(1, HelpText = "Remote machine's PIN code")]
        public int Code { get; set; }
        */
    }
}