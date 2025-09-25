using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class PlateTrackingWithOpticalFlowStrategy1 : IPlateReadingStrategy
    {
        private bool m_isRunning = false;

        private CameraChannel m_cameraChannel;
        private CancellationTokenSource m_cts;
        private Task m_processTask;
        private Task m_trackingTask;

        private Mat m_prevGray;
        private Point2f[] m_prevPoints;

        private List<TrackedPlate> m_trackedPlates = new List<TrackedPlate>();

        private int m_nextPlateId = 0;

        private List<bool> m_framePattern;
        private int m_frameIndex = 0;

        private BlockingCollection<FrameWithROIRect> m_frameQueue;
        private BlockingCollection<PossiblePlate> m_plateQueue;

        private bool m_autoLight;
        private bool m_autoWhite;
        private int m_cameraId;

        private IOCRImageAnalyzer m_ocrAnalyzer;
        private OCRWorker m_ocrWorker;
        private OCRResultAggregator m_ocrAggregator;

        private const int FallbackInterval = 20; // her 20 karede bir fallback detection

        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;

        public void Configure(CameraConfiguration config, IOCRImageAnalyzer analyzer)
        {
            m_cameraId = config.Id;
            m_autoLight = config.AutoLightControl;
            m_autoWhite = config.AutoWhiteBalance;
            m_framePattern = config.FramePattern;

            m_frameQueue = new BlockingCollection<FrameWithROIRect>(boundedCapacity: 5);
            m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);

            m_ocrAnalyzer = analyzer;

            m_ocrAggregator = new OCRResultAggregator(m_cameraId);
            m_ocrAggregator.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);
            m_ocrAggregator.PlateImageReady += (s, e) => PlateImageReady?.Invoke(this, e);

            m_ocrWorker = new OCRWorker(m_ocrAnalyzer, m_plateQueue, m_ocrAggregator);
        }

        public void SetCameraChannel(CameraChannel channel)
        {
            m_cameraChannel = channel;
            m_cameraChannel.Reader.OnFrameCaptured += Reader_OnFrameCaptured;
        }


        //private void Reader_OnFrameCaptured(Bitmap rawFrame)
        //{
        //    try
        //    {
        //        using var frame = rawFrame.ToMat();
        //        using var gray = new Mat();
        //        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //        bool motionDetected = IsMotionDetected(m_prevGray, gray, out Mat motionMask);

        //        if (motionDetected)
        //        {
        //            var motionPlates = DetectPlatesFromMovingRegions(frame, motionMask);
        //            AddNewTrackedPlates(motionPlates);
        //        }

        //        UpdateAllTrackedPlates(gray);

        //        if (m_frameIndex % FallbackInterval == 0)
        //        {
        //            var fallbackPlates = DetectPlatesFromWholeFrame(frame);
        //            AddNewTrackedPlates(fallbackPlates);
        //        }

        //        m_prevGray?.Dispose();
        //        m_prevGray = gray.Clone();

        //        m_prevPoints = Cv2.GoodFeaturesToTrack(
        //            gray, 100, 0.01, 5, null, 3, false, 0.04
        //        ) ?? Array.Empty<Point2f>();
        //    }
        //    catch (Exception ex)
        //    {
        //        //Debug.WriteLine($"[PlateTrackingWithOpticalFlow] Hata: {ex.Message}");
        //    }
        //    finally
        //    {
        //        m_frameIndex++;
        //    }
        //}
        private void Reader_OnFrameCaptured(Bitmap rawFrame)
        {
            try
            {
                using var frame = rawFrame.ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                // Hareket maskesi yeniden oluşturuluyor
                bool motionDetected = IsMotionDetected(m_prevGray, gray, out Mat motionMask);

                // Sadece hareket varsa kuyruğa ekle
                if (motionDetected)
                {
                    Rect[] motionRects = ExtractMotionRects(motionMask);

                    foreach (Rect rect in motionRects)
                    {


                        Mat roi = new Mat(frame, rect);

                        m_frameQueue.Add(new FrameWithROIRect { Frame = roi.Clone(), ROI = rect }); // Kuyrukta başka thread işleyecek
                    }
                }

                m_prevGray?.Dispose();
                m_prevGray = gray.Clone(); // Sonraki kare için sakla
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"[Reader_OnFrameCaptured] Hata: {ex.Message}");
            }
        }



        private double m_motionThreshold = 1.5;
        private int m_minMotionPixelCount = 200;
        private bool IsMotionDetected(Mat prevGray, Mat currGray, out Mat motionMask)
        {
            motionMask = new Mat();

            if (prevGray == null || prevGray.Empty() || currGray == null || currGray.Empty())
                return false;

            using var flow = new Mat();
            Cv2.CalcOpticalFlowFarneback(prevGray, currGray, flow,
                                          0.5, 3, 15, 3, 5, 1.2, OpticalFlowFlags.FarnebackGaussian);

            // Akışın büyüklüğünü hesapla
            Mat[] flowChannels = flow.Split(); // 2 kanal: x ve y
            using var magnitude = new Mat();
            Cv2.Magnitude(flowChannels[0], flowChannels[1], magnitude);

            // Hareket maskesini oluştur
            Cv2.Threshold(magnitude, motionMask, m_motionThreshold, 255, ThresholdTypes.Binary);
            motionMask.ConvertTo(motionMask, MatType.CV_8UC1); // Binary maskeye dönüştür

            //Cv2.Dilate(motionMask, motionMask, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)));

            //Cv2.ImShow("MotionMagnitude", motionMask ); // Görselleştir (normalize)
            //Cv2.WaitKey(1);

            int motionPixels = Cv2.CountNonZero(motionMask);

            return motionPixels > m_minMotionPixelCount;
        }

        //orjinal hali
        //private List<PossiblePlate> DetectPlatesFromMovingRegions(Mat frame, Mat motionMask)
        //{
        //    List<PossiblePlate> possiblePlates = new();

        //    // 1. Kontur bul
        //    OpenCvSharp.Point[][] contours;
        //    HierarchyIndex[] hierarchy;
        //    Cv2.FindContours(motionMask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        //    foreach (var contour in contours)
        //    {
        //        var rect = Cv2.BoundingRect(contour);

        //        // 2. Boyut ve oran filtresi uygula (plaka benzeri mi?)
        //        int minWidth = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinWidth;
        //        int maxWidth = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxWidth;
        //        int minHeight = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinHeight;
        //        int maxHeight = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxHeight;

        //        if (rect.Width < minWidth || rect.Width > maxWidth ||
        //            rect.Height < minHeight || rect.Height > maxHeight)
        //            continue;

        //        double aspectRatio = (double)rect.Width / rect.Height;
        //        if (aspectRatio < 2.0 || aspectRatio > 6.5)  // Plaka oranına yakın aralık
        //            continue;

        //        // 3. ROI kırp
        //        var roi = new Rect(
        //            Math.Max(rect.X, 0),
        //            Math.Max(rect.Y, 0),
        //            Math.Min(rect.Width, frame.Width - rect.X),
        //            Math.Min(rect.Height, frame.Height - rect.Y)
        //        );

        //        Mat plateRegion = new Mat(frame, roi).Clone();

        //        PossiblePlate plate = new PossiblePlate
        //        {
        //            addedRects = roi,
        //            possiblePlateRegions = plateRegion
        //        };

        //        possiblePlates.Add(plate);
        //    }

        //    return possiblePlates;
        //}

        private List<PossiblePlate> DetectPlatesFromMovingRegions(Mat frame, Mat motionMask)
        {
            List<PossiblePlate> osman = new List<PossiblePlate>();

            Rect[] motionRects = ExtractMotionRects(motionMask);

            foreach (Rect rect in motionRects)
            {
                Mat roi = new Mat(frame, rect);

                var plates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roi);

                osman.AddRange(plates);

            }





            return osman;
        }


        internal Rect[] ExtractMotionRects(Mat motionMask)
        {
            List<Rect> rects = new();

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(motionMask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);

                if (rect.Width < 5 || rect.Height < 5) // çok küçük bölgeleri filtrele
                    continue;

                rects.Add(rect);

                //Cv2.Rectangle(motionMask, rect, Scalar.Red,2);
                    
            }


            return rects.ToArray();
        }






        //orjinal
        private List<PossiblePlate> DetectPlatesFromWholeFrame(Mat frame)
        {
            List<PossiblePlate> possiblePlates = new();

            using var gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            //Opsiyonel: histogram eşitleme(kontrast artırma)
            Cv2.EqualizeHist(gray, gray);

            //Kenarları bul(Sobel veya Canny)
            using var edges = new Mat();
            Cv2.Canny(gray, edges, 100, 200);

            //Morfolojik işlemler(küçük parçaları birleştir)
            using var morph = new Mat();
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(17, 3));
            Cv2.MorphologyEx(edges, morph, MorphTypes.Close, kernel);

            //Konturları bul
            Cv2.FindContours(morph, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy,
                             RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);

                //Boyut ve oran filtresi(plaka benzeri mi?)
                int minWidth = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinWidth;
                int maxWidth = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxWidth;
                int minHeight = MainForm.m_mainForm.m_preProcessingSettings.m_plateMinHeight;
                int maxHeight = MainForm.m_mainForm.m_preProcessingSettings.m_plateMaxHeight;

                if (rect.Width < minWidth || rect.Width > maxWidth ||
                    rect.Height < minHeight || rect.Height > maxHeight)
                    continue;

                double aspectRatio = (double)rect.Width / rect.Height;
                if (aspectRatio < 2.0 || aspectRatio > 6.5)
                    continue;

                //ROI’yi al
                var roi = new Rect(
                    Math.Max(rect.X, 0),
                    Math.Max(rect.Y, 0),
                    Math.Min(rect.Width, frame.Width - rect.X),
                    Math.Min(rect.Height, frame.Height - rect.Y)
                );

                Mat plateRegion = new Mat(frame, roi).Clone();

                possiblePlates.Add(new PossiblePlate
                {
                    addedRects = roi,
                    possiblePlateRegions = plateRegion
                });
            }

            return possiblePlates;
        }

        //private List<PossiblePlate> DetectPlatesFromWholeFrame(Mat frame)
        //{
        //    using var gray = new Mat();
        //    Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //    Cv2.EqualizeHist(gray, gray);

        //    using var edges = new Mat();
        //    Cv2.Canny(gray, edges, 100, 200);

        //    using var morph = new Mat();
        //    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(17, 3));
        //    Cv2.MorphologyEx(edges, morph, MorphTypes.Close, kernel);

        //    // 1. Konturlar üzerinden ROI listesi çıkar
        //    Cv2.FindContours(morph, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy,
        //                     RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        //    List<Rect> roiCandidates = new();

        //    foreach (var contour in contours)
        //    {
        //        var rect = Cv2.BoundingRect(contour);

        //        if (rect.Width < 10 || rect.Height < 10)
        //            continue;

        //        roiCandidates.Add(rect);
        //    }

        //    // 2. SVM destekli filtrelemeyi uygula
        //    var plates = Plate.FindPlateCandidatesFromROI(roiCandidates.ToArray(), frame);

        //    // 3. (Opsiyonel) Frame clone ata
        //    foreach (var plate in plates)
        //    {
        //        plate.frame = frame.Clone();
        //    }

        //    return plates;
        //}






        private void AddNewTrackedPlates(List<PossiblePlate> plates)
        {
            foreach (var plate in plates)
            {
                if (plate == null || plate.possiblePlateRegions == null)
                    continue;

                var tracked = new TrackedPlate(
                    id: m_nextPlateId++,
                    bbox: plate.addedRects,
                    frameIndex: m_frameIndex, // güncel frame index
                    plateImage: plate.possiblePlateRegions,
                    frame: plate.frame
                );

                m_trackedPlates.Add(tracked);
            }
        }

        private void UpdateAllTrackedPlates(Mat currGray)
        {
            foreach (var plate in m_trackedPlates)
            {
                // Önceki görüntü gri değil, renkli
                if (plate.Frame == null || plate.Frame.Empty())
                    continue;

                using var prevGray = new Mat();
                Cv2.CvtColor(plate.Frame, prevGray, ColorConversionCodes.BGR2GRAY);

                Rect bbox = plate.BoundingBox;

                // ROI alanını kırp
                var prevROI = new Mat(prevGray, bbox);
                var currROI = new Mat(currGray, bbox);

                if (prevROI.Width <= 0 || prevROI.Height <= 0 ||
                    currROI.Width <= 0 || currROI.Height <= 0)
                    continue;

                // Optical flow hesapla (Dense)
                using var flow = new Mat();
                Cv2.CalcOpticalFlowFarneback(prevROI, currROI, flow,
                                             0.5, 3, 15, 3, 5, 1.2,
                                             OpticalFlowFlags.FarnebackGaussian);

                // Flow ortalamasını hesapla
                var flowChannels = flow.Split();
                Scalar avgX = Cv2.Mean(flowChannels[0]);
                Scalar avgY = Cv2.Mean(flowChannels[1]);

                // Yeni konumu hesapla
                int dx = (int)Math.Round(avgX.Val0);
                int dy = (int)Math.Round(avgY.Val0);
                Rect newBox = new Rect(bbox.X + dx, bbox.Y + dy, bbox.Width, bbox.Height);

                // Kare sınırında kalmasını sağla
                newBox.X = Math.Clamp(newBox.X, 0, currGray.Width - newBox.Width);
                newBox.Y = Math.Clamp(newBox.Y, 0, currGray.Height - newBox.Height);

                // Yeni plaka alanını kırp
                var newPlateROI = new Mat(currGray, newBox);
                Mat colorCurr = new Mat(); // Griyi tekrar BGR yap
                Cv2.CvtColor(currGray, colorCurr, ColorConversionCodes.GRAY2BGR);
                var newFrameCrop = new Mat(colorCurr, newBox);

                // Güncelle
                plate.Update(newBox, m_frameIndex, newPlateROI, newFrameCrop);
            }
        }

        //orj
        //private void ProcessFrames(CancellationToken token)
        //{
        //    while (!token.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            if (!m_frameQueue.TryTake(out Mat frame, TimeSpan.FromMilliseconds(100)))
        //                continue;

        //            using (frame)
        //            {
        //                using var gray = new Mat();
        //                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //                bool motionDetected = IsMotionDetected(m_prevGray, gray, out Mat motionMask);

        //                if (motionDetected)
        //                {
        //                    var motionPlates = DetectPlatesFromMovingRegions(frame, motionMask);
        //                    AddNewTrackedPlates(motionPlates);
        //                    EnqueueForOCR(motionPlates);
        //                }

        //                UpdateAllTrackedPlates(gray);

        //                if (m_frameIndex % FallbackInterval == 0)
        //                {
        //                    var fallbackPlates = DetectPlatesFromWholeFrame(frame);
        //                    AddNewTrackedPlates(fallbackPlates);
        //                    EnqueueForOCR(fallbackPlates);
        //                }

        //                m_prevGray?.Dispose();
        //                m_prevGray = gray.Clone();

        //                m_frameIndex++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            //Debug.WriteLine($"[PlateTrackingWithOpticalFlowStrategy::Process] Hata: {ex.Message}");
        //        }
        //    }

        //    //Debug.WriteLine("[PlateTrackingWithOpticalFlowStrategy] Process döngüsü durdu.");
        //}


        private void ProcessFrames(CancellationToken token)
        {
            //foreach (var item in m_frameQueue.GetConsumingEnumerable(token))
            //{
            //    try
            //    {
            //        using var roi = item.Frame;
            //        Rect roiRect = item.ROI;

            //        ROI'de plaka tespiti yap
            //        var localPlates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roi);

            //        ROI içinde tespit edilen plaka koordinatlarını global frame'e göre düzelt
            //        foreach (var plate in localPlates)
            //        {
            //            plate.addedRects = new Rect(
            //                plate.addedRects.X + roiRect.X,
            //                plate.addedRects.Y + roiRect.Y,
            //                plate.addedRects.Width,
            //                plate.addedRects.Height
            //            );


            //            burada plaka kuyruğuna eklememeliyiz bence biraz takip ettikten sonra ocr kuyruğuna eklemeleyiz?
            //            m_plateQueue.Add(plate);  // OCR kuyruğuna ekle
            //        }

            //        AddNewTrackedPlates(localPlates);

            //        ROI'nin gri versiyonunu çıkar (takip için)
            //        using var gray = new Mat();
            //        Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
            //        UpdateAllTrackedPlates(gray);
            //    }
            //    catch (Exception ex)
            //    {
            //        //Debug.WriteLine($"[ProcessFrames] Hata: {ex.Message}");
            //    }

            //}

            foreach (var item in m_frameQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    using var roi = item.Frame;
                    Rect roiRect = item.ROI;

                    //ROI'de plaka tespiti yap
                    var localPlates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roi);

                    //ROI içinde tespit edilen plaka koordinatlarını global frame'e göre düzelt
                    foreach (var plate in localPlates)
                    {
                        plate.addedRects = new Rect(
                            plate.addedRects.X + roiRect.X,
                            plate.addedRects.Y + roiRect.Y,
                            plate.addedRects.Width,
                            plate.addedRects.Height
                        );


                        //burada plaka kuyruğuna eklememeliyiz bence biraz takip ettikten sonra ocr kuyruğuna eklemeleyiz?
                        m_plateQueue.Add(plate);  // OCR kuyruğuna ekle
                    }

                    AddNewTrackedPlates(localPlates);

                    //ROI'nin gri versiyonunu çıkar (takip için)
                    using var gray = new Mat();
                    Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
                    UpdateAllTrackedPlates(gray);
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"[ProcessFrames] Hata: {ex.Message}");
                }

            }
        }



        //    while (!token.IsCancellationRequested)
        //{
        //    if (!m_frameQueue.TryTake(out var frame, 100))
        //        continue;

        //    try
        //    {


        //// Hareket maskesi yeniden oluşturuluyor
        //bool motionDetected = IsMotionDetected(m_prevGray, gray, out Mat motionMask);

        //if (motionDetected)
        //{
        //var motionPlates = DetectPlatesFromMovingRegions(frame, motionMask);





        //}

        //UpdateAllTrackedPlates(gray);

        //if (m_frameIndex % FallbackInterval == 0)
        //{
        //    var fallbackPlates = DetectPlatesFromWholeFrame(frame);
        //    AddNewTrackedPlates(fallbackPlates);


        //    foreach (var plate in fallbackPlates)
        //        m_plateQueue.Add(plate);  // OCR kuyruğuna ekle
        //}

        //m_prevGray?.Dispose();
        //m_prevGray = gray.Clone();

        //m_prevPoints = Cv2.GoodFeaturesToTrack(gray, 100, 0.01, 5, null, 3, false, 0.04)
        //                ?? Array.Empty<Point2f>();

        //m_frameIndex++;
        //}
        //catch (Exception ex)
        //{
        //    //Debug.WriteLine($"[ProcessFrames] Hata: {ex.Message}");
        //}
        //finally
        //{
        //    frame.Dispose();
        //}
        //}




        private void EnqueueForOCR(List<PossiblePlate> plates)
        {
            foreach (var plate in plates)
            {
                if (plate != null && plate.possiblePlateRegions != null)
                {
                    m_plateQueue.Add(plate);
                }
            }
        }

        public void Start()
        {
            if (m_isRunning)
                return;

            m_cts = new CancellationTokenSource();
            m_isRunning = true;

            // 2. Frame işleyici (motion & takip & fallback)
            m_processTask = Task.Factory.StartNew(() =>
                ProcessFrames(m_cts.Token), TaskCreationOptions.LongRunning);


            // 1. OCR iş parçacığını başlat
            m_trackingTask = Task.Factory.StartNew(() =>
                m_ocrWorker.Start(m_cts.Token), TaskCreationOptions.LongRunning);

         

        }

        public void Stop()
        {
            if (!m_isRunning)
                return;

            m_cts.Cancel();

            try
            {
                m_processTask?.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                    Debug.WriteLine($"[Stop - ProcessTask] {inner.Message}");
            }

            m_ocrWorker.Stop();
            m_isRunning = false;
        }

        public void Dispose()
        {
            Stop();
            m_frameQueue?.Dispose();
            m_cts?.Dispose();
        }
    }
}
