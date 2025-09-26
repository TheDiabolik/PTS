using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal sealed class FrameWithRoi : IDisposable
    {
        public Mat Frame;          // işlenmiş BGR frame (tek kopya)
        public List<Rect> Rects;   // global koordinatlar (plural!)
        //public int FrameIndex;     // debug için faydalı
        //public long Ticks;         // zaman damgası (Stopwatch.GetTimestamp())

        public void Dispose()
        {
            Frame?.Dispose();

            Frame = null;
            Rects = null;
        }
    }
}
