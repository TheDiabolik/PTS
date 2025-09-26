using Accessibility;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal  class ContinuousOCRImageAnalysis2 
    {
        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;


        private int _cameraId;

        public ThreadSafeList<PlateResult> m_ocrResultQueue;
        public  int m_ocrResultCounter = 0;

        public Stopwatch m_plateQueueWatch;
        public const int PLATE_QUEUE_IDLE_TIMEOUT_MS = 1800;


        public DateTime m_lastQueueAddTime;

        private  System.Threading.Timer timeoutTimer;


        public readonly object queueLock = new object();
        public ContinuousOCRImageAnalysis2()
        {
            m_ocrResultQueue = new ThreadSafeList<PlateResult>();
            m_plateQueueWatch = new Stopwatch();
            m_lastQueueAddTime = DateTime.Now;

        }


        public ContinuousOCRImageAnalysis2(int cameraId) : this()
        {
            //m_onPlateResultReady = onPlateResultReady;
            _cameraId = cameraId;
        }


        private void OnBestPlateDetected(string plateText)
        {
            PlateResultReady?.Invoke(this, new PlateOCRResultEventArgs
            {
                //CameraId = _cameraId,
                PlateText = plateText,
                DisplayDurationMs = 1000
            });
        }



        public  void StartTimeoutWatcher()
        {
            if (timeoutTimer != null) return; // zaten çalışıyorsa tekrar başlatma

            timeoutTimer = new System.Threading.Timer(_ =>
            {
                lock (queueLock)
                {
                    if (m_ocrResultQueue.Count > 0 &&
                        (DateTime.Now - m_lastQueueAddTime).TotalMilliseconds > PLATE_QUEUE_IDLE_TIMEOUT_MS)
                    {
                        ////Debug.WriteLine("⏱ Timer-based timeout tetikledi.");
                        FinalizeQueueAndSelectResult();
                    }
                }
            }, null, 0, 1000); // 0.5 saniyede bir kontrol
        }


        public  void StopTimeoutWatcher()
        {
            if (timeoutTimer != null)
            {
                timeoutTimer.Dispose();
                timeoutTimer = null;
                ////Debug.WriteLine("🛑 Timeout watcher durduruldu.");
            }
        }

        public  void OcrPlatesFromQueue(PossiblePlate plate)
        {
          

                ThreadSafeList<CharacterSegmentationResult> possibleCharacters =
                Character.SegmentCharactersInPlate(plate);

            ThreadSafeList<PlateResult> ts =
                Helper.KuyrukRecognizeAndDisplayPlateResultsListeDöner(possibleCharacters, MainForm.m_mainForm.m_preProcessingSettings);



            lock (queueLock)
            {
                foreach (var result in ts)
                    result.LastDetectionTime = DateTime.Now;

                m_ocrResultQueue.AddRange(ts);
                m_ocrResultCounter++;
                m_lastQueueAddTime = DateTime.Now;


                m_plateQueueWatch.Restart();

                if (m_ocrResultCounter == 3)
                {
                    FinalizeQueueAndSelectResult();
                }
            }
        }

        private  void FinalizeQueueAndSelectResult()
        {
            lock (queueLock)
            {

                PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(m_ocrResultQueue);

                if (bestPlate != null)
                {
                    ////Debug.WriteLine($"✅ Seçilen plaka: {bestPlate.readingPlateResult} - Güven: {bestPlate.readingPlateResultProbability:F2}");
                    ////MainForm.m_mainForm.m_plateResults.Add(bestPlate);

                    //m_onPlateResultReady?.Invoke("Kanal : "+ _cameraId.ToString()+ " - "+ bestPlate.readingPlateResult,1000);


                    OnBestPlateDetected(bestPlate.readingPlateResult);

                    //DisplayManager.LabelInvoke(MainForm.m_mainForm.label2, bestPlate.readingPlateResult,1000);

                    //bestPlate.plate.Save("D:\\Ahmet\\" + "Kanal" + _cameraId.ToString() + " - " + bestPlate.readingPlateResult + ".jpg");

                    //MainForm.m_mainForm.label1.Text = bestPlate.readingPlateResult;

                }
                else
                {
                    var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(m_ocrResultQueue);
                    Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;
                    List<PlateResult> bestPlates = plateType == Enums.PlateType.Turkish
                        ? PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity)
                        : groupPlatesByProximity.SelectMany(g => g).ToList();

                    foreach (PlateResult besties in bestPlates)
                    {
                        MainForm.m_mainForm.m_plateResults.Add(besties);

                        
   

                        OnBestPlateDetected(besties.readingPlateResult);

                        //bestPlate.readingPlateResult

                        //m_onPlateResultReady?.Invoke("Kanal : " + _cameraId.ToString() + " - " + besties.readingPlateResult, 1000);

                        //besties.plate.Save("D:\\Ahmet\\" + "Kanal" + _cameraId.ToString() + " - " + besties.readingPlateResult + ".jpg");

                        ////DisplayManager.LabelInvoke(MainForm.m_mainForm.label2, besties.readingPlateResult,1000);

                    }

                }

                m_ocrResultCounter = 0;
                m_ocrResultQueue.Clear();

            }

        }
    }
}
