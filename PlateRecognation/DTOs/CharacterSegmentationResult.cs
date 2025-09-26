using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class CharacterSegmentationResult
    {
        public List<Mat> threshouldPossibleCharacters { get; set; }
        public Mat thresh { get; set; }

        public Mat segmentedPlate { get; set; }
        public Mat colorPlate { get; set; }

        public OpenCvSharp.Rect plateLocation { get; set; }
    }
}
