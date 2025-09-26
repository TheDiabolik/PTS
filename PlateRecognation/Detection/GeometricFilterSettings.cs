using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class GeometricFilterSettings
    {
        private const int BaseWidth = 640;
        private const int BaseHeight = 480;
        private const double BaseAspectRatio = 4.8;


        public int MinWidth;
        public int MaxWidth;
        public int MinHeight;
        public int MaxHeight;
        public double MinAspectRatio;
        public double MaxAspectRatio;



        public static GeometricFilterSettings GetSettingsForResolution(int currentWidth, int currentHeight)
        {
            double wScale = currentWidth / (double)BaseWidth;
            double hScale = currentHeight / (double)BaseHeight;

            double estimatedPlateWidth = BaseWidth / 5.0;
            double estimatedPlateHeight = BaseHeight / 15.0;

            //geometriksclae

            return new GeometricFilterSettings
            {
                MinWidth = (int)(estimatedPlateWidth * 0.5 * wScale),
                MaxWidth = (int)(estimatedPlateWidth * 1.5 * wScale),
                MinHeight = (int)(estimatedPlateHeight * 0.5 * hScale),
                MaxHeight = (int)(estimatedPlateHeight * 1.5 * hScale),
                MinAspectRatio = BaseAspectRatio * 0.523,
                MaxAspectRatio = BaseAspectRatio * 1.9


            };
        }
    }
}
