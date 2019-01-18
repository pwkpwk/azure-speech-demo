namespace Speech.Demo.Desktop
{
    using Accord.Audio;
    using Accord.DirectSound;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    sealed class RecognitionDemo : IDisposable
    {
        private static readonly int SamplesPerMillisecond = 16;

        private bool _disposed;
        private readonly SpeechRecognizer _recognizer;
        private readonly PushAudioInputStream _audioInput;
        private readonly AudioCaptureDevice _audioCapture;
        private readonly int _millisecondsPerFrame;
        private readonly Stream _audio;
        private readonly TextWriter _transcript;
        private readonly Stopwatch _stopwatch;

        private int _framesCaptured;
        private int _intermediateResultsReceived;
        private int _finalResultsReceived;
        private int _identicalResults;
        private string _lastResult;

        public RecognitionDemo(string region, string key, string locale, int millisecondsPerFrame)
        {
            _disposed = false;
            _millisecondsPerFrame = millisecondsPerFrame;
            SpeechConfig config = SpeechConfig.FromSubscription(key, region);
            config.SpeechRecognitionLanguage = locale;
            config.OutputFormat = OutputFormat.Detailed;
            _audioInput = CreateAudioInputStream();
            _recognizer = new SpeechRecognizer(config, AudioConfig.FromStreamInput(_audioInput));
            _audioCapture = CreateAudioCaptureDevice();
            _audio = new FileStream("audio.raw", FileMode.Create);
            _transcript = new StreamWriter(new FileStream("transcript.txt", FileMode.Create), Encoding.UTF8);
            _stopwatch = new Stopwatch();

            _framesCaptured = 0;
            _intermediateResultsReceived = 0;
            _finalResultsReceived = 0;
            _identicalResults = 0;
            _lastResult = null;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int FramesCaptured { get { return _framesCaptured; } }
        public int IntermediateResultsReceived { get { return _intermediateResultsReceived; } }
        public int FinalResultsReceived { get { return _finalResultsReceived; } }
        public int IdenticalResultsReceived { get { return _identicalResults; } }
        public long ElapsedMilliseconds { get { return _stopwatch.ElapsedMilliseconds; } }

        public void Start()
        {
            _recognizer.Recognizing += OnSpeechRecognized;
            _recognizer.Recognized += OnSpeechRecognized;
            _recognizer.SpeechStartDetected += OnSpeechStartDetected;
            _recognizer.Canceled += OnRecognitionCancelled;

            _recognizer.StartContinuousRecognitionAsync().Wait();
            _audioCapture.NewFrame += OnAudioFrameCaptured;
            _audioCapture.Start();
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
            _recognizer.Recognizing -= OnSpeechRecognized;
            _recognizer.Recognized -= OnSpeechRecognized;
            _recognizer.SpeechStartDetected -= OnSpeechStartDetected;
            _recognizer.Canceled -= OnRecognitionCancelled;
            _audioCapture.NewFrame -= OnAudioFrameCaptured;
            _audioCapture.SignalToStop();
            _audioCapture.WaitForStop();
            _recognizer.StopContinuousRecognitionAsync().Wait();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _recognizer.Dispose();
                    _audioCapture.Dispose();
                    _audio.Dispose();
                    _transcript.Dispose();
                }

                _disposed = true;
            }
        }

        private PushAudioInputStream CreateAudioInputStream()
        {
            return AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM((uint)SamplesPerMillisecond * 1000, 16, 1));
        }

        private AudioCaptureDevice CreateAudioCaptureDevice()
        {
            return new AudioCaptureDevice()
            {
                Format = SampleFormat.Format16Bit,
                SampleRate = SamplesPerMillisecond * 1000,
                Channels = 1,
                DesiredFrameSize = SamplesPerMillisecond * _millisecondsPerFrame
            };
        }

        private void OnAudioFrameCaptured(object sender, NewFrameEventArgs e)
        {
            Interlocked.Increment(ref _framesCaptured);
            Trace.WriteLine($"Sending {e.Signal.RawData.Length}");
            _audioInput.Write(e.Signal.RawData);
            _audio.Write(e.Signal.RawData, 0, e.Signal.RawData.Length);
            _audio.Flush();
        }

        private void OnSpeechRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            string previousResult = Interlocked.Exchange(ref _lastResult, e.Result.Text);

            switch (e.Result.Reason)
            {
                case ResultReason.RecognizingSpeech:
                    Interlocked.Increment(ref _intermediateResultsReceived);
                    Console.Out.Write($"[{e.Result.Text.Length}]\r");
                    if (previousResult != null && previousResult.Equals(e.Result.Text))
                    {
                        Interlocked.Increment(ref _identicalResults);
                    }
                    break;

                case ResultReason.RecognizedSpeech:
                    Interlocked.Increment(ref _finalResultsReceived);
                    foreach (DetailedSpeechRecognitionResult result in SpeechRecognitionResultExtensions.Best(e.Result))
                    {
                        string confidence = result.Confidence.ToString("F2");
                        string text = $"{confidence}|{result.Text}";
                        Trace.WriteLine(text);
                        _transcript.WriteLine(text);
                        Console.Out.WriteLine(text);
                    }
                    _transcript.WriteLine();
                    _transcript.Flush();
                    break;
            }
        }

        private void OnSpeechStartDetected(object sender, RecognitionEventArgs e)
        {
            Console.Out.WriteLine("Speech start detected.");
        }

        private void OnRecognitionCancelled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            Console.Error.WriteLine($"Recognition cancelled: Session={e.SessionId}|{e.Reason}");
            Console.Error.WriteLine(e.ErrorDetails);
        }
    }
}
