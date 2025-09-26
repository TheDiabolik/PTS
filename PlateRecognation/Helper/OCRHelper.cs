using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class OCRHelper
    {
        //static Stopwatch sw = new Stopwatch();
        public static (string plateKara, double confidence, List<Mat> characterMat, TupleList<string, double>) SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(List<Mat> characterRegions, int confidence, int countingFrameCount)
        {
            //sw.Restart();

            // Her karakterin güven skorlarını depolamak için bir liste
            List<double> characterConfidences = new List<double>();
            List<Mat> characterMat = new List<Mat>();
            TupleList<string, double> tupleList = new TupleList<string, double>();

            string plateKara = "";
            double plateConfidence = 0;

            foreach (var bbox in characterRegions)
            {
                Mat mat = bbox.Clone();

                Cv2.Resize(mat, mat, new OpenCvSharp.Size(20, 20)); // HOG ile uyumlu boyut
                //Cv2.Resize(mat, mat, new OpenCvSharp.Size(20, 20), 0, 0, InterpolationFlags.Lanczos4);

                var prediction = CNNHelper.TestCNN(MainForm.m_mainForm.m_loadedCNN, mat);

                //if (prediction.confidence > confidence)
                {
                    int eeee = prediction.predictedClass;

                    string cha = FindCharacter(eeee);

                    if (cha != "NC")
                    {
                        plateKara += cha;
                        characterConfidences.Add(prediction.confidence);
                        characterMat.Add(bbox);

                        tupleList.Add(cha, prediction.confidence);

                    }

                }

            }


 

            //if (!string.IsNullOrEmpty(plateKara) && (OCRHelper..Plate(plateKara.ToCharArray())))
            //if (!string.IsNullOrEmpty(plateKara) && (plateKara.Length >= 6) && (PlateFormatHelper.IsPlatePatternValid(plateKara)))
            //if (!string.IsNullOrEmpty(plateKara) && (plateKara.Length >= 6) && (enforceTurkishPlatePattern == Enums.PlateType.All || PlateFormatHelper.IsProbablyTurkishPlate(plateKara)))
            if (!string.IsNullOrEmpty(plateKara) && (plateKara.Length >= 6))
            {
                // Tüm karakterlerin ortalama güven skoru
                plateConfidence = characterConfidences.Average();

                if (plateConfidence >= confidence)
                {
                    ////Debug.WriteLine("PTS Geçen Süre : " + sw.ElapsedMilliseconds.ToString());
                    return (plateKara, plateConfidence, characterMat, tupleList);
                }
                    
            }
            else
                plateKara = "";


            ////Debug.WriteLine("PTS Geçen Süre : " + sw.ElapsedMilliseconds.ToString());

            return (plateKara, plateConfidence, characterMat, tupleList);

            
        }

        public static  (string plateKara, double confidence, List<Mat> characterMat) SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(List<Mat> characterRegions, int confidence, int countingFrameCount, Enums.PlateType enforceTurkishPlatePattern = Enums.PlateType.All)
        {
            // Her karakterin güven skorlarını depolamak için bir liste
            List<double> characterConfidences = new List<double>();
            List<Mat> characterMat = new List<Mat>();
            TupleList<string, double> tupleList = new TupleList<string, double>();

            string plateKara = "";
            double plateConfidence = 0;

            foreach (var bbox in characterRegions)
            {
                Mat mat = bbox.Clone();

                Cv2.Resize(mat, mat, new OpenCvSharp.Size(20, 20)); // HOG ile uyumlu boyut

                //Cv2.Resize(mat, mat, new OpenCvSharp.Size(20, 20), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

                //mat = ResizeWithAspectRatio(mat, 16, 32);


                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, BitmapConverter.ToBitmap(mat));


                var prediction = CNNHelper.TestCNN(MainForm.m_mainForm.m_loadedCNN, mat);

                //if (prediction.confidence > confidence)
                {
                    int eeee = prediction.predictedClass;

                    string cha = FindCharacter(eeee);

                    if (cha != "NC")
                    {
                        plateKara += cha;
                        characterConfidences.Add(prediction.confidence);
                        characterMat.Add(bbox);

                        tupleList.Add(cha, prediction.confidence);

                    }

                }

            }




            //if (!string.IsNullOrEmpty(plateKara) && (OCRHelper..Plate(plateKara.ToCharArray())))
            //if (!string.IsNullOrEmpty(plateKara) && (plateKara.Length >= 6) && (PlateFormatHelper.IsPlatePatternValid(plateKara)))
            if (!string.IsNullOrEmpty(plateKara) && (plateKara.Length >= 6) && (enforceTurkishPlatePattern == Enums.PlateType.All || PlateFormatHelper.IsProbablyTurkishPlate(plateKara)))
            //if (!string.IsNullOrEmpty(plateKara))
            {
                // Tüm karakterlerin ortalama güven skoru
                plateConfidence = characterConfidences.Average();

                if (plateConfidence >= confidence )
                    return (plateKara, plateConfidence, characterMat);
            }
            else
                plateKara = "";



            //if (!string.IsNullOrEmpty(plateKara) && (plateKara.Count() >= 7))
            //if (!string.IsNullOrEmpty(plateKara) && (OCRHelper.Plate(plateKara.ToCharArray())))
            //if (!string.IsNullOrEmpty(plateKara))
            //{
            //string applyMajority = ApplyMajorityVoting(detectedPlates, plateKara, countingFrameCount);

            ////plateKara = applyMajority;

            //return plateKara;





            return (plateKara, plateConfidence, characterMat);
        }



        public static Mat ResizeWithAspectRatio(Mat input, int targetWidth, int targetHeight)
        {
            int originalWidth = input.Width;
            int originalHeight = input.Height;

            if (originalWidth == 0 || originalHeight == 0)
                return new Mat(targetHeight, targetWidth, MatType.CV_8UC1, Scalar.White);

            float aspectRatioInput = (float)originalWidth / originalHeight;
            float aspectRatioTarget = (float)targetWidth / targetHeight;

            int newWidth, newHeight;

            if (aspectRatioInput > aspectRatioTarget)
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / aspectRatioInput);
            }
            else
            {
                newHeight = targetHeight;
                newWidth = (int)(targetHeight * aspectRatioInput);
            }

            Mat resized = new Mat();
            Cv2.Resize(input, resized, new OpenCvSharp.Size(newWidth, newHeight),0,0,InterpolationFlags.Lanczos4);

            // 🔲 Beyaz zeminli padding ekle
            int top = (targetHeight - newHeight) / 2;
            int bottom = targetHeight - newHeight - top;
            int left = (targetWidth - newWidth) / 2;
            int right = targetWidth - newWidth - left;

            Mat padded = new Mat();
            Cv2.CopyMakeBorder(resized, padded, top, bottom, left, right, BorderTypes.Constant, Scalar.White);

            return padded;
        }








        public static async Task<(string plateKara, double confidence, List<Mat> characterMat)>
    SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2Async(List<Mat> characterRegions, int confidence, int countingFrameCount)
        {
            List<double> characterConfidences = new List<double>();
            List<Mat> characterMat = new List<Mat>();
            string plateKara = "";
            double plateConfidence = 0;

            List<Task<(int predictedClass, double confidence)>> tasks = new List<Task<(int, double)>>();

            foreach (var bbox in characterRegions)
            {
                Mat mat = bbox.Clone();
                Cv2.Resize(mat, mat, new OpenCvSharp.Size(20, 20)); // HOG ile uyumlu boyut


                //var prediction = CNNHelper.TestCNNAsync(MainForm.m_mainForm.m_loadedCNN, mat).Wait();
                //prediction.wa


                //int eeee = prediction.predictedClass;

                //string cha = FindCharacter(eeee);

                //if (cha != "NC")
                //{
                //    plateKara += cha;
                //    characterConfidences.Add(prediction.confidence);
                //    characterMat.Add(bbox);
                //}
                // **Asenkron olarak TestCNN çağır**
                tasks.Add(CNNHelper.TestCNNAsync(MainForm.m_mainForm.m_loadedCNN, mat));
            }

            // **Tüm asenkron işlemlerin bitmesini bekle**
            var results = await Task.WhenAll(tasks);




            for (int i = 0; i < results.Length; i++)
            {
                var prediction = results[i];
                int predictedClass = prediction.predictedClass;
                string cha = FindCharacter(predictedClass);

                if (cha != "NC")
                {
                    plateKara += cha;
                    characterConfidences.Add(prediction.confidence);
                    characterMat.Add(characterRegions[i]);
                }
            }

            if (!string.IsNullOrEmpty(plateKara) && plateKara.Length >= 7)
            {
                plateConfidence = characterConfidences.Average();
                if (plateConfidence >= confidence)
                    return (plateKara, plateConfidence, characterMat);
            }

            return ("", 0, characterMat);
        }
        public static string FindCharacter(int value)
        {
            string[] classes = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "V", "Y", "Z", "NC" };

            string osman = classes[value];
            return osman;
        }
    }
}
