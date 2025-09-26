using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class FrameProcessingHelper
    {
        public static bool ShouldApplyWhiteBalance(Mat frame, ILightAdjustmentState state)
        {
            Mat lab = new Mat();
            Cv2.CvtColor(frame, lab, ColorConversionCodes.BGR2Lab);
            Mat[] labChannels = Cv2.Split(lab);

            Scalar meanA = Cv2.Mean(labChannels[1]);
            Scalar meanB = Cv2.Mean(labChannels[2]);

            // 🎯 Global değişkenlerle önceki değerleri karşılaştır
            if (state.PreviousMeanA.Val0 < 0)
                return true;

            double diffA = Math.Abs(meanA.Val0 - state.PreviousMeanA.Val0);
            double diffB = Math.Abs(meanB.Val0 - state.PreviousMeanB.Val0);

            bool shouldAdjust = diffA > 10 || diffB > 10;

            if (shouldAdjust)
            {
                state.PreviousMeanA = meanA;
                state.PreviousMeanB = meanB;
            }

            return shouldAdjust;
        }


        //public static Mat NewProcessFrame(Mat frame, bool autoLightControl, bool autoWhiteBalance)
        //{
        //    Mat balancedFrame = frame.Clone();

        //    if (autoWhiteBalance)
        //    {
        //        // 💡 Beyaz dengesini sadece önemli fark varsa uygula
        //        if (ShouldApplyWhiteBalance(frame))
        //        {
        //            balancedFrame = ImageEnhancementHelper.AutoAdjustWhiteBalance(balancedFrame);
        //        }
        //    }

        //    if (autoLightControl)
        //    {
        //        balancedFrame = SceneEnhancementHelper.ApplySmartEnhancementPipelineForBetaTest(balancedFrame);


        //        //balancedFrame = SceneEnhancementHelper.ApplySmartEnhancementPipeline(balancedFrame);

        //        //balancedFrame = ImageEnhancementHelper.ApplyGammaCorrection(balancedFrame, 0.8);
        //    }

        //    return balancedFrame;
        //}


        public static Mat ProcessFrame(Mat frame, bool autoLightControl, bool autoWhiteBalance)
        {
            Mat balancedFrame = frame.Clone();

            if (autoWhiteBalance)
            {
                balancedFrame = ImageEnhancementHelper.AutoAdjustWhiteBalance(balancedFrame);
            }

            if (autoLightControl)
            {
                balancedFrame = ImageEnhancementHelper.ApplyGammaCorrection(balancedFrame, 0.8);
            }

            return balancedFrame;
        }

      
    }
}
