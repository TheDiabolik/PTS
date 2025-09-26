using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    //static olarak kalacak
    internal class ImageEnhancementHelper
    {



        public static Mat ApplyLightGaussianBlur(Mat src, int kernelSize = 3)
        {
            Mat result = new Mat();
            Cv2.GaussianBlur(src, result, new OpenCvSharp.Size(kernelSize, kernelSize), 0);
            return result;
        }
        public static Mat ApplyHistogramStretching(Mat input)
        {
            Mat stretched = new Mat();

            if (input.Channels() == 1)
            {
                // Gri görüntü için doğrudan normalize et
                Cv2.Normalize(input, stretched, 0, 255, NormTypes.MinMax);
            }
            else
            {
                // Renkli görüntüyse, her kanalı ayrı ayrı normalize et
                Mat[] channels = Cv2.Split(input);
                for (int i = 0; i < channels.Length; i++)
                {
                    Cv2.Normalize(channels[i], channels[i], 0, 255, NormTypes.MinMax);
                }
                Cv2.Merge(channels, stretched);
            }

            return stretched;
        }
        public static double ComputeWhitePixelRatioAhmet(Mat grayImage, double threshold = 200)
        {
            using(Mat whiteMask = new Mat())
            {
                Cv2.Compare(grayImage, new Scalar(threshold), whiteMask, CmpType.GT);
                double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);

                return whiteRatio;
            }
        }

        public static double ComputeWhitePixelRatio(Mat grayImage, byte threshold = 200)
        {
            if (grayImage.Channels() != 1)
                throw new ArgumentException("Giriş görüntüsü gri tonlamalı (tek kanal) olmalıdır.");

            using var binary = new Mat();
            Cv2.Threshold(grayImage, binary, threshold, 255, ThresholdTypes.Binary);
            int whitePixels = Cv2.CountNonZero(binary);
            int totalPixels = grayImage.Rows * grayImage.Cols;

            return totalPixels == 0 ? 0 : (double)whitePixels / totalPixels;
        }

        public static double ComputeLaplacianBlurScore(Mat grayImage)
        {
            if (grayImage.Channels() != 1)
                throw new ArgumentException("Giriş görüntüsü gri (tek kanallı) olmalıdır.");

            Mat laplacian = new Mat();
            Cv2.Laplacian(grayImage, laplacian, MatType.CV_64F); // float64 hassasiyetle

            // Varyansı hesapla
            Mat mean = new Mat(), stdDev = new Mat();
            Cv2.MeanStdDev(laplacian, mean, stdDev);

            double blurScore = Math.Pow(stdDev.At<double>(0, 0), 2);
            return blurScore;
        }


        public static Mat ComputeSobelEdges(Mat grayImage, double alpha = 1, double beta = 1, double gamma = 0)
        {
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Mat sobelCombined = new Mat();
         
            // **Sobel X ve Sobel Y hesapla**
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_8U, 0, 1, 3);

            // **Sobel X ve Y birleştir (Ağırlıklı toplama)**
            Cv2.AddWeighted(sobelX, alpha, sobelY, beta, gamma, sobelCombined);

            //Cv2.Normalize(sobelCombined, sobelCombined, 0, 255, NormTypes.MinMax);

            return sobelCombined;
        }
        //harekete duyarlı için
        public static Mat ComputeSobelEdges(Mat grayImage, SobelDetectionSettings settings, double alpha = 1, double beta = 1, double gamma = 0)
        {
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Mat sobelCombined = new Mat();

            int ksize = settings.SobelKernelSize;

            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, ksize);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_8U, 0, 1, ksize);

            Cv2.AddWeighted(sobelX, alpha, sobelY, beta, gamma, sobelCombined);

            // Normalize: MSER daha stabil çalışsın
            //Cv2.Normalize(sobelCombined, sobelCombined, 0, 255, NormTypes.MinMax);

            return sobelCombined;
        }

        public static double ComputeSobelEdgeDensity(Mat grayImage)
        {
            var sobel = ComputeSobelEdges(grayImage);
            return (double)Cv2.CountNonZero(sobel) / (sobel.Rows * sobel.Cols);
        }

        public static (Mat sobelImage, double edgeDensity) ComputeSobelEdgesWithDensity(Mat grayImage)
        {
            var sobel = ComputeSobelEdges(grayImage);
            double density = (double)Cv2.CountNonZero(sobel) / (sobel.Rows * sobel.Cols);
            return (sobel, density);
        }


        public static Mat ComputeAdaptiveSobelEdges(Mat grayImage)
        {
            // 1️⃣ Ortalama parlaklık ve kontrastı hesapla
            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(grayImage);

            // 2️⃣ Sobel X ve Y kenarlarını hesapla
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            Cv2.Sobel(grayImage, sobelX, MatType.CV_8U, 1, 0, 3);
            Cv2.Sobel(grayImage, sobelY, MatType.CV_8U, 0, 1, 3);

            // 3️⃣ Ağırlıkları otomatik belirle
            double alpha = 1.0;  // sobelX (yatay kenar)
            double beta = 1.0;   // sobelY (dikey kenar)
            double gamma = 0.0;  // parlaklık ofseti

            if (meanBrightness > 200) // çok parlak sahne → karakterler silik olabilir
            {
                alpha = 1.0;
                beta = 0.8;      // X yönünü vurgula
                gamma = 5;
            }
            else if (meanBrightness < 80) // çok karanlık sahne → kenarlar az belirgin
            {
                alpha = 1.0;
                beta = 1.0;
                gamma = 15;
            }

            if (contrast < 30) // düşük kontrast sahnelerde hafiflet
            {
                alpha *= 0.8;
                beta *= 0.8;
                gamma += 10;
            }

            // 4️⃣ Sobel X + Y ağırlıklı birleşimi
            Mat sobelCombined = new Mat();
            Cv2.AddWeighted(sobelX, alpha, sobelY, beta, gamma, sobelCombined);

            // 5️⃣ (İsteğe bağlı) Gürültü azaltma için hafif morphology
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(sobelCombined, sobelCombined, MorphTypes.Close, kernel);

            return sobelCombined;
        }
        //Görüntüdeki maksimum ve minimum parlaklık farkını ölçer
        public static double ComputeImageContrast(Mat grayImage)
        {
            // Minimum ve maksimum piksel değerlerini bul
            double minVal, maxVal;
            Cv2.MinMaxLoc(grayImage, out minVal, out maxVal);

            // Kontrast = Maksimum - Minimum
            double contrast = maxVal - minVal;

            return contrast;
        }
        //Piksel yoğunluğu varyasyonunu, yani görsel karmaşıklığı ölçer
        public static double ComputeImageStdDev(Mat grayImage)
        {
            // Ortalama parlaklık ve standart sapmayı hesapla

            using(Mat mean = new Mat())
            using (Mat stdDev = new Mat())
            {
                Cv2.MeanStdDev(grayImage, mean, stdDev);

                return stdDev.At<double>(0, 0);  // Standart sapma değeri
            }
        }

        public static Mat ApplyGammaCorrectionLUT(Mat src, double gamma)
        {
            Mat lut = new Mat(1, 256, MatType.CV_8UC1);

            for (int i = 0; i < 256; i++)
            {
                byte value = (byte)(Math.Pow(i / 255.0, gamma) * 255.0);
                lut.Set(0, i, value);
            }

            Mat result = new Mat();
            Cv2.LUT(src, lut, result);
            return result;
        }

        // 🔹 **Gamma Düzeltmesi**
        public static Mat ApplyGammaCorrection(Mat inputGray, double gamma = 0.5)
        {
            Mat corrected = new Mat();
            inputGray.ConvertTo(corrected, MatType.CV_32F);
            Cv2.Pow(corrected / 255.0, gamma, corrected);
            corrected *= 255;
            corrected.ConvertTo(corrected, MatType.CV_8U);
            return corrected;
        }

        public static double GetAdaptiveGammaForBetaTest(double brightness)
        {
            if (brightness < 100) return 1.3;
            if (brightness < 120) return 1.1;
            return 0.9;
        }


        public static double GetAdaptiveBetaForBetaTest(double brightness)
        {
            if (brightness < 100) return 20;
            if (brightness < 130) return 10;
            return -10;
        }


        public static double GetAdaptiveGamma(double meanBrightness)
        {
            // 80 ile 150 aralığında normalize edip 1.0 ile 0.7 arasında bir gamma değeri üret
            if (meanBrightness < 80)
                return 1.0; // Değişiklik yapma

            if (meanBrightness > 150)
                return 0.7; // Maksimum düzeltme

            // 80–150 arası: çizgisel olarak azalt
            double normalized = (meanBrightness - 80) / 70.0; // 0 ile 1 arasında
            return 1.0 - normalized * 0.3; // 1.0 → 0.7 arası gamma
        }

        public static double GetAdaptiveGamma(double meanBrightness, bool isAlreadyEnhanced = false)
        {
            if (meanBrightness < 80)
                return 1.0;

            if (meanBrightness > 150)
                return isAlreadyEnhanced ? 0.8 : 0.7;

            double normalized = (meanBrightness - 80) / 70.0;
            return 1.0 - normalized * (isAlreadyEnhanced ? 0.2 : 0.3);
        }

        public static double GetGammaForHighContrast(double contrast)
        {
            // 180 altı için düzeltme yapma
            if (contrast <= 180)
                return 1.0;

            // 180–255 aralığını normalize et
            contrast = Math.Min(contrast, 255);
            double normalized = (contrast - 180) / (255 - 180); // 0 → 1
            return 1.0 - normalized * 0.4; // 1.0 → 0.6 arasında gamma uygula
        }

        public static double GetGammaForHighContrastv0(double contrast)
        {
            // Kontrast çok yüksekse (örn. 255) → düşük gamma uygula (örneğin 0.65)
            if (contrast >= 240) return 0.65;
            if (contrast >= 220) return 0.7;
            if (contrast >= 200) return 0.75;
            if (contrast >= 180) return 0.8;
            return 1.0; // Normal kontrastlar için gamma düzeltme gerekmez
        }

        //plaka sınıfnın içinde overload methodu yazılabilir uygun vakitte
        public static Mat ApplyUnsharpMask(Mat image, double strength = 1.0, int blurSize = 5)
        {
            Mat blurred = new Mat();
            Cv2.GaussianBlur(image, blurred, new OpenCvSharp.Size(blurSize, blurSize), 0);

            Mat sharpened = new Mat();
            Cv2.AddWeighted(image, 1 + strength, blurred, -strength, 0, sharpened);

            return sharpened;
        }





        public static Mat ApplyGammaAndBrightness(Mat input, double gamma = 1.0, double beta = 0.0)
        {
            // 1. Normalize ve gamma uygula
            Mat floatImage = new Mat();
            input.ConvertTo(floatImage, MatType.CV_32F, 1.0 / 255.0); // Normalize to [0,1]
            Cv2.Pow(floatImage, gamma, floatImage); // Gamma correction
            floatImage *= 255.0;

            // 2. Beta (parlaklık) için skalar ekleme
            Mat betaMat = new Mat(floatImage.Size(), floatImage.Type(), new Scalar(beta));
            Cv2.Add(floatImage, betaMat, floatImage); // Add beta safely

            // 3. 8-bit'e geri dön (clamp edilen)
            Mat output = new Mat();
            floatImage.ConvertTo(output, MatType.CV_8U); // Clamp values into [0,255]
            return output;
        }








        // 🔹 **CLAHE Uygulama**
        public static Mat ApplyCLAHEToGray(Mat inputGray)
        {
            var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            Mat enhanced = new Mat();
            clahe.Apply(inputGray, enhanced);
            return enhanced;
        }

        public static Mat ApplyCLAHEToColor(Mat inputBGR)
        {
            Mat ycrcb = new Mat();
            Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb); // BGR -> YCrCb dönüşümü

            Mat[] channels = Cv2.Split(ycrcb); // Kanalları ayır
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));

            clahe.Apply(channels[0], channels[0]); // Y (Luminance) kanalına CLAHE uygula

            Cv2.Merge(channels, ycrcb); // Kanalları geri birleştir
            Mat enhanced = new Mat();
            Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR); // YCrCb -> BGR dönüşümü

            return enhanced;
        }

        // 🔹 **White Balance (Beyaz Dengesi)**
        public static Mat AutoAdjustWhiteBalance(Mat frame)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);

            Mat[] labChannels = Cv2.Split(lab);
            Scalar meanA = Cv2.Mean(labChannels[1]);
            Scalar meanB = Cv2.Mean(labChannels[2]);

            Mat shiftA = new Mat(labChannels[1].Size(), labChannels[1].Type(), new Scalar(meanA.Val0 - 128));
            Mat shiftB = new Mat(labChannels[2].Size(), labChannels[2].Type(), new Scalar(meanB.Val0 - 128));

            Cv2.Subtract(labChannels[1], shiftA, labChannels[1]);
            Cv2.Subtract(labChannels[2], shiftB, labChannels[2]);

            Cv2.Merge(labChannels, lab);
            Mat balanced = new Mat();
            Cv2.CvtColor(lab, balanced, ColorConversionCodes.Lab2BGR);
            return balanced;
        }






        //public static Mat ApplySmartCLAHE(Mat inputBGR)
        //{
        //    Mat ycrcb = new Mat();
        //    Cv2.CvtColor(inputBGR, ycrcb, ColorConversionCodes.BGR2YCrCb); // Renk uzayını dönüştür

        //    Mat[] channels = Cv2.Split(ycrcb); // Y, Cr, Cb kanallarını ayır
        //    Mat luminance = channels[0];       // Y kanalı (aydınlık bilgisi)

        //    // Ortalama parlaklığı al
        //    double meanBrightness = Cv2.Mean(luminance).Val0;

        //    // Dinamik parametreler
        //    double clipLimit;
        //    OpenCvSharp.Size tileGridSize;

        //    if (meanBrightness > 220)
        //    {
        //        clipLimit = 5.0;
        //        tileGridSize = new OpenCvSharp.Size(2, 2); // Çok parlak → lokal kontrast vurgusu
        //    }
        //    else if(meanBrightness < 80)
        //    {
        //        clipLimit = 3.0;
        //        tileGridSize = new OpenCvSharp.Size(4, 4);
        //    }
        //    else if (meanBrightness < 170)
        //    {
        //        clipLimit = 2.0;
        //        tileGridSize = new OpenCvSharp.Size(8, 8);
        //    }
        //    else
        //    {
        //        clipLimit = 1.5;
        //        tileGridSize = new OpenCvSharp.Size(16, 16);
        //    }

        //    // CLAHE uygula
        //    CLAHE clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
        //    clahe.Apply(luminance, luminance);

        //    // Kanalları birleştir ve tekrar BGR’ye çevir
        //    Cv2.Merge(channels, ycrcb);
        //    Mat enhanced = new Mat();
        //    Cv2.CvtColor(ycrcb, enhanced, ColorConversionCodes.YCrCb2BGR);

        //    return enhanced;
        //}



        public static Mat NormalizeContrastBGR(Mat src, double targetContrast = 110)
        {
            Mat[] channels = Cv2.Split(src);
            Mat[] normalized = new Mat[3];

            for (int i = 0; i < 3; i++)
            {
                normalized[i] = NormalizeContrast(channels[i], targetContrast);
            }

            Mat result = new Mat();
            Cv2.Merge(normalized, result);
            return result;
        }

        public static Mat NormalizeContrast(Mat src, double targetContrast = 110)
        {
            // Griye çevir
            Mat gray = new Mat();
            if (src.Channels() == 3)
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = src.Clone();

            // Şu anki kontrastı hesapla (basitçe max - min)
            double minVal, maxVal;
            Cv2.MinMaxLoc(gray, out minVal, out maxVal);
            double currentContrast = maxVal - minVal;

            if (Math.Abs(currentContrast) < 1e-3) // sabit görüntü
                return src.Clone();

            // Ölçek faktörü hesapla
            double scale = targetContrast / currentContrast;

            // Genişliği merkezden ölçekle (orta gri etrafında aç/kıs)
            Mat result = new Mat();
            gray.ConvertTo(result, MatType.CV_32F);
            result = (result - new Scalar(128)) * scale + new Scalar(128);
            Cv2.MinMaxLoc(result, out minVal, out maxVal);

            // Sınırlandır ve dönüştür
            Cv2.Min(result, 255, result);
            Cv2.Max(result, 0, result);
            result.ConvertTo(result, MatType.CV_8U);

            return result;
        }


        // CLAHE'yi sadece gri kanala uygulayıp tekrar renklendirme fonksiyonu
        public static Mat ApplyCLAHEToGrayscaleAndMerge(Mat frame)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);

            Mat[] labChannels;
            Cv2.Split(lab, out labChannels);

            CLAHE clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            clahe.Apply(labChannels[0], labChannels[0]);  // CLAHE'yi sadece parlaklık kanalına uygula

            Cv2.Merge(labChannels, lab);
            Mat result = new Mat();
            Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR); // Lab'den geri dönüştür

            return result;
        }


      


    }
}
