using Accord.Statistics.Running;
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
    internal class MotionBasedPlateReadingStrategy : IPlateReadingStrategy
    {
        private  int _cameraId;

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
        //public void Configure(CameraConfiguration cameraConfiguration)
        //{
        //    _cameraId = cameraConfiguration.Id;
        //    m_videoSource = cameraConfiguration.VideoSource;
        //    m_AutoLightControl = cameraConfiguration.AutoLightControl;
        //    m_AutoWhiteBalance = cameraConfiguration.AutoWhiteBalance;

        //    m_framePattern = cameraConfiguration.FramePattern;
        //    m_framePatternEffectiveLength = cameraConfiguration.FramePattern.Count(x => x);

        //    m_frameQueue = new BlockingCollection<Mat>(boundedCapacity: m_framePatternEffectiveLength);
        //    m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);


        //}

        public void Configure(CameraConfiguration cameraConfiguration, IOCRImageAnalyzer analyzer)
        {
            _cameraId = cameraConfiguration.Id;
            m_AutoLightControl = cameraConfiguration.AutoLightControl;
            m_AutoWhiteBalance = cameraConfiguration.AutoWhiteBalance;

            m_framePattern = cameraConfiguration.FramePattern;
            m_framePatternEffectiveLength = cameraConfiguration.FramePattern.Count(x => x);

            m_frameQueue = new BlockingCollection<Mat>(boundedCapacity: m_framePatternEffectiveLength);
            m_plateQueue = new BlockingCollection<PossiblePlate>(boundedCapacity: 3);

            m_ocrAnalyzer = analyzer;


            //m_continuousOCRImage.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);

            m_ocrAggregator = new OCRResultAggregator(_cameraId);
            m_ocrAggregator.PlateResultReady += (s, e) => PlateResultReady?.Invoke(this, e);
            m_ocrAggregator.PlateImageReady += (s, e) => PlateImageReady?.Invoke(this, e);

            m_ocrWorker = new OCRWorker(m_ocrAnalyzer, m_plateQueue, m_ocrAggregator);

        }

        Mat prevGray = null;
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

                    if (shouldProcess)
                    {
                        //Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                        //List<OpenCvSharp.Point> allPoints = new List<OpenCvSharp.Point>();

                        //foreach (var contour in contours)
                        //{
                        //    //Rect motionRect = Cv2.BoundingRect(contour);
                        //    //if (motionRect.Width < 15 || motionRect.Height < 5)
                        //    //    continue;

                        //    allPoints.AddRange(contour);
                        //}

                        //if (allPoints.Count > 0)
                        {
                            //Rect combinedRect = Cv2.BoundingRect(allPoints);
                            //combinedRect = combinedRect.Intersect(new Rect(0, 0, frame.Width, frame.Height));

                            using (Mat processImage = m_cameraChannel.NewProcessFrame(
                                frame,
                                m_AutoLightControl,
                                m_AutoWhiteBalance))
                            {
                                //using (Mat roi = new Mat(processImage, combinedRect))
                                {
                                    if (!m_frameQueue.TryAdd(processImage.Clone()))
                                    {
                                        // kuyruk doluysa önemli değil
                                    }
                                }
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
                    Task.WaitAll(new[] {  m_processTask, m_OCRTask }, 3000);
                }
                catch (AggregateException) { /* task hatası olabilir, yutulabilir */ }

                m_cts.Dispose();
                m_cts = null;
            }

            m_isRunning = false;
        }

        public void ReadProcessDisplayAndEnqueueTest(CancellationToken token)
        {
           //RequestToStartStream();

           // using (VideoCapture captureCam = new VideoCapture(m_videoSource))
           // {
           //     if (!captureCam.IsOpened())
           //     {
           //         MessageBox.Show("Video açılamadı!");
           //         return;
           //     }

           //     m_cameraChannel.ResetProcessingState();
           //     captureCam.Set(VideoCaptureProperties.PosFrames, currentFramePos);

           //     int fps = (int)captureCam.Get(VideoCaptureProperties.Fps);
           //     int delay = fps > 0 ? 1000 / fps : 30;

           //     Mat frame = new Mat();
           //     Mat prevGray = null;
           //     int frameCount = 0;

               

           //     while (!token.IsCancellationRequested && m_stream)
           //     {
           //         if (!captureCam.Read(frame) || frame.Empty())
           //         {
           //             m_cameraChannel.ResetProcessingState();
           //             Console.WriteLine("Video bitti veya hata oluştu!");
           //             break;
           //         }

           //         currentFramePos = (int)captureCam.Get(VideoCaptureProperties.PosFrames);

           //         Cv2.Resize(frame, frame, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

           //         m_onFrameReady?.Invoke(BitmapConverter.ToBitmap(frame));

           //         //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxOriginalImage, BitmapConverter.ToBitmap(frame));

           //         Mat gray = new Mat();
           //         Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

           //         if (prevGray != null)
           //         {
           //             Mat diff = new Mat();
           //             Cv2.Absdiff(prevGray, gray, diff);
           //             Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

           //             int motionPixels = Cv2.CountNonZero(diff);
           //             double motionRatio = (double)motionPixels / (gray.Rows * gray.Cols);

           //             bool shouldProcess = motionRatio > 0.02 && m_framePattern[frameCount % m_framePattern.Count];

           //             if (shouldProcess)
           //             {
           //                 //Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
           //                 //List<OpenCvSharp.Point> allPoints = new List<OpenCvSharp.Point>();

           //                 //foreach (var contour in contours)
           //                 //{
           //                 //    //Rect motionRect = Cv2.BoundingRect(contour);
           //                 //    //if (motionRect.Width < 15 || motionRect.Height < 5)
           //                 //    //    continue;

           //                 //    allPoints.AddRange(contour);
           //                 //}

           //                 //if (allPoints.Count > 0)
           //                 {
           //                     //Rect combinedRect = Cv2.BoundingRect(allPoints);
           //                     //combinedRect = combinedRect.Intersect(new Rect(0, 0, frame.Width, frame.Height));

           //                     using (Mat processImage = m_cameraChannel.NewProcessFrame(
           //                         frame,
           //                         m_AutoLightControl,
           //                         m_AutoWhiteBalance))
           //                     {
           //                         //using (Mat roi = new Mat(processImage, combinedRect))
           //                         {
           //                             if (!m_frameQueue.TryAdd(processImage.Clone()))
           //                             {
           //                                 // kuyruk doluysa önemli değil
           //                             }
           //                         }
           //                     }
           //                 }
           //             }

           //             //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(diff));
           //             diff.Dispose();
           //         }

           //         frameCount++;
           //         prevGray?.Dispose();
           //         prevGray = gray.Clone();
           //         gray.Dispose();

           //         Thread.Sleep(delay);
           //     }

           //     frame.Dispose();
           //     prevGray?.Dispose();
           // }
        }

        //public void ReadProcessDisplayAndEnqueueTestButunHareketlilerTekROI(CancellationToken token)
        //{
        //    MainForm.m_mainForm.RequestToStartStream();

        //    using (VideoCapture captureCam = new VideoCapture("edsvideo.mkv"))
        //    {
        //        if (!captureCam.IsOpened())
        //        {
        //            MessageBox.Show("Video açılamadı!");
        //            return;
        //        }

        //        ImageEnhancementHelper.ResetProcessingState();

        //        captureCam.Set(VideoCaptureProperties.PosFrames, currentFramePos);
        //        int fps = (int)captureCam.Get(VideoCaptureProperties.Fps);
        //        int delay = fps > 0 ? 1000 / fps : 30;

        //        Mat frame = new Mat();
        //        Mat prevGray = null;

        //        int frameCount = 0;
        //        int processEveryNFrame = 3;

        //        while (!token.IsCancellationRequested && m_stream)
        //        {
        //            if (!captureCam.Read(frame) || frame.Empty())
        //            {
        //                ImageEnhancementHelper.ResetProcessingState();
        //                Console.WriteLine("Video bitti veya hata oluştu!");
        //                break;
        //            }

        //            currentFramePos = (int)captureCam.Get(VideoCaptureProperties.PosFrames);

        //            ////Cv2.Resize(frame, frame, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4);

        //            // Görüntüyü ekranda göster
        //            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(frame));

        //            // Görüntüyü griye çevir
        //            Mat gray = new Mat();
        //            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //            if (prevGray != null)
        //            {
        //                Mat diff = new Mat();
        //                Cv2.Absdiff(prevGray, gray, diff);
        //                Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);


        //                // Hareketli piksel sayısı
        //                int motionPixels = Cv2.CountNonZero(diff);
        //                double motionRatio = (double)motionPixels / (gray.Rows * gray.Cols);

        //                if ((motionRatio > 0.02) && (frameCount % processEveryNFrame == 0))
        //                {
        //                    // Hareketli alanları bul
        //                    Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        //                    List<OpenCvSharp.Point> allPoints = new List<OpenCvSharp.Point>();

        //                    foreach (var contour in contours)
        //                    {
        //                        Rect motionRect = Cv2.BoundingRect(contour);

        //                        // Çok küçük gürültüleri filtrele
        //                        if (motionRect.Width < 15 || motionRect.Height < 5)
        //                            continue;

        //                        allPoints.AddRange(contour);
        //                    }

        //                    if (allPoints.Count == 0)
        //                        continue;

        //                    Rect combinedRect = Cv2.BoundingRect(allPoints);
        //                    combinedRect = combinedRect.Intersect(new Rect(0, 0, frame.Width, frame.Height));

        //                    using (Mat processImage = FrameProcessingHelper.NewProcessFrame(frame, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance))
        //                    {
        //                        using (Mat roi = new Mat(processImage, combinedRect))
        //                        {
        //                            if (!m_frameQueue.TryAdd(roi.Clone()))
        //                            {
        //                                // kuyruk doluysa önemli değil
        //                            }
        //                        }
        //                    }


        //                }
        //                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(diff));

        //                diff.Dispose();
        //            }

        //            frameCount++;



        //            prevGray?.Dispose();
        //            prevGray = gray.Clone();
        //            gray.Dispose();
        //            Thread.Sleep(delay);
        //        }

        //        frame.Dispose();
        //        prevGray?.Dispose();
        //    }
        //}

        //public void ReadProcessDisplayAndEnqueueTestButunHareketlilerTekROI_PatternTemelli(CancellationToken token)
        //{
        //    MainForm.m_mainForm.RequestToStartStream();

        //    using (VideoCapture captureCam = new VideoCapture("edsvideo.mkv"))
        //    {
        //        if (!captureCam.IsOpened())
        //        {
        //            MessageBox.Show("Video açılamadı!");
        //            return;
        //        }

        //        ImageEnhancementHelper.ResetProcessingState();
        //        captureCam.Set(VideoCaptureProperties.PosFrames, currentFramePos);

        //        int fps = (int)captureCam.Get(VideoCaptureProperties.Fps);
        //        int delay = fps > 0 ? 1000 / fps : 30;

        //        Mat frame = new Mat();
        //        Mat prevGray = null;
        //        int frameCount = 0;

        //        // Frame örüntüsü: işlem yapılacak frame'ler
        //        List<bool> framePattern = new List<bool> { true, true, false, false };

        //        while (!token.IsCancellationRequested && MainForm.m_mainForm.m_stream)
        //        {
        //            if (!captureCam.Read(frame) || frame.Empty())
        //            {
        //                ImageEnhancementHelper.ResetProcessingState();
        //                Console.WriteLine("Video bitti veya hata oluştu!");
        //                break;
        //            }

        //            currentFramePos = (int)captureCam.Get(VideoCaptureProperties.PosFrames);
        //            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(frame));

        //            Mat gray = new Mat();
        //            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //            if (prevGray != null)
        //            {
        //                Mat diff = new Mat();
        //                Cv2.Absdiff(prevGray, gray, diff);
        //                Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

        //                int motionPixels = Cv2.CountNonZero(diff);
        //                double motionRatio = (double)motionPixels / (gray.Rows * gray.Cols);

        //                bool shouldProcess = motionRatio > 0.02 && framePattern[frameCount % framePattern.Count];

        //                if (shouldProcess)
        //                {
        //                    //Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        //                    //List<OpenCvSharp.Point> allPoints = new List<OpenCvSharp.Point>();

        //                    //foreach (var contour in contours)
        //                    //{
        //                    //    //Rect motionRect = Cv2.BoundingRect(contour);
        //                    //    //if (motionRect.Width < 15 || motionRect.Height < 5)
        //                    //    //    continue;

        //                    //    allPoints.AddRange(contour);
        //                    //}

        //                    //if (allPoints.Count > 0)
        //                    {
        //                        //Rect combinedRect = Cv2.BoundingRect(allPoints);
        //                        //combinedRect = combinedRect.Intersect(new Rect(0, 0, frame.Width, frame.Height));

        //                        using (Mat processImage = FrameProcessingHelper.NewProcessFrame(
        //                            frame,
        //                            MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl,
        //                            MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance))
        //                        {
        //                            //using (Mat roi = new Mat(processImage, combinedRect))
        //                            {
        //                                if (!m_frameQueue.TryAdd(processImage.Clone()))
        //                                {
        //                                    // kuyruk doluysa önemli değil
        //                                }
        //                            }
        //                        }
        //                    }
        //                }

        //                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(diff));
        //                diff.Dispose();
        //            }

        //            frameCount++;
        //            prevGray?.Dispose();
        //            prevGray = gray.Clone();
        //            gray.Dispose();

        //            Thread.Sleep(delay);
        //        }

        //        frame.Dispose();
        //        prevGray?.Dispose();
        //    }
        //}

        //public void ReadProcessDisplayAndEnqueueTestBütünFramelerdeİyileştirme(CancellationToken token)
        //{
        //    MainForm.m_mainForm.RequestToStartStream();

        //    using (VideoCapture captureCam = new VideoCapture("edsvideo.mkv"))
        //    {
        //        if (!captureCam.IsOpened())
        //        {
        //            MessageBox.Show("Video açılamadı!");
        //            return;
        //        }

        //        ImageEnhancementHelper.ResetProcessingState();

        //        captureCam.Set(VideoCaptureProperties.PosFrames, currentFramePos);
        //        int fps = (int)captureCam.Get(VideoCaptureProperties.Fps);
        //        int delay = fps > 0 ? 1000 / fps : 30;

        //        Mat frame = new Mat();
        //        Mat prevGray = null;

        //        while (!token.IsCancellationRequested && MainForm.m_mainForm.m_stream)
        //        {
        //            if (!captureCam.Read(frame) || frame.Empty())
        //            {
        //                ImageEnhancementHelper.ResetProcessingState();
        //                Console.WriteLine("Video bitti veya hata oluştu!");
        //                break;
        //            }

        //            currentFramePos = (int)captureCam.Get(VideoCaptureProperties.PosFrames);

        //            Cv2.Resize(frame, frame, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4);

        //            // Görüntüyü griye çevir
        //            Mat gray = new Mat();
        //            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //            // Preprocessing tüm frame'e uygula
        //            using (Mat processedFrame = FrameProcessingHelper.NewProcessFrame(
        //                frame,
        //                MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl,
        //                MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance))
        //            {
        //                // Görüntüyü ekranda göster
        //                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(processedFrame));

        //                if (prevGray != null)
        //                {
        //                    Mat diff = new Mat();
        //                    Cv2.Absdiff(prevGray, gray, diff);
        //                    Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

        //                    // Gürültüyü azalt
        //                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
        //                    Cv2.MorphologyEx(diff, diff, MorphTypes.Close, kernel);

        //                    // Hareketli alanları bul
        //                    Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        //                    foreach (var contour in contours)
        //                    {
        //                        Rect motionRect = Cv2.BoundingRect(contour);

        //                        // Çok küçük gürültüleri filtrele
        //                        if (motionRect.Width < 60 || motionRect.Height < 20)
        //                            continue;

        //                        // İşlenmiş görüntüden ROI kırp
        //                        using (Mat roi = new Mat(processedFrame, motionRect))
        //                        {
        //                            if (!m_frameQueue.TryAdd(roi.Clone()))
        //                            {
        //                                // kuyruk dolu, problem değil
        //                            }

        //                            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(roi.Clone()));
        //                        }
        //                    }

        //                    diff.Dispose();
        //                    kernel.Dispose();
        //                }
        //            }

        //            prevGray?.Dispose();
        //            prevGray = gray.Clone();

        //            Thread.Sleep(delay);
        //        }

        //        frame.Dispose();
        //        prevGray?.Dispose();
        //    }
        //}

        //public void ReadProcessDisplayAndEnqueueTestÇalıştırdum(CancellationToken token)
        //{
        //    MainForm.m_mainForm.RequestToStartStream();

        //    using (VideoCapture captureCam = new VideoCapture("edsvideo.mkv"))
        //    {
        //        if (!captureCam.IsOpened())
        //        {
        //            MessageBox.Show("Video açılamadı!");
        //            return;
        //        }

        //        ImageEnhancementHelper.ResetProcessingState();

        //        captureCam.Set(VideoCaptureProperties.PosFrames, currentFramePos);
        //        int fps = (int)captureCam.Get(VideoCaptureProperties.Fps);
        //        int delay = fps > 0 ? 1000 / fps : 30;

        //        Mat frame = new Mat();
        //        Mat prevGray = null;

        //        int frameCount = 0;

        //        // 5 frame'lik örüntü: [al, al, alma, alma, alma]
        //        //List<bool> framePattern = new List<bool> { true, true, true, false, false, false };

        //        while (!token.IsCancellationRequested && MainForm.m_mainForm.m_stream)
        //        {
        //            if (!captureCam.Read(frame) || frame.Empty())
        //            {
        //                ImageEnhancementHelper.ResetProcessingState();
        //                Console.WriteLine("Video bitti veya hata oluştu!");
        //                break;
        //            }

        //            currentFramePos = (int)captureCam.Get(VideoCaptureProperties.PosFrames);

        //            Cv2.Resize(frame, frame, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4);

        //            // Görüntüyü ekranda göster
        //            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(frame));

        //            // Görüntüyü griye çevir
        //            Mat gray = new Mat();
        //            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);



        //            if (prevGray != null)
        //            {
        //                Mat diff = new Mat();
        //                Cv2.Absdiff(prevGray, gray, diff);
        //                Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

        //                // Gürültüyü azalt
        //                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
        //                Cv2.MorphologyEx(diff, diff, MorphTypes.Close, kernel);

        //                // Hareketli alanları bul
        //                Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        //                foreach (var contour in contours)
        //                {
        //                    Rect motionRect = Cv2.BoundingRect(contour);

        //                    // Çok küçük gürültüleri filtrele
        //                    if (motionRect.Width < 60 || motionRect.Height < 20)
        //                        continue;


        //                    // Kuyruk doluysa yeni frame eklemeyi atla
        //                    //if (framePattern[frameCount % framePattern.Count])
        //                    {
        //                        using (Mat processImage = FrameProcessingHelper.NewProcessFrame(frame, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance))
        //                        {
        //                            using (Mat roi = new Mat(processImage, motionRect))
        //                            {
        //                                // Kuyruğa eklemeyi dene
        //                                if (!m_frameQueue.TryAdd(roi.Clone()))
        //                                {
        //                                    // kuyruk dolu, problem değil
        //                                }

        //                                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(diff));

        //                            }
        //                        }
        //                    }

        //                }

        //                frameCount++;

        //                diff.Dispose();
        //                kernel.Dispose();
        //            }

        //            prevGray?.Dispose();
        //            prevGray = gray.Clone();





        //            Thread.Sleep(delay);
        //        }

        //        frame.Dispose();
        //        prevGray?.Dispose();
        //    }
        //}

        //public void ReadProcessDisplayAndEnqueueTestv1(CancellationToken token)
        //{
        //    MainForm.m_mainForm.RequestToStartStream();

        //    using (VideoCapture captureCam = new VideoCapture("edsvideo.mkv"))
        //    {
        //        if (!captureCam.IsOpened())
        //        {
        //            MessageBox.Show("Video açılamadı!");
        //            return;
        //        }

        //        ImageEnhancementHelper.ResetProcessingState();

        //        captureCam.Set(VideoCaptureProperties.PosFrames, currentFramePos);
        //        int fps = (int)captureCam.Get(VideoCaptureProperties.Fps);
        //        int delay = fps > 0 ? 1000 / fps : 30;

        //        Mat frame = new Mat();
        //        Mat prevGray = null;

        //        int frameCount = 0;

        //        // 5 frame'lik örüntü: [al, al, alma, alma, alma]
        //        List<bool> framePattern = new List<bool> { true, true, true, false, false, false };

        //        while (!token.IsCancellationRequested && MainForm.m_mainForm.m_stream)
        //        {
        //            if (!captureCam.Read(frame) || frame.Empty())
        //            {
        //                ImageEnhancementHelper.ResetProcessingState();
        //                Console.WriteLine("Video bitti veya hata oluştu!");
        //                break;
        //            }

        //            currentFramePos = (int)captureCam.Get(VideoCaptureProperties.PosFrames);

        //            Cv2.Resize(frame, frame, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4);

        //            // Görüntüyü ekranda göster
        //            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(frame));

        //            // Görüntüyü griye çevir
        //            Mat gray = new Mat();
        //            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        //            Mat processImage = frame;

        //            if (frameCount % 5 == 0)
        //                processImage = FrameProcessingHelper.NewProcessFrame(frame, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance);


        //            if (prevGray != null)
        //            {
        //                Mat diff = new Mat();
        //                Cv2.Absdiff(prevGray, gray, diff);
        //                Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);

        //                // Gürültüyü azalt
        //                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
        //                Cv2.MorphologyEx(diff, diff, MorphTypes.Close, kernel);

        //                // Hareketli alanları bul
        //                Cv2.FindContours(diff, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        //                foreach (var contour in contours)
        //                {
        //                    Rect motionRect = Cv2.BoundingRect(contour);

        //                    // Çok küçük gürültüleri filtrele
        //                    if (motionRect.Width < 60 || motionRect.Height < 20)
        //                        continue;


        //                    // Kuyruk doluysa yeni frame eklemeyi atla
        //                    //if (framePattern[frameCount % framePattern.Count])
        //                    //{
        //                    //using (Mat processImage = FrameProcessingHelper.NewProcessFrame(frame, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance))
        //                    //{
        //                    using (Mat roi = new Mat(processImage, motionRect))
        //                    {
        //                        // Kuyruğa eklemeyi dene
        //                        if (!m_frameQueue.TryAdd(roi.Clone()))
        //                        {
        //                            // kuyruk dolu, problem değil
        //                        }

        //                        //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox7, BitmapConverter.ToBitmap(diff));

        //                    }

        //                    //}
        //                }



        //                diff.Dispose();
        //                kernel.Dispose();

        //            }

        //            frameCount++;


        //            prevGray?.Dispose();
        //            prevGray = gray.Clone();





        //            Thread.Sleep(delay);
        //        }

        //        frame.Dispose();
        //        prevGray?.Dispose();
        //    }
        //}

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


        //public void OcrPlatesFromQueue(CancellationToken token)
        //{




        //    foreach (var plate in m_plateQueue.GetConsumingEnumerable(token))
        //    {
        //        //ImageAnalysisHelper.OcrPlatesFromQueue(plate);

        //        OcrPlatesFromQueue(plate);
        //    }
        //}



        //public void OcrPlatesFromQueue(PossiblePlate plate)
        //{
        //    ThreadSafeList<CharacterSegmentationResult> possibleCharacters =
        //        Character.SegmentCharactersInPlate(plate);

        //    ThreadSafeList<PlateResult> ts =
        //        Helper.KuyrukRecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);



        //    ocrResultQueue.AddRange(ts);
        //    ocrResultCounter++;


        //    if (ocrResultCounter == 3)
        //    {
        //        FinalizeQueueAndSelectResult();
        //    }

        //}

        //private  void FinalizeQueueAndSelectResult()
        //{
        //    PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(ocrResultQueue);

        //    if (bestPlate != null)
        //    {
        //        ////Debug.WriteLine($"✅ Seçilen plaka: {bestPlate.readingPlateResult} - Güven: {bestPlate.readingPlateResultProbability:F2}");
        //        MainForm.m_mainForm.m_plateResults.Add(bestPlate);

        //        m_onPlateResultReady?.Invoke("Kanal : " + _cameraId.ToString() + " - " + bestPlate.readingPlateResult, 1000);

        //        //DisplayManager.LabelInvoke(MainForm.m_mainForm.label2, bestPlate.readingPlateResult, 1000);
        //    }
        //    else
        //    {
        //        var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(ocrResultQueue);
        //        Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;
        //        List<PlateResult> bestPlates = plateType == Enums.PlateType.Turkish
        //            ? PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity)
        //            : groupPlatesByProximity.SelectMany(g => g).ToList();

        //        foreach (PlateResult besties in bestPlates)
        //        {
        //            MainForm.m_mainForm.m_plateResults.Add(besties);

        //            m_onPlateResultReady?.Invoke("Kanal : " + _cameraId.ToString() + " - " + besties.readingPlateResult, 1000);

        //            //DisplayManager.LabelInvoke(MainForm.m_mainForm.label2, besties.readingPlateResult, 1000);

        //        }
        //    }



        //    ocrResultCounter = 0;
        //    ocrResultQueue.Clear();
        //}

      
    }
}
