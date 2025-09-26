using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class SceneEnhancementHelper
    {

        public static Mat ApplySmartEnhancementPipelinevelelele(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = frame.Clone();

            // 🔥 Yüksek kontrast ama gölgede kalmış sahne → CLAHE + Gamma
            if (contrast > 180 && meanBrightness < 180)
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }
            else if (meanBrightness < 80)
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
            }
            else if (contrast < 35) // Daha agresif CLAHE uygula
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
            }
            else if (contrast < 40 || stdDev < 20)
            {
                enhanced = ApplySmartCLAHE(enhanced);
            }
            else if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.5);
            }
            else if (meanBrightness >= 80 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }

            // ✨ Kontrast düşük ve sahne orta parlaklıktaysa: Detayları keskinleştir
            if (contrast < 40 && meanBrightness > 90 && meanBrightness < 160)
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.8);
            }

            return enhanced;
        }


        public static Mat ApplySmartEnhancementPipelineForBetaTest(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            // 1. Çok loş veya kontrastsız → CLAHE
            if (meanBrightness < 80 || contrast < 35 || stdDev < 20)
            {
                return ApplySmartCLAHEToGrayscaleAndMerge(frame);
            }

            // 2. Çok kontrastlı sahne → Gamma düşür
            if (contrast > 180)
            {
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast);
                return ImageEnhancementHelper.ApplyGammaCorrection(frame, gamma);
            }

            // ✅ 3. Orta-üst parlak sahne → Orta düzey gamma + beta (yeni blok)
            if (meanBrightness > 180 && meanBrightness <= 200)
            {
                return ImageEnhancementHelper.ApplyGammaAndBrightness(frame, gamma: 0.7, beta: -15);
            }


            // 3. Çok parlak sahne → Gamma + negatif beta
            if (meanBrightness > 200)
            {
                return ImageEnhancementHelper.ApplyGammaAndBrightness(frame, gamma: 0.5, beta: -30);
            }

            // 4. Orta parlaklıkta adaptif gamma + beta
            if (meanBrightness >= 80 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGammaForBetaTest(meanBrightness);
                double beta = ImageEnhancementHelper.GetAdaptiveBetaForBetaTest(meanBrightness);
                return ImageEnhancementHelper.ApplyGammaAndBrightness(frame, gamma, beta);
            }

            // 5. Kontrast düşük ama sahne makul → Unsharp mask
            if (contrast < 40 && meanBrightness > 90 && meanBrightness < 160)
            {
                return ImageEnhancementHelper.ApplyUnsharpMask(frame, strength: 0.8);
            }

            // 6. Fallback → Dokunmadan döndür
            return frame.Clone();
        }



        public static Mat ApplySmartEnhancementPipeline(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = frame.Clone();

            if (meanBrightness < 80)
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
            }
            else if (contrast < 35) // Daha agresif CLAHE uygula
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
            }
            else if (contrast < 40 || stdDev < 20)
            {
                enhanced = ApplySmartCLAHE(enhanced);
            }
            else if (contrast > 180)
            {
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }
            else if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.5);
            }
            else if (meanBrightness >= 80 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }

            if (contrast < 40 && meanBrightness > 90 && meanBrightness < 160)
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.8);
            }

            return enhanced;
        }


        public static Mat ApplySmartCLAHEToGrayscaleAndMerge(Mat frame)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);

            Mat[] labChannels;
            Cv2.Split(lab, out labChannels);

            double meanBrightness = Cv2.Mean(labChannels[0]).Val0;

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            // 🌞 Çok parlak sahne → lokal vurgu
            if (meanBrightness > 220)
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            // 🌤 Orta düzey parlaklık
            else if (meanBrightness > 170)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            // 🌑 Karanlık görüntüler → daha güçlü CLAHE
            else if (meanBrightness < 80)
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(labChannels[0], labChannels[0]);

            Cv2.Merge(labChannels, lab);
            Mat result = new Mat();
            Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

            return result;
        }

        public static Mat ApplySmartCLAHE(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            // 📌 Sahneye göre dinamik parametre ayarı
            if (meanBrightness > 220) // Aşırı parlak sahne
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness > 180) // Aydınlık sahne
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness < 80) // Karanlık sahne
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness < 120) // Hafif karanlık
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else // Dengeli sahne
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }

            // CLAHE uygula
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            // Kanalları birleştir
            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }
        public static Mat ApplySmartCLAHEToGray(Mat gray)
        {
            if (gray.Channels() > 1)
                throw new ArgumentException("Giriş gri görüntü olmalıdır.");

            double meanBrightness = Cv2.Mean(gray).Val0;

            // Dinamik parametre belirleme
            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness < 80)
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness < 170)
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else
            {
                clipLimit = 1.5;
                tileGridSize = new OpenCvSharp.Size(16, 16);
            }

            // CLAHE uygula
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            Mat enhancedGray = new Mat();
            clahe.Apply(gray, enhancedGray);

            return enhancedGray;
        }


        public static Mat AutoAdjustSmart(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat adjustedFrame = frame.Clone();

            if (meanBrightness < 80)
            {
                adjustedFrame = ApplySmartCLAHEToGray(frame);
            }
            else if (meanBrightness > 200)
            {
                adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, 0.5);
            }
            else if (contrast < 50 || stdDev < 20)
            {
                adjustedFrame = ApplySmartCLAHE(frame);
            }

            else if (meanBrightness >= 80 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness);
                adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, gamma);
            }

            return adjustedFrame;
        }
        public static Mat AutoAdjustLight(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            // Ortalama parlaklığı hesapla
            double meanBrightness = Cv2.Mean(gray).Val0;

            Mat adjustedFrame = frame.Clone(); // Orijinal görüntünün bir kopyasını al

            if (meanBrightness < 80) // Çok karanlık görüntüler için CLAHE uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyCLAHEToColor(frame); // Renkli görüntüye CLAHE uygula
            }
            else if (meanBrightness > 200) // Çok parlak görüntüler için Gamma düzeltmesi uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, 0.75); // Renkli görüntüye gamma uygula
            }

            return adjustedFrame;
        }

        public static Mat AutoAdjustLight2(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            // Ortalama parlaklığı hesapla
            double meanBrightness = Cv2.Mean(gray).Val0;

            Mat adjustedFrame = frame.Clone(); // Orijinal görüntünün bir kopyasını al

            if (meanBrightness < 80)  // Çok karanlık görüntüler için CLAHE uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyCLAHEToGrayscaleAndMerge(frame); // CLAHE'yi gri kanala uygula, sonra renklendir
            }
            else if (meanBrightness > 200)  // Çok parlak görüntüler için güçlü Gamma düzeltmesi uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, 0.5);
            }
            else if (meanBrightness >= 80 && meanBrightness <= 150)  // Orta parlaklıkta, hafif Gamma düzeltmesi uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, 0.7);
            }

            return adjustedFrame;
        }

        public static Mat AutoAdjustLight3(Mat frame)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            // Ortalama parlaklık ve kontrastı hesapla
            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat adjustedFrame = frame.Clone();

            if (contrast < 50 || stdDev < 20) // Düşük kontrastlı görüntüler için CLAHE uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyCLAHEToColor(frame);
            }
            else if (meanBrightness > 200) // Çok parlak görüntüler için Gamma düzeltmesi uygula
            {
                adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, 0.5);
            }

            //adjustedFrame = ImageEnhancementHelper.ApplyGammaCorrection(frame, 0.6);

            return adjustedFrame;
        }


    }
}
