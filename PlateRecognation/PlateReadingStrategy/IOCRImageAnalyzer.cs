using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public interface IOCRImageAnalyzer
    {
        //event EventHandler<PlateResultEventArgs> PlateResultReady;

        //void StartTimeoutWatcher();

        //void OcrPlatesFromQueue(PossiblePlate plate);

        List<PlateResult> OcrPlatesFromQueue(PossiblePlate plate);
    }
}
