using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class PlateEnhancementHelper
    {

        public static Mat ApplyPlateEnhancementPipelineForOriginalPlateImage(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🚨 Çok zayıf görüntüler (karanlık + kontrastsız)
            if (meanBrightness < 50 && contrast < 40)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEToGrayscaleAndMergeVOriginalPlateImage(enhanced);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.7);
            }
            // 🌑 Çok karanlık: Grayscale CLAHE + renklendirme
            else if (meanBrightness < 70)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEToGrayscaleAndMergeVOriginalPlateImage(enhanced);
            }
            // ☁️ Düşük kontrast ya da düşük varyasyon: Renkli CLAHE
            else if (contrast < 50 || stdDev < 20)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEvOriginalPlateImage(enhanced);
            }
            // 🌋 Aşırı kontrastlı plaka: Gamma düzeltme ile yumuşatma
            else if (contrast > 180)
            {
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }
            // 🔆 Çok parlak plaka: Sabit gamma bastırma
            else if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.4);
            }

            //else if (contrast > 180 || meanBrightness > 200)
            //{
            //    double gamma = (meanBrightness > 200) ? 0.4 : ImageEnhancementHelper.GetGammaForHighContrast(contrast);
            //    enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            //}
            // 🌤 Orta parlaklıkta: Yumuşak adaptif gamma
            else if (meanBrightness >= 70 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness,false);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }

            // ✨ Detay artırımı: Netlik (düşük varyasyonlu ama kurtarılabilir plakalar için)
            if ((contrast < 55 && meanBrightness > 90 && meanBrightness < 170) ||
                (contrast >= 55 && contrast < 70 && stdDev < 22))
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.85, blurSize: 3);
            }

            return enhanced;
        }

        public static Mat ApplyPlateEnhancementPipelineForOriginalPlateImageForBetaTest(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🚨 Çok zayıf görüntüler (karanlık + kontrastsız)
            if (meanBrightness < 50 && contrast < 40)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEToGrayscaleAndMergeVOriginalPlateImage(enhanced);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.7);
            }
            // 🌑 Çok karanlık: CLAHE + renklendirme
            else if (meanBrightness < 70)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEToGrayscaleAndMergeVOriginalPlateImage(enhanced);
            }
            // ☁️ Düşük kontrast ya da düşük varyasyon: Renkli CLAHE
            else if (contrast < 50 || stdDev < 20)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEvOriginalPlateImage(enhanced);
            }
            // 🌋 Aşırı kontrastlı plaka: Gamma düzeltme ile yumuşatma
            else if (contrast > 180)
            {
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }
            // 🔆 Çok parlak plaka: Gamma + beta
            else if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaAndBrightness(enhanced, 0.5, -30);
            }
            // 🌤 Orta-üst parlaklıkta, kontrastı düşük plaka → gamma + hafif beta
            else if (meanBrightness > 160 && meanBrightness <= 200 && contrast < 65)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaAndBrightness(enhanced, 0.7, -15);
            }
            // 🌥 Orta parlaklık → adaptif gamma
            else if (meanBrightness >= 70 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness, false);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }

            // ✨ Detay artırımı: Netlik (düşük varyasyonlu ama kurtarılabilir plakalar için)
            if ((contrast < 55 && meanBrightness > 90 && meanBrightness < 170) ||
                (contrast >= 55 && contrast < 70 && stdDev < 22))
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.85, blurSize: 3);
            }

            return enhanced;
        }



        public static Mat ApplyPlateEnhancementPipelineForEnhancementPlateImagev0(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🔍 Sadece çok kontrastsız veya karanlık durumlarda destekle
            if (contrast < 40 || stdDev < 15 || meanBrightness < 70)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEvEnhancedPlateImage(enhanced);
            }

            // ✨ Çok düşük varyasyon varsa (örneğin yumuşak kenarlar), netleştir
            if ((stdDev < 17 && meanBrightness > 90 && meanBrightness < 160))
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.6, blurSize: 3);
            }

            return enhanced;
        }

     
     

        public static Mat ApplyPlateEnhancementPipelineForEnhancementPlateImage(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🚫 Aşırı işlenmiş görüntü — normalize et (yumuşaklaştırarak sadeleştir)
            if (contrast >= 250 && stdDev > 40 && meanBrightness > 100)
            {
                //    // Hafif Gauss bulanıklığı uygulayarak MSER'e sade bir görüntü sun
                //    //enhanced = ImageEnhancementHelper.ApplyLightGaussianBlur(enhanced, kernelSize: 3);

                //    // Alternatif: Gamma düzeltmesiyle doygunluk yumuşatma
                //    //enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 1);

                enhanced = ImageEnhancementHelper.NormalizeContrastBGR(enhanced, 110);
                //    //return enhanced;

                //    //enhanced = PlateEnhancementHelper.ApplySmartCLAHEvEnhancedPlateImage(enhanced);
            }

            // 🔍 Sadece çok kontrastsız veya karanlık durumlarda CLAHE uygula
            if (contrast < 40 || stdDev < 15 || meanBrightness < 70)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEvEnhancedPlateImage(enhanced);
            }

            // ✨ Düşük varyasyonlu ama parlaklık dengeli plakalar için Unsharp uygula
            if ((stdDev < 17 && meanBrightness > 90 && meanBrightness < 160))
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.6, blurSize: 3);
            }

            return enhanced;
        }


        public static Mat ApplyPlateEnhancementPipelineForEnhancementPlateImageForBetaTest(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🚫 Aşırı işlenmiş ve doygun plaka — normalize et (netliği yumuşat)
            if (contrast >= 250 && stdDev > 40 && meanBrightness > 100)
            {
                enhanced = ImageEnhancementHelper.NormalizeContrastBGR(enhanced, targetContrast: 110);
            }

            // 🌑 Çok karanlık veya kontrastsızsa → CLAHE + gamma
            if (contrast < 40 || stdDev < 15 || meanBrightness < 70)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEvEnhancedPlateImage(enhanced);
                if (meanBrightness < 50)
                {
                    enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.7);
                }
            }

            // 🔆 Çok parlak işlenmiş plaka → gamma + beta
            if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaAndBrightness(enhanced, 0.5, -30);
            }

            // 🌓 Orta-üst parlaklık ama varyasyon zayıf → gamma + hafif beta
            if (meanBrightness > 160 && meanBrightness <= 200 && stdDev < 20)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaAndBrightness(enhanced, 0.7, -15);
            }

            // ✨ Detay az ama kurtarılabilir plaka → Unsharp
            if ((stdDev < 17 && meanBrightness > 90 && meanBrightness < 160) ||
                (contrast < 55 && stdDev < 20))
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.6, blurSize: 3);
            }

            return enhanced;
        }



        public static Mat ApplyPlateEnhancementPipelineForEnhancementPlateImage11(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🕶️ Çok karanlık veya düşük kontrastlıysa CLAHE
            if (contrast < 40 || stdDev < 15 || meanBrightness < 70)
            {
                enhanced = PlateEnhancementHelper.ApplySmartCLAHEvEnhancedPlateImage(enhanced);
            }

            // 🌞 Aşırı parlak ve düşük varyasyonluysa gamma düzeltme
            if (meanBrightness > 200 && stdDev < 10)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma: 0.5);
            }

            // ⚡ Çok düşük varyasyon ama orta parlaklıkta netleştirme
            if (stdDev < 17 && meanBrightness > 90 && meanBrightness < 160)
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.6, blurSize: 3);
            }

            // 🧊 Çok karanlık ve kontrastsız (kar gibi gri) durumlar için histogram stretching
            if (meanBrightness < 50 && stdDev < 10)
            {
                enhanced = ImageEnhancementHelper.ApplyHistogramStretching(enhanced);
            }

            return enhanced;
        }

        public static Mat ApplyPlateEnhancementPipeline(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🌑 Çok karanlık plaka: Grayscale CLAHE + renklendirme
            if (meanBrightness < 70)
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMergeV2(enhanced);
            }
            // ☁️ Düşük kontrast ya da düşük varyasyon: Renkli CLAHE
            else if (contrast < 50 || stdDev < 20)
            {
                enhanced = ApplySmartCLAHEv2(enhanced);
            }
            // 🌋 Aşırı kontrastlı plaka: Gamma düzeltme ile yumuşatma
            else if (contrast > 180)
            {
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }
            // 🔆 Çok parlak plaka: Sabit gamma bastırma
            else if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.4);
            }
            // 🌤 Orta parlaklıkta: Yumuşak adaptif gamma
            else if (meanBrightness >= 70 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }

            // ✨ Detay artırımı: Netlik
            if (contrast < 55 && meanBrightness > 90 && meanBrightness < 170)
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.85, 3);
            }

            // ✨ Detay artırımı → Unsharp mask (kenarlardaki karakterleri iyileştirebilir)
            //if (contrast < 55 && meanBrightness > 85 && meanBrightness < 170)
            //{
            //enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.95, blurSize: 3);
            //}
            //// 🔧 Alternatif olarak hafif netlik desteği (çok hafif gölgelenenler için)
            //else if (contrast >= 55 && contrast < 65 && meanBrightness >= 90 && meanBrightness <= 150)
            //{
            //    enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.95, blurSize: 3);
            //}

            return enhanced;
        }

        public static Mat ApplySmartCLAHEToGrayscaleAndMergev1(Mat frame)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);

            Mat[] labChannels;
            Cv2.Split(lab, out labChannels);

            // 🔍 Parlaklık ortalamasına göre parametre belirle
            double meanBrightness = Cv2.Mean(labChannels[0]).Val0;

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 220)
            {
                clipLimit = 5.0;
                tileGridSize = new OpenCvSharp.Size(2, 2); // Çok parlak → lokal vurgulama
            }
            else if (meanBrightness < 80)
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

            // 📦 CLAHE uygula
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(labChannels[0], labChannels[0]);

            // 🔄 Renkli hale dönüştür
            Cv2.Merge(labChannels, lab);
            Mat result = new Mat();
            Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

            return result;
        }

        public static Mat ApplySmartCLAHEToGrayscaleAndMergeV2(Mat frame)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);

            Mat[] labChannels;
            Cv2.Split(lab, out labChannels);

            double meanBrightness = Cv2.Mean(labChannels[0]).Val0;

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 230) // çok parlak
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness > 200)
            {
                clipLimit = 3.2;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 170)
            {
                clipLimit = 2.4;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 130)
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(10, 10);
            }
            else if (meanBrightness > 120)
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 100)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 85)
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(5, 5);
            }
            else if (meanBrightness > 70)
            {
                clipLimit = 3.8;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else // çok karanlık
            {
                clipLimit = 4.5;
                tileGridSize = new OpenCvSharp.Size(3, 3);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(labChannels[0], labChannels[0]);

            Cv2.Merge(labChannels, lab);
            Mat result = new Mat();
            Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

            return result;
        }

        public static Mat ApplySmartCLAHEToGrayscaleAndMergeVOriginalPlateImage(Mat frame)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);

            Mat[] labChannels;
            Cv2.Split(lab, out labChannels);

            double meanBrightness = Cv2.Mean(labChannels[0]).Val0;

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 230)
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 200)
            {
                clipLimit = 2.8;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 170)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 130)
            {
                clipLimit = 2.2;
                tileGridSize = new OpenCvSharp.Size(10, 10);
            }
            else if (meanBrightness > 100)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 80)
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 60)
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else
            {
                clipLimit = 4.2;
                tileGridSize = new OpenCvSharp.Size(3, 3);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(labChannels[0], labChannels[0]);

            Cv2.Merge(labChannels, lab);
            Mat result = new Mat();
            Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

            return result;
        }

        public static Mat ApplySmartCLAHEv1(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 220) // Aşırı parlak
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(4, 4); // Daha dengeli lokal alan
            }
            else if (meanBrightness > 180)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness < 80) // Karanlık
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness < 120)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }

        public static Mat ApplySmartCLAHEv2(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(luminance); // varyasyon durumu


            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 230) // çok parlak
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness > 200) // parlak
            {
                clipLimit = 3.2;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 170) // orta-üst
            {
                clipLimit = 2.4;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 130) // orta
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(10, 10);
            }
            else if (meanBrightness > 120) // yeterli parlaklık ama lokal kontrast artırımı iyi olur
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 100) // hafif karanlık
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 85) // belirgin karanlık, güçlü lokal iyileştirme gerekir
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(5, 5);
            }
            else if (meanBrightness > 70) // çok karanlık sınırında
            {
                clipLimit = 3.8;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else // çok karanlık
            {
                clipLimit = 4.5;
                tileGridSize = new OpenCvSharp.Size(3, 3);
            }

            // ⚠️ Gürültü düşükse daha yumuşak CLAHE uygula
            //if (stdDev < 18)
            //{
            //    clipLimit = Math.Min(clipLimit, 2.5);
            //}

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }

        public static Mat ApplySmartCLAHEv3(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(luminance); // varyasyon durumu

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            // 🌕 Aşırı parlak
            if (meanBrightness > 230)
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            // 🌤 Parlak
            else if (meanBrightness > 200)
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            // 🔆 Hafif parlak
            else if (meanBrightness > 170)
            {
                clipLimit = 2.2;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            // ☁️ Dengeli
            else if (meanBrightness > 140)
            {
                clipLimit = 1.8;
                tileGridSize = new OpenCvSharp.Size(10, 10);
            }
            // 🌗 Orta-alt
            else if (meanBrightness > 110)
            {
                clipLimit = 2.2;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            // 🌑 Karanlık
            else if (meanBrightness > 85)
            {
                clipLimit = 3;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            // ⚫ Çok karanlık
            else
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }

            // ⚠️ Gürültü düşükse daha yumuşak CLAHE uygula
            if (stdDev < 18)
            {
                clipLimit = Math.Min(clipLimit, 2.5);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }

        public static Mat ApplySmartCLAHEv4(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(luminance); // varyasyon durumu


            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 230) // çok parlak
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness > 200) // parlak
            {
                clipLimit = 3.2;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 170) // orta-üst
            {
                clipLimit = 2.4;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 130) // orta
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(10, 10);
            }
            else if (meanBrightness > 120) // yeterli parlaklık ama lokal kontrast artırımı iyi olur
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            //else if (meanBrightness > 100) // hafif karanlık
            //{
            //    clipLimit = 2.5;
            //    tileGridSize = new OpenCvSharp.Size(6, 6);
            //}
            else if (meanBrightness > 85) // belirgin karanlık, güçlü lokal iyileştirme gerekir
            {
                //clipLimit = 3.0;
                //tileGridSize = new OpenCvSharp.Size(5, 5);,

                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 70) // çok karanlık sınırında
            {
                clipLimit = 3.8;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else // çok karanlık
            {
                clipLimit = 4.5;
                tileGridSize = new OpenCvSharp.Size(3, 3);
            }

            // ⚠️ Gürültü düşükse daha yumuşak CLAHE uygula
            //if (stdDev < 18)
            //{
            //    clipLimit = Math.Min(clipLimit, 2.5);
            //}

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }
        public static Mat ApplySmartCLAHEvOriginalPlateImageVOld(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(luminance);

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 230)
            {
                clipLimit = 4.0;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness > 200)
            {
                if(stdDev < 18)
                {
                    clipLimit = 3.6;
                    tileGridSize = new OpenCvSharp.Size(5, 5);
                }
                else
                {
                    clipLimit = 3.2;
                    tileGridSize = new OpenCvSharp.Size(6, 6);
                }
            }
            else if (meanBrightness > 170)
            {
                if (stdDev < 18)
                {
                    clipLimit = 3.0;
                    tileGridSize = new OpenCvSharp.Size(6, 6);
                }
                else
                {
                    clipLimit = 2.4;
                    tileGridSize = new OpenCvSharp.Size(8, 8);
                }
            }
            else if (meanBrightness > 130)
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(10, 10);
            }
            else if (meanBrightness > 120)
            {
                clipLimit = 2.0;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 100)
            {
                //if (stdDev < 14)
                //{
                //    clipLimit = 3.2;
                //    tileGridSize = new OpenCvSharp.Size(5, 5);
                //}
                //else
                //{
                //    clipLimit = 2.5;
                //    tileGridSize = new OpenCvSharp.Size(8, 8);
                //}

                //if (stdDev < 14.5)
                //{
                //    clipLimit = 3.8; // Daha düşük clipLimit
                //    tileGridSize = new OpenCvSharp.Size(2,2);
                //}
                 if (stdDev < 15)
                {
                    clipLimit = 3.6; // Daha düşük clipLimit
                    tileGridSize = new OpenCvSharp.Size(3,3);
                }
                else if (stdDev < 16)
                {
                    clipLimit = 3.6;
                    tileGridSize = new OpenCvSharp.Size(4, 4);
                }
                else if (stdDev < 17)
                {
                    clipLimit = 3.3;
                    tileGridSize = new OpenCvSharp.Size(5, 5);
                }
                else if (stdDev < 18)
                {
                    clipLimit = 3.0;
                    tileGridSize = new OpenCvSharp.Size(5, 5);
                }
                else if (stdDev < 19)
                {
                    clipLimit = 2.7;
                    tileGridSize = new OpenCvSharp.Size(6, 6);
                }
                else if (stdDev < 20)
                {
                    clipLimit = 2.4;
                    tileGridSize = new OpenCvSharp.Size(6, 6);
                }
                else
                {
                    clipLimit = 2.0;
                    tileGridSize = new OpenCvSharp.Size(8, 8);
                }
            }
            else if (meanBrightness > 85)
            {
                if (stdDev < 14)
                {
                    //clipLimit = 3; // En agresif
                    //tileGridSize = new OpenCvSharp.Size(5,5);

                    clipLimit = 3; // En agresif
                    tileGridSize = new OpenCvSharp.Size(2, 2);
                }
                else if (stdDev < 15)
                {
                    //clipLimit = 4.5; // En agresif
                    //tileGridSize = new OpenCvSharp.Size(3, 3);

                    clipLimit = 4.5; // En agresif
                    tileGridSize = new OpenCvSharp.Size(2, 2);
                }
                else if (stdDev < 16)
                {
                    //clipLimit = 4.4;
                    //tileGridSize = new OpenCvSharp.Size(3, 3);

                    //clipLimit = 4.2; // 4.4 yerine biraz yumuşak ama hala agresif
                    //tileGridSize = new OpenCvSharp.Size(3, 3);

                    clipLimit = 3; // 4.4 yerine biraz yumuşak ama hala agresif
                    tileGridSize = new OpenCvSharp.Size(5, 5);
                }
                else if (stdDev < 17)
                {
                    //clipLimit = 4.4;
                    //tileGridSize = new OpenCvSharp.Size(2,2);

                    clipLimit = 3.5; // 4.4 çok agresif olabilir
                    tileGridSize = new OpenCvSharp.Size(2, 2);
                }
                else if (stdDev < 18)
                {
                    clipLimit = 4.0;
                    tileGridSize = new OpenCvSharp.Size(2, 2);
                }
                else if (stdDev < 19)
                {
                    clipLimit = 3.8;
                    tileGridSize = new OpenCvSharp.Size(2, 2);
                }
                else if (stdDev < 20)
                {
                    //clipLimit = 3.5;
                    //tileGridSize = new OpenCvSharp.Size(6, 6);

                    clipLimit = 3.6;
                    tileGridSize = new OpenCvSharp.Size(2, 2);
                }
                else
                {
                    clipLimit = 2.2;
                    tileGridSize = new OpenCvSharp.Size(7, 7);
                }


                //if (stdDev < 14)
                //{
                //    clipLimit = 3;
                //    tileGridSize = new OpenCvSharp.Size(2, 2);
                //}
                //else if (stdDev < 15)
                //{
                //    clipLimit = 3.2;
                //    tileGridSize = new OpenCvSharp.Size(2, 2);
                //}
                //else if (stdDev < 16)
                //{
                //    clipLimit = 3.4;
                //    tileGridSize = new OpenCvSharp.Size(3, 3);
                //}
                //else if (stdDev < 17)
                //{
                //    clipLimit = 3.6;
                //    tileGridSize = new OpenCvSharp.Size(2, 2);
                //}
                //else if (stdDev < 18)
                //{
                //    clipLimit = 3.8;
                //    tileGridSize = new OpenCvSharp.Size(4, 4);
                //}
                //else if (stdDev < 19)
                //{
                //    clipLimit = 3.6;
                //    tileGridSize = new OpenCvSharp.Size(5, 5);
                //}
                //else if (stdDev < 20)
                //{
                //    clipLimit = 3;
                //    tileGridSize = new OpenCvSharp.Size(6, 6);
                //}
                //else if (stdDev < 21)
                //{
                //    clipLimit = 3.2;
                //    tileGridSize = new OpenCvSharp.Size(6, 6);
                //}
                //else if (stdDev < 22)
                //{
                //    clipLimit = 3.0;
                //    tileGridSize = new OpenCvSharp.Size(7, 7);
                //}
                //else
                //{
                //    clipLimit = 2.2;
                //    tileGridSize = new OpenCvSharp.Size(8, 8);
                //}
            }
            else if (meanBrightness > 70)
            {
                if (stdDev < 18)
                {
                    clipLimit = 4.2;
                    tileGridSize = new OpenCvSharp.Size(3, 3);
                }
                else
                {
                    clipLimit = 3.8;
                    tileGridSize = new OpenCvSharp.Size(4, 4);
                }
            }
            else
            {
                clipLimit = 4.5;
                tileGridSize = new OpenCvSharp.Size(3, 3);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }


        public static Mat ApplySmartCLAHEvOriginalPlateImage(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(luminance);

            double clipLimit;
            OpenCvSharp.Size tileGridSize;

            if (meanBrightness > 230)
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (meanBrightness > 200)
            {
                clipLimit = stdDev < 18 ? 3.2 : 2.8;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 170)
            {
                clipLimit = stdDev < 18 ? 2.8 : 2.4;
                tileGridSize = new OpenCvSharp.Size(7, 7);
            }
            else if (meanBrightness > 130)
            {
                clipLimit = 2.2;
                tileGridSize = new OpenCvSharp.Size(8, 8);
            }
            else if (meanBrightness > 110)
            {
                clipLimit = stdDev < 16 ? 3.3 : 2.6;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            else if (meanBrightness > 90)
            {
                if (stdDev < 14)
                {
                    clipLimit = 4.2;
                    tileGridSize = new OpenCvSharp.Size(3, 3);
                }
                else if (stdDev < 16)
                {
                    clipLimit = 3.8;
                    tileGridSize = new OpenCvSharp.Size(4, 4);
                }
                else if (stdDev < 18)
                {
                    clipLimit = 3.2;
                    tileGridSize = new OpenCvSharp.Size(5, 5);
                }
                else
                {
                    clipLimit = 2.5;
                    tileGridSize = new OpenCvSharp.Size(6, 6);
                }
            }
            else if (meanBrightness > 70)
            {
                clipLimit = stdDev < 18 ? 4.2 : 3.6;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else
            {
                clipLimit = 4.5;
                tileGridSize = new OpenCvSharp.Size(3, 3);
            }

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }

        public static Mat ApplySmartCLAHEvEnhancedPlateImage(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb);

            Mat[] channels = Cv2.Split(ycrcb);
            Mat luminance = channels[0];

            double meanBrightness = Cv2.Mean(luminance).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(luminance);

            double clipLimit = 2.0;
            OpenCvSharp.Size tileGridSize = new OpenCvSharp.Size(8, 8);

            // Sadece belirli zayıf varyasyon durumlarında müdahale et
            if (stdDev < 14)
            {
                clipLimit = 3.5;
                tileGridSize = new OpenCvSharp.Size(4, 4);
            }
            else if (stdDev < 16)
            {
                clipLimit = 3.0;
                tileGridSize = new OpenCvSharp.Size(5, 5);
            }
            else if (stdDev < 18)
            {
                clipLimit = 2.5;
                tileGridSize = new OpenCvSharp.Size(6, 6);
            }
            // stdDev > 18 için zaten yeterince iyi → default parametre kalır

            // CLAHE uygula
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            clahe.Apply(luminance, luminance);

            Cv2.Merge(channels, ycrcb);
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

            return enhanced;
        }

        public static Mat ApplyPlateSpecificEnhancementvGereksizseYapma(Mat plateROI)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

            double meanBrightness = Cv2.Mean(gray).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);

            Mat enhanced = plateROI.Clone();

            // 🚫 Eğer her şey yolundaysa hiçbir şey yapma!
            //if (meanBrightness >= 90 && meanBrightness <= 180 && contrast > 55)
            //{
            //    return enhanced;  // Görüntü zaten düzgün
            //}

            // 🌑 Çok karanlık: Daha agresif CLAHE
            if (meanBrightness < 70)
            {
                enhanced = ApplySmartCLAHEToGrayscaleAndMergeV2(enhanced);
            }
            // ☁️ Düşük kontrast veya varyasyon: CLAHE
            else if (contrast < 50 || stdDev < 20)
            {
                enhanced = ApplySmartCLAHEv2(enhanced);
            }
            // 🌋 Aşırı kontrastlı plaka: Gamma ile dengele
            else if (contrast > 170)
            {
                double gamma = ImageEnhancementHelper.GetGammaForHighContrast(contrast); // örn: 0.85 gibi yumuşak değer
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }
            // 🔆 Çok parlak: Sabit gamma bastırma
            else if (meanBrightness > 200)
            {
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, 0.4);
            }
            // 🌤 Orta parlaklık: adaptif gamma ile dengele
            else if (meanBrightness >= 70 && meanBrightness <= 150)
            {
                double gamma = ImageEnhancementHelper.GetAdaptiveGamma(meanBrightness);
                enhanced = ImageEnhancementHelper.ApplyGammaCorrection(enhanced, gamma);
            }

            //✨ Karakter ayrımı için ekstra netlik(ama sadece kararsız sahnelerde)
            if (contrast < 55 && meanBrightness > 85 && meanBrightness < 170)
            {
                enhanced = ImageEnhancementHelper.ApplyUnsharpMask(enhanced, strength: 0.9);
            }

            return enhanced;
        }

        //public static Mat ApplyPlateSpecificEnhancementv1(Mat plateROI)
        //{
        //    Mat gray = new Mat();
        //    Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

        //    double meanBrightness = Cv2.Mean(gray).Val0;
        //    double contrast = ComputeImageContrast(gray);
        //    double stdDev = ComputeImageStdDev(gray);

        //    Mat enhanced = plateROI.Clone();

        //    // 🌑 Çok karanlık: Daha agresif CLAHE
        //    if (meanBrightness < 70)
        //    {
        //        enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
        //    }
        //    // ☁️ Düşük kontrast veya varyasyon: CLAHE
        //    else if (contrast < 60 || stdDev < 25)
        //    {
        //        enhanced = ApplySmartCLAHE(enhanced);
        //    }
        //    // 🌋 Aşırı kontrastlı plaka: Gamma ile dengele
        //    else if (contrast > 160)
        //    {
        //        double gamma = GetGammaForHighContrast(contrast); // örn: 0.85 gibi yumuşak değer
        //        enhanced = ApplyGammaCorrection(enhanced, gamma);
        //    }
        //    // 🔆 Çok parlak: Sabit gamma bastırma
        //    else if (meanBrightness > 200)
        //    {
        //        enhanced = ApplyGammaCorrection(enhanced, 0.4);
        //    }
        //    // 🌤 Orta parlaklık: adaptif gamma ile dengele
        //    else if (meanBrightness >= 70 && meanBrightness <= 150)
        //    {
        //        double gamma = GetAdaptiveGamma(meanBrightness);
        //        enhanced = ApplyGammaCorrection(enhanced, gamma);
        //    }

        //    // ✨ Karakter ayrımı için ekstra netlik
        //    if (contrast < 60 && meanBrightness > 85 && meanBrightness < 160)
        //    {
        //        enhanced = ApplyUnsharpMask(enhanced, strength: 0.9);
        //    }

        //    return enhanced;
        //}

        //public static Mat ApplyPlateSpecificEnhancementv2(Mat plateROI)
        //{
        //    Mat gray = new Mat();
        //    Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

        //    double meanBrightness = Cv2.Mean(gray).Val0;
        //    double contrast = ComputeImageContrast(gray);
        //    double stdDev = ComputeImageStdDev(gray);

        //    Mat enhanced = plateROI.Clone();

        //    // 🌑 Çok karanlık: CLAHE (Grayscale) + Renkli Birleştirme
        //    if (meanBrightness < 70)
        //    {
        //        enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
        //    }
        //    // ☁️ Düşük kontrast: Renkli CLAHE
        //    else if (contrast < 50 || stdDev < 20)
        //    {
        //        enhanced = ApplySmartCLAHE(enhanced);
        //    }
        //    // 🌋 Aşırı kontrastlı plaka: Gamma ile dengele
        //    else if (contrast > 180)
        //    {
        //        double gamma = GetGammaForHighContrast(contrast);
        //        enhanced = ApplyGammaCorrection(enhanced, gamma);
        //    }
        //    // 🔆 Aşırı parlak: Sert gamma düzeltmesi
        //    else if (meanBrightness > 200)
        //    {
        //        enhanced = ApplyGammaCorrection(enhanced, 0.4);
        //    }
        //    // 🌤 Orta parlaklık: Yumuşak adaptif gamma
        //    else if (meanBrightness >= 70 && meanBrightness <= 150)
        //    {
        //        double gamma = GetAdaptiveGamma(meanBrightness);
        //        enhanced = ApplyGammaCorrection(enhanced, gamma);
        //    }

        //    // ✨ Netlik artırımı (görsel derinlik kazandırmak için)
        //    if (contrast < 55 && meanBrightness > 90 && meanBrightness < 170)
        //    {
        //        enhanced = ApplyUnsharpMask(enhanced, strength: 0.85);
        //    }

        //    return enhanced;
        //}

        //public static Mat ApplyPlateSpecificEnhancement1(Mat plateROI)
        //{
        //    Mat gray = new Mat();
        //    Cv2.CvtColor(plateROI, gray, ColorConversionCodes.BGR2GRAY);

        //    double meanBrightness = Cv2.Mean(gray).Val0;
        //    double contrast = ComputeImageContrast(gray);
        //    double stdDev = ComputeImageStdDev(gray);

        //    Mat enhanced = plateROI.Clone();

        //    // 🔲 Aşırı parlak ve düşük kontrast plakalara özel strateji
        //    if (meanBrightness > 210 && contrast < 60)
        //    {
        //        // 1. Adım: Gamma düzeltmesiyle parlaklığı bastır
        //        enhanced = ApplyGammaCorrection(enhanced, 0.4);

        //        // 2. Adım: CLAHE ile kontrastı yükselt
        //        enhanced = ApplySmartCLAHE(enhanced);

        //        // 3. Adım: Gerekirse hafif netleştirme
        //        enhanced = ApplyUnsharpMask(enhanced, strength: 0.8);
        //    }
        //    // 🌑 Çok karanlık plaka: Daha agresif CLAHE
        //    else if (meanBrightness < 70)
        //    {
        //        enhanced = ApplySmartCLAHEToGrayscaleAndMerge(enhanced);
        //    }
        //    // ☁️ Düşük kontrast: CLAHE
        //    else if (contrast < 60 || stdDev < 25)
        //    {
        //        enhanced = ApplySmartCLAHE(enhanced);
        //    }
        //    // 🌋 Aşırı kontrast: Yumuşatma
        //    else if (contrast > 160)
        //    {
        //        double gamma = GetGammaForHighContrast(contrast);
        //        enhanced = ApplyGammaCorrection(enhanced, gamma);
        //    }
        //    // 🔆 Parlak: Sabit gamma
        //    else if (meanBrightness > 200)
        //    {
        //        enhanced = ApplyGammaCorrection(enhanced, 0.4);
        //    }
        //    // 🌤 Orta parlaklık: adaptif gamma
        //    else if (meanBrightness >= 70 && meanBrightness <= 150)
        //    {
        //        double gamma = GetAdaptiveGamma(meanBrightness);
        //        enhanced = ApplyGammaCorrection(enhanced, gamma);
        //    }

        //    // ✨ Ekstra netlik sadece belli koşullarda
        //    if (contrast < 60 && meanBrightness > 85 && meanBrightness < 160)
        //    {
        //        enhanced = ApplyUnsharpMask(enhanced, strength: 0.9);
        //    }

        //    return enhanced;
        //}

        public static bool IsPlateAlreadyEnhancedOld(Mat plateBGR)
        {
            if (plateBGR.Channels() != 3)
                throw new ArgumentException("Görüntü renkli (BGR) olmalıdır.");

            Mat gray = new Mat();
            Cv2.CvtColor(plateBGR, gray, ColorConversionCodes.BGR2GRAY);

            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);
            double mean = Cv2.Mean(gray).Val0;

            // 🌟 Tipik "enhanced" profil → yüksek detay, denge, iyi ton aralığı
            bool highContrastAndDetail = contrast > 160 && stdDev > 25;
            bool goodMeanRange = mean > 85 && mean < 190;

            if (highContrastAndDetail && goodMeanRange)
                return true;

            // ⚠️ Özel durum: çok parlak ama detaylı (örneğin gamma+CLAHE sonrası)
            if (mean > 190 && stdDev > 23 && contrast > 130)
                return true;

            // ⚠️ Özel durum: çok dengeli ama detay zayıf (yeniden işlenebilir)
            if (contrast < 60 || stdDev < 18 || mean < 70 || mean > 220)
                return false;

            // ❓ Orta seviye: gri alan. Uygulama kararına göre bu kısmı dinamik bırakabiliriz.
            return false;
        }
        public static bool IsPlateAlreadyEnhancedv0(Mat plateBGR)
        {
            if (plateBGR.Channels() != 3)
                throw new ArgumentException("Görüntü renkli (BGR) olmalıdır.");

            // Griye çevir
            Mat gray = new Mat();
            Cv2.CvtColor(plateBGR, gray, ColorConversionCodes.BGR2GRAY);

            // Görüntü istatistiklerini al
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);
            double mean = Cv2.Mean(gray).Val0;

            // Aşırı net, doygun, dengeli plakalar zaten işlenmiş olabilir
            if (contrast > 160 && stdDev > 25 && mean >= 90 && mean <= 180)
                return true;

            // Düşük kontrast veya varyasyon varsa işlenmemiş olabilir
            if (contrast < 60 || stdDev < 20)
                return false;

            // Ortalamada kalıyorsa belirsiz, ama işlenmemiş saymak daha güvenli
            return false;
        }
        public static bool IsPlateAlreadyEnhancedv1(Mat plateBGR)
        {
            if (plateBGR.Channels() != 3)
                throw new ArgumentException("Görüntü renkli (BGR) olmalıdır.");

            Mat gray = new Mat();
            Cv2.CvtColor(plateBGR, gray, ColorConversionCodes.BGR2GRAY);

            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);
            double mean = Cv2.Mean(gray).Val0;

            // 🚫 Aşırı işlenmiş/doygun plaka → işlenmiş kabul et
            if (contrast >= 240 && stdDev >= 40 && mean >= 100 && mean <= 200)
                return true;

            // ✅ Dengeli ama güçlü varyasyonlu plaka → işlenmiş olabilir
            if (contrast > 160 && stdDev > 28 && mean >= 90 && mean <= 180)
                return true;

            // 🌑 Çok karanlık, düşük varyasyon → kesin işlenmemiş
            if (mean < 65 && stdDev < 20)
                return false;

            // ⚠️ Belirsiz ama çok yüksek kontrast yoksa → işlenmemiş say
            if (contrast < 100 || stdDev < 22)
                return false;

            // 🔄 Son kararsızlık: belirsizse yine işlenmemiş say
            return false;
        }



        public static bool IsPlateAlreadyEnhanced(Mat plateBGR)
        {
            if (plateBGR.Channels() != 3)
                throw new ArgumentException("Görüntü renkli (BGR) olmalıdır.");

            // Griye çevir
            Mat gray = new Mat();
            Cv2.CvtColor(plateBGR, gray, ColorConversionCodes.BGR2GRAY);

            // Görüntü istatistiklerini al
            double contrast = ImageEnhancementHelper.ComputeImageContrast(gray);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(gray);
            double mean = Cv2.Mean(gray).Val0;


            // 🔴 255 kontrast → büyük ihtimalle aşırı işlenmiş görüntü (binary değilse)
            if (contrast == 255 && stdDev > 40 && mean > 100)
                return true;

            // 🌟 Net, doygun, orta parlaklıktaki görüntüler işlenmiş sayılabilir
            if (contrast > 140 && stdDev > 22 && mean >= 75 && mean <= 200)
                return true;

            // 🌞 Çok parlak ama doygun net görüntüler (muhtemelen gamma uygulanmış)
            if (mean > 200 && stdDev > 40 && contrast > 100)
                return true;

            // 🌫️ Düşük kontrast veya varyasyon varsa işlenmemiş olabilir
            if (contrast < 60 || stdDev < 20)
                return false;

            // 🤔 Diğer tüm durumlar belirsiz – işlenmemiş gibi davranmak daha güvenli
            return false;
        }

        public static Mat CheckPlateStatus(Mat plateBGR)
        {
            Mat enhanced;
            string osman;



            if (IsPlateAlreadyEnhancedv0(plateBGR))
            {
                enhanced = ApplyPlateEnhancementPipelineForEnhancementPlateImage(plateBGR);
                osman = "Enhanced Plate Image";
            }
            else
            {
                enhanced = ApplyPlateEnhancementPipelineForOriginalPlateImage(plateBGR);
                osman = "Original Plate Image";
            }

            ////Debug.WriteLine("-------------");
            ////Debug.WriteLine(osman);
            ////Debug.WriteLine("-------------");

            return enhanced;

        }

        public static Mat CheckPlateStatusForBetaTest(Mat plateBGR)
        {
            Mat enhanced;
            string osman;



            if (IsPlateAlreadyEnhancedv1(plateBGR))
            {
                enhanced = ApplyPlateEnhancementPipelineForEnhancementPlateImageForBetaTest(plateBGR);
                osman = "Enhanced Plate Image";
            }
            else
            {
                enhanced = ApplyPlateEnhancementPipelineForOriginalPlateImageForBetaTest(plateBGR);
                osman = "Original Plate Image";
            }

            ////Debug.WriteLine("-------------");
            ////Debug.WriteLine(osman);
            ////Debug.WriteLine("-------------");

            return enhanced;

        }
    }
}
