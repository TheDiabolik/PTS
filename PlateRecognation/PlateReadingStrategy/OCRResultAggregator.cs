using Accord.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class OCRResultAggregator
    {
        private readonly ThreadSafeList<PlateResult> _buffer = new();
        private readonly object _lock = new();
        private DateTime _lastResultTime;
        private readonly int _timeoutMs;
        private readonly int _cameraId;
        private readonly int _threshold;

        private System.Threading.Timer _timeoutTimer;

        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;

        public OCRResultAggregator(int cameraId, int threshold = 3, int timeoutMs = 1000)
        {
            _cameraId = cameraId;
            _threshold = threshold;
            _timeoutMs = timeoutMs;
            _lastResultTime = DateTime.Now;
        }

        public void StartTimeoutWatcher()
        {
            if (_timeoutTimer != null) return;
            _timeoutTimer = new System.Threading.Timer(_ => CheckTimeout(), null, 0, 500);
        }

        public void StopTimeoutWatcher()
        {
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;
        }

        public void AddResults(IEnumerable<PlateResult> results)
        {
            lock (_lock)
            {
                _buffer.AddRange(results);
                _lastResultTime = DateTime.Now;

                if (_buffer.Count >= _threshold)
                    FinalizeAndEmit();
            }
        }

        private void CheckTimeout()
        {
            lock (_lock)
            {
                if (_buffer.Count > 0 && (DateTime.Now - _lastResultTime).TotalMilliseconds > _timeoutMs)
                    FinalizeAndEmit();
            }
        }

        private void FinalizeAndEmit()
        {
            if (_buffer.Count == 0) return;

            PlateResult bestPlate = PlateHelper.SelectBestPlateFromCandidates(_buffer);

            if (bestPlate != null)
            {
                PlateResultReady?.Invoke(this, new PlateOCRResultEventArgs
                {
                    PlateText = bestPlate.readingPlateResult,
                    DisplayDurationMs = 1000
                });

                PlateImageReady?.Invoke(this, new PlateImageEventArgs
                {
                    Frame = bestPlate.frame,
                    PlateImage = bestPlate.plate,
                    ReadingResult = bestPlate.readingPlateResult,
                    Probability = bestPlate.readingPlateResultProbability
                });
            }
            else
            {
                var groupPlatesByProximity = PlateHelper.GroupPlatesByProximity(_buffer);
                Enums.PlateType plateType = MainForm.m_mainForm.m_preProcessingSettings.m_PlateType;

                List<PlateResult> bestPlates = plateType == Enums.PlateType.Turkish
                    ? PlateHelper.SelectBestTurkishPlatesFromGroupsv1(groupPlatesByProximity)
                    : groupPlatesByProximity.SelectMany(g => g).ToList();

                foreach (PlateResult besties in bestPlates)
                {
                    //MainForm.m_mainForm.m_plateResults.Add(besties);

                    //PlateResultReady?.Invoke(this, new PlateResultEventArgs
                    //{
                    //    PlateText = besties.readingPlateResult,
                    //    DisplayDurationMs = 1000
                    //});

                    PlateResultReady?.Invoke(this, new PlateOCRResultEventArgs
                    {
                        PlateText = besties.readingPlateResult,
                        DisplayDurationMs = 1000
                    });

                    PlateImageReady?.Invoke(this, new PlateImageEventArgs
                    {
                        Frame =  besties.frame,
                        PlateImage = besties.plate,
                        ReadingResult = besties.readingPlateResult,
                        Probability = besties.readingPlateResultProbability
                    });
                }
            }

            _buffer.Clear();
        }
    }
}
