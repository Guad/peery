using System;
using System.Text;

namespace Peery
{
    public class ProgressBar
    {
        private DateTime _lastCheck;
        private long _dataTransferredThen;

        public void Start()
        {
            //Console.WriteLine();
        }

        public void End()
        {
            Console.WriteLine();
        }

        public void Update(SegmentedFile file)
        {
            if (file == null) return;

            StringBuilder sb = new StringBuilder();

            sb.Append('\r');

            double percentage = file.Position / (double) file.Length;

            sb.Append(string.Format("{0:P0}", percentage).PadRight(10));
            sb.Append('[');

            int spaceForBar = Console.WindowWidth - (10 + 2 + 16 + 12 + 16);
            int fill = (int) (spaceForBar * percentage);
            
            for (int i = 0; i < fill; i++)
                sb.Append('=');
            for (int i = 0; i < spaceForBar - fill - 1; i++)
                sb.Append(' ');

            sb.Append("] ");
            sb.Append(string.Format("{0}", HumanifyBytes(file.Position)).PadRight(16));

            if (DateTime.Now.Subtract(_lastCheck).TotalMilliseconds > 1000)
            {
                _lastCheck = DateTime.Now;

                long delta = file.Position - _dataTransferredThen;

                sb.Append(string.Format("{0}/s", HumanifyBytes(delta)).PadRight(12));

                if (delta > 0)
                {
                    TimeSpan eta = TimeSpan.FromSeconds((file.Length - file.Position) / delta);

                    sb.Append(string.Format("ETA {0}", eta).PadRight(16));
                }

                _dataTransferredThen = file.Position;
            }

            Console.Write(sb.ToString());
        }

        private string HumanifyBytes(long bytes)
        {
            string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB" };
            int order = 0;
            double len = (double) bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}