using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class SVMHelper
    {

        public static int  AskSVMPredictionForPlateRegion(MulticlassSupportVectorMachine<Linear> svm, Mat image)
        {
            double[] testInput = FeatureExtractionHelper.ExtractHOGFeaturesForPlate(image);

            int predictedClass = svm.Decide(testInput);

            return predictedClass;
        }

        public static (int predictedClass, double score) AskSVMPredictionForPlateRegionWithScorev1(MulticlassSupportVectorMachine<Linear> svm, Mat image)
        {
            //double[] testInput = Helper.AskPlate(path);

            double score = 0;

            double[] testInput = FeatureExtractionHelper.ExtractHOGFeaturesForPlate(image);

            int predictedClass = svm.Decide(testInput);

            if (predictedClass == 0)
            {
                score = svm.Score(testInput);
                ////Debug.WriteLine("PTS Score : " + score);
            }

            return (predictedClass,score);
        }

        public static (bool isPlate, int predictedClass, double score) AskSVMPredictionForPlateRegionWithScore(MulticlassSupportVectorMachine<Linear> svm, Mat image, double scoreThreshold = 0.7)
        {
            double[] testInput = FeatureExtractionHelper.ExtractHOGFeaturesForPlate(image);

            int predictedClass = svm.Decide(testInput);
            double score = svm.Score(testInput);

            // Sadece class == 0 ve score > 0 ise geçerli olsun
            bool isPlate = (predictedClass == 0 && score >= scoreThreshold);

            return (isPlate, predictedClass, score);
        }
    }
}
