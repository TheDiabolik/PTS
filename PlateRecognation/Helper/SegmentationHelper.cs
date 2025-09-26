using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class SegmentationHelper
    {
        //public static void ApplyAndDrawInPictureBoxVerticalProjection(Mat binaryImage)
        //{
        //    // 1) Histogram çıkar
        //    int[] projection = VerticalProjectionProfile(binaryImage);

        //    // 2) Histogramı pürüzsüzleştir (smoothing)
        //    int[] smoothed = SmoothHistogram(projection, windowSize: 3);

        //    // 3) Histogramı çizmek için boş bir görüntü oluştur (sadece görselleştirme amacıyla)
        //    Mat projectionImage = new Mat(binaryImage.Size(), MatType.CV_8UC1, Scalar.All(255));
        //    for (int x = 0; x < smoothed.Length; x++)
        //    {
        //        // Her sütun için dikey çizgi
        //        Cv2.Line(
        //            projectionImage,
        //            new OpenCvSharp.Point(x, binaryImage.Rows),
        //            new OpenCvSharp.Point(x, binaryImage.Rows - smoothed[x]),
        //            Scalar.All(0)
        //        );
        //    }

        //    // 4) Yerel minimumları bul (dinamik eşik kullanarak)
        //    double average = smoothed.Average();  // Histogramın ortalaması
        //    List<int> segmentPoints = new List<int>();

        //    for (int x = 1; x < smoothed.Length - 1; x++)
        //    {
        //        // Komşularından daha düşük olan ve ortalamanın belirli oranından da düşük olan noktalar
        //        if (smoothed[x] < smoothed[x - 1] &&
        //            smoothed[x] < smoothed[x + 1] &&
        //            smoothed[x] < average * 0.5)
        //        {
        //            segmentPoints.Add(x);
        //        }
        //    }

        //    // 5) Çok dar boşlukları ele (hatalı bölmeleri engelle)
        //    int minWidth = 5;    // En az 5 piksel genişlik
        //    List<int> finalSegments = new List<int>();
        //    for (int i = 0; i < segmentPoints.Count - 1; i++)
        //    {
        //        int gap = segmentPoints[i + 1] - segmentPoints[i];
        //        if (gap >= minWidth)
        //        {
        //            finalSegments.Add(segmentPoints[i]);
        //        }
        //    }
        //    // Son elemanı da eklemek istersen yorum satırından çıkarabilirsin
        //    // finalSegments.Add(segmentPoints.Last());

        //    // 6) Segment çizgilerini histogram üzerinde çiz (görselleştirme)
        //    foreach (int seg in finalSegments)
        //    {
        //        Cv2.Line(
        //            projectionImage,
        //            new OpenCvSharp.Point(seg, 0),
        //            new OpenCvSharp.Point(seg, binaryImage.Rows),
        //            Scalar.Red,
        //            2
        //        );
        //    }

        //    // 7) Sonuçları göster
        //    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1,
        //                                    BitmapConverter.ToBitmap(projectionImage));
        //    DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.m_pictureBoxCharacterSegmented,
        //                                    BitmapConverter.ToBitmap(binaryImage));
        //}

        /// <summary>
        /// Basit bir kayan ortalama (moving average) filtresiyle histogramı yumuşatma.
        /// windowSize=3 ise, her noktayı önceki 3, kendisi, sonraki 3 pikselin ortalaması alarak pürüzsüzleştirir.
        /// </summary>
        private static int[] SmoothHistogram(int[] data, int windowSize)
        {
            int length = data.Length;
            int[] smoothed = new int[length];

            for (int i = 0; i < length; i++)
            {
                int sum = 0;
                int count = 0;
                // [-windowSize, +windowSize] aralığında gezin
                for (int w = -windowSize; w <= windowSize; w++)
                {
                    int idx = i + w;
                    if (idx >= 0 && idx < length)
                    {
                        sum += data[idx];
                        count++;
                    }
                }
                smoothed[i] = sum / count;
            }

            return smoothed;
        }

        /// <summary>
        /// Binarize edilmiş resimdeki her sütundaki siyah piksel (0) sayısını döndüren dikey projeksiyon.
        /// </summary>
        private static int[] VerticalProjectionProfile(Mat binaryImage)
        {
            int height = binaryImage.Rows;
            int width = binaryImage.Cols;
            int[] verticalProjection = new int[width];

            for (int x = 0; x < width; x++)
            {
                int sum = 0;
                for (int y = 0; y < height; y++)
                {
                    // Siyah piksel (0) say
                    if (binaryImage.At<byte>(y, x) == 0)
                    {
                        sum++;
                    }
                }
                verticalProjection[x] = sum;
            }

            return verticalProjection;
        }

       
    }
}
