using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class PlateHelper
    {
        public static void DrawPossiblePlateRegionToOriginalImage(List<PossiblePlate> possibleRegions, Mat originalImage)
        {
            // Her bir MSER bölgesini rastgele renklendirin, büyükten küçüğe doğru
            Random rng = new Random();

            foreach (PossiblePlate possibleRegion in possibleRegions)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(originalImage, possibleRegion.addedRects, randomColor, 2);

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(originalImage));

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(possibleRegion.possiblePlateRegions));


            }
        }


        public static void DrawPossiblePlateRegionToOriginalImage(Rect possibleRegions, Mat originalImage)
        {
            // Her bir MSER bölgesini rastgele renklendirin, büyükten küçüğe doğru
            Random rng = new Random();

            //foreach (PossiblePlate possibleRegion in possibleRegions)
            {
                Scalar randomColor = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)); // Rastgele renk oluştur
                Cv2.Rectangle(originalImage, possibleRegions, randomColor, 2);

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxFirstChannel, BitmapConverter.ToBitmap(originalImage));

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(possibleRegion.possiblePlateRegions));


            }
        }

        public static PlateResult SelectBestPlateFromCandidates(ThreadSafeList<PlateResult> plateCandidates)
        {
            if (plateCandidates == null || plateCandidates.Count == 0)
                return null;

            if (plateCandidates.Count == 1)
                return plateCandidates[0];

            // 1️⃣ İlk plaka yazısını al (normalize)
            string firstPlate = plateCandidates[0].readingPlateResult?.Trim().ToUpperInvariant();

            // 2️⃣ Bütün plaka yazıları aynı mı kontrol et
            bool allSame = plateCandidates.All(p => (p.readingPlateResult?.Trim().ToUpperInvariant()) == firstPlate);

            if (allSame)
            {
                // 3️⃣ Hepsi aynıysa güvenlik skoruna göre en iyiyi seç
                PlateResult bestPlate = plateCandidates.OrderByDescending(p => p.readingPlateResultProbability).First();
                return bestPlate;
            }

            // 4️⃣ Eğer hepsi aynı değilse, ileride burada başka kurallara geçeceğiz (heuristic vs.)
            return null; // Şimdilik null döndür, sonra geliştiririz
        }

        public static PlateResult SelectBestPlateFromCandidatesWithCorrection(ThreadSafeList<PlateResult> plateCandidates)
        {
            if (plateCandidates == null || plateCandidates.Count == 0)
                return null;

            if (plateCandidates.Count == 1)
            {
                //türkçeplaka için
                var only = plateCandidates[0];

                string cleaned = PlateHelper.ExtractProbableTurkishPlate(only.readingPlateResult);
                if (!string.IsNullOrEmpty(cleaned))
                    only.readingPlateResult = cleaned;

                return only;
            }
                

            string firstPlate = plateCandidates[0].readingPlateResult?.Trim().ToUpperInvariant();

            bool allSame = plateCandidates.All(p => (p.readingPlateResult?.Trim().ToUpperInvariant()) == firstPlate);

            if (allSame)
            {
                var best = plateCandidates.OrderByDescending(p => p.readingPlateResultProbability).First();

                // Plaka metnini temizlemeye çalış (örneğin "I34AB1234" → "34AB1234")
                string cleaned = PlateHelper.ExtractProbableTurkishPlate(best.readingPlateResult);

                if (!string.IsNullOrEmpty(cleaned))
                    best.readingPlateResult = cleaned;

                return best;
            }

            return null; // hepsi aynı değilse dışarıya bırak
        }


        public static List<PlateResult> GroupAndRankPlateCandidates(ThreadSafeList<PlateResult> plateCandidates)
        {
            if (plateCandidates == null || plateCandidates.Count == 0)
                return new List<PlateResult>();

            // 1️⃣ Gruplama
            var groupedPlates = plateCandidates
                .GroupBy(p => p.readingPlateResult?.Trim().ToUpperInvariant()) // normalize ederek grupla
                .ToList();

            // 2️⃣ Her grubun içinden en yüksek güven skorlu adayı seç
            List<PlateResult> bestPlatesFromGroups = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                var bestInGroup = group.OrderByDescending(p => p.readingPlateResultProbability).First();
                bestPlatesFromGroups.Add(bestInGroup);
            }

            // 3️⃣ Seçilen adayları skorlarına göre sırala
            var sortedPlates = bestPlatesFromGroups
                .OrderByDescending(p => p.readingPlateResultProbability)
                .ToList();

            return sortedPlates;
        }
        public static List<PlateResult> SelectMostConfidentGroupedPlates(List<List<PlateResult>> groupedPlates)
        {
            List<PlateResult> bestPlates = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                if (group == null || group.Count == 0)
                    continue;

                // Aynı plaka metnine sahip olanları grupla
                var groupedByPlateText = group
                    .GroupBy(p => p.readingPlateResult)
                    .ToList();

                foreach (var samePlateGroup in groupedByPlateText)
                {
                    // Aynı plaka metnine sahip olanlar arasında en yüksek güven skorunu bul
                    var mostConfident = samePlateGroup
                        .OrderByDescending(p => p.readingPlateResultProbability)
                        .First();

                    bestPlates.Add(mostConfident);
                }
            }

            // Sonuçları genel güven skoruna göre sırala
            var finalSortedList = bestPlates
                .OrderByDescending(p => p.readingPlateResultProbability)
                .ToList();

            return finalSortedList;
        }
        public static List<List<PlateResult>> GroupPlatesByProximity(ThreadSafeList<PlateResult> plates, int maxYDiff = 20, int maxXDiff = 250)
        {
            List<List<PlateResult>> groups = new List<List<PlateResult>>();
            List<PlateResult> platesToProcess = new List<PlateResult>(plates); // Değiştirilebilir kopya

            while (platesToProcess.Count > 0)
            {
                PlateResult reference = platesToProcess[0];
                platesToProcess.RemoveAt(0);

                List<PlateResult> currentGroup = new List<PlateResult> { reference };

                // Referans plakaya yakın olanları bul
                for (int i = platesToProcess.Count - 1; i >= 0; i--)
                {
                    PlateResult candidate = platesToProcess[i];

                    // Burada merkez noktalara göre kıyaslama yapıyoruz
                    var refCenter = GetRectCenter(reference.addedRects);
                    var candCenter = GetRectCenter(candidate.addedRects);

                    double yDiff = Math.Abs(refCenter.Y - candCenter.Y);
                    double xDiff = Math.Abs(refCenter.X - candCenter.X);

                    if (yDiff <= maxYDiff && xDiff <= maxXDiff)
                    {
                        currentGroup.Add(candidate);
                        platesToProcess.RemoveAt(i);
                    }
                }

                groups.Add(currentGroup);
            }

            return groups;
        }

        private static OpenCvSharp.Point GetRectCenter(OpenCvSharp.Rect rect)
        {
            return new OpenCvSharp.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        public static List<PlateResult> SelectBestPlatesFromGroups(List<List<PlateResult>> groupedPlates)
        {
            List<PlateResult> bestPlates = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                if (group == null || group.Count == 0)
                    continue;

                // En yüksek güven skoruna sahip olanı al
                PlateResult bestInGroup = group
                    .OrderByDescending(p => p.readingPlateResultProbability)
                    .First();

                bestPlates.Add(bestInGroup);
            }

            return bestPlates;
        }

        public static List<PlateResult> SelectBestTurkishPlatesFromGroups(List<List<PlateResult>> groupedPlates)
        {
            List<PlateResult> bestPlates = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                if (group == null || group.Count == 0)
                    continue;

                // Türk plaka desenine uyanları filtrele
                var turkishCandidates = group
                    .Where(p => PlateFormatHelper.IsProbablyTurkishPlate(p.readingPlateResult))
                    .ToList();

                if (turkishCandidates.Count == 0)
                    continue;

                // Türk formatına uyanlar arasında en yüksek güven skorluyu al
                PlateResult bestInGroup = turkishCandidates
                    .OrderByDescending(p => p.readingPlateResultProbability)
                    .First();

                bestPlates.Add(bestInGroup);
            }

            return bestPlates;
        }

        public static List<PlateResult> SelectBestPlatesByConfidenceOnly(List<List<PlateResult>> groupedPlates)
        {
            List<PlateResult> bestPlates = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                if (group == null || group.Count == 0)
                    continue;

                // Her gruptan en yüksek güven skoruna sahip olanı seç
                var bestInGroup = group
                    .OrderByDescending(p => p.readingPlateResultProbability)
                    .First();

                bestPlates.Add(bestInGroup);
            }

            return bestPlates;
        }

        public static List<PlateResult> SelectBestTurkishPlatesFromGroupsvAhmet(List<List<PlateResult>> groupedPlates)
        {
            List<PlateResult> bestPlates = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                if (group == null || group.Count == 0)
                    continue;

                List<(PlateResult original, string cleanedPlate)> turkishCandidates = new List<(PlateResult, string)>();

                foreach (var plate in group)
                {
                    string cleaned = ExtractProbableTurkishPlate(plate.readingPlateResult);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        turkishCandidates.Add((plate, cleaned));
                    }
                }

                if (turkishCandidates.Count == 0)
                    continue;

                // En yüksek güven skorlu olanı seç
                //var bestInGroup = turkishCandidates
                //    .OrderByDescending(p => p.original.readingPlateResultProbability)
                //    .First();

                var bestInGroup = turkishCandidates
    .OrderByDescending(p => p.original.readingPlateResult.Length)                 // 1️⃣ Uzunluk önceliği
    .ThenByDescending(p => p.original.readingPlateResultProbability)             // 2️⃣ Aynı uzunlukta ise güven skoruna göre
    .First();

                // Seçilen plakanın temizlenmiş halini setle (opsiyonel)
                bestInGroup.original.readingPlateResult = bestInGroup.cleanedPlate;

                bestPlates.Add(bestInGroup.original);
            }

            return bestPlates;
        }

        public static List<PlateResult> SelectBestTurkishPlatesFromGroupsv1(List<List<PlateResult>> groupedPlates)
        {
            List<PlateResult> bestPlates = new List<PlateResult>();

            foreach (var group in groupedPlates)
            {
                if (group == null || group.Count == 0)
                    continue;

                List<(PlateResult original, string cleanedPlate)> turkishCandidates = new List<(PlateResult, string)>();

                foreach (var plate in group)
                {
                    string cleaned = ExtractProbableTurkishPlate(plate.readingPlateResult);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        turkishCandidates.Add((plate, cleaned));
                    }
                }

                if (turkishCandidates.Count == 0)
                    continue;

                // En yüksek güven skorlu olanı seç
                //var bestInGroup = turkishCandidates
                //    .OrderByDescending(p => p.original.readingPlateResultProbability)
                //    .First();

                var bestInGroup = turkishCandidates
    .OrderByDescending(p => p.original.readingPlateResult.Length)                 // 1️⃣ Uzunluk önceliği
    .ThenByDescending(p => p.original.readingPlateResultProbability)             // 2️⃣ Aynı uzunlukta ise güven skoruna göre
    .First();

                // Seçilen plakanın temizlenmiş halini setle (opsiyonel)
                bestInGroup.original.readingPlateResult = bestInGroup.cleanedPlate;

                bestPlates.Add(bestInGroup.original);
            }

            return bestPlates;
        }

        public static string ExtractProbableTurkishPlate(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length < 7)
                return "";

            string bestMatch = "";
            int bestLength = 0;

            for (int start = 0; start < raw.Length - 6; start++) // 6 çünkü min plaka uzunluğu 7
            {
                for (int length = 7; length <= Math.Min(9, raw.Length - start); length++)
                {
                    string candidate = raw.Substring(start, length);

                    if (PlateFormatHelper.IsProbablyTurkishPlate(candidate))
                    {
                        if (candidate.Length > bestLength)
                        {
                            bestMatch = candidate;
                            bestLength = candidate.Length;
                        }
                    }
                }
            }

            return bestMatch;
        }

        public static (string cleanedPlate, double adjustedScore) ExtractProbableTurkishPlateWithScore(string raw, TupleList<string, double> characterConfidences)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length < 6 || characterConfidences == null || characterConfidences.Count == 0)
                return ("", 0);

            string bestMatch = "";
            double bestScore = 0;

            for (int start = 0; start < raw.Length - 5; start++)
            {
                for (int length = 6; length <= Math.Min(9, raw.Length - start); length++)
                {
                    string candidate = raw.Substring(start, length);

                    if (PlateFormatHelper.IsProbablyTurkishPlate(candidate))
                    {
                        // Karakterleri eşleştirip skor ortalamasını hesapla
                        int matchStartIndex = raw.IndexOf(candidate, StringComparison.Ordinal);
                        if (matchStartIndex >= 0 && matchStartIndex + candidate.Length <= characterConfidences.Count)
                        {
                            var matchedScores = characterConfidences
                                .Skip(matchStartIndex)
                                .Take(candidate.Length)
                                .Select(t => t.Item2)
                                .ToList();

                            double avgScore = matchedScores.Average();

                            if (candidate.Length > bestMatch.Length || (candidate.Length == bestMatch.Length && avgScore > bestScore))
                            {
                                bestMatch = candidate;
                                bestScore = avgScore;
                            }
                        }
                    }
                }
            }

            return (bestMatch, bestScore);
        }
    }
}
