using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class PlateDetectionSettings
    {
        public int MinWidth;
        public int MaxWidth;
        public int MinHeight;
        public int MaxHeight;
        public double MinAspectRatio;
        public double MaxAspectRatio;
        public int MserMinArea;
        public Size SobelKernel;
    }
}
