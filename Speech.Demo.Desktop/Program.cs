namespace Speech.Demo.Desktop
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Out.WriteLine("Use: Speech.Demo.Core {region} {key} {locale}");
            }
            else
            {
                using (RecognitionDemo recognition = new RecognitionDemo(args[0], args[1], args[2]))
                {
                    recognition.Start();
                    Console.Out.WriteLine("Press any key to stop the demo.");
                    Console.ReadKey();
                    Console.Out.WriteLine("\r\nTerminating...");
                    recognition.Stop();
                }
            }
        }
    }
}
