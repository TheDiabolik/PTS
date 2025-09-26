using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class PossiblePlate
    {
        public Mat frame { get; set; }

        public Mat possiblePlateRegions { get; set; }
        public OpenCvSharp.Rect addedRects { get; set; }



        // Yeni eklenen scoring alanları
        public double PlateScore { get; set; } = -1; // Başlangıçta henüz hesaplanmamış

        public double AspectRatioScore { get; set; }
        public double BlurScore { get; set; }
        public double EdgeDensityScore { get; set; }
    }
}
