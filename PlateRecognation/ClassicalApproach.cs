using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public static class ClassicalApproach
    {
        public static Rect[] FindPlateRegionWithCanny(Mat grayImage)
        {
            List<Rect> loo = new List<Rect>();

            Random rng = new Random();

            Mat edges = new Mat();
            Cv2.Canny(grayImage, edges, 100, 200);

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            Mat frameWithPlates = grayImage.Clone();

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                loo.Add(boundingBox);

                //if (boundingBox.Width > 60 && boundingBox.Width < 100 && boundingBox.Height > 20 && boundingBox.Height < 150)
                //{
                //    Scalar color = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256));
                //    Cv2.Rectangle(frameWithPlates, boundingBox, color, 4);
                //}
            }

            return loo.ToArray();
        }

        public static Rect[] FindPlateRegionWithSobel(Mat grayImage)
        {
            List<Rect> loo = new List<Rect>();


            // **Gürültüyü azaltmak için Gaussian Blur**
            Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // **Sobel X Operatörü (Yatay Kenarları Tespit Eder)**
            Mat sobelX = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);

            //// **Eşikleme Uygula (Binary Threshold)**
            Mat thresh = new Mat();
            Cv2.Threshold(sobelX, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 3)));


            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);


            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);


                if (boundingBox.Width > 30 && boundingBox.Height > 15)
                {
                    loo.Add(boundingBox);
                    //    Scalar color = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256));
                    //    Cv2.Rectangle(frameWithPlates, boundingBox, color, 4);
                }
            }


            return loo.ToArray();
        }


        public static Rect[] FindPlateRegionWithSobel1(Mat grayImage)
        {
            List<Rect> possiblePlates = new List<Rect>();

            // **Gürültüyü azaltmak için Gaussian Blur**
            //Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // **Sobel X Operatörü (Yatay Kenarları Tespit Eder)**
            Mat sobelX = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);

            // **Eşikleme Uygula (Binary Threshold)**
            Mat thresh = new Mat();
            Cv2.Threshold(sobelX, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // **Morfolojik İşlemler (Küçük Gürültüleri Temizleme)**
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 3)));

            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                // 1️⃣ **Boyut Filtreleme (Mantıklı plaka boyutları)**
                if (boundingBox.Width < 60 || boundingBox.Height < 20)
                    continue;

                // 2️⃣ **Kenar Yoğunluğu Hesapla (CountNonZero)**
                Mat roi = new Mat(thresh, boundingBox);
                int edgeDensity = Cv2.CountNonZero(roi);

                // **Minimum Kenar Yoğunluğu Eşiği (Deneyerek Ayarla)**
                //if ((edgeDensity < 1000) || (edgeDensity > 64000)) // Bu eşiği test ederek uygun değeri bulabilirsin
                if ((edgeDensity < 2000) || (edgeDensity > 30000))
                    continue;

                // 3️⃣ **Doluluk Oranı (ContourArea / BoundingBoxArea)**
                double contourArea = Cv2.ContourArea(contour);
                double boundingBoxArea = boundingBox.Width * boundingBox.Height;
                double fillRatio = contourArea / boundingBoxArea;

                if (fillRatio < 0.3 || fillRatio > 0.9)  // Çok boş veya çok doluysa at
                                                         //if (fillRatio < 0.45 || fillRatio > 0.85)
                    continue;



                Mat roiGray = new Mat(grayImage, boundingBox);
                Scalar meanIntensity = Cv2.Mean(roiGray);

                //// Histogram tabanlı filtreleme
                //if (meanIntensity.Val0 < 90 || meanIntensity.Val0 > 210)
                //    continue;

                // **Plaka olarak değerlendir**
                possiblePlates.Add(boundingBox);
            }

            return possiblePlates.ToArray();
        }


        public static Rect[] FindPlateRegionWithSobel2(Mat grayImage)
        {
            List<Rect> possiblePlates = new List<Rect>();

            // **Sobel X Operatörü (Yatay Kenarları Tespit Eder)**
            Mat sobelX = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);

            // **Eşikleme Uygula (Binary Threshold)**
            Mat thresh = new Mat();
            Cv2.Threshold(sobelX, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // **Morfolojik İşlemler (Küçük Gürültüleri Temizleme)**
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close,
                Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 3)));

            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                // **Sadece BoundingBox döndürülüyor, filtreleme başka yerde yapılacak**
                possiblePlates.Add(boundingBox);
            }

            return possiblePlates.ToArray();
        }

        public static Rect[] FindPlateRegionWithSobel3(Mat grayImage)
        {
            List<Rect> possiblePlates = new List<Rect>();

            // Sobel X ve Y
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_8U, 0, 1, 3);


            // Kenarları birleştir (Ağırlıklı veya mutlak toplam)
            Mat sobelCombined = new Mat();
            Cv2.AddWeighted(sobelX, 1, sobelY, 1, 0, sobelCombined);

            // **Eşikleme Uygula (Binary Threshold)**
            Mat thresh = new Mat();
            Cv2.Threshold(sobelCombined, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // **Morfolojik İşlemler (Küçük Gürültüleri Temizleme)**
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close,
                Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 3)));

            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                // **Sadece BoundingBox döndürülüyor, filtreleme başka yerde yapılacak**
                possiblePlates.Add(boundingBox);
            }

            return possiblePlates.ToArray();
        }


        public static Rect[] FindPlateRegionWithSobel4(Mat sobelCombined)
        {
            List<Rect> possiblePlates = new List<Rect>();

        
            // **Eşikleme Uygula (Binary Threshold)**
            Mat thresh = new Mat();
            Cv2.Threshold(sobelCombined, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // **Morfolojik İşlemler (Küçük Gürültüleri Temizleme)**
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close,
                Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21,3)));

            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                // **Sadece BoundingBox döndürülüyor, filtreleme başka yerde yapılacak**
                possiblePlates.Add(boundingBox);
            }

            return possiblePlates.ToArray();
        }

        public static Rect[] FindPlateRegionWithSobel4(Mat sobelCombined, SobelDetectionSettings settings)
        {
            List<Rect> possiblePlates = new List<Rect>();

            Mat thresh = new Mat();
            Cv2.Threshold(sobelCombined, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close,
                Cv2.GetStructuringElement(MorphShapes.Rect, settings.MorphKernelSize));

            Cv2.FindContours(thresh, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);
                possiblePlates.Add(boundingBox);
            }

            return possiblePlates.ToArray();
        }

        public static Rect[] FindPlateRegionWithSobelXY(Mat grayImage)
        {
            List<Rect> possiblePlates = new List<Rect>();

            // **Gürültüyü azaltmak için Gaussian Blur**
            //Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // **Sobel X ve Sobel Y Operatörleri (Yatay ve Dikey Kenarlar)**
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_8U, 0, 1, 3);

            // **SobelX ve SobelY'yi birleştir (Mutlak Değer)**
            Mat sobelCombined = new Mat();
            Cv2.AddWeighted(sobelX, 1, sobelY,1, 0, sobelCombined);

            // **Otsu Threshold ile Binarize Et**
            Mat thresh = new Mat();
            Cv2.Threshold(sobelCombined, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // **Morfolojik İşlemler (Gürültüyü Temizleme)**
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 3)));

            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                // **Boyut Filtreleme**
                //if (boundingBox.Width < 30 || boundingBox.Height < 15)
                //    continue;

                // **Kenar Yoğunluğu Hesapla**
                Mat roi = new Mat(thresh, boundingBox);
                int edgeDensity = Cv2.CountNonZero(roi);

                //if ((edgeDensity < 1000) || (edgeDensity > 64000))
                //    continue;

                // **Doluluk Oranı (ContourArea / BoundingBoxArea)**
                double contourArea = Cv2.ContourArea(contour);
                double boundingBoxArea = boundingBox.Width * boundingBox.Height;
                double fillRatio = contourArea / boundingBoxArea;

                //if (fillRatio < 0.3 || fillRatio > 0.9)
                //    continue;

                // **Plaka olarak değerlendir**
                possiblePlates.Add(boundingBox);
            }

            return possiblePlates.ToArray();
        }



        public static Rect[] FindPlateRegionWithSobelV2(Mat grayImage)
        {
            List<Rect> loo = new List<Rect>();


            // **Gürültüyü azaltmak için Gaussian Blur**
            //Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // **Sobel X Operatörü (Yatay Kenarları Tespit Eder)**
            Mat sobelX = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3); // Sadece X yönünde kenarlar
            Cv2.BitwiseNot(sobelX, sobelX);




            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(17, 3));
            Mat morphed = new Mat();
            Cv2.MorphologyEx(sobelX, morphed, MorphTypes.Close, kernel);


            // **Konturları Bul**
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(morphed, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);



            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);


                //if (boundingBox.Width > 30 && boundingBox.Height > 10)
                {
                    loo.Add(boundingBox);
                    //    Scalar color = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256));
                    //    Cv2.Rectangle(frameWithPlates, boundingBox, color, 4);
                }
            }


            return loo.ToArray();
        }
    }
}
