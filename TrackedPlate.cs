using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class TrackedPlate
    {
        public int Id { get; set; }
        public Rect BoundingBox { get; set; }
        public int FirstSeenFrame { get; set; }
        public int LastSeenFrame { get; set; }
        public int TimesSeen { get; set; }

        public List<OpenCvSharp.Point> CenterHistory { get; private set; } = new();
        public Mat PlateImage { get; set; }   // Son görülen plaka alanı (crop)
        public Mat Frame { get; set; }        // Bu plakayı içeren tüm görüntü (referans olarak)

        public TrackedPlate(int id, Rect bbox, int frameIndex, Mat plateImage, Mat frame)
        {
            Id = id;
            BoundingBox = bbox;
            FirstSeenFrame = frameIndex;
            LastSeenFrame = frameIndex;
            TimesSeen = 1;
            CenterHistory.Add(GetCenter(bbox));

            PlateImage = plateImage?.Clone();
            Frame = frame?.Clone();
        }

        public void Update(Rect bbox, int frameIndex, Mat plateImage, Mat frame)
        {
            BoundingBox = bbox;
            LastSeenFrame = frameIndex;
            TimesSeen++;
            CenterHistory.Add(GetCenter(bbox));

            PlateImage?.Dispose();
            Frame?.Dispose();

            PlateImage = plateImage?.Clone();
            Frame = frame?.Clone();
        }

        private OpenCvSharp.Point GetCenter(Rect rect)
        {
            return new OpenCvSharp.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        public double IoU(Rect other)
        {
            int x1 = Math.Max(BoundingBox.X, other.X);
            int y1 = Math.Max(BoundingBox.Y, other.Y);
            int x2 = Math.Min(BoundingBox.X + BoundingBox.Width, other.X + other.Width);
            int y2 = Math.Min(BoundingBox.Y + BoundingBox.Height, other.Y + other.Height);

            int interArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            int unionArea = BoundingBox.Width * BoundingBox.Height + other.Width * other.Height - interArea;

            return unionArea == 0 ? 0 : (double)interArea / unionArea;
        }

        public bool IsMatch(Rect candidate, double iouThreshold = 0.3)
        {
            return IoU(candidate) > iouThreshold;
        }

        public void Dispose()
        {
            PlateImage?.Dispose();
            Frame?.Dispose();
        }
    }
}
