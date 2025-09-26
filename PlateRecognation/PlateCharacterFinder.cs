using Accord.Imaging.Filters;
using Accord.Statistics.Kernels;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class PlateCharacterFinder
    {
        public static List<Rect> GetConnectedComponentBoxes(Mat thresholdedPlate)
        {
            // Çıktı için listemizi hazırlıyoruz
            List<Rect> boundingBoxes = new List<Rect>();

            Mat color = thresholdedPlate.Clone();

            Mat binary = new Mat();

            // Eğer görüntü gri değilse, griye dönüştür
            // (thresholdedPlate zaten ikili bir mat ise bu adım aslında gereksiz)
            if (thresholdedPlate.Type() != MatType.CV_8UC1)
            {
                Cv2.CvtColor(thresholdedPlate, thresholdedPlate, ColorConversionCodes.BGR2GRAY);

                //Cv2.Threshold(thresholdedPlate, binary, 127, 255, ThresholdTypes.BinaryInv);

                Cv2.AdaptiveThreshold(
    thresholdedPlate,
    binary,
    255,
    AdaptiveThresholdTypes.GaussianC,
    ThresholdTypes.BinaryInv,
    11,   // blockSize
    2     // C
);
            }

            // Connected Components için gerekli mat değişkenleri
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();

            // 8-bağlantı ile bileşen analizi (CV_32S tamsayı tipiyle etiketlenecek)
            int numberOfLabels = Cv2.ConnectedComponentsWithStats(
                binary,
                labels,
                stats,
                centroids,
                PixelConnectivity.Connectivity4,
                MatType.CV_32S
            );

            // 0 numaralı etiket arka plan olduğu için 1'den başlıyoruz
            for (int label = 1; label < numberOfLabels; label++)
            {
                // stats matrisinde (x, y, width, height, area) sırasıyla 5 kolon var
                int x = stats.At<int>(label, 0);
                int y = stats.At<int>(label, 1);
                int width = stats.At<int>(label, 2);
                int height = stats.At<int>(label, 3);
                // int area   = stats.At<int>(label, 4); // Eğer alanı da kontrol etmek istersen

                // Burada istersen min/max boyut filtrelemesi yapabilirsin
                 if (width > 5 && height > 10) //{ ... gibi }

                boundingBoxes.Add(new Rect(x, y, width, height));
            }


            foreach (Rect rect in boundingBoxes)
            {

                Cv2.Rectangle(color, rect, Scalar.Red, 2);

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(color));
            }

            return boundingBoxes;
        }














        public static CharacterSegmentationResult FindCharacterInPlateRegion(Mat possiblePlateRegion)
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
                ////burası koordinata göre karakterleri bölmek için kommentlendi çalışıyor

                //List<Rect> charac = SegmentationHelper.SegmentCharactersUsingProjections((characterRegionBinaryImage));
                //SegmentationHelper.SegmentCharactersUsingVerticalProjectionAndDraw(possiblePlateRegion.Clone(), characterRegionBinaryImage);

                //verticalprokection
                //    List<Rect> charac = SegmentationHelper.AhmetVerticalProjectionSegmentCharacters(characterRegionBinaryImage);

                //    foreach (Rect item in charac)
                //    {
                //        Mat d = new Mat(characterRegion, item);





                //        Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur

                //        int wi = item.X + bbox.BBox.X;
                //        int hi = item.Y + bbox.BBox.Y;

                //        Rect rect = new Rect(wi, hi, item.Width, item.Height);



                //        Cv2.Threshold(d, d, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);


                //        double sdf = CalculateCharacterDensity(d);


                //        //if(sdf < 0.6)
                //        {
                //            characterRegions.Add(d);

                //            Cv2.Rectangle(cloneColorPlate, rect, randomColor);

                //        }

                //    }

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
        public static List<MserResult> FilterNonCharacterRegions1(List<MserResult> regions, Mat plateImage)
        {
            List<MserResult> filteredRegions = new List<MserResult>();

            // Ortalama genişlik ve yükseklik hesapla
            double avgWidth = regions.Average(r => r.BBox.Width);
            double avgHeight = regions.Average(r => r.BBox.Height);

            foreach (var region in regions)
            {
                Rect bbox = region.BBox;
                double aspectRatio = (double)bbox.Width / bbox.Height;

                // 1️⃣ Geometrik Filtreleme: Çok geniş veya çok dar dikdörtgenleri ele
                if (aspectRatio < 0.2 || aspectRatio > 1.0) continue;

                // 2️⃣ Boyut Filtreleme: Çok küçük veya çok büyük dikdörtgenleri ele
                if (bbox.Height < avgHeight * 0.6 || bbox.Height > avgHeight * 1.4) continue;

                // 3️⃣ Yoğunluk Analizi: Çok parlak veya çok koyu dikdörtgenleri ele
                Mat roi = new Mat(plateImage, bbox);
                double meanIntensity = roi.Mean().Val0;
                if (meanIntensity > 200 || meanIntensity < 50) continue;

                // 4️⃣ Kenar (Edge) Analizi: Kenar sayısı az olanları ele
                Mat edges = new Mat();
                Cv2.Canny(roi, edges, 80, 200);  // Kenar algılama için eşikleri artırdık
                int edgeCount = Cv2.CountNonZero(edges);
                if (edgeCount < (bbox.Width * bbox.Height) * 0.10) continue; // Daha sıkı filtre

                // 5️⃣ Yoğunluk Değişimi Analizi: Gürültüye karşı ek koruma
                Mat gradX = new Mat();
                Cv2.Sobel(roi, gradX, MatType.CV_8U, 1, 0, 3);
                double gradientSum = Cv2.Sum(gradX).Val0 / (bbox.Width * bbox.Height);
                if (gradientSum < 10) continue; // Çok düşük yoğunluk farkına sahip dikdörtgenleri ele

               

                // Tüm filtrelerden geçenleri ekleyelim
                filteredRegions.Add(region);
            }

            return filteredRegions;
        }
        public static List<MserResult> FilterNonCharacterRegionsScore(List<MserResult> regions, Mat plateImage, double thresholdScore = 6.0)
        {
            List<MserResult> filteredRegions = new List<MserResult>();

            double avgWidth = regions.Average(r => r.BBox.Width);
            double avgHeight = regions.Average(r => r.BBox.Height);

            foreach (var region in regions)
            {
                Rect bbox = region.BBox;
                double aspectRatio = (double)bbox.Width / bbox.Height;

                double score = 0;

                // 1️⃣ Geometri Skoru
                if (aspectRatio >= 0.2 && aspectRatio <= 1.0) score += 2;
                if (bbox.Height >= avgHeight * 0.6 && bbox.Height <= avgHeight * 1.4) score += 2;

                // 2️⃣ Yoğunluk Skoru
                Mat roi = new Mat(plateImage, bbox);
                double meanIntensity = roi.Mean().Val0;
                if (meanIntensity >= 50 && meanIntensity <= 200) score += 2;

                // 3️⃣ Kenar (Edge) Skoru
                Mat edges = new Mat();
                Cv2.Canny(roi, edges, 80, 200);
                int edgeCount = Cv2.CountNonZero(edges);
                if (edgeCount > (bbox.Width * bbox.Height) * 0.10) score += 2;

                // 4️⃣ Gradient Skoru
                Mat gradX = new Mat();
                Cv2.Sobel(roi, gradX, MatType.CV_8U, 1, 0, 3);
                double gradientSum = Cv2.Sum(gradX).Val0 / (bbox.Width * bbox.Height);
                if (gradientSum < 10) score += 2;

                // 5️⃣ Konum Skoru
                if (bbox.X >= plateImage.Cols * 0.05 && bbox.X <= plateImage.Cols * 0.95) score += 2;

                // **🔥 Eğer skor belirlenen eşikten büyükse karakter olarak kabul et**
                if (score >= thresholdScore)
                {
                    filteredRegions.Add(region);
                }
            }

            return filteredRegions;
        }

        public static List<MserResult> FilterNonCharacterRegions(List<MserResult> regions, Mat plateImage)
        {
            List<MserResult> filteredRegions = new List<MserResult>();

            double avgWidth = regions.Average(r => r.BBox.Width);
            double avgHeight = regions.Average(r => r.BBox.Height);

            foreach (var region in regions)
            {
                Rect bbox = region.BBox;
                double aspectRatio = (double)bbox.Width / bbox.Height;

                // 1️⃣ Geometrik Filtreleme
                if (aspectRatio < 0.2 || aspectRatio > 1.2)
                    continue;

                // 2️⃣ Konum Filtreleme
                if (bbox.Height < avgHeight * 0.5 || bbox.Height > avgHeight * 1.5)
                    continue;

                // 3️⃣ Histogram Yoğunluk Analizi
                Mat roi = new Mat(plateImage, bbox);
                //Cv2.CvtColor(roi, roi, ColorConversionCodes.BGR2GRAY);
                double meanIntensity = roi.Mean().Val0;
                if (meanIntensity > 220 || meanIntensity < 30)
                    continue;

                // 4️⃣ Kenar Tespiti ile Gürültü Temizleme
                Mat edges = new Mat();
                Cv2.Canny(roi, edges, 50, 150);
                int edgeCount = Cv2.CountNonZero(edges);
                if (edgeCount < (bbox.Width * bbox.Height) * 0.05)
                    continue;

                // 5️⃣ **Projeksiyon Profili ile Gürültü Temizleme**
                if (!IsValidCharacterRegion(roi))
                    continue;

                // 🎯 **Karakter olduğuna karar verildi, listeye ekle**
                filteredRegions.Add(region);
            }

            return filteredRegions;
        }
        public static List<MserResult> FilterNonCharacterRegions5(List<MserResult> regions, Mat plateImage)
        {
            List<MserResult> filteredRegions = new List<MserResult>();

            // Ortalama genişlik ve yükseklik hesapla
            double avgWidth = regions.Average(r => r.BBox.Width);
            double avgHeight = regions.Average(r => r.BBox.Height);

            foreach (var region in regions)
            {
                Rect bbox = region.BBox;
                double aspectRatio = (double)bbox.Width / bbox.Height;

                // **1️⃣ Geometrik Filtreleme**
                if (aspectRatio < 0.2 || aspectRatio > 1.2) 
                    continue;

                // **2️⃣ Konum Filtreleme**
                if (bbox.Height < avgHeight * 0.5 || bbox.Height > avgHeight * 1.5) 
                    continue;

                // **3️⃣ Histogram Yoğunluk Analizi**
                Mat roi = new Mat(plateImage, bbox);
                double meanIntensity = roi.Mean().Val0;

                if (meanIntensity > 220 || meanIntensity < 30) 
                    continue;

                // **4️⃣ Kenar Tespiti ile Gürültü Temizleme**
                Mat edges = new Mat();
                Cv2.Canny(roi, edges, 50, 150);
                int edgeCount = Cv2.CountNonZero(edges);

                if (edgeCount < (bbox.Width * bbox.Height) * 0.05) 
                    continue;

                // **5️⃣ Yatay ve Dikey Projeksiyon ile Gürültü Temizleme**
                if (!IsValidCharacterRegion(roi))
                    continue;

                // 🎯 **Geçenleri ekleyelim**
                filteredRegions.Add(region);
            }

            return filteredRegions;
        }

        public static Mat AutoThreshold(Mat grayImage)
        {
            Mat binaryImage = new Mat();

            // **1️⃣ Histogram Hesaplama**
            Mat hist = new Mat();
            int histSize = 256;
            Rangef range = new Rangef(0, 256);
            Cv2.CalcHist(new Mat[] { grayImage }, new int[] { 0 }, null, hist, 1, new int[] { histSize }, new Rangef[] { range });

            // **2️⃣ Histogram Varyansını Hesapla**
            Mat mean = new Mat(), stddev = new Mat();
            Cv2.MeanStdDev(hist, mean, stddev);
            double variance = stddev.At<double>(0); // Standart sapmayı al

            // **3️⃣ Eşikleme Yöntemini Belirle**
            if (variance < 30)  // Eğer varyans düşükse, ışık homojen → Otsu uygundur
            {
                Cv2.Threshold(grayImage, binaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                Console.WriteLine("Otsu kullanıldı.");
            }
            else  // Eğer varyans yüksekse, ışık değişken → Adaptive Threshold daha iyi çalışır
            {
                Cv2.AdaptiveThreshold(grayImage, binaryImage, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 15, 5);
                Console.WriteLine("Adaptive Threshold kullanıldı.");
            }

            return binaryImage;
        }
        public static CharacterSegmentationResult SegmentCharactersWithMSER(Mat possiblePlateRegion)
        {
            CharacterSegmentationResult segmentedCharacter = new CharacterSegmentationResult();

            Mat colorPlate = possiblePlateRegion.Clone();
            List<Mat> characterRegions = new List<Mat>();

            Mat cloneColorPlate = colorPlate.Clone();
            Mat cloneColorPlate1 = colorPlate.Clone();
            Mat cloneColorPlate2 = colorPlate.Clone();
            Mat cloneColorPlate3 = colorPlate.Clone();

            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(colorPlate.Clone());


            Mat grayPlate1 = grayPlate.Clone();


            Mat adaptiveThresholdPlate = new Mat();

            //Cv2.Threshold(grayPlate, adaptiveThresholdPlate, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
            Cv2.AdaptiveThreshold(grayPlate, adaptiveThresholdPlate, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 17, 5);
            //Cv2.AdaptiveThreshold(grayPlate, adaptiveThresholdPlate, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 15, 5);
            //Mat adaptiveThresholdPlate = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(colorPlate.Clone());

            //Mat adaptiveThresholdPlate = AutoThreshold(grayPlate.Clone());

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(adaptiveThresholdPlate));




            List<MserResult> possibleCharacterRegionsInGray = CharacterHelper.DetectCharactersFromPlate(grayPlate);
            List<MserResult> possibleCharacterRegionsInBinary = CharacterHelper.DetectCharactersFromPlate(adaptiveThresholdPlate);

            List<MserResult> mergedResults = SmartMergeMserResults(possibleCharacterRegionsInGray, possibleCharacterRegionsInBinary);

            Random rng = new Random();


            #region rect çizmek için
            foreach (var item in possibleCharacterRegionsInGray)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate1, item.BBox, randomColor);
            }

            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(cloneColorPlate1));

            foreach (var item in mergedResults)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate2, item.BBox, randomColor);
            }

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(cloneColorPlate2));
            #endregion

            foreach (MserResult bbox in mergedResults)
            {
                // Bu bölgeyi Mat olarak kaydedin
                Mat characterRegion = new Mat(grayPlate, bbox.BBox);


                Mat characterRegionBinaryImage = new Mat();
                Cv2.Threshold(characterRegion, characterRegionBinaryImage, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                ///normal segmentasyon
                characterRegions.Add(characterRegionBinaryImage);

                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(cloneColorPlate, bbox.BBox, randomColor);
             
            }

            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(cloneColorPlate));



            Mat binaryPlate = ImagePreProcessingHelper.ColorMatToBinary(colorPlate);

            segmentedCharacter.thresh = binaryPlate;
            segmentedCharacter.threshouldPossibleCharacters = characterRegions;
            segmentedCharacter.segmentedPlate = cloneColorPlate;

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(cloneColorPlate));

            return segmentedCharacter;
        }


        public static ThreadSafeList<CharacterSegmentationResult> SegmentCharactersInPossiblePlateRegion(List<PossiblePlate> possiblePlateRegions)
        {
            ThreadSafeList<CharacterSegmentationResult> segmentedCharacters = new ThreadSafeList<CharacterSegmentationResult>();

            foreach (PossiblePlate possibleRegion in possiblePlateRegions)
            {
              

                Mat colorPlate = possibleRegion.possiblePlateRegions;

                Cv2.Resize(colorPlate, colorPlate, new OpenCvSharp.Size(114, 32), 0, 0, InterpolationFlags.Lanczos4);

                CharacterSegmentationResult possibleCharacterRegions = PlateCharacterFinder.SegmentCharactersWithMSER(colorPlate);

                //PlateCharacterFinder.GetConnectedComponentBoxes(colorPlate);

              



                //CharacterSegmentationResult possibleCharacterRegions = PlateCharacterFinder.FindCharacterInPlateRegion(colorPlate);

                List<Mat> characters = possibleCharacterRegions.threshouldPossibleCharacters;

                //karakter bölgeleri 5 ten fazla 9 dan küçük olması lazım ön filtre
                if ((characters.Count >= 5) && (characters.Count < 10))
                {
                    possibleCharacterRegions.colorPlate = colorPlate;

                    segmentedCharacters.Add(possibleCharacterRegions);
                }
            }

            return segmentedCharacters;
        }


        public static List<Rect> FilterCharacterSegmentsWithDBSCAN(IEnumerable<MserResult> mserResults)
        {
            List<Rect> characterBoxes = mserResults.Select(m => m.BBox).ToList();

            // Bounding Box merkezlerini al
            double[][] characterCenters = characterBoxes
                .Select(box => new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 })
                .ToArray();

            // DBSCAN modelini oluştur ve çalıştır
            var dbscan = new DBSCAN(epsilon: 15, minPoints: 2);
            int[] clusterLabels = dbscan.Fit(characterCenters);

            for (int i = 0; i < clusterLabels.Length; i++)
            {
                //Debug.WriteLine($"Bounding Box {i}: Küme {clusterLabels[i]}");
            }

            // En büyük kümeyi bul
            var clusters = clusterLabels
                .Select((label, index) => new { Label = label, Box = characterBoxes[index] })
                .Where(x => x.Label != -1) // Gürültüleri çıkar
                .GroupBy(x => x.Label)
                .OrderByDescending(g => g.Count()) // En büyük kümeyi al
                .FirstOrDefault();

            if (clusters == null)
                return new List<Rect>(); // Hiç küme bulunmazsa boş liste döndür.

            return clusters.Select(x => x.Box).ToList(); // En büyük kümeye ait karakterleri döndür.
        }

        public static List<Rect> FilterCharacterSegmentsWithDBSCAN(OpenCvSharp.Rect[] mserResults)
        {
            List<Rect> characterBoxes = new List<Rect>();
            characterBoxes.AddRange(mserResults);
            
            //mserResults;// .Select(m => m.BBox).ToList();

            // Bounding Box merkezlerini al
            double[][] characterCenters = characterBoxes
                .Select(box => new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 })
                .ToArray();

            // DBSCAN modelini oluştur ve çalıştır
            var dbscan = new DBSCAN(epsilon: 10, minPoints: 1);
            int[] clusterLabels = dbscan.Fit(characterCenters);

            for (int i = 0; i < clusterLabels.Length; i++)
            {
                //Debug.WriteLine($"Bounding Box {i}: Küme {clusterLabels[i]}");
            }

            // En büyük kümeyi bul
            var clusters = clusterLabels
                .Select((label, index) => new { Label = label, Box = characterBoxes[index] })
                .Where(x => x.Label != -1) // Gürültüleri çıkar
                .GroupBy(x => x.Label)
                .OrderByDescending(g => g.Count()) // En büyük kümeyi al
                .FirstOrDefault();

            if (clusters == null)
                return new List<Rect>(); // Hiç küme bulunmazsa boş liste döndür.

            return clusters.Select(x => x.Box).ToList(); // En büyük kümeye ait karakterleri döndür.
        }


        public static List<Rect> ClusterCharacterBoundingBoxes1(OpenCvSharp.Rect[] mserResults)
        {
            List<Rect> characterBoxes = new List<Rect>();
            characterBoxes.AddRange(mserResults);

            if (characterBoxes.Count == 0)
                return new List<Rect>(); // Eğer bounding box yoksa, boş liste döndür.

            // 1️⃣ Bounding Box merkezlerini al
            double[][] characterCenters = characterBoxes
                .Select(box => new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 })
                .ToArray();

            // 2️⃣ DBSCAN modelini oluştur ve çalıştır
            var dbscan = new DBSCAN(epsilon: 20, minPoints: 3);  // 10 piksel mesafe içinde en az 2 nokta
            int[] clusterLabels = dbscan.Fit(characterCenters);

            // 3️⃣ DBSCAN çıktılarını gruplandır
            var clusters = clusterLabels
                .Select((label, index) => new { Label = label, Box = characterBoxes[index] })
                .Where(x => x.Label != -1) // Gürültüleri çıkar
                .GroupBy(x => x.Label)
                .ToList();

            List<Rect> clusteredBoundingBoxes = new List<Rect>();

            // 4️⃣ Her küme için en küçük kapsayan dikdörtgeni oluştur
            foreach (var cluster in clusters)
            {
                int minX = cluster.Min(r => r.Box.X);
                int minY = cluster.Min(r => r.Box.Y);
                int maxX = cluster.Max(r => r.Box.X + r.Box.Width);
                int maxY = cluster.Max(r => r.Box.Y + r.Box.Height);
                Rect mergedBox = new Rect(minX, minY, maxX - minX, maxY - minY);

                // 5️⃣ Yanlış pozitifleri filtrele
                double aspectRatio = (double)mergedBox.Width / mergedBox.Height;
                //if (mergedBox.Width > 5 && mergedBox.Height > 10 && aspectRatio < 2.0)
                {
                    clusteredBoundingBoxes.Add(mergedBox);
                }
            }

            return clusteredBoundingBoxes;
        }

        public static Rect FindPlateRegionFromCharacters(OpenCvSharp.Rect[] mserResults)
        {
            List<Rect> characterBoxes = new List<Rect>();
            characterBoxes.AddRange(mserResults);

            //if (characterBoxes.Count == 0)
            //    return null; // Eğer hiç karakter kutusu yoksa plaka da yoktur.

            // 1️⃣ Bounding Box merkezlerini al
            double[][] characterCenters = characterBoxes
                .Select(box => new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 })
                .ToArray();

            // 2️⃣ DBSCAN modelini oluştur ve çalıştır
            var dbscan = new DBSCAN(epsilon: 20, minPoints: 3);
            int[] clusterLabels = dbscan.Fit(characterCenters);

            // 3️⃣ En büyük kümeyi bul
            var clusters = clusterLabels
                .Select((label, index) => new { Label = label, Box = characterBoxes[index] })
                .Where(x => x.Label != -1) // Gürültüleri çıkar
                .GroupBy(x => x.Label)
                .OrderByDescending(g => g.Count()) // En büyük kümeyi al
                .FirstOrDefault();

            //if (clusters == null)
            //    return null; // Eğer hiçbir küme bulunamazsa plakayı tespit edemedi.

            // 4️⃣ Seçilen kümeye ait karakterlerin kapsadığı en küçük dikdörtgeni al
            int minX = clusters.Min(r => r.Box.X);
            int minY = clusters.Min(r => r.Box.Y);
            int maxX = clusters.Max(r => r.Box.X + r.Box.Width);
            int maxY = clusters.Max(r => r.Box.Y + r.Box.Height);
            Rect plateCandidate = new Rect(minX, minY, maxX - minX, maxY - minY);

            // 5️⃣ Plaka oran filtresi (Türkiye plakaları genellikle 3:1 - 6:1 arasında olur)
            double plateAspectRatio = (double)plateCandidate.Width / plateCandidate.Height;
            //if (plateAspectRatio < 3 || plateAspectRatio > 6)
            //    return null; // Eğer oran yanlışsa, büyük ihtimalle plaka değil.

            return plateCandidate;
        }


        public static List<Rect> ClusterAndMergeBoundingBoxes(List<Rect> boundingBoxes, double epsilon, int minPoints)
        {
            if (boundingBoxes.Count == 0)
                return new List<Rect>();

            // 1️⃣ Bounding Box merkezlerini al
            double[][] characterCenters = boundingBoxes
                .Select(box => new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 })
                .ToArray();

            // 2️⃣ DBSCAN modelini oluştur ve çalıştır
            var dbscan = new DBSCAN(epsilon, minPoints);
            int[] clusterLabels = dbscan.Fit(characterCenters);

            // 3️⃣ Kümeleme sonuçlarını al
            var clusters = clusterLabels
                .Select((label, index) => new { Label = label, Box = boundingBoxes[index] })
                .Where(x => x.Label != -1) // Gürültüleri çıkar
                .GroupBy(x => x.Label)
                .ToList();

            List<Rect> mergedBoundingBoxes = new List<Rect>();

            // 4️⃣ Her küme için en küçük kapsayan dikdörtgeni oluştur
            foreach (var cluster in clusters)
            {
                int minX = cluster.Min(r => r.Box.X);
                int minY = cluster.Min(r => r.Box.Y);
                int maxX = cluster.Max(r => r.Box.X + r.Box.Width);
                int maxY = cluster.Max(r => r.Box.Y + r.Box.Height);
                Rect mergedBox = new Rect(minX, minY, maxX - minX, maxY - minY);

                // 5️⃣ Yanlış pozitifleri filtrele
                double aspectRatio = (double)mergedBox.Width / mergedBox.Height;
                if (mergedBox.Width > 5 && mergedBox.Height > 10 && aspectRatio < 2.0)
                {
                    mergedBoundingBoxes.Add(mergedBox);
                }
            }

            return mergedBoundingBoxes;
        }

        public static List<Rect> ClusterCharacterBoundingBoxes(IEnumerable<MserResult> mserResults)
        {
            List<Rect> characterBoxes = mserResults.Select(m => m.BBox).ToList();

            if (characterBoxes.Count == 0)
                return new List<Rect>(); // Eğer bounding box yoksa, boş liste döndür.

            // 1️⃣ Bounding Box merkezlerini al
            double[][] characterCenters = characterBoxes
                .Select(box => new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 })
                .ToArray();

            // 2️⃣ DBSCAN modelini oluştur ve çalıştır
            var dbscan = new DBSCAN(epsilon: 2, minPoints: 2);
            int[] clusterLabels = dbscan.Fit(characterCenters);

            // 3️⃣ DBSCAN çıktılarını gruplandır
            var clusters = clusterLabels
                .Select((label, index) => new { Label = label, Box = characterBoxes[index] })
                .Where(x => x.Label != -1) // Gürültüleri çıkar
                .GroupBy(x => x.Label)
                .ToList();

            List<Rect> clusteredBoundingBoxes = new List<Rect>();

            // 4️⃣ Her küme için en küçük kapsayan dikdörtgeni oluştur
            foreach (var cluster in clusters)
            {
                int minX = cluster.Min(r => r.Box.X);
                int minY = cluster.Min(r => r.Box.Y);
                int maxX = cluster.Max(r => r.Box.X + r.Box.Width);
                int maxY = cluster.Max(r => r.Box.Y + r.Box.Height);
                Rect mergedBox = new Rect(minX, minY, maxX - minX, maxY - minY);

                // 5️⃣ Yanlış pozitifleri filtrele
                double aspectRatio = (double)mergedBox.Width / mergedBox.Height;
                //if (mergedBox.Width > 3 && mergedBox.Height > 10)// && aspectRatio < 2.0)
                {
                    clusteredBoundingBoxes.Add(mergedBox);
                }
            }

            return clusteredBoundingBoxes;
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

        public static Mat ProjectedHistogram(Mat img, bool horizontal)
        {
            int sz = horizontal ? img.Rows : img.Cols;
            Mat mhist = new Mat(1, sz, MatType.CV_32F, Scalar.All(0)); // Mat.Zeros yerine bu satır kullanıldı

            for (int j = 0; j < sz; j++)
            {
                Mat data = horizontal ? img.Row(j) : img.Col(j);
                mhist.Set<float>(0, j, Cv2.CountNonZero(data)); // Değer atama düzetildi
            }

            // Normalize et
            double min, max;
            Cv2.MinMaxLoc(mhist, out min, out max);
            if (max > 0)
                mhist.ConvertTo(mhist, -1, 1.0f / max, 0);

            return mhist;
        }

        public static bool IsValidCharacterRegion1(Mat roi)
        {
            // 1️⃣ Threshold uygula (Otsu ile binarize et)
            Cv2.Threshold(roi, roi, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // 2️⃣ Normalize edilmiş histogramları al
            Mat horizontalHist = ProjectedHistogram(roi, true);  // Yatay histogram
            Mat verticalHist = ProjectedHistogram(roi, false);   // Dikey histogram

            // 3️⃣ Histogramı float dizisine çevir
            float[] horizontalData = new float[horizontalHist.Cols];
            for (int i = 0; i < horizontalHist.Cols; i++)
            {
                horizontalData[i] = horizontalHist.At<float>(0, i);
            }

            float[] verticalData = new float[verticalHist.Cols];
            for (int i = 0; i < verticalHist.Cols; i++)
            {
                verticalData[i] = verticalHist.At<float>(0, i);
            }

            // 4️⃣ Ortalama Hesapla
            double avgH = horizontalData.Average();
            double avgV = verticalData.Average();

            // 5️⃣ Standart Sapma Hesapla
            double stdDevH = Math.Sqrt(horizontalData.Select(v => Math.Pow(v - avgH, 2)).Average());
            double stdDevV = Math.Sqrt(verticalData.Select(v => Math.Pow(v - avgV, 2)).Average());

            // 6️⃣ Dinamik eşik belirleme
            double thresholdH = Math.Max(avgH - (stdDevH * 0.3), 0.1);
            double thresholdV = Math.Max(avgV - (stdDevV * 0.3), 0.1);

            // 7️⃣ Karakter olup olmadığını kontrol et
            bool hasValidHorizontalVariation = horizontalData.Count(v => v > thresholdH) > 3;
            bool hasValidVerticalVariation = verticalData.Count(v => v > thresholdV) > 3;

            return hasValidHorizontalVariation && hasValidVerticalVariation;
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

        public static bool IsValidCharacterRegion(Mat roi)
        {
            //Cv2.CvtColor(roi, roi, ColorConversionCodes.BGR2GRAY); // Gri tonlamaya çevir
            Cv2.Threshold(roi, roi, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu); // Binary Threshold


            int width = roi.Cols;
            int height = roi.Rows;

            // **Yatay ve dikey projeksiyon profillerini oluştur**
            int[] horizontalProfile = new int[height];
            int[] verticalProfile = new int[width];

            for (int y = 0; y < height; y++)
                horizontalProfile[y] = Cv2.CountNonZero(roi.Row(y));

            for (int x = 0; x < width; x++)
                verticalProfile[x] = Cv2.CountNonZero(roi.Col(x));

            // **Ortalama ve Standart Sapma Hesapla**
            double avgH = horizontalProfile.Average();
            double stdDevH = Math.Sqrt(horizontalProfile.Average(v => Math.Pow(v - avgH, 2)));

            double avgV = verticalProfile.Average();
            double stdDevV = Math.Sqrt(verticalProfile.Average(v => Math.Pow(v - avgV, 2)));

            // **Dinamik eşik belirleme**
            double thresholdH = Math.Max(avgH - (stdDevH * 0.3), 3);  // Daha hassas ve minimum 5 olacak
            double thresholdV = Math.Max(avgV - (stdDevV * 0.3), 3);  // Daha hassas ve minimum 5 olacak

            // **Eşik değerine göre karakter olup olmadığını kontrol et**
            bool hasValidHorizontalVariation = horizontalProfile.Count(v => v > thresholdH) > 3;
            bool hasValidVerticalVariation = verticalProfile.Count(v => v > thresholdV) > 3;


            return hasValidHorizontalVariation && hasValidVerticalVariation;
        }

        public static bool IsValidCharacter(Mat roi)
        {
            if (roi.Empty())
                return false;

            // ROI'yi binarize et
            Mat binary = new Mat();
            Cv2.Threshold(roi, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // **📊 Yatay ve Dikey Profilleri Hesapla**
            int[] horizontalProfile = new int[binary.Rows];
            int[] verticalProfile = new int[binary.Cols];

            // Yatay profili hesapla
            for (int y = 0; y < binary.Rows; y++)
            {
                horizontalProfile[y] = Cv2.CountNonZero(binary.Row(y));
            }

            // Dikey profili hesapla
            for (int x = 0; x < binary.Cols; x++)
            {
                verticalProfile[x] = Cv2.CountNonZero(binary.Col(x));
            }

            // **📊 Gürültü veya karakter olup olmadığını anlamak için analiz yap**
            double avgH = horizontalProfile.Average();
            double avgV = verticalProfile.Average();

            // **Karakter içeren bir ROI, yatay ve dikey projeksiyonlarda belirgin iniş çıkışlar göstermeli**
            bool hasValidHorizontalVariation = horizontalProfile.Count(v => v > avgH * 0.5) > 2;
            bool hasValidVerticalVariation = verticalProfile.Count(v => v > avgV * 0.5) > 2;

            return hasValidHorizontalVariation && hasValidVerticalVariation;
        }


        private static List<MserResult> SmartMergeMserResults(List<MserResult> grayResults, List<MserResult> binaryResults)
        {
            List<MserResult> mergedResults = new List<MserResult>();

            // **Hem Gray hem de Binary’de tespit edilenleri birleştir**
            foreach (var gray in grayResults)
            {
                var matched = binaryResults.FirstOrDefault(b => RectSimilarity(gray.BBox, b.BBox));

                if (matched != null)
                {
                    // **Ortalama bir Bounding Box al**
                    Rect mergedBox = MergeBoundingBoxes(gray.BBox, matched.BBox);
                    mergedResults.Add(new MserResult { BBox = mergedBox, Points = gray.Points });
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
                if (!grayResults.Any(g => RectSimilarity(binary.BBox, g.BBox)))
                {
                    if (IsValidCharacter(binary, mergedResults))
                    {
                        mergedResults.Add(binary);
                    }
                }
            }

            return mergedResults;
        }

        private static Rect MergeBoundingBoxes(Rect a, Rect b)
        {
            int x = Math.Min(a.X, b.X);
            int y = Math.Min(a.Y, b.Y);
            int width = Math.Max(a.X + a.Width, b.X + b.Width) - x;
            int height = Math.Max(a.Y + a.Height, b.Y + b.Height) - y;

            return new Rect(x, y, width, height);
        }
        private static double GetArea(Rect rect)
        {
            return rect.Width * rect.Height;
        }

        private static bool RectSimilarity(Rect a, Rect b, double threshold = 0.5)
        {
            double intersectionArea = GetArea(a & b); // Kesim alanı
            double minArea = Math.Min(GetArea(a), GetArea(b)); // En küçük alanı al

            return (intersectionArea / minArea) > threshold;
        }
    }
}
