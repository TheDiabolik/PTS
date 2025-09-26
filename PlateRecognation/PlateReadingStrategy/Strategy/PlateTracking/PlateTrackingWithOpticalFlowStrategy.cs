using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace PlateRecognation
{
    internal class PlateTrackingWithOpticalFlowStrategy : IPlateReadingStrategy, IDisposable
    {
        private bool m_isRunning = false;

        private CameraChannel m_cameraChannel;
        private CancellationTokenSource m_cts;
        private Task m_motionTask;
        private Task m_processTask;
        private Task m_trackingTask;

        // ---- Kuyruklar ----
        // Raw frame kuyruğu (latest-wins ile tüketilecek)
        private BlockingCollection<Mat> m_rawFrameQueue;
        // Motion sonrası ROI kuyruğu (ProcessFrames tüketir)
        private BlockingCollection<FrameWithROIRect> m_frameQueue;
        // OCR kuyruğu
        private BlockingCollection<PossiblePlate> m_plateQueue;


        // ---- OCR/aggregator ----
        private IOCRImageAnalyzer m_ocrAnalyzer;
        private OCRWorker m_ocrWorker;
        private OCRResultAggregator m_ocrAggregator;

        // ---- Tracking ----
        private readonly object m_trackedLock = new();
        private List<TrackedPlate> m_trackedPlates = new();
        private int m_nextPlateId = 0;

        // ---- Parametreler / durum ----
        private bool m_autoLight;
        private bool m_autoWhite;
        private int m_cameraId;

        private List<bool> m_framePattern;
        private int m_patternIdx = 0;
        private int m_capturePatternIdx = 0;

        //ahmet
        private int m_frameIndex = 0;


        private const int FallbackInterval = 20; // her 20 karede bir fallback detection

        // ---- Motion/Farneback parametreleri (640x480 referansı) ----
        private int m_flowEveryK = 2;       // Farneback her k karede
        private int m_absDiffGate = 1200;   // küçük farkta flow'u atla
        private double m_motionThreshold = 1.5; // mag eşiği (piksel)
        private int m_minMotionPixelCount = 200;

        // ROI çıkarım filtreleri
        private int m_minContourArea = 150;
        private int m_maxRoiCountForRoiMode = 6;
        private int m_minRoiWidth = 40, m_minRoiHeight = 12;
        private double m_minRoiAspect = 1.6, m_maxRoiAspect = 7.0;


        // ---- Reusable buffer'lar (GC baskısını azalt) ----
        private Mat m_flow = new();
        private Mat m_mag2 = new();
        private Mat m_workMask = new();
        private Mat m_kernel3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));

        // ---- Olaylar ----
        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;

        public void Configure(CameraConfiguration config, IOCRImageAnalyzer analyzer)
        {
            m_cameraId = config.Id;
            m_autoLight = config.AutoLightControl;
            m_autoWhite = config.AutoWhiteBalance;


            m_framePattern = (config.FramePattern != null && config.FramePattern.Count > 0)
                ? config.FramePattern
                : new List<bool> { true, false }; // default pattern


            //m_rawFrameQueue = (config.FramePattern != null && config.FramePattern.Count > 0)
            //   ? config.FramePattern
            //   : new List<bool> { true, false }; // default pattern

            // Kuyruklar (bounded: backpressure)
            m_rawFrameQueue = new BlockingCollection<Mat>();
            m_frameQueue = new BlockingCollection<FrameWithROIRect>();
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



        private void Reader_OnFrameCapturedOld(Bitmap rawFrame)
        {
            try
            {
                //burada kaldım yarın buradan devam

                // 1) Pattern gating burada
                bool accept = (m_framePattern == null || m_framePattern.Count == 0)
                              ? true
                              : m_framePattern[m_capturePatternIdx];

                if (m_framePattern != null && m_framePattern.Count > 0)
                    m_capturePatternIdx = (m_capturePatternIdx + 1) % m_framePattern.Count;

                if (!accept)
                    return;


                //if (!m_framePattern[m_capturePatternIdx])
                //    return;


                using var bgr = rawFrame.ToMat();       // BGR
                var owned = bgr.Clone();                // sahipliği queue’ya ver
                if (!m_rawFrameQueue.TryAdd(owned))
                    owned.Dispose(); // latest-wins
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"[Reader_OnFrameCaptured] Hata: {ex.Message}");
            }

            //finally
            //{
            //    m_capturePatternIdx = (m_capturePatternIdx + 1) % m_framePattern.Count;
            //}
        }

        private void Reader_OnFrameCaptured(Bitmap rawFrame)
        {
            try
            {
                bool accept = (m_framePattern == null || m_framePattern.Count == 0)
                              ? true
                              : m_framePattern[m_capturePatternIdx];

                if (m_framePattern != null && m_framePattern.Count > 0)
                    m_capturePatternIdx = (m_capturePatternIdx + 1) % m_framePattern.Count;

                if (!accept) 
                    return;

                // Tek kopya üret (owned) ve kuyruğa ver
                Mat owned = rawFrame.ToMat();

                if (!m_rawFrameQueue.TryAdd(owned))
                {
                    // latest-wins: en eskiyi atıp tekrar dene
                    if (m_rawFrameQueue.TryTake(out var old)) 
                        old.Dispose();
                    
                    if (!m_rawFrameQueue.TryAdd(owned)) 
                        owned.Dispose();
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"[Reader_OnFrameCaptured] Hata: {ex.Message}");
            }
        }

        private void MotionWorkerOld(CancellationToken token)
        {
            Mat prevGray = new Mat();
            int kCounter = 0;

            while (!token.IsCancellationRequested)
            {
                if (!m_rawFrameQueue.TryTake(out var bgr, 50))
                    continue;

                // latest-wins: kuyruğu boşalt
                while (m_rawFrameQueue.TryTake(out var extra))
                {
                    bgr.Dispose();
                    bgr = extra;
                }

                using (bgr)
                using (var gray = new Mat())
                {
                    Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                    if (prevGray.Empty()) { prevGray = gray.Clone(); continue; }

                    // absdiff gate
                    using (var diff = new Mat())
                    {
                        Cv2.Absdiff(prevGray, gray, diff);
                        Cv2.Threshold(diff, diff, 8, 255, ThresholdTypes.Binary);
                        if (Cv2.CountNonZero(diff) < m_absDiffGate)
                        { prevGray.Dispose(); prevGray = gray.Clone(); continue; }
                    }

                    // k-çiçek: Farneback
                    kCounter++;
                    if ((kCounter % m_flowEveryK) != 0)
                    { prevGray.Dispose(); prevGray = gray.Clone(); continue; }

                    if (IsMotionDetectedFast(prevGray, gray, out var mask))
                        using (mask)
                        {
                            var rects = ExtractMotionRects(mask);

                            //ahmet
                            //if (rects.Length == 0 || rects.Length > m_maxRoiCountForRoiMode)
                            //{
                            //    if ((m_capturePatternIdx % FallbackInterval) == 0)
                            //    {
                            //        var fallback = DetectPlatesFromWholeFrame(bgr);
                            //        AddNewTrackedPlates(fallback);
                            //        EnqueueForOCR(fallback);
                            //    }
                            //}
                            //else
                            //{
                                foreach (var r in rects)
                                {
                                    using var roi = new Mat(bgr, r);
                                    var owned = roi.Clone();
                                    if (!m_frameQueue.TryAdd(new FrameWithROIRect { Frame = owned, ROI = r }))
                                        owned.Dispose();
                                }
                            //}
                        }

                    prevGray.Dispose();
                    prevGray = gray.Clone();
                }

                Interlocked.Increment(ref m_capturePatternIdx);
            }

            prevGray.Dispose();
        }

        private int m_motionPatternIdx = 0;

        private void MotionWorkerOld1(CancellationToken token)
        {
            Mat prevGray = new Mat();
            int kCounter = 0;

            try
            {
                foreach (var first in m_rawFrameQueue.GetConsumingEnumerable(token))
                {
                    Mat bgr = first;

                    // latest-wins: kuyrukta ne varsa boşalt, en son gelenle çalış
                    while (m_rawFrameQueue.TryTake(out var extra))
                    {
                        bgr.Dispose();
                        bgr = extra;
                    }

                    try
                    {
                        using (bgr)
                        using (var gray = new Mat())
                        {
                            Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                            if (prevGray.Empty())
                            {
                                prevGray = gray.Clone();
                                continue;
                            }

                            // 1) ucuz kapı: absdiff
                            using (var diff = new Mat())
                            {
                                Cv2.Absdiff(prevGray, gray, diff);
                                Cv2.Threshold(diff, diff, 8, 255, ThresholdTypes.Binary);
                                if (Cv2.CountNonZero(diff) < m_absDiffGate)
                                {
                                    prevGray.Dispose();
                                    prevGray = gray.Clone();
                                    continue;
                                }
                            }

                            // 2) (opsiyonel) pattern: absdiff GEÇTİKTEN sonra uygula
                            if (m_framePattern != null && m_framePattern.Count > 0)
                            {
                                bool accept = m_framePattern[m_motionPatternIdx];
                                m_motionPatternIdx = (m_motionPatternIdx + 1) % m_framePattern.Count;
                                if (!accept)
                                {
                                    prevGray.Dispose();
                                    prevGray = gray.Clone();
                                    continue;
                                }
                            }

                            // 3) k-çiçek: Farneback’i seyrek çalıştır
                            kCounter++;
                            if ((kCounter % m_flowEveryK) != 0)
                            {
                                prevGray.Dispose();
                                prevGray = gray.Clone();
                                continue;
                            }

                            // 4) motion + ROI
                            if (IsMotionDetectedFast(prevGray, gray, out var mask))
                                using (mask)
                                {
                                    var rects = ExtractMotionRects(mask);

                                    foreach (var r in rects)
                                    {
                                        using var roiView = new Mat(bgr, r);
                                        var owned = roiView.Clone();
                                        if (!m_frameQueue.TryAdd(new FrameWithROIRect { Frame = owned, ROI = r }))
                                            owned.Dispose();
                                    }
                                }

                            prevGray.Dispose();
                            prevGray = gray.Clone();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine($"[MotionWorker] Hata: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal kapanış
            }
            finally
            {
                prevGray.Dispose();
            }
        }

        private void MotionWorker(CancellationToken token)
        {
            Mat prevGray = new Mat();

            try
            {
                foreach (var first in m_rawFrameQueue.GetConsumingEnumerable(token))
                {
                    Mat bgr = first;
                    // latest-wins: kuyruğu boşalt, en son gelenle çalış
                    while (m_rawFrameQueue.TryTake(out var extra))
                    {
                        bgr.Dispose();
                        bgr = extra;
                    }

                    try
                    {
                        using (bgr)
                        using (var gray = new Mat())
                        {
                            Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                            if (prevGray.Empty())
                            {
                                prevGray = gray.Clone();
                                continue;
                            }

                            // 1) Ucuz kapı: absdiff
                            using (var diff = new Mat())
                            {
                                Cv2.Absdiff(prevGray, gray, diff);
                                Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

                                int motionPixels = Cv2.CountNonZero(diff);
                                double motionRatio = (double)motionPixels / (gray.Rows * gray.Cols);

                                if (motionRatio < 0.01)
                                {
                                    prevGray.Dispose();
                                    prevGray = gray.Clone();
                                    continue;
                                }
                            }

                            //IsMotionDetected(prevGray.Clone(), gray.Clone(), out var masklolo);

                            // 2) Farneback + maske
                            if (IsMotionDetectedMag2Blur(prevGray, gray, out var mask))
                                
                                
                                using (mask)
                                {
                                    var rects = ExtractMotionRects(mask);

                                    // 3) Tüm ROI’leri ROI kuyruğuna at (ROI seviyesinde pattern YOK)
                                    foreach (var r in rects)
                                    {
                                        using var roiView = new Mat(bgr, r);
                                        var ownedRoi = roiView.Clone();

                                        

                                        if (!m_frameQueue.TryAdd(new FrameWithROIRect { Frame = ownedRoi, ROI = r }))
                                            ownedRoi.Dispose();
                                    }
                                }

                            prevGray.Dispose();
                            prevGray = gray.Clone();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine($"[MotionWorker] Hata: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal kapanış
            }
            finally
            {
                prevGray.Dispose();
            }
        }


        private bool IsMotionDetectedMagnitude(Mat prevGray, Mat currGray, out Mat motionMask)
        {
            motionMask = new Mat();

            if (prevGray == null || prevGray.Empty() || currGray == null || currGray.Empty())
                return false;

            using var flow = new Mat();
            Cv2.CalcOpticalFlowFarneback(prevGray, currGray, flow,
                                          0.5, 5, 31, 5, 7, 1.4, OpticalFlowFlags.FarnebackGaussian);

            // Akışın büyüklüğünü hesapla
            Mat[] flowChannels = flow.Split(); // 2 kanal: x ve y
            using var magnitude = new Mat();
            Cv2.Magnitude(flowChannels[0], flowChannels[1], magnitude);

            // Hareket maskesini oluştur
            Cv2.Threshold(magnitude, motionMask, m_motionThreshold, 255, ThresholdTypes.Binary);
            motionMask.ConvertTo(motionMask, MatType.CV_8UC1); // Binary maskeye dönüştür

            //Cv2.Dilate(motionMask, motionMask, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)));

            Cv2.ImShow("MotionMagnitude", motionMask); // Görselleştir (normalize)
            Cv2.WaitKey(1);

            int motionPixels = Cv2.CountNonZero(motionMask);

            return motionPixels > m_minMotionPixelCount;
        }

        private bool IsMotionDetectedMag2(Mat prevGray, Mat currGray, out Mat motionMask)
        {
            motionMask = new Mat();

            if (prevGray == null || prevGray.Empty() || currGray == null || currGray.Empty())
                return false;

            using var flow = new Mat();
            Cv2.CalcOpticalFlowFarneback(
                prevGray, currGray, flow,
                0.5, 5, 31, 5, 7, 1.4,
                OpticalFlowFlags.FarnebackGaussian
            );

            // Akışın büyüklüğünü karekök almadan hesapla (mag²)
            var flowChannels = flow.Split(); // ch[0]=dx, ch[1]=dy
            using var mag2 = new Mat();
            Cv2.Multiply(flowChannels[0], flowChannels[0], flowChannels[0]); // dx²
            Cv2.Multiply(flowChannels[1], flowChannels[1], flowChannels[1]); // dy²
            Cv2.Add(flowChannels[0], flowChannels[1], mag2);                  // mag² = dx² + dy²

            // Eşikleme (m_motionThreshold karekök yerine kareli ölçekte ayarlanmalı)
            double thr2 = m_motionThreshold * m_motionThreshold;
            Cv2.Threshold(mag2, motionMask, thr2, 255, ThresholdTypes.Binary);
            motionMask.ConvertTo(motionMask, MatType.CV_8UC1); // Binary maskeye dönüştür

            foreach (var ch in flowChannels)
                ch.Dispose();

            int motionPixels = Cv2.CountNonZero(motionMask);

            Cv2.ImShow("MotionMaskZorro", motionMask);
            Cv2.WaitKey(1);

            return motionPixels > m_minMotionPixelCount;
        }



        private bool IsMotionDetectedMag2Blur(Mat prevGray, Mat currGray, out Mat motionMask)
        {
            motionMask = new Mat();

            if (prevGray == null || prevGray.Empty() || currGray == null || currGray.Empty())
                return false;

            using var flow = new Mat();
            Cv2.CalcOpticalFlowFarneback(
                prevGray, currGray, flow,
                0.5, 5, 31, 5, 7, 1.4,
                OpticalFlowFlags.FarnebackGaussian
            );

            // Akışın büyüklüğünü karekök almadan hesapla (mag²)
            var flowChannels = flow.Split(); // ch[0]=dx, ch[1]=dy
            using var mag2 = new Mat();
            Cv2.Multiply(flowChannels[0], flowChannels[0], flowChannels[0]); // dx²
            Cv2.Multiply(flowChannels[1], flowChannels[1], flowChannels[1]); // dy²
            Cv2.Add(flowChannels[0], flowChannels[1], mag2);                  // mag² = dx² + dy²

            Cv2.GaussianBlur(mag2, mag2, new OpenCvSharp.Size(3, 3), 0);


            // Eşikleme (m_motionThreshold karekök yerine kareli ölçekte ayarlanmalı)
            double thr2 = m_motionThreshold * m_motionThreshold;
            Cv2.Threshold(mag2, motionMask, thr2, 255, ThresholdTypes.Binary);
            motionMask.ConvertTo(motionMask, MatType.CV_8UC1); // Binary maskeye dönüştür

            foreach (var ch in flowChannels)
                ch.Dispose();

            int motionPixels = Cv2.CountNonZero(motionMask);

            Cv2.ImShow("MotionMaskZorro", motionMask);
            Cv2.WaitKey(1);

            return motionPixels > m_minMotionPixelCount;
        }


        private bool IsMotionDetectedFastAhmetEfendi(Mat prevGray, Mat currGray, out Mat motionMaskClone)
        {
            motionMaskClone = null;

            // --- 0) Giriş doğrulama ---
            if (prevGray == null || currGray == null || prevGray.Empty() || currGray.Empty())
                return false;
            if (prevGray.Size() != currGray.Size() || prevGray.Type() != MatType.CV_8UC1 || currGray.Type() != MatType.CV_8UC1)
                return false;

            int w = currGray.Cols, h = currGray.Rows;
            double sx = w / 640.0, sy = h / 480.0;

            // --- 0.5) Luma sıçraması sigortası (AE/AWB zıplaması) ---
            // Global parlaklık değişimi çok büyükse hareketi yok say.
            // T_luma: 6–12 arası deneyebilirsin; kameraya göre kalibre et.
            double meanPrev = Cv2.Mean(prevGray).Val0;
            double meanCurr = Cv2.Mean(currGray).Val0;
            double lumaJump = Math.Abs(meanCurr - meanPrev);
            double T_luma = 10.0; // deneysel eşik
            if (lumaJump > T_luma)
            {
                // AE/AWB zıplaması: maske üretmeden çık
                return false;
            }

            // --- 1) Farneback parametreleri (çözünürlüğe göre uyarlama) ---
            // winsize: 15 taban, çözünürlüğe göre 13..31 arası tek sayı
            int winsize = (int)Math.Round(15 * Math.Min(Math.Max(sx, 0.75), 2.0));
            if (winsize < 13) winsize = 13;
            if ((winsize & 1) == 0) winsize++; // tek sayı
            int levels = (w >= 960 || h >= 540) ? 4 : 3;

            // --- 2) Dense optical flow ---
            // m_flow: class üyesi, CV_32FC2 boyutunda olmalı
            Cv2.CalcOpticalFlowFarneback(prevGray, currGray, m_flow,
                                          0.5, 5, 31, 5, 7, 1.4, OpticalFlowFlags.FarnebackGaussian);

            // --- 3) Magnitüd (float) + global akışı çıkarma ---
            var ch = m_flow.Split(); // ch[0]=dx (32F), ch[1]=dy (32F)
                                     // Global flow ofsetini al (kamera pan/titremesi bastırma)
            var mx = Cv2.Mean(ch[0]).Val0;
            var my = Cv2.Mean(ch[1]).Val0;
            Cv2.Subtract(ch[0], new Scalar(mx), ch[0]);
            Cv2.Subtract(ch[1], new Scalar(my), ch[1]);

            // |v| = sqrt(dx^2 + dy^2) → karekök işlemi küçük bir maliyet ama eşiği sadeleştirir
            // m_mag: class üyesi CV_32F; yoksa local Mat da kullanabilirsin
            using var mag = new Mat();
            Cv2.Magnitude(ch[0], ch[1], mag); // CV_32F

            // --- 3.5) Hafif smooth (tuz-biberi kır) ---
            Cv2.GaussianBlur(mag, mag, new OpenCvSharp.Size(5, 5), 0);

            // --- 4) Dinamik eşik: mean + k*std ---
            // k: 1.5–3.0 aralığında deney; sahneye göre sabitle.
            Cv2.MeanStdDev(mag, out var mean, out var stddev);
            float dynThr = (float)(mean.Val0 + 2.0 * stddev.Val0);

            // Eski sabit eşiği taban sınır olarak kullan (opsiyonel güvenlik)
            // m_motionThreshold "magnitüd" ölçeğinde olmalı (mag^2 değil).
            float baseThr = (float)Math.Max(m_motionThreshold, 0.0);
            float thr = Math.Max(dynThr, baseThr);

            // --- 5) Eşikleme (float → 8U maske) ---
            // m_workMask: class üyesi CV_8U; 0/255
            Cv2.Threshold(mag, m_workMask, thr, 255, ThresholdTypes.Binary);
            m_workMask.ConvertTo(m_workMask, MatType.CV_8U);

            // --- 6) Morfoloji: Close → Open ---
            int kx = Math.Max(7, Math.Min(17, (int)Math.Round(9 * sx))); if ((kx & 1) == 0) kx++;
            int ky = Math.Max(3, Math.Min(7, (int)Math.Round(3 * sy))); if ((ky & 1) == 0) ky++;
            using (var kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(kx, ky)))
                Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Close, kClose, iterations: 1);
            using (var kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Open, kOpen, iterations: 1);

            // --- 7) White-ratio freni (global değişim koruması) ---
            double whiteRatio = Cv2.CountNonZero(m_workMask) / (double)(w * h);
            if (whiteRatio > 0.70) // %70 sınırı; 0.6–0.8 arası deneyebilirsin
            {
                // Büyük olasılıkla global aydınlık değişimi/blur → hareket kabul etme
                return false;
            }

            //Cv2.ImShow("MotionMagnitude", m_workMask); // Görselleştir (normalize)
            //Cv2.WaitKey(1);

            // --- 8) Çözünürlükle orantılı min-alan eşiği ---
            double areaScale = (w * h) / 307200.0; // 640*480 referans
            int adaptiveMinPixels = Math.Max(100, (int)Math.Round(m_minMotionPixelCount * areaScale));

            int motionPixels = Cv2.CountNonZero(m_workMask);
            if (motionPixels > adaptiveMinPixels)
            {
                motionMaskClone = m_workMask.Clone(); // yalnız gerektiğinde kopyala
                return true;
            }

            return false;
        }

        private bool IsMotionDetectedFast(Mat prevGray, Mat currGray, out Mat motionMaskClone)
        {
            motionMaskClone = null;

            // --- 0) Giriş doğrulama ---
            if (prevGray == null || currGray == null || prevGray.Empty() || currGray.Empty())
                return false;
            if (prevGray.Size() != currGray.Size() || prevGray.Type() != MatType.CV_8UC1 || currGray.Type() != MatType.CV_8UC1)
                return false;

            int w = currGray.Cols, h = currGray.Rows;
            double sx = w / 640.0, sy = h / 480.0;

            // --- 0.5) Luma sıçraması sigortası (AE/AWB zıplaması) ---
            // Global parlaklık değişimi çok büyükse hareketi yok say.
            // T_luma: 6–12 arası deneyebilirsin; kameraya göre kalibre et.
            double meanPrev = Cv2.Mean(prevGray).Val0;
            double meanCurr = Cv2.Mean(currGray).Val0;
            double lumaJump = Math.Abs(meanCurr - meanPrev);
            double T_luma = 10.0; // deneysel eşik
            if (lumaJump > T_luma)
            {
                // AE/AWB zıplaması: maske üretmeden çık
                return false;
            }

            // --- 1) Farneback parametreleri (çözünürlüğe göre uyarlama) ---
            // winsize: 15 taban, çözünürlüğe göre 13..31 arası tek sayı
            int winsize = (int)Math.Round(15 * Math.Min(Math.Max(sx, 0.75), 2.0));
            if (winsize < 13) winsize = 13;
            if ((winsize & 1) == 0) winsize++; // tek sayı
            int levels = (w >= 960 || h >= 540) ? 4 : 3;

            // --- 2) Dense optical flow ---
            // m_flow: class üyesi, CV_32FC2 boyutunda olmalı
            //Cv2.CalcOpticalFlowFarneback(
            //    prevGray, currGray, m_flow,
            //    0.5, levels, winsize, 3, 5, 1.2,
            //    OpticalFlowFlags.FarnebackGaussian
            //);

            Cv2.CalcOpticalFlowFarneback(prevGray, currGray, m_flow,
                                        0.5, 5, 31, 5, 7, 1.4, OpticalFlowFlags.FarnebackGaussian);

            // --- 3) Magnitüd (float) + global akışı çıkarma ---
            var ch = m_flow.Split(); // ch[0]=dx (32F), ch[1]=dy (32F)
                                           // Global flow ofsetini al (kamera pan/titremesi bastırma)
            var mx = Cv2.Mean(ch[0]).Val0;
            var my = Cv2.Mean(ch[1]).Val0;
            Cv2.Subtract(ch[0], new Scalar(mx), ch[0]);
            Cv2.Subtract(ch[1], new Scalar(my), ch[1]);

            // |v| = sqrt(dx^2 + dy^2) → karekök işlemi küçük bir maliyet ama eşiği sadeleştirir
            // m_mag: class üyesi CV_32F; yoksa local Mat da kullanabilirsin
            using var mag = new Mat();
            Cv2.Magnitude(ch[0], ch[1], mag); // CV_32F

            // --- 3.5) Hafif smooth (tuz-biberi kır) ---
            Cv2.GaussianBlur(mag, mag, new OpenCvSharp.Size(5, 5), 0);

            // --- 4) Dinamik eşik: mean + k*std ---
            // k: 1.5–3.0 aralığında deney; sahneye göre sabitle.
            Cv2.MeanStdDev(mag, out var mean, out var stddev);
            float dynThr = (float)(mean.Val0 + 2.0 * stddev.Val0);

            // Eski sabit eşiği taban sınır olarak kullan (opsiyonel güvenlik)
            // m_motionThreshold "magnitüd" ölçeğinde olmalı (mag^2 değil).
            float baseThr = (float)Math.Max(m_motionThreshold, 0.0);
            float thr = Math.Max(dynThr, baseThr);

            // --- 5) Eşikleme (float → 8U maske) ---
            // m_workMask: class üyesi CV_8U; 0/255
            Cv2.Threshold(mag, m_workMask, thr, 255, ThresholdTypes.Binary);
            m_workMask.ConvertTo(m_workMask, MatType.CV_8U);

            // --- 6) Morfoloji: Close → Open ---
            int kx = Math.Max(7, Math.Min(17, (int)Math.Round(9 * sx))); if ((kx & 1) == 0) kx++;
            int ky = Math.Max(3, Math.Min(7, (int)Math.Round(3 * sy))); if ((ky & 1) == 0) ky++;
            using (var kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(kx, ky)))
                Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Close, kClose, iterations: 1);
            using (var kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Open, kOpen, iterations: 1);

            // --- 7) White-ratio freni (global değişim koruması) ---
            double whiteRatio = Cv2.CountNonZero(m_workMask) / (double)(w * h);
            if (whiteRatio > 0.70) // %70 sınırı; 0.6–0.8 arası deneyebilirsin
            {
                // Büyük olasılıkla global aydınlık değişimi/blur → hareket kabul etme
                return false;
            }

            // --- 8) Çözünürlükle orantılı min-alan eşiği ---
            double areaScale = (w * h) / 307200.0; // 640*480 referans
            int adaptiveMinPixels = Math.Max(100, (int)Math.Round(m_minMotionPixelCount * areaScale));

            int motionPixels = Cv2.CountNonZero(m_workMask);
            if (motionPixels > adaptiveMinPixels)
            {
                motionMaskClone = m_workMask.Clone(); // yalnız gerektiğinde kopyala
                return true;
            }

            return false;
        }


        private bool IsMotionDetectedFastPatlamalı(Mat prevGray, Mat currGray, out Mat motionMaskClone)
        {
            motionMaskClone = null;

            if (prevGray == null || currGray == null || prevGray.Empty() || currGray.Empty())
                return false;

            // --- 1) Çözünürlüğe göre parametre adaptasyonu ---
            int w = currGray.Cols, h = currGray.Rows;
            double sx = w / 640.0, sy = h / 480.0;
            bool large = (w >= 960 || h >= 540);
            int levels = large ? 4 : 3;
            int winsize = large ? 21 : 15;

            // --- 2) Farneback (dense) ---
            Cv2.CalcOpticalFlowFarneback(
                prevGray, currGray, m_flow,
                0.5, levels, winsize, 3, 5, 1.2,
                OpticalFlowFlags.FarnebackGaussian
            );

            // --- 3) mag^2 = (dx^2 + dy^2) (karekök yok) ---
            var ch = m_flow.Split(); // ch[0]=dx, ch[1]=dy
            try
            {
                // (Opsiyonel ama faydalı) global akışı çıkar: kamera pan/titremesinde işe yarar
                Scalar mx = Cv2.Mean(ch[0]);
                Scalar my = Cv2.Mean(ch[1]);
                Cv2.Subtract(ch[0], new Scalar(mx.Val0), ch[0]);
                Cv2.Subtract(ch[1], new Scalar(my.Val0), ch[1]);

                // Kare al + topla → mag^2
                Cv2.Multiply(ch[0], ch[0], ch[0]); // dx^2 (in-place)
                Cv2.Multiply(ch[1], ch[1], ch[1]); // dy^2
                Cv2.Add(ch[0], ch[1], m_mag2);     // mag2 = dx^2 + dy^2
            }
            finally
            {
                foreach (var c in ch) c.Dispose();
            }

            // (Çok hafif) pre-smooth: “tuz-biber”i azaltır, parçaları birleştirir
            Cv2.GaussianBlur(m_mag2, m_mag2, new OpenCvSharp.Size(5, 5), 0);

            double thr2 = m_motionThreshold * m_motionThreshold;

            // --- 4) Eşikleme ---
            Cv2.Compare(m_mag2, new Scalar(thr2), m_workMask, CmpType.GT); // 0/1
            m_workMask.ConvertTo(m_workMask, MatType.CV_8UC1, 255.0);      // 0/255

            // --- 5) Morfoloji: Close (yatay birleştir) → Open (küçük gürültüyü al) ---
            int kx = Math.Max(7, Math.Min(17, (int)Math.Round(9 * sx))); if ((kx & 1) == 0) kx++;
            int ky = Math.Max(3, Math.Min(7, (int)Math.Round(3 * sy))); if ((ky & 1) == 0) ky++;

            using (var kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(kx, ky)))
                Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Close, kClose, iterations: 1);

            using (var kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Open, kOpen, iterations: 1);

            // --- 6) Çözünürlükle orantılı alan eşiği ---
            double areaScale = (w * h) / 307200.0; // 640*480 referans
            int adaptiveMinPixels = Math.Max(100, (int)Math.Round(m_minMotionPixelCount * areaScale));

            int motionPixels = Cv2.CountNonZero(m_workMask);
            if (motionPixels > adaptiveMinPixels)
            {
                // DEBUG için:
                // Cv2.ImShow("MotionMask", m_workMask);
                // Cv2.WaitKey(1);

                motionMaskClone = m_workMask.Clone(); // yalnız ihtiyaç varsa kopyala
                return true;
            }

            return false;
        }


        private bool IsMotionDetectedFastAhmet(Mat prevGray, Mat currGray, out Mat motionMaskClone)
        {
            motionMaskClone = null;

            if (prevGray == null || currGray == null || prevGray.Empty() || currGray.Empty())
                return false;

            // --- 1) Çözünürlüğe göre hafif parametre adaptasyonu ---
            int w = currGray.Cols, h = currGray.Rows;
            bool large = (w >= 960 || h >= 540);
            int levels = large ? 4 : 3;
            int winsize = large ? 21 : 15;

            // --- 2) Farneback (dense) ---
            Cv2.CalcOpticalFlowFarneback(prevGray, currGray, m_flow,
                                         0.5, levels, winsize, 3, 5, 1.2,
                                         OpticalFlowFlags.FarnebackGaussian);

            // --- 3) mag^2 = x^2 + y^2 (karekök yok → daha hızlı) ---
            var ch = m_flow.Split(); // ch[0]=dx, ch[1]=dy
            try
            {
                Cv2.Multiply(ch[0], ch[0], ch[0]); // dx^2 (in-place)
                Cv2.Multiply(ch[1], ch[1], ch[1]); // dy^2
                Cv2.Add(ch[0], ch[1], m_mag2);     // mag2 = dx^2 + dy^2
            }
            finally
            {
                foreach (var c in ch) c.Dispose();
            }

            double thr2 = m_motionThreshold * m_motionThreshold;

            // --- 4) Eşikleme + morfolojik temizlik ---
            Cv2.Compare(m_mag2, new Scalar(thr2), m_workMask, CmpType.GT);     // 0/1
            m_workMask.ConvertTo(m_workMask, MatType.CV_8UC1, 255.0);          // 0/255

            Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Open, m_kernel3, iterations: 1);
            Cv2.Dilate(m_workMask, m_workMask, m_kernel3, iterations: 1);

            // --- 5) Çözünürlükle orantılı alan eşiği ---
            double areaScale = (w * h) / 307200.0; // 307200 = 640*480 referansı
            int adaptiveMinPixels = Math.Max(100, (int)Math.Round(m_minMotionPixelCount * areaScale));

            int motionPixels = Cv2.CountNonZero(m_workMask);
            if (motionPixels > adaptiveMinPixels)
            {


                Cv2.ImShow("MotionMagnitude", m_workMask); // Görselleştir (normalize)
                Cv2.WaitKey(1);

                // Yalnızca gerekiyorsa kopyala (GC tasarrufu)
                motionMaskClone = m_workMask.Clone();
                return true;
            }

            return false;
        }



        //private bool IsMotionDetectedFast(Mat prevGray, Mat currGray, out Mat motionMaskClone)
        //{
        //    motionMaskClone = null;
        //    if (prevGray == null || currGray == null || prevGray.Empty() || currGray.Empty())
        //        return false;

        //    int w = currGray.Cols, h = currGray.Rows;
        //    bool large = (w >= 960 || h >= 540);
        //    int levels = large ? 4 : 3;
        //    int winsize = large ? 21 : 15;

        //    // 1) Farneback
        //    Cv2.CalcOpticalFlowFarneback(prevGray, currGray, m_flow,
        //                                 0.5, levels, winsize, 3, 5, 1.2,
        //                                 OpticalFlowFlags.FarnebackGaussian);

        //    // 2) mag^2
        //    var ch = m_flow.Split();
        //    try
        //    {
        //        Cv2.Multiply(ch[0], ch[0], ch[0]);
        //        Cv2.Multiply(ch[1], ch[1], ch[1]);
        //        Cv2.Add(ch[0], ch[1], m_mag2);
        //    }
        //    finally { foreach (var c in ch) c.Dispose(); }

        //    double thr2 = m_motionThreshold * m_motionThreshold;

        //    // 3) motion mask + temizlik
        //    Cv2.Compare(m_mag2, new Scalar(thr2), m_workMask, CmpType.GT);
        //    m_workMask.ConvertTo(m_workMask, MatType.CV_8UC1, 255.0);
        //    Cv2.MorphologyEx(m_workMask, m_workMask, MorphTypes.Open, m_kernel3, iterations: 1);
        //    Cv2.Dilate(m_workMask, m_workMask, m_kernel3, iterations: 1);

        //    // === (YENİ) Plaka-bias: dikey kenar maskesi ile kesiştir ===
        //    using (var sobelX16 = new Mat())
        //    using (var sobelAbs = new Mat())
        //    using (var edgeMask = new Mat())
        //    {
        //        // Dikey kenarlar (x yönünde türev)
        //        Cv2.Sobel(currGray, sobelX16, MatType.CV_16S, 1, 0, 3);
        //        Cv2.ConvertScaleAbs(sobelX16, sobelAbs);

        //        // Otsu ile adaptif eşik (sahneye göre ayarlanır)
        //        Cv2.Threshold(sobelAbs, edgeMask, 0, 255, ThresholdTypes.Otsu);
        //        Cv2.MorphologyEx(edgeMask, edgeMask, MorphTypes.Open, m_kernel3, iterations: 1);

        //        // motion ∩ edge
        //        Cv2.BitwiseAnd(m_workMask, edgeMask, m_workMask);
        //    }

        //    // (Opsiyonel) Sabit kamera için dikey bant önceliği (alt-orta bölgeyi tut)
        //    // int yTop = (int)(h * 0.25), yBot = (int)(h * 0.95);
        //    // Cv2.Rectangle(m_workMask, new Rect(0, 0, w, yTop), Scalar.Black, -1);
        //    // Cv2.Rectangle(m_workMask, new Rect(0, yBot, w, h - yBot), Scalar.Black, -1);

        //    // 4) adaptif alan eşiği
        //    double areaScale = (w * h) / 307200.0;
        //    int adaptiveMinPixels = Math.Max(100, (int)Math.Round(m_minMotionPixelCount * areaScale));

        //    int motionPixels = Cv2.CountNonZero(m_workMask);
        //    if (motionPixels > adaptiveMinPixels)
        //    {
        //        motionMaskClone = m_workMask.Clone();
        //        return true;
        //    }
        //    return false;
        //}






        private void ProcessFrames(CancellationToken token)
        {
            foreach (var item in m_frameQueue.GetConsumingEnumerable(token))
            {
                using var roiBgr = item.Frame; // sahiplik burada
                var roiRect = item.ROI;


                DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, roiBgr.ToBitmap());

                try
                {
                    // 1) (Opsiyonel) ROI iyileştirme (contrast/gamma/sharpen vs.)
                    // İyileştirilen Mat 'enhanced' ROI olarak kullanılacak.
                    // Eğer NewProcessFrame ROI üzerinde çalışıyorsa:
                    Mat enhanced = null;
                    try
                    {
                        enhanced = m_cameraChannel?.NewProcessFrame(roiBgr, m_autoLight, m_autoWhite);
                    }
                    catch { /* iyileştirme başarısızsa sessizce devam */ }


                    using var roiForDetect = enhanced ?? roiBgr; // fallback: orijinal ROI


                    //ROI'de plaka tespiti yap
                    var localPlates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roiForDetect);

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

                    //AddNewTrackedPlates(localPlates);

                    ////ROI'nin gri versiyonunu çıkar (takip için)
                    //using var gray = new Mat();
                    //Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
                    //UpdateAllTrackedPlates(gray);
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"[ProcessFrames] Hata: {ex.Message}");
                }


            }

        }


        public void Start()
        {
            if (m_isRunning) return;

            m_cts = new CancellationTokenSource();
            m_isRunning = true;

            m_motionTask = Task.Factory.StartNew(() => MotionWorker(m_cts.Token), TaskCreationOptions.LongRunning);
            m_processTask = Task.Factory.StartNew(() => ProcessFrames(m_cts.Token), TaskCreationOptions.LongRunning);
            m_trackingTask = Task.Factory.StartNew(() => m_ocrWorker.Start(m_cts.Token), TaskCreationOptions.LongRunning);
        }


        public void Stop()
        {
            if (!m_isRunning) return;

            m_cts.Cancel();

            try { m_motionTask?.Wait(); } catch (AggregateException ex) { foreach (var e in ex.InnerExceptions) Debug.WriteLine(e.Message); }
            try { m_processTask?.Wait(); } catch (AggregateException ex) { foreach (var e in ex.InnerExceptions) Debug.WriteLine(e.Message); }

            m_ocrWorker.Stop();
            m_isRunning = false;
        }
        internal Rect[] ExtractMotionRects(Mat motionMask)
        {
            int W = motionMask.Width, H = motionMask.Height;
            double sx = W / 640.0, sy = H / 480.0;

            // 1) Close (yatay kırıkları kapat) — ölçekli kernel
            int kx = Math.Max(7, Math.Min(17, (int)Math.Round(9 * sx)));
            
            if ((kx & 1) == 0) 
                kx++; // tek yap
            int ky = Math.Max(3, Math.Min(7, (int)Math.Round(3 * sy)));
            
            if ((ky & 1) == 0) 
                ky++;

            using var closed = new Mat();
            //using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(kx, ky)))
            //    Cv2.MorphologyEx(motionMask, closed, MorphTypes.Close, k, iterations: 1);


            //gpt öneri deneme
            // 4) Morfoloji (küçük gürültüyü at, parçaları birleştir)
            Cv2.MorphologyEx(motionMask, motionMask, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 3)));
            Cv2.MorphologyEx(motionMask, motionMask, MorphTypes.Open, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)));


            // 2) Konturlar
            Cv2.FindContours(motionMask, out OpenCvSharp.Point[][] contours, out _,
                             RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // Min alan (w/h yerine): 640x480 için ~80 px taban
            double minArea = Math.Max(80, 0.0001 * W * H); // 0.01% alan

            var rects = new List<Rect>();

            foreach (var c in contours)
            {
                var r = Cv2.BoundingRect(c);

                if (r.Width <= 0 || r.Height <= 0) 
                    continue;
                
                if ((double)r.Width * r.Height < minArea) 
                    continue; // çok ufak gürültüyü at
                
                rects.Add(r);
            }

            if (rects.Count == 0) 
                return Array.Empty<Rect>();

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, closed.ToBitmap());

            //Cv2.ImShow("MotionMask", closed);
            //Cv2.WaitKey(1);


            // 3) (Opsiyonel) Yakın/örtüşen kutuları hafifçe birleştir (yatay yakın)
            //rects = MergeRectsByProximity(rects, maxGapX: (int)Math.Round(12 * sx), minVertOverlap: 0.40);

            // 4) Hafif büyüt (OCR için bağlam)
            //int growX = (int)Math.Round(8 * sx);
            //int growY = (int)Math.Round(5 * sy);
            //for (int i = 0; i < rects.Count; i++)
            //    rects[i] = GrowRect(rects[i], growX, growY, W, H);

            //// 5) Soldan sağa sırala (deterministik)
            //rects.Sort((a, b) => a.X.CompareTo(b.X));

            return rects.ToArray();
        }


        internal Rect[] ExtractMotionRectsgptlirevizeöncesi(Mat motionMask)
        {
            List<Rect> rects = new();

            // 1) Yatay kırıkları kapat (plaka yatay uzun olduğu için 9x3 iyi başlatma)
            using var closed = new Mat();
            using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 3)))
            {
                Cv2.MorphologyEx(motionMask, closed, MorphTypes.Close, k, iterations: 1);
            }



            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(closed, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

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

        internal Rect[] ExtractMotionRectsOld(Mat motionMask)
        {
            // 1) Yatay kırıkları kapat (plaka yatay uzun olduğu için 9x3 iyi başlatma)
            using var closed = new Mat();
            using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 3)))
            {
                Cv2.MorphologyEx(motionMask, closed, MorphTypes.Close, k, iterations: 1);
            }

            // 2) Kontur → aday dikdörtgenler
            Cv2.FindContours(closed, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var candidates = new List<Rect>();
            foreach (var c in contours)
            {
                var r = Cv2.BoundingRect(c);

                // Boyut ve alan filtreleri
                if (r.Width < m_minRoiWidth || r.Height < m_minRoiHeight) continue;
                double area = r.Width * r.Height;
                if (area < m_minContourArea) continue;

                // En/Boy oranı (plaka benzeri)
                double aspect = (double)r.Width / Math.Max(1, r.Height);
                if (aspect < m_minRoiAspect || aspect > m_maxRoiAspect) continue;

                candidates.Add(r);
            }
            if (candidates.Count == 0)
                return Array.Empty<Rect>();

            // 3) Yakın/örtüşen kutuları yatayda birleştir
            var merged = MergeRectsByProximity(candidates, maxGapX: 12, minVertOverlap: 0.40);

            // 4) Büyüt ve görüntü sınırına sabitle
            var grown = merged
                .Select(r => GrowRect(r, 10, 6, motionMask.Width, motionMask.Height))
                .ToList();

            // 5) NMS ile fazla örtüşenleri ele (iou>0.6 → büyük olanı tut)
            var finalRects = Nms(grown, iouThresh: 0.60);

            // 6) Soldan sağa sırala
            finalRects.Sort((a, b) => a.X.CompareTo(b.X));
            return finalRects.ToArray();
        }


        private static Rect GrowRect(Rect r, int growX, int growY, int maxW, int maxH)
        {
            // Geçersizse dokunma
            if (r.Width <= 0 || r.Height <= 0) return r;

            // Kenarlardan simetrik büyüt
            long x = (long)r.X - growX;
            long y = (long)r.Y - growY;
            long w = (long)r.Width + 2L * growX;
            long h = (long)r.Height + 2L * growY;

            // Sınırlar içinde tut
            x = Math.Max(0, Math.Min(x, maxW - 1));
            y = Math.Max(0, Math.Min(y, maxH - 1));

            w = Math.Max(1, Math.Min(w, maxW - x));
            h = Math.Max(1, Math.Min(h, maxH - y));

            return new Rect((int)x, (int)y, (int)w, (int)h);
        }


        // --- Yardımcılar ---

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

        private static List<Rect> Nms(List<Rect> rects, double iouThresh)
        {
            if (rects.Count <= 1) return new List<Rect>(rects);

            // Alanı büyük olanları öncele
            var order = rects
                .Select(r => (r, area: r.Width * r.Height))
                .OrderByDescending(t => t.area)
                .Select(t => t.r)
                .ToList();

            var kept = new List<Rect>();
            var suppressed = new bool[order.Count];

            for (int i = 0; i < order.Count; i++)
            {
                if (suppressed[i]) continue;
                var ri = order[i];
                kept.Add(ri);

                for (int j = i + 1; j < order.Count; j++)
                {
                    if (suppressed[j]) continue;
                    if (IoU(ri, order[j]) > iouThresh)
                        suppressed[j] = true;
                }
            }
            return kept;
        }

        private static double IoU(Rect a, Rect b)
        {
            int x1 = Math.Max(a.X, b.X);
            int y1 = Math.Max(a.Y, b.Y);
            int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            int iw = Math.Max(0, x2 - x1);
            int ih = Math.Max(0, y2 - y1);
            double inter = iw * ih;
            if (inter <= 0) return 0.0;

            double union = (a.Width * a.Height) + (b.Width * b.Height) - inter;
            return inter / Math.Max(1.0, union);
        }















        public void Dispose()
        {
            Stop();

            m_rawFrameQueue?.Dispose();
            m_frameQueue?.Dispose();
            m_plateQueue?.Dispose();

            m_flow?.Dispose();
            m_mag2?.Dispose();
            m_workMask?.Dispose();
            m_kernel3?.Dispose();
        }

    }
}
