using Accord;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal partial class HybridPlateReadingStrategy : IPlateReadingStrategy
    {

        private void SeedTracker(Rect r, double seedScore, OpenCvSharp.Size frameSize, Mat currGrayFull, Mat potentialPlate, int frameIdx)
        {
            var bounds = new Rect(0, 0, frameSize.Width, frameSize.Height);

            // 1) SeedRect'i güvenli al (sıkı kutu)
            var seedRect = r.Intersect(bounds);

            if (seedRect.Width <= 0 || seedRect.Height <= 0)
                return;


            var trackRect = seedRect;

            //var trackRect = RectGeometryHelper.GrowRectAdaptive(seedRect, frameSize);   // ← büyüt

            // 3) 144x32 normalize örnek (AddOcrSample içeride clone ediyor)
            //using var sample = potentialPlate.Clone();
            //Cv2.Resize(sample, sample, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);


            // 5) Tracker nesnesini hazırla (Passes=0 ile başla)
            var tp = new SimpleTracker
            {
                Id = System.Threading.Interlocked.Increment(ref _nextId),
                TrackRect = trackRect,      // kanonik
                DetectionRect = seedRect,
                DetectedThisFrame = true,


                Passes = 0,
                Misses = 0,
                OcrEnqueued = false,
                LastScore = seedScore,
                PrevPts = null,
                FrameIndex = frameIdx,
                FirstSeenFrame = frameIdx,
                LastSeenFrame = frameIdx,
                OcrSamplesCap = m_OcrSamplesCap,
                NeedPasses = _needPasses,
                MaxMisses = _maxMisses
            };

            //tp.EnsureFeatures(currGrayFull, trackRect, minInliers: 6);

            // 6) İlk feature'lar (kilit DIŞI) → TrackRect üzerinden
            //if (_prevGrayFull != null)
            //    tp.EnsureFeatures(_prevGrayFull, trackRect, minInliers: 5);

            //// 7) Seed örneğini tp'nin küçük tamponuna ekle (tp tarafında AddOcrSample clone ediyor)
            //tp.AddOcrSample(sample, sharp: 0, seedScore, frameIdx);



            // 8) Son duplicate kontrolü + listeye ekleme (kısa kilit)

            _tracked.Add(tp);

        }

        int FindBestTrackerMatch(Rect det, ThreadSafeList<SimpleTracker> tracked, double iouThr = 0.6, double maxCenter = 30.0)
        {
            var snap = tracked.Snapshot();
            int bestIdx = -1;
            double bestScore = 0;

            for (int i = 0; i < snap.Length; i++)
            {
                var tr = snap[i];

                if (tr.IsDead())
                    continue;

                var iou = RectComparisonHelper.IoU(det, tr.TrackRect);

                double score = iou;

                if (iou < iouThr)
                {
                    var cd = RectGeometryHelper.CenterDist(det, tr.TrackRect);
                    if (cd <= maxCenter)
                        score = 0.5 + 1.0 / (1.0 + cd); // zayıf fallback
                    else
                        continue;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }


        int FindBestTrackerMatchGated(
    Rect det,
    ThreadSafeList<SimpleTracker> tracked,
    int frameIdx,
    double iouMin = 0.30,          // IoU alt eşiği
    double maxCenterNorm = 0.45,   // normalize merkez mesafesi eşiği
    double maxScaleLog = 0.5878,   // ~ log(1.8)  → %80 ölçek farkına kadar tolere
    int maxAge = 10                // bu kadar kare görülmemişse eşleşme arama
)
        {
            var snap = tracked.Snapshot();

            int bestId = -1;
            double bestScore = double.NegativeInfinity;

            // det için özellikler
            double cdx = det.X + det.Width / 2.0;
            double cdy = det.Y + det.Height / 2.0;
            double diagDet = Math.Sqrt(det.Width * det.Width + det.Height * det.Height);
            double areaDet = Math.Max(1, det.Width * det.Height);

            for (int i = 0; i < snap.Length; i++)
            {
                var tr = snap[i];

                if (tr.IsDead()) continue;
                if (tr.OcrEnqueued) continue; // OCR'a gidenleri yeniden bağlama
                if (tr.DetectedThisFrame && tr.LastSeenFrame == frameIdx) continue; // aynı karede iki kez işaretlenmesin

                int age = frameIdx - tr.LastSeenFrame;
                if (age > maxAge) continue;

                var trr = tr.TrackRect;

                double iou = RectComparisonHelper.IoU(det, trr);

                // normalize merkez mesafesi (iki kutunun ortalama diyagonaline böl)
                double ctx = trr.X + trr.Width / 2.0;
                double cty = trr.Y + trr.Height / 2.0;
                double centerDist = Math.Sqrt((cdx - ctx) * (cdx - ctx) + (cdy - cty) * (cdy - cty));

                double diagTr = Math.Sqrt(trr.Width * trr.Width + trr.Height * trr.Height);
                double centerNorm = centerDist / Math.Max(1.0, 0.5 * (diagDet + diagTr));

                // ölçek farkı (alan oranının log mutlak değeri)
                double areaTr = Math.Max(1, trr.Width * trr.Height);
                double scaleLog = Math.Abs(Math.Log(areaDet / areaTr));

                // OR-gating: IoU düşükse merkez çok da uzak olmamalı
                if (iou < iouMin && centerNorm > maxCenterNorm) continue;
                if (scaleLog > maxScaleLog) continue;

                // Skor: IoU ödüllendir, merkez/ölçek/yaş cezalandır
                double score = 2.0 * iou - 1.0 * centerNorm - 0.5 * scaleLog - 0.02 * age;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = tr.Id;
                }
            }

            if (bestId < 0) return -1;

            // Canlı listedeki indeksi döndür (snapshot indexine güvenmeyelim)
            int liveIdx = tracked.FindIndex(t => t.Id == bestId);
            return liveIdx;
        }

        private void DetectAndAssociate(FrameWithRoi f, Mat currBgr, Mat currGrayFull, int frameIdx)
        {
            if (f.Rects == null || f.Rects.Count == 0)
                return;

            foreach (var r in f.Rects)
            {
                var roiSafe = RectGeometryHelper.Clip(r, currBgr.Cols, currBgr.Rows);

                if (roiSafe.Width <= 0 || roiSafe.Height <= 0)
                    continue;

                using var roiBgr = new Mat(currBgr, roiSafe);

                var plates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roiBgr);

                if (plates == null || plates.Count == 0)
                    continue;

                foreach (var p in plates)
                {
                    var g = new Rect(roiSafe.X + p.addedRects.X, roiSafe.Y + p.addedRects.Y, p.addedRects.Width, p.addedRects.Height);
                    var gSafe = RectGeometryHelper.Clip(g, currBgr.Cols, currBgr.Rows);

                    if (gSafe.Width <= 0 || gSafe.Height <= 0)
                        continue;

                    //int matchIdx = FindBestTrackerMatch(gSafe, _tracked, iouThr: 0.6, maxCenter: 30);

                    int matchIdx = FindBestTrackerMatchGated(gSafe, _tracked, frameIdx);

                    if (matchIdx >= 0)
                    {
                        var tr = _tracked[matchIdx];

                        bool alreadyMarkedThisFrame = (tr.LastSeenFrame == frameIdx) && tr.DetectedThisFrame;

                        if (alreadyMarkedThisFrame)
                        {
                            if (p.PlateScore > tr.LastScore)
                            {
                                tr.DetectionRect = gSafe;
                                tr.LastScore = p.PlateScore;
                            }
                        }
                        else
                        {
                            tr.DetectedThisFrame = true;
                            tr.DetectionRect = gSafe;
                            tr.LastScore = p.PlateScore;

                            tr.TrackRect = gSafe;

                            tr.ResetMiss();
                            tr.LastSeenFrame = frameIdx;

                            // İstersen koşullu reseed (SimpleTracker API'si varsa):
                            // if (tr.PrevPts == null || tr.PrevPts.Length < 6)
                            //     tr.ReseedFromROI(currGrayFull, gSafe, minInliers: 6);
                        }

                        _tracked[matchIdx] = tr;

                        //Debug.WriteLine("Track edilecek plaka alanı bulundu. - Frame : " + frameIdx.ToString());

                        // Debug:
                        //using var loo = new Mat(currGrayFull, tr.DetectionRect);
                        //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, loo.ToBitmap());
                    }
                    else
                    {
                        //Debug.WriteLine("Seed edilecek plaka alanı bulundu. - Frame : " + frameIdx.ToString());


                        using var seedCropGray = new Mat(currGrayFull, gSafe);
                        SeedTracker(gSafe, p.PlateScore, currBgr.Size(), currGrayFull, seedCropGray, frameIdx);
                        DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxPlateSeed, seedCropGray.ToBitmap());
                    }
                }
            }
        }

        //private void UpdateTrackers(Mat prevGray, Mat currGray, ThreadSafeList<SimpleTracker> trackers, int frameIdx)
        //{
        //    if (prevGray == null || prevGray.Empty() || currGray == null || currGray.Empty())
        //        return;

        //    var imgSize = currGray.Size();

        //    // 1) Hızlı erişim için id->index haritası (isteğe bağlı optimizasyon)
        //    var snap = trackers.Snapshot();

        //    var idToIdx = new Dictionary<int, int>(snap.Length);

        //    for (int i = 0; i < snap.Length; i++)
        //    {
        //        idToIdx[snap[i].Id] = trackers.FindIndex(tr => tr.Id == snap[i].Id); // ya da ThreadSafeList uygun metodu
        //    }

        //    foreach (var t in snap)
        //    {
        //        // Bu karede deteksiyonla zaten güncellendiyse → optiği atla
        //        if (t.LastSeenFrame == frameIdx)
        //            continue;

        //        if (!idToIdx.TryGetValue(t.Id, out int idx) || idx < 0)
        //            continue;

        //        var tr = trackers[idx];

        //        // Ölü veya tolerans aşılmışsa atla (tercihe bağlı)
        //        if (tr.IsDead())
        //            continue;

        //        // LK adımı
        //        bool alive = tr.StepLK(prevGray, currGray, imgSize, frameIdx, minInliers: 6, tolMisses: _maxMisses);

        //        if (alive)
        //        {
        //            // Sadece başarılı adımda "bu karede görüldü" işaretle
        //            //tr.LastSeenFrame = frameIdx;
        //            // tr.TrackRect zaten StepLK içinde güncellenmiş olmalı (flow centroid/median ile)

        //            //Debug.WriteLine("Optical flow plaka alanını takip etti. - Frame : " + frameIdx.ToString());
        //        }
        //        else
        //        {
        //            // Misses içeride artıyorsa burada sadece bırak; 
        //            // hard-case: re-anchor bayrağı vs. set edebilirsin
        //            // tr.NeedsReanchor = true; (opsiyon)
        //        }

        //        trackers[idx] = tr;
        //    }
        //}

        private void UpdateTrackers(Mat prevGray, Mat currGray, ThreadSafeList<SimpleTracker> trackers, int frameIdx)
        {
            if (prevGray == null || prevGray.Empty() || currGray == null || currGray.Empty())
                return;

            var imgSize = currGray.Size();

            // 1) Canlı listedeki indexleri tek geçişte haritalandır (O(N))
            var idToIdx = new Dictionary<int, int>(trackers.Count);
            for (int i = 0; i < trackers.Count; i++)
                idToIdx[trackers[i].Id] = i;

            // 2) Snapshot üzerinden güvenli iterasyon
            var snap = trackers.Snapshot();

            for (int s = 0; s < snap.Length; s++)
            {
                var sid = snap[s].Id;
                if (!idToIdx.TryGetValue(sid, out int idx) || idx < 0)
                    continue;

                var tr = trackers[idx];

                // Ölü/bozuk track’leri atla (StepLK de korur ama erken çıkış iyi)
                if (tr.IsDead())
                    continue;

                // --- Detection geldiyse bu karede LK’yi atla ---
                // “Detection ile güncellendi”nin güvenli tanımı:
                //  - tr.DetectedThisFrame == true
                //  - tr.LastSeenFrame == frameIdx  (bu karede gerçekten görüldü)
                bool detHit = tr.DetectedThisFrame && (tr.LastSeenFrame == frameIdx);
                bool validDetRect = tr.DetectionRect.Width > 0 && tr.DetectionRect.Height > 0;

                if (detHit && validDetRect)
                {
                    // Bu karede kutuyu detection’a “snap” edeceksin (frame sonunda CommitDetection),
                    // burada sadece bir SONRAKİ kare için feature’ları currGray üzerinde tazele/edin.
                    var featRect = tr.DetectionRect;

                    // Yeterli inlier yoksa TrackRect’i dene (çok dar detection’larda işe yarar)
                    if (!tr.EnsureFeatures(currGray, featRect, minInliers: 6))
                        tr.EnsureFeatures(currGray, tr.TrackRect, minInliers: 6);

                    trackers[idx] = tr;
                    continue; // LK yok
                }

                // Detection yoksa/invalid ise → LK ile takip et (StepLK içi Pass/Miss/LastSeenFrame’i yönetiyor)
                bool alive = tr.StepLK(prevGray, currGray, imgSize, frameIdx, minInliers: 6, tolMisses: _maxMisses);

                // alive olmasa da StepLK Miss/Dead durumunu içeride güncelledi.
                trackers[idx] = tr;
            }
        }


        private int PruneTrackers(ThreadSafeList<SimpleTracker> trackers, int frameIdx, int staleTtl, int ocrTtl, int warmupMinAge)
        {
            var snap = trackers.Snapshot();
            var idsToRemove = new List<int>(snap.Length);

            foreach (var t in snap)
            {
                // Bu karede görüldüyse asla prune etme
                if (t.LastSeenFrame == frameIdx)
                    continue;

                // Yaşlar (guard’lı)
                int ageSinceFirst = Math.Max(0, frameIdx - t.FirstSeenFrame);
                int ageSinceLast = Math.Max(0, frameIdx - t.LastSeenFrame);

                bool invalidRect = (t.TrackRect.Width <= 0 || t.TrackRect.Height <= 0);
                bool tooManyMisses = t.IsDead();
                bool tooStale = (t.LastSeenFrame > 0) && (ageSinceLast > staleTtl);
                bool retireAfterOcr = t.OcrEnqueued && (ageSinceLast > ocrTtl);
                //bool retireAfterOcr = t.OcrEnqueued;

                bool inWarmup = (t.FirstSeenFrame > 0) && (ageSinceFirst < warmupMinAge);

                //// Isınma koruması: çok genç tracker’ları acele silme
                //if (!invalidRect && ageSinceFirst < warmupMinAge)
                //    continue;

                //if (invalidRect || tooManyMisses || tooStale || retireAfterOcr)
                //    idsToRemove.Add(t.Id);

                // 1) Her koşulda silinecekler
                if (invalidRect || tooManyMisses)
                {
                    idsToRemove.Add(t.Id);
                    //Debug.WriteLine($"PRUNE id={t.Id} reason={(invalidRect ? "invalidRect" : "tooManyMisses")} ageLast={ageSinceLast}");
                    continue;
                }

                // 2) Warmup koruması: sadece STALE’i ertele (miss/invalid zaten yukarıda elendi)
                if (inWarmup)
                    continue;

                // 3) Stale / OCR TTL
                if (tooStale || retireAfterOcr)
                {
                    idsToRemove.Add(t.Id);
                    //Debug.WriteLine($"PRUNE id={t.Id} reason={(tooStale ? "stale" : "ocrTtl")} ageLast={ageSinceLast}");
                }
            }

            // Güvenli kaldırma
            foreach (var id in idsToRemove)
            {
                int idx = trackers.FindIndex(tr => tr.Id == id);

                if (idx >= 0)
                {
                    var tr = trackers[idx];
                    tr.Dispose();            // _ocrBuf içindeki Mat’leri de Dispose ettiğinden emin ol
                    trackers.RemoveAt(idx);
                }
            }

            return idsToRemove.Count;
        }

        private void EvaluateTrackersForOcrvOrj(Mat currGrayFull, Mat currBgr, int frameIdx)
        {
            var snap = _tracked.Snapshot();

            for (int s = 0; s < snap.Length; s++)
            {
                var tId = snap[s].Id;
                int idx = _tracked.FindIndex(tr => tr.Id == tId);

                if (idx < 0)
                    continue;
                
                var tracker = _tracked[idx];

                if (tracker.OcrEnqueued)
                {
                    tracker.CommitDetection();   // detection varsa TrackRect’e commit et + flag’i temizle
                    _tracked[idx] = tracker;
                    continue;
                }

                // Crop seçimi (detection varsa onu tercih et)
                var cropRect = tracker.DetectedThisFrame ? tracker.DetectionRect : tracker.TrackRect;
                cropRect = RectGeometryHelper.Clip(cropRect, currGrayFull.Cols, currGrayFull.Rows);

                if (cropRect.Width <= 0 || cropRect.Height <= 0)
                {
                    tracker.DetectedThisFrame = false;
                    _tracked[idx] = tracker;
                    continue;
                }
               
                bool hitThisFrame = (tracker.LastSeenFrame == frameIdx); // detection veya LK successful

                if (hitThisFrame /* && tracker.LastScore >= _svmTrackThr*/) // istersen burada skor eşiği uygula
                {
                    using var svmCrop = new Mat(currGrayFull, cropRect);
                    Cv2.Resize(svmCrop, svmCrop, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);

                    var result = SVMHelper.AskSVMPredictionForPlateRegionWithScore(MainForm.m_mainForm.m_loadedSvmForPlateRegion, svmCrop, 0);
                    tracker.LastScore = result.score;


                    tracker.MarkPass();

                    bool hasThisFrame = tracker._ocrBuf.Any(s => s.FrameIndex == frameIdx);

                    //if (!hasThisFrame)
                    //    tracker.AddOcrSampleWithCapacity(svmCrop, sharpness: 0, tracker.LastScore, frameIdx, tracker.OcrSamplesCap);

                    if (!hasThisFrame)
                        // tracker.AddOcrSampleWithCapacity((svmCrop, sharpness: 0, tracker.LastScore, frameIdx, tracker.OcrSamplesCap,tracker.TrackRect);
                        tracker.AddOrReplaceOcrSample(svmCrop, sharpness: 0, tracker.LastScore, frameIdx, tracker.TrackRect, tracker.OcrSamplesCap);



                    if (tracker.IsReadyForOcr() && tracker.TryMarkOcrEnqueued())
                    {
                        if (tracker.TryPickBestOcrSample(out SimpleTracker.OcrSample best)) // 0->1 atomik geçiş
                        {
                            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, best.Img144x32.ToBitmap());

                            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM1, tracker._ocrBuf[0].Img144x32.ToBitmap());
                            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM2, tracker._ocrBuf[1].Img144x32.ToBitmap());
                            DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM3, tracker._ocrBuf[2].Img144x32.ToBitmap());


                            //string zo = string.Format("1. item : {0} - 2. item : {1} - 3. item : {2}", tracker._ocrBuf[0].FrameIndex,
                            //    tracker._ocrBuf[1].FrameIndex, tracker._ocrBuf[2].FrameIndex);

                            //Debug.WriteLine(zo);

                            //Debug.WriteLine($"ENQ id={tracker.Id} thr={System.Threading.Thread.CurrentThread.ManagedThreadId} "
                            //       + $"buf=({tracker._ocrBuf[0].FrameIndex},{tracker._ocrBuf[0].Rect.Location}," +
                            //       $"{tracker._ocrBuf[1].FrameIndex},{tracker._ocrBuf[1].Rect.Location}," +
                            //       $"{tracker._ocrBuf[2].FrameIndex},{tracker._ocrBuf[2].Rect.Location}) f={frameIdx}");

                            Debug.WriteLine($"ENQ id={tracker.Id} thr={System.Threading.Thread.CurrentThread.ManagedThreadId} "
                                  + $"buf=({tracker._ocrBuf[0].FrameIndex}" + " "+
                                  $"{tracker._ocrBuf[1].FrameIndex}" + " " +
                                  $"{tracker._ocrBuf[2].FrameIndex}) f={frameIdx}");


                            // Kuyruğa ekle (frame klonlama + crop görseli)
                            EnqueueBestPlate(currBgr, cropRect, tracker.LastScore, best);
                        }

                        else
                        {
                            tracker.ClearOcrEnqueued(); // ← kritik: kilidi geri bırak
                        }
                    }
                   

                            
                }
                else
                {
                    // Ardışık istiyorsan:
                    tracker.ResetPass();

                    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxNotPlate, svmCrop.ToBitmap());
                }

                // Bu kare bitti: detection bayrağını sıfırla (frame sonu reset'te de yapılabilir)
                //tracker.DetectedThisFrame = false;

                tracker.CommitDetection();

                _tracked[idx] = tracker;
            }
        }
        private void EvaluateTrackersForOcr(Mat currGrayFull, Mat currBgr, int frameIdx)
        {
            if (currGrayFull == null || currGrayFull.Empty()) return;

            // Aynı karede aynı plakayı ikinci kez OCR’a sokmayı önlemek için
            var enqueuedThisFrame = new List<Rect>();

            var snap = _tracked.Snapshot();

            for (int s = 0; s < snap.Length; s++)
            {
                var tId = snap[s].Id;
                int idx = _tracked.FindIndex(tr => tr.Id == tId);
                if (idx < 0) continue;

                var tracker = _tracked[idx];

                // Zaten OCR kuyruğuna işaretlenmişse: sadece detection'ı TrackRect'e commit et, devam etme
                if (tracker.OcrEnqueued)
                {
                    tracker.CommitDetection();   // (valid ise) TrackRect = DetectionRect; flag temizlenir
                    _tracked[idx] = tracker;
                    continue;
                }

                // Crop seçimi (detection varsa onu, yoksa track)
                var cropRect = tracker.DetectedThisFrame ? tracker.DetectionRect : tracker.TrackRect;
                cropRect = RectGeometryHelper.Clip(cropRect, currGrayFull.Cols, currGrayFull.Rows);

                // Geçersiz crop → bu kareyi pas geç (detection flag'i de temizle)
                if (cropRect.Width <= 0 || cropRect.Height <= 0)
                {
                    tracker.CommitDetection();
                    _tracked[idx] = tracker;
                    continue;
                }

                // Bu karede görüldü mü? (detection ya da LK başarılı)
                bool hitThisFrame = (tracker.LastSeenFrame == frameIdx);

                if (hitThisFrame) // ağır işleri sadece hit olduğunda çalıştır
                {
                    using var svmCrop = new Mat(currGrayFull, cropRect);
                    Cv2.Resize(svmCrop, svmCrop, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);

                    var result = SVMHelper.AskSVMPredictionForPlateRegionWithScore(
                                     MainForm.m_mainForm.m_loadedSvmForPlateRegion, svmCrop, 0);
                    tracker.LastScore = result.score;

                    tracker.MarkPass();

                    // Aynı karede gelen sample için add-or-replace (frame+IoU ile tekilleştirir)
                    tracker.AddOrReplaceOcrSample(
                        img144x32: svmCrop,
                        sharpness: 0,
                        svmScore: tracker.LastScore,
                        frameIdx: frameIdx,
                        rect: cropRect,
                        maxBuf: tracker.OcrSamplesCap
                    );

                    // OCR'a hazırsa 0->1 atomik geçiş dene
                    if (tracker.IsReadyForOcr() && tracker.TryMarkOcrEnqueued())
                    {
                        if (tracker.TryPickBestOcrSample(out SimpleTracker.OcrSample best) && best.Img144x32 != null)
                        {
                            // Per-frame mekânsal dedup: aynı karede aynı plakayı ikinci kez enqueuelama
                            double maxIou = 0.0;
                            foreach (var rPrev in enqueuedThisFrame)
                            {
                                double iou = RectComparisonHelper.IoU(rPrev, cropRect);
                                if (iou > maxIou) maxIou = iou;
                            }

                            if (maxIou >= 0.65) // sahneye göre 0.6–0.75 arasında deneyebilirsin
                            {
                                Debug.WriteLine($"DEDUP drop id={tracker.Id} f={frameIdx} IOUmax={maxIou:0.00}");
                                tracker.ClearOcrEnqueued();
                            }
                            else
                            {
                                // Görseller (guard'lı)
                                int cnt = tracker._ocrBuf.Count;
                                DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, best.Img144x32.ToBitmap());
                                if (cnt > 0) DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM1, tracker._ocrBuf[0].Img144x32.ToBitmap());
                                if (cnt > 1) DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM2, tracker._ocrBuf[1].Img144x32.ToBitmap());
                                if (cnt > 2) DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM3, tracker._ocrBuf[2].Img144x32.ToBitmap());

                                // Teşhis logu (IndexOutOfRange'a dayanıklı)
                                //string BufEntry(int i)
                                //{
                                //    if (i < 0 || i >= cnt) return "-";
                                //    var sEntry = tracker._ocrBuf[i];
                                //    return $"[{i}] f={sEntry.FrameIndex} rect=({sEntry.Rect.X},{sEntry.Rect.Y},{sEntry.Rect.Width},{sEntry.Rect.Height}) svm={sEntry.SvmScore:0.000}";
                                //}
                                //Debug.WriteLine(
                                //    $"ENQ id={tracker.Id} thr={System.Threading.Thread.CurrentThread.ManagedThreadId} f={frameIdx} " +
                                //    $"buf: {BufEntry(0)} | {BufEntry(1)} | {BufEntry(2)}"
                                //);


                                //********* burası

                                // Kuyruğa ekle (frame klonlama + crop)
                                //EnqueueBestPlate(currBgr, cropRect, tracker.LastScore, best);

                                // Bu karede kabul edilen bölgeler listesine ekle
                                //enqueuedThisFrame.Add(cropRect);

                                bool dup = enqueuedThisFrame.Any(rPrev => RectComparisonHelper.IsSamePlate(rPrev, cropRect));

                                if (dup)
                                {
                                    Debug.WriteLine($"DEDUP drop id={tracker.Id} f={frameIdx} (combo gate)");
                                    tracker.ClearOcrEnqueued();
                                }
                                else
                                {

                                 
                                    EnqueueBestPlate(currBgr, cropRect, tracker.LastScore, best);
                                    enqueuedThisFrame.Add(cropRect);

                                    Debug.WriteLine($"ENQ id={tracker.Id} thr={System.Threading.Thread.CurrentThread.ManagedThreadId} "
                         + $"buf=({tracker._ocrBuf[0].FrameIndex}" + " " +
                         $"{tracker._ocrBuf[1].FrameIndex}" + " " +
                         $"{tracker._ocrBuf[2].FrameIndex}) f={frameIdx}");


                                }




                            }
                        }
                        else
                        {
                            // Best sample yoksa kilidi geri bırak (başka karede tekrar denesin)
                            tracker.ClearOcrEnqueued();
                        }
                    }
                }
                else
                {
                    // Ardışık istiyorsan: hit değilse pass serisini sıfırla
                    tracker.ResetPass();
                }

                // Bu kare bitti: detection'ı TrackRect'e commit et (valid ise) + flag'i temizle
                tracker.CommitDetection();

                _tracked[idx] = tracker;
            }
        } // fortest



        private void EnqueueBestPlate(Mat currBgr, Rect cropRect, double score, SimpleTracker.OcrSample best)
        {
            if (best.Img144x32 == null) 
                return;

            using var mat = best.Img144x32.Clone();
            Cv2.CvtColor(mat, mat, ColorConversionCodes.GRAY2BGR);

            var pp = new PossiblePlate
            {
                frame = currBgr.Clone(),
                addedRects = cropRect,
                PlateScore = score,
                possiblePlateRegions = mat.Clone()
            };

            m_plateQueue.TryAdd(pp);
        }

     


        private void ResetDetectionFlags(ThreadSafeList<SimpleTracker> trackers, int frameIdx)
        {
            var snap = trackers.Snapshot();

            for (int i = 0; i < snap.Length; i++)
            {
                int idx = trackers.FindIndex(tr => tr.Id == snap[i].Id);

                if (idx < 0)
                    continue;

                var tr = trackers[idx];

                if (tr.LastSeenFrame != frameIdx && tr.DetectedThisFrame) // bu karede hit değilse bayrağı kapat
                {
                    tr.DetectedThisFrame = false;
                    trackers[idx] = tr;
                }
            }
        }

        // ==== Yardımcılar ====

        // Point2f[] -> Mat (Nx1, CV_32FC2)
        private static Mat PointsToMat(Point2f[] pts)
        {
            var m = new Mat(pts.Length, 1, MatType.CV_32FC2);
            var idx = m.GetGenericIndexer<Vec2f>();
            for (int i = 0; i < pts.Length; i++)
                idx[i, 0] = new Vec2f(pts[i].X, pts[i].Y);
            return m;
        }

        private static double Median(IList<double> xs)
        {
            if (xs == null || xs.Count == 0) return 0;
            var arr = xs.OrderBy(v => v).ToArray();
            int n = arr.Length;
            return (n % 2 == 1) ? arr[n / 2] : 0.5 * (arr[n / 2 - 1] + arr[n / 2]);
        }

        static float Median(IEnumerable<float> xs)
        {
            var a = xs.OrderBy(v => v).ToArray();
            int n = a.Length;
            return n % 2 == 1 ? a[n / 2] : (a[n / 2 - 1] + a[n / 2]) * 0.5f;
        }

    }
}
