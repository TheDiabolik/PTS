using Accord;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using static System.Reflection.Metadata.BlobBuilder;

namespace PlateRecognation
{
    internal partial class HybridPlateReadingStrategy : IPlateReadingStrategy
    {
        // ---- Ayarlar (sahnene göre oynat) ----
        const double DEDUP_IOU = 0.65;   // enqueue öncesi asıl kapı
        const bool DEDUP_USE_ADVANCED = false;  // şimdilik IoU-only
       


        private void SeedTracker(Rect r, double seedScore, OpenCvSharp.Size frameSize, Mat currGrayFull, Mat potentialPlate, int frameIdx)
        {
            var bounds = new Rect(0, 0, frameSize.Width, frameSize.Height);

            // 1) SeedRect'i güvenli al (sıkı kutu)
            var seedRect = r.Intersect(bounds);

            if (seedRect.Width <= 0 || seedRect.Height <= 0)
                return;


            //var trackRect = seedRect;

            var trackRect = RectGeometryHelper.GrowRectAdaptive(seedRect, frameSize);   // ← büyüt



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


        int FindBestTrackerMatchGated(Rect det, ThreadSafeList<SimpleTracker> tracked, int frameIdx,
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
                if (tr.DetectedThisFrame && tr.LastSeenFrame == frameIdx) 
                    continue; // aynı karede iki kez işaretlenmesin

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
                if (iou < iouMin && centerNorm > maxCenterNorm) 
                    continue;

                if (scaleLog > maxScaleLog) 
                    continue;

                // Skor: IoU ödüllendir, merkez/ölçek/yaş cezalandır
                double score = 2.0 * iou - 1.0 * centerNorm - 0.5 * scaleLog - 0.02 * age;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = tr.Id;
                }
            }

            if (bestId < 0) 
                return -1;

            // Canlı listedeki indeksi döndür (snapshot indexine güvenmeyelim)
            int liveIdx = tracked.FindIndex(t => t.Id == bestId);
            return liveIdx;
        }

        private void DetectAndAssociate(FrameWithRoi f, Mat currBgr, Mat currGrayFull, int frameIdx)
        {
            if (f.Rects == null || f.Rects.Count == 0)
                return;

            // Bu karede “sahiplenilmiş” bölgeler (hem match hem seed için)
            var claimsThisFrame = new List<Rect>();

            foreach (var r in f.Rects)
            {
                var roiSafe = RectGeometryHelper.Clip(r, currBgr.Cols, currBgr.Rows);

                if (roiSafe.Width <= 0 || roiSafe.Height <= 0)
                    continue;

                using var roiBgr = new Mat(currBgr, roiSafe);


                //Mat sdsd = new Mat(f.Frame, r);

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, sdsd.ToBitmap());

                var plates = ImageAnalysisHelper.ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roiBgr);

                //var plates = ImageAnalysisHelper.SobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(roiBgr);

                if (plates == null || plates.Count == 0)
                    continue;

                plates.Sort((a, b) => b.PlateScore.CompareTo(a.PlateScore));

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


                        // 3) Bu claim’i kaydet (seed dedup’u için)
                        claimsThisFrame.Add(tr.DetectionRect.Width > 0 ? tr.DetectionRect : gSafe);

                        //Debug.WriteLine("Track edilecek plaka alanı bulundu. - Frame : " + frameIdx.ToString());

                        // Debug:
                        //using var loo = new Mat(currGrayFull, tr.DetectionRect);
                        //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, loo.ToBitmap());
                    }
                    else
                    {
                        //Debug.WriteLine("Seed edilecek plaka alanı bulundu. - Frame : " + frameIdx.ToString());


                        // 4) SEED ÖNCESİ SIKI DEDUP
                        bool clash = claimsThisFrame.Any(cr => RectComparisonHelper.IsNearDuplicateSeed(cr, gSafe));

                        if (clash)
                        {
                            // Debug: neden elendiğini görmek istersen
                            Debug.WriteLine($"SEED DEDUP drop f={frameIdx} rect=({gSafe.X},{gSafe.Y},{gSafe.Width},{gSafe.Height})");
                            continue;
                        }


                        using var seedCropGray = new Mat(currGrayFull, gSafe);
                        SeedTracker(gSafe, p.PlateScore, currBgr.Size(), currGrayFull, seedCropGray, frameIdx);
                        DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxPlateSeed, seedCropGray.ToBitmap());

                        // 6) Yeni claim’i ekle — böylece aynı kare içinde buna çarpan ikinci bir seed engellenir
                        claimsThisFrame.Add(gSafe);
                    }
                }
            }
        }

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



        // Yan etkisiz: sadece crop seçer, clip eder ve geçerli mi söyler
        public static bool TrySelectCrop(SimpleTracker tracker, OpenCvSharp.Size imgSize, out Rect cropRect)
        {
            cropRect = tracker.DetectedThisFrame ? tracker.DetectionRect : tracker.TrackRect;

            if (imgSize.Width > 0 && imgSize.Height > 0)
                cropRect = RectGeometryHelper.Clip(cropRect, imgSize.Width, imgSize.Height);

            return cropRect.Width > 0 && cropRect.Height > 0;
        }

        public static bool EnsureCropOrCommit(SimpleTracker tracker, OpenCvSharp.Size imgSize, out Rect cropRect)
        {
            if (TrySelectCrop(tracker, imgSize, out cropRect))
                return true;

            // Geçersiz crop durumda ortak davranış:
            tracker.CommitDetection();
            return false;
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

                if (idx < 0) 
                    continue;

                var tracker = _tracked[idx];

                // Zaten OCR kuyruğuna işaretliyse: detection'ı TrackRect'e commit et ve geç
                if (tracker.OcrEnqueued)
                {
                    tracker.CommitDetection();
                    _tracked[idx] = tracker;
                    continue;
                }


                if (!EnsureCropOrCommit(tracker, currGrayFull.Size(), out var cropRect))
                {
                    _tracked[idx] = tracker;
                    continue;
                }


                // Bu karede görüldü mü? (detection ya da LK başarılı)
                bool hitThisFrame = (tracker.LastSeenFrame == frameIdx);

                if (hitThisFrame) // ağır işleri sadece hit olduğunda çalıştır
                {
                    using var svmCrop = new Mat(currGrayFull, cropRect);
                    Cv2.Resize(svmCrop, svmCrop, new OpenCvSharp.Size(144, 32), 0, 0, InterpolationFlags.Lanczos4);

                    var result = SVMHelper.AskSVMPredictionForPlateRegionWithScore(MainForm.m_mainForm.m_loadedSvmForPlateRegion, svmCrop, 0);
                    tracker.LastScore = result.score;

                    tracker.MarkPass();

                    // Same-frame tekilleştirme (frame + IoU)
                    tracker.AddOrReplaceOcrSample(
                        img144x32: svmCrop,
                        sharpness: 0,
                        svmScore: tracker.LastScore,
                        frameIdx: frameIdx,
                        rect: cropRect,
                        maxBuf: tracker.OcrSamplesCap);


                    // OCR'a hazırsa 0->1 atomik geçiş dene
                    if (tracker.IsReadyForOcr() && tracker.TryMarkOcrEnqueued())
                    {
                        if (tracker.TryPickBestOcrSample(out SimpleTracker.OcrSample best) && best.Img144x32 != null)
                        {
                            // Dedup kararı (enqueue öncesi, doğru yer)
                            var candidateRect = (best.Rect.Width > 0 && best.Rect.Height > 0) ? best.Rect : cropRect;

                            bool dup = enqueuedThisFrame.Any(rPrev => RectComparisonHelper.IsSamePlate(rPrev, candidateRect, iouThr: DEDUP_IOU, useAdvanced: DEDUP_USE_ADVANCED));

                            if (dup)
                            {
                                Debug.WriteLine($"DEDUP drop id={tracker.Id} f={frameIdx} (combo gate)");
                                tracker.ClearOcrEnqueued(); // sonraki framelerde tekrar denesin
                            }
                            else
                            {
                                // --- UI/diagnostic: dedup kararından SONRA ---
                                // Güvenli buf log
                                int cnt = tracker._ocrBuf.Count;

                                string BufEntry(int i)
                                {
                                    if (i < 0 || i >= cnt)
                                        return "-";
                                    
                                    var e = tracker._ocrBuf[i];
                                    
                                    return $"[{i}] f={e.FrameIndex} rect=({e.Rect.X},{e.Rect.Y},{e.Rect.Width},{e.Rect.Height}) svm={e.SvmScore:0.000}";
                                }
                                Debug.WriteLine(
                                    $"ENQ id={tracker.Id} thr={System.Threading.Thread.CurrentThread.ManagedThreadId} f={frameIdx} " +
                                    $"buf: {BufEntry(0)} | {BufEntry(1)} | {BufEntry(2)}"
                                );


                                // Kuyruğa ekle (frame klonlama + crop)
                                //EnqueueBestPlate(currBgr, cropRect, tracker.LastScore, best);

                                como(tracker);

                                // Bu karede kabul edilenler listesine "gerçek" ROI’yi ekle
                                enqueuedThisFrame.Add(candidateRect);



                                // Görseller (guard'lı)
                                //int cnt = tracker._ocrBuf.Count;
                                DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, best.Img144x32.ToBitmap());

                                if (cnt > 0)
                                    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM1, tracker._ocrBuf[0].Img144x32.ToBitmap());
                                
                                if (cnt > 1)
                                    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM2, tracker._ocrBuf[1].Img144x32.ToBitmap());
                                
                                if (cnt > 2)
                                    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxSVM3, tracker._ocrBuf[2].Img144x32.ToBitmap());

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

                // Kare sonu: detection'ı TrackRect'e commit et (valid ise) + flag temizle
                tracker.CommitDetection();
                _tracked[idx] = tracker;
            }
        }




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




        private void como(SimpleTracker tracker)
        {

            ThreadSafeList<PossiblePlate> possiblePlates = new ThreadSafeList<PossiblePlate>();

            List<List<CharacterWithROI>> possibleCharacters = new();

            //ThreadSafeList<CharacterWithROI> possibleCharacters = new();


            foreach (SimpleTracker.OcrSample item in tracker._ocrBuf)
            {
                using var mat = item.Img144x32.Clone();
                Cv2.CvtColor(mat, mat, ColorConversionCodes.GRAY2BGR);


                List<CharacterWithROI> characterSegmentationResult = Character.AhmetAhmetAhmetFindAndCombineCharacterCandidatesv2(mat);

                possibleCharacters.Add(characterSegmentationResult);

            }

            ThreadSafeList<AhmetPlateResult> ahmet = Helper.AhmetAhmetAhmetKuyrukRecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);


            if (ahmet.Count > 0)
            {
                int L = ahmet.Max(a => a.m_characters.Count);

                const double BONUS_3of3 = 0.20;
                const double BONUS_2of3 = 0.08;
                const double EPS = 1e-9;


                var outChars = new char[L];
                var posConfs = new List<double>(new double[L]);

                for (int i = 0; i < L; i++)
                {

                    var agg = new Dictionary<char, double>(Charset34.Length);

                    foreach (var c in Charset34)
                        agg[c] = 0.0;

                    var argmaxVotes = new Dictionary<char, int>();

                    foreach (var item in ahmet)
                    {
                        if (i >= item.m_characters.Count)
                            continue; // bu örnekte bu pozisyon yok, geç


                        var seg = item.m_characters[i];


                        foreach (var kv in seg.Confidance)
                            agg[Convert.ToChar(kv.Item1)] += kv.Item2;


                        // çoğunluk oyu için bu örnekteki en yüksek hangi karakter?
                        var bestKV = seg.Confidance.Aggregate((a, b) => a.Item2 > b.Item2 ? a : b);

                        char bestChar = Convert.ToChar(bestKV.Item1);
                        argmaxVotes[bestChar] = argmaxVotes.TryGetValue(bestChar, out var cnt) ? cnt + 1 : 1;

                    }


                    // 3) Çoğunluk bonusu ekle
                    foreach (var kv in argmaxVotes)
                    {
                        if (kv.Value >= 3)
                            agg[kv.Key] += BONUS_3of3;
                        else if (kv.Value == 2)
                            agg[kv.Key] += BONUS_2of3;
                    }


                    // 4) En yüksek skorlu karakteri seç + pozisyon güvenini oranla hesapla
                    var best = agg.Aggregate((a, b) => a.Value > b.Value ? a : b);
                    outChars[i] = best.Key;

                    double denom = agg.Values.Sum() + EPS;
                    posConfs[i] = best.Value / denom; // "seçilen / toplam" = pozisyon güv.

                }



                // 5) Metni ve plaka güvenini üret
                string plate = new string(outChars);
                double plateConf = posConfs.Average();

                PlateImageReady?.Invoke(this, new PlateImageEventArgs
                {
                    Frame = tracker._ocrBuf[0].Img144x32.ToBitmap(),
                    PlateImage = tracker._ocrBuf[0].Img144x32.ToBitmap(),
                    ReadingResult = plate,
                    Probability = plateConf
                });
            }


           
        }

        static readonly char[] Charset34 = "0123456789ABCDEFGHIJKLMNOPRSTUVYZQ".ToCharArray();

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

      

    }
}
