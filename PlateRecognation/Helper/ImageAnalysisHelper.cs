using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PlateRecognation.HybridWeightHelper;

namespace PlateRecognation
{
    public class ImageAnalysisHelper
    {

        public static List<PossiblePlate> DetectPlateRegionsROI(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {
                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

                Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
                Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();

                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesFromROI(concatPlatesRect, processImage);
            }
        }

        public static List<PossiblePlate> DetectPlateRegions(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {
                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

                Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
                Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();

                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesAdaptive(concatPlatesRect, processImage);
            }
        }

        public static List<PossiblePlate> DetectPlateRegionsResize(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage,
                SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height)))
            {

                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionROI(grayImage);
                //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionROI(sobelImage);
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage,
                    SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height));
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                    SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height));

                //Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();

                Rect[] concatMSER = MSERGrayBboxes.ToArray();
                Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
                Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();

                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesFromROI(concatPlatesRect, processImage);
            }
        }

        public static List<PossiblePlate> DetectPlateRegionsResizeHybrid(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage,
                SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height)))
            {

                //Mat hybrid = new Mat();


                //(double gw, double sw) = HybridWeightHelper.GetAdaptiveHybridWeights(grayImage);
                //Cv2.AddWeighted(grayImage, gw, sobelImage, sw, 0, hybrid);

                //HybridWeightHelper.GetAdaptiveHybridWeights(grayImage, out double gw, out double sw, out double gamma);
                //Mat hybrid = new Mat();
                //Cv2.AddWeighted(grayImage, gw, sobelImage, sw, gamma, hybrid);

                //HybridWeightHelper.GetAdaptiveHybridWeights(grayImage, out double gw, out double sw, out double gamma, out bool onlyGray, out bool onlySobel);


                HybridWeightHelper.GetAdaptiveHybridWeights34TE0077AA(grayImage, out double gw, out double sw, out double gamma, out bool onlyGray, out bool onlySobel);


                Mat hybrid = new Mat();
                Mat mserInput;

                if (onlyGray)
                {
                    //hybrid = grayImage.Clone();
                    //mserInput = grayImage;

                    // gamma uygulanmış versiyon
                    grayImage.ConvertTo(hybrid, MatType.CV_8U, 1, gamma);
                    mserInput = hybrid;

                    // //Debug.WriteLine("📌 MSER input: Gray");
                }
                else if (onlySobel)
                {
                    //hybrid = sobelImage.Clone();
                    //mserInput = sobelImage;

                    sobelImage.ConvertTo(hybrid, MatType.CV_8U, 1, gamma);
                    mserInput = hybrid;


                    //  //Debug.WriteLine("📌 MSER input: Sobel");

                }
                else
                {
                    Cv2.AddWeighted(grayImage, gw, sobelImage, sw, gamma, hybrid);

                    mserInput = hybrid;
                    ////Debug.WriteLine("📌 MSER input: Hybrid");
                }


                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(hybrid));


                Rect[] MSERHybridBboxes = MSEROperations.FindPlateRegionROI(mserInput);
                //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionROI(sobelImage);
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(mserInput,
                    SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height));
                //Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                //    SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height));

                //Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();

                //Rect[] allRects = MSERHybridBboxes.Concat(MSERSobelBboxes).Concat(graySobelBboxes).ToArray();


                Rect[] allRects = MSERHybridBboxes.Concat(graySobelBboxes).ToArray();



                //Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);
                //Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                //Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

                //Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                //Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
                //Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();



                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesFromROI(allRects, processImage);
            }
        }

        public static List<PossiblePlate> YENİMSERRESIMLIDetectPlateRegionsResizeHybrid(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage,
                SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height)))
            {
                //HybridWeightHelper.GetAdaptiveHybridWeightsvOld1(grayImage, out double gw, out double sw, out double gamma, out bool onlyGray, out bool onlySobel);


                //HybridWeightHelper.GetAdaptiveHybridWeightsGPTRevizeÇalışsan(grayImage, out bool onlyGray, out bool onlySobel, out double gamma, out double gw, out double sw );

                HybridWeightHelper.GetAdaptiveHybridWeights_Dinamik_Rev2(grayImage, out bool onlyGray, out bool onlySobel, out double gamma, out double gw, out double sw);


                Mat hybrid = new Mat();
                Mat mserInput;

                string imageType;

                if (onlyGray)
                {
                    // gamma uygulanmış versiyon
                    grayImage.ConvertTo(hybrid, MatType.CV_8U, 1, gamma);
                    mserInput = hybrid;

                    imageType = "gray";

                    // //Debug.WriteLine("📌 MSER input: Gray");
                }
                else if (onlySobel)
                {
                    sobelImage.ConvertTo(hybrid, MatType.CV_8U, 1, gamma);
                    mserInput = hybrid;

                    imageType = "sobel";

                    // //Debug.WriteLine("📌 MSER input: Sobel");

                }
                else
                {
                    Cv2.AddWeighted(grayImage, gw, sobelImage, sw, gamma, hybrid);

                    mserInput = hybrid;

                    imageType = "hybrid";


                    ////Debug.WriteLine("📌 MSER input: Hybrid");
                }


                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(mserInput));



                //Rect[] MSERHybridBboxes = MSEROperations.FindPlateRegionROI(mserInput);



                Rect[] MSERHybridBboxes = MSEROperations.FindPlateRegionROI(mserInput, imageType);
                //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionROI(sobelImage);


                //Rect[] mserInputBboxes = ClassicalApproach.FindPlateRegionWithSobel4(mserInput,
                //SobelDetectionSettings.GetAdaptiveSobelSettings(sobelImage.Width, sobelImage.Height));

                //Rect[] grayImageBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage,
                //  SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height));

                //Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                //    SobelDetectionSettings.GetAdaptiveSobelSettings(sobelImage.Width, sobelImage.Height));

                //Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();

                //Rect[] allRects = MSERHybridBboxes.Concat(MSERSobelBboxes).Concat(graySobelBboxes).ToArray();


                //Rect[] allRects = MSERHybridBboxes.Concat(mserInputBboxes).Concat(grayImageBboxes).Concat(sobelBboxes).ToArray();


                //burası çalışan
                // Rect[] allRects = MSERHybridBboxes.Concat(mserInputBboxes).Concat(grayImageBboxes).Concat(sobelBboxes).ToArray();

                //var dd = RectComparisonHelper.MergeAndFilterPlateRects(allRects);

                //var looo = mserInputBboxes.Concat(grayImageBboxes).Concat(sobelBboxes).ToArray();


                //var looo = grayImageBboxes.Concat(sobelBboxes).ToArray();


                //var dd = RectComparisonHelper.MergeAndFilterPlateRects(looo);


                Rect[] allRects = MSERHybridBboxes.ToArray();

                


                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesFromROI(allRects, processImage);
            }
        }


        public static List<PossiblePlate> ROIMOTIONSobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage,
                SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height)))
            {
                using (HybridWeightHelper hybridWeightHelper = new HybridWeightHelper())
                {
                    hybridWeightHelper.ComputeWeights(grayImage);


                    List<Rect> allRects = new List<Rect>();
                    Mat mserInput;
                    string imageType;


                    switch (hybridWeightHelper.Mode)
                    {
                        case PlateFusionMode.GrayOnly:
                            {

                                Mat hybrid = new Mat();
                                // gamma uygulanmış versiyon
                                grayImage.ConvertTo(hybrid, MatType.CV_8U, 1, hybridWeightHelper.Gamma);
                                mserInput = hybrid;

                                imageType = "gray";

                                ////Debug.WriteLine("📌 MSER input: Gray");

                                allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(grayImage,
                             SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                                break;
                            }



                        case PlateFusionMode.SobelOnly:
                            {
                                Mat hybrid = new Mat();

                                sobelImage.ConvertTo(hybrid, MatType.CV_8U, 1, hybridWeightHelper.Gamma);
                                mserInput = hybrid;

                                imageType = "sobel";

                                ////Debug.WriteLine("📌 MSER input: Sobel");

                                allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                     SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                                break;
                            }

                        case PlateFusionMode.Hybrid:
                            {
                                Mat hybrid = new Mat();

                                Cv2.AddWeighted(grayImage, hybridWeightHelper.GrayWeight, sobelImage, hybridWeightHelper.SobelWeight, hybridWeightHelper.Gamma, hybrid);

                                mserInput = hybrid;

                                imageType = "hybrid";


                               // //Debug.WriteLine("📌 MSER input: Hybrid");

                                //             allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                                //SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                                break;

                            }

                        default:
                            throw new InvalidOperationException("Unknown fusion mode.");
                    }


                    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(mserInput));


                    //MserDetectionSettings mserDetectionSettings = MserDetectionSettingsFactory.GetPlateRegionSettings(mserInput, imageType);

                    MserDetectionSettings mserDetectionSettings = MserDetectionSettingsFactory.GetPlateRegionSettingsForROI(mserInput, imageType);

                    // MSER Processor oluştur
                    var plateDetector = new MSERProcessor(mserDetectionSettings);



                    //mser detection plate
                    //allRects.AddRange(plateDetector.DetectPlate(mserInput));

                    allRects.AddRange(plateDetector.DetectPlateROI(mserInput));


                    // Olası plaka bölgelerini döndür
                    return Plate.FindPlateCandidatesFromROI(allRects.ToArray(), processImage);
                }

            }
        }


        public static List<PossiblePlate> SobelliYENİMSERRESIMLIDetectPlateRegionsResizeHybrid(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage,
                SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height)))
            {
                using (HybridWeightHelper hybridWeightHelper = new HybridWeightHelper())
                {
                    hybridWeightHelper.ComputeWeights(grayImage);


                    List<Rect> allRects = new List<Rect>();
                    Mat mserInput;
                    string imageType;


                    switch (hybridWeightHelper.Mode)
                    {
                        case PlateFusionMode.GrayOnly:
                            {

                                Mat hybrid = new Mat();
                                // gamma uygulanmış versiyon
                                grayImage.ConvertTo(hybrid, MatType.CV_8U, 1, hybridWeightHelper.Gamma);
                                mserInput = hybrid;

                                imageType = "gray";

                                //  //Debug.WriteLine("📌 MSER input: Gray");

                                allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(grayImage,
                             SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                                break;
                            }



                        case PlateFusionMode.SobelOnly:
                            {
                                Mat hybrid = new Mat();

                                sobelImage.ConvertTo(hybrid, MatType.CV_8U, 1, hybridWeightHelper.Gamma);
                                mserInput = hybrid;

                                imageType = "sobel";

                                ////Debug.WriteLine("📌 MSER input: Sobel");

                                allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                     SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                                break;
                            }

                        case PlateFusionMode.Hybrid:
                            {
                                Mat hybrid = new Mat();

                                Cv2.AddWeighted(grayImage, hybridWeightHelper.GrayWeight, sobelImage, hybridWeightHelper.SobelWeight, hybridWeightHelper.Gamma, hybrid);

                                mserInput = hybrid;

                                imageType = "hybrid";


                                // //Debug.WriteLine("📌 MSER input: Hybrid");

                                //             allRects.AddRange(ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                                //SobelDetectionSettings.GetAdaptiveSobelSettings(mserInput.Width, mserInput.Height)));
                                break;

                            }

                        default:
                            throw new InvalidOperationException("Unknown fusion mode.");
                    }


                    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(mserInput));


                    MserDetectionSettings mserDetectionSettings = MserDetectionSettingsFactory.GetPlateRegionSettings(mserInput, imageType);

                    //MserDetectionSettings mserDetectionSettings = MserDetectionSettingsFactory.GetPlateRegionSettingsForROI(mserInput, imageType);

                    // MSER Processor oluştur
                    var plateDetector = new MSERProcessor(mserDetectionSettings);



                    //mser detection plate
                    allRects.AddRange(plateDetector.DetectPlate(mserInput));

                    //allRects.AddRange(plateDetector.DetectPlateROI(mserInput));


                    // Olası plaka bölgelerini döndür
                    return Plate.FindPlateCandidatesFromROI(allRects.ToArray(), processImage);
                }

            }
        }


        public static List<PossiblePlate> Sulo(Mat processImage)
        {
            Rect[] concatMSER = null;
            Rect[] concatSobel = null;

            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage,
                SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height)))
            {

                //mser detection plate
                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);

                ////edge detection plate with sobel
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage,
                     SobelDetectionSettings.GetAdaptiveSobelSettings(grayImage.Width, grayImage.Height));
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage,
                     SobelDetectionSettings.GetAdaptiveSobelSettings(sobelImage.Width, sobelImage.Height));

                //sonuçları birleştir
                concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();

                Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();




                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesFromROI(concatPlatesRect, processImage);


            }
        }

        public static List<PossiblePlate> DetectPlateRegionsNMS(Mat processImage)
        {
            using (Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage))
            using (Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage))
            {
                Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
                Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);
                Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
                Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

                Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
                Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();
                Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();

                // Olası plaka bölgelerini döndür
                return Plate.FindPlateCandidatesAdaptiveNMS(concatPlatesRect, processImage);
            }
        }



        //public static void ImageAnalysis(Mat mat)
        //{
        //    Mat originalImage = mat;


        //    Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(originalImage);
        //    Rect[] bboxes = MSEROperations.FindPlateRegion(grayImage);

        //    List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPossiblePlateRegion(bboxes, originalImage.Clone());
        //    //List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPlateRegionAdaptive(bboxes, originalImage.Clone());




        //    //PlateDetector.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage.Clone());

        //    Helper.AddPossiblePlateRegionToDataGridView(possiblePlateRegions, false);


        //}

        public static void ImageAnalysis1(object mat)
        {
            ////lock (mat)
            //{

            //    Mat originalImage = (Mat)mat;
            ////{
            //    Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(originalImage);
            //    Rect[] bboxes = MSEROperations.FindPlateRegion(grayImage);

            //    //List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPossiblePlateRegion(bboxes, originalImage.Clone());
            //    List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPlateRegionAdaptive(bboxes, originalImage.Clone());


               



            //    ////Debug.WriteLine(possiblePlateRegions.Count.ToString());

            //    //PlateDetector.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage.Clone());

            //    Helper.AddPossiblePlateRegionToDataGridView(possiblePlateRegions, false);
            //}



        }

        public static List<Rect> GroupCharacterRectsIntoPlateCandidates(List<Rect> characterRects)
        {
            List<List<Rect>> grouped = new List<List<Rect>>();

            foreach (var rect in characterRects)
            {
                bool added = false;

                foreach (var group in grouped)
                {
                    // Aynı satırda (yüksekliği benzeyen) ve yatayda yakın olanları grupla
                    if (AreRectsCloseHorizontally(group, rect))
                    {
                        group.Add(rect);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    grouped.Add(new List<Rect> { rect });
                }
            }

            // Grupları kapsayan minimum dikdörtgenleri çıkar
            List<Rect> plateCandidates = grouped
                .Where(g => g.Count >= 4) // En az 4 karakter olsun (heuristik)
                .Select(g => GetBoundingRectForGroup(g))
                .ToList();

            return plateCandidates;
        }

        private static bool AreRectsCloseHorizontally(List<Rect> group, Rect rect)
        {
            Rect refRect = group[0];

            int verticalTolerance = (int)(refRect.Height * 0.5);
            int horizontalTolerance = (int)(refRect.Width * 3);

            return Math.Abs(refRect.Y - rect.Y) < verticalTolerance &&
                   (Math.Abs(refRect.X - rect.X) < horizontalTolerance ||
                    Math.Abs((refRect.X + refRect.Width) - rect.X) < horizontalTolerance);
        }

        private static Rect GetBoundingRectForGroup(List<Rect> group)
        {
            int minX = group.Min(r => r.X);
            int minY = group.Min(r => r.Y);
            int maxX = group.Max(r => r.X + r.Width);
            int maxY = group.Max(r => r.Y + r.Height);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static void ImageAnalysis2(object mat)
        {
            //Mat originalImage = (Mat)mat;

            //Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(originalImage);
            //Rect[] bboxes = MSEROperations.FindPlateRegion(grayImage);

            ////List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPossiblePlateRegion(bboxes, originalImage.Clone());
            //List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPlateRegionAdaptive(bboxes, originalImage.Clone());


            //ThreadSafeList<CharacterSegmentationResult> possibleCharacters = PlateCharacterFinder.SegmentCharactersInPossiblePlateRegion(possiblePlateRegions);

            //if (MainForm.m_mainForm.m_checkBoxSignPlate.Checked)
            //    PlateDetector.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage.Clone());

            //Helper.RecognizeAndDisplayPlateResults1(possibleCharacters);
        }

        public static List<PossiblePlate> RemoveDuplicatePlates(List<PossiblePlate> possiblePlateRegions)
        {
            List<PossiblePlate> uniquePlates = new List<PossiblePlate>();

            foreach (var plate in possiblePlateRegions)
            {
                bool isDuplicate = uniquePlates.Any(existingPlate =>
                    RectComparisonHelper.AreRectsSimilar(existingPlate.addedRects, plate.addedRects));

                if (!isDuplicate)
                {
                    uniquePlates.Add(plate);
                }
            }

            return uniquePlates.OrderBy(p => p.addedRects.X).ToList(); 
        }


        public static void TestAdaptiveMSER(Mat inputGrayImage)
        {
            List<(int delta, int minArea, double maxVariation)> paramCombos = new List<(int, int, double)>
    {
        (5, 60, 0.4),
        (5, 80, 0.5),
        (5, 100, 0.6),
        (4, 80, 0.65),
        (6, 70, 0.55)
    };

            int testId = 1;

            foreach (var (delta, minArea, maxVariation) in paramCombos)
            {
                var mser = MSER.Create(
                    delta: delta,
                    minArea: minArea,
                    maxArea: 4000,
                    maxVariation: maxVariation,
                    minDiversity: 0.2,
                    maxEvolution: 200,
                    areaThreshold: 0.8,
                    minMargin: 0.3,
                    edgeBlurSize: 3
                );

                OpenCvSharp.Point[][] msers;
                OpenCvSharp.Rect[] bboxes;
                mser.DetectRegions(inputGrayImage, out msers, out bboxes);

                // Görselleştirme
                Mat clone = inputGrayImage.CvtColor(ColorConversionCodes.GRAY2BGR);

                foreach (var rect in bboxes)
                {
                    Cv2.Rectangle(clone, rect, Scalar.RandomColor(), 2);
                }

                //string label = $"Test #{testId} - Δ:{delta} | minA:{minArea} | maxVar:{maxVariation}";
                //Cv2.PutText(clone, label, new OpenCvSharp.Point(10, 25), HersheyFonts.HersheySimplex, 0.7, Scalar.Yellow, 2);

                // Görüntüyü göster
                string winName = $"MSER Test {testId}";
                Cv2.ImShow(winName, clone);

                testId++;
            }

            Cv2.WaitKey(0); // Testleri görsel olarak değerlendirmen için bekler
            Cv2.DestroyAllWindows();
        }

        public static void ImageAnalysisVoitingSobelliKombine(object mat)
        {
            Mat originalImage = (Mat)mat;



            Cv2.Resize(originalImage, originalImage, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut



            //Helper.RemovePlateListThreadSafe();

            Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(originalImage);
            Mat binaryImage = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(originalImage);

            //Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayImage);

            //Mat grayImage = new Mat();
            //TestAdaptiveMSER(grayImage);


            //mser detection plate
            //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(binaryImage);
            Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);

            //edge detection plate with sobel
            //Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel2(grayImage);
            //Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel2(sobelEdges);


            #region ikisi tek filtre

            //Rect[] concatlooo = MSERGrayBboxes.Concat(graySobelBboxes).ToArray();


            //concat all bboxes
            //Rect[] concatMSERPlates = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
            //Rect[] concatEdgePlates = graySobelBboxes.Concat(sobelBboxes).ToArray();

            //Rect[] concatPlatesRect = concatMSERPlates.Concat(concatEdgePlates).ToArray();

            //Rect[] validatedCandidates = ValidationHelper.FilterValidPlates(concatPlatesRect, grayImage, binaryImage);
            #endregion

            #region ayrı ayrı filtre

            //concat all bboxes
            //Rect[] concatMSERPlates = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
            //Rect[] validatedconcatMSERPlates = ValidationHelper.FilterValidPlates(concatMSERPlates, grayImage, binaryImage);

            //Rect[] concatEdgePlates = graySobelBboxes.Concat(sobelBboxes).ToArray();
            //Rect[] validatedConcatEdgePlates = ValidationHelper.FilterValidPlates(concatEdgePlates, grayImage, binaryImage);


            //Rect[] concatPlatesRect = validatedconcatMSERPlates.Concat(validatedConcatEdgePlates).ToArray();


            //Rect[] validatedCandidates = ValidationHelper.FilterValidPlates(concatPlatesRect, grayImage, binaryImage);

            #endregion

            //var sdfsdf = GroupCharacterRectsIntoPlateCandidates(MSERGrayBboxes.ToList());
            List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(MSERGrayBboxes, originalImage);



            //List<PossiblePlate> loso = RemoveDuplicatePlates(possiblePlateRegions);

            ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);

            //if (MainForm.m_mainForm.m_signPlate)
            //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage);



            Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
            //PlateResult plateResult =
            ////PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults(possibleCharacters);

            //if (!string.IsNullOrEmpty(plateResult.readingPlateResult))
            //{
            //    MainForm.m_mainForm.m_plateResults.Add(plateResult);
            //}
        }



        //public static void ImageAnalysisPreProcess(object mat)
        //{
        //    Mat originalImage = (Mat)mat;

        //    Cv2.Resize(originalImage, originalImage, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

        //    Mat processImage = FrameProcessingHelper.NewProcessFrame(originalImage, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance); 

        //    Helper.RemovePlateListThreadSafe();

        //    Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage);
        //    Mat binaryImage = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(processImage);

        //    Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayImage);


        //    //mser detection plate
        //    //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(binaryImage);
        //    Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);

        //    //edge detection plate with sobel
        //    Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelEdges);
        //    //Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel2(sobelEdges);

        //    Rect[] concatPlatesRect = MSERGrayBboxes.Concat(graySobelBboxes).ToArray();
        //    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(sobelEdges));


        //    //var sdfsdf = GroupCharacterRectsIntoPlateCandidates(MSERGrayBboxes.ToList());
        //    //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage, processImage);
        //    List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage);
        //    //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, processImage);



        //    //List<PossiblePlate> loso = RemoveDuplicatePlates(possiblePlateRegions);

        //    ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);

        //    if (MainForm.m_mainForm.m_signPlate)
        //        PlateHelper.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage);



        //    Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
        //    //PlateResult plateResult =
        //    ////PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults(possibleCharacters);

        //    //if (!string.IsNullOrEmpty(plateResult.readingPlateResult))
        //    //{
        //    //    MainForm.m_mainForm.m_plateResults.Add(plateResult);
        //    //}
        //}
        //public static void DetectPlates_MSERonGrayAndEdges_WithSobelContours(object mat)
        //{
        //    Mat originalImage = (Mat)mat;

        //    Cv2.Resize(originalImage, originalImage, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

        //    Mat processImage = FrameProcessingHelper.NewProcessFrame(originalImage, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance);

        //    Helper.RemovePlateListThreadSafe();

        //    Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage);
        //    //Mat binaryImage = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(processImage);

        //    Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayImage);


        //    //mser detection plate
        //    Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
        //    Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelEdges);


        //    ////edge detection plate with sobel
        //    //Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
        //    Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelEdges);


        //    Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
        //    //Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();

        //    Rect[] concatPlatesRect = concatMSER.Concat(sobelBboxes).ToArray();


        //    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(sobelEdges));


        //    //var sdfsdf = GroupCharacterRectsIntoPlateCandidates(MSERGrayBboxes.ToList());
        //    //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage, processImage);
        //    List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage);
        //    //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, processImage);



        //    //List<PossiblePlate> loso = RemoveDuplicatePlates(possiblePlateRegions);

        //    ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);

        //    if (MainForm.m_mainForm.m_signPlate)
        //        PlateHelper.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage);



        //    Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
        //    //PlateResult plateResult =
        //    ////PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults(possibleCharacters);

        //    //if (!string.IsNullOrEmpty(plateResult.readingPlateResult))
        //    //{
        //    //    MainForm.m_mainForm.m_plateResults.Add(plateResult);
        //    //}
        //}


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


            List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, processImage);


            ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);

            //if (MainForm.m_mainForm.m_signPlate)
            //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, processImage);



            Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);



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


        public static void DetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages(object mat)
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


            ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);



            ////ThreadSafeList<PlateResult> ts =  Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings.m_PlateType);
            ThreadSafeList<PlateResult> ts = Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);


            if (ts.Count > 0)
            {
                //PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidatesWithCorrection(ts);

                //listedeki bütün plakalar aynı iste burası
                PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(ts);

                if (bestPlate != null)
                {
                    ////Debug.WriteLine($"Seçilen plaka: {bestPlate.readingPlateResult} - Güven: {bestPlate.readingPlateResultProbability:F2}");

                    MainForm.m_mainForm.m_plateResults.Add(bestPlate);

                    //if (MainForm.m_mainForm.m_signPlate)
                    //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);

                }
                else
                {
                 
                    //buranında kontroledilmesi lazım
                    var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(ts);

                    Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;

                    List<PlateResult> bestPlates = new List<PlateResult>();

                    if (plateType == Enums.PlateType.Turkish)
                        bestPlates = PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity);
                    else
                    {
                        foreach (List<PlateResult> item in groupPlatesByProximity)
                        {
                            bestPlates.AddRange(item);
                        }


                    }

                    foreach (PlateResult besties in bestPlates)
                    {
                        MainForm.m_mainForm.m_plateResults.Add(besties);

                        //if (MainForm.m_mainForm.m_signPlate)
                        //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(besties.addedRects, processImage);
                    }

                }
            }

        }




       


        public static void OcrPlatesFromQueue(PossiblePlate plate)
        {
            ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(plate);

            ////ThreadSafeList<PlateResult> ts =  Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings.m_PlateType);
            ThreadSafeList<PlateResult> ts = Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);


            if (ts.Count > 0)
            {
                //PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidatesWithCorrection(ts);

                //listedeki bütün plakalar aynı iste burası
                PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(ts);

                if (bestPlate != null)
                {
                    // //Debug.WriteLine($"Seçilen plaka: {bestPlate.readingPlateResult} - Güven: {bestPlate.readingPlateResultProbability:F2}");

                    MainForm.m_mainForm.m_plateResults.Add(bestPlate);

                    //if (MainForm.m_mainForm.m_signPlate)
                    //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);

                }
                else
                {

                    //buranında kontroledilmesi lazım
                    var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(ts);

                    Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;

                    List<PlateResult> bestPlates = new List<PlateResult>();

                    if (plateType == Enums.PlateType.Turkish)
                        bestPlates = PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity);
                    else
                    {
                        foreach (List<PlateResult> item in groupPlatesByProximity)
                        {
                            bestPlates.AddRange(item);
                        }


                    }

                    foreach (PlateResult besties in bestPlates)
                    {
                        MainForm.m_mainForm.m_plateResults.Add(besties);

                        //if (MainForm.m_mainForm.m_signPlate)
                        //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(besties.addedRects, processImage);
                    }

                }
            }


        }

        public static void OcrPlatesFromQueue(List<PossiblePlate> plate)
        {

            ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(plate);



            ////ThreadSafeList<PlateResult> ts =  Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings.m_PlateType);
            ThreadSafeList<PlateResult> ts = Helper.RecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);


            if (ts.Count > 0)
            {
                //PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidatesWithCorrection(ts);

                //listedeki bütün plakalar aynı iste burası
                PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(ts);

                if (bestPlate != null)
                {
                    //  //Debug.WriteLine($"Seçilen plaka: {bestPlate.readingPlateResult} - Güven: {bestPlate.readingPlateResultProbability:F2}");

                    MainForm.m_mainForm.m_plateResults.Add(bestPlate);

                    //if (MainForm.m_mainForm.m_signPlate)
                    //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(bestPlate.addedRects, processImage);

                }
                else
                {

                    //buranında kontroledilmesi lazım
                    var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(ts);

                    Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;

                    List<PlateResult> bestPlates = new List<PlateResult>();

                    if (plateType == Enums.PlateType.Turkish)
                        bestPlates = PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity);
                    else
                    {
                        foreach (List<PlateResult> item in groupPlatesByProximity)
                        {
                            bestPlates.AddRange(item);
                        }


                    }

                    foreach (PlateResult besties in bestPlates)
                    {
                        MainForm.m_mainForm.m_plateResults.Add(besties);

                        //if (MainForm.m_mainForm.m_signPlate)
                        //    PlateHelper.DrawPossiblePlateRegionToOriginalImage(besties.addedRects, processImage);
                    }

                }
            }


        }













      //  public static void DetectPlatesUsingMSERAndSobelOverGrayAndEdgeImagesPlakaAdayPuanlamalı(object mat)
      //  {
      //      Mat originalImage = (Mat)mat;

      //      Cv2.Resize(originalImage, originalImage, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

      //      Mat processImage = FrameProcessingHelper.NewProcessFrame(originalImage, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance);

      //      Helper.RemovePlateListThreadSafe();

      //      Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage);
      //      Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage);


      //      //mser detection plate
      //      Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);
      //      Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);

      //      ////edge detection plate with sobel
      //      Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(grayImage);
      //      Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

      //      //sonuçları birleştir
      //      Rect[] concatMSER = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
      //      Rect[] concatSobel = graySobelBboxes.Concat(sobelBboxes).ToArray();

      //      Rect[] concatPlatesRect = concatMSER.Concat(concatSobel).ToArray();

      //      //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage, processImage);
      //      List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage);

      //      //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(sobelImage));


      //      if (possiblePlateRegions.Count > 0)
      //      {
      //          //var bestPlate = possiblePlateRegions
      //          //    .Where(p => p.PlateScore >= 0.5)
      //          //    .OrderByDescending(p => p.PlateScore)
      //          //    .FirstOrDefault();

      //          var bestPlate = possiblePlateRegions
      //.OrderByDescending(p => p.PlateScore)
      //.FirstOrDefault();


      //          if (bestPlate != null)
      //          {
      //              List<PossiblePlate> selectedPlates = new List<PossiblePlate> { bestPlate };

      //              var possibleCharacters = Character.SegmentCharactersInPlate(bestPlate);

      //              if (MainForm.m_mainForm.m_signPlate)
      //                  PlateHelper.DrawPossiblePlateRegionToOriginalImage(selectedPlates, originalImage);

      //              Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
      //          }
      //      }


      //  }

      //  public static void DetectPlatesUsingSobel(object mat)
      //  {
      //      Mat originalImage = (Mat)mat;

      //      Cv2.Resize(originalImage, originalImage, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

      //      Mat processImage = FrameProcessingHelper.NewProcessFrame(originalImage, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance);

      //      Helper.RemovePlateListThreadSafe();

      //      Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(processImage);
      //      Mat sobelImage = ImageEnhancementHelper.ComputeSobelEdges(grayImage); 

      //      //mser detection plate
      //      Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(sobelImage);


      //      ////edge detection plate with sobel
      //      Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelImage);

      //      //sonuçları birleştir
      //      Rect[] concatMSER = MSERSobelBboxes.Concat(sobelBboxes).ToArray();
         



      //      //var sdfsdf = GroupCharacterRectsIntoPlateCandidates(MSERGrayBboxes.ToList());
      //      //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage, processImage);
      //      List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatMSER, originalImage);
      //      //List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, processImage);

      //      //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(sobelImage));

      //      //List<PossiblePlate> loso = RemoveDuplicatePlates(possiblePlateRegions);

      //      ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);

      //      if (MainForm.m_mainForm.m_signPlate)
      //          PlateHelper.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage);



      //      Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
      //      //PlateResult plateResult =
      //      ////PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults(possibleCharacters);

      //      //if (!string.IsNullOrEmpty(plateResult.readingPlateResult))
      //      //{
      //      //    MainForm.m_mainForm.m_plateResults.Add(plateResult);
      //      //}
      //  }


        public static Mat CombineGrayAndSobelForMSER(Mat grayImage, Mat sobelEdges, double grayWeight = 0.7, double sobelWeight = 0.3)
        {
            // 1. Sobel çıktısını normalize et (min-max 0-255 aralığına)
            Mat normalizedSobel = new Mat();
            Cv2.Normalize(sobelEdges, normalizedSobel, 0, 255, NormTypes.MinMax, MatType.CV_8U);

            // 2. (Opsiyonel) Sobel’e biraz blur ekleyerek gürültüyü yumuşat (çerçeve çizgileri bozulmaz)
            Mat blurredSobel = new Mat();
            Cv2.GaussianBlur(normalizedSobel, blurredSobel, new OpenCvSharp.Size(3, 3), 0);

            // 3. AddWeighted ile füzyon görüntüsü oluştur
            Mat fused = new Mat();
            Cv2.AddWeighted(grayImage, grayWeight, blurredSobel, sobelWeight, 0, fused);

            return fused;
        }

        //public static void ImageAnalysisVoiting(object mat)
        //{
        //    Mat originalImage = (Mat)mat;

        //    Mat processImage = FrameProcessingHelper.NewProcessFrame(originalImage, MainForm.m_mainForm.m_preProcessingSettings.m_AutoLightControl, MainForm.m_mainForm.m_preProcessingSettings.m_AutoWhiteBalance);

        //    Cv2.Resize(originalImage, originalImage, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4); // HOG ile uyumlu boyut

        //    Helper.RemovePlateListThreadSafe();

        //    Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(originalImage);
        //    Mat binaryImage = ImagePreProcessingHelper.ColorMatToAdaptiveThreshold(originalImage);

        //    Mat sobelEdges = ImageEnhancementHelper.ComputeSobelEdges(grayImage);

        //    //Mat grayImage = new Mat();
        //    //TestAdaptiveMSER(grayImage);


        //    //mser detection plate
        //    //Rect[] MSERSobelBboxes = MSEROperations.FindPlateRegionv2(binaryImage);
        //    Rect[] MSERGrayBboxes = MSEROperations.FindPlateRegionv2(grayImage);

        //    //edge detection plate with sobel
        //    Rect[] graySobelBboxes = ClassicalApproach.FindPlateRegionWithSobel4(sobelEdges);
        //    //Rect[] sobelBboxes = ClassicalApproach.FindPlateRegionWithSobel2(sobelEdges);

        //    Rect[] concatPlatesRect = MSERGrayBboxes.Concat(graySobelBboxes).ToArray();
        //    //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox8, BitmapConverter.ToBitmap(sobelEdges));

        //    #region ikisi tek filtre

        //    //Rect[] concatlooo = MSERGrayBboxes.Concat(graySobelBboxes).ToArray();


        //    //concat all bboxes
        //    //Rect[] concatMSERPlates = MSERGrayBboxes.Concat(MSERSobelBboxes).ToArray();
        //    //Rect[] concatEdgePlates = graySobelBboxes.Concat(sobelBboxes).ToArray();

        //    //Rect[] concatPlatesRect = concatMSERPlates.Concat(concatEdgePlates).ToArray();

        //    //Rect[] validatedCandidates = ValidationHelper.FilterValidPlates(concatPlatesRect, grayImage, binaryImage);
        //    #endregion



        //    //var sdfsdf = GroupCharacterRectsIntoPlateCandidates(MSERGrayBboxes.ToList());
        //    List<PossiblePlate> possiblePlateRegions = Plate.FindPlateCandidatesAdaptive(concatPlatesRect, originalImage);



        //    //List<PossiblePlate> loso = RemoveDuplicatePlates(possiblePlateRegions);

        //    ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(possiblePlateRegions);

        //    if (MainForm.m_mainForm.m_signPlate)
        //        PlateHelper.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage);



        //    Helper.RecognizeAndDisplayPlateResultsİlkVersiyon(possibleCharacters);
        //    //PlateResult plateResult =
        //    ////PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults(possibleCharacters);

        //    //if (!string.IsNullOrEmpty(plateResult.readingPlateResult))
        //    //{
        //    //    MainForm.m_mainForm.m_plateResults.Add(plateResult);
        //    //}
        //}
        //public static void ImageAnalysisVoiting(object mat)
        //{
        //    Mat originalImage = (Mat)mat;


        //    //Helper.RemovePlateListThreadSafe();


        //    Mat grayImage = ImagePreProcessingHelper.ColorMatToGray(originalImage);
        //    var bboxes = MSEROperations.FindPlateRegionsWithMorphology(originalImage);



        //    //List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPossiblePlateRegion(bboxes, originalImage.Clone());
        //    //List<PossiblePlate> possiblePlateRegions = PlateDetector.FindPlateRegionAdaptive(bboxes, originalImage.Clone());


        //    //ThreadSafeList<CharacterSegmentationResult> possibleCharacters = PlateCharacterFinder.SegmentCharactersInPossiblePlateRegion(possiblePlateRegions);

        //    //if (MainForm.m_mainForm.m_signPlate)
        //    //    PlateDetector.DrawPossiblePlateRegionToOriginalImage(possiblePlateRegions, originalImage.Clone());

        //    ////PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults1(possibleCharacters);
        //    //PlateResult plateResult = Helper.RecognizeAndDisplayPlateResults(possibleCharacters);


        //    //if (!string.IsNullOrEmpty(plateResult.readingPlateResult))
        //    //{
        //    //    MainForm.m_mainForm.m_plateResults.Add(plateResult);


        //    //}

        //}

    }
}
