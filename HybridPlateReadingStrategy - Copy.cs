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
    //takip mekanizması eklenmeden temel harekete göre çalışan versiyon
    internal class HybridPlateReadingStrategyOldWorkingVersion : IPlateReadingStrategy
    {
        private int _cameraId;

        private CancellationTokenSource m_cts;

        internal volatile int currentFramePos = 0; // Videodaki mevcut pozisyonu sakla


        private string m_videoSource;

        //private Task m_readTask;
        private Task m_processTask;
        private Task m_OCRTask;

        internal volatile bool m_stream;
        volatile bool m_isProcessingFrames;

        // 5 frame'lik örüntü: [al, al, alma, alma, alma]
        internal List<bool> m_framePattern;
        internal int m_framePatternEffectiveLength; // burada: 3

        internal BlockingCollection<FrameWithRoi> m_framewithROIQueue;


        IOCRImageAnalyzer m_ocrAnalyzer;
        private OCRWorker m_ocrWorker;
        private OCRResultAggregator m_ocrAggregator;
        internal BlockingCollection<PossiblePlate> m_plateQueue;

        private Action<string, int> m_onPlateResultReady;

        private int m_frameIndex = 0;


        CameraChannel m_cameraChannel;


        bool m_AutoLightControl, m_AutoWhiteBalance, m_isRunning;


        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;

        public void Configure(CameraConfiguration cameraConfiguration, IOCRImageAnalyzer analyzer)
        {
            _cameraId = cameraConfiguration.Id;
            m_AutoLightControl = cameraConfiguration.AutoLightControl;
            m_AutoWhiteBalance = cameraConfiguration.AutoWhiteBalance;

            m_framePattern = cameraConfiguration.FramePattern;
            m_framePatternEffectiveLength = cameraConfiguration.FramePattern.Count(x => x);

            m_framewithROIQueue = new BlockingCollection<FrameWithRoi>(boundedCapacity: m_framePatternEffectiveLength);
            m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);

            m_ocrAnalyzer = analyzer;

            m_ocrAggregator = new OCRResultAggregator(_cameraId);
            m_ocrAggregator.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);
            m_ocrAggregator.PlateImageReady += (s, e) => PlateImageReady?.Invoke(this, e);

            m_ocrWorker = new OCRWorker(m_ocrAnalyzer, m_plateQueue, m_ocrAggregator);
        }

        public void SetCameraChannel(CameraChannel cameraChannel)
        {
            m_cameraChannel = cameraChannel;

            m_cameraChannel.Reader.OnFrameCaptured += Reader_OnFrameCaptured;
        }

        public void Start()
        {
            if (m_isRunning)
                return; // veya önce Stop() çağırabilirsin

            m_cts = new CancellationTokenSource();
            m_isRunning = true;


            m_processTask = Task.Factory.StartNew(() =>
                ProcessFrames(m_cts.Token), TaskCreationOptions.LongRunning);

            m_OCRTask = Task.Factory.StartNew(() =>
     m_ocrWorker.Start(m_cts.Token), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (!m_isRunning)
                return;

            if (m_cts != null)
            {
                m_cts.Cancel();

                m_ocrWorker.Stop();

                try
                {
                    Task.WaitAll(new[] { m_processTask, m_OCRTask }, 3000);
                }
                catch (AggregateException) { /* task hatası olabilir, yutulabilir */ }

                m_cts.Dispose();
                m_cts = null;
            }

            m_isRunning = false;
        }


        Mat prevGray = null;
        private void Reader_OnFrameCaptured(Bitmap rawFrame)
        {
            Mat gray = null;
            Mat diff = null;


            try
            {
                Mat frame = rawFrame.ToMat();

                gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                if (prevGray != null)
                {
                    diff = new Mat();
                    Cv2.Absdiff(prevGray, gray, diff);
                    Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

                  


                    int motionPixels = Cv2.CountNonZero(diff);
                    double motionRatio = (double)motionPixels / (gray.Rows * gray.Cols);

                    bool shouldProcess = motionRatio > 0.02 && m_framePattern[m_frameIndex % m_framePattern.Count];

                    if (shouldProcess)
                    {
                        //gpt önerisi
                        using (var kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 3)))
                            Cv2.MorphologyEx(diff, diff, MorphTypes.Close, kClose);
                        using (var kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                            Cv2.MorphologyEx(diff, diff, MorphTypes.Open, kOpen);

                        //morfolojik operasyonlar

                        Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                       
                        
                        List<OpenCvSharp.Rect> motionROI = new List<OpenCvSharp.Rect>();

                        foreach (var contour in contours)
                        {
                            Rect motionRect = Cv2.BoundingRect(contour);

                            if (motionRect.Width < 40 || motionRect.Height < 12)
                                continue;

                            motionROI.Add(motionRect);
                        }

                        motionROI = MergeRectsByProximity(motionROI, maxGapX: 12, minVertOverlap: 0.4);


                        using (Mat processImage = m_cameraChannel.NewProcessFrame(
                           frame,
                           m_AutoLightControl,
                           m_AutoWhiteBalance))
                        {
                            {
                                var item = new FrameWithRoi
                                {
                                    Frame = processImage.Clone(),     // kuyruk sahiplenir
                                    Rects = motionROI,
                                };

                                if (!m_framewithROIQueue.TryAdd(item))
                                    item.Dispose(); // kuyruk doluysa leak olmasın
                            }

                        }


                    }
                }

            }
            catch (Exception ex)
            {
                // Log optional
                Console.WriteLine($"Frame process failed: {ex.Message}");
            }
            finally
            {
                // prevGray güncellemesi
                prevGray?.Dispose();
                prevGray = gray.Clone();

                gray?.Dispose();
                diff?.Dispose();

                m_frameIndex = (m_frameIndex + 1) % m_framePattern.Count;
            }
        }

        private void ProcessFrames(CancellationToken token)
        {
            foreach (var frame in m_framewithROIQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    using (frame)
                    {

                        //ahmet yazdı yazdı mı yazar he!
                        //foreach (var r in frame.Rects)
                        //{
                        //    Cv2.Rectangle(frame.Frame, r, Scalar.Red, 2);


                        //}

                        ////Cv2.ImShow("MotionMask", frame.Frame);
                        ////Cv2.WaitKey(1);
                        ///


                        foreach (var r in frame.Rects)
                        {
                            Cv2.Rectangle(frame.Frame, r, Scalar.Red, 2);

                            Mat roiMAt = new Mat(frame.Frame, r);

                            var plates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roiMAt);

                            if (plates?.Any() == true)
                            {
                                var bestPlate = plates
                                    .Where(p => p != null && p.addedRects.Width > 0 && p.addedRects.Height > 0)
                                    .OrderByDescending(p => p.PlateScore)
                                    .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                                    .FirstOrDefault();

                                if (bestPlate != null)
                                {
                                    bestPlate.frame = roiMAt.Clone();

                                    if (!m_plateQueue.TryAdd(bestPlate))
                                        Debug.WriteLine("Plaka kuyruğuna ekleme başarısız.");
                                }
                                else
                                {
                                    //Debug.WriteLine("Geçerli plaka adayı bulunamadı.");
                                }
                            }

                        }

                        Cv2.ImShow("MotionMask", frame.Frame);
                        Cv2.WaitKey(1);




                    }


                    //var plates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(frame);

                    //if (plates?.Any() == true)
                    //{
                    //    var bestPlate = plates
                    //        .Where(p => p != null && p.addedRects.Width > 0 && p.addedRects.Height > 0)
                    //        .OrderByDescending(p => p.PlateScore)
                    //        .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                    //        .FirstOrDefault();

                    //    if (bestPlate != null)
                    //    {
                    //        bestPlate.frame = frame.Clone();

                    //        if (!m_plateQueue.TryAdd(bestPlate))
                    //            //Debug.WriteLine("Plaka kuyruğuna ekleme başarısız.");
                    //    }
                    //    else
                    //    {
                    //        //Debug.WriteLine("Geçerli plaka adayı bulunamadı.");
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"Plaka analiz hatası: {ex.Message}");
                }
                finally
                {
                    frame.Dispose();
                }
            }
        }



        private static List<Rect> MergeRectsByProximity(List<Rect> rects, int maxGapX, double minVertOverlap)
        {
            if (rects.Count <= 1) return new List<Rect>(rects);

            // soldan sağa sırala
            rects = rects.OrderBy(r => r.X).ToList();

            var merged = new List<Rect>();
            Rect cur = rects[0];

            for (int i = 1; i < rects.Count; i++)
            {
                var nxt = rects[i];

                bool horizClose = nxt.X <= cur.X + cur.Width + maxGapX; // yatayda temas/boşluk küçük
                double vOverlap = VerticalOverlapRatio(cur, nxt);

                if (horizClose && vOverlap >= minVertOverlap)
                {
                    // Birleştir (union)
                    cur = Union(cur, nxt);
                }
                else
                {
                    merged.Add(cur);
                    cur = nxt;
                }
            }
            merged.Add(cur);
            return merged;
        }

        private static double VerticalOverlapRatio(Rect a, Rect b)
        {
            int top = Math.Max(a.Y, b.Y);
            int bottom = Math.Min(a.Y + a.Height, b.Y + b.Height);
            int overlap = Math.Max(0, bottom - top);
            int minH = Math.Max(1, Math.Min(a.Height, b.Height));
            return (double)overlap / minH; // [0..1]
        }

        private static Rect Union(Rect a, Rect b)
        {
            int x1 = Math.Min(a.X, b.X);
            int y1 = Math.Min(a.Y, b.Y);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }

    }
}
