using Accord.Imaging.Filters;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    //static olarak kalacak
    public class FilterHelper
    {
        public static List<MserResult> FilterAndGroupCharacterCandidates(List<MserResult> characterCandidates, int imageRowCount)
        {

            //plakanın orta noktasından sonra tespit edilen alan varsa filtrele
            List<MserResult> possibleRegions = RectProcessingHelper.FilterRectsBelowAverageY(characterCandidates, imageRowCount);


            //alanları boyutlarına göre filtrele
            List<MserResult> filteredCharacterRegions = CharacterHelper.FilterPossibleCharacterRegions(possibleRegions);

            //filter for diagonal
            //List<MserResult> mamu = RectFilterHelper.FilterByDiagonalLengthZScore(filteredCharacterRegions, 0.8);



            //alanları x koordinatına göre 4'erli olarak grupluyor
            List<List<MserResult>> rectsByProximity = RectProcessingHelper.GroupRectsByProximity(filteredCharacterRegions, 4);

            //iki boundingbox rect'ti karşılaştırıyor çok yakın olan ve kesişen rectleri birleştiriyor - mergerect
            List<List<MserResult>> characterRegions = RectProcessingHelper.CheckRectCoordinat(rectsByProximity);

            //olası karakter alanı için boundingbox grubu içindeki en uygun item'i median olarak karşılaştırıp seçiyor 
            List<MserResult> characters = FilterHelper.SelectSimilarItemsFromGroupsWithMedian(characterRegions);
            //List<MserResult> characters = FilterHelper.SelectItemsWithSequentialBoundingBoxes(characterRegions);




            //karakter rect'lerinin yüksekliklerini median'a göre karşılaştırıp threshould'a uymayanları filtrele
            List<MserResult> filteredCharacters = FilterHelper.FilterGroupsByHeightMedian(characters, 5);

            //filteredCharacters = FillMissingCharacters(filteredCharacters);


          

            return filteredCharacters;
        }


        public static List<MserResult> FilterAndGroupCharacterCandidatesByAhmet(List<MserResult> characterCandidates, int imageRowCount)
        {

            //plakanın orta noktasından sonra tespit edilen alan varsa filtrele
            List<MserResult> possibleRegions = RectProcessingHelper.FilterRectsBelowAverageY(characterCandidates, imageRowCount);


            //alanları boyutlarına göre filtrele
            List<MserResult> filteredCharacterRegions = CharacterHelper.FilterPossibleCharacterRegions(possibleRegions);

            //alanları x koordinatına göre 4'erli olarak grupluyor
            List<List<MserResult>> rectsByProximity = RectProcessingHelper.GroupRectsByProximity(filteredCharacterRegions, 4);

            //iki boundingbox rect'ti karşılaştırıyor çok yakın olan ve kesişen rectleri birleştiriyor - mergerect
            List<List<MserResult>> characterRegions = RectProcessingHelper.CheckRectCoordinat(rectsByProximity);

            ////olası karakter alanı için boundingbox grubu içindeki en uygun item'i median olarak karşılaştırıp seçiyor 
            List<MserResult> characters = FilterHelper.SelectSimilarItemsFromGroupsWithMedianvCalisan(characterRegions);
           
            List<MserResult> filteredCharacters = FilterHelper.FilterGroupsByHeightMedianPercentageThreshold(characters, 0.2);

            //filteredCharacters = FillMissingCharacters(filteredCharacters);


            //var lolo = characterRegions.SelectMany(x => x).ToList();
            //var lulu = rectsByProximity.SelectMany(x => x).ToList();

            return filteredCharacters;
        }
        public static List<List<MserResult>> KeepLargestRectInEachGroup(List<List<MserResult>> groups)
        {
            var cleanedGroups = new List<List<MserResult>>();

            foreach (var group in groups)
            {
                if (group == null || group.Count == 0)
                    continue;

                // Alanı en büyük olan rect'i bul
                var largest = group.OrderByDescending(r => r.BBox.Width * r.BBox.Height).First();

                // Yeni grup sadece bu rect ile oluşturulur
                cleanedGroups.Add(new List<MserResult> { largest });
            }

            return cleanedGroups;
        }
        public static List<List<MserResult>> RemoveContainedRects(List<List<MserResult>> groups)
        {
            var cleanedGroups = new List<List<MserResult>>();

            foreach (var group in groups)
            {
                var cleanedGroup = group
                    .Where(r1 => !group.Any(r2 => r2 != r1 && IsContained(r1.BBox, r2.BBox)))
                    .ToList();

                if (cleanedGroup.Count > 0)
                    cleanedGroups.Add(cleanedGroup);
            }

            return cleanedGroups;
        }
        public static List<MserResult> RemoveContainedRects(List<MserResult> rects)
        {
            return rects.Where(r1 =>
                !rects.Any(r2 =>
                    r2 != r1 && IsContained(r1.BBox, r2.BBox))
            ).ToList();
        }

        private static bool IsContained(Rect inner, Rect outer)
        {
            return outer.Contains(new OpenCvSharp.Point(inner.X, inner.Y)) &&
                   outer.Contains(new OpenCvSharp.Point(inner.X + inner.Width, inner.Y + inner.Height));
        }

        public static List<MserResult> SelectSimilarItemsFromGroupsHybrid(List<List<MserResult>> groups)
        {
            var selectedItems = new List<MserResult>();

            if (groups == null || groups.Count == 0)
                return selectedItems;

            int previousRight = 0;

            foreach (var group in groups)
            {
                if (group.Count == 0)
                    continue;

                // Grup içindeki median boyutları hesapla
                double medianWidth = CalculateMedian(group.Select(r => (double)r.BBox.Width).ToList());
                double medianHeight = CalculateMedian(group.Select(r => (double)r.BBox.Height).ToList());

                // Önceki karakterin sağından başlayan rect'leri filtrele
                var validRects = group.Where(r => r.BBox.X >= previousRight).ToList();

                if (validRects.Count == 0)
                {
                    // Eğer hiçbir rect uymuyorsa grubu atlamak yerine tüm grubu kullan (segment atlamamak için)
                    validRects = group;
                }

                // Medyan değerlere göre en yakın olanı seç
                var closestItem = validRects.OrderBy(r =>
                    Math.Abs(r.BBox.Width - medianWidth) +
                    Math.Abs(r.BBox.Height - medianHeight)
                ).FirstOrDefault();

                if (closestItem != null)
                {
                    selectedItems.Add(closestItem);
                    previousRight = closestItem.BBox.Right;
                }
            }

            // Son olarak X'e göre sırala
            return selectedItems.OrderBy(r => r.BBox.X).ToList();
        }
        public static List<List<MserResult>> MergeTouchingRectsInGroupsSafe(List<List<MserResult>> groups, int xGapThreshold = 3, int heightTolerance = 3)
        {
            List<List<MserResult>> mergedGroups = new List<List<MserResult>>();

            foreach (var group in groups)
            {
                List<MserResult> newGroup = new List<MserResult>(group);

                for (int i = 0; i < newGroup.Count - 1; i++)
                {
                    var a = newGroup[i];
                    var b = newGroup[i + 1];

                    int gap = b.BBox.X - a.BBox.Right;
                    int heightDiff = Math.Abs(a.BBox.Height - b.BBox.Height);
                    int widthA = a.BBox.Width;
                    int widthB = b.BBox.Width;

                    // ⚠️ Güvenlik kontrolleri:
                    bool widthsTooSmall = widthA < 8 || widthB < 8;  // I harfi gibi çok dar karakterler varsa birleşme iptal
                    bool aspectTooExtreme = (double)widthA / widthB > 2.5 || (double)widthB / widthA > 2.5; // çok orantısızsa iptal
                    bool areaTooDifferent = Math.Abs((a.BBox.Width * a.BBox.Height) - (b.BBox.Width * b.BBox.Height)) > 150; // alan farkı çoksa iptal

                    if (gap >= 0 && gap <= xGapThreshold &&
                        heightDiff <= heightTolerance &&
                        !widthsTooSmall &&
                        !aspectTooExtreme &&
                        !areaTooDifferent)
                    {
                        // Uygun birleşim → Union dikdörtgeni oluştur
                        var unionRect = a.BBox | b.BBox;

                        var merged = new MserResult
                        {
                            BBox = unionRect
                            // gerekiyorsa: merged.Region = MergeRegion(a.Region, b.Region);
                        };

                        newGroup.RemoveAt(i);
                        newGroup.RemoveAt(i); // eski b'yi de çıkar
                        newGroup.Insert(i, merged);

                        i = -1; // Baştan başla, çünkü liste değişti
                    }
                }

                mergedGroups.Add(newGroup);
            }

            return mergedGroups;
        }

        public static List<MserResult> FilterNestedRects(List<MserResult> rects, double containmentThreshold = 0.9)
        {
            List<MserResult> filtered = new List<MserResult>();

            for (int i = 0; i < rects.Count; i++)
            {
                Rect rectA = rects[i].BBox;
                bool isContained = false;

                for (int j = 0; j < rects.Count; j++)
                {
                    if (i == j) continue;

                    Rect rectB = rects[j].BBox;

                    // Eğer rectA, rectB'nin içinde ve ondan %90'dan küçükse — küçük bir iç parça olabilir
                    if (rectB.Contains(rectA) && Area(rectA) < containmentThreshold * Area(rectB))
                    {
                        isContained = true;
                        break;
                    }
                }

                if (!isContained)
                {
                    filtered.Add(rects[i]);
                }
            }

            return filtered;
        }

        private static int Area(Rect rect)
        {
            return rect.Width * rect.Height;
        }

        private static List<MserResult> FillMissingCharacters(List<MserResult> characters)
        {
            List<MserResult> completedCharacters = new List<MserResult>(characters);
            characters = characters.OrderBy(c => c.BBox.X).ToList();

            for (int i = 0; i < characters.Count - 1; i++)
            {
                var current = characters[i];
                var next = characters[i + 1];

                double avgWidth = characters.Average(c => c.BBox.Width);
                double gap = next.BBox.X - (current.BBox.X + current.BBox.Width);

                if (gap > avgWidth * 0.7) // Eğer iki karakter arasında büyük bir boşluk varsa
                {
                    Rect missingCharRect = new Rect(current.BBox.X + current.BBox.Width + (int)(avgWidth * 0.3), current.BBox.Y, (int)avgWidth, current.BBox.Height);
                    completedCharacters.Add(new MserResult { BBox = missingCharRect, Points = new OpenCvSharp.Point[0] });
                }
            }

            return completedCharacters;
        }


        public static List<MserResult> SelectSimilarItemsFromGroupsWithMedianByGPT(List<List<MserResult>> groups)
        {
            var selectedItems = new List<MserResult>();

            if (groups == null || groups.Count == 0)
                return selectedItems;

            foreach (var group in groups)
            {
                // Bu grup içindeki median yükseklik ve genişlik değerlerini hesapla
                double medianWidth = CalculateMedian(group.Select(r => (double)r.BBox.Width).ToList());
                double medianHeight = CalculateMedian(group.Select(r => (double)r.BBox.Height).ToList());

                // Bu median değerlere en yakın rect'i seç
                var closestItem = group.OrderBy(r =>
                    Math.Abs(r.BBox.Width - medianWidth) +
                    Math.Abs(r.BBox.Height - medianHeight)
                ).FirstOrDefault();

                if (closestItem != null)
                    selectedItems.Add(closestItem);
            }

            // X koordinatına göre son sıralama (soldan sağa doğru düzenli)
            selectedItems = selectedItems.OrderBy(r => r.BBox.X).ToList();

            return selectedItems;
        }

        public static List<MserResult> SelectSimilarItemsFromGroupsWithMedianv0(List<List<MserResult>> groups)
        {
            List<MserResult> selectedItems = new List<MserResult>();

            if (groups == null || groups.Count == 0)
                return selectedItems;

            var allRects = groups.SelectMany(g => g).ToList();

            double medianWidth = CalculateMedian(allRects.Select(r => (double)r.BBox.Width).ToList());
            double medianHeight = CalculateMedian(allRects.Select(r => (double)r.BBox.Height).ToList());

            int previousRight = 0;

            var ldas = medianHeight * 0.6;

            foreach (var group in groups)
            {
                //var validRects = group
                //    .Where(r =>
                //        r.BBox.X >= previousRight &&
                //        r.BBox.Height >= medianHeight * 0.6 &&
                //        (r.BBox.Width >= medianWidth * 0.65 || r.BBox.Width <= medianWidth * 0.5)
                //    ).ToList();


                //var validRects = group
                //    .Where(r =>
                //        r.BBox.X >= previousRight &&
                //        r.BBox.Height >= medianHeight  && r.BBox.Width >= medianWidth
                //        //(r.BBox.Width >= medianWidth * 0.65 || r.BBox.Width <= medianWidth * 0.5)
                //    ).ToList();


                //var validRects = group.Where(r => r.BBox.X >= previousRight).ToList();

                var validRects = group

                   .Where(r => r.BBox.Height >= medianHeight &&
                       (r.BBox.Width >= medianWidth)

                   ).Where(r => r.BBox.X >= previousRight).ToList();




                if (validRects.Count > 0)
                {
                    var closestItem = validRects.OrderBy(r =>
                        Math.Abs(r.BBox.Width - medianWidth) +
                        Math.Abs(r.BBox.Height - medianHeight)
                    ).First();

                    selectedItems.Add(closestItem);
                    previousRight = closestItem.BBox.Right;
                }
            }

            return selectedItems;
        }

        public static List<MserResult> SelectSimilarItemsFromGroupsWithMedian(List<List<MserResult>> groups)
        {
            List<MserResult> selectedItems = new List<MserResult>();

            if (groups == null || groups.Count == 0)
                return selectedItems;

            var allRects = groups.SelectMany(g => g).ToList();

            double medianWidth = CalculateMedian(allRects.Select(r => (double)r.BBox.Width).ToList());
            double medianHeight = CalculateMedian(allRects.Select(r => (double)r.BBox.Height).ToList());

            double minWidthThreshold = medianWidth * 0.65;
            double minHeightThreshold = medianHeight * 0.6;

            int previousRight = 0;

            foreach (var group in groups)
            {
                // Çok küçük karakterleri (gürültüleri) filtrele
                var filteredGroup = group
                    .Where(r => r.BBox.Width >= minWidthThreshold && r.BBox.Height >= minHeightThreshold)
                    .Where(r => r.BBox.X >= previousRight) // önceki karakterden sonra gelsin
                    .ToList();

                if (filteredGroup.Count > 0)
                {
                    MserResult closestItem = filteredGroup.OrderBy(r =>
                        Math.Abs(r.BBox.Width - medianWidth) +
                        Math.Abs(r.BBox.Height - medianHeight)
                    ).First();

                    selectedItems.Add(closestItem);
                    previousRight = closestItem.BBox.Right;
                }
            }

            return selectedItems;
        }

        public static List<MserResult> SelectSimilarItemsFromGroupsWithMedianvCalisan(List<List<MserResult>> groups)
        {
            List<MserResult> selectedItems = new List<MserResult>();

            // Eğer groups listesi boşsa, direkt boş liste döndür
            if (groups == null || groups.Count == 0)
            {
                return selectedItems;
            }

            // Tüm bounding box'ların genişlik, yükseklik, X ve Y medyanlarını hesapla (tüm gruplar genelinde)
            var allRects = groups.SelectMany(g => g).ToList();

            double medianWidth = CalculateMedian(allRects.Select(r => (double)r.BBox.Width).ToList()); //allRects.Average(r => (double)r.BBox.Width);  //
            double medianHeight = CalculateMedian(allRects.Select(r => (double)r.BBox.Height).ToList());
            double medianX = CalculateMedian(allRects.Select(r => (double)r.BBox.X).ToList());
            double medianY = CalculateMedian(allRects.Select(r => (double)r.BBox.Y).ToList());

            int previousRight = 0;  // İlk bounding box'tan önceki sağ kenar başlangıcı 0

            // Her gruptan medyan değerlere en yakın olan item'i seç
            foreach (var group in groups)
            {
                // Önceki bounding box'ın bittiği yerden (previousRight) daha büyük olanları filtrele
                var validRects = group.Where(r => r.BBox.X >= previousRight).ToList();

                if (validRects.Count > 0)
                {
                    // Medyan değerlere göre en yakın olan item'i seç
                    MserResult closestItem = validRects.OrderBy(r =>
                        Math.Abs(r.BBox.Width - medianWidth)
                        + Math.Abs(r.BBox.Height - medianHeight)

                    //Math.Abs(r.BBox.X - medianX)
                    //Math.Abs(r.BBox.Y - medianY)y
                    ).First();

                    // Seçilen item'ı ekle
                    selectedItems.Add(closestItem);

                    // Seçilen item'in sağ kenarını güncelle
                    previousRight = closestItem.BBox.Right;
                }
            }

            return selectedItems;
        }
        // Medyan hesaplama fonksiyonu
        public static double CalculateMedian(List<double> values)
        {
            values.Sort();
            int count = values.Count;

            if (count % 2 == 0)
            {
                // Çift sayı ise, ortadaki iki değerin ortalamasını al
                return (values[count / 2 - 1] + values[count / 2]) / 2.0;
            }
            else
            {
                // Tek sayı ise, ortadaki değeri al
                return values[count / 2];
            }
        }

        private static double CalculateMedian(List<int> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;

            if (count % 2 == 0)
            {
                // Çift sayıda değer varsa ortadaki iki değerin ortalamasını al
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
            }
            else
            {
                // Tek sayıda değer varsa ortadaki değeri al
                return sortedValues[count / 2];
            }
        }

        public static List<MserResult> FilterGroupsByHeightMedianPercentageThreshold(List<MserResult> groups, double percentageThreshold = 0.2)
        {
            if (groups == null || groups.Count == 0)
                return groups;

            double medianHeight = CalculateMedian(groups.Select(r => r.BBox.Height).ToList());

            // Medyana göre yüzdesel sapma hesabıyla filtreleme
            var filteredGroups = groups.Where(r =>
            {
                double deviationRatio = Math.Abs(r.BBox.Height - medianHeight) / medianHeight;
                return deviationRatio <= percentageThreshold;
            }).ToList();

            return filteredGroups;
        }
        public static List<MserResult> FilterGroupsByHeightMedian(List<MserResult> groups, double medianThreshold = 2)
        {
            // Eğer groups listesi boşsa, direkt boş liste döndür
            if (groups == null || groups.Count == 0)
            {
                return groups;
            }

            // Tüm bounding box'ların yüksekliklerini hesaplayalım
            var heights = groups.Select(r => r.BBox.Height).ToList();

            // Yüksekliklerin medyanını hesapla
            double medianHeight = CalculateMedian(heights);

            // Aykırı değerleri filtrelemek için valid bir liste oluştur
            List<MserResult> filteredGroups = new List<MserResult>();

            foreach (var rect in groups)
            {
                double height = rect.BBox.Height;

                // Medyan ile farkını hesapla
                double deviation = Math.Abs(height - medianHeight);

                // Eğer sapma threshold içinde ise bu elemanı valid listesine ekle
                if (deviation <= medianThreshold)
                {
                    filteredGroups.Add(rect);
                }
            }

            return filteredGroups;  // Aykırı olanlar silinmiş yeni grup
        }
    }
}
