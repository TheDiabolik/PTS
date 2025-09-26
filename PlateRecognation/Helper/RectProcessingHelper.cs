using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class RectProcessingHelper
    { 
        //diğer gruplama methodlarından farkı if conditioanları 
        public static List<List<MserResult>> GroupRectsByProximity(List<MserResult> rects, int proximityThreshold = 10)
        {
            List<List<MserResult>> groups = new List<List<MserResult>>();

            // Dikdörtgenleri soldan sağa doğru sıralıyoruz
            rects = rects.OrderBy(r => r.BBox.X).ToList();

            int lastProcessedX = int.MinValue; // Son işlenen dikdörtgenin X konumu

            foreach (MserResult rect in rects)
            {
                // Eğer mevcut dikdörtgen en son gruplanan dikdörtgenin X + threshold değeri içindeyse
                if (rect.BBox.X <= lastProcessedX + proximityThreshold)
                {
                    // Var olan gruplar içinde yakın dikdörtgen var mı kontrol et
                    bool addedToGroup = false;

                    foreach (List<MserResult> group in groups)
                    {
                        // Sadece son gruptaki en son eklenen dikdörtgenle kontrol yaparak karşılaştırmayı hızlandırıyoruz
                        var lastRectInGroup = group.First();

                        if (RectComparisonHelper.AreRectsNearEachOther(lastRectInGroup.BBox, rect.BBox, proximityThreshold))
                        {
                            group.Add(rect);
                            addedToGroup = true;
                            break; // Bir gruba eklendiyse diğer gruplara bakmaya gerek yok
                        }
                    }

                    // Eğer bir gruba eklenmediyse, yeni bir grup oluştur
                    if (!addedToGroup)
                    {
                        groups.Add(new List<MserResult> { rect });
                    }
                }
                else
                {
                    // Yeni bir grup oluştur, çünkü mevcut dikdörtgen threshold dışındadır
                    groups.Add(new List<MserResult> { rect });

                    lastProcessedX = rect.BBox.X + rect.BBox.Width;
                }
            }

            return groups;
        }

        public static List<List<MserResult>> GroupPlateCharacters(List<MserResult> rects)
        {
            List<List<MserResult>> groups = new List<List<MserResult>>();

            if (rects == null || rects.Count == 0)
                return groups;

            // Ön filtreleme: Çok küçük dikdörtgenleri ele
            //rects = rects.Where(r => r.BBox.Width > 5 && r.BBox.Height > 10).ToList();

            // Sol → Sağ sıralama
            rects = rects.OrderBy(r => r.BBox.X).ToList();

            // Ortalama genişlik hesaplama (dinamik threshold)
            double averageCharacterWidth = rects.Average(r => r.BBox.Width);
            int horizontalThreshold = (int)(averageCharacterWidth * 0.5);

            // Ortalama Y değeri ile yükseklik hizalama kontrolü
            double averageCharacterY = rects.Average(r => r.BBox.Y);
            int verticalTolerance = (int)(rects.Average(r => r.BBox.Height) * 0.5);

            List<MserResult> currentGroup = new List<MserResult> { rects[0] };

            for (int i = 1; i < rects.Count; i++)
            {
                var currentRect = rects[i];
                var previousRect = rects[i - 1];

                int distanceX = currentRect.BBox.X - (previousRect.BBox.X + previousRect.BBox.Width);
                int distanceY = Math.Abs(currentRect.BBox.Y - previousRect.BBox.Y);

                if (distanceX <= horizontalThreshold && distanceY <= verticalTolerance)
                {
                    currentGroup.Add(currentRect);
                }
                else
                {
                    groups.Add(currentGroup);
                    currentGroup = new List<MserResult> { currentRect };
                }
            }

            if (currentGroup.Count > 0)
                groups.Add(currentGroup);

            return groups;
        }

        public static List<List<MserResult>> GroupRectsByProximityByGPT(List<MserResult> rects, int proximityThreshold = 10)
        {
            List<List<MserResult>> groups = new List<List<MserResult>>();

            if (rects == null || rects.Count == 0)
                return groups;

            // Dikdörtgenleri soldan sağa sıralıyoruz
            rects = rects.OrderBy(r => r.BBox.X).ToList();

            // İlk grubu oluşturup ilk rect'i ekleyelim
            List<MserResult> currentGroup = new List<MserResult> { rects[0] };

            for (int i = 1; i < rects.Count; i++)
            {
                var currentRect = rects[i];
                var previousRect = rects[i - 1];

                // X eksenindeki uzaklık hesaplanıyor
                int distanceX = currentRect.BBox.X - (previousRect.BBox.X + previousRect.BBox.Width);

                if (distanceX <= proximityThreshold)
                {
                    // Dikdörtgen yakınsa, mevcut gruba ekleyelim
                    currentGroup.Add(currentRect);
                }
                else
                {
                    // Yeni bir grup oluştur, çünkü uzaklık fazla
                    groups.Add(currentGroup);
                    currentGroup = new List<MserResult> { currentRect };
                }
            }

            // Son kalan grubu ekle
            if (currentGroup.Count > 0)
                groups.Add(currentGroup);

            return groups;
        }


        public static List<List<MserResult>> CheckRectCoordinatByDeisim(List<List<MserResult>> groups)
        {
           //BU METHOD SONRASI BAZI SEGMENTASYONLARI BİRLEŞTİRMEK LAZIM BUNA BAK

            for (int i = 0; i < groups.Count - 1; i++)
            {

                List<MserResult> currentGroup = groups[i];
                List<MserResult> nextGroup = groups[i + 1];

                // Mevcut gruptaki dikdörtgenlerin en küçük X koordinatını bul
                int minXInCurrentGroup = currentGroup.Min(r => r.BBox.X);

                // Mevcut gruptaki dikdörtgenlerin genişliklerinin ortalamasını hesapla
                double minWidthInCurrentGroup = (currentGroup.Min(r => r.BBox.Width));

                // Genişlik ortalaması ile en küçük X koordinatını topla
                double thresholdX = minXInCurrentGroup + minWidthInCurrentGroup;

                // 🔹 Ek hesaplamalar: ortalama ve medyan (şu an sadece ortalama kullanılıyor)
                //double ortalamawith = currentGroup.Average(r => r.BBox.X + r.BBox.Width);
                double medianWidth = FilterHelper.CalculateMedian(currentGroup.Select(r => (double)r.BBox.X + r.BBox.Width).ToList());


                // nextGroup’un ilk elemanının X değeri
                int firstXInNextGroup = nextGroup.First().BBox.X;
                // Bir sonraki gruptaki en küçük X değerini bul
                //int minXInNextGroup = nextGroup.Min(r => r.BBox.X);

                // 🔀 Şart 1: thresholdX kontrolü (önceki davranış)
                bool condition1 = thresholdX > firstXInNextGroup;

                // 🔀 Şart 2: ortalamawith kontrolü (senin istediğin yeni davranış)
                bool condition2 = medianWidth > firstXInNextGroup;

              


                // Eğer en az bir şart sağlanıyorsa grupları birleştir
                if (condition1 || condition2)
                {
                    var filteredRects = nextGroup.FindAll(r => r.BBox.X < thresholdX || r.BBox.X < medianWidth);

                    currentGroup.AddRange(filteredRects);
                    nextGroup.RemoveAll(r => filteredRects.Contains(r));

                    if (nextGroup.Count == 0)
                        groups.RemoveAt(i + 1);

                    i--; // Aynı grup üzerinde tekrar işlem yapmak için i’yi azalt
                }

                //// Eğer mevcut grubun threshold değeri bir sonraki grubun X değerinden büyükse
                //if (thresholdX > minXInNextGroup)
                //{
                //    var filteredRects = nextGroup.FindAll(r => r.BBox.X < thresholdX);

                //    // Bir sonraki grubun tüm dikdörtgenlerini mevcut gruba ekle
                //    currentGroup.AddRange(filteredRects);

                //    // Bir sonraki gruptan eklenen dikdörtgenleri kaldır
                //    nextGroup.RemoveAll(r => filteredRects.Contains(r));


                //    if (nextGroup.Count == 0)
                //        groups.RemoveAt(i + 1);

                //    ////// İndeksi azaltarak yeniden aynı grup için kontrol et
                //    i--;
                //}

            }

            return groups;
        }


        public static List<List<MserResult>> CheckRectCoordinat(List<List<MserResult>> groups)
        {
            //BU METHOD SONRASI BAZI SEGMENTASYONLARI BİRLEŞTİRMEK LAZIM BUNA BAK

            for (int i = 0; i < groups.Count - 1; i++)
            {

                List<MserResult> currentGroup = groups[i];
                List<MserResult> nextGroup = groups[i + 1];

                // Mevcut gruptaki dikdörtgenlerin en küçük X koordinatını bul
                int minXInCurrentGroup = currentGroup.Min(r => r.BBox.X);

                // Mevcut gruptaki dikdörtgenlerin genişliklerinin ortalamasını hesapla
                double minWidthInCurrentGroup = currentGroup.Min(r => r.BBox.Width);

                // Genişlik ortalaması ile en küçük X koordinatını topla
                double thresholdX = minXInCurrentGroup + minWidthInCurrentGroup;

                // Bir sonraki gruptaki en küçük X değerini bul
                int minXInNextGroup = nextGroup.Min(r => r.BBox.X);

                //// Eğer mevcut grubun threshold değeri bir sonraki grubun X değerinden büyükse
                if (thresholdX > minXInNextGroup)
                {
                    var filteredRects = nextGroup.FindAll(r => r.BBox.X < thresholdX);

                    // Bir sonraki grubun tüm dikdörtgenlerini mevcut gruba ekle
                    currentGroup.AddRange(filteredRects);

                    // Bir sonraki gruptan eklenen dikdörtgenleri kaldır
                    nextGroup.RemoveAll(r => filteredRects.Contains(r));


                    if (nextGroup.Count == 0)
                        groups.RemoveAt(i + 1);

                    ////// İndeksi azaltarak yeniden aynı grup için kontrol et
                    i--;
                }
            }

            return groups;
        }

        public static List<List<MserResult>> CheckRectCoordinatWithCenter(List<List<MserResult>> groups)
        {
            for (int i = 0; i < groups.Count - 1; i++)
            {
                List<MserResult> currentGroup = groups[i];
                List<MserResult> nextGroup = groups[i + 1];

                // Mevcut gruptaki dikdörtgenlerin en küçük X koordinatını bul
                int minXInCurrentGroup = currentGroup.Min(r => r.BBox.X);

                // Minimum genişlikten bir büyüğünü al (daha kararlı eşik için)
                var sortedWidths = currentGroup.Select(r => r.BBox.Width).OrderBy(w => w).ToList();
                double minWidthInCurrentGroup = sortedWidths.Count >= 2 ? sortedWidths[1] : sortedWidths.FirstOrDefault();

                // X tabanlı örtüşme eşiği
                double thresholdX = minXInCurrentGroup + minWidthInCurrentGroup;

                // Bir sonraki gruptaki en küçük X değerini bul
                int minXInNextGroup = nextGroup.Min(r => r.BBox.X);

                // 🔹 1. Geleneksel thresholdX karşılaştırması
                bool xOverlap = thresholdX > minXInNextGroup;

                // 🔹 2. Merkez noktası mesafe kontrolü
                double avgWidth = currentGroup.Average(r => r.BBox.Width);
                double centerThreshold = avgWidth * 1.5;

                bool centerCloseEnough = currentGroup.Any(c =>
                    nextGroup.Any(n =>
                    {
                        var cCenter = new OpenCvSharp.Point(c.BBox.X + c.BBox.Width / 2, c.BBox.Y + c.BBox.Height / 2);
                        var nCenter = new OpenCvSharp.Point(n.BBox.X + n.BBox.Width / 2, n.BBox.Y + n.BBox.Height / 2);
                        double dist = Math.Sqrt(Math.Pow(cCenter.X - nCenter.X, 2) + Math.Pow(cCenter.Y - nCenter.Y, 2));
                        return dist < centerThreshold;
                    }));

                // 🔀 Eğer thresholdX veya merkez yakınlığı yeterliyse grupları birleştir
                if (xOverlap || centerCloseEnough)
                {
                    var filteredRects = nextGroup.FindAll(r =>
                    {
                        bool xCondition = r.BBox.X < thresholdX;

                        bool centerCondition = currentGroup.Any(c =>
                        {
                            var cCenter = new OpenCvSharp.Point(c.BBox.X + c.BBox.Width / 2, c.BBox.Y + c.BBox.Height / 2);
                            var rCenter = new OpenCvSharp.Point(r.BBox.X + r.BBox.Width / 2, r.BBox.Y + r.BBox.Height / 2);
                            double dist = Math.Sqrt(Math.Pow(cCenter.X - rCenter.X, 2) + Math.Pow(cCenter.Y - rCenter.Y, 2));
                            return dist < centerThreshold;
                        });

                        return xCondition || centerCondition;
                    });

                    currentGroup.AddRange(filteredRects);
                    nextGroup.RemoveAll(r => filteredRects.Contains(r));

                    if (nextGroup.Count == 0)
                        groups.RemoveAt(i + 1);

                    i--; // yeniden aynı grup üzerinde çalış
                }
            }

            return groups;
        }





        public static List<List<MserResult>> CheckRectCoordinatWithContainment(List<List<MserResult>> groups)
        {
            // 1️⃣ Adım: X sıralı bindirme kontrolü ve birleştirme
            for (int i = 0; i < groups.Count - 1; i++)
            {
                List<MserResult> currentGroup = groups[i];
                List<MserResult> nextGroup = groups[i + 1];

                double currentGroupWidthAvg = currentGroup.Min(r => r.BBox.Width);
                int minXInCurrentGroup = currentGroup.Min(r => r.BBox.X);
                double thresholdX = minXInCurrentGroup + currentGroupWidthAvg;

                int minXInNextGroup = nextGroup.Min(r => r.BBox.X);

                if (thresholdX > minXInNextGroup)
                {
                    var filteredRects = nextGroup.FindAll(r => r.BBox.X < thresholdX);
                    currentGroup.AddRange(filteredRects);
                    nextGroup.RemoveAll(r => filteredRects.Contains(r));

                    if (nextGroup.Count == 0)
                        groups.RemoveAt(i + 1);

                    i--;
                }
            }

            // 2️⃣ Adım: İç içe geçmiş dikdörtgenleri filtrele
            foreach (var group in groups)
            {
                var toRemove = new HashSet<MserResult>();

                for (int i = 0; i < group.Count; i++)
                {
                    var outer = group[i].BBox;

                    for (int j = 0; j < group.Count; j++)
                    {
                        if (i == j) continue;

                        var inner = group[j].BBox;

                        // inner dikdörtgen, outer'ın tamamen içindeyse
                        if (IsContained(inner, outer))
                        {
                            toRemove.Add(group[j]);
                        }
                    }
                }

                // Silinecekleri çıkar
                group.RemoveAll(r => toRemove.Contains(r));
            }

            return groups;
        }

        private static bool IsContained(Rect inner, Rect outer)
        {
            return inner.X >= outer.X &&
                   inner.Y >= outer.Y &&
                   inner.Right <= outer.Right &&
                   inner.Bottom <= outer.Bottom;
        }



        public static List<List<MserResult>> CheckRectCoordinateByGpt(List<List<MserResult>> groups)
        {
            if (groups == null || groups.Count <= 1)
                return groups;

            List<List<MserResult>> mergedGroups = new List<List<MserResult>>();
            mergedGroups.Add(new List<MserResult>(groups[0]));

            for (int i = 1; i < groups.Count; i++)
            {
                var previousGroup = mergedGroups.Last();
                var currentGroup = groups[i];

                // Önceki grubun maksimum sağ sınırını bul
                int previousGroupRight = previousGroup.Max(r => r.BBox.Right);

                // Mevcut grubun minimum X sınırını bul
                int currentGroupLeft = currentGroup.Min(r => r.BBox.X);

                // Eğer önceki grubun sağ sınırı mevcut grubun sol sınırını aşıyorsa
                if (previousGroupRight >= currentGroupLeft)
                {
                    // Mevcut grubun önceki gruba eklenmesi
                    previousGroup.AddRange(currentGroup);
                }
                else
                {
                    // Yeni grup olarak ekle
                    mergedGroups.Add(new List<MserResult>(currentGroup));
                }
            }

            return mergedGroups;
        }

        public static List<List<MserResult>> CheckRectCoordinatGenis(List<List<MserResult>> groups)
        {
            for (int i = 0; i < groups.Count - 1; i++)
            {
                List<MserResult> currentGroup = groups[i];
                List<MserResult> nextGroup = groups[i + 1];

                // 1️⃣ **Mevcut gruptaki en büyük dikdörtgenin genişliğini al**
                double currentGroupMaxWidth = currentGroup.Max(r => r.BBox.Width);

                // 2️⃣ **Mevcut gruptaki en küçük X koordinatını bul**
                int minXInCurrentGroup = currentGroup.Min(r => r.BBox.X);

                // 3️⃣ **thresholdX’i en büyük genişlik ile belirle**
                double thresholdX = minXInCurrentGroup + currentGroupMaxWidth;

                // 4️⃣ **Sonraki grubun en küçük X değerini bul**
                int minXInNextGroup = nextGroup.Min(r => r.BBox.X);

                // 5️⃣ **Eğer thresholdX sonraki grubun X’inden büyükse, birleştirme yap**
                if (thresholdX > minXInNextGroup)
                {
                    var filteredRects = nextGroup.FindAll(r => r.BBox.X < thresholdX);

                    // 6️⃣ **Sadece daha büyük dikdörtgenleri ekle**
                    foreach (var rect in filteredRects)
                    {
                        if (!currentGroup.Any(existingRect => existingRect.BBox.Width > rect.BBox.Width))
                        {
                            currentGroup.Add(rect);
                        }
                    }

                    // 7️⃣ **Eklenenleri sonraki gruptan çıkar**
                    nextGroup.RemoveAll(r => filteredRects.Contains(r));

                    // 8️⃣ **Sonraki grup boşaldıysa, onu listeden sil**
                    if (nextGroup.Count == 0)
                        groups.RemoveAt(i + 1);

                    // 9️⃣ **İndeksi azaltarak tekrar kontrol et**
                    i--;
                }
            }

            return groups;
        }

        public static List<Rect> GroupCloseRects(List<Rect> rects, int horizontalThreshold, int verticalThreshold)
        {
            if (rects.Count == 0)
                return new List<Rect>();

            // 1️⃣ Dikdörtgenleri X koordinatına göre soldan sağa sıralayalım
            rects = rects.OrderBy(r => r.X).ToList();

            List<List<Rect>> groups = new List<List<Rect>>();
            bool[] used = new bool[rects.Count];

            for (int i = 0; i < rects.Count; i++)
            {
                if (used[i]) continue;

                List<Rect> currentGroup = new List<Rect> { rects[i] };
                used[i] = true;

                for (int j = i + 1; j < rects.Count; j++)
                {
                    if (used[j]) continue;

                    // 2️⃣ Yatay mesafe (X farkı) ve Dikey mesafe (Y farkı) kontrolü
                    if (Math.Abs(rects[j].X - (rects[i].X + rects[i].Width)) <= horizontalThreshold &&
                        Math.Abs(rects[j].Y - rects[i].Y) <= verticalThreshold)
                    {
                        currentGroup.Add(rects[j]);
                        used[j] = true;
                    }
                }

                groups.Add(currentGroup);
            }

            // 3️⃣ Her grup için en küçük kapsayan dikdörtgeni bul
            List<Rect> mergedRects = new List<Rect>();
            foreach (var group in groups)
            {
                int minX = group.Min(r => r.X);
                int minY = group.Min(r => r.Y);
                int maxX = group.Max(r => r.X + r.Width);
                int maxY = group.Max(r => r.Y + r.Height);
                mergedRects.Add(new Rect(minX, minY, maxX - minX, maxY - minY));
            }

            return mergedRects;
        }

        public static List<MserResult> FilterRectsBelowAverageY(List<MserResult> mserResults, int plateY)
        {
            if (mserResults == null || mserResults.Count == 0)
                return new List<MserResult>();

            // Y koordinatının ortalamasını hesapla (bu durumda plateY / 2 kullanılıyor)
            double averageY = plateY / 2;

            // Ortalama Y'nin altında kalan MSER sonuçlarını filtrele
            List<MserResult> filteredMserResults = mserResults
                .Where(r => r.BBox.Y <= averageY)  // Y'si ortalamadan büyük ya da eşit olanları seç
                .ToList();

            return filteredMserResults;
        }


        public static List<MserResult> FilterSortedBBoxes(List<MserResult> sortedBBoxes)
        {
            List<MserResult> filteredResults = new List<MserResult>();

            for (int i = 0; i < sortedBBoxes.Count; i++)
            {
                bool isInner = false;

                for (int j = 0; j < sortedBBoxes.Count; j++)
                {
                    if (i != j && sortedBBoxes[j].BBox.Contains(sortedBBoxes[i].BBox))
                    {
                        isInner = true; // The current box is inside another
                        break;
                    }
                }

                if (!isInner)
                {
                    filteredResults.Add(sortedBBoxes[i]);
                }
            }
            return filteredResults;
        }

        public static List<MserResult> AlignedResults(List<MserResult> filteredResults)
        {
            // Aynı düzlemde olup olmadığını kontrol edin
            List<MserResult> alignedResults = new List<MserResult>();

            // Yükseklik için tolerans
            const int yTolerance = 50; // Yükseklik farkı için tolerans
            const int heightTolerance = 55; // Karakterlerin yükseklik toleransı

            for (int i = 0; i < filteredResults.Count; i++)
            {
                Rect currentBox = filteredResults[i].BBox;
                bool isAligned = true;

                for (int j = 0; j < filteredResults.Count; j++)
                {
                    if (i != j)
                    {
                        Rect otherBox = filteredResults[j].BBox;

                        // Aynı düzlemde olup olmadığını kontrol edin
                        if (Math.Abs(currentBox.Y - otherBox.Y) > yTolerance || Math.Abs(currentBox.Height - otherBox.Height) > heightTolerance)
                        {
                            isAligned = false;
                            break;
                        }
                    }
                }

                if (isAligned)
                {
                    alignedResults.Add(filteredResults[i]); // Aynı hizadaki karakterler
                }
            }

            return alignedResults;
        }
    }
}
