using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    class SimpleTracker : IDisposable
    {
        public int Id;
        public Rect TrackRect;              // global ROI
        public Mat mat;


        //for tracking
        public Rect DetectionRect;
        public bool DetectedThisFrame;


        public int Passes;             // ardışık başarılı SVM sayısı
        public int Misses;             // başarısız (OF/SVM) sayısı
        public bool OcrEnqueued;       // OCR’a 1 kez atmak için
        public double LastScore;

        public Point2f[] PrevPts;  // PyrLK noktaları


        public int FrameIndex { get; set; }           // UpdateTrackers'ta tr.FrameIndex = m_frameIndex;
     
        public int FirstSeenFrame { get; set; }
        public int LastSeenFrame { get; set; }

        public int NeedPasses { get; set; }
        public int MaxMisses { get; set; }

        public void MarkPass() => System.Threading.Interlocked.Increment(ref Passes);
        public void ResetPass() => System.Threading.Interlocked.Exchange(ref Passes, 0);
        public void MarkMiss() => System.Threading.Interlocked.Increment(ref Misses);
        public void ResetMiss() => System.Threading.Interlocked.Exchange(ref Misses, 0);
        public bool IsDead() => Misses > MaxMisses;
        public bool IsReadyForOcr() => Passes >= NeedPasses && !OcrEnqueued;



        public int OcrSamplesCap;

        public readonly List<OcrSample> _ocrBuf = new List<OcrSample>(3);
        public struct OcrSample
        {
            public Mat Img144x32;
            public double Sharpness;
            public double SvmScore;
            public int FrameIndex;
            public Rect Rect;

        }




        public bool TryMarkOcrEnqueued()
        {
            if (OcrEnqueued) 
                return false;
            
            OcrEnqueued = true;
            
            return true;
        }
        public void ClearOcrEnqueued() => OcrEnqueued = false;





        public void CommitDetection()
        {
            if (DetectedThisFrame && DetectionRect.Width > 0 && DetectionRect.Height > 0)
                TrackRect = DetectionRect;   // detection bu karede kazanır
           
            DetectedThisFrame = false;        // bayrağı temizle
        }


        public void AddOcrSample(Mat img144x32, double sharp, double svmScore, int frameIdx)
        {
            if (_ocrBuf.Count >= OcrSamplesCap)
            {
                _ocrBuf[0].Img144x32?.Dispose();
                _ocrBuf.RemoveAt(0);
            }
            _ocrBuf.Add(new OcrSample { Img144x32 = img144x32.Clone(), Sharpness = sharp, SvmScore = svmScore, FrameIndex = frameIdx });
        }


        //public void AddOcrSampleWithCapacity(Mat img144x32, double sharp, double score, int frameIdx, int maxBuf)
        //{
        //    // içerde clone ediyorsan burada sadece referansı alabilirsin
        //    _ocrBuf.Add(new OcrSample { Img144x32 = img144x32.Clone(), Sharpness = sharp, SvmScore = score, FrameIndex = frameIdx });

        //    if (_ocrBuf.Count > maxBuf)
        //    {
        //        // “en kötü” örneği çıkar: örn. (Score ağırlıklı ve Sharp ikincil)
        //        int worst = 0; 
        //        double worstKey = double.MaxValue;

        //        for (int i = 0; i < _ocrBuf.Count; i++)
        //        {
        //            // Küçük skor ve küçük keskinlik “kötü”
        //            double key = 0.8 * (1.0 - _ocrBuf[i].SvmScore) + 0.2 * (1.0 / (1.0 + _ocrBuf[i].Sharpness));

        //            if (key < worstKey) 
        //                continue;

        //            worstKey = key; worst = i;
        //        }
        //        _ocrBuf.RemoveAt(worst);
        //    }
        //}

        public void AddOcrSampleWithCapacity(Mat img144x32, double sharpness, double svmScore, int frameIdx, int maxBuf)
        {
            if (img144x32 == null || img144x32.Empty())
                return;

            // Aynı kareden duplicate ekleme (sen zaten dışarıda kontrol ediyorsun ama ek güvenlik):
            if (_ocrBuf.Any(s => s.FrameIndex == frameIdx)) 
                return;

            // Mat mutlaka clone edilerek saklanmalı (yoksa dışarıda dispose olur):
            var clone = img144x32.Clone();

            _ocrBuf.Add(new OcrSample
            {
                Img144x32 = clone,
                Sharpness = sharpness,
                SvmScore = svmScore,
                FrameIndex = frameIdx
            });

            int cap = (maxBuf > 0 ? maxBuf : (OcrSamplesCap > 0 ? OcrSamplesCap : 3));

            if (_ocrBuf.Count <= cap) 
                return;

            // Kapasite aşıldı → “en kötü” örneği çıkar
            int worst = 0;
            double worstKey = double.NegativeInfinity; // daha büyük = daha kötü

            for (int i = 0; i < _ocrBuf.Count; i++)
            {
                // Kötülük ölçütü: düşük SVM ve düşük keskinlik kötü.
                // (Sharpness yoksa sadece SVM’le karar ver)
                double bad = 1.0 - _ocrBuf[i].SvmScore;

                if (_ocrBuf[i].Sharpness > 0)
                    bad = 0.8 * (1.0 - _ocrBuf[i].SvmScore) + 0.2 * (1.0 / (1.0 + _ocrBuf[i].Sharpness));

                if (bad > worstKey) 
                { 
                    worstKey = bad; 
                    worst = i; 
                }
            }

            _ocrBuf[worst].Img144x32?.Dispose(); // sızıntıyı önle
            _ocrBuf.RemoveAt(worst);
        }



        public void AddOcrSampleWithCapacityvOrj(Mat img144x32, double sharpness, double svmScore, int frameIdx, int maxBuf, Rect rect)
        {
            if (img144x32 == null || img144x32.Empty())
                return;

            // Aynı kareden duplicate ekleme (sen zaten dışarıda kontrol ediyorsun ama ek güvenlik):
            if (_ocrBuf.Any(s => s.FrameIndex == frameIdx))
                return;

            // Mat mutlaka clone edilerek saklanmalı (yoksa dışarıda dispose olur):
            var clone = img144x32.Clone();

            _ocrBuf.Add(new OcrSample
            {
                Img144x32 = clone,
                Sharpness = sharpness,
                SvmScore = svmScore,
                FrameIndex = frameIdx,
                Rect = rect
            });

            int cap = (maxBuf > 0 ? maxBuf : (OcrSamplesCap > 0 ? OcrSamplesCap : 3));

            if (_ocrBuf.Count <= cap)
                return;

            // Kapasite aşıldı → “en kötü” örneği çıkar
            int worst = 0;
            double worstKey = double.NegativeInfinity; // daha büyük = daha kötü

            for (int i = 0; i < _ocrBuf.Count; i++)
            {
                // Kötülük ölçütü: düşük SVM ve düşük keskinlik kötü.
                // (Sharpness yoksa sadece SVM’le karar ver)
                double bad = 1.0 - _ocrBuf[i].SvmScore;

                if (_ocrBuf[i].Sharpness > 0)
                    bad = 0.8 * (1.0 - _ocrBuf[i].SvmScore) + 0.2 * (1.0 / (1.0 + _ocrBuf[i].Sharpness));

                if (bad > worstKey)
                {
                    worstKey = bad;
                    worst = i;
                }
            }

            _ocrBuf[worst].Img144x32?.Dispose(); // sızıntıyı önle
            _ocrBuf.RemoveAt(worst);
        }


        public void AddOrReplaceOcrSample(Mat img144x32, double sharpness, double svmScore, int frameIdx, Rect rect, int maxBuf, double iouSameFrameThr = 0.60)
        {
            if (img144x32 == null || img144x32.Empty()) return;

            // Aynı kareden halihazırda sample var mı? (IoU ile kontrol et)
            int sameFrameIdx = -1;
            for (int i = 0; i < _ocrBuf.Count; i++)
            {
                var s = _ocrBuf[i];
                if (s.FrameIndex != frameIdx) continue;

                double iou = RectComparisonHelper.IoU(s.Rect, rect);
                if (iou >= iouSameFrameThr) { sameFrameIdx = i; break; }
            }

            // “İyilik” karşılaştırıcısı: önce SVM, eşitse Sharpness
            bool IsBetter(double newScore, double newSharp, double oldScore, double oldSharp)
                => (newScore > oldScore) || (newScore == oldScore && newSharp > oldSharp);

            var clone = img144x32.Clone();

            if (sameFrameIdx >= 0)
            {
                // Aynı kare + aynı bölge sayılır → iyiyse replace
                var old = _ocrBuf[sameFrameIdx];
                if (IsBetter(svmScore, sharpness, old.SvmScore, old.Sharpness))
                {
                    old.Img144x32?.Dispose();
                    _ocrBuf[sameFrameIdx] = new OcrSample
                    {
                        Img144x32 = clone,
                        Sharpness = sharpness,
                        SvmScore = svmScore,
                        FrameIndex = frameIdx,
                        Rect = rect
                    };
                }
                else
                {
                    clone.Dispose(); // sızıntı yok
                }
                return;
            }

            // Aynı karede benzer bölge yoksa yeni ekle
            _ocrBuf.Add(new OcrSample
            {
                Img144x32 = clone,
                Sharpness = sharpness,
                SvmScore = svmScore,
                FrameIndex = frameIdx,
                Rect = rect
            });

            int cap = (maxBuf > 0 ? maxBuf : (OcrSamplesCap > 0 ? OcrSamplesCap : 3));
            if (_ocrBuf.Count <= cap) return;

            // Kapasite aşıldı → en kötü örneği at
            int worst = 0;
            double worstKey = double.NegativeInfinity;
            for (int i = 0; i < _ocrBuf.Count; i++)
            {
                double bad = 1.0 - _ocrBuf[i].SvmScore;
                if (_ocrBuf[i].Sharpness > 0)
                    bad = 0.8 * (1.0 - _ocrBuf[i].SvmScore) + 0.2 * (1.0 / (1.0 + _ocrBuf[i].Sharpness));

                if (bad > worstKey) { worstKey = bad; worst = i; }
            }
            _ocrBuf[worst].Img144x32?.Dispose();
            _ocrBuf.RemoveAt(worst);
        }



        public bool TryPickBestOcrSample(out OcrSample best)
        {
            if (_ocrBuf.Count == 0) 
            { 
                best = default; 
                return false; 
            }
            
            int bi = 0;
            
            for (int i = 1; i < _ocrBuf.Count; i++)
            {
                //if (_ocrBuf[i].Sharpness > _ocrBuf[bi].Sharpness ||
                //   (_ocrBuf[i].Sharpness == _ocrBuf[bi].Sharpness && _ocrBuf[i].SvmScore > _ocrBuf[bi].SvmScore))
                //    bi = i;
                if (_ocrBuf[i].SvmScore >= _ocrBuf[bi].SvmScore)
                    bi = i;
            }
            best = _ocrBuf[bi];
            return true;
        }

        //private bool ShouldRunGuard() => (FrameIndex - _lastGuardAt) >= GuardIntervalFrames;

        /// <summary>
        /// Tek karelik “takip” adımı: gerekirse feature edin, LK takip et, outlier filtrele, kutuyu taşı.
        /// Başarılıysa true; tolMisses’ı aştıysa false.
        /// </summary>
        public bool StepLK(
            Mat prevGray, Mat currGray, OpenCvSharp.Size imgSize, int frameIdx,
            int minInliers = 6, int tolMisses = 2)
        {
            // ROI güvenli mi? (çağıran genelde garanti eder, yine de defensif davran)
            if (TrackRect.Width <= 0 || TrackRect.Height <= 0)
            {
                MarkMiss();
                return !IsDead();
            }

            // 1) Feature yoksa edin
            if (!EnsureFeatures(prevGray, TrackRect, minInliers))
            {
                MarkMiss();
                return !IsDead();
            }

            // 2) LK takip
            if (!TrackLK(prevGray, currGray, out var goodPrev, out var goodNext))
            {
                MarkMiss();
                return !IsDead();
            }

            // 3) Yeterli eşleşme yok → re-acquire şansı
            if (goodNext.Count < minInliers)
            {
                if (!TryReacquire(currGray, imgSize, minInliers))
                {
                    MarkMiss();
                    return !IsDead();
                }

            
                ResetMiss();

                LastSeenFrame = frameIdx;

             

                // re-acquire olduysa bu karede bbox zorlamıyoruz
                return true;
            }

            // 4) Outlier (MAD) + medyan kayma
            if (!FilterInliersMAD(goodPrev, goodNext, minInliers, out var inPrev, out var inNext))
            {
                MarkMiss();
                return !IsDead();
            }

            if (!ComputeMedianShift(inPrev, inNext, out double dx, out double dy))
            {
                MarkMiss();
                return !IsDead();
            }

            // --- BU ÇOK ÖNEMLİ ---
            var prevRect = TrackRect;  // Guard için şablon burada alınacak

            // 5) Kutu taşı
            if (!UpdateRectByShift(dx, dy, imgSize))
            {
                MarkMiss();
                return !IsDead();
            }


            // 6) Noktaları güncelle, miss sıfırla
            PrevPts = inNext.ToArray();



            ResetMiss();

            LastSeenFrame = frameIdx;

           

            return true;
        }





        // ---------- İç yardımcılar (tracker'a özel) ----------

        public bool EnsureFeatures(Mat grayPrev, Rect roiGlobal, int minInliers)
        {
            if (PrevPts != null && PrevPts.Length >= minInliers) 
                return true;
            
            if (!AcquireFeaturesFromROI(grayPrev, roiGlobal, out var pts)) 
                return false;
            
            PrevPts = pts;
            RefineSubPix(grayPrev, win: 5);
            
            
            return PrevPts != null && PrevPts.Length >= minInliers;
        }


        public bool ReseedFromROI(Mat gray, Rect roi, int minInliers)
        {
            if (!AcquireFeaturesFromROI(gray, roi, out var pts) || pts.Length < minInliers)
                return false;

            PrevPts = pts;
            RefineSubPix(gray, 5);
            ResetMiss();
            return true;
        }

        // ---- Yardımcılar (tracker’a özel) ----

        private bool AcquireFeaturesFromROI(Mat gray, Rect roi, out Point2f[] pts)
        {
            pts = null;

            if (roi.Width <= 0 || roi.Height <= 0) 
                return false;

            using var roiMat = new Mat(gray, roi);

            //var local = Cv2.GoodFeaturesToTrack(
            //    roiMat,
            //    maxCorners: 100,
            //    qualityLevel: 0.015,
            //    minDistance: 5,
            //    mask: null,
            //    blockSize: 3,
            //    useHarrisDetector: false,
            //    k: 0.04
            //);

            var local = Cv2.GoodFeaturesToTrack(
               roiMat,
               maxCorners: 150,
               qualityLevel: 0.010,
               minDistance: 7,
               mask: null,
               blockSize: 5,
               useHarrisDetector: false,
               k: 0.06
           );


            if (local == null || local.Length == 0) 
                    return false;

            // ROI -> global koordinat
            for (int k = 0; k < local.Length; k++)
                local[k] = new Point2f(local[k].X + roi.X, local[k].Y + roi.Y);

            pts = local;
            return true;
        }

        public void RefineSubPix(Mat gray, int win = 5)
        {
            if (PrevPts == null || PrevPts.Length == 0) 
                return;
            
            int W = gray.Cols, H = gray.Rows;

            var safe = new List<Point2f>(PrevPts.Length);
            
            foreach (var p in PrevPts)
            {
                if (p.X >= win && p.Y >= win && p.X < W - win && p.Y < H - win)
                    safe.Add(p);
            }

            if (safe.Count == 0) 
                return;

            var arr = safe.ToArray();
            
            Cv2.CornerSubPix(
                gray,
                arr,
                new OpenCvSharp.Size(win, win),
                new OpenCvSharp.Size(-1, -1),
                new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.01)
            );


            PrevPts = arr;
        }

        public void RefineSubPixv(Mat gray, int win = 5)
        {
            if (PrevPts == null || PrevPts.Length == 0) return;
            if (gray.Channels() != 1 || (gray.Type() != MatType.CV_8U && gray.Type() != MatType.CV_32F)) return;

            int W = gray.Cols, H = gray.Rows;
            int halfWin = Math.Max(2, win);

            var safe = new List<Point2f>(PrevPts.Length);
            foreach (var p in PrevPts)
            {
                if (double.IsNaN(p.X) || double.IsNaN(p.Y) || double.IsInfinity(p.X) || double.IsInfinity(p.Y))
                    continue;

                if (p.X >= halfWin && p.Y >= halfWin && p.X < W - halfWin && p.Y < H - halfWin)
                    safe.Add(p);
            }
            if (safe.Count == 0) return;

            var arr = safe.ToArray();
            Cv2.CornerSubPix(
                gray, arr,
                new OpenCvSharp.Size(halfWin, halfWin),
                new OpenCvSharp.Size(-1, -1),
                new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.01)
            );
            PrevPts = arr;
        }


        private bool TrackLK(Mat prevGray, Mat currGray, out List<Point2f> goodPrev, out List<Point2f> goodNext)
        {
            goodPrev = new List<Point2f>();
            goodNext = new List<Point2f>();

            if (PrevPts == null || PrevPts.Length == 0) 
                return false;

            using var prevPtsMat = ToMat(PrevPts);
            using var nextPtsMat = new Mat(); // Nx1, CV_32FC2
            using var statusMat = new Mat();  // Nx1, CV_8UC1
            using var errMat = new Mat();     // Nx1, CV_32FC1

            Cv2.CalcOpticalFlowPyrLK(
                prevGray, currGray,
                prevPtsMat, nextPtsMat, statusMat, errMat,
                new OpenCvSharp.Size(25, 25), 5,
                new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.01),
                OpticalFlowFlags.None,
                minEigThreshold: 1e-4

            );

            if (nextPtsMat.Empty() || statusMat.Empty())
                return false;

            int nRows = nextPtsMat.Rows;
            var nextIdx = nextPtsMat.GetGenericIndexer<Vec2f>();
            var statusIdx = statusMat.GetGenericIndexer<byte>();

            int m = Math.Min(PrevPts.Length, nRows);
            
            for (int k = 0; k < m; k++)
            {
                if (statusIdx[k, 0] != 0)
                {
                    var v = nextIdx[k, 0];
                    goodPrev.Add(PrevPts[k]);
                    goodNext.Add(new Point2f(v.Item0, v.Item1));
                }
            }
            return true;
        }

        private bool FilterInliersMAD(List<Point2f> prev, List<Point2f> next, int minInliers, out List<Point2f> inPrev, out List<Point2f> inNext)
        {
            inPrev = new List<Point2f>();
            inNext = new List<Point2f>();

            if (next.Count < minInliers) 
                return false;

            int n = next.Count;
            var dx = new double[n];
            var dy = new double[n];

            for (int i = 0; i < n; i++)
            {
                dx[i] = next[i].X - prev[i].X;
                dy[i] = next[i].Y - prev[i].Y;
            }

            double mdx = Median(dx), mdy = Median(dy);
            double madx = Median(dx.Select(v => Math.Abs(v - mdx)).ToArray()) + 1e-6;
            double mady = Median(dy.Select(v => Math.Abs(v - mdy)).ToArray()) + 1e-6;

            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(dx[i] - mdx) <= 3 * madx &&
                    Math.Abs(dy[i] - mdy) <= 3 * mady)
                {
                    inPrev.Add(prev[i]);
                    inNext.Add(next[i]);
                }
            }

            return inNext.Count >= minInliers;
        }

        private bool ComputeMedianShift(List<Point2f> inPrev, List<Point2f> inNext, out double dx, out double dy)
        {
            dx = 0; dy = 0;

            if (inNext.Count == 0 || inPrev.Count != inNext.Count) 
                return false;

            int n = inNext.Count;
            var sdx = new double[n];
            var sdy = new double[n];

            for (int i = 0; i < n; i++)
            {
                sdx[i] = inNext[i].X - inPrev[i].X;
                sdy[i] = inNext[i].Y - inPrev[i].Y;
            }

            dx = Median(sdx);
            dy = Median(sdy);
            
            return true;
        }

        //private bool UpdateRectByShift(double dx, double dy, OpenCvSharp.Size imgSize)
        //{
        //    int newX = (int)Math.Round(TrackRect.X + dx);
        //    int newY = (int)Math.Round(TrackRect.Y + dy);

        //    newX = Math.Max(0, Math.Min(newX, imgSize.Width - TrackRect.Width));
        //    newY = Math.Max(0, Math.Min(newY, imgSize.Height - TrackRect.Height));

        //    var newRect = new Rect(newX, newY, TrackRect.Width, TrackRect.Height);

        //    if (newRect.Width <= 0 || newRect.Height <= 0)
        //        return false;

        //    TrackRect = newRect;
        //    return true;
        //}
     

        private bool UpdateRectByShift(double dx, double dy, OpenCvSharp.Size imgSize)
        {
            var cx = TrackRect.X + TrackRect.Width / 2.0 + dx;
            var cy = TrackRect.Y + TrackRect.Height / 2.0 + dy;

            int newX = (int)Math.Round(cx - TrackRect.Width / 2.0);
            int newY = (int)Math.Round(cy - TrackRect.Height / 2.0);

            newX = Math.Max(0, Math.Min(newX, imgSize.Width - TrackRect.Width));
            newY = Math.Max(0, Math.Min(newY, imgSize.Height - TrackRect.Height));

            var newRect = new Rect(newX, newY, TrackRect.Width, TrackRect.Height);

            if (newRect.Width <= 0 || newRect.Height <= 0)
                return false;

            TrackRect = newRect;
            return true;
        }

        private bool UpdateRectByShiftv4(double dx, double dy, OpenCvSharp.Size imgSize, int padX = 10)
        {
            // 1) Yeni merkez (öteleme)
            double cx = TrackRect.X + TrackRect.Width / 2.0 + dx;
            double cy = TrackRect.Y + TrackRect.Height / 2.0 + dy;

            // 2) Hedef genişlik (soldan+sağdan padX)
            int targetW = TrackRect.Width + 2 * padX;
            int targetH = TrackRect.Height; // sadece yatay genişletme isteniyor

            // 3) Sol-üst köşe (merkezden)
            int x = (int)Math.Round(cx - targetW / 2.0);
            int y = (int)Math.Round(cy - targetH / 2.0);

            // 4) Görüntü sınırlarına uydur (genişlik taşarsa daralt)
            // X ve Width
            if (targetW > imgSize.Width)
            {
                // resimden büyükse: tüm genişliği kapla
                x = 0;
                targetW = imgSize.Width;
            }
            else
            {
                x = Math.Max(0, Math.Min(x, imgSize.Width - targetW));
            }

            // Y ve Height (yükseklik değişmedi ama yine de sınırla)
            if (targetH > imgSize.Height)
            {
                y = 0;
                targetH = imgSize.Height;
            }
            else
            {
                y = Math.Max(0, Math.Min(y, imgSize.Height - targetH));
            }

            if (targetW <= 0 || targetH <= 0) return false;

            TrackRect = new Rect(x, y, targetW, targetH);
            return true;
        }


        public bool TryReacquire(Mat gray, OpenCvSharp.Size imgSize, int minInliers, int shrink = 2)
        {
            // Daraltılmış ROI
            var refill = InflateClamp(TrackRect, -shrink, -shrink, imgSize);

            if (!AcquireFeaturesFromROI(gray, refill, out var pts) || pts.Length < minInliers)
                return false;

            PrevPts = pts;
            RefineSubPix(gray, 5);
            return true;
        }


        //double GUARD_THRESH = 0.75;   // Eşik
        //double SEARCH_SCALE = 1.6;    // Arama bölgesini 1.6x genişlet

        //public void GuardZNCC(Mat gray, Mat prevGray, ref Rect trackRect, OpenCvSharp.Size imgSize, Rect? templateRect = null)
        //{
        //    var templRect = templateRect ?? trackRect;          // varsayılan eski sürümle uyumlu
        //    templRect = ClampRect(templRect, imgSize);
        //    if (templRect.Width < 8 || templRect.Height < 8) return;

        //    using var templ = prevGray[templRect];              // ŞABLON: prevGray'den

        //    var search = ExpandRect(trackRect, imgSize, SEARCH_SCALE); // ARAMA: currGray etrafı
        //    using var searchROI = gray[search];

        //    using var res = new Mat();
        //    Cv2.MatchTemplate(searchROI, templ, res, TemplateMatchModes.CCoeffNormed);
        //    Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

        //    if (maxVal >= GUARD_THRESH)
        //    {
        //        var found = new Rect(search.X + maxLoc.X, search.Y + maxLoc.Y, templ.Cols, templ.Rows);
        //        trackRect = ClampRect(found, imgSize);
        //    }
        //}

        // ---- Mini yardımcılar ----

        private static Mat ToMat(Point2f[] pts)
        {
            var m = new Mat(pts.Length, 1, MatType.CV_32FC2);
            var idx = m.GetGenericIndexer<Vec2f>();

            for (int i = 0; i < pts.Length; i++)
                idx[i, 0] = new Vec2f(pts[i].X, pts[i].Y);
            
            return m;
        }

        private static Rect InflateClamp(Rect r, int growX, int growY, OpenCvSharp.Size size)
        {
            int x = Math.Max(0, r.X - growX);
            int y = Math.Max(0, r.Y - growY);
            int w = Math.Min(size.Width - x, r.Width + 2 * growX);
            int h = Math.Min(size.Height - y, r.Height + 2 * growY);


            if (w < 1) 
                w = 1;
            
            if (h < 1) 
                h = 1;

            return new Rect(x, y, w, h);
        }

        private static double Median(IList<double> xs)
        {
            if (xs == null || xs.Count == 0) 
                return 0;
            
            var a = xs.OrderBy(v => v).ToArray();
            int n = a.Length;
            
            return (n % 2 == 1) ? a[n / 2] : 0.5 * (a[n / 2 - 1] + a[n / 2]);
        }

        public void ClearOcrBuffer()
        {
            foreach (var s in _ocrBuf) s.Img144x32?.Dispose();
            _ocrBuf.Clear();
        }

        public void Dispose() => ClearOcrBuffer();

        // Yardımcılar:
        private static Rect ClampRect(Rect r, OpenCvSharp.Size s)
        {
            int x = Math.Max(0, Math.Min(r.X, s.Width - r.Width));
            int y = Math.Max(0, Math.Min(r.Y, s.Height - r.Height));
            int w = Math.Min(r.Width, s.Width);
            int h = Math.Min(r.Height, s.Height);
            return new Rect(x, y, w, h);
        }

        private static Rect ExpandRect(Rect r, OpenCvSharp.Size s, double scale)
        {
            double cx = r.X + r.Width / 2.0;
            double cy = r.Y + r.Height / 2.0;
            int w = (int)Math.Round(r.Width * scale);
            int h = (int)Math.Round(r.Height * scale);
            int x = (int)Math.Round(cx - w / 2.0);
            int y = (int)Math.Round(cy - h / 2.0);
            var R = new Rect(x, y, w, h);
            return ClampRect(R, s);
        }
    }




}




