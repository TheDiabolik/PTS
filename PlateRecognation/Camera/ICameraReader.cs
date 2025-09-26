using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public interface ICameraReader
    {
        event Action<Bitmap> OnFrameCaptured;

        bool Start();
        void Stop();

        bool IsRunning { get; }
    }
}
