﻿using System;
using System.Net;
using System.Threading.Tasks;

namespace Peery
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            CommandlineArguments arguments = Parser.Parse<CommandlineArguments>(args);

            if (arguments.Help)
            {
                Console.WriteLine(Parser.GenerateHelpText<CommandlineArguments>());
                return;
            }

            if (string.IsNullOrEmpty(arguments.File))
            {
                Console.WriteLine("Missing parameter: File");
                return;
            }

            SegmentedFile.BufferFlushInterval = arguments.BufferSize;

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

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => exchange.Stop();
            Console.CancelKeyPress += (sender, eventArgs) => exchange.Stop();

            await exchange.Start();

            ProgressBar bar = new ProgressBar();

            if (!arguments.Verbose) bar.Start();

            while (!exchange.Finished)
            {
                if (!arguments.Verbose) bar.Update(exchange.File);
                await exchange.Pulse();
            }

            if (!arguments.Verbose) bar.End();

            exchange.Stop();
        }
    }
}
