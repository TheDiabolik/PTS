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
    internal class MserDetectionSettings
    {
     

        #region MSERParameters
        public int Delta { get; set; }
        public int MinArea { get; set; }
        public int MaxArea { get; set; }
        public double MaxVariation { get; set; }
        public double MinDiversity { get; set; }

        public int MaxEvolution { get; set; } = 200;
        public double AreaThreshold { get; set; } = 1.01;
        public double MinMargin { get; set; } = 0.5;
        public int EdgeBlurSize { get; set; }
        #endregion

        public static MserDetectionSettings GetScaledSettings(int width, int height)
        {
            const int baseWidth = 640;
            const int baseHeight = 480;
            double scale = Math.Sqrt((width * height) / (double)(baseWidth * baseHeight));

            return new MserDetectionSettings
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
        }


       

        public override string ToString()
        {
            return $"Δ={Delta}, MinA={MinArea}, MaxA={MaxArea}, Var={MaxVariation:F2}, Div={MinDiversity:F2}, Mgn={MinMargin}, Blur={EdgeBlurSize}";
        }


        public static MserDetectionSettings TuneParamsForScene(Mat image)
        {
            double mean = Cv2.Mean(image).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(image);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(image);

            double scale = Math.Sqrt(image.Width * image.Height / (640.0 * 480.0));

            MserDetectionSettings p = new MserDetectionSettings
            {
                Delta = 5,
                MinArea = (int)(60 * scale),
                MaxArea = (int)(0.5 * image.Width * image.Height),
                MaxVariation = 0.45,
                MinDiversity = 0.25,
                AreaThreshold = 1.01,
                MinMargin = 0.003,
                EdgeBlurSize = 5
            };

            if (mean > 200)
            {
                p.MaxVariation = 0.35;
                p.MinDiversity = 0.2;
                p.Delta = (int)Math.Clamp(6 * scale, 3, 8);
            }
            else if (mean < 80)
            {
                p.MinArea = Math.Max((int)(40 * scale), 20);
                p.MaxVariation = 0.55;
                p.Delta = Math.Max(4, p.Delta - 1);
            }

            if (contrast < 40)
            {
                p.MinArea = Math.Max((int)(40 * scale), 20);
                p.MaxVariation = 0.55;
                p.Delta = Math.Max(3, p.Delta - 1);
            }

            if (stdDev < 20)
            {
                p.MinArea = Math.Max((int)(30 * scale), 20);
                p.MaxVariation = Math.Min(0.6, p.MaxVariation + 0.1);
                p.Delta = Math.Max(3, p.Delta - 1);
            }
            else if (stdDev > 70)
            {
                p.MaxVariation = Math.Min(0.45, p.MaxVariation);
                p.MinDiversity += 0.05;
            }

            // Debug bilgisi
            //Debug.WriteLine($"🔧 MSER ayarları: mean={mean:F2}, stdDev={stdDev:F2}, contrast={contrast:F2}, delta={p.Delta}, minArea={p.MinArea}");

            return p;
        }

        public static MSER TuneParamsForScene(Mat image, string inputType)
        {
            double mean = Cv2.Mean(image).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(image);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(image);

            double scale = Math.Sqrt(image.Width * image.Height / (640.0 * 480.0));

            //MserDetectionSettings p = new MserDetectionSettings
            //{
            //    Delta = 5,
            //    MinArea = (int)(60 * scale),
            //    MaxArea = (int)(0.5 * image.Width * image.Height),
            //    MaxVariation = 0.45,
            //    MinDiversity = 0.25,
            //    AreaThreshold = 1.01,
            //    MinMargin = 0.003,
            //    EdgeBlurSize = 5
            //};

            MserDetectionSettings p = GetScaledSettings(image.Width, image.Height);


            switch (inputType.ToLower())
            {
                case "gray":
                    if (mean > 200)
                    {
                        p.MaxVariation = 0.35;
                        p.MinDiversity = 0.2;
                        p.Delta = (int)Math.Clamp(6 * scale, 3, 8);
                    }
                    else if (mean < 80)
                    {
                        p.MinArea = Math.Max((int)(40 * scale), 20);
                        p.MaxVariation = 0.55;
                        p.Delta = Math.Max(4, p.Delta - 1);
                    }
                    if (contrast < 40)
                    {
                        p.MinArea = Math.Max((int)(40 * scale), 20);
                        p.MaxVariation = 0.55;
                        p.Delta = Math.Max(3, p.Delta - 1);
                    }
                    break;

                case "sobel":
                    p.MinArea = Math.Max((int)(50 * scale), 25);
                    p.MaxVariation = 0.5;
                    p.MinDiversity = 0.2;
                    if (stdDev < 30)
                        p.Delta = 3;
                    else
                        p.Delta = 5;
                    break;

                case "hybrid":
                    if (mean > 190 && stdDev < 40)
                    {
                        p.MaxVariation = 0.4;
                        p.MinDiversity = 0.2;
                        p.Delta = 4;
                    }
                    else if (mean < 100 && stdDev > 30)
                    {
                        p.MinArea = Math.Max((int)(50 * scale), 30);
                        p.MaxVariation = 0.55;
                        p.Delta = 5;
                    }
                    else
                    {
                        p.Delta = 5;
                        p.MinArea = Math.Max((int)(60 * scale), 30);
                    }
                    break;
            }

            if (stdDev < 20)
            {
                p.MinArea = Math.Max((int)(30 * scale), 20);
                p.MaxVariation = Math.Min(0.6, p.MaxVariation + 0.1);
                p.Delta = Math.Max(3, p.Delta - 1);
            }
            else if (stdDev > 70)
            {
                p.MaxVariation = Math.Min(0.45, p.MaxVariation);
                p.MinDiversity += 0.05;
            }

            //Debug.WriteLine($"🔧 MSER ayarları [{inputType.ToUpper()}]: mean={mean:F2}, stdDev={stdDev:F2}, contrast={contrast:F2}, delta={p.Delta}, minArea={p.MinArea}");


            return MSER.Create(
               p.Delta,
               p.MinArea,
               p.MaxArea,
               p.MaxVariation,
               p.MinDiversity,
               200,            // maxEvolution
               p.AreaThreshold,
               p.MinMargin,
               p.EdgeBlurSize
           );

           
        }


        public static MSER TuneParamsForScene(Mat image, string inputType, MserDetectionSettings p)
        {
            double mean = Cv2.Mean(image).Val0;
            double contrast = ImageEnhancementHelper.ComputeImageContrast(image);
            double stdDev = ImageEnhancementHelper.ComputeImageStdDev(image);

            double scale = Math.Sqrt(image.Width * image.Height / (640.0 * 480.0));


            switch (inputType.ToLower())
            {
                case "gray":
                    if (mean > 200)
                    {
                        p.MaxVariation = 0.35;
                        p.MinDiversity = 0.2;
                        p.Delta = (int)Math.Clamp(6 * scale, 3, 8);
                    }
                    else if (mean < 80)
                    {
                        p.MinArea = Math.Max((int)(40 * scale), 20);
                        p.MaxVariation = 0.55;
                        p.Delta = Math.Max(4, p.Delta - 1);
                    }
                    if (contrast < 40)
                    {
                        p.MinArea = Math.Max((int)(40 * scale), 20);
                        p.MaxVariation = 0.55;
                        p.Delta = Math.Max(3, p.Delta - 1);
                    }
                    break;

                case "sobel":
                    p.MinArea = Math.Max((int)(50 * scale), 25);
                    p.MaxVariation = 0.5;
                    p.MinDiversity = 0.2;
                    if (stdDev < 30)
                        p.Delta = 3;
                    else
                        p.Delta = 5;
                    break;

                case "hybrid":
                    if (mean > 190 && stdDev < 40)
                    {
                        p.MaxVariation = 0.4;
                        p.MinDiversity = 0.2;
                        p.Delta = 4;
                    }
                    else if (mean < 100 && stdDev > 30)
                    {
                        p.MinArea = Math.Max((int)(50 * scale), 30);
                        p.MaxVariation = 0.55;
                        p.Delta = 5;
                    }
                    else
                    {
                        p.Delta = 5;
                        p.MinArea = Math.Max((int)(60 * scale), 30);
                    }
                    break;
            }

            if (stdDev < 20)
            {
                p.MinArea = Math.Max((int)(30 * scale), 20);
                p.MaxVariation = Math.Min(0.6, p.MaxVariation + 0.1);
                p.Delta = Math.Max(3, p.Delta - 1);
            }
            else if (stdDev > 70)
            {
                p.MaxVariation = Math.Min(0.45, p.MaxVariation);
                p.MinDiversity += 0.05;
            }

            //Debug.WriteLine($"🔧 MSER ayarları [{inputType.ToUpper()}]: mean={mean:F2}, stdDev={stdDev:F2}, contrast={contrast:F2}, delta={p.Delta}, minArea={p.MinArea}");


            return MSER.Create(
               p.Delta,
               p.MinArea,
               p.MaxArea,
               p.MaxVariation,
               p.MinDiversity,
               200,            // maxEvolution
               p.AreaThreshold,
               p.MinMargin,
               p.EdgeBlurSize
           );


        }
    }

}
