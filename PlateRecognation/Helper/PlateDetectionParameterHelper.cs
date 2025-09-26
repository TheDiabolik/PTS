using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class PlateDetectionParameterHelper
    {
        private const int BaseWidth = 640;
        private const int BaseHeight = 480;
        private const double BaseAspectRatio = 4.8;



        public static PlateDetectionSettings GetSettingsForResolution(int currentWidth, int currentHeight)
        {
            double wScale = currentWidth / (double)BaseWidth;
            double hScale = currentHeight / (double)BaseHeight;

            return new PlateDetectionSettings
            {
                MinWidth = (int)(64 * wScale),
                MaxWidth = (int)(192 * wScale),
                MinHeight = (int)(16 * hScale),
                MaxHeight = (int)(48 * hScale),
                MinAspectRatio = 2.4,
                MaxAspectRatio = 9.5,
                MserMinArea = (int)(60 * wScale * hScale),
                SobelKernel = new Size(3, 3) // sabit de bırakılabilir
            };
        }
    }
}
