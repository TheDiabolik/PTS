using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class MserDetectionSettingsFactory
    {
        #region imagesize
        private const int BaseWidth = 640;
        private const int BaseHeight = 480;
        #endregion

        public static MserDetectionSettings GetPlateRegionSettings(int width, int height, string imageType = "gray", double? mean = null, double? contrast = null, double? stdDev = null)
        {
            double scale = Math.Sqrt((width * height) / (double)(BaseWidth * BaseHeight));

            MserDetectionSettings settings = new MserDetectionSettings
            {
                Delta = Math.Clamp((int)(5 * scale), 3, 8),
                MinArea = Math.Max((int)(60 * scale), 30),
                MaxArea = Math.Min((int)(5000 * scale * scale), (width * height) / 10),
                MaxVariation = 0.5,
                MinDiversity = 0.3,
                AreaThreshold = 1.01,
                MinMargin = 0.5,
                EdgeBlurSize = Math.Clamp((int)(5 * scale), 3, 9)
            };

            if (imageType == "gray")
            {
                if (mean.HasValue && mean > 200)
                {
                    settings.MaxVariation = 0.35;
                    settings.MinDiversity = 0.2;
                    settings.Delta = Math.Clamp((int)(6 * scale), 3, 8);
                }
                else if (mean.HasValue && mean < 80)
                {
                    settings.MinArea = Math.Max((int)(40 * scale), 20);
                    settings.MaxVariation = 0.55;
                    settings.Delta = Math.Max(4, settings.Delta - 1);
                }

                if (contrast.HasValue && contrast < 40)
                {
                    settings.MinArea = Math.Max((int)(40 * scale), 20);
                    settings.MaxVariation = 0.55;
                    settings.Delta = Math.Max(3, settings.Delta - 1);
                }
            }
            else if (imageType == "sobel")
            {
                settings.MinArea = Math.Max((int)(50 * scale), 25);
                settings.MaxVariation = 0.5;
                settings.MinDiversity = 0.2;

                settings.Delta = stdDev.HasValue && stdDev < 30 ? 3 : 5;
            }
            else if (imageType == "hybrid")
            {
                if (mean > 190 && stdDev < 40)
                {
                    settings.MaxVariation = 0.4;
                    settings.MinDiversity = 0.2;
                    settings.Delta = 4;
                }
                else if (mean < 100 && stdDev > 30)
                {
                    settings.MinArea = Math.Max((int)(50 * scale), 30);
                    settings.MaxVariation = 0.55;
                    settings.Delta = 5;
                }
                else
                {
                    settings.Delta = 5;
                    settings.MinArea = Math.Max((int)(60 * scale), 30);
                }
            }

            // Global stddev ayarlamaları
            if (stdDev.HasValue)
            {
                if (stdDev < 20)
                {
                    settings.MinArea = Math.Max((int)(30 * scale), 20);
                    settings.MaxVariation = Math.Min(0.6, settings.MaxVariation + 0.1);
                    settings.Delta = Math.Max(3, settings.Delta - 1);
                }
                else if (stdDev > 70)
                {
                    settings.MaxVariation = Math.Min(0.45, settings.MaxVariation);
                    settings.MinDiversity += 0.05;
                }
            }

            return settings;


        }

        public static MserDetectionSettings GetPlateRegionSettings(Mat image, string imageType)
        {
            // Ortalama değerleri hesapla
            double mean = Cv2.Mean(image).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(image);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(image);

            double scale = Math.Sqrt((image.Width * image.Height) / (double)(BaseWidth * BaseHeight));

            MserDetectionSettings settings = new MserDetectionSettings
            {
                Delta = Math.Clamp((int)(5 * scale), 3, 8),
                MinArea = Math.Max((int)(60 * scale), 30),
                MaxArea = Math.Min((int)(5000 * scale * scale), (image.Width * image.Height) / 10),
                MaxVariation = 0.5,
                MinDiversity = 0.3,
                AreaThreshold = 1.01,
                MinMargin = 0.5,
                EdgeBlurSize = Math.Clamp((int)(5 * scale), 3, 9)
            };

            if (imageType == "gray")
            {
                if (mean > 200)
                {
                    settings.MaxVariation = 0.35;
                    settings.MinDiversity = 0.2;
                    settings.Delta = Math.Clamp((int)(6 * scale), 3, 8);
                }
                else if (mean < 80)
                {
                    settings.MinArea = Math.Max((int)(40 * scale), 20);
                    settings.MaxVariation = 0.55;
                    settings.Delta = Math.Max(4, settings.Delta - 1);
                }

                if (contrast < 40)
                {
                    settings.MinArea = Math.Max((int)(40 * scale), 20);
                    settings.MaxVariation = 0.55;
                    settings.Delta = Math.Max(3, settings.Delta - 1);
                }
            }
            else if (imageType == "sobel")
            {
                settings.MinArea = Math.Max((int)(50 * scale), 25);
                settings.MaxVariation = 0.5;
                settings.MinDiversity = 0.2;

                settings.Delta = stdDev < 30 ? 3 : 5;
            }
            else if (imageType == "hybrid")
            {
                if (mean > 190 && stdDev < 40)
                {
                    settings.MaxVariation = 0.4;
                    settings.MinDiversity = 0.2;
                    settings.Delta = 4;
                }
                else if (mean < 100 && stdDev > 30)
                {
                    settings.MinArea = Math.Max((int)(50 * scale), 30);
                    settings.MaxVariation = 0.55;
                    settings.Delta = 5;
                }
                else
                {
                    settings.Delta = 5;
                    settings.MinArea = Math.Max((int)(60 * scale), 30);
                }
            }

            // Global stddev ayarlamaları

            if (stdDev < 20)
            {
                settings.MinArea = Math.Max((int)(30 * scale), 20);
                settings.MaxVariation = Math.Min(0.6, settings.MaxVariation + 0.1);
                settings.Delta = Math.Max(3, settings.Delta - 1);
            }
            else if (stdDev > 70)
            {
                settings.MaxVariation = Math.Min(0.45, settings.MaxVariation);
                settings.MinDiversity += 0.05;
            }

            return settings;


        }

        public static MserDetectionSettings GetPlateRegionSettingsForROI(Mat roiImage, string imageType)
        {
            double mean = Cv2.Mean(roiImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(roiImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(roiImage);

            double scale = Math.Sqrt((roiImage.Width * roiImage.Height) / (double)(BaseWidth * BaseHeight));

            MserDetectionSettings settings = new MserDetectionSettings
            {
                Delta = Math.Clamp((int)(5 * scale), 3, 7),
                MinArea = Math.Clamp((int)(60 * scale), 20, 80),
                MaxArea = Math.Clamp((int)(5000 * scale * scale), 200, 1500),
                MaxVariation = 0.5,
                MinDiversity = 0.3,
                AreaThreshold = 1.01,
                MinMargin = 0.005,
                EdgeBlurSize = Math.Clamp((int)(5 * scale), 3, 7)
            };

            // Diğer ışık, kontrast, stddev bazlı ayarlamalar...
            if (imageType == "gray")
            {
                if (mean > 200)
                {
                    settings.MaxVariation = 0.35;
                    settings.MinDiversity = 0.2;
                    settings.Delta = Math.Clamp((int)(6 * scale), 3, 7);
                }
                else if (mean < 80)
                {
                    settings.MinArea = Math.Max((int)(40 * scale), 20);
                    settings.MaxVariation = 0.55;
                    settings.Delta = Math.Max(3, settings.Delta - 1);
                }

                if (contrast < 40)
                {
                    settings.MinArea = Math.Max((int)(40 * scale), 20);
                    settings.MaxVariation = 0.55;
                    settings.Delta = Math.Max(3, settings.Delta - 1);
                }
            }
            else if (imageType == "sobel")
            {
                settings.MinArea = Math.Max((int)(50 * scale), 25);
                settings.MaxVariation = 0.5;
                settings.MinDiversity = 0.2;

                settings.Delta = stdDev < 30 ? 3 : 5;
            }
            else if (imageType == "hybrid")
            {
                if (mean > 190 && stdDev < 40)
                {
                    settings.MaxVariation = 0.4;
                    settings.MinDiversity = 0.2;
                    settings.Delta = 4;
                }
                else if (mean < 100 && stdDev > 30)
                {
                    settings.MinArea = Math.Max((int)(50 * scale), 30);
                    settings.MaxVariation = 0.55;
                    settings.Delta = 5;
                }
                else
                {
                    settings.Delta = 5;
                    settings.MinArea = Math.Max((int)(60 * scale), 30);
                }
            }

            if (stdDev < 20)
            {
                settings.MinArea = Math.Max((int)(30 * scale), 20);
                settings.MaxVariation = Math.Min(0.6, settings.MaxVariation + 0.1);
                settings.Delta = Math.Max(3, settings.Delta - 1);
            }
            else if (stdDev > 70)
            {
                settings.MaxVariation = Math.Min(0.45, settings.MaxVariation);
                settings.MinDiversity += 0.05;
            }

            return settings;
        }

        public static MserDetectionSettings GetCharacterRegionSettings(Mat plateImage)
        {
            double mean = Cv2.Mean(plateImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(plateImage);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(plateImage);

            MserDetectionSettings settings = new MserDetectionSettings
            {
                Delta = 4,
                MinArea = 18,
                MaxArea = 450,
                MaxVariation = 0.45,
                MinDiversity = 0.4,
                MinMargin = 0.5,
                EdgeBlurSize = 5
            };

            // 🌕 Çok parlak ve düşük varyasyon → karakter erimiş olabilir
            if (mean > 210 && stdDev < 12)
            {
                settings.MaxVariation = 0.35;
                settings.MinDiversity = 0.3;
                settings.MinArea = 22;
                settings.Delta = 5;
            }
            else if (mean > 210)
            {
                settings.MaxVariation = 0.35;
                settings.MinDiversity = 0.3;
                settings.MinArea = 20;
                settings.Delta = 5;
            }
            else if (mean > 180)
            {
                settings.MaxVariation = 0.38;
                settings.MinDiversity = 0.32;
                settings.MinArea = 18;
            }
            else if (mean < 80)
            {
                settings.MaxVariation = 0.5;
                settings.MinArea = 12;
                settings.Delta = 3;
            }

            // ⚡ Kontrast düşük → daha seçici varyasyon, küçük delta
            if (contrast < 25)
            {
                settings.MaxVariation = Math.Min(0.42, settings.MaxVariation);
                settings.Delta = 3;
                settings.MinArea = Math.Min(15, settings.MinArea);
            }

            // 📉 Varyasyon çok düşük → biraz daha toleranslı
            if (stdDev < 18)
            {
                settings.MaxVariation += 0.05;
                settings.MinDiversity = Math.Max(0.25, settings.MinDiversity - 0.05);
                settings.Delta = Math.Max(3, settings.Delta - 1);
            }
            else if (stdDev > 50)
            {
                settings.MaxVariation = Math.Min(0.4, settings.MaxVariation);
                settings.MinDiversity += 0.05;
            }
            else if (stdDev > 60)
            {
                settings.MaxVariation = Math.Min(0.35, settings.MaxVariation);
                settings.MinDiversity += 0.05;
            }

            // 🧩 Küçük plaka çözünürlüklerinde minArea esnet
            if (plateImage.Rows < 35)
            {
                settings.MinArea = Math.Min(settings.MinArea, 14);
            }

            return settings;
        }
    }
}
