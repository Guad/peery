using System;

namespace Peery
{
    public class ProgressBar
    {
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

            //ClearLine();
            Console.Write('\r');

            double percentage = file.Position / (double) file.Length;

            Console.Write(string.Format("{0:P0}", percentage).PadRight(10));
            Console.Write('[');

            int spaceForBar = Console.WindowWidth - 37;
            int fill = (int) (spaceForBar * percentage);
            
            for (int i = 0; i < fill; i++)
                Console.Write('=');
            for (int i = 0; i < spaceForBar - fill - 1; i++)
                Console.Write(' ');

            Console.Write("] ");
            Console.Write(string.Format("{0:##,#}", file.Position).PadRight(20));
        }

        private void ClearLine()
        {
            Console.Write('\r');
            int width = Console.WindowWidth;
            for (int i = 0; i < width; i++)
                Console.Write(' ');
            Console.Write('\r');
        }
    }
}