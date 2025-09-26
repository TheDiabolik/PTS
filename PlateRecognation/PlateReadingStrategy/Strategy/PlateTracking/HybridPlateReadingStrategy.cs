using Accord.Imaging.Filters;
using Accord.Statistics;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal partial class HybridPlateReadingStrategy : IPlateReadingStrategy
    {

        public int m_OcrSamplesCap = 3;

        int m_staleTtl = 10;
        int m_ocrTtl = 10;
        int m_warmupMinAge = 5;


        Mat prevGray = null;

        #region takipiçin
        private readonly ThreadSafeList<SimpleTracker> _tracked = new();
        private int _nextId = 0;

        // Eşikler
        private readonly int _needPasses = 3;   // 3 kare ardışık
        private readonly int _maxMisses = 2;    // 2 kez kaçırırsa bırak
        private readonly double _svmTrackThr = 0.5;

        // OF için prev/curr full gray ve son BGR
        private Mat _prevGrayFull;
        private Mat _lastBgrFull;
        #endregion


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

            //m_framewithROIQueue = new BlockingCollection<FrameWithRoi>(boundedCapacity: 5);
            m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);

            m_ocrAnalyzer = analyzer;

            m_ocrAggregator = new OCRResultAggregator(_cameraId, 1, 1);

            //m_ocrAggregator = new OCRResultAggregator(_cameraId);

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

                    //gpt önerisi
                    using (var kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(7, 3)))
                        Cv2.MorphologyEx(diff, diff, MorphTypes.Close, kClose);
                    using (var kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 3)))
                        Cv2.MorphologyEx(diff, diff, MorphTypes.Open, kOpen);


                    int motionPixels = Cv2.CountNonZero(diff);
                    double motionRatio = (double)motionPixels / Math.Max(1.0, gray.Total());

                    bool shouldProcess = motionRatio > 0.02;

                    if (shouldProcess)
                    {
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

                        //Cv2.ImShow("loo", diff);

                        //Cv2.WaitKey(1);

                        motionROI = RectComparisonHelper.MergeRectsByProximity(motionROI, maxGapX: 12, minVertOverlap: 0.4);

                        using (Mat processImage = m_cameraChannel.NewProcessFrame(frame, m_AutoLightControl, m_AutoWhiteBalance))
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

                //m_frameIndex = (m_frameIndex + 1) % m_framePattern.Count;

                //m_frameIndex++;
            }
        }

        private void ProcessFrames(CancellationToken token)
        {
            foreach (FrameWithRoi frameWithRoi in m_framewithROIQueue.GetConsumingEnumerable(token))
            {
                int frameIdx = System.Threading.Interlocked.Increment(ref m_frameIndex);

                // 1) BGR/GRAY hazırla
                Mat currBgr = frameWithRoi.Frame;
                using var currGrayFull = new Mat();
                Cv2.CvtColor(currBgr, currGrayFull, ColorConversionCodes.BGR2GRAY);

                // 2) Detection & association (seed / emme)
                DetectAndAssociate(frameWithRoi, currBgr, currGrayFull, frameIdx);

                // 3) Takip (prev varsa)
                UpdateTrackers(_prevGrayFull, currGrayFull, _tracked, frameIdx);

                // 4) Prune
                PruneTrackers(_tracked, frameIdx, m_staleTtl, m_ocrTtl, m_warmupMinAge);

                // 5) OCR değerlendirme + kuyruk
                EvaluateTrackersForOcr(currGrayFull, currBgr, frameIdx);

                // 6) Frame sonu: bayrak temizliği (hit olmayanların detection bayrağını indir)
                ResetDetectionFlags(_tracked, frameIdx);

                // 7) prevGray devir
                _prevGrayFull?.Dispose();
                _prevGrayFull = currGrayFull.Clone();
                //}
                //catch (Exception ex)
                //{
                //    //Debug.WriteLine($"[ProcessFrames] Hata: {ex.Message}");
                //}
                //finally
                //{
                //    // FrameWithRoi IDisposable ise burada bırak
                //    // frameWithRoi?.Dispose();

                //    // currBgr'a sahiplik sizdeyse ve tekrar kullanılmayacaksa:
                //    // currBgr?.Dispose();
                //}
            }
        }






       
    }
}
