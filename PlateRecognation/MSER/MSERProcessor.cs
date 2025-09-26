using Accord.Math;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class MSERProcessor
    {
        private readonly MserDetectionSettings m_settings;

        public MSERProcessor(MserDetectionSettings settings)
        {
            m_settings = settings;
        }

        private MSER CreateDetector()
        {
            return MSER.Create(
                m_settings.Delta,
                m_settings.MinArea,
                m_settings.MaxArea,
                m_settings.MaxVariation,
                m_settings.MinDiversity,
                m_settings.MaxEvolution,
                m_settings.AreaThreshold,
                m_settings.MinMargin,
                m_settings.EdgeBlurSize
            );
        }

        public  Rect[] DetectPlate(Mat grayImage)
        {
            List<Rect> plateRects = new List<Rect>();


            using var mserDetector = CreateDetector();


            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mserDetector.DetectRegions(grayImage, out msers, out bboxes);

            foreach (var rect in bboxes)
            {
                if (rect.Width > 30 && rect.Height > 10 && rect.Width < 300 && rect.Height < 120)
                {
                    plateRects.Add(rect);
                }
            }

            return plateRects.ToArray();
        }


        public Rect[] DetectPlateROI(Mat grayImage)
        {
            List<Rect> plateRects = new List<Rect>();


            using var mserDetector = CreateDetector();


            // Anahtar noktaları ve bölge vektörlerini tespit etme
            OpenCvSharp.Point[][] msers;
            OpenCvSharp.Rect[] bboxes;
            mserDetector.DetectRegions(grayImage, out msers, out bboxes);

            int imgWidth = grayImage.Width;
            int imgHeight = grayImage.Height;

            foreach (var rect in bboxes)
            {
                double aspectRatio = rect.Width / (double)rect.Height;
                double area = rect.Width * rect.Height;
                double relativeArea = area / (double)(imgWidth * imgHeight);

                // Daha esnek ve oran bazlı filtreleme
                bool validSize = rect.Width >= 30 && rect.Height >= 10;
                bool validAspect = aspectRatio >= 2.0 && aspectRatio <= 6.0;
                bool validArea = relativeArea > 0.01 && relativeArea < 0.5;
                bool withinImage = rect.X >= 0 && rect.Y >= 0 && rect.Right <= imgWidth && rect.Bottom <= imgHeight;

                if (validSize && validAspect && validArea && withinImage)
                {
                    plateRects.Add(rect);
                }
            }

            return plateRects.ToArray();
        }

        public  List<MserResult> DetectCharacters(Mat image)
        {
            using var mserDetector = CreateDetector();

            // Bölgeleri tespit edin
            OpenCvSharp.Point[][] msersPlate;
            Rect[] bboxesPlate;
            mserDetector.DetectRegions(image, out msersPlate, out bboxesPlate);

            var sortedBBoxes = msersPlate
                           .Select((points, index) => new MserResult { Points = points, Area = Cv2.ContourArea(points), BBox = bboxesPlate[index] })
                           .OrderBy(bbox => bbox.BBox.X)  // Soldan sağa sırala
                           .ToList();

            return sortedBBoxes;

        }



    }
}
