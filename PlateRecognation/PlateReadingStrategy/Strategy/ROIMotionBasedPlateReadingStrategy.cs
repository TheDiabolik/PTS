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
    internal class MotionBasedMultiROIPlateStrategy : IPlateReadingStrategy
    {
        private int _cameraId;

        private CancellationTokenSource m_cts;

        internal volatile int currentFramePos = 0; // Videodaki mevcut pozisyonu sakla
        Mat prevGray = null;

        private string m_videoSource;

        //private Task m_readTask;
        private Task m_processTask;
        private Task m_OCRTask;

        internal volatile bool m_stream;
        volatile bool m_isProcessingFrames;

        // 5 frame'lik örüntü: [al, al, alma, alma, alma]
        internal List<bool> m_framePattern;
        internal int m_framePatternEffectiveLength; // burada: 3

        internal BlockingCollection<Mat> m_frameQueue;



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


        public void SetCameraChannel(CameraChannel cameraChannel)
        {
            m_cameraChannel = cameraChannel;

            m_cameraChannel.Reader.OnFrameCaptured += Reader_OnFrameCaptured;

        }

        public void Configure(CameraConfiguration cameraConfiguration, IOCRImageAnalyzer analyzer)
        {
            _cameraId = cameraConfiguration.Id;
            m_AutoLightControl = cameraConfiguration.AutoLightControl;
            m_AutoWhiteBalance = cameraConfiguration.AutoWhiteBalance;

            m_framePattern = cameraConfiguration.FramePattern;
            m_framePatternEffectiveLength = cameraConfiguration.FramePattern.Count(x => x);

            //m_frameQueue = new BlockingCollection<Mat>(boundedCapacity: m_framePatternEffectiveLength);

            m_frameQueue = new BlockingCollection<Mat>();
            m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);

            m_ocrAnalyzer = analyzer;


            //m_continuousOCRImage.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);

            m_ocrAggregator = new OCRResultAggregator(_cameraId);
            m_ocrAggregator.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);
            m_ocrAggregator.PlateImageReady += (s, e) => PlateImageReady?.Invoke(this, e);

            m_ocrWorker = new OCRWorker(m_ocrAnalyzer, m_plateQueue, m_ocrAggregator);

        }
        
        private void Reader_OnFrameCaptured(Bitmap rawFrame)
        {
            Mat gray = null;
            Mat diff = null;

            try
            {
                //if (!m_framePattern[m_frameIndex])
                //    return;

                //using var matFrame = BitmapConverter.ToMat(rawFrame);
                //using var frameClone = matFrame.Clone(); // her zaman clone'u iyileştirme için kullan

                //using var enhancedFrame = m_cameraChannel.NewProcessFrame(frameClone, m_AutoLightControl, m_AutoWhiteBalance);

                //// Eğer kuyruk doluysa, discard edilecek — clone zaten using bloğunda, leak olmaz
                //m_frameQueue.TryAdd(enhancedFrame.Clone());  // orijinali değil, clone ver

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

                    //bool shouldProcess = motionRatio > 0.03;


                    if (shouldProcess)
                    {
                        Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                        using (Mat processImage = m_cameraChannel.NewProcessFrame(frame, m_AutoLightControl, m_AutoWhiteBalance))
                        {

                            foreach (var contour in contours)
                            {
                                Rect rect = Cv2.BoundingRect(contour);

                                if (rect.Width < 60 || rect.Height < 20)
                                    continue;


                                rect = rect.Intersect(new Rect(0, 0, frame.Width, frame.Height));

                                using Mat roi = new Mat(processImage, rect);


                                //Cv2.ImShow("MotionMagnitude", roi); // Görselleştir (normalize)
                                //Cv2.WaitKey(1);

                                m_frameQueue.TryAdd(roi.Clone()); // önemli: clone et
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

        private void ProcessFrames(CancellationToken token)
        {
            foreach (var frame in m_frameQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    var plates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(frame);

                    if (plates?.Any() == true)
                    {
                        var bestPlate = plates
                            .Where(p => p != null && p.addedRects.Width > 0 && p.addedRects.Height > 0)
                            .OrderByDescending(p => p.PlateScore)
                            .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                            .FirstOrDefault();

                        if (bestPlate != null)
                        {
                            bestPlate.frame = frame.Clone();

                            if (!m_plateQueue.TryAdd(bestPlate))
                                Debug.WriteLine("Plaka kuyruğuna ekleme başarısız.");
                        }
                        else
                        {
                            //Debug.WriteLine("Geçerli plaka adayı bulunamadı.");
                        }
                    }
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



    }
}
