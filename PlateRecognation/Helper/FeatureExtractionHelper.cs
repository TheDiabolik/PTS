using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class FeatureExtractionHelper
    {
        public static double[] ExtractHOGFeaturesForPlate(Mat image)
        {

            var hog = new HOGDescriptor(
            new OpenCvSharp.Size(144, 32),  // WinSize: Hedef resim boyutu
            new OpenCvSharp.Size(16, 16),   // BlockSize: HOG bloklarının boyutu
            new OpenCvSharp.Size(8, 8),     // BlockStride: Bloklar arasındaki adım boyutu
            new OpenCvSharp.Size(8, 8),     // CellSize: Hücre boyutu
            9                              // Bin sayısı
            );

            float[] descriptors = hog.Compute(image);
            return Array.ConvertAll(descriptors, x => (double)x);
        }
    }
}
