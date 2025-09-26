using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class PlateScoringHelper
    {
        public static void ComputePlateScore(PossiblePlate plate)
        {
            // 1️⃣ Aspect Ratio
            double aspectRatio = RectGeometryHelper.CalculateAspectRatio(plate.addedRects);
            plate.AspectRatioScore = 1.0 / (1.0 + Math.Abs(aspectRatio - 4.5)); // 4.8'e yakın olanlar daha yüksek puan alır

            // 2️⃣ Blur Score
            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(plate.possiblePlateRegions);
            double blurScore = ImageEnhancementHelper.ComputeLaplacianBlurScore(grayPlate);
            //plate.BlurScore = Math.Min(blurScore / 600.0, 1.0); // 100'ü normalize ediyoruz
            //double normalizedBlurScore = Math.Min(blurScore, 1400) / 600.0;
            double normalizedBlurScore = blurScore / 1200.0;
            plate.BlurScore = Math.Min(normalizedBlurScore, 1.0);

            // 3️⃣ Edge Density Score
            Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayPlate);
            double edgeDensity = (double)Cv2.CountNonZero(sobelEdges) / (sobelEdges.Rows * sobelEdges.Cols);
            plate.EdgeDensityScore = Math.Min(edgeDensity * 1.2, 1.0); // Normalize

            // 🎯 4️⃣ Final Plate Score
            //plate.PlateScore = (plate.AspectRatioScore * 0.4) + (plate.BlurScore * 0.3) + (plate.EdgeDensityScore * 0.3);

            plate.PlateScore = (plate.AspectRatioScore * 0.35) + (plate.BlurScore * 0.4) + (plate.EdgeDensityScore * 0.25);

            //plate.PlateScore = (plate.BlurScore * 0.6) + (plate.EdgeDensityScore * 0.4);

            //plate.PlateScore = (plate.AspectRatioScore * 0.2) + (plate.BlurScore * 0.4) + (plate.EdgeDensityScore * 0.4);
        }

        public static void ComputePlateScoreAdaptive(PossiblePlate plate)
        {
            // 1️⃣ Aspect Ratio
            double aspectRatio = RectGeometryHelper.CalculateAspectRatio(plate.addedRects);
            plate.AspectRatioScore = 1.0 / (1.0 + Math.Abs(aspectRatio - 4.3)); // 4.3'e yakın olanlar daha yüksek puan alır

            // 2️⃣ Blur Score
            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(plate.possiblePlateRegions);
            double blurScore = ImageEnhancementHelper.ComputeLaplacianBlurScore(grayPlate);

            // 🔥 Adaptive Blur Normalization
            double blurNormalizationFactor;
            if (blurScore > 800)
                blurNormalizationFactor = 800.0;
            else if (blurScore > 500)
                blurNormalizationFactor = 600.0;
            else
                blurNormalizationFactor = 400.0;

            double normalizedBlurScore = Math.Min(blurScore, blurNormalizationFactor * 2) / blurNormalizationFactor;
            plate.BlurScore = Math.Min(normalizedBlurScore, 1.0);

            // 3️⃣ Edge Density Score
            Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayPlate);
            double edgeDensity = (double)Cv2.CountNonZero(sobelEdges) / (sobelEdges.Rows * sobelEdges.Cols);

            // 🔥 Adaptive Edge Density Normalization
            double edgeNormalizationFactor;
            if (blurScore > 800)
                edgeNormalizationFactor = 1.0;
            else if (blurScore > 500)
                edgeNormalizationFactor = 1.2;
            else
                edgeNormalizationFactor = 1.5;

            plate.EdgeDensityScore = Math.Min(edgeDensity * edgeNormalizationFactor, 1.0);

            // 🎯 4️⃣ Final Plate Score
            plate.PlateScore = (plate.AspectRatioScore * 0.35) + (plate.BlurScore * 0.4) + (plate.EdgeDensityScore * 0.25);
        }

        public static void ComputePlateScoreAdaptivevNew(PossiblePlate plate)
        {
            // 1️⃣ Aspect Ratio (kalsın ya da kaldırılabilir, opsiyonel)
            double aspectRatio = RectGeometryHelper.CalculateAspectRatio(plate.addedRects);
            plate.AspectRatioScore = 1.0 / (1.0 + Math.Abs(aspectRatio - 4.5));

            // 2️⃣ Blur Score
            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(plate.possiblePlateRegions);
            double blurScore = ImageEnhancementHelper.ComputeLaplacianBlurScore(grayPlate);
            double normalizedBlurScore = blurScore / 1200.0;
            plate.BlurScore = Math.Min(normalizedBlurScore, 1.0);

            // 3️⃣ Edge Density Score
            Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayPlate);
            double edgeDensity = (double)Cv2.CountNonZero(sobelEdges) / (sobelEdges.Rows * sobelEdges.Cols);

            // 🔥 Adaptive Edge Normalization based on each cropped plate's blur
            double edgeNormalizationFactor;
            if (blurScore > 3500)
                edgeNormalizationFactor = 1.0; // Çok net - çok sıkı
            else if (blurScore > 2000)
                edgeNormalizationFactor = 1.2; // Net ama biraz tolerans
            else if (blurScore > 1000)
                edgeNormalizationFactor = 1.4; // Orta netlik - daha toleranslı
            else
                edgeNormalizationFactor = 1.6; // Çok bulanık - daha tolerans

            plate.EdgeDensityScore = Math.Min(edgeDensity * edgeNormalizationFactor, 1.0);

            // 🎯 4️⃣ Final Plate Score
            //plate.PlateScore = (plate.BlurScore * 0.4) + (plate.EdgeDensityScore * 0.4);
            plate.PlateScore = (plate.AspectRatioScore * 0.2) + (plate.BlurScore * 0.5) + (plate.EdgeDensityScore * 0.3);
        }

        public static void ComputePlateScoreAdaptivev1(PossiblePlate plate)
        {
            // 1️⃣ Aspect Ratio Score (isteğe bağlı aktif)
            double aspectRatio = RectGeometryHelper.CalculateAspectRatio(plate.addedRects);
            plate.AspectRatioScore = 1.0 / (1.0 + Math.Abs(aspectRatio - 4.5)); // 4.5 ya da 4.8 ayarlanabilir

            // 2️⃣ Blur Score
            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(plate.possiblePlateRegions);
            double blurScore = ImageEnhancementHelper.ComputeLaplacianBlurScore(grayPlate);

            double plateWidth = plate.addedRects.Width;
            double plateHeight = plate.addedRects.Height;
            double plateSize = Math.Min(plateWidth, plateHeight);

            double dynamicBlurReference;

            if (plateSize < 40)
                dynamicBlurReference = 500.0;
            else if (plateSize < 70)
                dynamicBlurReference = 800.0;
            else
                dynamicBlurReference = 1200.0;

            double normalizedBlurScore = Math.Min(blurScore / dynamicBlurReference, 1.0);
            plate.BlurScore = normalizedBlurScore;

            // 3️⃣ Edge Density Score (blur ile adaptif)
            Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayPlate);
            double edgeDensity = (double)Cv2.CountNonZero(sobelEdges) / (sobelEdges.Rows * sobelEdges.Cols);

            double edgeDensityMultiplier;

            //if (blurScore < 500)
            //    edgeDensityMultiplier = 1.5;
            //else if (blurScore < 800)
            //    edgeDensityMultiplier = 1.2;
            //else
            //    edgeDensityMultiplier = 1.0;


            if (blurScore > 1200)
                edgeDensityMultiplier = 1.0;
            else if (blurScore > 800)
                edgeDensityMultiplier = 1.2;
            else if (blurScore > 500)
                edgeDensityMultiplier = 1.4;
            else
                edgeDensityMultiplier = 1.6;

            double normalizedEdgeDensity = Math.Min(edgeDensity * edgeDensityMultiplier, 1.0);
            plate.EdgeDensityScore = normalizedEdgeDensity;

            // 🎯 4️⃣ Final Score (AspectRatio opsiyonel katılımı)
            // Eğer AspectRatio'yu dışlamak istersen burayı değiştirirsin
            plate.PlateScore = (plate.BlurScore * 0.6) + (plate.EdgeDensityScore * 0.4);
            //plate.PlateScore = (plate.AspectRatioScore * 0.2) + (plate.BlurScore * 0.5) + (plate.EdgeDensityScore * 0.3);
        }

        public static void ComputePlateScoreAdaptiveKenar(PossiblePlate plate)
        {
            double aspectRatio = RectGeometryHelper.CalculateAspectRatio(plate.addedRects);
            plate.AspectRatioScore = 1.0 / (1.0 + Math.Abs(aspectRatio - 4.5));

            Mat grayPlate = ImagePreProcessingHelper.ColorMatToGray(plate.possiblePlateRegions);
            double blurScore = ImageEnhancementHelper.ComputeLaplacianBlurScore(grayPlate);

            double plateWidth = plate.addedRects.Width;
            double plateHeight = plate.addedRects.Height;
            double plateSize = Math.Min(plateWidth, plateHeight);

            double dynamicBlurReference;
            if (plateSize < 40)
                dynamicBlurReference = 500.0;
            else if (plateSize < 70)
                dynamicBlurReference = 800.0;
            else
                dynamicBlurReference = 1200.0;

            double normalizedBlurScore = Math.Min(blurScore / dynamicBlurReference, 1.0);
            plate.BlurScore = normalizedBlurScore;

            Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayPlate);
            double edgeDensity = (double)Cv2.CountNonZero(sobelEdges) / (sobelEdges.Rows * sobelEdges.Cols);

            double edgeDensityMultiplier;
            if (blurScore < 500)
                edgeDensityMultiplier = 1.5;
            else if (blurScore < 800)
                edgeDensityMultiplier = 1.2;
            else
                edgeDensityMultiplier = 1.0;

            double normalizedEdgeDensity = Math.Min(edgeDensity * edgeDensityMultiplier, 1.0);
            plate.EdgeDensityScore = normalizedEdgeDensity;

            // 🎯 Final skor
            plate.PlateScore = (plate.BlurScore * 0.6) + (plate.EdgeDensityScore * 0.4);

            // 📛  Kenara değiyor mu? Kontrol edelim
            if (Plate.IsBoxTouchingFrameEdge(plate.addedRects, plate.possiblePlateRegions.Width, plate.possiblePlateRegions.Height, margin: 2))
            {
                plate.PlateScore *= 0.85; // %15 ceza ver
            }
        }
    }
}
