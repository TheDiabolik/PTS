using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public record FrameWithROIRect
    {
        public Mat Frame { get; set; }
        public Rect ROI { get; set; }
    }
}
