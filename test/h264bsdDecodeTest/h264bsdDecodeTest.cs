// ==> Extract h264 byte stream from mp4
// ffmpeg - i max_intro.mp4 - profile:v baseline -f h264 max_intro.h264

using System;

using System.IO;

namespace h264bsd
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("h264bsd Decoding Test Console");

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
