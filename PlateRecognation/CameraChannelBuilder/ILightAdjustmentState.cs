using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal interface ILightAdjustmentState
    {
        Scalar PreviousMeanA { get; set; }
        Scalar PreviousMeanB { get; set; }
        double PreviousBrightness { get; set; }

        void ResetProcessingState();
    }
}
