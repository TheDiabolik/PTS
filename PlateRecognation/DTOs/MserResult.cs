using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class MserResult
    {
        public OpenCvSharp.Point[] Points { get; set; }
        public double Area { get; set; }
        public OpenCvSharp.Rect BBox { get; set; }
    }

}
