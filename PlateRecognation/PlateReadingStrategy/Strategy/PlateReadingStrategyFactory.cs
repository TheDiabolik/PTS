using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public  class PlateReadingStrategyFactory
    {
        public static IPlateReadingStrategy Create(CameraConfiguration cameraConfiguration)
        {
            IPlateReadingStrategy strategy;

            switch (cameraConfiguration.OCRType)
            {
                case Enums.OCRWorkingType.Motion:
                    {
                        //strategy = new MotionBasedPlateReadingStrategy();

                        //strategy = new MotionBasedMultiROIPlateStrategy();

                        //strategy = new OpticalFlowMotionDetectionStrategy();

                        //strategy = new PlateTrackingWithOpticalFlowStrategy();

                        strategy = new HybridPlateReadingStrategy();

                        break;

                    }
                case Enums.OCRWorkingType.Continuous:
                    {
                        strategy = new ContinuousPlateReadingStrategy();
                        break;

                    }
                default:
                    {
                        throw new NotSupportedException($"OCR type {cameraConfiguration.OCRType} not supported");


                    }

            }

            return strategy;
        }
    }

}
