using Accord.Imaging.Filters;
using Accord.Math;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class OCRProcess
    {
        public static void NormalTESTDetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages(object mat)
        {
            Mat processImage = (Mat)mat;

            // Geçici kutular (scope dışında kullanılacaklar)
            Rect[] concatMSER = null;
            Rect[] concatSobel = null;

            //Helper.RemovePlateListThreadSafe();

            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {
                //mser detection plate
                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);

                ////edge detection plate with sobel
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

                //sonuçları birleştir
                concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
            }

            Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();


            List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, processImage);


            if (possiblePlateRegions != null && possiblePlateRegions.Count > 0)
            {
                // Score'u en yüksek olan PossiblePlate'i bul
                //PossiblePlate bestPlate = possiblePlateRegions
                //    .OrderByDescending(p => p.PlateScore)
                //   .First();

                PossiblePlate bestPlate = possiblePlateRegions
                    .OrderByDescending(p => p.PlateScore)
                    .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                   .First();



                ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(bestPlate);


                //if (MainForm.m_mainForm.m_signPlate)
                //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);



                Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
            }





        }

            public static void MNSTESTDetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages(object mat)
        {
            Mat processImage = (Mat)mat;

            // Geçici kutular (scope dışında kullanılacaklar)
            Rect[] concatMSER = null;
            Rect[] concatSobel = null;

            //Helper.RemovePlateListThreadSafe();

            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {
                //mser detection plate
                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);

                ////edge detection plate with sobel
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

                //sonuçları birleştir
                concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
            }

            Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();


            List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptiveNMS(concatPlatesRect, processImage);


            if (possiblePlateRegions != null && possiblePlateRegions.Count > 0)
            {
                // Score'u en yüksek olan PossiblePlate'i bul
                //PossiblePlate bestPlate = possiblePlateRegions
                //    .OrderByDescending(p => p.PlateScore)
                //   .First();

                PossiblePlate bestPlate = possiblePlateRegions
                    .OrderByDescending(p => p.PlateScore)
                    .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                   .First();



                ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(bestPlate);


                //if (MainForm.m_mainForm.m_signPlate)
                //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);



                Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
            }

          

          



            ////ThreadSafeList<PlateResult> ts =  Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings.m_PlateType);
            //ThreadSafeList<PlateResult> ts = Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);


            //if (ts.Count > 0)
            //{
            //    //PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidatesWithCorrection(ts);

            //    //listedeki bütün plakalar aynı iste burası
            //    PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(ts);

            //    if (bestPlate != null)
            //    {
            //        //Debug.WriteLine($"Seçilen plaka: {bestPlate.readingPlateResult} - Güven: {bestPlate.readingPlateResultProbability:F2}");

            //        MainForm.m_mainForm.m_plateResults.Add(bestPlate);

            //        if (MainForm.m_mainForm.m_signPlate)
            //            PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);

            //    }
            //    else
            //    {

            //        //buranında kontroledilmesi lazım
            //        var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(ts);

            //        Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;

            //        List<PlateResult> bestPlates = new List<PlateResult>();

            //        if (plateType == Enums.PlateType.Turkish)
            //            bestPlates = PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity);
            //        else
            //        {
            //            foreach (List<PlateResult> item in groupPlatesByProximity)
            //            {
            //                bestPlates.AddRange(item);
            //            }


            //        }

            //        foreach (PlateResult besties in bestPlates)
            //        {
            //            MainForm.m_mainForm.m_plateResults.Add(besties);

            //            if (MainForm.m_mainForm.m_signPlate)
            //                PlateHelper.DrawPossiblePlateRegionToOriginalImage(besties.addedRects, processImage);
            //        }

            //    }
            //}

        }


        public static void HibritTESTDetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages(object mat)
        {
            Mat processImage = (Mat)mat;

            // Geçici kutular (scope dışında kullanılacaklar)
            Rect[] MSERHybridBboxes = null;
            Rect[] graySobelBboxes = null;
            Rect[] sobelBboxes = null;

            //Helper.RemovePlateListThreadSafe();

            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {
                Mat hybrid = new Mat();
                //Cv2.AddWeighted(grayImage, 0.35, sobelImage, 0.65, 0, hybrid);

                Cv2.Max(grayImage, sobelImage, hybrid);

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(hybrid));

                //mser detection plate
               MSERHybridBboxes = MSEROperations.FindPlateRegionROI(sobelImage);

                ////edge detection plate with sobel
                 graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                 sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

               
            }

            //sonuçları birleştir
            Rect[] allRects = MSERHybridBboxes.Concat(sobelBboxes).Concat(graySobelBboxes).ToArray();


            List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesFromROI(allRects, processImage);


          


            if (possiblePlateRegions != null && possiblePlateRegions.Count > 0)
            {
                // Score'u en yüksek olan PossiblePlate'i bul
                //PossiblePlate bestPlate = possiblePlateRegions
                //    .OrderByDescending(p => p.PlateScore)
                //   .First();

                PossiblePlate bestPlate = possiblePlateRegions
                    .OrderByDescending(p => p.PlateScore)
                    .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                   .First();



                ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(bestPlate);


                //if (MainForm.m_mainForm.m_signPlate)
                //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);



                Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
            }




        }

        public static void TEOkumaTestHibritTESTDetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages(object mat)
        {
            Mat processImage = (Mat)mat;

            // Geçici kutular (scope dışında kullanılacaklar)
            Rect[] MSERHybridBboxes = null;
            Rect[] graySobelBboxes = null;
            Rect[] sobelBboxes = null;

                 Rect[] ahmet = null;
            Rect[] ahmet1 = null;


            List<Rect> allRects = new List<Rect>();

            //Helper.RemovePlateListThreadSafe();

            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {


                HybridWeightHelper.GetAdaptiveHybridWeights_Dinamik_Rev2(grayImage, out bool onlyGray, out bool onlySobel, out double gamma, out double gw, out double sw);

                //HybridWeightHelper.GetAdaptiveHybridWeightsMinibus(grayImage, out double gw, out double sw, out double gamma, out bool onlyGray, out bool onlySobel);
                //HybridWeightHelper.GetAdaptiveHybridWeightsGPTAgırlıklı(grayImage, out bool onlyGray, out bool onlySobel, out double gamma, out double gw, out double sw);
                //HybridWeightHelper.GetAdaptiveHybridWeightsGPTRevizeÇalışsan(grayImage, out bool onlyGray, out bool onlySobel, out double gamma, out double gw, out double sw);


                Mat hybrid = new Mat();
                Mat mserInput;
                string imageType;
                if (onlyGray)
                {
                    //hybrid = grayImage.Clone();
                    //mserInput = grayImage;

                    // gamma uygulanmış versiyon
                    grayImage.ConvertTo(hybrid, MatType.CV_8U, 1, gamma);
                    mserInput = hybrid;
                    imageType = "gray";
                    //Debug.WriteLine("📌 MSER input: Gray");


                    allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(grayImage,
                  SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                }
                else if (onlySobel)
                {
                    //hybrid = sobelImage.Clone();
                    //mserInput = sobelImage;

                    sobelImage.ConvertTo(hybrid, MatType.CV_8U, 1, gamma);
                    mserInput = hybrid;
                    imageType = "sobel";

                    //Debug.WriteLine("📌 MSER input: Sobel");

                    allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                  SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));

                }
                else
                {
                    Cv2.AddWeighted(grayImage, gw, sobelImage, sw, gamma, hybrid);

                    mserInput = hybrid;

                    imageType = "hybrid";
                    //Debug.WriteLine("📌 MSER input: Hybrid");

                    allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(mserInput,
                 SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                }


                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(mserInput));

             

                //mser detection plate
                allRects.AddRange(MSEROperations.FindPlateRegionROI(mserInput, imageType));

            }

            List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesFromROI(allRects.ToArray(), processImage);


            foreach (var item in possiblePlateRegions)
            {
                //if (MainForm.m_mainForm.m_signPlate)
                //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(item.addedRects, processImage);
            }


            if (possiblePlateRegions != null && possiblePlateRegions.Count > 0)
            {
                // Score'u en yüksek olan PossiblePlate'i bul
                //PossiblePlate bestPlate = possiblePlateRegions
                //    .OrderByDescending(p => p.PlateScore)
                //   .First();

                PossiblePlate bestPlate = possiblePlateRegions
                    .Where(p => p != null && p.addedRects.Width > 0 && p.addedRects.Height > 0)
                        .OrderByDescending(p => p.PlateScore)
                        .ThenByDescending(p => p.addedRects.Width * p.addedRects.Height)
                        .FirstOrDefault();



                ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(bestPlate);


                //if (MainForm.m_mainForm.m_signPlate)
                //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);



                Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
            }




        }
    }
}
