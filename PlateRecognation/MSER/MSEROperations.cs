using Accord.Imaging.Filters;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace PlateRecognation
{
    internal class MSEROperations
    {
        public static Mat ApplyCLAHE(Mat grayImage)
        {
            Mat claheImage = new Mat();
            var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8));
            clahe.Apply(grayImage, claheImage);
            return claheImage;
        }

        public static Mat ApplyUnsharpMask(Mat image)
        {
            Mat blurred = new Mat();
            Cv2.GaussianBlur(image, blurred, new OpenCvSharp.Size(0, 0), 3);
            Mat sharpImage = new Mat();
            Cv2.AddWeighted(image, 1.5, blurred, -0.5, 0, sharpImage);
            return sharpImage;
        }
        public static Mat ApplyEdgeDetection(Mat grayImage)
        {
            Mat edges = new Mat();
            Cv2.Canny(grayImage, edges, 50, 150);  // Düşük ve yüksek eşik değerleri
            return edges;
        }

        public static Rect[] FindPlateRegion(Mat grayImage)
        {
            // MSER nesnesini oluşturma
            var mser = MSER.Create();

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }


        public static Rect[] FindPlateRegionv2(Mat grayImage)
        {
            // MSER nesnesini oluşturma
            var mser = CreateAdaptiveMSERForScene(grayImage);

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }

        public static Rect[] FindPlateRegionROI(Mat grayImage)
        {
            //Thread.Sleep(100);

            // MSER nesnesini oluşturma
            var mser = CreateAdaptiveMSERForSceneROI(grayImage);

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }


        public static Rect[] FindPlateRegionROITipsizTest(Mat grayImage)
        {
            //Thread.Sleep(100);

            // MSER nesnesini oluşturma
            var mser = CreateAdaptiveMSERForSceneROI(grayImage);

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }


        public static Rect[] FindPlateRegionROI(Mat grayImage, string inputType)
        {
            //Thread.Sleep(100);

            // MSER nesnesini oluşturma
            var mser = MserDetectionSettings.TuneParamsForScene(grayImage, inputType);

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }


        public static Rect[] FindPlateRegionvSobel(Mat grayImage)
        {
            // MSER nesnesini oluşturma
            var mser = CreateMSERForSobelImage(grayImage);

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }

        public static Rect[] FindPlateRegionv3(Mat grayImage)
        {
            // MSER nesnesini oluşturma
            var mser = CreateAdaptiveMSERForSceneV1(grayImage);

            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mser.DetectRegions(grayImage, out msers, out bboxes);

            return bboxes;
        }

        //public static MSER CreateAdaptiveMSERForScene(Mat grayImage)
        //{
        //    //double mean = Cv2.Mean(grayImage).Val0;
        //    ////double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);
        //    //double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);


        //    //// Başlangıç değerleri
        //    //int delta = 5;
        //    //int minArea = 60;
        //    //int maxArea = 2000;
        //    //double maxVariation = 0.45;
        //    //double minDiversity = 0.5;

        //    //if (mean < 60)
        //    //{
        //    //    minArea = 30;
        //    //    maxVariation = 0.6;
        //    //    minDiversity = 0.4;
        //    //}
        //    //else if (mean > 200)
        //    //{
        //    //    maxVariation = 0.35;
        //    //    minDiversity = 0.6;
        //    //}

        //    //if (stdDev < 30)
        //    //{
        //    //    delta = 3;
        //    //    minArea -= 20;
        //    //}
        //    //else if (stdDev > 70)
        //    //{
        //    //    minArea += 20;
        //    //    maxVariation = 0.4;
        //    //}

        //    //return MSER.Create(
        //    //    delta: delta,
        //    //    minArea: minArea,
        //    //    maxArea: maxArea,
        //    //    maxVariation: maxVariation,
        //    //    minDiversity: minDiversity,
        //    //    maxEvolution: 200,
        //    //    areaThreshold: 1.01,
        //    //    minMargin: 0.5,
        //    //    edgeBlurSize: 5
        //    //);

        //    double mean = Cv2.Mean(grayImage).Val0;
        //    double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);

        //    // MSER parametreleri
        //    //int delta = 5;
        //    //int minArea = 80;  // Plaka büyük bölgedir, karakter gibi küçük olmamalı
        //    //int maxArea = 5000; // Daha büyük alanlara izin veriyoruz
        //    //double maxVariation = 0.4; // Çok şekil değişkenliği olmasın (plakalar genelde düzgün)
        //    //double minDiversity = 0.4;
        //    //int maxEvolution = 200;
        //    //double areaThreshold = 1.1;
        //    //double minMargin = 0.3;  // Çok düşük tut, çünkü plaka sınırı belirgin olmayabilir
        //    //int edgeBlurSize = 7;


        //    int delta = 4;
        //    int minArea = 60;  // Plaka büyük bölgedir, karakter gibi küçük olmamalı
        //    int maxArea = 5000; // Daha büyük alanlara izin veriyoruz
        //    double maxVariation = 0.5; // Çok şekil değişkenliği olmasın (plakalar genelde düzgün)
        //    double minDiversity = 0.2;
        //    int maxEvolution = 200;
        //    double areaThreshold = 0.8;
        //    double minMargin = 0.3;  // Çok düşük tut, çünkü plaka sınırı belirgin olmayabilir
        //    int edgeBlurSize = 3;

        //    //int delta = 5;
        //    //int minArea = 60;  // Plaka büyük bölgedir, karakter gibi küçük olmamalı
        //    //int maxArea = 5000; // Daha büyük alanlara izin veriyoruz
        //    //double maxVariation = 0.3; // Çok şekil değişkenliği olmasın (plakalar genelde düzgün)
        //    //double minDiversity = 0.2;
        //    //int maxEvolution = 200;
        //    //double areaThreshold = 1.001;
        //    //double minMargin = 0.2;  // Çok düşük tut, çünkü plaka sınırı belirgin olmayabilir
        //    //int edgeBlurSize = 3;


        //    // Dinamik ayarlamalar
        //    //if (mean > 200) // Çok parlaksa, şekil değişkenliğini azalt
        //    //{
        //    //    maxVariation = 0.2;
        //    //}
        //    //else if (mean < 80) // Çok karanlıksa, daha toleranslı ol
        //    //{
        //    //maxVariation = 0.5;
        //    //    minDiversity = 0.1;
        //    //    minArea = 100;
        //    //}

        //    //if (contrast < 40)
        //    //{
        //    //    maxVariation += 0.1;
        //    //    minArea = 100;
        //    //}

        //    return MSER.Create(
        //        delta,
        //        minArea,
        //        maxArea,
        //        maxVariation,
        //        minDiversity,
        //        maxEvolution,
        //        areaThreshold,
        //        minMargin,
        //        edgeBlurSize
        //    );
        //}


        public static MSER CreateAdaptiveMSERForSceneVOld(Mat grayImage)
        {
            double mean = Cv2.Mean(grayImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            // İlk değerler
            int delta = 5;
            int minArea = 60;
            int maxArea = 5000;
            double maxVariation = 0.5;
            double minDiversity = 0.3;
            int maxEvolution = 200;
            double areaThreshold = 1.0;
            double minMargin = 0.5;
            int edgeBlurSize = 5;

            // 🌑 Aşırı parlak sahneler (plaka kontrastsız olabilir)
            if (mean > 200)
            {
                maxVariation = 0.35;
                minDiversity = 0.25;
                delta = 6;
            }
            // 🌄 Düşük parlaklık, MSER kaçırabilir
            else if (mean < 80)
            {
                delta = 4;
                minArea = 40;
                maxVariation = 0.55;
            }

            // ❄️ Düşük kontrast (detay kaybı)
            if (contrast < 40 || stdDev < 20)
            {
                minArea = 40;
                maxVariation += 0.1;
                edgeBlurSize = 7; // Köşe gürültüsü azaltmak için
            }

            // 🔥 Yüksek kontrast (aşırı detay)
            if (contrast > 150)
            {
                //maxVariation = 0.3;
                //delta = 6;
                //edgeBlurSize = 3;

                maxVariation = 0.5;     // 0.3 → 0.4 yapıldı, çünkü plaka karakterleri çok keskin olmayabilir
                delta = 5;              // 6 → 5 ile hassasiyet biraz artırıldı
                edgeBlurSize = 2;       // 3 → 2 ile kenar detayları biraz daha vurgulanabilir hale gelir
            }

            return MSER.Create(
                delta,
                minArea,
                maxArea,
                maxVariation,
                minDiversity,
                maxEvolution,
                areaThreshold,
                minMargin,
                edgeBlurSize
            );
        }

        public static MSER CreateAdaptiveMSERForSceneVOld1(Mat grayImage)
        {
            double mean = Cv2.Mean(grayImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int delta = 4;
            int minArea = 60;
            int maxArea = 5000;
            double maxVariation = 0.5;
            double minDiversity = 0.2;
            int maxEvolution = 200;
            double areaThreshold = 0.8;
            double minMargin = 0.5;
            int edgeBlurSize = 3;

            // 🌫️ Düşük kontrast sahneler için esneklik artır
            if (contrast < 45)
            {
                maxVariation = 0.55;
                minArea = 40;
                minDiversity = 0.15;
                delta = 4;
            }
            // ⚡ Yüksek kontrastlı sahnelerde şekil kısıtlaması
            else if (contrast > 150)
            {
                maxVariation = 0.4;
                //delta = 5;
                edgeBlurSize = 3;

                //maxVariation = 0.48;
                //delta = 5;
                //edgeBlurSize = 3;
            }

            // 🌑 Aşırı parlak sahneler
            if (mean > 200)
            {
                maxVariation = 0.35;
                minDiversity = 0.25;
                delta = 6;
            }
            // 🌄 Düşük parlaklık
            else if (mean < 80)
            {
                delta = 3;
                minArea = 40;
                maxVariation = 0.55;
            }

            return MSER.Create(
                delta,
                minArea,
                maxArea,
                maxVariation,
                minDiversity,
                maxEvolution,
                areaThreshold,
                minMargin,
                edgeBlurSize
            );
        }
        public static MSER CreateMSERForSobelImage(Mat sobelImage)
        {
            //// Sobel görüntüsünde istatistikler yanıltıcı olabilir, sabit parametre daha güvenli
            //int delta = 6;
            //int minArea = 70;
            //int maxArea = 5000;
            //double maxVariation = 0.4;
            //double minDiversity = 0.3;
            //double areaThreshold = 1.01;
            //double minMargin = 0.5;
            //int edgeBlurSize = 5;

            //return MSER.Create(
            //    delta,
            //    minArea,
            //    maxArea,
            //    maxVariation,
            //    minDiversity,
            //    200,
            //    areaThreshold,
            //    minMargin,
            //    edgeBlurSize
            //);

            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(sobelImage);

            int delta = 6;
            int minArea = 70;
            double maxVariation = 0.4;
            double minDiversity = 0.3;

            if (stdDev > 80)
            {
                delta = 7;
                maxVariation = 0.35;
            }
            else if (stdDev < 30)
            {
                delta = 5;
                maxVariation = 0.5;
            }

            return MSER.Create(
                delta,
                minArea,
                5000,
                maxVariation,
                minDiversity,
                200,
                1.01,
                0.5,
                5
            );
        }

        public static MSER CreateAdaptiveMSERForScene(Mat grayImage)
        {
            double mean = Cv2.Mean(grayImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int delta = 5;
            int minArea = 60;
            int maxArea = 5000;
            double maxVariation = 0.5;
            double minDiversity = 0.3;
            double areaThreshold = 1.01;
            double minMargin = 0.5;
            int edgeBlurSize = 5;

            // 🌕 Aşırı parlak sahne → sıkı varyasyon, daha az farklılık
            if (mean > 200)
            {
                maxVariation = 0.35;
                minDiversity = 0.25;
                delta = 6;
            }
            // 🌑 Karanlık sahne → daha esnek
            else if (mean < 80)
            {
                delta = 4;
                minArea = 40;
                maxVariation = 0.55;
            }

            // 🌫 Düşük kontrast sahneler → detaylara izin ver
            if (contrast < 40)
            {
                minArea = 40;
                maxVariation = 0.55;
                delta = 4;
            }

            // 📉 Çok düşük varyasyon → MSER çalışamayabilir
            if (stdDev < 20)
            {
                delta = Math.Max(3, delta - 1);  // daha hassas
                minArea = Math.Max(30, minArea - 10);
                maxVariation = Math.Min(0.6, maxVariation + 0.1);
            }
            else if (stdDev > 70)
            {
                maxVariation = Math.Min(0.45, maxVariation);
                minDiversity += 0.05;
            }

            return MSER.Create(
                delta,
                minArea,
                maxArea,
                maxVariation,
                minDiversity,
                200,            // maxEvolution
                areaThreshold,
                minMargin,
                edgeBlurSize
            );
        }

        public static MSER CreateAdaptiveMSERForSceneROI(Mat grayImage)
        {
            // Tüm parametreler MserDetectionSettings sınıfından alınır
            var settings = MserDetectionSettings.GetScaledSettings(grayImage.Width, grayImage.Height);


            double mean = Cv2.Mean(grayImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int delta = settings.Delta;
            int minArea = settings.MinArea;
            int maxArea = settings.MaxArea;
            double maxVariation = settings.MaxVariation;
            double minDiversity = settings.MinDiversity;
            double areaThreshold = settings.AreaThreshold;
            double minMargin =  settings.MinMargin;
            int edgeBlurSize = settings.EdgeBlurSize;

            // 🌕 Aşırı parlak sahne → sıkı varyasyon, daha az farklılık
            if (mean > 200)
            {
                maxVariation = 0.35;
                minDiversity = 0.25;
                delta = Math.Clamp((int)(6 * Math.Sqrt(grayImage.Width * grayImage.Height / (640.0 * 480.0))), 3, 8);
            }
            // 🌑 Karanlık sahne → daha esnek
            else if (mean < 80)
            {
                minArea = Math.Max((int)(40 * Math.Sqrt(grayImage.Width * grayImage.Height / (640.0 * 480.0))), 20);
                maxVariation = 0.55;
                delta = Math.Max(4, delta - 1);
            }

            // 🌫 Düşük kontrast sahneler → detaylara izin ver
            if (contrast < 40)
            {
                minArea = Math.Max((int)(40 * Math.Sqrt(grayImage.Width * grayImage.Height / (640.0 * 480.0))), 20);
                maxVariation = 0.55;
                delta = Math.Max(3, delta - 1);
            }

            // 📉 Çok düşük varyasyon → MSER çalışamayabilir
            if (stdDev < 20)
            {
                minArea = Math.Max((int)(30 * Math.Sqrt(grayImage.Width * grayImage.Height / (640.0 * 480.0))), 20);
                maxVariation = Math.Min(0.6, maxVariation + 0.1);
                delta = Math.Max(3, delta - 1);
            }
            else if (stdDev > 70)
            {
                maxVariation = Math.Min(0.45, maxVariation);
                minDiversity += 0.05;
            }

            

            return MSER.Create(
                delta,
                minArea,
                maxArea,
                maxVariation,
                minDiversity,
                200,            // maxEvolution
                areaThreshold,
                minMargin,
                edgeBlurSize
            );

        }

        private static readonly object lolo = new object();

        public static MSER CreateAdaptiveMSERForSceneV1(Mat grayImage)
        {
            lock (lolo)
            {



                double mean = Cv2.Mean(grayImage).Val0;
                double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);
                double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

                // Default değerler
                int delta = 5;
                int minArea = 60;
                int maxArea = 5000;
                double maxVariation = 0.5;
                double minDiversity = 0.3;
                double areaThreshold = 1.01;
                double minMargin = 0.5;
                int edgeBlurSize = 5;

                // 🌕 Aşırı parlak sahne
                if (mean > 200)
                {
                    maxVariation = 0.35;
                    minDiversity = 0.25;
                    delta = 6;
                }
                // 🌑 Karanlık sahne
                else if (mean < 80)
                {
                    delta = 4;
                    minArea = 40;
                    maxVariation = 0.55;
                }

                // 🌫 Düşük kontrast sahne
                if (contrast < 40)
                {
                    minArea = 40;
                    maxVariation = 0.55;
                    delta = 4;
                }

                // ⚠️ Çok düşük detaylı görüntüler
                if (stdDev < 10)
                {
                    delta = Math.Max(3, delta - 1);  // daha hassas
                    minArea = Math.Max(30, minArea - 10);
                    maxVariation = Math.Min(0.65, maxVariation + 0.15);
                    minDiversity = Math.Max(0.2, minDiversity - 0.1); // daha fazla benzer yapıya izin
                }
                else if (stdDev > 70)
                {
                    maxVariation = Math.Min(0.45, maxVariation);
                    minDiversity += 0.05;
                }

                return MSER.Create(
                    delta,
                    minArea,
                    maxArea,
                    maxVariation,
                    minDiversity,
                    200,            // maxEvolution
                    areaThreshold,
                    minMargin,
                    edgeBlurSize
                );
            }
        }

        public static List<MserResult> FindCharactersWithMSER(Mat image)
        {
            var mserRegion = MSER.Create(
 delta: 5,
 minArea: 12,
 maxArea: 300,
 maxVariation: 0.45,
 minDiversity: 0.4,
 maxEvolution: 200,
 areaThreshold: 1.01,
 minMargin: 0.5,  // Düşük bir minMargin değeri
 edgeBlurSize: 5
);
            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(image, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();

            return sortedBBoxes;

        }

        public static List<MserResult> FindCharactersWithMSERv2(Mat image)
        {
            var mserRegion = CreateAdaptiveMSERForResizedPlateForEnhanced(image);

            //var mserRegion = CreateAdaptiveMSERForResizedPlateForEnhancedHighContrast(image);

            //var mserRegion = MSER.Create();

            //var mserRegion = CreateAdaptiveMSERForResizedPlate(image);

            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(image, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();

            return sortedBBoxes;

        }


        public static List<MserResult> FindCharactersWithMSERvBetaTestSon(Mat image)
        {
            var mserRegion = CharacterMSERBeta(image);

            //var mserRegion = CreateAdaptiveMSERForResizedPlateForEnhancedHighContrast(image);

            //var mserRegion = MSER.Create();

            //var mserRegion = CreateAdaptiveMSERForResizedPlate(image);

            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(image, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();

            return sortedBBoxes;

        }



        public static MSER CreateAdaptiveMSERForResizedPlate(Mat plateImage)
        {
            //Mat gray = new Mat();
            //Cv2.CvtColor(plateImage, gray, ColorConversionCodes.BGR2GRAY);

            double mean = Cv2.Mean(plateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(plateImage);

            int minArea = 20;
            int maxArea = 400;
            double maxVariation = 0.45;
            double minDiversity = 0.4;

            if (mean > 210)
            {
                maxVariation = 0.35;
                minDiversity = 0.3;
            }
            else if (mean < 70)
            {
                minArea = 15;
                maxVariation = 0.5;
            }

            if (contrast < 30)
            {
                minArea = 12;
                maxVariation = 0.3;
            }

            return MSER.Create(
                delta: 4,
                minArea: minArea,
                maxArea: maxArea,
                maxVariation: maxVariation,
                minDiversity: minDiversity,
                maxEvolution: 200,
                areaThreshold: 1.01,
                minMargin: 0.5,
                edgeBlurSize: 5
            );
        }

        //public static MSER CreateAdaptiveMSERForResizedPlateForEnhanced(Mat enhancedPlateImage)
        //{
        //    double mean = Cv2.Mean(enhancedPlateImage).Val0;
        //    double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
        //    double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

        //    int minArea = 18;
        //    int maxArea = 450;
        //    double maxVariation = 0.45;
        //    double minDiversity = 0.4;

        //    // 🌕 Çok parlak görüntüler → daha seçici
        //    if (mean > 210)
        //    {
        //        maxVariation = 0.35;
        //        minDiversity = 0.3;
        //        minArea = 20;
        //    }

        //    // 🌑 Karanlık durumlar → daha toleranslı
        //    else if (mean < 70)
        //    {
        //        maxVariation = 0.5;
        //        minArea = 15;
        //    }

        //    // ⚡ Düşük kontrast (hala biraz silik olabilir)
        //    if (contrast < 30)
        //    {
        //        maxVariation = 0.4;
        //        minArea = 12;
        //    }

        //    // 📉 Düşük varyasyon → biraz daha toleranslı davran
        //    if (stdDev < 20)
        //    {
        //        maxVariation += 0.05;
        //        minDiversity -= 0.05;
        //    }

        //    return MSER.Create(
        //        delta: 4,
        //        minArea: minArea,
        //        maxArea: maxArea,
        //        maxVariation: maxVariation,
        //        minDiversity: minDiversity,
        //        maxEvolution: 200,
        //        areaThreshold: 1.01,
        //        minMargin: 0.5,
        //        edgeBlurSize: 5
        //    );
        //}

        //public static MSER CreateAdaptiveMSERForResizedPlateForEnhanced(Mat enhancedPlateImage)
        //{
        //    double mean = Cv2.Mean(enhancedPlateImage).Val0;
        //    double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
        //    double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

        //    int minArea = 18;
        //    int maxArea = 450;
        //    double maxVariation = 0.40;
        //    double minDiversity = 0.4;

        //    // 🌕 Aşırı parlak → daha seçici
        //    if (mean > 210)
        //    {
        //        maxVariation = 0.32;
        //        minDiversity = 0.35;
        //        minArea = 20;
        //    }
        //    // 🌑 Karanlık → biraz daha esnek
        //    else if (mean < 70)
        //    {
        //        maxVariation = 0.45;
        //        minArea = 15;
        //    }

        //    // ⚡ Düşük kontrastlı plakalarda (CLAHE sonrası bile silik olabilir)
        //    if (contrast < 35)
        //    {
        //        maxVariation = Math.Min(maxVariation + 0.05, 0.45);
        //        minArea = Math.Max(minArea - 2, 12);
        //    }

        //    // 📉 Düşük varyasyonlu (stdDev) → küçük tolerans
        //    if (stdDev < 18)
        //    {
        //        maxVariation = Math.Min(maxVariation + 0.03, 0.45);
        //        minDiversity = Math.Max(minDiversity - 0.05, 0.3);
        //    }

        //    return MSER.Create(
        //        delta: 4,
        //        minArea: minArea,
        //        maxArea: maxArea,
        //        maxVariation: maxVariation,
        //        minDiversity: minDiversity,
        //        maxEvolution: 200,
        //        areaThreshold: 1.01,
        //        minMargin: 0.5,
        //        edgeBlurSize: 5
        //    );
        //}

        public static MSER CreateAdaptiveMSERForResizedPlateForEnhancedvAAAhmet(Mat enhancedPlateImage)
        {
            double mean = Cv2.Mean(enhancedPlateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

            int minArea = 18;
            int maxArea = 450;
            double maxVariation = 0.45;
            double minDiversity = 0.4;
            int delta = 5;
            int edgeBlurSize = 5;

            // 🌕 Çok parlak görüntü → varyasyon düşükse daha sıkı
            if (mean > 210 && stdDev < 20)
            {
                maxVariation = 0.35;
                minDiversity = 0.3;
                minArea = 22;
                delta = 5;
            }
            else if (mean > 180)
            {
                maxVariation = 0.38;
                minDiversity = 0.32;
                minArea = 18;
            }
            else if (mean < 80)
            {
                maxVariation = 0.5;
                minArea = 12;
                delta = 3;
            }

            // ⚡ Düşük kontrast → ama normalize edildiyse sert davranma
            if (contrast < 30 && stdDev < 25)
            {
                maxVariation = Math.Min(0.40, maxVariation); // çok gevşeme
                delta = Math.Max(3, delta - 1);
                minArea = Math.Max(15, minArea);
            }

            // 📉 Düşük varyasyon → daha toleranslı
            if (stdDev < 18)
            {
                maxVariation += 0.04;
                minDiversity = Math.Max(0.25, minDiversity - 0.05);
                delta = Math.Max(3, delta - 1);
            }

            // 📈 Yüksek varyasyon → küçük varyasyonlara karşı dikkatli ol
            else if (stdDev > 50)
            {
                maxVariation = Math.Min(0.4, maxVariation);
                minDiversity += 0.05;
            }

            // 🧠 Özel durum: normalize edilmiş ama hâlâ parlak
            if (stdDev < 10 && mean > 180)
            {
                maxVariation = Math.Min(0.36, maxVariation);
                minArea = Math.Max(22, minArea);
            }

            return MSER.Create(
                delta: delta,
                minArea: minArea,
                maxArea: maxArea,
                maxVariation: maxVariation,
                minDiversity: minDiversity,
                maxEvolution: 200,
                areaThreshold: 1.01,
                minMargin: 0.5,
                edgeBlurSize: edgeBlurSize
            );
        }

        public static MSER CreateAdaptiveMSERForResizedPlateForEnhanced(Mat enhancedPlateImage)
        {
            double mean = Cv2.Mean(enhancedPlateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

            int minArea = 18;
            int maxArea = 450;
            double maxVariation = 0.45;
            double minDiversity = 0.4;
            int delta = 4;
            int edgeBlurSize = 5;

            // 🌕 Çok parlak ve düşük varyasyon
            if (mean > 210)
            {
                maxVariation = 0.35;
                minDiversity = 0.3;
                minArea = 20;
                delta = 5;
            }
            else if (mean > 180)
            {
                maxVariation = 0.38;
                minDiversity = 0.32;
                minArea = 18;
            }
            else if (mean < 80) // Karanlık → Daha küçük alanlar kabul
            {
                maxVariation = 0.5;
                minArea = 12;
                delta = 3;
            }

            // ⚡ Kontrast düşük → daha seçici varyasyon, küçük delta
            if (contrast < 25)
            {
                maxVariation = Math.Min(0.42, maxVariation);
                delta = 3;
                minArea = Math.Min(15, minArea);
            }

            // 📉 Varyasyon düşük → daha esnek
            // 📉 Düşük varyasyon
            if (stdDev < 18)
            {
                maxVariation += 0.05;
                minDiversity = Math.Max(0.25, minDiversity - 0.05);
                delta = Math.Max(3, delta - 1);
            }

            else if (stdDev > 50)
            {
                maxVariation = Math.Min(0.4, maxVariation);
                minDiversity += 0.05;
            }

            // 🔥 Yeni önerilen kontrol buraya eklenmeli:
            //if (stdDev < 10 && mean > 180)
            //{
            //    // Çok temiz ama detay kaybı riski olan parlaklık – karakterler erimiş olabilir
            //    maxVariation = Math.Min(0.38, maxVariation);
            //    minArea = Math.Max(22, minArea);
            //}

            return MSER.Create(
                delta: delta,
                minArea: minArea,
                maxArea: maxArea,
                maxVariation: maxVariation,
                minDiversity: minDiversity,
                maxEvolution: 200,
                areaThreshold: 1.01,
                minMargin: 0.5,
                edgeBlurSize: edgeBlurSize
            );

            //double mean = Cv2.Mean(enhancedPlateImage).Val0;
            //double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
            //double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

            //int minArea = 18;
            //int maxArea = 450;
            //double maxVariation = 0.45;
            //double minDiversity = 0.4;

            //// 🌕 Çok parlak görüntüler → daha seçici
            //if (mean > 210)
            //{
            //    maxVariation = 0.35;
            //    minDiversity = 0.3;
            //    minArea = 20;
            //}

            //// 🌑 Karanlık durumlar → daha toleranslı
            //else if (mean < 70)
            //{
            //    maxVariation = 0.5;
            //    minArea = 15;
            //}

            //// ⚡ Düşük kontrast (hala biraz silik olabilir)
            //if (contrast < 30)
            //{
            //    maxVariation = 0.4;
            //    minArea = 12;
            //}

            //// 📉 Düşük varyasyon → biraz daha toleranslı davran
            //if (stdDev < 20)
            //{
            //    maxVariation += 0.05;
            //    minDiversity -= 0.05;
            //}

            //return MSER.Create(
            //    delta: 4,
            //    minArea: minArea,
            //    maxArea: maxArea,
            //    maxVariation: maxVariation,
            //    minDiversity: minDiversity,
            //    maxEvolution: 200,
            //    areaThreshold: 1.01,
            //    minMargin: 0.5,
            //    edgeBlurSize: 5
            //);
        }


        public static MSER CharacterMSERBeta(Mat enhancedPlateImage)
        {
            double mean = Cv2.Mean(enhancedPlateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

            int minArea = 18;
            int maxArea = 450;
            double maxVariation = 0.45;
            double minDiversity = 0.4;
            int delta = 4;
            int edgeBlurSize = 5;

            // 🌕 Çok parlak ve düşük varyasyon → karakter erimiş olabilir
            if (mean > 210 && stdDev < 12)
            {
                maxVariation = 0.35;
                minDiversity = 0.3;
                minArea = 22;
                delta = 5;
            }
            else if (mean > 210)
            {
                maxVariation = 0.35;
                minDiversity = 0.3;
                minArea = 20;
                delta = 5;
            }
            else if (mean > 180)
            {
                maxVariation = 0.38;
                minDiversity = 0.32;
                minArea = 18;
            }
            else if (mean < 80)
            {
                maxVariation = 0.5;
                minArea = 12;
                delta = 3;
            }

            // ⚡ Kontrast düşük → daha seçici varyasyon, küçük delta
            if (contrast < 25)
            {
                maxVariation = Math.Min(0.42, maxVariation);
                delta = 3;
                minArea = Math.Min(15, minArea);
            }

            // 📉 Varyasyon çok düşük → biraz daha toleranslı
            if (stdDev < 18)
            {
                maxVariation += 0.05;
                minDiversity = Math.Max(0.25, minDiversity - 0.05);
                delta = Math.Max(3, delta - 1);
            }
            else if (stdDev > 50)
            {
                maxVariation = Math.Min(0.4, maxVariation);
                minDiversity += 0.05;
            }
            else if (stdDev > 60)
            {
                maxVariation = Math.Min(0.35, maxVariation);
                minDiversity += 0.05;
            }

            // 🧩 Küçük plaka çözünürlüklerinde minArea esnet
            if (enhancedPlateImage.Rows < 35)
            {
                minArea = Math.Min(minArea, 14);
            }

            return MSER.Create(
                delta: delta,
                minArea: minArea,
                maxArea: maxArea,
                maxVariation: maxVariation,
                minDiversity: minDiversity,
                maxEvolution: 200,
                areaThreshold: 1.01,
                minMargin: 0.5,
                edgeBlurSize: edgeBlurSize
            );
        }

        public static MSER CreateAdaptiveMSERForResizedPlateForEnhancedHighContrast(Mat enhancedPlateImage)
        {
            double mean = Cv2.Mean(enhancedPlateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

            int minArea = 18;
            int maxArea = 450;
            double maxVariation = 0.45;
            double minDiversity = 0.4;
            int delta = 4;
            int edgeBlurSize = 5;

            //Debug.WriteLine($"[MSER PARAM] mean: {mean:F1}, contrast: {contrast:F1}, stdDev: {stdDev:F1}");

            // 🚨 Aşırı işlenmiş veya normalize edilmiş görüntü — fazla segmentasyonu engelle
            if (contrast >= 250 && stdDev > 30)
            {
                maxVariation = 0.35;
                minDiversity = 0.35;
                delta = 5;
                minArea = Math.Max(minArea, 22);

                //Debug.WriteLine("[MSER MODE] Aşırı kontrastlı → Sert segmentasyon");
            }
            else
            {
                // 🌕 Çok parlak görüntü
                if (mean > 210)
                {
                    maxVariation = 0.35;
                    minDiversity = 0.3;
                    minArea = 20;
                    delta = 5;

                    //Debug.WriteLine("[MSER MODE] Çok parlak plaka");
                }
                else if (mean > 180)
                {
                    maxVariation = 0.38;
                    minDiversity = 0.32;
                    minArea = 18;

                    //Debug.WriteLine("[MSER MODE] Parlak plaka");
                }
                else if (mean < 80)
                {
                    maxVariation = 0.5;
                    minArea = 12;
                    delta = 3;

                    //Debug.WriteLine("[MSER MODE] Karanlık plaka");
                }

                // ⚡ Düşük kontrast
                if (contrast < 25)
                {
                    maxVariation = Math.Min(0.42, maxVariation);
                    delta = 3;
                    minArea = Math.Min(15, minArea);

                    //Debug.WriteLine("[MSER MODE] Düşük kontrast");
                }

                // 📉 Düşük varyasyon
                if (stdDev < 18)
                {
                    maxVariation += 0.05;
                    minDiversity = Math.Max(0.25, minDiversity - 0.05);
                    delta = Math.Max(3, delta - 1);

                    //Debug.WriteLine("[MSER MODE] Düşük varyasyon");
                }
                else if (stdDev > 50)
                {
                    maxVariation = Math.Min(0.4, maxVariation);
                    minDiversity += 0.05;

                    //Debug.WriteLine("[MSER MODE] Yüksek varyasyon");
                }
            }

            return MSER.Create(
                delta,
                minArea,
                maxArea,
                maxVariation,
                minDiversity,
                200,            // maxEvolution
                1.01,           // areaThreshold
                0.5,            // minMargin
                edgeBlurSize
            );
        }
        public static MSER CreateAdaptiveMSERForResizedPlateForEnhancedOld(Mat enhancedPlateImage)
        {
            double mean = Cv2.Mean(enhancedPlateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(enhancedPlateImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(enhancedPlateImage);

            int minArea = 18;
            int maxArea = 450;
            double maxVariation = 0.45;
            double minDiversity = 0.4;
            int delta = 4;
            int edgeBlurSize = 5;

            // 🌕 Çok parlak ve düz sahne
            if (mean > 210)
            {
                maxVariation = 0.35;
                minDiversity = 0.3;
                minArea = 20;
                delta = 5;
            }

            // 🌑 Karanlık görüntü → daha toleranslı
            if (mean < 70)
            {
                maxVariation = 0.5;
                minArea = 15;
                delta = 3;
            }

            // ⚡ Düşük kontrast karakterler → daha hassas ama daha seçici
            if (contrast < 30)
            {
                maxVariation = 0.4;
                minArea = 12;
                delta = 3;
            }

            // 📉 Çok düşük varyasyon → daha esnek yaklaş
            if (stdDev < 20)
            {
                maxVariation += 0.05;
                minDiversity -= 0.05;
                delta = Math.Max(3, delta - 1);  // daha sık sampling
            }

            return MSER.Create(
                delta: delta,
                minArea: minArea,
                maxArea: maxArea,
                maxVariation: maxVariation,
                minDiversity: minDiversity,
                maxEvolution: 200,
                areaThreshold: 1.01,
                minMargin: 0.5,
                edgeBlurSize: edgeBlurSize
            );
        }

        public static Rect[] DetectPlateWithCannyMSER(Mat grayImage)
        {

            List<Rect> loo = new List<Rect>();

            // **1️⃣ Kenar Algılama (Canny)**
            Mat edges = new Mat();
            Cv2.Canny(grayImage, edges, 0, 200); // Kenarları belirginleştir

            OpenCvSharp.Rect[] bboxes;

            var contours = new OpenCvSharp.Point[][] { };
            var hierarchy = new HierarchyIndex[] { };
            Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // **2️⃣ MSER Algılama**
            var mser = MSER.Create();


            foreach (var contour in contours)
            {
                Rect boundingBox = Cv2.BoundingRect(contour);

                if (boundingBox.Width > 60 && boundingBox.Width < 4000 && boundingBox.Height > 20 && boundingBox.Height < 1500)
                {
                    // 🚀 **MSER kullanarak plaka doğrulama**
                    Mat roi = grayImage[boundingBox];
                    mser.DetectRegions(roi, out var mserContours, out bboxes);

                    if (mserContours.Length > 0)
                    {
                        loo.Add(boundingBox);
                    }
                }
            }


            //Mat mask = new Mat(grayImage.Size(), MatType.CV_8UC1, Scalar.All(0));
            //var regions = new List<Rect>();



            //mser.DetectRegions(edges, out var contours, out bboxes);

            //foreach (var contour in contours)
            //{
            //    Rect boundingBox = Cv2.BoundingRect(contour);

            //    // **3️⃣ Geometrik Filtreleme (Mantıklı Plaka Boyutlarını Seç)**
            //    if (IsValidPlateSize(boundingBox, frame.Size()))
            //    {
            //        regions.Add(boundingBox);
            //    }
            //}

            //if (regions.Count == 0)
            //    return null; // Plaka bulunamadı

            //// **4️⃣ En iyi plakayı seç (Örneğin en büyük olanı)**
            //Rect bestPlate = regions.OrderByDescending(r => r.Width * r.Height).First();

            return loo.ToArray();
        }

        public static IEnumerable<MserResult> SegmentCharacterInPlateWithMSER(Mat threshould)
        {

            // MSER nesnesini oluşturma
            //// MSER algoritmasını başlatın
            var mserRegion = MSER.Create(
                delta: 20,
                minArea: 17,
                maxArea: 250,
                maxVariation: 0.25,
                minDiversity: 0.3,
                maxEvolution: 200,
                areaThreshold: 1.01,
                minMargin: 0.001,
                edgeBlurSize: 10
            );

            //var mserRegion = MSER.Create();
            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(threshould, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();

            List<MserResult> filteredResults = RectProcessingHelper.FilterSortedBBoxes(sortedBBoxes);

            List<MserResult> alignedResults = RectProcessingHelper.AlignedResults(filteredResults);

            return alignedResults;
        }

        public static IEnumerable<MserResult> SegmentCharacterInPlate(Mat threshould)
        {
            var mserRegion = MSER.Create(
 delta: 5,
 minArea: 12,
 maxArea: 250,
 maxVariation: 0.45,
 minDiversity: 0.4,
 maxEvolution: 200,
 areaThreshold: 1.01,
 minMargin: 0.5,  // Düşük bir minMargin değeri
 edgeBlurSize: 5
);



            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(threshould, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();


            //plakanın orta noktasından sonra tespit edilen alan varsa filtrele
            List<MserResult> possibleRegions = RectProcessingHelper.FilterRectsBelowAverageY(sortedBBoxes, threshould.Rows);

            //alanları boyutlarına göre filtrele
            List<MserResult> filteredCharacterRegions = CharacterHelper.FilterPossibleCharacterRegions(possibleRegions);


            //filter for diagonal
            //List<MserResult> mamu = RectFilterHelper.FilterByDiagonalLengthZScore(filteredCharacterRegions, 0.8);



            //alanları x koordinatına göre 4'erli olarak grupluyor
            List<List<MserResult>> rectsByProximity = RectProcessingHelper.GroupRectsByProximity(filteredCharacterRegions, 4);

            //iki boundingbox rect'ti karşılaştırıyor çok yakın olan ve kesişen rectleri birleştiriyor - mergerect
            List<List<MserResult>> characterRegions = RectProcessingHelper.CheckRectCoordinat(rectsByProximity);

            //olası karakter alanı için boundingbox grubu içindeki en uygun item'i median olarak karşılaştırıp seçiyor 
            List<MserResult> characters = FilterHelper.SelectSimilarItemsFromGroupsWithMedian(characterRegions);
            //List<MserResult> characters = FilterHelper.SelectItemsWithSequentialBoundingBoxes(characterRegions);




            //karakter rect'lerinin yüksekliklerini median'a göre karşılaştırıp threshould'a uymayanları filtrele
            List<MserResult> filteredCharacters = FilterHelper.FilterGroupsByHeightMedian(characters, 5);




            return filteredCharacters;
        }


        public static IEnumerable<MserResult> SegmentCharacterInPlate1(Mat threshould)
        {
            var mserRegion = MSER.Create(
 delta: 5,
 minArea: 12,
 maxArea: 150,
 maxVariation: 0.45,
 minDiversity: 0.4,
 maxEvolution: 200,
 areaThreshold: 1.01,
 minMargin: 0.5,  // Düşük bir minMargin değeri
 edgeBlurSize: 5
);
            Mat edges = new Mat();
            Cv2.AdaptiveThreshold(threshould, edges, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 15, 5);
            //Cv2.Threshold(threshould, edges, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(edges, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();


            //plakanın orta noktasından sonra tespit edilen alan varsa filtrele
            List<MserResult> possibleRegions = RectProcessingHelper.FilterRectsBelowAverageY(sortedBBoxes, threshould.Rows);

            //alanları boyutlarına göre filtrele
            List<MserResult> filteredCharacterRegions = CharacterHelper.FilterPossibleCharacterRegions(possibleRegions);


            //filter for diagonal
            //List<MserResult> mamu = RectFilterHelper.FilterByDiagonalLengthZScore(filteredCharacterRegions, 0.8);



            //alanları x koordinatına göre 4'erli olarak grupluyor
            List<List<MserResult>> rectsByProximity = RectProcessingHelper.GroupRectsByProximity(filteredCharacterRegions, 4);

            //iki boundingbox rect'ti karşılaştırıyor çok yakın olan ve kesişen rectleri birleştiriyor - mergerect
            List<List<MserResult>> characterRegions = RectProcessingHelper.CheckRectCoordinat(rectsByProximity);

            //olası karakter alanı için boundingbox grubu içindeki en uygun item'i median olarak karşılaştırıp seçiyor 
            List<MserResult> characters = FilterHelper.SelectSimilarItemsFromGroupsWithMedian(characterRegions);
            //List<MserResult> characters = FilterHelper.SelectItemsWithSequentialBoundingBoxes(characterRegions);




            //karakter rect'lerinin yüksekliklerini median'a göre karşılaştırıp threshould'a uymayanları filtrele
            List<MserResult> filteredCharacters = FilterHelper.FilterGroupsByHeightMedian(characters, 5);





            return filteredCharacters;
        }




        public static IEnumerable<MserResult> SegmentCharacterInPlateDB(Mat threshould)
        {
            var mserRegion = MSER.Create(
 delta: 5,
 minArea: 12,
 maxArea: 250,
 maxVariation: 0.45,
 minDiversity: 0.4,
 maxEvolution: 200,
 areaThreshold: 1.01,
 minMargin: 0.5,  // Düşük bir minMargin değeri
 edgeBlurSize: 5
);



            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(threshould, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();


            //plakanın orta noktasından sonra tespit edilen alan varsa filtrele
            List<MserResult> possibleRegions = RectProcessingHelper.FilterRectsBelowAverageY(sortedBBoxes, threshould.Rows);

            //alanları boyutlarına göre filtrele
            List<MserResult> filteredCharacterRegions = CharacterHelper.FilterPossibleCharacterRegions(possibleRegions);


            //filter for diagonal
            //List<MserResult> mamu = RectFilterHelper.FilterByDiagonalLengthZScore(filteredCharacterRegions, 0.8);



            //alanları x koordinatına göre 4'erli olarak grupluyor
            List<List<MserResult>> rectsByProximity = RectProcessingHelper.GroupRectsByProximity(filteredCharacterRegions, 4);

            //iki boundingbox rect'ti karşılaştırıyor çok yakın olan ve kesişen rectleri birleştiriyor - mergerect
            List<List<MserResult>> characterRegions = RectProcessingHelper.CheckRectCoordinat(rectsByProximity);

            //olası karakter alanı için boundingbox grubu içindeki en uygun item'i median olarak karşılaştırıp seçiyor 
            List<MserResult> characters = FilterHelper.SelectSimilarItemsFromGroupsWithMedian(characterRegions);
            //List<MserResult> characters = FilterHelper.SelectItemsWithSequentialBoundingBoxes(characterRegions);




            //karakter rect'lerinin yüksekliklerini median'a göre karşılaştırıp threshould'a uymayanları filtrele
            List<MserResult> filteredCharacters = FilterHelper.FilterGroupsByHeightMedian(characters, 5);


            List<MserResult> mergedList = rectsByProximity.SelectMany(group => group).ToList();

            // List<MserResult> huhu = new List<MserResult>();

            // foreach (var characterRegion in rectsByProximity)
            // {
            //     huhu.Add(characterRegion.);
            // }

            //var sdf = rectsByProximity.ToList();



            return characters;
        }




        public static IEnumerable<MserResult> SegmentCharacterInPlate1222(Mat threshould)
        {
            Mat gray = new Mat();

            Mat sdsdsd = new Mat();

            Cv2.CvtColor(threshould, gray, ColorConversionCodes.BGR2GRAY);

            var mserRegion = MSER.Create(
 delta: 5,
 minArea: 12,
 maxArea: 150,
 maxVariation: 0.45,
 minDiversity: 0.4,
 maxEvolution: 200,
 areaThreshold: 1.01,
 minMargin: 0.5,  // Düşük bir minMargin değeri
 edgeBlurSize: 5
);


            Mat edges = new Mat();
            Cv2.AdaptiveThreshold(gray, sdsdsd, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 15, 5);
            Cv2.Threshold(gray, edges, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserRegion.DetectRegions(sdsdsd, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
                           .ToList();




            Mat frameWithPlates = threshould.Clone();
            Mat frameWithPlates2 = threshould.Clone();

            Random rng = new Random();




            //mserRegion.DetectRegions(gray, out msersPlate, out bboxesPlate);

            //var sortedBBoxes2 = msersPlate
            //               .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
            //               .OrderBy(bbox => bbox.BBox.X)  // En büyükten küçüğe sırala
            //               .ToList();


            //foreach (var item in sortedBBoxes2)
            //{
            //    Scalar color = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256));
            //    Cv2.Rectangle(frameWithPlates2, item.BBox, color, 2);
            //}


            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(threshould));

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(frameWithPlates));

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(frameWithPlates2));






            //plakanın orta noktasından sonra tespit edilen alan varsa filtrele
            List<MserResult> possibleRegions = RectProcessingHelper.FilterRectsBelowAverageY(sortedBBoxes, threshould.Rows);

            //alanları boyutlarına göre filtrele
            List<MserResult> filteredCharacterRegions = CharacterHelper.FilterPossibleCharacterRegions(possibleRegions);


            //filter for diagonal
            //List<MserResult> mamu = RectFilterHelper.FilterByDiagonalLengthZScore(filteredCharacterRegions, 0.8);



            //alanları x koordinatına göre 4'erli olarak grupluyor
            List<List<MserResult>> rectsByProximity = RectProcessingHelper.GroupRectsByProximity(filteredCharacterRegions, 4);

            //iki boundingbox rect'ti karşılaştırıyor çok yakın olan ve kesişen rectleri birleştiriyor - mergerect
            List<List<MserResult>> characterRegions = RectProcessingHelper.CheckRectCoordinat(rectsByProximity);

            //olası karakter alanı için boundingbox grubu içindeki en uygun item'i median olarak karşılaştırıp seçiyor 
            List<MserResult> characters = FilterHelper.SelectSimilarItemsFromGroupsWithMedian(characterRegions);
            //List<MserResult> characters = FilterHelper.SelectItemsWithSequentialBoundingBoxes(characterRegions);




            //karakter rect'lerinin yüksekliklerini median'a göre karşılaştırıp threshould'a uymayanları filtrele
            List<MserResult> filteredCharacters = FilterHelper.FilterGroupsByHeightMedian(characters, 5);

            foreach (var item in sortedBBoxes)
            {
                Scalar color = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256));
                Cv2.Rectangle(frameWithPlates, item.BBox, color, 2);
            }

            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(threshould));

            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(frameWithPlates));



            return sortedBBoxes;
        }

    }
}
