using Accord.Statistics.Running;
using ConvNetSharp.Core;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace PlateRecognation
{
    public class CameraReader : ICameraReader
    {
        private VideoCapture _capture;

        private Task _readingTask;
        private CancellationTokenSource _cts;


        public event Action<Bitmap> OnFrameCaptured;

        private readonly string _videoSource;
        private bool _isRunning;

        public int Delay { get; set; }

        public bool IsRunning => _isRunning;

        public CameraReader(string videoSource)
        {
            _videoSource = videoSource;
        }

        public bool Start()
        {
            _capture = new VideoCapture(_videoSource);

            if (!_capture.IsOpened())
                return false;

            _isRunning = true;

            int fps = (int)_capture.Get(VideoCaptureProperties.Fps);
            Delay = fps > 0 ? 1000 / fps : 30;

            _cts = new CancellationTokenSource();
            _readingTask = Task.Factory.StartNew(() => CaptureLoop(_cts.Token), TaskCreationOptions.LongRunning);

            return true;
        }

        private void CaptureLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Mat frame = new Mat();

                if (!_capture.Read(frame) || frame.Empty())
                    break;



                using (var bitmap = BitmapConverter.ToBitmap(frame))
                {
                    OnFrameCaptured?.Invoke((Bitmap)bitmap.Clone()); // ya da doğrudan bitmap
                }

                Thread.Sleep(Delay);

            }
        }


        public void Stop()
        {
            _isRunning = false;

            _cts?.Cancel();
            _readingTask?.Wait();

            _capture?.Release();
            _capture?.Dispose();
            _cts?.Dispose();

            
        }
    }
}
