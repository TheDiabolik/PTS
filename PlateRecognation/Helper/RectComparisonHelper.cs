using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;

namespace PlateRecognation
{
    internal class RectComparisonHelper
    {
        /// <summary>
        /// İki dikdörtgenin aynı veya çok yakın olup olmadığını kontrol eden fonksiyon.
        /// </summary>
        /// <param name="rect1">İlk dikdörtgen</param>
        /// <param name="rect2">İkinci dikdörtgen</param>
        /// <returns>Eğer dikdörtgenler aynı veya çok benzer ise true döner.</returns>
        public static bool AreRectsCloseAndSimilar(Rect rect1, Rect rect2)
        {
            // Dikdörtgenlerin merkezlerini ve boyutlarını karşılaştırarak benzer olup olmadıklarını kontrol et
            int centerX1 = rect1.X + rect1.Width / 2;
            int centerY1 = rect1.Y + rect1.Height / 2;

            int centerX2 = rect2.X + rect2.Width / 2;
            int centerY2 = rect2.Y + rect2.Height / 2;

            int distanceThreshold = 10; // Merkezler arası mesafe eşiği (pikseller cinsinden)
            bool isCenterClose = (Math.Abs(centerX1 - centerX2) < distanceThreshold && Math.Abs(centerY1 - centerY2) < distanceThreshold);

            // Boyutların da çok farklı olmaması için kontrol et
            double sizeThreshold = 0.2; // Boyut farkı eşiği (oran olarak)
            bool isSizeSimilar = (Math.Abs(rect1.Width - rect2.Width) / (double)rect1.Width < sizeThreshold) &&
                                 (Math.Abs(rect1.Height - rect2.Height) / (double)rect1.Height < sizeThreshold);

            return isCenterClose && isSizeSimilar;
        }

        public static bool AreRectsSimilar(Rect rect1, Rect rect2, double positionThreshold = 10, double sizeThreshold = 0.2)
        {
            // **📌 1️⃣ Merkez koordinatlarını hesapla**
            int centerX1 = rect1.X + rect1.Width / 2;
            int centerY1 = rect1.Y + rect1.Height / 2;
            int centerX2 = rect2.X + rect2.Width / 2;
            int centerY2 = rect2.Y + rect2.Height / 2;

            // **📌 2️⃣ Merkezler arasındaki mesafeyi hesapla**
            double distance = Math.Sqrt(Math.Pow(centerX1 - centerX2, 2) + Math.Pow(centerY1 - centerY2, 2));

            // **📌 3️⃣ Boyut farklarını hesapla**
            double widthDifference = Math.Abs(rect1.Width - rect2.Width) / (double)Math.Max(rect1.Width, rect2.Width);
            double heightDifference = Math.Abs(rect1.Height - rect2.Height) / (double)Math.Max(rect1.Height, rect2.Height);

            // **📌 4️⃣ Eğer dikdörtgenler hem yakınsa hem de boyutları benzerse aynı kabul edilir**
            bool isPositionSimilar = distance <= positionThreshold;
            bool isSizeSimilar = widthDifference < sizeThreshold && heightDifference < sizeThreshold;

            return isPositionSimilar && isSizeSimilar;
        }

        public static bool AreRectsIntersecting(Rect a, Rect b, double threshold = 0.5)
        {
            double intersectionArea = RectGeometryHelper.CalculateRectangleArea(a & b); // Kesim alanı
            double minArea = Math.Min(RectGeometryHelper.CalculateRectangleArea(a), RectGeometryHelper.CalculateRectangleArea(b)); // En küçük alanı al

            return (intersectionArea / minArea) > threshold;
        }

        // İki dikdörtgenin birbirine yakın olup olmadığını kontrol eden metot
        public static bool AreRectsNearEachOther(Rect rectA, Rect rectB, int threshold)
        {
            // Dikdörtgenlerin x ve y koordinatlarına göre yakınlıkları
            bool closeInX = Math.Abs(rectA.X - rectB.X) <= threshold;
            bool closeInY = Math.Abs(rectA.Y - rectB.Y) <= 10;

            return closeInX && closeInY;
        }

        //public static double IoU(Rect a, Rect b)
        //{
        //    int x1 = Math.Max(a.Left, b.Left);
        //    int y1 = Math.Max(a.Top, b.Top);
        //    int x2 = Math.Min(a.Right, b.Right);
        //    int y2 = Math.Min(a.Bottom, b.Bottom);

        //    int interWidth = Math.Max(0, x2 - x1 + 1);
        //    int interHeight = Math.Max(0, y2 - y1 + 1);
        //    double interArea = interWidth * interHeight;

        //    double unionArea = a.Width * a.Height + b.Width * b.Height - interArea;

        //    return interArea / unionArea;
        //}

        public static double IoU1(Rect a, Rect b)
        {
            int x1 = Math.Max(a.X, b.X), y1 = Math.Max(a.Y, b.Y);
            int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            int iw = Math.Max(0, x2 - x1), ih = Math.Max(0, y2 - y1);
            double inter = (double)iw * ih;
            if (inter <= 0) return 0.0;
            double uni = (double)a.Width * a.Height + (double)b.Width * b.Height - inter;
            return inter / uni;
        }
        public static double IoU(Rect a, Rect b)
        {
            // aRightInc = a.Right-1, aBottomInc = a.Bottom-1
            int aL = a.Left, aT = a.Top, aR = a.Right - 1, aB = a.Bottom - 1;
            int bL = b.Left, bT = b.Top, bR = b.Right - 1, bB = b.Bottom - 1;

            int x1 = Math.Max(aL, bL), y1 = Math.Max(aT, bT);
            int x2 = Math.Min(aR, bR), y2 = Math.Min(aB, bB);
            int iw = Math.Max(0, x2 - x1 + 1), ih = Math.Max(0, y2 - y1 + 1);

            double inter = (double)iw * ih;
            if (inter <= 0) return 0.0;

            double aArea = (double)(aR - aL + 1) * (aB - aT + 1);
            double bArea = (double)(bR - bL + 1) * (bB - bT + 1);
            double uni = aArea + bArea - inter;
            return inter / uni;
        }

        public static List<Rect> MergeAndFilterPlateRects(Rect[] rectSources, double iouThreshold = 0.4)
        {
            var loo = rectSources.ToList();

            List<Rect> allRects = loo.Select(r => r).ToList();

            // 1. IoU bazlı benzer dikdörtgenleri grupla ve birleştir
            List<Rect> mergedRects = MergeOverlappingRects(allRects, iouThreshold);

            // 2. Filtreleme: Çok küçük veya absürt oranlı dikdörtgenleri çıkar
            List<Rect> filteredRects = mergedRects
                .Where(r =>
                {
                    double aspectRatio = r.Width / (double)r.Height;
                    return r.Width > 30 && r.Height > 10 && aspectRatio >= 1.5 && aspectRatio <= 6.0;
                })
                .ToList();

            return filteredRects;
        }


        public static List<Rect> MergeRectsByProximity(List<Rect> rects, int maxGapX, double minVertOverlap)
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
                    cur = RectGeometryHelper.Union(cur, nxt);
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

        private static List<Rect> MergeOverlappingRects(List<Rect> rects, double iouThreshold)
        {
            List<Rect> result = new List<Rect>();

            while (rects.Count > 0)
            {
                Rect current = rects[0];
                rects.RemoveAt(0);

                List<Rect> toMerge = new List<Rect> { current };

                for (int i = rects.Count - 1; i >= 0; i--)
                {
                    if (IoU(current, rects[i]) > iouThreshold)
                    {
                        toMerge.Add(rects[i]);
                        rects.RemoveAt(i);
                    }
                }

                result.Add(MergeRects(toMerge));
            }

            return result;
        }
        private static Rect MergeRects(List<Rect> rects)
        {
            int minX = rects.Min(r => r.X);
            int minY = rects.Min(r => r.Y);
            int maxX = rects.Max(r => r.X + r.Width);
            int maxY = rects.Max(r => r.Y + r.Height);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }


        private static double IntersectionArea(Rect a, Rect b)
        {
            int x1 = Math.Max(a.X, b.X);
            int y1 = Math.Max(a.Y, b.Y);
            int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            int w = Math.Max(0, x2 - x1);
            int h = Math.Max(0, y2 - y1);
            return (double)w * h;
        }
        private static bool IsDuplicate(Rect a, Rect b)
        {
            // 1) Klasik IoU
            if (RectComparisonHelper.IoU(a, b) > 0.45) return true;

            // 2) IoM (Intersection over Min) – küçük kutu büyük kutunun içindeyse güçlü sinyal
            double inter = IntersectionArea(a, b);
            double areaMin = Math.Max(1, Math.Min(a.Width * a.Height, b.Width * b.Height));
            double iom = inter / areaMin;
            if (iom > 0.70) return true;

            // 3) Merkez testi – yeni adayın merkezi mevcut kutunun içinde mi?
            var cx = b.X + b.Width / 2;
            var cy = b.Y + b.Height / 2;
            if (a.Contains(new OpenCvSharp.Point(cx, cy))) return true;

            return false;
        }


        private static bool IsDuplicatev1(Rect a, Rect b)
        {
            if (RectComparisonHelper.IoU(a, b) > 0.60) return true;

            // IoM + benzer AR
            double arA = (double)a.Width / Math.Max(1, a.Height);
            double arB = (double)b.Width / Math.Max(1, b.Height);
            if (IntersectionArea(a, b) / Math.Max(1.0, Math.Min(a.Width * a.Height, b.Width * b.Height)) > 0.85
                && Math.Abs(arA - arB) / Math.Max(arA, arB) < 0.25)
                return true;

            // Merkez + yakınlık
            var ca = new OpenCvSharp.Point(a.X + a.Width / 2, a.Y + a.Height / 2);
            var cb = new OpenCvSharp.Point(b.X + b.Width / 2, b.Y + b.Height / 2);
            double d = Math.Sqrt((ca.X - cb.X) * (ca.X - cb.X) + (ca.Y - cb.Y) * (ca.Y - cb.Y));
            if (d < 0.4 * Math.Max(Math.Max(a.Width, a.Height), Math.Max(b.Width, b.Height)))
                return true;

            return false;
        }


        // kapsama oranı (küçüğün ne kadarı kesişiyor?)
        static double OverlapRatio(Rect s, Rect t)
        {
            int x1 = Math.Max(s.X, t.X);
            int y1 = Math.Max(s.Y, t.Y);
            int x2 = Math.Min(s.Right, t.Right);
            int y2 = Math.Min(s.Bottom, t.Bottom);
            int iw = Math.Max(0, x2 - x1), ih = Math.Max(0, y2 - y1);
            double inter = iw * ih;
            double small = Math.Max(1, s.Width * s.Height);
            return inter / small;
        }

       public  static bool IsSamePlate(Rect a, Rect b)
        {
            double iou = RectComparisonHelper.IoU(a, b);

            // merkez normalizasyonu (ortalama diyagonale göre)
            double ax = a.X + a.Width / 2.0, ay = a.Y + a.Height / 2.0;
            double bx = b.X + b.Width / 2.0, by = b.Y + b.Height / 2.0;
            double dx = ax - bx, dy = ay - by;
            double diagA = Math.Sqrt(a.Width * a.Width + a.Height * a.Height);
            double diagB = Math.Sqrt(b.Width * b.Width + b.Height * b.Height);
            double centerNorm = Math.Sqrt(dx * dx + dy * dy) / Math.Max(1.0, 0.5 * (diagA + diagB));

            // ölçek farkı (alan oranı)
            double areaA = Math.Max(1, a.Width * a.Height);
            double areaB = Math.Max(1, b.Width * b.Height);
            double scaleLog = Math.Abs(Math.Log(areaA / areaB)); // ~0 → benzer ölçek

            double containAB = OverlapRatio(a, b);
            double containBA = OverlapRatio(b, a);

            bool iouOk = iou >= 0.60;               // biraz gevşettim (0.60)
            bool nearCenters = centerNorm <= 0.18 && scaleLog <= Math.Log(1.8);
            bool containment = containAB >= 0.75 || containBA >= 0.75;

            return iouOk || nearCenters || containment;
        }

        public static bool IsSamePlate(
    Rect a, Rect b,
    double iouThr = 0.65,
    bool useAdvanced = false,
    double centerThr = 0.16,   // merkez norm (küçük diyagonale göre)
    double scaleMax = 1.6,     // alan oran üst sınırı
    double aspectTol = 0.20,   // en-boy fark toleransı (göreli)
    double containThr = 0.80   // containment eşiği
)
        {
            double iou = IoU(a, b);

            if (iou >= iouThr) 
                return true;          // Varsayılan: IoU-only

            if (!useAdvanced) 
                return false;          // Gelişmiş kapı kapalıysa burada biter

            // --- Gelişmiş ölçütler ---
            double ax = a.X + a.Width * 0.5, ay = a.Y + a.Height * 0.5;
            double bx = b.X + b.Width * 0.5, by = b.Y + b.Height * 0.5;
            double cdist = Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
            double diagA = Math.Sqrt(a.Width * a.Width + a.Height * a.Height);
            double diagB = Math.Sqrt(b.Width * b.Width + b.Height * b.Height);
            double centerNorm = cdist / Math.Max(1.0, Math.Min(diagA, diagB)); // küçük diyagonale göre

            double areaA = Math.Max(1, a.Width * a.Height);
            double areaB = Math.Max(1, b.Width * b.Height);
            double scaleRatio = Math.Max(areaA, areaB) / Math.Min(areaA, areaB);

            double aspA = (double)a.Width / Math.Max(1, a.Height);
            double aspB = (double)b.Width / Math.Max(1, b.Height);
            double aspDiffRel = Math.Abs(aspA - aspB) / Math.Max(aspA, aspB);

            double containAB = OverlapRatio(a, b);
            double containBA = OverlapRatio(b, a);
            bool containmentOk = Math.Max(containAB, containBA) >= containThr;

            // Oylama: en az 3 koşul + containment varsa daha da güçlü say
            int votes = 0;

            if (centerNorm <= centerThr) 
                votes++;

            if (scaleRatio <= scaleMax)
                votes++;

            if (aspDiffRel <= aspectTol)
                votes++;

            if (containmentOk) 
                votes++;

            // containment tek başına yeterli olmasın; en azından merkez veya oran da yakın olsun
            if (containmentOk && (centerNorm <= centerThr || aspDiffRel <= aspectTol)) 
                return true;

            return votes >= 3;
        }


    }
}
