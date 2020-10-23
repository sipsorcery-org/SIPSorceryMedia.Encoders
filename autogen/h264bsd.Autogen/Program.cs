using System;
using CppSharp;

namespace h264bsd.AutoGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("h264bsd C# bindings auto-generator.");
            ConsoleDriver.Run(new h264bsdGenerator());
            Console.WriteLine("Finished.");
            Console.ReadLine();
        }
    }
}
