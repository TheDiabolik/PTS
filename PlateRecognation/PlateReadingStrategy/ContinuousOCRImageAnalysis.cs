using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    /// <summary>
    /// Bu sınıf, tespit edilmiş plaka görüntüleri üzerinde OCR (Optik Karakter Tanıma) işlemi yapar.
    ///
    /// Görevi; kendisine verilen PossiblePlate nesnesi üzerinden karakter segmentasyonu uygulamak
    /// ve plaka üzerindeki karakterleri tanıyarak olası PlateResult sonuçlarını üretmektir.
    /// 
    /// Bu sınıf zamanlama, sonuç birleştirme veya event tetikleme gibi görevleri üstlenmez.
    /// Sadece işlenmiş plaka görüntüsünden karakter tanıma işlemini gerçekleştirir ve dış yapıya
    /// PlateResult listesini döner.
    ///
    /// Tipik kullanım adımları:
    /// - PossiblePlate nesnesi alınır
    /// - Karakter segmentasyonu yapılır
    /// - Karakterler tanınır ve PlateResult listesi üretilir
    /// - Sonuçlar dış bir yapı (örneğin OCRWorker) tarafından işlenir
    ///
    /// Bu sınıf IOCRImageAnalyzer arayüzünü uygular ve işleme hattı içinde bir analiz bileşeni olarak çalışır.
    /// </summary>

    internal class ContinuousOCRImageAnalysis : IOCRImageAnalyzer
    {
        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;

        private int _cameraId;

        public ContinuousOCRImageAnalysis() { }

        public ContinuousOCRImageAnalysis(int cameraId) : this()
        {
            _cameraId = cameraId;
        }



        public ThreadSafeList<AhmetPlateResult> SegmentCharactersFromOcrBuffer(OcrBatch tracker)
        {
            List<(List<CharacterDTO>, Mat)> possibleCharacters = new();

            foreach (SimpleTracker.OcrSample item in tracker.Samples)
            {
                using var mat = item.Img144x32.Clone();
                Cv2.CvtColor(mat, mat, ColorConversionCodes.GRAY2BGR);


                List<CharacterDTO> characterSegmentationResult = Character.AhmetAhmetAhmetFindAndCombineCharacterCandidatesv2(mat);

                possibleCharacters.Add((characterSegmentationResult, item.Img144x32.Clone()));

            }


            ThreadSafeList<AhmetPlateResult> ahmet = Helper.AhmetAhmetAhmetKuyrukRecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);


            return ahmet;
        }


        public List<PlateResult> OcrPlatesFromQueue(PossiblePlate plate)
        {
            ThreadSafeList<CharacterSegmentationResult> possibleCharacters = Character.SegmentCharactersInPlate(plate);

            ThreadSafeList<PlateResult> ts = Helper.KuyrukRecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);

            foreach (var result in ts)
            {
                result.LastDetectionTime = DateTime.Now;
                //result.frame = plate.frame.ToBitmap();
            }
                

            return ts.ToList();
        }

        // Event testleri veya özel senaryolar için hâlâ kullanılabilir durumda bırakıldı
        private void OnBestPlateDetected(string plateText)
        {
            PlateResultReady?.Invoke(this, new PlateOCRResultEventArgs
            {
                PlateText = plateText,
                DisplayDurationMs = 1000
            });
        }

      
    }
}
