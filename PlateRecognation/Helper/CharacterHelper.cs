using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class CharacterHelper
    {
        public static List<MserResult> FilterPossibleCharacterRegions(List<MserResult> mserResults)
        {
            var result = new List<MserResult>();

            MainForm.m_mainForm.m_preProcessingSettings = MainForm.m_mainForm.m_preProcessingSettings.DeSerialize(MainForm.m_mainForm.m_preProcessingSettings);

            int minWidth = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinWidth;
            int maxWidth = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxWidth;

            int minHeight = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinHeight;
            int maxHeight = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxHeight;

            //double minAspectRatio = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinAspectRatio;
            //double maxAspectRatio = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxAspectRatio;

            //double minAspectRatio = Math.Round(RectGeometryHelper.CalculateAspectRatio(minWidth, minHeight), 1); 
            //double maxAspectRatio = RectGeometryHelper.CalculateAspectRatio(maxWidth, maxHeight);

            double minAspectRatio = RectGeometryHelper.CalculateAspectRatio(minWidth, maxHeight);
            double maxAspectRatio = RectGeometryHelper.CalculateAspectRatio(maxWidth, minHeight);

            double minDiagonalLength = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinDiagonalLength;
            double maxDiagonalLength = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxDiagonalLength;



            foreach (var bbox in mserResults)
            {
                // Boyut ve en-boy oranı kriterlerini kontrol edin
                if ((bbox.BBox.Width >= minWidth && bbox.BBox.Width < maxWidth) && (bbox.BBox.Height >= minHeight && bbox.BBox.Height <= maxHeight))
                {
                    //double diagonalLength = RectGeometryHelper.CalculateDiagonalLength(bbox.BBox);
                    double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox.BBox);

                    //if ((aspectRatio >= minAspectRatio && aspectRatio < maxAspectRatio))// && (diagonalLength >= minDiagonalLength && diagonalLength <= maxDiagonalLength)) // Bu aralık karakter olabilecek bölgeler için uygundur
                    {
                        result.Add(bbox);

                    }
                }
                //else
                //{
                //    ////Debug.WriteLine("fsfsd");
                //}
            }

            return result;
        }

        public static List<MserResult> DetectCharactersFromPlate(Mat plateImage)
        {
            // **1️⃣ MSER ile karakter adaylarını bul**
            var characterCandidates = MSEROperations.FindCharactersWithMSERv2(plateImage);

         

            // **2️⃣ Filtreleme ve gruplama işlemleri**
            var filteredCharacters = FilterHelper.FilterAndGroupCharacterCandidatesByAhmet(characterCandidates, plateImage.Rows);

            return filteredCharacters;
        }

        public static List<MserResult> DetectCharactersFromPlateBetaCharacter(Mat plateImage)
        {
            // **1️⃣ MSER ile karakter adaylarını bul**
            var characterCandidates = MSEROperations.FindCharactersWithMSERvBetaTestSon(plateImage);



            // **2️⃣ Filtreleme ve gruplama işlemleri**
            var filteredCharacters = FilterHelper.FilterAndGroupCharacterCandidatesByAhmet(characterCandidates, plateImage.Rows);

            return filteredCharacters;
        }

        public static List<MserResult> DetectCharactersFromPlateBetaCharacterOOP(Mat plateImage)
        {
            MserDetectionSettings mserDetectionSettings = MserDetectionSettingsFactory.GetCharacterRegionSettings(plateImage);

            var characterDetector = new MSERProcessor(mserDetectionSettings);

            var characterCandidates = characterDetector.DetectCharacters(plateImage);

            // **1️⃣ MSER ile karakter adaylarını bul**
            //var characterCandidates = MSEROperations.FindCharactersWithMSERvBetaTestSon(plateImage);



            // **2️⃣ Filtreleme ve gruplama işlemleri**
            var filteredCharacters = FilterHelper.FilterAndGroupCharacterCandidatesByAhmet(characterCandidates, plateImage.Rows);

            return filteredCharacters;
        }

        public static List<MserResult> SmartMergeMserResults(List<MserResult> grayResults, List<MserResult> binaryResults)
        {
            List<MserResult> mergedResults = new List<MserResult>();

            // **Hem Gray hem de Binary’de tespit edilenleri birleştir**
            foreach (var gray in grayResults)
            {
                var matched = binaryResults.FirstOrDefault(b => RectComparisonHelper.AreRectsIntersecting(gray.BBox, b.BBox));

                if (matched != null)
                {
                    // **Ortalama bir Bounding Box al**
                    Rect mergedBox = MergeBoundingBoxes(gray.BBox, matched.BBox);


                    // **Büyük kutuları kontrol et ve ikiye böl**
                    //List<Rect> processedRects = SplitIfTooLarge(mergedBox, grayResults, binaryResults);


                    //foreach (var rect in processedRects)
                    //{
                        mergedResults.Add(new MserResult { BBox = mergedBox, Points = gray.Points });
                    //}
                }
                else
                {
                    // **Sadece Gray’de bulunmuşsa, güvenilirlik kontrolü yap**
                    if (IsValidCharacter(gray, mergedResults))
                    {
                        mergedResults.Add(gray);
                    }
                }
            }

            // **Binary sonuçlarından Gray’de olmayanları ekleyelim**
            foreach (var binary in binaryResults)
            {
                if (!grayResults.Any(g => RectComparisonHelper.AreRectsIntersecting(binary.BBox, g.BBox)))
                {
                    if (IsValidCharacter(binary, mergedResults))
                    {
                        mergedResults.Add(binary);
                    }
                }
            }



        

            return mergedResults.OrderBy(p => p.BBox.X).ToList();
        }


        private static Rect MergeBoundingBoxes(Rect a, Rect b)
        {
            int x = Math.Min(a.X, b.X);
            int y = Math.Min(a.Y, b.Y);
            int width = Math.Max(a.X + a.Width, b.X + b.Width) - x;
            int height = Math.Max(a.Y + a.Height, b.Y + b.Height) - y;

            return new Rect(x, y, width, height);
        }
      
        

        public static bool IsValidCharacterRegion(Mat plateImage, Rect bbox)
        {
            // Seçilen dikdörtgen bölgesini al
            Mat roi = new Mat(plateImage, bbox);
            Cv2.CvtColor(roi, roi, ColorConversionCodes.BGR2GRAY); // Gri tonlamaya çevir
            Cv2.Threshold(roi, roi, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu); // Binary Threshold

            int width = roi.Cols;
            int height = roi.Rows;

            // Yatay ve dikey projeksiyon profillerini oluştur
            int[] horizontalProfile = new int[height];
            int[] verticalProfile = new int[width];

            for (int y = 0; y < height; y++)
                horizontalProfile[y] = Cv2.CountNonZero(roi.Row(y));

            for (int x = 0; x < width; x++)
                verticalProfile[x] = Cv2.CountNonZero(roi.Col(x));

            // Ortalama ve Standart Sapma Hesapla
            double avgH = horizontalProfile.Average();
            double stdDevH = Math.Sqrt(horizontalProfile.Average(v => Math.Pow(v - avgH, 2)));

            double avgV = verticalProfile.Average();
            double stdDevV = Math.Sqrt(verticalProfile.Average(v => Math.Pow(v - avgV, 2)));

            // **Dinamik eşik belirleme**
            double thresholdH = avgH - stdDevH * 0.5;
            double thresholdV = avgV - stdDevV * 0.5;

            // **Eşik değerine göre karakter olup olmadığını kontrol et**
            bool hasValidHorizontalVariation = horizontalProfile.Count(v => v > thresholdH) > 2;
            bool hasValidVerticalVariation = verticalProfile.Count(v => v > thresholdV) > 2;

            return hasValidHorizontalVariation && hasValidVerticalVariation;
        }

        private static bool IsValidCharacter(MserResult candidate, List<MserResult> existingResults)
        {
            if (existingResults.Count == 0)
                return true;

            // **Mevcut karakterlerin ortalama genişliği ve yüksekliği**
            double avgWidth = existingResults.Average(r => r.BBox.Width);
            double avgHeight = existingResults.Average(r => r.BBox.Height);

            double widthDiff = Math.Abs(candidate.BBox.Width - avgWidth);
            double heightDiff = Math.Abs(candidate.BBox.Height - avgHeight);

            // **Eğer boyut farkı çok büyükse, gürültü olabilir**
            return widthDiff < avgWidth * 0.55 && heightDiff < avgHeight * 0.55;
        }

        private static List<Rect> SplitIfTooLarge(Rect candidate, List<MserResult> grayResults, List<MserResult> binaryResults)
        {
            List<Rect> resultRects = new List<Rect>();

            // **Mevcut dikdörtgenlerin ortalama genişliğini hesapla**
            double avgWidth = grayResults.Concat(binaryResults).Average(r => r.BBox.Width);

            // **Eğer bu dikdörtgen ortalamaya göre aşırı büyükse (1.8 katından fazla)**
            if (candidate.Width > avgWidth * 1.6)
            {
                int newWidth = candidate.Width / 2;

                Rect leftHalf = new Rect(candidate.X, candidate.Y, newWidth, candidate.Height);
                Rect rightHalf = new Rect(candidate.X + newWidth, candidate.Y, newWidth, candidate.Height);

                resultRects.Add(leftHalf);
                resultRects.Add(rightHalf);
            }
            else
            {
                resultRects.Add(candidate); // Normal boyutluysa değiştirme
            }

            return resultRects;
        }
    }
}
