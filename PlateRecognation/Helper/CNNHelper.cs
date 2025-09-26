using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class CNNHelper
    {
        static Shape m_shape = new Shape(20, 20, 1);

        public static (int predictedClass, double confidence) TestCNN(Net<double> network, Mat image)
        {
            double[] inputVector = Helper.ImageToPixel(image);


            // Giriş verisini Volume<double> formatına dönüştür
            Volume<double> inputVolume = BuilderInstance<double>.Volume.From(inputVector, m_shape);

            // İleri geçiş (forward pass) yap
            Volume<double> outputVolume = network.Forward(inputVolume);

            int[] sdds = network.GetPrediction();




            // Çıktıdaki maksimum olasılığı ve indexini bul
            double[] predictions = outputVolume.ToArray(); // Çıktı olasılıklarını al
            int predictedClass = Array.IndexOf(predictions, predictions.Max());
            double confidence = predictions.Max() * 100; // En büyük olasılığı % olarak hesapla


            var erswefwe = SoftmaxWithIndices(predictions);

            return (predictedClass, confidence);


        }

        public static async Task<(int predictedClass, double confidence)> TestCNNAsync(Net<double> network, Mat image)
        {
            return await Task.Run(() =>
            {
                double[] inputVector = Helper.ImageToPixel(image);

                // Giriş verisini Volume<double> formatına dönüştür
                Volume<double> inputVolume = BuilderInstance<double>.Volume.From(inputVector, m_shape);

                // **İleri geçiş (forward pass) yap**
                Volume<double> outputVolume = network.Forward(inputVolume);

                // Çıktıdaki maksimum olasılığı ve indexini bul
                double[] predictions = outputVolume.ToArray(); // Çıktı olasılıklarını al

                int predictedClass = -1;
                double confidence = 0;

                if (predictions.Length > 0)
                {
                    predictedClass = Array.IndexOf(predictions, predictions.Max());
                    confidence = predictions.Max() * 100; // En büyük olasılığı % olarak hesapla
                }

                var sortedPredictions = SoftmaxWithIndices(predictions);

                return (predictedClass, confidence);
            });
        }

       

        public static List<(int Index, double Value)> SoftmaxWithIndices(double[] values)
        {
            double maxVal = values.Max();

            // Softmax hesaplamasını yap
            double scale = values.Sum(x => Math.Exp(x - maxVal));
            double[] softmaxValues = values.Select(x => Math.Exp(x - maxVal) / scale).ToArray();

            // Index ve softmax değeri ile tuple listesi oluştur
            var indexedSoftmaxValues = softmaxValues
                .Select((value, index) => (Index: index, Value: value))
                .OrderByDescending(item => item.Value)  // Değerleri büyükten küçüğe sırala
                .ToList();

            return indexedSoftmaxValues;
        }
    }
}
