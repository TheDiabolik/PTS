using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class SobelDetectionSettings
    {
        public OpenCvSharp.Size MorphKernelSize { get; set; }
        public int SobelKernelSize { get; set; } = 3; // default 3

        public static SobelDetectionSettings GetAdaptiveSobelSettings(int width, int height)
        {
            double scale = Math.Sqrt((width * height) / (640.0 * 480.0));
            scale = Math.Max(scale, 0.5); // Çok küçük çözünürlüklerde kernel'in yok olmasını engeller



            int kernel = (int)Math.Round(3 * scale);

            if (kernel % 2 == 0) kernel += 1;
            kernel = Math.Clamp(kernel, 3, 7); // hem alt hem üst sınır


            int morphW = (int)(21 * scale);
            int morphH = (int)(3 * scale);
            if (morphW % 2 == 0) morphW += 1;
            if (morphH % 2 == 0) morphH += 1;

            return new SobelDetectionSettings
            {
                MorphKernelSize = new OpenCvSharp.Size(morphW, morphH),
                SobelKernelSize = kernel
            };
        }
    }
}
