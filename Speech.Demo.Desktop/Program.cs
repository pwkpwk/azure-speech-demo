namespace Speech.Demo.Desktop
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.Out.WriteLine("Use: Speech.Demo.Core {region} {key} {locale} {millisecond per audio frame}");
            }
            else
            {
                if (!Int32.TryParse(args[3], out int mspf) || mspf < 60 || mspf > 2000)
                {
                    Console.Error.WriteLine($"Invalid length of input audio frames \"{args[3]}\". Must be between 60 and 2000");
                }
                else
                {
                    using (RecognitionDemo recognition = new RecognitionDemo(args[0], args[1], args[2], mspf))
                    {
                        recognition.Start();
                        Console.Out.WriteLine("Press any key to stop the demo.");
                        Console.ReadKey();
                        Console.Out.WriteLine("\r\nTerminating...");
                        recognition.Stop();

                        double rps = recognition.IntermediateResultsReceived + recognition.FinalResultsReceived;
                        double seconds = recognition.ElapsedMilliseconds;
                        seconds /= 1000.0;
                        rps /= seconds;

                        Console.Out.WriteLine($"        Audio frames captured: {recognition.FramesCaptured}");
                        Console.Out.WriteLine($"Intermediate results received: {recognition.IntermediateResultsReceived}");
                        Console.Out.WriteLine($"   Identical results received: {recognition.IdenticalResultsReceived}");
                        Console.Out.WriteLine($"       Final results received: {recognition.FinalResultsReceived}");
                        Console.Out.WriteLine($"           Results per second: {rps}");
                    }
                }
            }
        }
    }
}
