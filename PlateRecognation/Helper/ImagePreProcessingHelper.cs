using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Accord.Imaging.Filters;

namespace PlateRecognation
{
    //static olarak kalacak
    internal class ImagePreProcessingHelper
    {
        public static Mat ColorMatToGray(Mat mat)
        {
            Mat originalImage = mat;
            //originalImage = mat.Clone();

            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);


            return grayImage;
        }

        public static Mat ColorMatToBinary(Mat mat)
        {
            Mat originalImage = mat;

            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);

            int kernel = MainForm.m_mainForm.m_preProcessingSettings.m_GaussianBlurKernel;
            Mat blurredImage = new Mat();
            Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(kernel, kernel), 0);

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, BitmapConverter.ToBitmap(grayImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(sharpenedImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(blurredImage));

            // Eşikleme işlemi
            Mat thresh = new Mat();
            Cv2.AdaptiveThreshold(grayImage, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 17, 5);

            return thresh;
        }


        public static Mat ColorMatToAdaptiveThreshold(Mat mat)
        {
            Mat originalImage = mat;

            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);


            // Eşikleme işlemi
            Mat thresh = new Mat();
            Cv2.AdaptiveThreshold(grayImage, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 15, 5);

            return thresh;
        }

        public static Mat ColorMatToGaussianBlurClaheOtsuBinary(Mat mat)
        {
            Mat originalImage = mat;


            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);

            int kernel = MainForm.m_mainForm.m_preProcessingSettings.m_GaussianBlurKernel;
            Mat blurredImage = new Mat();
            Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(kernel, kernel), 0);

            Mat imgClahe = new Mat();
            var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            clahe.Apply(blurredImage, imgClahe);


            // Eşikleme işlemi
            Mat thresh = new Mat();
            Cv2.Threshold(grayImage, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, BitmapConverter.ToBitmap(grayImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(blurredImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(imgClahe));


            return thresh;
        }

        public static Mat ColorMatToGaussianBlurClaheAdaptiveBinary(Mat mat)
        {
            Mat originalImage = mat;

            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);

            int kernel = MainForm.m_mainForm.m_preProcessingSettings.m_GaussianBlurKernel;
            Mat blurredImage = new Mat();
            Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(kernel, kernel), 0);

            Mat imgClahe = new Mat();
            var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            clahe.Apply(blurredImage, imgClahe);


            // Eşikleme işlemi
            #region Adaptive Threshould
            int block = MainForm.m_mainForm.m_preProcessingSettings.m_adaptiveThreshouldBlock;
            int c = MainForm.m_mainForm.m_preProcessingSettings.m_adaptiveThreshouldC;

            AdaptiveThresholdTypes adaptiveThresholdTypes;

            if (MainForm.m_mainForm.m_preProcessingSettings.m_AdaptiveThreshouldType == Enums.AdaptiveThreshouldType.Gaussian)
                adaptiveThresholdTypes = AdaptiveThresholdTypes.GaussianC;
            else
                adaptiveThresholdTypes = AdaptiveThresholdTypes.MeanC;

            Mat thresh = new Mat();
            Cv2.AdaptiveThreshold(blurredImage, thresh, 255, adaptiveThresholdTypes, ThresholdTypes.Binary, block, c);
            #endregion

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, BitmapConverter.ToBitmap(grayImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(blurredImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(imgClahe));

            return thresh;
        }
        public static Mat ColorMatToGaussianBlurHistEqualizeOtsuBinary(Mat mat)
        {
            Mat originalImage = mat;

            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);


            int kernel = MainForm.m_mainForm.m_preProcessingSettings.m_GaussianBlurKernel;
            Mat blurredImage = new Mat();
            Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(kernel, kernel), 0);

            Mat enhanced = new Mat();
            Cv2.EqualizeHist(blurredImage, enhanced);

            // Eşikleme işlemi
            Mat thresh = new Mat();
            Cv2.Threshold(grayImage, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, BitmapConverter.ToBitmap(grayImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(blurredImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(enhanced));

            return thresh;
        }
        public static Mat ColorMatToGaussianBlurHistEqualizeAdaptiveBinary(Mat mat)
        {
            Mat originalImage = mat;

            // Gri tonlamalıya çevirin
            Mat grayImage = new Mat();
            Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);

            int kernel = MainForm.m_mainForm.m_preProcessingSettings.m_GaussianBlurKernel;
            Mat blurredImage = new Mat();
            Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(kernel, kernel), 0);

            ////Mat medianFilter = new Mat();
            ////medianFilter = ApplyMedianBlur(blurredImage);
            //Cv2.BilateralFilter(grayImage, blurredImage, 9, 75, 75);
            ////Cv2.FastNlMeansDenoising(grayImage, blurredImage, 30, 7, 21);

            Mat enhanced = new Mat();
            Cv2.EqualizeHist(blurredImage, enhanced);

            // Eşikleme işlemi
            #region Adaptive Threshould
            int block = MainForm.m_mainForm.m_preProcessingSettings.m_adaptiveThreshouldBlock;
            int c = MainForm.m_mainForm.m_preProcessingSettings.m_adaptiveThreshouldC;

            AdaptiveThresholdTypes adaptiveThresholdTypes;

            if (MainForm.m_mainForm.m_preProcessingSettings.m_AdaptiveThreshouldType == Enums.AdaptiveThreshouldType.Gaussian)
                adaptiveThresholdTypes = AdaptiveThresholdTypes.GaussianC;
            else
                adaptiveThresholdTypes = AdaptiveThresholdTypes.MeanC;

            Mat thresh = new Mat();
            Cv2.AdaptiveThreshold(enhanced, thresh, 255, adaptiveThresholdTypes, ThresholdTypes.Binary, block, c);
            #endregion

            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox2, BitmapConverter.ToBitmap(grayImage));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox3, BitmapConverter.ToBitmap(blurredImage));
            ////DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox4, BitmapConverter.ToBitmap(medianFilter));
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox5, BitmapConverter.ToBitmap(enhanced));

            return thresh;
        }

        public static Mat SelectPreProcessingType(Mat mat)
        {
            Mat thresh = new Mat();


            if (MainForm.m_mainForm.m_preProcessingSettings.m_preProcessingType == Enums.PreProcessingType.BlurCLAHEOtsu)
            {
                thresh = ImagePreProcessingHelper.ColorMatToGaussianBlurClaheOtsuBinary(mat);
            }
            else if (MainForm.m_mainForm.m_preProcessingSettings.m_preProcessingType == Enums.PreProcessingType.BlurCLAHEAdaptive)
            {
                thresh = ImagePreProcessingHelper.ColorMatToGaussianBlurClaheAdaptiveBinary(mat);
            }
            else if (MainForm.m_mainForm.m_preProcessingSettings.m_preProcessingType == Enums.PreProcessingType.BlurHistEqualizeOtsu)
            {
                thresh = ImagePreProcessingHelper.ColorMatToGaussianBlurHistEqualizeOtsuBinary(mat);
            }
            else if (MainForm.m_mainForm.m_preProcessingSettings.m_preProcessingType == Enums.PreProcessingType.BlurHistEqualizeAdaptive)
            {
                thresh = ImagePreProcessingHelper.ColorMatToGaussianBlurHistEqualizeAdaptiveBinary(mat);
            }


            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxPlateThreshould, BitmapConverter.ToBitmap(thresh));

            return thresh;
        }

      

     

       

      

    }
}
