using Accord.Math;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class HybridWeightHelper : IDisposable
    {
        bool m_disposed;
        public enum PlateFusionMode
        {
            Hybrid,
            GrayOnly,
            SobelOnly
           
        }

        public double GrayWeight { get; private set; }
        public double SobelWeight { get; private set; }
        public double Gamma { get; private set; }
      
        public PlateFusionMode Mode { get; private set; }

        public HybridWeightHelper()
        {
            // Default values if needed
            GrayWeight = 0.5;
            SobelWeight = 0.5;
            Gamma = 0.0;
          
        }

        public void ComputeWeights(Mat grayImage)
        {

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int adaptiveThreshold = (meanBrightness > 120) ? 220 :
                                    (meanBrightness > 100) ? 210 :
                                    (meanBrightness > 80) ? 200 : 190;

            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, adaptiveThreshold);
            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");


            // 1. Yalnızca Gray ya da yalnızca Sobel'e karar ver
            if (whiteRatio > 0.02 && meanBrightness > 150 && stdDev < 40)
            {
               Mode = PlateFusionMode.GrayOnly;
                Gamma = -8;
                GrayWeight = 1.0;
                SobelWeight = 0.0;
                //Debug.WriteLine("🔴 Aşırı parlak sahne → Sadece Gray (gamma -8)");
                return;
            }

            // 🔸 Minibüs - Parlak ama kenarlar zayıf
            if (stdDev < 55 && meanBrightness > 95 && whiteRatio > 0.07 && whiteRatio < 0.18)
            {
                Mode = PlateFusionMode.GrayOnly;
                Gamma = -8;
                GrayWeight = 1.0;
                SobelWeight = 0.0;
                //Debug.WriteLine("🟠 Minibüs → Sadece Gray (gamma -8)");
                return;
            }

            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    Mode = PlateFusionMode.GrayOnly;
                    Gamma = -8;
                    GrayWeight = 1.0;
                    SobelWeight = 0.0;
                    //Debug.WriteLine("🔸 Parlak ama kenarlar zayıf → Sadece Gray (gamma -8)");
                }
                else
                {
                    Mode = PlateFusionMode.SobelOnly;
                    Gamma = -20;
                    GrayWeight  = 0.0;
                    SobelWeight = 1.0;
                    //Debug.WriteLine("🔸 Parlak ama kontrastsız → Sadece Sobel");
                }
                return;
            }

            // 🆕 Ek koşul: Aşırı karanlık ve kontrastsız → Hibrit (0.5/0.5)
            if (whiteRatio < 0.002 && meanBrightness < 80 && stdDev < 25)
            {
                Mode = PlateFusionMode.Hybrid;
                Gamma = 1.6;
                GrayWeight = 0.5;
                SobelWeight = 0.5;
                //Debug.WriteLine("⚫ Çok karanlık sahne ama kontrast da düşük → Hibrit (0.5/0.5)");
                return;
            }

            // 2. Dinamik ağırlıklar
            GrayWeight = 0.6;
            SobelWeight = 0.4;
            Gamma = 1.0;

            if (meanBrightness > 140) Gamma = -10;
            else if (meanBrightness > 120) Gamma = -5;
            else if (meanBrightness > 100) Gamma = 0.8;
            else if (meanBrightness > 80) Gamma = 1.2;
            else Gamma = 1.5;

            if (whiteRatio < 0.002)
            {
                GrayWeight = 0.3;
                SobelWeight = 0.7;
                //Debug.WriteLine("🔹 Low whiteRatio → Gray (0.3), Sobel (0.7)");
            }
            else if (whiteRatio < 0.02)
            {
                GrayWeight = 0.8;
                SobelWeight = 0.2;
                //Debug.WriteLine("🔹 Medium whiteRatio → Gray (0.8), Sobel (0.2)");
            }
            else if (whiteRatio < 0.05)
            {
                GrayWeight = 0.45;
                SobelWeight  = 0.55;
                //Debug.WriteLine("🔹 Balanced whiteRatio → Gray (0.45), Sobel (0.55)");
            }
            else if (whiteRatio > 0.05 && stdDev < 35)
            {
                GrayWeight = 0.4;
                SobelWeight = 0.6;
                //Debug.WriteLine("🔹 Beyaz çok ama detay az → Gray (0.4), Sobel (0.6)");
            }
            else
            {
                GrayWeight = 0.5;
                SobelWeight = 0.5;
                //Debug.WriteLine("🔹 High whiteRatio → Gray (0.5), Sobel (0.5)");
            }

            // 🔧 Mode belirtilmeli:
            Mode = PlateFusionMode.Hybrid;

            //Debug.WriteLine($"🎯 Rev2 çıktı → Gamma: {Gamma}, GrayWeight: {GrayWeight}, SobelWeight: {SobelWeight}");
        }



















        public static (double grayWeight, double sobelWeight) GetAdaptiveHybridWeights(Mat grayImage)
        {
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Çok parlak piksellerin oranını hesapla
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whitePixelRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            double grayWeight = 0.45;
            double sobelWeight = 0.55;

            if (whitePixelRatio > 0.25)
            {
                sobelWeight = 0.7;
                grayWeight = 0.3;
            }
            else if (meanBrightness > 180 && stdDev > 50)
            {
                sobelWeight = 0.6;
                grayWeight = 0.4;
            }
            else if (meanBrightness < 80 && stdDev < 30)
            {
                sobelWeight = 0.3;
                grayWeight = 0.7;
            }
            // Aksi halde default ağırlık
            return (grayWeight, sobelWeight);
        }

        public static void GetAdaptiveHybridWeights(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma)
        {
            // Ortalama parlaklık ve standart sapma
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Beyaz piksel oranı (çok parlak alanlar)
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Varsayılanlar
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;

            // Çok aydınlık ve yansımalı sahneler (sobel baskın, gamma düşür)
            if (whiteRatio > 0.25)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                gamma = -30;
            }
            // Parlaklık yüksek ve kontrast iyi → Sobel biraz daha baskın, gamma hafif düşür
            else if (meanBrightness > 180 && stdDev > 50)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;
            }
            // Çok karanlık ve düşük kontrastlı sahneler → Gray baskın, gamma artır
            else if (meanBrightness < 80 && stdDev < 30)
            {
                grayWeight = 0.7;
                sobelWeight = 0.3;
                gamma = +10;
            }
            // Normal sahneler → default
        }


       

        public static void GetAdaptiveHybridWeights34TE0077AA(
    Mat grayImage,
    out double grayWeight,
    out double sobelWeight,
    out double gamma,
    out bool useOnlyGray,
    out bool useOnlySobel)
        {
            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);



            // Beyaz piksel oranı (aşırı parlaklık)
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(210), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();


            useOnlyGray = false;
            useOnlySobel = false;
            gamma = 0;
            grayWeight = 0.5;
            sobelWeight = 0.5;

            // 🔷 GÜNCELLEMELER: Minibüs sahnesi gibi durumlar için daha toleranslı eşik
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Sadece Gray");
                return;
            }

            // ⚠️ Çok kontrastsız ve karanlık ortam → Sobel'e ağırlık ver
            if (stdDev < 20 && meanBrightness < 80)
            {
                useOnlySobel = true;
                gamma = -10;
                //Debug.WriteLine("⚠️ Çok karanlık + kontrastsız → Sadece Sobel");
                return;
            }

            // 🎯 Normal senaryo → hibrit kullan, ama ağırlıkları ayarla
            // Hafif parlak ama detay var → Gray daha ağır basmalı
            if (meanBrightness > 100 && stdDev > 30)
            {
                grayWeight = 0.65;
                sobelWeight = 0.35;
                gamma = -5;
                //Debug.WriteLine("🎯 Açık sahne + orta kontrast → Gray ağırlıklı hibrit");
            }
            // Daha düşük parlaklık ve düşük detay → Sobel katkısı artırılabilir
            else if (meanBrightness < 90 && stdDev < 30)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;
                //Debug.WriteLine("🌒 Loş sahne → Sobel ağırlıklı hibrit");
            }
            else
            {
                grayWeight = 0.5;
                sobelWeight = 0.5;
                gamma = 0;
                //Debug.WriteLine("⚖️ Nötr koşullar → Eşit hibrit");
            }
        }
        public static void GetAdaptiveHybridWeightsGPTRevize1(Mat grayImage,
                                   out bool useOnlyGray,
                                   out bool useSobel,
                                   out double gamma,
                                   out double grayWeight,
                                   out double sobelWeight)
        {
            useOnlyGray = false;
            useSobel = false;
            gamma = 1.0;
            grayWeight = 0.5;
            sobelWeight = 0.5;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);
            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatio(grayImage, 240);

            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 🔷 Aşırı parlak ve kenarlar zayıf → sadece gray
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Gray (1.0), Sobel (0.0)");
                return;
            }

            // 🔷 Loş ve std düşük → Sobel baskın
            if (stdDev < 20 && meanBrightness < 100)
            {
                useSobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔷 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔷 Aşırı parlak sahne ama kenarları güçlü → Sobel baskın
            if (whiteRatio > 0.1 && stdDev > 30 && meanBrightness > 110)
            {
                gamma = 1.1;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔷 Aşırı parlak ama kenarları iyi → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔷 Aydınlık ve kenarlar belirgin → dengeli hibrit
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük ışıkta kenarlar iyiyse Sobel biraz daha baskın
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔷 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔷 Dengeli sahne → varsayılan hibrit
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük kontrast sahne → Gray baskın
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("🔷 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔷 Fallback → dengeli hibrit
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("🔷 Fallback → Gray (0.5), Sobel (0.5)");
        }

        public static void GetAdaptiveHybridWeights_Dinamik(
    Mat grayImage,
    out bool useOnlyGray,
    out bool useOnlySobel,
    out double gamma,
    out double grayWeight,
    out double sobelWeight)
        {
            useOnlyGray = false;
            useOnlySobel = false;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int adaptiveThreshold = (meanBrightness > 120) ? 220 :
                                    (meanBrightness > 100) ? 210 :
                                    (meanBrightness > 80) ? 200 : 190;

            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, adaptiveThreshold);
            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 1. Yalnızca Gray ya da yalnızca Sobel'e karar ver
            if (whiteRatio > 0.02 && meanBrightness > 150 && stdDev < 40)
            {
                useOnlyGray = true;
                gamma = -15;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔴 Aşırı parlak sahne → Sadece Gray");
                return;
            }

            // 🔸 Minibüs - Parlak ama kenarlar zayıf
            if (stdDev < 55 && meanBrightness > 95 && whiteRatio > 0.07 && whiteRatio < 0.18)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🟠 Minibüs → Sadece Gray");
                return;
            }

            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    useOnlyGray = true;
                    gamma = -10;
                    grayWeight = 1.0;
                    sobelWeight = 0.0;
                    //Debug.WriteLine("🔸 Parlak ama kenarlar zayıf → Sadece Gray");
                }
                else
                {
                    useOnlySobel = true;
                    gamma = -20;
                    grayWeight = 0.0;
                    sobelWeight = 1.0;
                    //Debug.WriteLine("🔸 Parlak ama kontrastsız → Sadece Sobel");
                }
                return;
            }

            // 2. Dinamik ağırlıklar
            grayWeight = 0.6;
            sobelWeight = 0.4;
            gamma = 1.0;

            // 🔹 Gamma değeri: kontrast ve parlaklığa bağlı ayar
            if (meanBrightness > 140) gamma = -10;
            else if (meanBrightness > 120) gamma = -5;
            else if (meanBrightness > 100) gamma = 0.8;
            else if (meanBrightness > 80) gamma = 1.2;
            else gamma = 1.5;

            // 🔹 Hibrit ağırlıklar: whiteRatio ve stdDev'e bağlı
            if (whiteRatio < 0.002)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔹 Low whiteRatio → Gray (0.3), Sobel (0.7)");
            }
            else if (whiteRatio < 0.02)
            {
                grayWeight = 0.8;
                sobelWeight = 0.2;
                //Debug.WriteLine("🔹 Medium whiteRatio → Gray (0.8), Sobel (0.2)");
            }
            else if (whiteRatio < 0.05)
            {
                grayWeight = 0.45;
                sobelWeight = 0.55;
                //Debug.WriteLine("🔹 Balanced whiteRatio → Gray (0.45), Sobel (0.55)");
            }
            else
            {
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔹 High whiteRatio → Gray (0.5), Sobel (0.5)");
            }

            //Debug.WriteLine($"🎯 Dinamik çıktı → Gamma: {gamma}, GrayWeight: {grayWeight}, SobelWeight: {sobelWeight}");
        }





        public static void GetAdaptiveHybridWeights_Dinamik_Rev2(
            Mat grayImage,
            out bool useOnlyGray,
            out bool useOnlySobel,
            out double gamma,
            out double grayWeight,
            out double sobelWeight)
        {
            useOnlyGray = false;
            useOnlySobel = false;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int adaptiveThreshold = (meanBrightness > 120) ? 220 :
                                    (meanBrightness > 100) ? 210 :
                                    (meanBrightness > 80) ? 200 : 190;

            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, adaptiveThreshold);
            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            //        //bu test için burada 
    //        if (meanBrightness > 90 && meanBrightness < 140 &&
    //whiteRatio > 0.01 && whiteRatio < 0.05 &&
    //stdDev < 45)
    //        {
    //            useOnlyGray = true;
    //            gamma = -5; // hafif kontrast artışı
    //            grayWeight = 1.0;
    //            sobelWeight = 0.0;
    //            //Debug.WriteLine("🧠 Gri ve yumuşak sahne — MSER için Gray tutuldu");
    //            return;
    //        }

            // 1. Yalnızca Gray ya da yalnızca Sobel'e karar ver
            if (whiteRatio > 0.02 && meanBrightness > 150 && stdDev < 40)
            {
                useOnlyGray = true;
                gamma = -8;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔴 Aşırı parlak sahne → Sadece Gray (gamma -8)");
                return;
            }

            // 🔸 Minibüs - Parlak ama kenarlar zayıf
            if (stdDev < 55 && meanBrightness > 95 && whiteRatio > 0.07 && whiteRatio < 0.18)
            {
                useOnlyGray = true;
                gamma = -8;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🟠 Minibüs → Sadece Gray (gamma -8)");
                return;
            }

            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    useOnlyGray = true;
                    gamma = -8;
                    grayWeight = 1.0;
                    sobelWeight = 0.0;
                    //Debug.WriteLine("🔸 Parlak ama kenarlar zayıf → Sadece Gray (gamma -8)");
                }
                else
                {
                    useOnlySobel = true;
                    gamma = -20;
                    grayWeight = 0.0;
                    sobelWeight = 1.0;
                    //Debug.WriteLine("🔸 Parlak ama kontrastsız → Sadece Sobel");
                }
                return;
            }

            // 🆕 Ek koşul: Aşırı karanlık ve kontrastsız → Hibrit (0.5/0.5)
            if (whiteRatio < 0.002 && meanBrightness < 80 && stdDev < 25)
            {
                useOnlyGray = false;
                useOnlySobel = false;
                gamma = 1.6;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("⚫ Çok karanlık sahne ama kontrast da düşük → Hibrit (0.5/0.5)");
                return;
            }

            // 2. Dinamik ağırlıklar
            grayWeight = 0.6;
            sobelWeight = 0.4;
            gamma = 1.0;

            if (meanBrightness > 140) gamma = -10;
            else if (meanBrightness > 120) gamma = -5;
            else if (meanBrightness > 100) gamma = 0.8;
            else if (meanBrightness > 80) gamma = 1.2;
            else gamma = 1.5;

            if (whiteRatio < 0.002)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔹 Low whiteRatio → Gray (0.3), Sobel (0.7)");
            }
            else if (whiteRatio < 0.02)
            {
                grayWeight = 0.8;
                sobelWeight = 0.2;
                //Debug.WriteLine("🔹 Medium whiteRatio → Gray (0.8), Sobel (0.2)");
            }
            else if (whiteRatio < 0.05)
            {
                grayWeight = 0.45;
                sobelWeight = 0.55;
                //Debug.WriteLine("🔹 Balanced whiteRatio → Gray (0.45), Sobel (0.55)");
            }
            else if (whiteRatio > 0.05 && stdDev < 35)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔹 Beyaz çok ama detay az → Gray (0.4), Sobel (0.6)");
            }
            else
            {
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔹 High whiteRatio → Gray (0.5), Sobel (0.5)");
            }

            //Debug.WriteLine($"🎯 Rev2 çıktı → Gamma: {gamma}, GrayWeight: {grayWeight}, SobelWeight: {sobelWeight}");
        }













        public static void GetAdaptiveHybridWeightsGPTFinal(Mat grayImage,
                        out bool useOnlyGray,
                        out bool useOnlySobel,
                        out double gamma,
                        out double grayWeight,
                        out double sobelWeight)
        {
            useOnlyGray = false;
            useOnlySobel = false;
            gamma = 1.0;
            grayWeight = 0.45;
            sobelWeight = 0.55;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);

            int adaptiveThreshold =
                (meanBrightness > 120) ? 220 :
                (meanBrightness > 100) ? 210 :
                (meanBrightness > 80) ? 200 :
                190;

            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, adaptiveThreshold);

            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 🔴 Aşırı parlak sahne (plaka parlaması)
            if (whiteRatio > 0.02 && meanBrightness > 150 && stdDev < 40)
            {
                useOnlyGray = true;
                gamma = -15;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔴 Aşırı parlak sahne → Sadece Gray");
                return;
            }

            // 🔸 Minibüs - Parlak ama kenarlar zayıf
            if (stdDev < 55 && meanBrightness > 95 && whiteRatio > 0.07)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🟠 Minibüs → Sadece Gray");
                return;
            }

            // 🔸 Parlak + kontrastsız
            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    useOnlyGray = true;
                    gamma = -10;
                    grayWeight = 1.0;
                    sobelWeight = 0.0;
                    //Debug.WriteLine("🟡 Parlak ama kenarlar zayıf → Sadece Gray");
                }
                else
                {
                    useOnlySobel = true;
                    gamma = -20;
                    grayWeight = 0.0;
                    sobelWeight = 1.0;
                    //Debug.WriteLine("🟡 Parlak + kontrastsız → Sadece Sobel");
                }
                return;
            }

            // 🔹 Gölgede ama parlak gövde (3 farklı parlaklık seviyesi için aynı yapı)
            if (meanBrightness > 110 && stdDev > 30)
            {
                if (whiteRatio < 0.002)
                {
                    useOnlyGray = false;
                    gamma = -7;
                    grayWeight = 0.3;
                    sobelWeight = 0.7;
                    //Debug.WriteLine("🔷 Gölgede → Gray (0.3), Sobel (0.7)");
                    return;
                }
                else if (whiteRatio < 0.02)
                {
                    useOnlyGray = false;
                    gamma = -3;
                    grayWeight = 0.8;
                    sobelWeight = 0.2;
                    //Debug.WriteLine("🔷 Gölgede → Gray (0.8), Sobel (0.2)");
                    return;
                }
                else if (whiteRatio < 0.05)
                {
                    useOnlyGray = false;
                    gamma = 1.0;
                    grayWeight = 0.45;
                    sobelWeight = 0.55;
                    //Debug.WriteLine("🔷 Gölgede → Gray (0.45), Sobel (0.55)");
                    return;
                }
            }

            // 🔸 Loş sahne
            if (stdDev < 20 && meanBrightness < 100)
            {
                useOnlySobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔸 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔸 Parlak ve net
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔸 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔸 Karanlık ama kontrastlı
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔸 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔸 Düşük kontrast sahne
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("🔸 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔸 Dengeli sahne
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔸 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔸 Fallback
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("🔸 Fallback → Gray (0.5), Sobel (0.5)");
        }


        public static void GetAdaptiveHybridWeightsGPTRevizeÇalışsan(Mat grayImage,
                                out bool useOnlyGray,
                                out bool useOnlySobel,
                                out double gamma,
                                out double grayWeight,
                                out double sobelWeight)
        {
            useOnlyGray = false;
            useOnlySobel = false;
            gamma = 1.0;
            grayWeight = 0.45;
            sobelWeight = 0.55;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);


            int adaptiveThreshold = (meanBrightness > 120) ? 220 :
                        (meanBrightness > 100) ? 210 :
                        (meanBrightness > 80) ? 200 :
                        190;



            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, adaptiveThreshold);

            //Debug.WriteLine($"\ud83d\udcca Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

           

            // 🔴 Aşırı parlak sahne (plaka parlaması) → Sadece Gray
            if (whiteRatio > 0.02 && meanBrightness > 150 && stdDev < 40)
            {
                useOnlyGray = true;
                gamma = -15;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔴 Aşırı parlak sahne → Sadece Gray");
                return;
            }

            //minibüs
            if ((stdDev < 55 && meanBrightness > 95 && whiteRatio > 0.07))
            {
                useOnlyGray = true;
                gamma = -10;

                //orjinal gpt değerleri
                grayWeight = 1.0;
                sobelWeight = 0.0;


                //Debug.WriteLine("\ud83d\udd39 Parlak ama kenarlar zayıf → Sadece Gray");
                return;

                //ahmet test
                //grayWeight = 0.45;
                //sobelWeight = 0.55;
            }

            // 🔹 Parlak ama kenarlar çok zayıf
            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    useOnlyGray = true;
                    gamma = -10;
                    //Debug.WriteLine("\ud83d\udd39 Parlak ama kenarlar zayıf → Sadece Gray");
                }
                else
                {
                    useOnlySobel = true;
                    gamma = -20;
                    //Debug.WriteLine("\ud83d\udd39 Parlak + kontrastsız → Sadece Sobel");
                }
                grayWeight = 1.0;
                sobelWeight = 0.0;
                return;
            }


            if ((meanBrightness > 110 && meanBrightness < 120) && stdDev > 30)
            {
                if (whiteRatio < 0.002)
                {
                    useOnlyGray = false;

                    gamma = -7;
                    grayWeight = 0.3;
                    sobelWeight = 0.7;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.3), Sobel (0.7)");
                    return;
                }
                else if (whiteRatio < 0.02)
                {
                    useOnlyGray = false;
                    gamma = -3;
                    grayWeight = 0.8;
                    sobelWeight = 0.2;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.8), Sobel (0.2)");
                    return;
                }
                else if (whiteRatio < 0.05)
                {
                    useOnlyGray = false;

                    gamma = 1.0;
                    grayWeight = 0.45;
                    sobelWeight = 0.55;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.45), Sobel (0.55)");
                    return;
                }
            }




            if ((meanBrightness > 120 && meanBrightness < 130)  && stdDev > 30)
            {
                if (whiteRatio < 0.002)
                {
                    useOnlyGray = false;

                    gamma = -7;
                    grayWeight = 0.3;
                    sobelWeight = 0.7;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.3), Sobel (0.7)");
                    return;
                }
                else if (whiteRatio < 0.02)
                {
                    useOnlyGray = false;
                    gamma = -3;
                    grayWeight = 0.8;
                    sobelWeight = 0.2;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.8), Sobel (0.2)");
                    return;
                }
                else if (whiteRatio < 0.05)
                {
                    useOnlyGray = false;

                    gamma = 1.0;
                    grayWeight = 0.3;
                    sobelWeight = 0.7;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.45), Sobel (0.55)");
                    return;
                }
            }


            if ((meanBrightness > 130) && stdDev > 30)
            {
                if (whiteRatio < 0.002)
                {
                    useOnlyGray = false;

                    gamma = -7;
                    grayWeight = 0.3;
                    sobelWeight = 0.7;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.3), Sobel (0.7)");
                    return;
                }
                else if (whiteRatio < 0.02)
                {
                    useOnlyGray = false;
                    gamma = -3;
                    grayWeight = 0.8;
                    sobelWeight = 0.2;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.8), Sobel (0.2)");
                    return;
                }
                else if (whiteRatio < 0.05)
                {
                    useOnlyGray = false;

                    gamma = 1.0;
                    grayWeight = 0.45;
                    sobelWeight = 0.55;
                    //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.45), Sobel (0.55)");
                    return;
                }
            }


            // 🔹 AE tipi (gölgede plaka + parlak gövde)
            //if (meanBrightness > 110 && stdDev > 30)
            //{
            //    if (whiteRatio < 0.002)
            //    {
            //        useOnlyGray = false;

            //        gamma = -7;
            //        grayWeight = 0.3;
            //        sobelWeight = 0.7;
            //        //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.3), Sobel (0.7)");
            //        return;
            //    }
            //    else if (whiteRatio < 0.02)
            //    {
            //        useOnlyGray = false;
            //        gamma = -3;
            //        grayWeight = 0.8;
            //        sobelWeight = 0.2;
            //        //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.8), Sobel (0.2)");
            //        return;
            //    }
            //    else if (whiteRatio < 0.05)
            //    {
            //        useOnlyGray = false;

            //        gamma = 1.0;
            //        grayWeight = 0.45;
            //        sobelWeight = 0.6;
            //        //Debug.WriteLine("\ud83d\udd39 Gölgede ama parlak gövde → Gray (0.45), Sobel (0.55)");
            //        return;
            //    }


            //}

            // 🔸 Loş ve std düşük → Sobel baskın
            if (stdDev < 20 && meanBrightness < 100)
            {
                useOnlySobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("\ud83d\udd38 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔸 Parlak ve net → Dengeli hibrit
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("\ud83d\udd38 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔸 Karanlık ama kontrastlı
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("\ud83d\udd38 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔸 Düşük kontrast sahne → Gray baskın
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("\ud83d\udd38 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔸 Dengeli sahne
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("\ud83d\udd38 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔸 Fallback
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("\ud83d\udd38 Fallback → Gray (0.5), Sobel (0.5)");
           
        }

        public static void GetAdaptiveHybridWeightsGPTAgırlıklı(Mat grayImage,
                                       out bool useOnlyGray,
                                       out bool useOnlySobel,
                                       out double gamma,
                                       out double grayWeight,
                                       out double sobelWeight)
        {
            useOnlyGray = false;
            useOnlySobel = false;
            gamma = 1.0;
            grayWeight = 0.5;
            sobelWeight = 0.5;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);
            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, 200);

            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 🔷 GÜNCELLEME: Gölgede kalan plaka, yüksek gövde parlaklığı → Sadece Gray kullan
            if (meanBrightness > 110 && stdDev > 35 && whiteRatio < 0.05)
            {
                useOnlyGray = false;
                gamma = -5;
                grayWeight = 0.8;
                sobelWeight = 0.2;
                //Debug.WriteLine("🔷 Gölgede ama parlak gövde → Gray (0.8), Sobel (0.2)");
                return;
            }

            // 🔷 Aşırı parlak ve kenarlar zayıf → sadece gray
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Gray (1.0), Sobel (0.0)");
                return;
            }

            // 🔷 Loş ve std düşük → Sobel baskın
            if (stdDev < 20 && meanBrightness < 100)
            {
                useOnlySobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔷 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔷 Aydınlık ve kenarlar belirgin → dengeli hibrit
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük ışıkta kenarlar iyiyse Sobel biraz daha baskın
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔷 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔷 Dengeli sahne → varsayılan hibrit
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük kontrast sahne → Gray baskın
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("🔷 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔷 Fallback → dengeli hibrit
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("🔷 Fallback → Gray (0.5), Sobel (0.5)");
        }


        public static void GetAdaptiveHybridWeightsGPTAgırlıklı2(Mat grayImage,
                                   out bool useOnlyGray,
                                   out bool useSobel,
                                   out double gamma,
                                   out double grayWeight,
                                   out double sobelWeight)
        {
            useOnlyGray = false;
            useSobel = false;
            gamma = 1.0;
            grayWeight = 0.5;
            sobelWeight = 0.5;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);
            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, 350);

            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 🔷 Çok aydınlık ve net sahne → Sobel biraz daha baskın hibrit
            if (meanBrightness > 120 && stdDev > 40 && whiteRatio < 0.06)
            {
                gamma = 1.2;
                grayWeight = 0.8;
                sobelWeight = 0.2;
                //Debug.WriteLine("🔷 Çok aydınlık ve net → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔷 Aşırı parlak ve kenarlar zayıf → sadece gray
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Gray (1.0), Sobel (0.0)");
                return;
            }

            // 🔷 Loş ve std düşük → Sobel baskın
            if (stdDev < 20 && meanBrightness < 100)
            {
                useSobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔷 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔷 Aydınlık ve kenarlar belirgin → dengeli hibrit
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük ışıkta kenarlar iyiyse Sobel biraz daha baskın
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔷 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔷 Dengeli sahne → varsayılan hibrit
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük kontrast sahne → Gray baskın
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("🔷 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔷 Fallback → dengeli hibrit
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("🔷 Fallback → Gray (0.5), Sobel (0.5)");
        }

        public static void GetAdaptiveHybridWeightsGPTAgırlıklı1(Mat grayImage,
                                            out bool useOnlyGray,
                                            out bool useSobel,
                                            out double gamma,
                                            out double grayWeight,
                                            out double sobelWeight)
        {
            useOnlyGray = false;
            useSobel = false;
            gamma = 1.0;
            grayWeight = 0.5;
            sobelWeight = 0.5;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);
            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, 250);

            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 🔷 Aşırı parlak ve kenarlar zayıf → sadece gray
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Gray (1.0), Sobel (0.0)");
                return;
            }

            // 🔷 Loş ve std düşük → Sobel baskın
            if (stdDev < 20 && meanBrightness < 100)
            {
                useSobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔷 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔷 Aydınlık ve kenarlar belirgin → dengeli hibrit
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük ışıkta kenarlar iyiyse Sobel biraz daha baskın
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔷 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔷 Dengeli sahne → varsayılan hibrit
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük kontrast sahne → Gray baskın
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("🔷 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔷 Fallback → dengeli hibrit
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("🔷 Fallback → Gray (0.5), Sobel (0.5)");
        }



        public static void GetAdaptiveHybridWeightsGPT(Mat grayImage,
                                            out bool useOnlyGray,
                                            out bool useSobel,
                                            out double gamma,
                                            out double grayWeight,
                                            out double sobelWeight)
        {
            useOnlyGray = false;
            useSobel = false;
            gamma = 1.0;
            grayWeight = 0.5;
            sobelWeight = 0.5;

            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);
            double whiteRatio = ImageEnhancementHelper.ComputeWhitePixelRatioAhmet(grayImage, 200);

            //Debug.WriteLine($"📊 Frame Stats: meanBrightness={meanBrightness:F2}, stdDev={stdDev:F2}, whiteRatio={whiteRatio:F4}");

            // 🔷 Aşırı parlak ve kenarlar zayıf → sadece gray
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                grayWeight = 1.0;
                sobelWeight = 0.0;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Gray (1.0), Sobel (0.0)");
                return;
            }

            // 🔷 Loş ve std düşük → Sobel baskın
            if (stdDev < 20 && meanBrightness < 100)
            {
                useSobel = true;
                gamma = 1.4;
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //Debug.WriteLine("🔷 Loş sahne → Gray (0.3), Sobel (0.7)");
                return;
            }

            // 🔷 Aydınlık ve kenarlar belirgin → dengeli hibrit
            if (whiteRatio >= 0.03 && meanBrightness >= 105 && stdDev > 35)
            {
                gamma = 1.3;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Parlak ve net → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük ışıkta kenarlar iyiyse Sobel biraz daha baskın
            if (meanBrightness < 90 && stdDev > 35)
            {
                gamma = 1.5;
                grayWeight = 0.4;
                sobelWeight = 0.6;
                //Debug.WriteLine("🔷 Karanlık ama kontrastlı → Gray (0.4), Sobel (0.6)");
                return;
            }

            // 🔷 Dengeli sahne → varsayılan hibrit
            if (meanBrightness > 85 && meanBrightness < 120 && stdDev > 25 && stdDev < 65)
            {
                gamma = 1.2;
                grayWeight = 0.5;
                sobelWeight = 0.5;
                //Debug.WriteLine("🔷 Dengeli sahne → Gray (0.5), Sobel (0.5)");
                return;
            }

            // 🔷 Düşük kontrast sahne → Gray baskın
            if (stdDev < 25)
            {
                gamma = 1.3;
                grayWeight = 0.7;
                sobelWeight = 0.3;
                //Debug.WriteLine("🔷 Düşük kontrast → Gray (0.7), Sobel (0.3)");
                return;
            }

            // 🔷 Fallback → dengeli hibrit
            gamma = 1.1;
            grayWeight = 0.5;
            sobelWeight = 0.5;
            //Debug.WriteLine("🔷 Fallback → Gray (0.5), Sobel (0.5)");
        }


        public static void GetAdaptiveHybridWeights34TE0077(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            double meanBrightness = Cv2.Mean(grayImage).Val0;
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(grayImage);



            // Beyaz piksel oranı (aşırı parlaklık)
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();




            useOnlyGray = false;
            useOnlySobel = false;

            // 🔷 GÜNCELLEME 1: Aşırı parlak ve kenarları zayıf durumlar (örn. minibüs sahnesi)
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Sadece Gray (yüksek eşik)");
                grayWeight = 1;
                sobelWeight = 0;
                return;
            }

            // 🔷 GÜNCELLEME 2: Orta seviyede parlaklık + zayıf kontrast (örn. 34TE0077 gibi)
            if (whiteRatio > 0.045 && stdDev < 50 && meanBrightness > 100)
            {
                useOnlyGray = true;
                gamma = -10;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Sadece Gray (orta seviye)");
                grayWeight = 1;
                sobelWeight = 0;
                return;
            }

            // 🔷 Aşırı parlak ama kontrast çok düşük değilse → Sobel etkisi baskın olabilir
            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    useOnlyGray = true;
                    gamma = -10;
                    //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Sadece Gray (genel eşik)");
                }
                else
                {
                    useOnlySobel = true;
                    gamma = -20;
                    //Debug.WriteLine("🔷 Çok parlak + kontrastsız → Sadece Sobel");
                }

                grayWeight = 1;
                sobelWeight = 0;
                return;
            }

            // 🔧 HİBRİT DURUM (Normal koşullar)
            // Gri ve Sobel görüntüleri harmanla, oranları sahneye göre ayarla
            grayWeight = 0.8;
            sobelWeight = 0.2;
            gamma = -5;
            //Debug.WriteLine("✅ Hibrit karışım → Gray %80 + Sobel %20");
        }

        public static void GetAdaptiveHybridWeightsMinibus(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            // Ortalama parlaklık ve standart sapma
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Beyaz piksel oranı (aşırı parlaklık)
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(220), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Başlangıç değerleri
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;
            useOnlyGray = false;
            useOnlySobel = false;

            //Debug.WriteLine($"[HybridDebug] mean={meanBrightness:F1}, stdDev={stdDev:F1}, whiteRatio={whiteRatio:P2}");

            // 🔷 GÜNCELLEMELER: Minibüs sahnesi gibi durumlar için daha toleranslı bir eşik
            if (whiteRatio > 0.07 && stdDev < 55 && meanBrightness > 95)
            {
                useOnlyGray = true;
                gamma = -10;
                //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Sadece Gray");
                return;
            }

            // 🔻 Çok parlak ama kontrast da zayıfsa → Sadece Sobel
            if (whiteRatio > 0.03 && stdDev < 20 && meanBrightness > 120)
            {
                useOnlySobel = true;
                gamma = -20;
                //Debug.WriteLine("🔻 Çok parlak + çok düşük kontrast → Sadece Sobel");
                return;
            }

            // 🔸 Çok karanlık ve kontrastsız → Sadece Gray
            if (meanBrightness < 60 && stdDev < 25)
            {
                if (whiteRatio > 0.02)
                {
                    grayWeight = 0.6;
                    sobelWeight = 0.4;
                    gamma = 5;
                    //Debug.WriteLine("🔸 Karanlık + lokal parlaklık → Gray ağırlıklı hibrit");
                    return;
                }

                useOnlyGray = true;
                gamma = 15;
                //Debug.WriteLine("🔸 Çok karanlık + kontrastsız → Sadece Gray");
                return;
            }

            // 🔺 Çok aydınlık sahne → Sobel ağırlıklı
            if (whiteRatio > 0.25)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                gamma = -30;
                //Debug.WriteLine("🔺 Aşırı parlak sahne → Sobel ağırlıklı hibrit");
                return;
            }

            // 🔹 Güçlü kenar + aydınlık → Sobel ağırlıklı
            if (meanBrightness > 180 && stdDev > 50)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;
                //Debug.WriteLine("🔹 Aydınlık + güçlü kenar → Sobel ağırlıklı");
                return;
            }

            // 🔸 Karanlık ama yer yer parlamalar varsa
            if (meanBrightness < 80 && stdDev < 30)
            {
                if (whiteRatio < 0.05)
                {
                    useOnlyGray = true;
                    gamma = 15;
                    //Debug.WriteLine("🔸 Düşük parlaklık + düşük kontrast → Sadece Gray");
                    return;
                }
                else
                {
                    grayWeight = 0.7;
                    sobelWeight = 0.3;
                    gamma = 10;
                    //Debug.WriteLine("🔸 Karanlık ama biraz parlaklık var → Gray ağırlıklı hibrit");
                    return;
                }
            }

            // 🔘 Genel sahne → Varsayılan Hibrit
            //Debug.WriteLine("🔘 Genel durum → Hibrit kullanılıyor");
        }

        public static void GetAdaptiveHybridWeightsv1(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            // Ortalama parlaklık ve standart sapma
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Beyaz (çok parlak) piksel oranı
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Varsayılanlar
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;
            useOnlyGray = false;
            useOnlySobel = false;

            // 🔷 Parlaklık baskın, kenar yok → Gray daha güvenli
            if (whiteRatio > 0.02 && stdDev < 25 && meanBrightness > 120)
            {
                useOnlyGray = true;
                gamma = -10;
                //Debug.WriteLine("☀️ Çok parlak ve detay yok → Gray tercih edildi.");
                return;
            }

            // 🌙 Karanlık ve kontrast düşük → Gray tercih
            if (meanBrightness < 70 && stdDev < 20)
            {
                useOnlyGray = true;
                gamma = 10;
                //Debug.WriteLine("🌙 Karanlık ve kontrast düşük → Gray tercih edildi.");
                return;
            }

            // ⚡️ Aşırı parlak ve çok fazla beyaz alan → Sobel
            if (whiteRatio > 0.25 && stdDev < 40)
            {
                useOnlySobel = true;
                gamma = -15;
                //Debug.WriteLine("⚡️ Aşırı parlak ve çok fazla beyaz alan → Sadece Sobel.");
                return;
            }

            // 📷 Detay zenginliği yüksek sahne → Hibrit uygundur
            if (stdDev > 40 && whiteRatio < 0.15)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = 0;
                //Debug.WriteLine("📷 Detaylı sahne → Hibrit uygulanıyor.");
                return;
            }

            // 🔸 Orta seviye her şey → Dengeli hibrit
            grayWeight = 0.5;
            sobelWeight = 0.5;
            gamma = 0;
            //Debug.WriteLine("⚖️ Orta seviye aydınlık/kontrast → Dengeli Hibrit.");
        }


        public static void GetAdaptiveHybridWeights34MTestiÖncesi(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            // Ortalama parlaklık ve kontrast
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Beyaz oranı (aşırı parlaklık ölçütü)
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(200), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Varsayılan ayarlar
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;
            useOnlyGray = false;
            useOnlySobel = false;

            // 🔴 Aşırı parlak plaka sahneleri: sadece Gray kullan
            if (whiteRatio > 0.02 && meanBrightness > 150 && stdDev < 40)
            {
                useOnlyGray = true;
                gamma = -15;

                //Debug.WriteLine("🔴 Aşırı parlak sahne (plaka parlaması) → Sadece Gray");
                return;
            }

            // 🔸 Çok karanlık ve kontrastsızsa → Gray
            if (meanBrightness < 60 && stdDev < 25)
            {
                if (whiteRatio > 0.02)
                {
                    grayWeight = 0.6;
                    sobelWeight = 0.4;
                    gamma = 5;

                    //Debug.WriteLine("🔸 Karanlık sahne ama lokal parlamalar var → Hibrit (Gray ağırlıklı)");
                }
                else
                {
                    useOnlyGray = true;
                    gamma = 15;

                    //Debug.WriteLine("🔸 Çok karanlık ve kontrastsız → Sadece Gray");
                }
                return;
            }

            // 🔷 Düşük kontrast + yüksek parlaklık → Sobel çok zayıfsa Gray'e öncelik ver
            if (whiteRatio > 0.015 && stdDev < 30)
            {
                if (meanBrightness > 100)
                {
                    useOnlyGray = true;
                    gamma = -10;

                    //Debug.WriteLine("🔷 Parlak ama kenarlar zayıf → Sadece Gray");
                }
                else
                {
                    useOnlySobel = true;
                    gamma = -20;

                    //Debug.WriteLine("🔷 Çok parlak + kontrastsız → Sadece Sobel");
                }
                return;
            }

            // 🔶 Çok parlak sahnelerde Sobel öncelikli hibrit
            if (whiteRatio > 0.25)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                gamma = -30;

                //Debug.WriteLine("🔶 Aşırı parlak sahne (genel) → Sobel ağırlıklı");
            }
            else if (meanBrightness > 180 && stdDev > 50)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;

                //Debug.WriteLine("🔶 Aydınlık ve keskin sahne → Sobel ağırlıklı");
            }
            else if (meanBrightness < 80 && stdDev < 30)
            {
                if (whiteRatio < 0.05)
                {
                    useOnlyGray = true;
                    gamma = 15;

                    //Debug.WriteLine("🔶 Düşük parlaklık + düşük kontrast → Sadece Gray");
                    return;
                }
                else
                {
                    grayWeight = 0.7;
                    sobelWeight = 0.3;
                    gamma = 10;

                    //Debug.WriteLine("🔶 Karanlık ama lokal aydınlık → Gray ağırlıklı hibrit");
                }
            }
        }


        public static void GetAdaptiveHybridWeightsvOld(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            // Ortalama parlaklık ve standart sapma
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Beyaz (çok parlak) piksel oranı
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Varsayılanlar
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;
            useOnlyGray = false;
            useOnlySobel = false;

            // 🔹 Aşırı parlak ve kenar zayıfsa → sadece sobel
            if (whiteRatio > 0.015 && stdDev < 30)
            {
                // Çok fazla parlak bölge yok ama düşük kontrast varsa
                if (meanBrightness > 100)
                {
                    // Gri tonlar da hâlâ bilgi taşıyabilir
                    //grayWeight = 0.6;
                    //sobelWeight = 0.4;
                    //gamma = -10;

                    useOnlyGray = true;
                    gamma = -10;

                    //Debug.WriteLine("Ahmet Deneme Sobel ağırlıklı (parlak + kontrastsız ama gray de kullanılabilir)");
                }
                else
                {
                    // Gerçekten kötü bir aydınlatma durumu
                    useOnlySobel = true;
                    gamma = -20;

                    //Debug.WriteLine("Sadece Sobel (çok parlak + çok düşük kontrast)");
                    return;
                }
            }

            // 🔹 Aşırı karanlık ve kontrast zayıfsa → sadece gray Mİ?
            if (meanBrightness < 60 && stdDev < 25)
            {
                if (whiteRatio > 0.02)
                {
                    // Karanlık ama lokal parlaklık varsa hibrit (gray ağırlıklı)
                    grayWeight = 0.6;
                    sobelWeight = 0.4;
                    gamma = 5;

                    //Debug.WriteLine("Karanlık + lokal parlaklık → Gray ağırlıklı hibrit");
                    return;
                }

                useOnlyGray = true;
                gamma = 15;

                //Debug.WriteLine("Sadece Gray (çok karanlık + kontrastsız)");
                return;
            }

            // 🔸 Genel aydınlık ve kontrastlı sahne → Sobel ağırlıklı
            if (whiteRatio > 0.25)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                gamma = -30;

                //Debug.WriteLine("Sobel ağırlıklı (çok parlak sahne)");
            }
            else if (meanBrightness > 180 && stdDev > 50)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;

                //Debug.WriteLine("Sobel ağırlıklı1 (aydınlık + güçlü kenar)");
            }
            else if (meanBrightness < 80 && stdDev < 30)
            {
                if (whiteRatio < 0.05)
                {
                    useOnlyGray = true;
                    gamma = 15;

                    //Debug.WriteLine("Gray2 (düşük parlaklık + düşük kontrast + az beyaz alan)");
                    return;
                }
                else
                {
                    grayWeight = 0.7;
                    sobelWeight = 0.3;
                    gamma = 10;

                    //Debug.WriteLine("Sobel ağırlıklı2 (karanlık ama biraz aydınlık var)");
                }
            }
        }




        public static void GetAdaptiveHybridWeightsVOld2(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            // Ortalama parlaklık ve standart sapmayı hesapla
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            // Çok parlak piksellerin oranı (whiteRatio)
            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Varsayılan ayarlar
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;
            useOnlyGray = false;
            useOnlySobel = false;

            // 🔹 Yalnızca Sobel kullanılacak durum (çok parlak ve düşük detay)
            if (whiteRatio > 0.015 && stdDev < 30)
            {
                useOnlySobel = true;
                gamma = -20;
                //Debug.WriteLine("Sadece Sobel.");
                return;
            }

            // 🔹 Yalnızca Gray kullanılacak durum (çok karanlık ve düşük detay)
            if (meanBrightness < 60 && stdDev < 25)
            {
                useOnlyGray = true;
                gamma = 10;
                //Debug.WriteLine("Sadece Gray.");
                return;
            }

            // 🔹 Orta-karanlık, az detay → gerekirse sadece gray'e geç
            else if (meanBrightness < 80 && stdDev < 30)
            {
                if (whiteRatio < 0.05)
                {
                    useOnlyGray = true;
                    gamma = 15;
                    //Debug.WriteLine("Sadece Gray (karanlık ve düz).");
                    return;
                }
                else
                {
                    grayWeight = 0.7;
                    sobelWeight = 0.3;
                    gamma = 10;
                    //Debug.WriteLine("Gray ağırlıklı (karanlık).");
                }
            }

            // 🔹 Aydınlık sahneler için sobel ağırlıklı
            else if (meanBrightness > 165 && meanBrightness < 200 && stdDev > 40)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;
                //Debug.WriteLine("Sobel ağırlıklı (aydınlık ve detaylı).");
            }

            // 🔹 Fazla aydınlık ve whiteRatio yüksekse daha da sobel ağırlıklı yap
            else if (whiteRatio > 0.025 && stdDev > 30)
            {
                grayWeight = 0.35;
                sobelWeight = 0.65;
                gamma = -15;
                //Debug.WriteLine("Sobel ağırlıklı (beyaz alanlar belirgin).");
            }

            // Aksi halde default
        }



        public static void GetAdaptiveHybridWeightsvOld1(Mat grayImage, out double grayWeight, out double sobelWeight, out double gamma, out bool useOnlyGray, out bool useOnlySobel)
        {
            Cv2.MeanStdDev(grayImage, out Scalar mean, out Scalar stddev);
            double meanBrightness = mean.Val0;
            double stdDev = stddev.Val0;

            Mat whiteMask = new Mat();
            Cv2.Compare(grayImage, new Scalar(240), whiteMask, CmpType.GT);
            double whiteRatio = Cv2.CountNonZero(whiteMask) / (double)(grayImage.Rows * grayImage.Cols);
            whiteMask.Dispose();

            // Varsayılanlar
            grayWeight = 0.45;
            sobelWeight = 0.55;
            gamma = 0;
            useOnlyGray = false;
            useOnlySobel = false;

            if (whiteRatio > 0.05 && stdDev < 20)
            {
                useOnlySobel = true;
                gamma = -20;
                //Debug.WriteLine("Sadece Sobel (aşırı parlak ve düz).");
                return;
            }
            else if (whiteRatio > 0.015 && stdDev < 30)
            {
                grayWeight = 0.25;
                sobelWeight = 0.75;
                gamma = -10;
                //Debug.WriteLine("Sobel ağırlıklı (orta derecede parlak ve düşük kontrast).");
                return;
            }
            else if (meanBrightness < 60 && stdDev < 25)
            {
                //useOnlyGray = true;
                //gamma = 10;

                ////Debug.WriteLine("Sadece Gray.");


                //return;

                if (whiteRatio < 0.01) // Neredeyse hiç ışık yoksa
                {
                    useOnlyGray = true;
                    gamma = 15;
                    //Debug.WriteLine("Çok karanlık, sadece Gray.");
                    return;
                }
                else
                {
                    // Yine düşük ışık, ama biraz sobel desteği olabilir
                    grayWeight = 0.65;
                    sobelWeight = 0.35;
                    gamma = 10;
                    //Debug.WriteLine("Karanlık ama Sobel de yardımcı olabilir.");
                }
            }
            else if (whiteRatio > 0.012)
            {
                grayWeight = 0.3;
                sobelWeight = 0.7;
                //gamma = -30;

                //Debug.WriteLine("sobel ağırlıklı");
            }
            else if (meanBrightness > 180 && stdDev > 50)
            {
                grayWeight = 0.4;
                sobelWeight = 0.6;
                gamma = -10;

                //Debug.WriteLine("sobel ağırlıklı1");
            }
            else if (meanBrightness < 80 && stdDev < 30)
            {
                //grayWeight = 0.7;
                //sobelWeight = 0.3;
                //gamma = 10;



                if (whiteRatio < 0.02)
                {
                    useOnlyGray = true;
                    gamma = 15;
                    //Debug.WriteLine("gray 2");
                    return;
                }
                else
                {
                    grayWeight = 0.7;
                    sobelWeight = 0.3;
                    gamma = 10;
                    //Debug.WriteLine("sobel ağırlıklı2");
                }

            }


            //// Aşırı parlak ve kontrast düşük → sadece sobel
            //if (whiteRatio > 0.3)
            //{
            //    useOnlySobel = true;
            //    gamma = -20;

            //    //Debug.WriteLine("Sadece Sobel.");

            //    return;
            //}

            //// Aşırı karanlık ve düşük kenar yoğunluğu → sadece gray
            //if (meanBrightness < 60 && stdDev < 25)
            //{
            //    useOnlyGray = true;
            //    gamma = 10;

            //    //Debug.WriteLine("Sadece Gray.");
            //    return;
            //}

            //// Aydınlık ve güçlü kenar → sobel ağırlıklı
            //if (whiteRatio > 0.25)
            //{
            //    grayWeight = 0.3;
            //    sobelWeight = 0.7;
            //    gamma = -30;

            //    //Debug.WriteLine("sobel ağırlıklı");
            //}
            //else if (meanBrightness > 180 && stdDev > 50)
            //{
            //    grayWeight = 0.4;
            //    sobelWeight = 0.6;
            //    gamma = -10;

            //    //Debug.WriteLine("sobel ağırlıklı1");
            //}
            //else if (meanBrightness < 80 && stdDev < 30)
            //{
            //    if (whiteRatio < 0.05)
            //    {
            //        useOnlyGray = true;
            //        gamma = 15;

            //        //Debug.WriteLine("gray 2");

            //        return;
            //    }
            //    else
            //    {
            //        grayWeight = 0.7;
            //        sobelWeight = 0.3;
            //        gamma = 10;

            //        //Debug.WriteLine("sobel ağırlıklı2");
            //    }


            //}
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!m_disposed)
        //    {
        //        if (disposing)
        //        {
        //            // Dispose time code 
        //            //buraya sonlanma için method eklenecek
        //        }

        //        // Finalize time code 
        //        m_disposed = true;
        //    }


        //}

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    //dispose managed resources
                }
            }
            //dispose unmanaged resources
            m_disposed = true;
        }
        public void Dispose()
        {
            //if (m_disposed)
            {
                Dispose(true);

                GC.SuppressFinalize(this);
            }
        }
    }
}
