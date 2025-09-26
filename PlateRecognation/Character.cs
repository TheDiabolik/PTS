using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PlateRecognation
{
    internal class Character
    {

        public static ThreadSafeList<CharacterSegmentationResult> SegmentCharactersInPlate(List<PossiblePlate> possiblePlateRegions)
        {
            ThreadSafeList<CharacterSegmentationResult> segmentedCharacters = new ThreadSafeList<CharacterSegmentationResult>();

            foreach (PossiblePlate possibleRegion in possiblePlateRegions)
            {
                Mat colorPlate = possibleRegion.possiblePlateRegions;
              

                CharacterSegmentationResult possibleCharacterRegions = FindAndCombineCharacterCandidatesv2(colorPlate);

                //CharacterSegmentationResult possibleCharacterRegions = PlateCharacterFinder.FindCharacterInPlateRegion(colorPlate);

                List<Mat> characters = possibleCharacterRegions.threshouldPossibleCharacters;

                //karakter bölgeleri 5 ten fazla 9 dan küçük olması lazım ön filtre
                if ((characters.Count >= 5) && (characters.Count <= 10))
                {
                    possibleCharacterRegions.colorPlate = colorPlate;

                    possibleCharacterRegions.plateLocation = possibleRegion.addedRects;

                    segmentedCharacters.Add(possibleCharacterRegions);
                }
            }

            return segmentedCharacters;
        }

        public static ThreadSafeList<CharacterSegmentationResult> SegmentCharactersInPlate(PossiblePlate possiblePlateRegions)
        {
            ThreadSafeList<CharacterSegmentationResult> segmentedCharacters = new ThreadSafeList<CharacterSegmentationResult>();

            //foreach (PossiblePlate possibleRegion in possiblePlateRegions)
            {
                Mat colorPlate = possiblePlateRegions.possiblePlateRegions;

                //Cv2.Resize(colorPlate, colorPlate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);
                //Cv2.Resize(colorPlate, colorPlate, new OpenCvSharp.Size(120, 40), 0, 0, InterpolationFlags.Lanczos4);

                //Cv2.Resize(colorPlate, colorPlate, new OpenCvSharp.Size(118, 25));

                CharacterSegmentationResult possibleCharacterRegions = FindAndCombineCharacterCandidatesv2(colorPlate);

                //CharacterSegmentationResult possibleCharacterRegions = PlateCharacterFinder.FindCharacterInPlateRegion(colorPlate);

                List<Mat> characters = possibleCharacterRegions.threshouldPossibleCharacters;

                //karakter bölgeleri 5 ten fazla 9 dan küçük olması lazım ön filtre
                if ((characters.Count >= 5) && (characters.Count <= 10))
                {
                    possibleCharacterRegions.colorPlate = colorPlate;

                    possibleCharacterRegions.plateLocation = possiblePlateRegions.addedRects;

                    segmentedCharacters.Add(possibleCharacterRegions);
                }
            }

            return segmentedCharacters;
        }



        public static CharacterSegmentationResult FindCharacterCandidates(Mat possiblePlateRegion)
        {
            CharacterSegmentationResult segmentedCharacter = new CharacterSegmentationResult();

            //List<Mat> possiblePlate = possiblePlateRegions;
            Mat plate = possiblePlateRegion.Clone();
            List<Mat> characterRegions = new List<Mat>();

            //DisplayManager.PictureBoxInvoke(m_pictureBoxCharacterSegmented,  new Bitmap(320,240));

            Random rng = new Random();


            Mat clonePlate = plate.Clone();
            Mat threshPlate = ImagePreProcessingHelper.SelectPreProcessingType(plate);


            var sdf = MSEROperations.SegmentCharacterInPlateWithMSER(threshPlate);

            #region Set Variables
            int minWidth = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinWidth;
            int maxWidth = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxWidth;

            int minHeight = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinHeight;
            int maxHeight = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxHeight;

            double minAspectRatio = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinAspectRatio;
            double maxAspectRatio = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxAspectRatio;

            double minDiagonalLength = MainForm.m_mainForm.m_preProcessingSettings.m_characterMinDiagonalLength;
            double maxDiagonalLength = MainForm.m_mainForm.m_preProcessingSettings.m_characterMaxDiagonalLength;
            #endregion

            foreach (var bbox in sdf)
            {
                // Boyut ve en-boy oranı kriterlerini kontrol edin
                //if ((bbox.BBox.Width >= minWidth && bbox.BBox.Width < maxWidth) && (bbox.BBox.Height >= minHeight && bbox.BBox.Height <= maxHeight))
                {
                    double diagonalLength = RectGeometryHelper.CalculateDiagonalLength(bbox.BBox);
                    double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox.BBox);

                    //if ((aspectRatio >= minAspectRatio && aspectRatio < maxAspectRatio) && (diagonalLength >= minDiagonalLength && diagonalLength <= maxDiagonalLength)) // Bu aralık karakter olabilecek bölgeler için uygundur
                    {
                        // Bu bölgeyi Mat olarak kaydedin
                        Mat characterRegion = new Mat(threshPlate, bbox.BBox);
                        characterRegions.Add(characterRegion);

                        Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                        Cv2.FillPoly(clonePlate, new OpenCvSharp.Point[][] { bbox.Points }, randomColor); // Bölgeyi rastgele renkle doldur

                        DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(clonePlate));
                    }
                }
            }


            segmentedCharacter.thresh = threshPlate;
            segmentedCharacter.threshouldPossibleCharacters = characterRegions;
            segmentedCharacter.segmentedPlate = clonePlate;



            return segmentedCharacter;
        }

        public static Mat ApplyAdaptiveThresholdSmart(Mat grayPlate)
        {
            double mean = Cv2.Mean(grayPlate).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayPlate);

            int blockSize = 19;
            int cValue = 7;

            // Dinamik eşikleme mantığı
            if (mean > 200 && stdDev < 30) // Aşırı parlak ve düşük varyasyon
            {
                blockSize = 25;
                cValue = 12;
            }
            else if (mean > 200 && stdDev >= 30)
            {
                blockSize = 23;
                cValue = 9;
            }
            else if (mean < 90 && stdDev < 20) // Çok karanlık ve zayıf detay
            {
                blockSize = 17;
                cValue = 2;
            }
            else if (mean < 90 && stdDev >= 20)
            {
                blockSize = 19;
                cValue = 4;
            }
            else if (stdDev < 15) // Zayıf kontrast sahneler
            {
                blockSize = 17;
                cValue = 6;
            }

            // blockSize tek sayı olmalı
            if (blockSize % 2 == 0)
                blockSize += 1;

            Mat binary = new Mat();
            Cv2.AdaptiveThreshold(grayPlate, binary, 255,
                AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv,
                blockSize, cValue);

            return binary;
        }
        public static Mat ApplyAdaptiveThresholdSmart_Çalışan(Mat grayPlate)
        {
            int width = grayPlate.Width;
            double localMean = Cv2.Mean(grayPlate).Val0;

            int blockSize;
            int cValue;

            if (localMean > 190) // Aşırı parlak
            {
                blockSize = 25;
                cValue = 10;
            }
            else if (localMean < 100) // Karanlık
            {
                blockSize = 15;
                cValue = 3;
            }
            else // Orta
            {
                blockSize = 19;
                cValue = 7;
            }

            // blockSize tek sayı olmalı
            if (blockSize % 2 == 0) blockSize += 1;

            Mat binary = new Mat();
            Cv2.AdaptiveThreshold(grayPlate, binary, 255,
                AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv,
                blockSize, cValue);

            return binary;
        }

        public static Mat ApplyAdaptiveThresholdSmartForTestRevV2(Mat grayPlate)
        {
            int width = grayPlate.Width;
            double mean = Cv2.Mean(grayPlate).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayPlate);

            int blockSize;
            int cValue;

            // 🌕 Aşırı parlak plaka → yüksek blockSize + yüksek C (fazla ışık bastırılır)
            if (mean > 200)
            {
                blockSize = 25;
                cValue = 10;
            }
            // 🌥 Düşük parlaklık ve düşük varyasyon → daha duyarlı eşikleme
            else if (mean < 90 && stdDev < 25)
            {
                blockSize = 13;
                cValue = 2;
            }
            // 🌒 Karanlık ama detaylı → daha küçük block + düşük C
            else if (mean < 95)
            {
                blockSize = 15;
                cValue = 3;
            }
            // 🌤 Orta seviye plaka → dengeli eşikleme
            else if (mean >= 100 && mean <= 140)
            {
                blockSize = 17;
                cValue = 6;
            }
            // ☁️ Genel geçer normal plaka
            else
            {
                blockSize = 21;
                cValue = 7;
            }

            // Ek Güvenlik: Görüntü küçükse blockSize'ı orantılı kısıtla
            blockSize = Math.Min(blockSize, (int)(width * 0.15));
            if (blockSize % 2 == 0) blockSize += 1;

            Mat binary = new Mat();
            Cv2.AdaptiveThreshold(grayPlate, binary, 255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.BinaryInv,
                blockSize,
                cValue);

            return binary;
        }

        public static Mat ApplyAdaptiveThresholdSmartFortTest(Mat grayPlate)
        {
            int width = grayPlate.Width;
            double mean = Cv2.Mean(grayPlate).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayPlate);

            int blockSize;
            int cValue;

            if (mean > 190)
            {
                blockSize = 25;
                cValue = 10;
            }
            else if (mean < 100)
            {
                blockSize = 15;
                cValue = 3;
            }
            else if (mean >= 100 && mean <= 140)
            {
                blockSize = 17;
                cValue = 6;
            }
            else
            {
                blockSize = 21;
                cValue = 8;
            }

            // Ek: küçük görüntülerde blockSize fazla olmasın
            blockSize = Math.Min(blockSize, (int)(width * 0.15));
            if (blockSize % 2 == 0) blockSize += 1;

            Mat binary = new Mat();
            Cv2.AdaptiveThreshold(grayPlate, binary, 255,
                AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv,
                blockSize, cValue);

            return binary;
        }

        public static Mat ApplyGradientFusionThreshold(Mat grayPlate)
        {
            // 1️⃣ Adaptive Threshold (akıllı)
            Mat adaptive = ApplyAdaptiveThresholdSmart(grayPlate);

            // 2️⃣ Sobel (Yatay Kenarlar için)
            Mat sobel = new Mat();
            Cv2.Sobel(grayPlate, sobel, MatType.CV_8U, 1, 0, 3);
            Cv2.Threshold(sobel, sobel, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // 3️⃣ Bitwise AND → ortak kenar + bölgesel bilgi
            Mat fused = new Mat();
            Cv2.BitwiseAnd(adaptive, sobel, fused);

            // 4️⃣ Gürültü için hafif morfoloji (isteğe bağlı)
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(fused, fused, MorphTypes.Close, kernel);

            return fused;
        }

        public static Mat ApplyGradientFusionThresholdV2(Mat grayPlate)
        {
            // 1️⃣ Adaptive Threshold (akıllı, ayarlanabilir)
            Mat adaptive = ApplyAdaptiveThresholdSmart(grayPlate); // dıştan gelen versiyon

            // 2️⃣ Sobel X ve Y (kenar tespiti)
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayPlate, sobelX, MatType.CV_8U, 1, 0, 5); // daha yumuşak kenarlar
            Cv2.Sobel(grayPlate, sobelY, MatType.CV_8U, 0, 1, 5);

            // 3️⃣ Binary Threshold (Otsu ile)
            Cv2.Threshold(sobelX, sobelX, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            Cv2.Threshold(sobelY, sobelY, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // 4️⃣ Kenarları birleştir (OR)
            Mat combinedSobel = new Mat();
            Cv2.BitwiseOr(sobelX, sobelY, combinedSobel);

            // 5️⃣ Adaptive + Sobel birleşimi (OR)
            Mat fused = new Mat();
            Cv2.BitwiseOr(adaptive, combinedSobel, fused);

            // 6️⃣ Gürültü azaltma (Opening deneyelim)
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(fused, fused, MorphTypes.Open, kernel);

            return fused;
        }


        public static Mat ApplyGradientFusionThresholdFORXY(Mat grayPlate)
        {
            // 1️⃣ Adaptive Threshold (akıllı)
            Mat adaptive = ApplyAdaptiveThresholdSmartFortTest(grayPlate);

            // 2️⃣ Sobel X (Yatay Kenar) + Sobel Y (Dikey Kenar)
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayPlate, sobelX, MatType.CV_8U, 1, 0, 3);
            Cv2.Sobel(grayPlate, sobelY, MatType.CV_8U, 0, 1, 3);

            // 3️⃣ Eşikleme (Otsu)
            Cv2.Threshold(sobelX, sobelX, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            Cv2.Threshold(sobelY, sobelY, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // 4️⃣ Sobel X + Y birleştir → Tüm kenarları temsil eden birleşik sobel görüntüsü
            Mat combinedSobel = new Mat();
            Cv2.BitwiseOr(sobelX, sobelY, combinedSobel);

            // 5️⃣ Adaptive & Sobel birleşimi
            Mat fused = new Mat();
            Cv2.BitwiseAnd(adaptive, combinedSobel, fused);

            // 6️⃣ Gürültü azaltmak için morfolojik Close işlemi
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(fused, fused, MorphTypes.Close, kernel);

            return fused;
        }

        //public static CharacterSegmentationResult FindAndCombineCharacterCandidatesv1(Mat possiblePlateRegion)
        //{
        //    CharacterSegmentationResult segmentedCharacter = new CharacterSegmentationResult();

           
        //    List<Mat> characterRegions = new List<Mat>();

        //    //Cv2.Resize(colorPlate, colorPlate, new OpenCvSharp.Size(144, 33), 0, 0, InterpolationFlags.Lanczos4);

        //    Mat cloneColorPlate = possiblePlateRegion.Clone();
        //    Mat cloneColorPlate1 = possiblePlateRegion.Clone();
        //    Mat cloneColorPlate2 = possiblePlateRegion.Clone();
        //    Mat cloneColorPlate3 = possiblePlateRegion.Clone();
        //    Mat cloneColorPlate4 = possiblePlateRegion.Clone();

        //    Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(possiblePlateRegion);

        //    Mat grayPlate1 = grayPlate.Clone();

        //    //Mat adaptiveThresholdPlate = new Mat();

        //    //Mat adaptiveThresholdPlate = ApplyAdaptiveThresholdSmart(grayPlate);

        //    Mat adaptiveThresholdPlate = ApplyGradientFusionThresholdFORXY(grayPlate);

        //    //Cv2.Threshold(grayPlate, adaptiveThresholdPlate, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
        //    //Cv2.AdaptiveThreshold(grayPlate, adaptiveThresholdPlate, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.BinaryInv, 17, 5);
        //    //Cv2.AdaptiveThreshold(grayPlate, adaptiveThresholdPlate, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 21, 7);
        //    //Mat adaptiveThresholdPlate = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(colorPlate.Clone());

        //    //Mat adaptiveThresholdPlate = AutoThreshold(grayPlate.Clone());

        //    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(adaptiveThresholdPlate));



        //    // **Erosion İçin Kernel Tanımla**
        //    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));

        //    // **Erosion Uygula (Beyaz Alanları Aşındırarak Karakterleri Kalınlaştırır)**
        //    //Cv2.Erode(adaptiveThresholdPlate, adaptiveThresholdPlate, kernel, iterations: 1);
        //    //Cv2.Dilate(adaptiveThresholdPlate, adaptiveThresholdPlate, kernel, iterations: 1);


        //    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(adaptiveThresholdPlate));


        //    List<MserResult> possibleCharacterRegionsInGray = CharacterHelper.DetectCharactersFromPlate(grayPlate);
        //    List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

        //    //List<MserResult> mergedResults = CharacterHelper.SmartMergeMserResults(possibleCharacterRegionsInGray, possibleCharacterRegionsInBinary);

        //    Random rng = new Random();




        //    //var sdsd = PlateCharacterFinder.ClusterCharacterBoundingBoxes(possibleCharacterRegionsInGray);


        //    //foreach (var item in sdsd)
        //    //{
        //    //    Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
        //    //    Cv2.Rectangle(cloneColorPlate4, item, randomColor);
        //    //}

        //    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(cloneColorPlate4));





        //    #region rect çizmek için
        //    foreach (var item in possibleCharacterRegionsInGray)
        //    {
        //        Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
        //        Cv2.Rectangle(cloneColorPlate1, item.BBox, randomColor);
        //    }

        //    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(cloneColorPlate1));

        //    foreach (var item in possibleCharacterRegionsInBinary)
        //    {
        //        Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
        //        Cv2.Rectangle(cloneColorPlate2, item.BBox, randomColor);
        //    }

        //    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(cloneColorPlate2));
        //    #endregion


        //    foreach (MserResult bbox in possibleCharacterRegionsInGray)
        //    {
        //        // Bu bölgeyi Mat olarak kaydedin
        //        Mat characterRegion = new Mat(grayPlate, bbox.BBox);

        //        Mat characterRegionBinaryImage = new Mat();
        //        Cv2.Threshold(characterRegion, characterRegionBinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);


        //        // **Erosion İçin Kernel Tanımla**
        //        //Mat kernel8 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));

        //        // **Erosion Uygula (Beyaz Alanları Aşındırarak Karakterleri Kalınlaştırır)**
        //        //Cv2.Erode(adaptiveThresholdPlate, adaptiveThresholdPlate, kernel, iterations: 1);
        //        //Cv2.Erode(characterRegionBinaryImage, characterRegionBinaryImage, kernel8, iterations: 1);


        //        //Cv2.Resize(characterRegionBinaryImage, characterRegionBinaryImage, new OpenCvSharp.Size(20, 20), 0, 0, InterpolationFlags.Lanczos4);

        //        ///normal segmentasyon
        //        characterRegions.Add(characterRegionBinaryImage);

        //        Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
        //        Cv2.Rectangle(cloneColorPlate, bbox.BBox, randomColor);

        //    }

        //    //foreach (var bbox in sdsd)
        //    //{
        //    //    // Bu bölgeyi Mat olarak kaydedin
        //    //    Mat characterRegion = new Mat(grayPlate, bbox);

        //    //    Mat characterRegionBinaryImage = new Mat();
        //    //    Cv2.Threshold(characterRegion, characterRegionBinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);


        //    //    // **Erosion İçin Kernel Tanımla**
        //    //    //Mat kernel8 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));

        //    //    // **Erosion Uygula (Beyaz Alanları Aşındırarak Karakterleri Kalınlaştırır)**
        //    //    //Cv2.Erode(adaptiveThresholdPlate, adaptiveThresholdPlate, kernel, iterations: 1);
        //    //    //Cv2.Erode(characterRegionBinaryImage, characterRegionBinaryImage, kernel8, iterations: 1);


        //    //    //Cv2.Resize(characterRegionBinaryImage, characterRegionBinaryImage, new OpenCvSharp.Size(20, 20), 0, 0, InterpolationFlags.Lanczos4);

        //    //    ///normal segmentasyon
        //    //    characterRegions.Add(characterRegionBinaryImage);

        //    //    Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
        //    //    Cv2.Rectangle(cloneColorPlate, bbox, randomColor);

        //    //}


        //    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(cloneColorPlate));



        //    Mat binaryPlate = ImagePreProcessingHelper.ColorMatToBinary(possiblePlateRegion);


        //    segmentedCharacter.thresh = binaryPlate;
        //    segmentedCharacter.threshouldPossibleCharacters = characterRegions;
        //    segmentedCharacter.segmentedPlate = cloneColorPlate;

        //    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(cloneColorPlate));

        //    return segmentedCharacter;
        //}


        public static CharacterSegmentationResult FindAndCombineCharacterCandidatesv2(Mat possiblePlateRegion)
        {
            CharacterSegmentationResult segmentedCharacter = new CharacterSegmentationResult();


            List<Mat> characterRegions = new List<Mat>();


            Mat cloneColorPlate = possiblePlateRegion.Clone();
            Mat cloneColorPlate1 = possiblePlateRegion.Clone();
            //Mat cloneColorPlate2 = possiblePlateRegion.Clone();
            //Mat cloneColorPlate3 = possiblePlateRegion.Clone();
            //Mat cloneColorPlate4 = possiblePlateRegion.Clone();

            //Mat enhanced = PlateEnhancementHelper.ApplyPlateSpecificEnhancementvGereksizseYapma(possiblePlateRegion);
            //Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(enhanced);

            Mat enhanced = PlateEnhancementHelper.CheckPlateStatusForBetaTest(possiblePlateRegion);
            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(enhanced);

            Mat grayPlate22 = ImagePreProcessingHelper.ColorMatToGray(possiblePlateRegion);

            //Mat grayPlate1 = grayPlate.Clone();

            //Mat adaptiveThresholdPlate = new Mat();
            //Cv2.AdaptiveThreshold(grayPlate, adaptiveThresholdPlate, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 21, 7);
            //Mat adaptiveThresholdPlate = ApplyAdaptiveThresholdSmartForTestRevV2(grayPlate);

            //Mat adaptiveThresholdPlate = ApplyAdaptiveThresholdSmart(grayPlate);

            //Mat adaptiveThresholdPlate = ApplyGradientFusionThresholdV2(grayPlate);

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(adaptiveThresholdPlate));

            //List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

            List<MserResult> possibleCharacterRegionsInGray = CharacterHelper.DetectCharactersFromPlateBetaCharacterOOP(grayPlate22);
            //List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

            //var possibleCharacterReeegionsInGray = MSEROperations.SegmentCharacterInPlateDB(grayPlate);

            //List<MserResult> possibleCharacterRegionsInBinary = MSEROperations.FindCharactersWithMSER(adaptiveThresholdPlate);


            //List<MserResult> mergedResults = CharacterHelper.SmartMergeMserResults(possibleCharacterRegionsInGray, possibleCharacterRegionsInBinary);

            Random rng = new Random();


            #region rect çizmek için
            foreach (var item in possibleCharacterRegionsInGray)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate1, item.BBox, randomColor);
            }

            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(cloneColorPlate1));

            //foreach (var item in possibleCharacterRegionsInBinary)
            //{
            //    Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
            //    Cv2.Rectangle(cloneColorPlate2, item.BBox, randomColor);
            //}

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(cloneColorPlate2));
            #endregion


            foreach (MserResult bbox in possibleCharacterRegionsInGray)
            {
                using (Mat characterRegion = new Mat(grayPlate22, bbox.BBox))
                {

                    Mat characterRegionBinaryImage = new Mat();
                    Cv2.Threshold(characterRegion, characterRegionBinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                    //Cv2.AdaptiveThreshold(characterRegion, characterRegionBinaryImage, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 7);

                    ///normal segmentasyon
                    characterRegions.Add(characterRegionBinaryImage);

                    Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                    Cv2.Rectangle(cloneColorPlate, bbox.BBox, randomColor);
                }
            }


            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(cloneColorPlate));


           


            Mat binaryPlate = ImagePreProcessingHelper.ColorMatToBinary(possiblePlateRegion);


            segmentedCharacter.thresh = binaryPlate;
            segmentedCharacter.threshouldPossibleCharacters = characterRegions;
            segmentedCharacter.segmentedPlate = cloneColorPlate;

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(cloneColorPlate));


            //binaryPlate.Dispose();
            grayPlate.Dispose();
            grayPlate22.Dispose();
            //cloneColorPlate.Dispose();

            enhanced.Dispose();

            return segmentedCharacter;
        }

        public static CharacterSegmentationResult FindAndCombineCharacterCandidatesv3(Mat possiblePlateRegion)
        {
            CharacterSegmentationResult segmentedCharacter = new CharacterSegmentationResult();


            List<Mat> characterRegions = new List<Mat>();


            Mat cloneColorPlate = possiblePlateRegion.Clone();
            Mat cloneColorPlate1 = possiblePlateRegion.Clone();
            //Mat cloneColorPlate2 = possiblePlateRegion.Clone();
            //Mat cloneColorPlate3 = possiblePlateRegion.Clone();
            //Mat cloneColorPlate4 = possiblePlateRegion.Clone();

            //Mat enhanced = PlateEnhancementHelper.ApplyPlateSpecificEnhancementvGereksizseYapma(possiblePlateRegion);
            //Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(enhanced);

            Mat enhanced = PlateEnhancementHelper.CheckPlateStatusForBetaTest(possiblePlateRegion);
            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(enhanced);

            Mat grayPlate22 = ImagePreProcessingHelper.ColorMatToGray(possiblePlateRegion);

            //Mat grayPlate1 = grayPlate.Clone();

            //Mat adaptiveThresholdPlate = new Mat();
            //Cv2.AdaptiveThreshold(grayPlate, adaptiveThresholdPlate, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 21, 7);
            //Mat adaptiveThresholdPlate = ApplyAdaptiveThresholdSmartForTestRevV2(grayPlate);

            //Mat adaptiveThresholdPlate = ApplyAdaptiveThresholdSmart(grayPlate);

            //Mat adaptiveThresholdPlate = ApplyGradientFusionThresholdV2(grayPlate);

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(adaptiveThresholdPlate));

            //List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

            List<MserResult> possibleTemp = CharacterHelper.DetectCharactersFromPlateBetaCharacter(grayPlate22);

            List<MserResult> possibleTemp1 = CharacterHelper.DetectCharactersFromPlateBetaCharacter(grayPlate);

            List<MserResult> possibleCharacterRegionsInGray = new List<MserResult>();

            possibleCharacterRegionsInGray.AddRange(possibleTemp);
            possibleCharacterRegionsInGray.AddRange(possibleTemp1);

            var filtered = FilterHelper.FilterAndGroupCharacterCandidatesByAhmet(possibleCharacterRegionsInGray, grayPlate22.Rows);

            //List <MserResult> possibleCharacterRegionsInGray = CharacterHelper.DetectCharactersFromPlateBetaCharacter(grayPlate22);
            //List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

            //var possibleCharacterReeegionsInGray = MSEROperations.SegmentCharacterInPlateDB(grayPlate);

            //List<MserResult> possibleCharacterRegionsInBinary = MSEROperations.FindCharactersWithMSER(adaptiveThresholdPlate);


            //List<MserResult> mergedResults = CharacterHelper.SmartMergeMserResults(possibleCharacterRegionsInGray, possibleCharacterRegionsInBinary);

            Random rng = new Random();


            #region rect çizmek için
            foreach (var item in filtered)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate1, item.BBox, randomColor);
            }

            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(cloneColorPlate1));

            //foreach (var item in possibleCharacterRegionsInBinary)
            //{
            //    Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
            //    Cv2.Rectangle(cloneColorPlate2, item.BBox, randomColor);
            //}

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(cloneColorPlate2));
            #endregion


            foreach (MserResult bbox in filtered)
            {
                using (Mat characterRegion = new Mat(grayPlate22, bbox.BBox))
                {

                    Mat characterRegionBinaryImage = new Mat();
                    Cv2.Threshold(characterRegion, characterRegionBinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                    //Cv2.AdaptiveThreshold(characterRegion, characterRegionBinaryImage, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 7);

                    ///normal segmentasyon
                    characterRegions.Add(characterRegionBinaryImage);

                    Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                    Cv2.Rectangle(cloneColorPlate, bbox.BBox, randomColor);
                }
            }


            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(cloneColorPlate));





            Mat binaryPlate = ImagePreProcessingHelper.ColorMatToBinary(possiblePlateRegion);


            segmentedCharacter.thresh = binaryPlate;
            segmentedCharacter.threshouldPossibleCharacters = characterRegions;
            segmentedCharacter.segmentedPlate = cloneColorPlate;

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(cloneColorPlate));


            //binaryPlate.Dispose();
            grayPlate.Dispose();
            grayPlate22.Dispose();
            //cloneColorPlate.Dispose();

            enhanced.Dispose();

            return segmentedCharacter;
        }

        public static int ComputeSobelEdgeDensity(Mat grayImage)
        {
            // **Sobel X ve Sobel Y hesapla**
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_64F, 1, 0, ksize: 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_64F, 0, 1, ksize: 3);

            // **Gradyan büyüklüğünü hesapla**
            Mat gradientMagnitude = new Mat();
            Cv2.Magnitude(sobelX, sobelY, gradientMagnitude);

            // **Normalize edilip 8-bit formata çevrildi**
            Mat gradientMagnitude8U = new Mat();
            Cv2.Normalize(gradientMagnitude, gradientMagnitude, 0, 255, NormTypes.MinMax);
            gradientMagnitude.ConvertTo(gradientMagnitude8U, MatType.CV_8U);

            // **Otsu ile otomatik eşik belirleme ve binary maske oluşturma**
            Mat edgeMask = new Mat();
            Cv2.Threshold(gradientMagnitude8U, edgeMask, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // **Kenarlardaki beyaz piksel sayısını hesapla**
            int edgeDensity = Cv2.CountNonZero(edgeMask);

            return edgeDensity;
        }

        public static double ComputeSobelEdgeDensity(Mat grayImage, out double pixelPercentage)
        {
            // **Sobel X ve Sobel Y hesapla**
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_64F, 1, 0, ksize: 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_64F, 0, 1, ksize: 3);

            // **Gradyan büyüklüğünü hesapla**
            Mat gradientMagnitude = new Mat();
            Cv2.Magnitude(sobelX, sobelY, gradientMagnitude);

            // **Normalize edilip 8-bit formata çevrildi**
            Mat gradientMagnitude8U = new Mat();
            Cv2.Normalize(gradientMagnitude, gradientMagnitude, 0, 255, NormTypes.MinMax);
            gradientMagnitude.ConvertTo(gradientMagnitude8U, MatType.CV_8U);

            // **Otsu + Inverted Threshold (Binary INV)**
            Mat edgeMask = new Mat();
            Cv2.Threshold(gradientMagnitude8U, edgeMask, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // **Kenarlardaki beyaz piksel sayısını hesapla**
            int edgePixelCount = Cv2.CountNonZero(edgeMask);

            // **Toplam alanı hesapla**
            double totalArea = grayImage.Rows * grayImage.Cols;

            // **Piksel yüzdesini hesapla**
            pixelPercentage = edgePixelCount / totalArea;

            return edgePixelCount; // Kenar yoğunluğunu döndürüyoruz
        }

        public static int ComputeSobelEdgeDensity(Mat grayImage, int threshold = 100)
        {
            // **Sobel X ve Sobel Y hesapla**
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_64F, 1, 0, ksize: 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_64F, 0, 1, ksize: 3);

            // **Gradyan büyüklüğünü hesapla**
            Mat gradientMagnitude = new Mat();
            Cv2.Magnitude(sobelX, sobelY, gradientMagnitude);

            // **Gradyanı normalize et ve 8-bit formata çevir**
            Mat gradientMagnitude8U = new Mat();
            Cv2.Normalize(gradientMagnitude, gradientMagnitude, 0, 255, NormTypes.MinMax);
            gradientMagnitude.ConvertTo(gradientMagnitude8U, MatType.CV_8U);

            // **Kenarlara eşik uygula**
            Mat edgeMask = new Mat();
            Cv2.Threshold(gradientMagnitude8U, edgeMask, threshold, 255, ThresholdTypes.Binary);

            // **Kenarlardaki beyaz (1) piksel sayısını hesapla**
            int edgeDensity = Cv2.CountNonZero(edgeMask);

            return edgeDensity;
        }



        public static CharacterSegmentationResult SegmentCharactersVertically(Mat possiblePlateRegion)
        {
            CharacterSegmentationResult segmentedCharacter = new CharacterSegmentationResult();

            Mat colorPlate = possiblePlateRegion.Clone();
            List<Mat> characterRegions = new List<Mat>();

            Mat cloneColorPlate = colorPlate.Clone();
            Mat cloneColorPlate1 = colorPlate.Clone();
            Mat cloneColorPlate2 = colorPlate.Clone();

            //Mat hophop = ImagePreProcessingHelper.ApplyUnsharpMask(colorPlate);

            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(colorPlate.Clone());

            Mat adaptiveThresholdPlate = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(colorPlate.Clone());



            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox6, BitmapConverter.ToBitmap(hophop));


            //

            #region mserli segment
            //var possibleCharacterRegionsInGray = MSEROperations.SegmentCharacterInPlate(grayPlate);

            List<MserResult> possibleCharacterRegionsInGray = CharacterHelper.DetectCharactersFromPlate(grayPlate);

            Mat bi = grayPlate.Clone();
            //Mat bi = new Mat();
            //Mat morp = new Mat();
            Random rng = new Random();

            #endregion



            //dbscan entegrasyon denemesi

            //var possibleCharacterRegions1 = MSEROperations.SegmentCharacterInPlateDB(grayPlate);

            List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

            ////List<Rect> osman = PlateCharacterFinder.ClusterCharacterBoundingBoxes(possibleCharacterRegions1);

            foreach (var item in possibleCharacterRegionsInBinary)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate1, item.BBox, randomColor);
            }


            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(cloneColorPlate1));

            Cv2.Threshold(grayPlate, bi, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            //morp = SegmentationHelper.ApplyMorphologicalOperations(bi);


            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox6, BitmapConverter.ToBitmap(morp));



            //SegmentationHelper.ApplyAndDrawInPictureBoxVerticalProjection(bi);

            //List<Rect> possibleCharacterRegions = SegmentationHelper.SegmentCharactersUsingAllSlopes(bi);
            //Random rng = new Random();

            foreach (MserResult bbox in possibleCharacterRegionsInGray)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox.BBox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox.BBox);

                // Bu bölgeyi Mat olarak kaydedin
                Mat characterRegion = new Mat(grayPlate, bbox.BBox);


                Mat characterRegionBinaryImage = new Mat();
                Cv2.AdaptiveThreshold(characterRegion, characterRegionBinaryImage, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 17, 5);
                //Cv2.Threshold(characterRegion, characterRegionBinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(characterRegionBinaryImage));




                ///normal segmentasyon
                characterRegions.Add(characterRegionBinaryImage);

                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate, bbox.BBox, randomColor);
            

                DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(cloneColorPlate));
                //} 

            }




            Mat binaryPlate = ImagePreProcessingHelper.ColorMatToBinary(colorPlate);



            segmentedCharacter.thresh = binaryPlate;
            segmentedCharacter.threshouldPossibleCharacters = characterRegions;
            segmentedCharacter.segmentedPlate = cloneColorPlate;

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(cloneColorPlate));

            return segmentedCharacter;
        }
    }
}
