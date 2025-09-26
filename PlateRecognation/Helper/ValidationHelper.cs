using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    class ValidationHelper
    {
        /// <summary>
        /// Birden fazla dikdörtgeni kontrol ederek geçerli olanları döner
        /// </summary>
        public static Rect[] FilterValidPlates(Rect[] rects, Mat grayImage, Mat binaryImage)
        {
            List<Rect> validPlates = new List<Rect>();

            foreach (var rect in rects)
            {
                if (IsValidPlate(rect, grayImage, binaryImage))
                {
                    validPlates.Add(rect);
                }
            }

            return validPlates.ToArray();
        }


        /// <summary>
        /// Tek bir dikdörtgenin plaka olup olmadığını doğrulayan method
        /// </summary>
        public static bool IsValidPlate(Rect rect, Mat grayImage, Mat binaryImage)
        {
            // 1️⃣ Genişlik / Yükseklik sınırı (temel filtre)
            if (rect.Width < 50 || rect.Height < 17)
                return false;

            // 2️⃣ ROI seç
            Mat roiGray = new Mat(grayImage, rect);
            Mat roiBinary = new Mat(binaryImage, rect);


            //// 3️⃣ Kenar Yoğunluğu (CountNonZero)
            int edgeDensity = Cv2.CountNonZero(roiBinary);

            if (edgeDensity < 300 || edgeDensity > 64000)
                return false;

            //// 4️⃣ Doluluk Oranı (ContourArea / BB Area)
            double contourArea = Cv2.CountNonZero(roiBinary); // Doluluk ölçütü
            int area = rect.Width * rect.Height;
            double fillRatio = contourArea / (double)area;

            //if (fillRatio < 0.2 || fillRatio > 0.9)
            //    return false;

            // 5️⃣ Ortalama Parlaklık (grayscale)
            //Scalar meanGray = Cv2.Mean(roiGray);
            //if (meanGray.Val0 < 50 || meanGray.Val0 > 230)
            //    return false;



            return true; // Plaka olabilir!
        }


        public static bool IsValidPlateDeneme(Rect rect, Mat roiGray, Mat roiBinary)
        {
            // 1️⃣ Genişlik / Yükseklik sınırı (temel filtre)
            //if (rect.Width < 50 || rect.Height < 17)
            //    return false;

            // 2️⃣ ROI seç
            //Mat roiGray = new Mat(grayImage, rect);
            //Mat roiBinary = new Mat(binaryImage, rect);


            // 3️⃣ Kenar Yoğunluğu (CountNonZero)
            int edgeDensity = Cv2.CountNonZero(roiBinary);
            int area = rect.Width * rect.Height;
            if (edgeDensity < 500 || edgeDensity > 64000)
                return false;

            // 4️⃣ Doluluk Oranı (ContourArea / BB Area)
            double contourArea = Cv2.CountNonZero(roiBinary); // Doluluk ölçütü
            double fillRatio = contourArea / (double)area;
            if (fillRatio < 0.2 || fillRatio > 0.9)
                return false;

            // 5️⃣ Ortalama Parlaklık (grayscale)
            Scalar meanGray = Cv2.Mean(roiGray);
            if (meanGray.Val0 < 50 || meanGray.Val0 > 230)
                return false;



            return true; // Plaka olabilir!
        }
    }
}
