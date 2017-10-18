using System.Net;
using CommandLine;
using CommandLine.Text;

namespace Peery
{
    public class CommandlineArguments
    {
        [Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option('p', "port", Default = 45554, HelpText = "Select what port to use to connect to the target machine")]
        public int Port { get; set; }

        [Option('r', "receive", Default = false, HelpText = "Receive a file and wait for connections")]
        public bool Receiving { get; set; }

        [Option('h', "host", HelpText = "IP of the target machine")]
        public string Host { get; set; }

        [Value(0, HelpText = "Path to the file to send or receive.", Required = true)]
        public string File { get; set; }

        /*
        [Value(1, HelpText = "Remote machine's PIN code")]
        public int Code { get; set; }
        */
    }
}