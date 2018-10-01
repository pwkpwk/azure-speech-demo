﻿namespace Speech.Demo.Desktop
{
    using Accord.Audio;
    using Accord.DirectSound;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using System;
    using System.IO;

    sealed class RecognitionDemo : IDisposable
    {
        private bool _disposed;
        private readonly SpeechRecognizer _recognizer;
        private readonly PushAudioInputStream _audioInput;
        private readonly AudioCaptureDevice _audioCapture;
        private readonly Stream _dump;

        public RecognitionDemo(string region, string key, string locale)
        {
            _disposed = false;
            SpeechConfig config = SpeechConfig.FromSubscription(key, region);
            config.SpeechRecognitionLanguage = locale;
            config.OutputFormat = OutputFormat.Detailed;
            _audioInput = CreateAudioInputStream();
            _recognizer = new SpeechRecognizer(config, AudioConfig.FromStreamInput(_audioInput));
            _audioCapture = CreateAudioCaptureDevice();
            _dump = new FileStream("dump.raw", FileMode.Create);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            _recognizer.Recognizing += OnSpeechRecognized;
            _recognizer.Recognized += OnSpeechRecognized;

            _recognizer.StartContinuousRecognitionAsync().Wait();
            _audioCapture.NewFrame += OnAudioFrameCaptured;
            _audioCapture.Start();
        }

        public void Stop()
        {
            _recognizer.Recognizing -= OnSpeechRecognized;
            _recognizer.Recognized -= OnSpeechRecognized;
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
                    _dump.Dispose();
                }

                _disposed = true;
            }
        }

        private PushAudioInputStream CreateAudioInputStream()
        {
            return AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        }

        private AudioCaptureDevice CreateAudioCaptureDevice()
        {
            return new AudioCaptureDevice()
            {
                Format = SampleFormat.Format16Bit,
                SampleRate = 16000,
                Channels = 1,
                DesiredFrameSize = 1600
            };
        }

        private void OnAudioFrameCaptured(object sender, NewFrameEventArgs e)
        {
            _audioInput.Write(e.Signal.RawData);
            _dump.Write(e.Signal.RawData, 0, e.Signal.RawData.Length);
        }

        private void OnSpeechRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            switch (e.Result.Reason)
            {
                case ResultReason.RecognizingSpeech:
                    Console.Out.Write($"{e.Result.Text} <--\r");
                    break;

                case ResultReason.RecognizedSpeech:
                    Console.Out.WriteLine($"{e.Result.Text} <<--");
                    break;
            }
        }
    }
}
