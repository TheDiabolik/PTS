using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class PlateResult
    {
        public Bitmap frame { get; set; }

        public Bitmap plate { get; set; }
        public Bitmap segmented { get; set; }
        public Bitmap threshould { get; set; }

        public OpenCvSharp.Rect addedRects { get; set; } // Plate'in yer bilgisi

        public string readingPlateResult { get; set; }

        public double readingPlateResultProbability { get; set; }

        public DateTime LastDetectionTime { get; set; } // Tespit edilen son zaman

        public List<Mat> m_characters { get; set; }
    }
}
