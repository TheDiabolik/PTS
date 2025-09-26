using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PlateRecognation
{
    public class Helper
    {
        public static ThreadSafeList<PlateResult> m_lastFivePlate = new ThreadSafeList<PlateResult>();


        public static void LoadImage()
        {
            PreProcessingSettings m_preProcessing = PreProcessingSettings.Singleton();
            m_preProcessing = m_preProcessing.DeSerialize(m_preProcessing);


            MainForm.m_mainForm.m_listBoxPath.Items.Clear();

            DirectoryInfo di = new DirectoryInfo(m_preProcessing.m_ReadPlateFromImagePath);
            FileInfo[] fi = di.GetFiles();

            foreach (FileInfo fileinfo in fi)
            {
                //if (Path.GetExtension(fileinfo.Name) == ".jpg")
                MainForm.m_mainForm.m_listBoxPath.Items.Add(fileinfo.Name);
            }
        }

        public static void AddPossiblePlateRegionToDataGridView(List<Mat> possiblePlateRegions)
        {
            MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows.Clear();

            foreach (Mat item in possiblePlateRegions)
            {
                int newRow = MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows.Add();

                MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[newRow].Cells[0].Value = BitmapConverter.ToBitmap(item);


            }

        }



        public void ShowImage(Bitmap plate, Bitmap segmented, Bitmap threshould)
        {
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxPossiblePlateRegion, plate);
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented, segmented);
            //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxPlateThreshould, threshould);
        }

        public static void AddPossiblePlateRegionToDataGridView(List<PossiblePlate> possibleRegions)
        {
            foreach (PossiblePlate possibleRegion in possibleRegions)
            {
                Mat plate = possibleRegion.possiblePlateRegions;

                CharacterSegmentationResult segmentedPlate = PlateCharacterFinder.FindCharacterInPlateRegion(plate);

                List<Mat> characters = segmentedPlate.threshouldPossibleCharacters;
                string plateToString=""; //= OCRHelper.SVMModalOCRPrediction(characters);
                //string plateToString = OCRHelper.KNNModalOCRPrediction(characters);

                if (!string.IsNullOrEmpty(plateToString))
                {
                    int newRowIndex = DisplayManager.DataGridViewAddRowInvoke(MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions);

                    MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[newRowIndex].Cells[0].Value = BitmapConverter.ToBitmap(plate);
                    MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[newRowIndex].Cells[1].Value = BitmapConverter.ToBitmap(segmentedPlate.segmentedPlate);
                    MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[newRowIndex].Cells[2].Value = BitmapConverter.ToBitmap(segmentedPlate.thresh);

                    MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[newRowIndex].Cells[3].Value = plateToString;


                    DisplayManager.DataGridViewFirstDisplayedScrollingRowIndexInvoke(MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions, newRowIndex);
                    MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[newRowIndex].Selected = true;
                }
            }
        }


        private static readonly object osman = new object();
        public static void AddPossiblePlateRegionToDataGridView(List<PossiblePlate> possibleRegions, bool cleanDataGrid)
        {
            lock (osman)
            {



                if (cleanDataGrid)
                    DisplayManager.DataGridViewRowClearInvoke(MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions);


                ////MainForm.m_mainForm.m_characters.Clear();


                foreach (PossiblePlate possibleRegion in possibleRegions)
                {
                    Mat colorPlate = possibleRegion.possiblePlateRegions;


                    //vertical denemesi için çalışan kısım commentlendi
                    //SegmentedCharacter segmentedPlate = Character.FindCharacterInPlateGrayForRectRegion(plate);

                    //CharacterSegmentationResult segmentedPlate = PlateCharacterFinder.FindCharacterInPlateRegion(colorPlate);
                    CharacterSegmentationResult segmentedPlate = PlateCharacterFinder.SegmentCharactersVertically(colorPlate);
                    //SegmentedCharacter segmentedPlate = Character.FindCharacterInPlateGrayWithContourForRectRegion(plate);




                    List<Mat> characters = segmentedPlate.threshouldPossibleCharacters;


                    //karakter bölgeleri 5 ten fazla 9 dan küçük olması lazım ön filtre
                    if ((characters.Count > 5) && (characters.Count < 9))
                    {
                        //string plateToString = OCRHelper.SVMModalOCRPrediction(characters);

                      

                        var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

                        //var readingResult = await OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2Async(characters, 60, 2);

                        string plateToString = readingResult.plateKara;
                        double readingProbability = readingResult.confidence;
                        List<Mat> newCharacters = readingResult.characterMat;

                        //string plateToString = OCRHelper.KNNModalOCRPrediction(characters);

                        //if (((!string.IsNullOrEmpty(plateToString)) && (plateToString.Count() >= 7)) && (OCRHelper.Plate(plateToString.ToCharArray())))
                        if (!string.IsNullOrEmpty(plateToString))

                        {
                            Bitmap bitmaoPlate = BitmapConverter.ToBitmap(colorPlate);
                            Bitmap segmented = BitmapConverter.ToBitmap(segmentedPlate.segmentedPlate);
                            Bitmap threshould = BitmapConverter.ToBitmap(segmentedPlate.thresh);

                            MainForm.m_mainForm.m_plateResults.Add(new PlateResult()
                            {
                                plate = bitmaoPlate,
                                segmented = segmented,
                                threshould = threshould,
                                readingPlateResult = plateToString,
                                readingPlateResultProbability = readingProbability,
                                m_characters = segmentedPlate.threshouldPossibleCharacters
                            });



                            MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions_CellClick(null, null);
                        }

                    }

                   
                    
                }

            }
        }


        public static void RecognizeAndDisplayPlateResultsİlkVersiyon(List<CharacterSegmentationResult> possibleCharacters)
        {
            lock (osman)
            {
                foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
                {
                    List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;


                   

                    var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

                    //var readingResult = await OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2Async(characters, 60, 2);

                    string plateToString = readingResult.plateKara;
                    double readingProbability = readingResult.confidence;
                    List<Mat> newCharacters = readingResult.characterMat;

                    //string plateToString = OCRHelper.KNNModalOCRPrediction(characters);

                    //if (((!string.IsNullOrEmpty(plateToString)) && (plateToString.Count() >= 7)) && (OCRHelper.Plate(plateToString.ToCharArray())))
                    if (!string.IsNullOrEmpty(plateToString))

                    {
                        Bitmap colorPlate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate);
                        Bitmap segmentedPlate = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate);
                        Bitmap threshouldPlate = BitmapConverter.ToBitmap(possibleCharacter.thresh);

                        MainForm.m_mainForm.m_plateResults.Add(new PlateResult()
                        {
                            plate = colorPlate,
                            segmented = segmentedPlate,
                            threshould = threshouldPlate,
                            readingPlateResult = plateToString,
                            readingPlateResultProbability = readingProbability,
                            m_characters = possibleCharacter.threshouldPossibleCharacters
                        });



                        //MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions_CellClick(null, null);
                    }
                }

            }
        }

        public static void RecognizeAndDisplayPlateResultsİlkVersiyon(ThreadSafeList<CharacterSegmentationResult> possibleCharacters)
        {
            lock (osman)
            {
                foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
                {
                    List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;




                    var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

                    //var readingResult = await OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2Async(characters, 60, 2);

                    string plateToString = readingResult.plateKara;
                    double readingProbability = readingResult.confidence;
                    List<Mat> newCharacters = readingResult.characterMat;

                    //string plateToString = OCRHelper.KNNModalOCRPrediction(characters);

                    //if (((!string.IsNullOrEmpty(plateToString)) && (plateToString.Count() >= 7)) && (OCRHelper.Plate(plateToString.ToCharArray())))
                   

                    if (string.IsNullOrEmpty(plateToString))
                        continue;

                   

                    Bitmap colorPlate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate);
                    Bitmap segmentedPlate = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate);
                    Bitmap threshouldPlate = BitmapConverter.ToBitmap(possibleCharacter.thresh);

                    MainForm.m_mainForm.m_plateResults.Add(new PlateResult()
                    {
                        plate = colorPlate,
                        segmented = segmentedPlate,
                        threshould = threshouldPlate,
                        readingPlateResult = plateToString,
                        readingPlateResultProbability = readingProbability,
                        m_characters = possibleCharacter.threshouldPossibleCharacters
                    });



                    //MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions_CellClick(null, null);

                }

            }
        }


        public static ThreadSafeList<PlateResult> RecognizeAndDisplayPlateResultsListeDöner(ThreadSafeList<CharacterSegmentationResult> possibleCharacters)
        {
            lock (osman)
            {
                ThreadSafeList<PlateResult> allPlateResult = new ThreadSafeList<PlateResult>();

                foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
                {
                    List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;


                    Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;

                    var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

                    //var readingResult = await OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2Async(characters, 60, 2);

                    string plateToString = readingResult.plateKara;
                    double readingProbability = readingResult.confidence;
                    List<Mat> newCharacters = readingResult.characterMat;

                    if (string.IsNullOrEmpty(plateToString))
                        continue;

                    Bitmap colorPlate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate);
                    Bitmap segmentedPlate = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate);
                    Bitmap threshouldPlate = BitmapConverter.ToBitmap(possibleCharacter.thresh);


                    PlateResult plateResult = new PlateResult(){
                        plate = colorPlate,
                        segmented = segmentedPlate,
                        threshould = threshouldPlate,
                        addedRects = possibleCharacter.plateLocation,
                        readingPlateResult = plateToString,
                        readingPlateResultProbability = readingProbability,
                        m_characters = possibleCharacter.threshouldPossibleCharacters
                    };

                    allPlateResult.Add(plateResult);

                    //çoklu sonuçları göstermek istersek comment kaldırılıp test yapılabilir
                    //MainForm.m_mainForm.m_plateResults.Add(plateResult);

                    //MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions_CellClick(null, null);
                }

                return allPlateResult;
            }
        }



        public static ThreadSafeList<PlateResult> RecognizeAndDisplayPlateResultsListeDöner(ThreadSafeList<CharacterSegmentationResult> possibleCharacters, Enums.PlateType plateType)
        {
            lock (osman)
            {
                ThreadSafeList<PlateResult> allPlateResults = new ThreadSafeList<PlateResult>();

                foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
                {
                    List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;

                    var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

                    string plateToString = readingResult.plateKara;
                    double readingProbability = readingResult.confidence;
                    var characterScores = readingResult.Item4; // TupleList<string, double>
                    List<Mat> newCharacters = readingResult.characterMat;

                    if (string.IsNullOrEmpty(plateToString))
                        continue;

                    // 🔍 Eğer sadece Türk plakaları isteniyorsa
                    if (plateType == Enums.PlateType.Turkish)
                    {
                        string cleanedPlate = PlateHelper.ExtractProbableTurkishPlate(plateToString);
                        if (string.IsNullOrEmpty(cleanedPlate))
                            continue;

                        // ✔ Karakterleri eşleştir ve yeniden skorla
                        var cleanedCharScores = characterScores
                            .Where(x => cleanedPlate.Contains(x.Item1))
                            .Select(x => x.Item2)
                            .ToList();

                        if (cleanedCharScores.Count == 0)
                            continue;

                        readingProbability = cleanedCharScores.Average();
                        plateToString = cleanedPlate;
                    }

                    // 📦 PlateResult objesi oluştur
                    PlateResult plateResult = new PlateResult()
                    {
                        plate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate),
                        segmented = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate),
                        threshould = BitmapConverter.ToBitmap(possibleCharacter.thresh),
                        addedRects = possibleCharacter.plateLocation,
                        readingPlateResult = plateToString,
                        readingPlateResultProbability = readingProbability,
                        m_characters = characters
                    };

                    allPlateResults.Add(plateResult);
                }

                return allPlateResults;
            }
        }

        public static ThreadSafeList<PlateResult> RecognizeAndDisplayPlateResultsListeDöner(ThreadSafeList<CharacterSegmentationResult> possibleCharacters, PreProcessingSettings preProcessingSettings)
        {
            //lock (osman)
            {
                ThreadSafeList<PlateResult> allPlateResults = new ThreadSafeList<PlateResult>();

                foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
                {
                    List<Mat> filteredCharacters = new List<Mat>();

                    List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;

                    var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

                    string plateToString = readingResult.plateKara;
                    double readingProbability = readingResult.confidence;
                    var characterScores = readingResult.Item4; // TupleList<string, double>
                    List<Mat> newCharacters = readingResult.characterMat;

                    if (string.IsNullOrEmpty(plateToString))
                        continue;


                    ////Debug.WriteLine("Okunan Plaka : " + plateToString);

                    // Öncelikle Türk plaka formatı kontrolü yapılır (her durumda)
                    bool isTurkishPlate = PlateFormatHelper.IsProbablyTurkishPlate(plateToString);

                    // 🔍 Eğer sadece Türk plakaları isteniyorsa
                    if (preProcessingSettings.m_PlateType == Enums.PlateType.Turkish)
                    {
                        //üretilen ocr sonuçlarından direk Türk formatına uygun olanlar seçilsin
                        if (!preProcessingSettings.m_FixOCRErrors)
                        {
                            if (!isTurkishPlate)
                                continue;

                            filteredCharacters.AddRange(newCharacters);

                        }
                        else//Türk plaka formatı isteniyor ve çıkan sonuçlar düzeltme yapılsın
                        {
                            string cleanedPlate = PlateHelper.ExtractProbableTurkishPlate(plateToString);

                            if (string.IsNullOrEmpty(cleanedPlate))
                                continue;

                            // Karakter eşleştirerek yeniden skor hesapla
                            TupleList<string, double> remainingTuples = new TupleList<string, double>();
                            remainingTuples.AddRange(characterScores);

                            List<double> matchedScores = new List<double>();

                            foreach (char c in cleanedPlate)
                            {
                                int index = remainingTuples.FindIndex(x => x.Item1 == c.ToString());

                                if (index >= 0)
                                {
                                    matchedScores.Add(remainingTuples[index].Item2);
                                    remainingTuples.RemoveAt(index);
                                    filteredCharacters.Add(newCharacters[index]);
                                }
                            }

                            if (matchedScores.Count == 0)
                                continue;

                            readingProbability = matchedScores.Average();
                            plateToString = cleanedPlate;
                        }

                    }
                    //bütün formatlar seçili ise
                    else
                    {
                        // Uymuyorsa ve düzeltme aktifse düzeltmeyi dene
                        string cleanedPlate = PlateHelper.ExtractProbableTurkishPlate(plateToString);

                        if (!string.IsNullOrEmpty(cleanedPlate))
                        {
                            TupleList<string, double> remainingTuples = new TupleList<string, double>();
                            remainingTuples.AddRange(characterScores);

                            List<double> matchedScores = new List<double>();

                            foreach (char c in cleanedPlate)
                            {
                                int index = remainingTuples.FindIndex(x => x.Item1 == c.ToString());
                                if (index >= 0)
                                {
                                    matchedScores.Add(remainingTuples[index].Item2);
                                    remainingTuples.RemoveAt(index);
                                    filteredCharacters.Add(newCharacters[index]);
                                }
                            }

                            if (matchedScores.Count > 0)
                            {
                                readingProbability = matchedScores.Average();
                                plateToString = cleanedPlate;
                            }
                            else
                            {
                                filteredCharacters.AddRange(newCharacters); // Hiç eşleşme yoksa orijinal haliyle devam
                            }
                        }
                        else
                        {
                            filteredCharacters.AddRange(newCharacters); // Düzeltilemezse de orijinali döndür
                        }
                    }

                    // 📦 PlateResult objesi oluştur
                    PlateResult plateResult = new PlateResult()
                    {
                        plate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate),
                        segmented = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate),
                        threshould = BitmapConverter.ToBitmap(possibleCharacter.thresh),
                        addedRects = possibleCharacter.plateLocation,
                        readingPlateResult = plateToString,
                        readingPlateResultProbability = readingProbability,
                        m_characters = filteredCharacters
                    };

                    allPlateResults.Add(plateResult);
                }

                return allPlateResults;
            }
        }

        public static ThreadSafeList<PlateResult> KuyrukRecognizeAndDisplayPlateResultsListeDöner(ThreadSafeList<CharacterSegmentationResult> possibleCharacters, PreProcessingSettings preProcessingSettings)
        {
            //lock (osman)
            {
                ThreadSafeList<PlateResult> allPlateResults = new ThreadSafeList<PlateResult>();

                foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
                {
                    List<Mat> filteredCharacters = new List<Mat>();

                    List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;

                    (string plateKara, double confidence, List<Mat> characterMat, TupleList<string, double>) readingResult ;

                    lock (osman)
                    {
                        readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);
                    }

                    

                    string plateToString = readingResult.plateKara;
                    double readingProbability = readingResult.confidence;
                    var characterScores = readingResult.Item4; // TupleList<string, double>
                    List<Mat> newCharacters = readingResult.characterMat;

                    if (string.IsNullOrEmpty(plateToString))
                        continue;


                    // //Debug.WriteLine("Okunan Plaka : " + plateToString);


                    // 📦 PlateResult objesi oluştur
                    PlateResult plateResult = new PlateResult()
                    {
                        plate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate),
                        segmented = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate),
                        threshould = BitmapConverter.ToBitmap(possibleCharacter.thresh),
                        addedRects = possibleCharacter.plateLocation,
                        readingPlateResult = plateToString,
                        readingPlateResultProbability = readingProbability,
                        m_characters = filteredCharacters
                       // LastDetectionTime = DateTime.Now
                    };

                    allPlateResults.Add(plateResult);
                }

                return allPlateResults;
            }
        }


        //static List<(string plateKara, double confidence, List<Mat> characterMat)> m_possible = new List<(string plateKara, double confidence, List<Mat> characterMat)>();

        public static List<(string plateKara, double confidence, CharacterSegmentationResult possibleCharacter)> m_possible = new List<(string plateKara, double confidence, CharacterSegmentationResult possibleCharacter)>();

        //public static PlateResult RecognizeAndDisplayPlateResults1(ThreadSafeList<CharacterSegmentationResult> possibleCharacters)
        //{
        //    lock (osman)
        //    {
        //        PlateResult plateResult = new PlateResult();
        //        List<(string plateKara, double confidence, CharacterSegmentationResult possibleCharacter)> ocrResult = new List<(string plateKara, double confidence, CharacterSegmentationResult possibleCharacter)>();

              

        //        foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
        //        {
        //            List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;


                   

        //            (string plateKara, double confidence, List<Mat> characterMat, TupleList<string, double> tupleList) readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 60, 2);

        //            string plateToString = readingResult.plateKara;
        //            double readingProbability = readingResult.confidence;
        //            List<Mat> newCharacters = readingResult.characterMat;

        //            //string plateToString = OCRHelper.KNNModalOCRPrediction(characters);

        //            //if (((!string.IsNullOrEmpty(plateToString)) && (plateToString.Count() >= 7)) && (OCRHelper.Plate(plateToString.ToCharArray())))
        //            if (!string.IsNullOrEmpty(plateToString))
        //            {
        //                m_possible.Add((plateToString, readingProbability, possibleCharacter));

        //                MainForm.m_mainForm.frameCount++; 

        //                // OCR sonucu listesi 5’e ulaştıysa çoğunluk oylaması yap
        //                if (MainForm.m_mainForm.frameCount >= MainForm.m_mainForm.maxFramesToAggregate)
        //                {
        //                    var finalPlate = MajorityVoting(m_possible);  // OCR sonuçlarını birleştir

        //                    foreach (var olates in m_possible)
        //                    {
        //                        //Debug.WriteLine($"Listedeki Plaka: {olates.plateKara}");
        //                    }
                            



        //                    //Debug.WriteLine($"Tespit edilen plaka: {finalPlate.Key}");


        //                    plateResult.readingPlateResult = finalPlate.Key;
        //                    plateResult.readingPlateResultProbability = finalPlate.Value.confidenceSum / 5;

        //                    Bitmap colorPlate = BitmapConverter.ToBitmap(finalPlate.Value.possibleCharacter.colorPlate);
        //                    Bitmap segmentedPlate = BitmapConverter.ToBitmap(finalPlate.Value.possibleCharacter.segmentedPlate);
        //                    Bitmap threshouldPlate = BitmapConverter.ToBitmap(finalPlate.Value.possibleCharacter.thresh);

        //                    plateResult.plate = colorPlate;
        //                    plateResult.segmented = segmentedPlate;
        //                    plateResult.threshould = threshouldPlate;



        //                    // Yeni araç gelene kadar tekrar OCR yapma
        //                    m_possible.Clear();
        //                    MainForm.m_mainForm.frameCount = 0;





        //                    return plateResult;

        //                }
                       
        //            }
        //        }

        //        return plateResult;
        //    }
        //}

       
        //public static PlateResult RecognizeAndDisplayPlateResults(ThreadSafeList<CharacterSegmentationResult> possibleCharacters)
        //{
        //    lock (osman)
        //    {
        //        PlateResult plateResult = new PlateResult();

        //        foreach (CharacterSegmentationResult possibleCharacter in possibleCharacters)
        //        {
        //            List<Mat> characters = possibleCharacter.threshouldPossibleCharacters;


        //            var readingResult = OCRHelper.SVMModalOCRPredictionWithConfidenceApplyMajorityVoting2(characters, 80, 2);

        //            string plateToString = readingResult.plateKara;
        //            double readingProbability = readingResult.confidence;
        //            List<Mat> newCharacters = readingResult.characterMat;

        //            //if (((!string.IsNullOrEmpty(plateToString)) && (plateToString.Count() >= 7)) && (OCRHelper.Plate(plateToString.ToCharArray())))
        //            if (!string.IsNullOrEmpty(plateToString))
        //            {
        //                // OCR sonucu listesine ekle
        //                m_lastFivePlate.Add(new PlateResult
        //                {
        //                    readingPlateResult = plateToString,
        //                    readingPlateResultProbability = readingProbability,
        //                    plate = BitmapConverter.ToBitmap(possibleCharacter.colorPlate),
        //                    segmented = BitmapConverter.ToBitmap(possibleCharacter.segmentedPlate),
        //                    threshould = BitmapConverter.ToBitmap(possibleCharacter.thresh),
        //                    m_characters = newCharacters,
        //                    LastDetectionTime = DateTime.Now
        //                });

        //                MainForm.m_mainForm.frameCount++;

        //                // OCR sonucu listesi 5’e ulaştıysa çoğunluk oylaması yap
        //                if (MainForm.m_mainForm.frameCount >= MainForm.m_mainForm.maxFramesToAggregate)
        //                {
        //                    var finalPlate = MajorityVotingOptimize(m_lastFivePlate);  // OCR sonuçlarını birleştir

        //                    foreach (var olates in m_lastFivePlate)
        //                    {
        //                        //Debug.WriteLine($"Listedeki Plaka: {olates.readingPlateResult}");
        //                    }




        //                    //Debug.WriteLine($"Tespit edilen plaka: {finalPlate.readingPlateResult}");


        //                    plateResult.readingPlateResult = finalPlate.readingPlateResult;
        //                    plateResult.readingPlateResultProbability = finalPlate.readingPlateResultProbability;

        //                    plateResult.plate = finalPlate.plate;
        //                    plateResult.segmented = finalPlate.segmented;
        //                    plateResult.threshould = finalPlate.threshould;

        //                    plateResult.LastDetectionTime = finalPlate.LastDetectionTime;
        //                    plateResult.m_characters = finalPlate.m_characters;

        //                    // Yeni araç gelene kadar tekrar OCR yapma
        //                    m_lastFivePlate.Clear();
        //                    MainForm.m_mainForm.frameCount = 0;





        //                    return plateResult;

        //                }
                       
        //            }
        //        }

        //        return plateResult;
        //    }
        //}

        internal static readonly object m_remove = new object();
        //public static void RemovePlateList()
        //{
        //    lock(m_remove)
        //    {
        //        DateTime currentTime = DateTime.Now;

        //        int removedCount = Helper.m_lastFivePlate.RemoveAll(result => (currentTime - result.LastDetectionTime).TotalSeconds > 2);


        //        if (removedCount > 0)
        //        {
        //            //Debug.WriteLine("Listeden Son Plakalar Silindi : " + removedCount.ToString());


        //            if (Helper.m_lastFivePlate.Count == 0)
        //            {
        //                MainForm.m_mainForm.frameCount = 0;  // 🚀 Yeni araç geldi, sıfırla!

        //                //Debug.WriteLine("Yeni araç geldi, sıfırla!");
        //            }

        //        }
        //    }
        //}

        private static DateTime lastExecutionTime = DateTime.MinValue;
        private static volatile  bool isProcessing = false; // Tek seferde bir işleme izin verir
        //public static void RemovePlateListThreadSafe()
        //{
        //    DateTime currentTime = DateTime.Now;

        //    // Eğer metod zaten çalışıyorsa veya çok sık çağrılıyorsa, çalıştırma
        //    if (isProcessing || (currentTime - lastExecutionTime).TotalMilliseconds < 50)
        //        return;

        //    lock (m_remove)
        //    {
        //        if (isProcessing) return; // İkinci bir güvenlik katmanı
        //        isProcessing = true;
        //        lastExecutionTime = currentTime;

        //        try
        //        {
        //            int removedCount = Helper.m_lastFivePlate.RemoveAll(result => (currentTime - result.LastDetectionTime).TotalSeconds > 2);

        //            if (removedCount > 0)
        //            {
        //                //Debug.WriteLine($"Listeden Son Plakalar Silindi: {removedCount}");

        //                if (Helper.m_lastFivePlate.Count == 0)
        //                {
        //                    MainForm.m_mainForm.frameCount = 0;  // 🚀 Yeni araç geldi, sıfırla!
        //                    //Debug.WriteLine("Yeni araç geldi, sıfırla!");
        //                }
        //            }
        //        }
        //        finally
        //        {
        //            isProcessing = false; // İşlem bitti, flag'i sıfırla
        //        }
        //    }
        //}

        public static double[] ImageToPixel(Mat image)
        {
            // Piksel değerlerini al ve 1D dizisine dönüştür
            double[] pixelValues = new double[image.Rows * image.Cols];

            for (int i = 0; i < image.Rows; i++)
            {
                for (int j = 0; j < image.Cols; j++)
                {
                    // 0-255 aralığındaki piksel değerlerini 0-1 aralığına getir
                    pixelValues[i * image.Cols + j] = image.At<byte>(i, j) / 255.0;
                }
            }

            return pixelValues;
        }

        public static KeyValuePair<string, (int count, double confidenceSum, CharacterSegmentationResult possibleCharacter)> MajorityVoting(List<(string plateKara, double confidence, CharacterSegmentationResult possibleCharacter)> m_possible)
        {
            //if (m_possible == null || m_possible.Count == 0)
            //    return string.Empty; // Boş liste gelirse geri dön

            // OCR sonuçlarını ve tekrar sayılarını takip eden sözlük
            Dictionary<string, (int count, double confidenceSum, CharacterSegmentationResult possibleCharacter)> plateCounts = new Dictionary<string, (int, double, CharacterSegmentationResult)>();

            // Her plakanın kaç kez tekrar ettiğini ve toplam güven skorunu hesapla
            foreach (var result in m_possible)
            {
                if (!string.IsNullOrEmpty(result.plateKara))
                {
                    if (plateCounts.ContainsKey(result.plateKara))
                        plateCounts[result.plateKara] = (plateCounts[result.plateKara].count + 1, plateCounts[result.plateKara].confidenceSum + result.confidence, result.possibleCharacter);
                    else
                        plateCounts[result.plateKara] = (1, result.confidence, result.possibleCharacter);
                }
            }

            // En sık tekrar eden plakayı bul
            var mostFrequent = plateCounts.OrderByDescending(p => p.Value.count).ThenByDescending(p => p.Value.confidenceSum).First();

           

            return mostFrequent; // En sık tekrar eden plakayı döndür
        }

        public static PlateResult MajorityVoting(ThreadSafeList<PlateResult> plateResult)
        {
            //if (m_possible == null || m_possible.Count == 0)
            //    return string.Empty; // Boş liste gelirse geri dön

            // OCR sonuçlarını ve tekrar sayılarını takip eden sözlük
            Dictionary<string, (int count, double confidenceSum, PlateResult possibleCharacter)> plateCounts = new Dictionary<string, (int, double, PlateResult)>();

            // Her plakanın kaç kez tekrar ettiğini ve toplam güven skorunu hesapla
            foreach (var result in plateResult)
            {
                if (!string.IsNullOrEmpty(result.readingPlateResult))
                {
                    if (plateCounts.ContainsKey(result.readingPlateResult))
                        plateCounts[result.readingPlateResult] = (plateCounts[result.readingPlateResult].count + 1, 
                            plateCounts[result.readingPlateResult].confidenceSum + result.readingPlateResultProbability, 
                            new PlateResult
                        {
                            readingPlateResult = result.readingPlateResult,
                            readingPlateResultProbability = result.readingPlateResultProbability,
                            plate = result.plate,
                            segmented = result.segmented,
                            threshould = result.threshould,
                            m_characters = result.m_characters,
                           LastDetectionTime = result.LastDetectionTime

                            });
                    else
                        plateCounts[result.readingPlateResult] = (1, result.readingPlateResultProbability, new PlateResult
                        {
                            readingPlateResult = result.readingPlateResult,
                            readingPlateResultProbability = result.readingPlateResultProbability,
                            plate = result.plate,
                            segmented = result.segmented,
                            threshould = result.threshould,
                            m_characters = result.m_characters,
                             LastDetectionTime = result.LastDetectionTime
                        });
                }
            }



            // En sık tekrar eden plakayı bul
            var mostFrequent = plateCounts.OrderByDescending(p => p.Value.count).ThenByDescending(p => p.Value.confidenceSum).First().Value.possibleCharacter;



            return mostFrequent; // En sık tekrar eden plakayı döndür
        }

        public static PlateResult MajorityVotingOptimize(ThreadSafeList<PlateResult> plateResults)
        {
            if (plateResults == null || plateResults.Count == 0)
                return null; // Boş liste gelirse null döndür

            // **OCR sonuçlarını ve tekrar sayılarını takip eden sözlük**
            Dictionary<string, (int count, double confidenceSum, PlateResult referencePlate)> plateCounts =
                new Dictionary<string, (int, double, PlateResult)>();

            // **Thread-safe okuma için lock kullan**
            lock (plateResults)
            {
                foreach (var result in plateResults)
                {
                    if (!string.IsNullOrEmpty(result.readingPlateResult))
                    {
                        // **Eğer plaka zaten varsa sayısını artır ve güven skorunu ekle**
                        if (plateCounts.TryGetValue(result.readingPlateResult, out var existingPlate))
                        {
                            plateCounts[result.readingPlateResult] = (
                                existingPlate.count + 1, // **Tekrar sayısını artır**
                                existingPlate.confidenceSum + result.readingPlateResultProbability, // **Güven skoru ekle**
                                existingPlate.referencePlate // **Mevcut PlateResult nesnesini koru**
                            );
                        }
                        else
                        {
                            // **Yeni plaka ekle**
                            plateCounts[result.readingPlateResult] = (1, result.readingPlateResultProbability, result);
                        }
                    }
                }
            }

            if (plateCounts.Count == 0)
                return null; // Eğer sözlük boşsa, null döndür

            // **En sık tekrar eden plakayı bul (Tekrar sayısına ve güven skoruna göre sıralama)**
            var mostFrequent = plateCounts
                .OrderByDescending(p => p.Value.count)  // **Öncelik: En çok tekrar eden**
                .ThenByDescending(p => p.Value.confidenceSum)  // **Eşitlik varsa, güven skoru yüksek olan**
                .First().Value.referencePlate; // **En iyi sonucu seç**

            return mostFrequent; // **En sık tekrar eden plakayı döndür**
        }

    }
}
