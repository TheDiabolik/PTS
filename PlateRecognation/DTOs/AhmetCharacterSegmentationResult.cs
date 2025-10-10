using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class AhmetCharacterSegmentationResult
    {
        public List<Rect> locationThreshouldPossibleCharacters { get; set; }
        public List<Mat> threshouldPossibleCharacters { get; set; }
        public Mat thresh { get; set; }

        //public Mat segmentedPlate { get; set; }
        //public Mat colorPlate { get; set; }

    }

    public record CharacterDTO
    {
        public Mat Character { get; set; }
        public Rect ROI { get; set; }

        public double Area { get; set; }

        public string OCRResult { get; set; }

        public List<(string, int, double)> Confidance { get; set; }
    }







    public record CharacterWithROI
    {
        public Mat Character { get; set; }
        public Rect ROI { get; set; }

        public double Area { get; set; }
    }

    public record CharacterWithConfidance
    {
        public Mat Character { get; set; }

        public string OCRResult { get; set; }

        public Rect ROI { get; set; }

        public double Area { get; set; }
        public List<(string, double)> Confidance { get; set; }
}
}
