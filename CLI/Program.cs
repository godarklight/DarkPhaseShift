using DarkPhaseShift.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace DarkPhaseShift.CLI
{
    public class Program
    {
        private static bool running = true;

        public static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool startAudio = true;
            bool nextIsFile = false;
            bool debug = false;
            string fileName = null;
            foreach (string str in args)
            {
                if (nextIsFile)
                {
                    nextIsFile = false;
                    fileName = str;
                }
                if (str == "--stdio")
                {
                    startAudio = false;
                }
                if (str == "--file")
                {
                    nextIsFile = true;
                    startAudio = false;
                }
                if (str == "--debug")
                {
                    debug = true;
                }
            }
            AudioDriver ad = new AudioDriver(startAudio, fileName, debug);
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => { ad.Stop(); Stop(); };
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => { ad.Stop(); Stop(); };
            while (running && ad.running)
            {
                Thread.Sleep(1000);
            }
            sw.Stop();
            Console.WriteLine($"Processing took {sw.ElapsedMilliseconds}ms");
        }

        private static void Stop()
        {
            running = false;
        }
    }
}
