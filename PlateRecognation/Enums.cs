using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class Enums
    {
        public enum OCRWorkingType { Motion, Continuous }
        public enum AdaptiveThreshouldType { Gaussian, Mean }

        public enum PlateType { Turkish, All }

        public enum PreProcessingType { BlurCLAHEOtsu, BlurCLAHEAdaptive, BlurHistEqualizeOtsu, BlurHistEqualizeAdaptive }
    }
}
