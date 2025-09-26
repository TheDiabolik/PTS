using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class OCRWorker
    {
        private readonly IOCRImageAnalyzer _ocrAnalyzer;
        private readonly BlockingCollection<PossiblePlate> _plateQueue;
        private readonly OCRResultAggregator _aggregator;

        public OCRWorker(IOCRImageAnalyzer analyzer, BlockingCollection<PossiblePlate> plateQueue, OCRResultAggregator aggregator)
        {
            _ocrAnalyzer = analyzer;
            _plateQueue = plateQueue;
            _aggregator = aggregator;
        }

        public void Start(CancellationToken token)
        {
            _aggregator.StartTimeoutWatcher();

            foreach (var plate in _plateQueue.GetConsumingEnumerable(token))
            {
                var results = _ocrAnalyzer.OcrPlatesFromQueue(plate);
                _aggregator.AddResults(results);
            }
        }

        public void Stop()
        {
            _aggregator.StopTimeoutWatcher();
        }
    }
}
