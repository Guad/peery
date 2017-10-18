using System;
using System.Net;
using System.Threading.Tasks;
using CommandLine;

namespace Peery
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CommandlineArguments arguments = null;
            var result = Parser.Default.ParseArguments<CommandlineArguments>(args);
            if (!result.MapResult(p => { arguments = p; return true; }, p => false))
                return;

            IFileExchange exchange;

            if (arguments.Receiving || string.IsNullOrEmpty(arguments.Host))
                exchange = new FileReceiver(arguments.Port, arguments.File);
            else
            {
                Console.Write("Enter remote PIN code: ");
                int code;
                if (!int.TryParse(Console.ReadLine(), out code))
                {
                    Console.WriteLine("Incorrect format for PIN code!");
                    return;
                }
                exchange = new FileSender(IPAddress.Parse(arguments.Host), arguments.Port, arguments.File, code);
            }

            exchange.Verbose = arguments.Verbose;

            Console.Write("Connecting...");

            Console.CancelKeyPress += (sender, eventArgs) => exchange.Stop();

            await exchange.Start();

            ProgressBar bar = new ProgressBar();

            if (!arguments.Verbose) bar.Start();

            while (!exchange.Finished)
            {
                await exchange.Pulse();
                if (!arguments.Verbose) bar.Update(exchange.File);
            }

            if (!arguments.Verbose) bar.End();

            exchange.Stop();
        }
    }
}
