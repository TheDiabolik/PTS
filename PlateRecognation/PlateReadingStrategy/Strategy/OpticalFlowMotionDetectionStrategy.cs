using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PlateRecognation
{
    public class OpticalFlowMotionDetectionStrategy : IPlateReadingStrategy
    {
        private bool m_isRunning = false;

        private int _cameraId;

        private CancellationTokenSource m_cts;

        private Task m_processTask;
        private Task m_OCRTask;

        private CameraChannel m_cameraChannel;

        private Mat _prevGray;
        private Point2f[] _prevPoints;

        private List<bool> m_framePattern;

        internal int m_framePatternEffectiveLength;

        private int m_frameIndex = 0;

        private BlockingCollection<Mat> m_frameQueue;

        private bool m_autoLight, m_autoWhite;

        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;

        private const int OpticalFlowMovementThreshold = 10;
        private const double MinOpticalFlowDistance = 2.0;


        IOCRImageAnalyzer m_ocrAnalyzer;
        private OCRWorker m_ocrWorker;
        private OCRResultAggregator m_ocrAggregator;
        internal BlockingCollection<PossiblePlate> m_plateQueue;

        bool m_AutoLightControl, m_AutoWhiteBalance;


        public void Configure(CameraConfiguration cameraConfiguration, IOCRImageAnalyzer analyzer)
        {
            _cameraId = cameraConfiguration.Id;
            m_AutoLightControl = cameraConfiguration.AutoLightControl;
            m_AutoWhiteBalance = cameraConfiguration.AutoWhiteBalance;


            m_framePattern = cameraConfiguration.FramePattern;
            m_autoLight = cameraConfiguration.AutoLightControl;
            m_autoWhite = cameraConfiguration.AutoWhiteBalance;

            m_framePatternEffectiveLength = cameraConfiguration.FramePattern.Count(x => x);

            m_frameQueue = new BlockingCollection<Mat>(boundedCapacity: m_framePatternEffectiveLength);
            m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);

            m_ocrAnalyzer = analyzer;

            m_ocrAggregator = new OCRResultAggregator(_cameraId);
            m_ocrAggregator.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);
            m_ocrAggregator.PlateImageReady += (s, e) => PlateImageReady?.Invoke(this, e);

            m_ocrWorker = new OCRWorker(m_ocrAnalyzer, m_plateQueue, m_ocrAggregator);
        }

        public void SetCameraChannel(CameraChannel channel)
        {
            m_cameraChannel = channel;
            m_cameraChannel.Reader.OnFrameCaptured += Reader_OnFrameCaptured;
        }

        private void Reader_OnFrameCaptured(Bitmap rawFrame)
        {
            try
            {
                using var frame = rawFrame.ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                bool shouldProcess = false;

                if (_prevGray != null && _prevPoints != null && _prevPoints.Length > 0)
                {
                    // _prevPoints dizisini Mat'e çevir
                    using var prevPtsMat = new Mat(_prevPoints.Length, 1, MatType.CV_32FC2);

                    for (int i = 0; i < _prevPoints.Length; i++)
                    {
                        prevPtsMat.Set(i, 0, _prevPoints[i]);
                    }

                    // Optical Flow çıktıları
                    using var nextPtsOutput = new Mat();
                    using var statusOutput = new Mat();
                    using var errOutput = new Mat();

                    Cv2.CalcOpticalFlowPyrLK(
                        _prevGray,
                        gray,
                        prevPtsMat,
                        nextPtsOutput,
                        statusOutput,
                        errOutput,
                        new OpenCvSharp.Size(21, 21),
                        3,
                        new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 30, 0.01),
                        OpticalFlowFlags.None,
                        1e-4
                    );

                    // Dönüşümler
                    Point2f[] nextPts = new Point2f[nextPtsOutput.Rows];
                    for (int i = 0; i < nextPtsOutput.Rows; i++)
                        nextPts[i] = nextPtsOutput.At<Point2f>(i);

                    byte[] status = new byte[statusOutput.Rows];
                    for (int i = 0; i < statusOutput.Rows; i++)
                        status[i] = statusOutput.At<byte>(i);

                    int movedCount = 0;
                    for (int i = 0; i < status.Length; i++)
                    {
                        if (status[i] == 1)
                        {
                            double dx = nextPts[i].X - _prevPoints[i].X;
                            double dy = nextPts[i].Y - _prevPoints[i].Y;
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance > MinOpticalFlowDistance)
                                movedCount++;
                        }
                    }

                    if (movedCount >= OpticalFlowMovementThreshold && m_framePattern[m_frameIndex % m_framePattern.Count])
                    {
                        shouldProcess = true;
                    }
                }

                if (shouldProcess)
                {
                    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, frame.ToBitmap());


                    using var processed = m_cameraChannel.NewProcessFrame(frame, m_autoLight, m_autoWhite);
                    m_frameQueue.TryAdd(processed.Clone()); // Clone et, bağımsız yaşasın
                }

                // Güncelleme
                var newGray = gray.Clone();
                _prevGray?.Dispose();
                _prevGray = newGray;

                _prevPoints = Cv2.GoodFeaturesToTrack(
                    src: newGray,
                    maxCorners: 100,
                    qualityLevel: 0.01,
                    minDistance: 5,
                    mask: null,
                    blockSize: 3,
                    useHarrisDetector: false,
                    k: 0.04
                ) ?? Array.Empty<Point2f>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpticalFlowMotionDetection] Hata: {ex.Message}");
            }
            finally
            {
                m_frameIndex++;
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

            //m_OCRTask = Task.Factory.StartNew(() =>
            //    OcrPlatesFromQueue(m_cts.Token), TaskCreationOptions.LongRunning);

            m_OCRTask = Task.Factory.StartNew(() =>
     m_ocrWorker.Start(m_cts.Token), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _prevGray?.Dispose();

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
                    var plates = ImageAnalysisHelper.SobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(frame);

                    if (plates?.Any() == true)
                    {
                        var bestPlate = plates
                            .Where(p => p != null && p.addedRects.Width > 0 && p.addedRects.Height > 0)
                            .OrderByDescending(p => p.PlateScore)
                            .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                            .FirstOrDefault();

                        if (bestPlate != null)
                        {
                            var cloned = frame.Clone();
                            bestPlate.frame = cloned;

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
