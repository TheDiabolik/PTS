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
    internal class Plate
    {

        public static List<PossiblePlate> FindPlateCandidates(Rect[] bboxes, Mat image)
        {
            //List<Mat> possiblePlateRegions = new List<Mat>();
            List<Rect> addedRects = new List<Rect>();  // Eklenen dikdörtgenlerin takibini yapmak için

            List<PossiblePlate> value = new List<PossiblePlate>();


            #region Set Variables
            int minWidth = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinWidth;
            int maxWidth = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxWidth;

            int minHeight = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinHeight;
            int maxHeight = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxHeight;

            double minAspectRatio = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinAspectRatio;
            double maxAspectRatio = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxAspectRatio;

            double minArea = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinArea;
            double maxArea = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxArea;
            #endregion


            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox);

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) && // Genişlik kontrolü (piksellere göre ayarlanmış)
                             (bbox.Height > minHeight && bbox.Height < maxHeight) && //&& // Yükseklik kontrolü (piksellere göre ayarlanmış)
                (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio) &&                      // En-boy oranı kontrolü
                 (area > minArea && area < maxArea);

                if (isPlate)
                {
                    // Aynı veya çakışan bir dikdörtgen eklenmiş mi kontrol et
                    bool alreadyExists = addedRects.Any(existingRect => RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                    if (!alreadyExists)
                    {
                        Mat plate = new Mat(image, bbox);

                        //plaka classifi
                        Mat mat = plate.Clone();
                        //Cv2.Resize(mat, mat, new OpenCvSharp.Size(144, 32)); // HOG ile uyumlu boyut

                        int isPlateRegion = SVMHelper.AskSVMPredictionForPlateRegion(MainForm.m_mainForm.m_loadedSvmForPlateRegion, mat);

                        //if (isPlateRegion == 0)
                        {
                            addedRects.Add(bbox);  // Bu dikdörtgeni eklenenler listesine ekle

                            PossiblePlate pos = new PossiblePlate();
                            pos.possiblePlateRegions = plate;
                            pos.addedRects = bbox;

                            value.Add(pos);
                        }
                    }
                }
            }


            return value;
        }



        public static List<PossiblePlate> FindPlateCandidatesAdaptive(Rect[] bboxes, Mat image)
        {
            List<PossiblePlate> value = new List<PossiblePlate>();

            List<Rect> candidatePlates = new List<Rect>();

            double imgWidth = image.Width;
            double imgHeight = image.Height;

            // **1️⃣ Ortalama plaka boyutunu görüntüye göre belirle**
            double estimatedPlateWidth = imgWidth / 5; // Görüntünün yaklaşık %20-30'u kadar
            double estimatedPlateHeight = imgHeight / 15; // Görüntünün yaklaşık %5-10'u kadar
            double estimatedAspectRatio = 4.8; // Ortalama Türk plakası oranı

            int minWidth = (int)(estimatedPlateWidth * 0.5);
            int maxWidth = (int)(estimatedPlateWidth * 1.5);
            int minHeight = (int)(estimatedPlateHeight * 0.5);
            int maxHeight = (int)(estimatedPlateHeight * 1.5);
            double minAspectRatio = estimatedAspectRatio * 0.523;
            double maxAspectRatio = estimatedAspectRatio * 1.9;


            //double minWidth = estimatedPlateWidth * 0.3;
            //double maxWidth = estimatedPlateWidth * 2.0;
            //double minHeight = estimatedPlateHeight * 0.3;
            //double maxHeight = estimatedPlateHeight * 2.0;
            //double minAspectRatio = estimatedAspectRatio * 0.6;
            //double maxAspectRatio = estimatedAspectRatio * 3.5;// 3.5;



            // **3️⃣ Dinamik Geometrik Filtreleme**
            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox);

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                            (bbox.Height > minHeight && bbox.Height < maxHeight) &&
                (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio);

              

                if (!isPlate)
                    continue;

                bool alreadyExists = candidatePlates.Any(existingRect => RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                if (alreadyExists)
                    continue;

                Mat plate = new Mat(image, bbox);



                //plaka classifi
                //Mat mat = plate.Clone();
                Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut


                var result  = SVMHelper.AskSVMPredictionForPlateRegionWithScorev1(MainForm.m_mainForm.m_loadedSvmForPlateRegion, plate);

                if (result.predictedClass == 0)
                //if (result.isPlate && result.score > 1)
                {
                    ////Debug.WriteLine("Aspect Ratio : " + aspectRatio.ToString());
                    //string guid = System.Guid.NewGuid().ToString();
                    //mat.SaveImage("D:\\Arge Projeler\\PlateRecognation - Kopya\\PlateRecognation\\bin\\Debug\\net8.0-windows\\Plates1\\" + guid + ".jpg");

                    candidatePlates.Add(bbox);  // Bu dikdörtgeni eklenenler listesine ekle

                    var possiblePlate = new PossiblePlate
                    {
                        possiblePlateRegions = plate.Clone(),
                        addedRects = bbox,
                        PlateScore = result.score
                        
                    };


                    //PlateScoringHelper.ComputePlateScoreAdaptiveKenar(possiblePlate);

                    value.Add(possiblePlate);

                }

                plate.Dispose();
            }

            return value;
        }



        public static List<PossiblePlate> FindPlateCandidatesAdaptiveNMS(Rect[] bboxes, Mat image)
        {
            List<PossiblePlate> value = new List<PossiblePlate>();

            List<Rect> candidatePlates = new List<Rect>();

            double imgWidth = image.Width;
            double imgHeight = image.Height;

            // **1️⃣ Ortalama plaka boyutunu görüntüye göre belirle**
            double estimatedPlateWidth = imgWidth / 5; // Görüntünün yaklaşık %20-30'u kadar
            double estimatedPlateHeight = imgHeight / 15; // Görüntünün yaklaşık %5-10'u kadar
            double estimatedAspectRatio = 4.8; // Ortalama Türk plakası oranı

            int minWidth = (int)(estimatedPlateWidth * 0.5);
            int maxWidth = (int)(estimatedPlateWidth * 1.5);
            int minHeight = (int)(estimatedPlateHeight * 0.5);
            int maxHeight = (int)(estimatedPlateHeight * 1.5);
            double minAspectRatio = estimatedAspectRatio * 0.523;
            double maxAspectRatio = estimatedAspectRatio * 1.9;




            // **3️⃣ Dinamik Geometrik Filtreleme**
            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox);

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                            (bbox.Height > minHeight && bbox.Height < maxHeight) &&
                (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio);



                if (!isPlate)
                    continue;


                Mat plate = new Mat(image, bbox);

                //plaka classifi
                Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut


                var result = SVMHelper.AskSVMPredictionForPlateRegionWithScore(MainForm.m_mainForm.m_loadedSvmForPlateRegion, plate,0);

                if (result.isPlate)
                {
                    candidatePlates.Add(bbox);  // Bu dikdörtgeni eklenenler listesine ekle

                    var possiblePlate = new PossiblePlate
                    {
                        possiblePlateRegions = plate.Clone(),
                        addedRects = bbox,
                        PlateScore = result.score

                    };


                    value.Add(possiblePlate);
                }


                plate.Dispose();
            }

            double nmsIouThreshold = 0.5; // ihtiyaca göre ayarlayabilirsin
            var nmsPlates = NonMaximumSuppression(value, nmsIouThreshold);
            return nmsPlates;


            
        }


        public static List<PossiblePlate> FindPlateCandidatesFromROI(Rect[] bboxes, Mat roiImage)
        {
            List<PossiblePlate> value = new List<PossiblePlate>();
            List<Rect> candidatePlates = new List<Rect>();

            GeometricFilterSettings settings =  GeometricFilterSettings.GetSettingsForResolution(roiImage.Width, roiImage.Height);


            int minWidth = settings.MinWidth;
            int maxWidth = settings.MaxWidth;
            int minHeight = settings.MinHeight;
            int maxHeight = settings.MaxHeight;
            double minAspectRatio = settings.MinAspectRatio;
            double maxAspectRatio = settings.MaxAspectRatio;

            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);

                //bool isPlate = bbox.Width > minWidth &&
                //               bbox.Width < maxWidth &&
                //               bbox.Height > minHeight &&
                //               bbox.Height < maxHeight &&
                //aspectRatio >= minAspectRatio &&
                //aspectRatio <= maxAspectRatio;



                bool isPlate = aspectRatio >= minAspectRatio &&
                             aspectRatio <= maxAspectRatio;

                if (!isPlate)
                    continue;


                //Mat plate2 = new Mat(roiImage, bbox);
                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, plate2.ToBitmap());


                bool alreadyExists = candidatePlates.Any(existingRect =>
                    RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                if (alreadyExists)
                    continue;

                using (Mat plate = new Mat(roiImage, bbox))
                {
                    Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);

                    var result = SVMHelper.AskSVMPredictionForPlateRegionWithScore(
                        MainForm.m_mainForm.m_loadedSvmForPlateRegion, plate, 0);

                    ////Debug.WriteLine("Score : " + result.score);

                    if (result.isPlate)
                    {
                        candidatePlates.Add(bbox);

                        value.Add(new PossiblePlate
                        {
                            possiblePlateRegions = plate.Clone(),
                            addedRects = bbox,
                            PlateScore = result.score
                        });
                    }
                }
            }

            return value;
        }


        public static List<PossiblePlate> FindPlateCandidatesFromROIAHMET(Rect[] bboxes, Mat roiImage)
        {
            List<PossiblePlate> value = new List<PossiblePlate>();
            List<Rect> candidatePlates = new List<Rect>();

            //GeometricFilterSettings settings = GeometricFilterSettings.GetSettingsForResolution(roiImage.Width, roiImage.Height);



            // **1️⃣ Ortalama plaka boyutunu görüntüye göre belirle**
            double estimatedPlateWidth = roiImage.Width / 5; // Görüntünün yaklaşık %20-30'u kadar
            double estimatedPlateHeight = roiImage.Height / 15; // Görüntünün yaklaşık %5-10'u kadar
            double estimatedAspectRatio = 4.8; // Ortalama Türk plakası oranı

            int minWidth = (int)(estimatedPlateWidth * 0.5);
            int maxWidth = (int)(estimatedPlateWidth * 1.5);
            int minHeight = (int)(estimatedPlateHeight * 0.5);
            int maxHeight = (int)(estimatedPlateHeight * 1.5);
            double minAspectRatio = estimatedAspectRatio * 0.523;
            double maxAspectRatio = estimatedAspectRatio * 1.9;

            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);

                //bool isPlate = bbox.Width > minWidth &&
                //               bbox.Width < maxWidth &&
                //               bbox.Height > minHeight &&
                //               bbox.Height < maxHeight;//&&
                                                       //aspectRatio >= minAspectRatio &&
                                                       //aspectRatio <= maxAspectRatio;

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                           (bbox.Height > minHeight && bbox.Height < maxHeight) &&
               (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio);


                //bool isPlate = aspectRatio >= minAspectRatio &&
                //             aspectRatio <= maxAspectRatio;

                if (!isPlate)
                    continue;

                bool alreadyExists = candidatePlates.Any(existingRect =>
                    RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                if (alreadyExists)
                    continue;

                using (Mat plate = new Mat(roiImage, bbox))
                {
                    Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);

                    var result = SVMHelper.AskSVMPredictionForPlateRegionWithScore(
                        MainForm.m_mainForm.m_loadedSvmForPlateRegion, plate, 0);

                    ////Debug.WriteLine("Score : " + result.score);

                    if (result.isPlate)
                    {
                        candidatePlates.Add(bbox);

                        value.Add(new PossiblePlate
                        {
                            possiblePlateRegions = plate.Clone(),
                            addedRects = bbox,
                            PlateScore = result.score
                        });
                    }
                }
            }

            return value;
        }



        public static List<PossiblePlate> NonMaximumSuppression(List<PossiblePlate> plates, double iouThreshold)
        {
            var selected = new List<PossiblePlate>();
            // Score'a göre sırala
            var sorted = plates.OrderByDescending(p => p.PlateScore).ToList();

            while (sorted.Any())
            {
                var current = sorted.First();
                selected.Add(current);
                sorted.RemoveAt(0);

                sorted = sorted
                    .Where(p =>  RectComparisonHelper.IoU(current.addedRects, p.addedRects) < iouThreshold)
                    .ToList();
            }
            return selected;
        }





















        public static ThreadSafeList<PossiblePlate> ThreadSafeFindPlateCandidatesAdaptive(Rect[] bboxes, Mat image)
        {
            ThreadSafeList<PossiblePlate> value = new ThreadSafeList<PossiblePlate>();

            ThreadSafeList<Rect> candidatePlates = new ThreadSafeList<Rect>();

            double imgWidth = image.Width;
            double imgHeight = image.Height;

            // **1️⃣ Ortalama plaka boyutunu görüntüye göre belirle**
            double estimatedPlateWidth = imgWidth / 5; // Görüntünün yaklaşık %20-30'u kadar
            double estimatedPlateHeight = imgHeight / 15; // Görüntünün yaklaşık %5-10'u kadar
            double estimatedAspectRatio = 4.8; // Ortalama Türk plakası oranı

            int minWidth = (int)(estimatedPlateWidth * 0.5);
            int maxWidth = (int)(estimatedPlateWidth * 1.5);
            int minHeight = (int)(estimatedPlateHeight * 0.5);
            int maxHeight = (int)(estimatedPlateHeight * 1.5);
            double minAspectRatio = estimatedAspectRatio * 0.523;
            double maxAspectRatio = estimatedAspectRatio * 1.9;


            //double minWidth = estimatedPlateWidth * 0.3;
            //double maxWidth = estimatedPlateWidth * 2.0;
            //double minHeight = estimatedPlateHeight * 0.3;
            //double maxHeight = estimatedPlateHeight * 2.0;
            //double minAspectRatio = estimatedAspectRatio * 0.6;
            //double maxAspectRatio = estimatedAspectRatio * 3.5;// 3.5;



            // **3️⃣ Dinamik Geometrik Filtreleme**
            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox);

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                            (bbox.Height > minHeight && bbox.Height < maxHeight) &&
                (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio);



                if (!isPlate)
                    continue;

                bool alreadyExists = candidatePlates.Any(existingRect => RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                if (alreadyExists)
                    continue;

                Mat plate = new Mat(image, bbox);



                //plaka classifi
                //Mat mat = plate.Clone();
                Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut


                int isPlateRegion = SVMHelper.AskSVMPredictionForPlateRegion(MainForm.m_mainForm.m_loadedSvmForPlateRegion, plate);

                if (isPlateRegion == 0)
                {
                    ////Debug.WriteLine("Aspect Ratio : " + aspectRatio.ToString());
                    //string guid = System.Guid.NewGuid().ToString();
                    //mat.SaveImage("D:\\Arge Projeler\\PlateRecognation - Kopya\\PlateRecognation\\bin\\Debug\\net8.0-windows\\Plates1\\" + guid + ".jpg");

                    candidatePlates.Add(bbox);  // Bu dikdörtgeni eklenenler listesine ekle

                  

                    var possiblePlate = new PossiblePlate
                    {
                        possiblePlateRegions = plate.Clone(),
                        addedRects = bbox
                    };


                    //PlateScoringHelper.ComputePlateScoreAdaptiveKenar(possiblePlate);

                    value.Add(possiblePlate);

                }

                plate.Dispose();
            }

            return value;
        }

        public static List<PossiblePlate> FindPlateCandidatesAdaptivevTest(Rect[] bboxes, Mat image, Mat process)
        {
            List<PossiblePlate> value = new List<PossiblePlate>();

            List<Rect> candidatePlates = new List<Rect>();

            double imgWidth = image.Width;
            double imgHeight = image.Height;

            // **1️⃣ Ortalama plaka boyutunu görüntüye göre belirle**
            double estimatedPlateWidth = imgWidth / 5; // Görüntünün yaklaşık %20-30'u kadar
            double estimatedPlateHeight = imgHeight / 15; // Görüntünün yaklaşık %5-10'u kadar
            double estimatedAspectRatio = 4.8; // Ortalama Türk plakası oranı

            int minWidth = (int)(estimatedPlateWidth * 0.5);
            int maxWidth = (int)(estimatedPlateWidth * 1.5);
            int minHeight = (int)(estimatedPlateHeight * 0.5);
            int maxHeight = (int)(estimatedPlateHeight * 1.5);
            double minAspectRatio = estimatedAspectRatio * 0.523;
            double maxAspectRatio = estimatedAspectRatio * 1.9;


            //double minWidth = estimatedPlateWidth * 0.3;
            //double maxWidth = estimatedPlateWidth * 2.0;
            //double minHeight = estimatedPlateHeight * 0.3;
            //double maxHeight = estimatedPlateHeight * 2.0;
            //double minAspectRatio = estimatedAspectRatio * 0.6;
            //double maxAspectRatio = estimatedAspectRatio * 3.5;// 3.5;



            // **3️⃣ Dinamik Geometrik Filtreleme**
            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox);

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                            (bbox.Height > minHeight && bbox.Height < maxHeight) &&
                (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio);



                if (!isPlate)
                    continue;

                bool alreadyExists = candidatePlates.Any(existingRect => RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                if (alreadyExists)
                    continue;

                Mat plate = new Mat(image, bbox);
                Mat plateprocess = new Mat(process, bbox);


                //plaka classifi
                Mat mat = plateprocess.Clone();
                Cv2.Resize(mat, mat, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut


                int isPlateRegion = SVMHelper.AskSVMPredictionForPlateRegion(MainForm.m_mainForm.m_loadedSvmForPlateRegion, mat);

                if (isPlateRegion == 0)
                {
                    ////Debug.WriteLine("Aspect Ratio : " + aspectRatio.ToString());
                    //string guid = System.Guid.NewGuid().ToString();
                    //mat.SaveImage("D:\\Arge Projeler\\PlateRecognation - Kopya\\PlateRecognation\\bin\\Debug\\net8.0-windows\\Plates1\\" + guid + ".jpg");


                    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(plate));
                    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox6, BitmapConverter.ToBitmap(plateprocess));

                    candidatePlates.Add(bbox);  // Bu dikdörtgeni eklenenler listesine ekle

                    var possiblePlate = new PossiblePlate
                    {
                        possiblePlateRegions = mat,
                        addedRects = bbox
                    };


                    //PlateScoringHelper.ComputePlateScoreAdaptiveKenar(possiblePlate);

                    value.Add(possiblePlate);

                }
            }

            return value;
        }

        public static bool IsBoxTouchingFrameEdge(Rect bbox, int imgWidth, int imgHeight, int margin = 3)
        {
            return bbox.Y <= margin ||
                   bbox.Y + bbox.Height >= imgHeight - margin ||
                   bbox.X <= margin ||
                   bbox.X + bbox.Width >= imgWidth - margin;
        }

        public static bool IsBoxTouchingFrameEdge(Rect bbox, Mat image)
        {
            int margin = 2;

            bool top = bbox.Y <= margin;
            bool bottom = bbox.Y + bbox.Height >= image.Height - margin;
            bool left = bbox.X <= margin;
            bool right = bbox.X + bbox.Width >= image.Width - margin;

            return top || bottom || left || right;
        }

        public static List<PossiblePlate> FindPlateCandidatesAdaptive(Rect[] bboxes, Mat image, Mat processFrame)
        {
            List<PossiblePlate> value = new List<PossiblePlate>();

            List<Rect> candidatePlates = new List<Rect>();

            double imgWidth = image.Width;
            double imgHeight = image.Height;

            // **1️⃣ Ortalama plaka boyutunu görüntüye göre belirle**
            double estimatedPlateWidth = imgWidth / 5; // Görüntünün yaklaşık %20-30'u kadar
            double estimatedPlateHeight = imgHeight / 15; // Görüntünün yaklaşık %5-10'u kadar
            double estimatedAspectRatio = 4.8; // Ortalama Türk plakası oranı

            int minWidth = (int)(estimatedPlateWidth * 0.5);
            int maxWidth = (int)(estimatedPlateWidth * 1.5);
            int minHeight = (int)(estimatedPlateHeight * 0.5);
            int maxHeight = (int)(estimatedPlateHeight * 1.5);
            double minAspectRatio = estimatedAspectRatio * 0.523;
            double maxAspectRatio = estimatedAspectRatio * 1.9;


            //double minWidth = estimatedPlateWidth * 0.3;
            //double maxWidth = estimatedPlateWidth * 2.0;
            //double minHeight = estimatedPlateHeight * 0.3;
            //double maxHeight = estimatedPlateHeight * 2.0;
            //double minAspectRatio = estimatedAspectRatio * 0.6;
            //double maxAspectRatio = estimatedAspectRatio * 3.5;// 3.5;


            int i = 0;

            // **3️⃣ Dinamik Geometrik Filtreleme**
            foreach (var bbox in bboxes)
            {
                double aspectRatio = RectGeometryHelper.CalculateAspectRatio(bbox);
                int area = RectGeometryHelper.CalculateRectangleArea(bbox);

                bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                            (bbox.Height > minHeight && bbox.Height < maxHeight) &&
                (aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio);

                //bool isPlate = (bbox.Width > minWidth && bbox.Width < maxWidth) &&
                //          (bbox.Height > minHeight && bbox.Height < maxHeight);

                if (!isPlate)
                    continue;

                bool alreadyExists = candidatePlates.Any(existingRect => RectComparisonHelper.AreRectsCloseAndSimilar(existingRect, bbox));

                if (alreadyExists)
                    continue;



                //candidatePlates.Add(bbox);

                Mat plate = new Mat(image, bbox);

                //Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(plate);

                // **Kenar yoğunluğu hesapla**
                //Mat edges = new Mat();
                //Cv2.Canny(plate, edges, 50, 200);
                //int edgeDensity = Cv2.CountNonZero(edges);

                //// **Kenar yoğunluğu çok düşükse, SVM'ye göndermeden eliyoruz.**
                //if (edgeDensity < 419)  // Bu değeri test ederek ayarlayabilirsin.
                //{
                //    ////Debug.WriteLine("Düşük kenar yoğunluğu: Atlanıyor.");
                //    continue;
                //}

                //Mat enhancedPlate = ImageEnhancementHelper.ApplyPlateSpecificEnhancement1(plate);

                //plaka classifi
                Mat mat = plate.Clone();
                //Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

                Cv2.Resize(plate, plate, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

                //Mat enhancedPlate = ImagePreProcessingHelper.ApplyCLAHEToColor(mat);
                //Mat dee = ImagePreProcessingHelper.ApplyGammaCorrection(enhancedPlate, 0.7);

                int isPlateRegion = SVMHelper.AskSVMPredictionForPlateRegion(MainForm.m_mainForm.m_loadedSvmForPlateRegion, plate);

                if (isPlateRegion == 0)
                {
                    ////Debug.WriteLine("Aspect Ratio : " + aspectRatio.ToString());
                    //string guid = System.Guid.NewGuid().ToString();
                    //mat.SaveImage("D:\\Arge Projeler\\PlateRecognation - Kopya\\PlateRecognation\\bin\\Debug\\net8.0-windows\\Plates1\\" + guid + ".jpg");


                    //Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(image);
                    //Mat binaryImage = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(image);

                    //bool fr = ValidationHelper.IsValidPlateDeneme(bbox, grayImage, binaryImage);

                    candidatePlates.Add(bbox);  // Bu dikdörtgeni eklenenler listesine ekle

                    value.Add(new PossiblePlate
                    {
                        possiblePlateRegions = plate,
                        addedRects = bbox
                    });


                }

            }

            return value;
        }
    }


   

}
