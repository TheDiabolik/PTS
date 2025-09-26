using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;

namespace PlateRecognation
{
    internal class RectGeometryHelper
    {

        public static double CalculateAspectRatio(int width, int height)
        {
            double aspectRatio = (double)width / height;

            return aspectRatio;
        }

        public static double CalculateAspectRatio(Rect rect)
        {
            int width = rect.Width;
            int height = rect.Height;

            double aspectRatio = (double)width / height;

            return aspectRatio;
        }
        public static int CalculateRectangleArea(Rect rect)
        {
            int width = rect.Width;
            int height = rect.Height;

            int area = width * height;

            return area;
        }

        public static double CalculateDiagonalLength(Rect rect)
        {
            // Dikdörtgenin sol üst ve sağ alt köşelerini alıyoruz
            Point topLeft = new Point(rect.Left, rect.Top);
            Point bottomRight = new Point(rect.Right, rect.Bottom);

            // Çizgiyi resim üzerinde göstermek istiyorsanız buraya ekleyebilirsiniz
            // Mat image = new Mat(500, 500, MatType.CV_8UC3, Scalar.All(255)); // Beyaz bir boş resim
            // Cv2.Line(image, topLeft, bottomRight, Scalar.Red, 2); // Çizgiyi çizin
            // Cv2.ImShow("Diagonal Line", image);
            // Cv2.WaitKey(0);

            // İki köşe arasındaki mesafeyi hesaplıyoruz
            double diagonalLength = Math.Sqrt(Math.Pow(bottomRight.X - topLeft.X, 2) + Math.Pow(bottomRight.Y - topLeft.Y, 2));

            return diagonalLength;
        }

        public static double CenterDist(Rect a, Rect b)
        {
            var ax = a.X + a.Width * 0.5; var ay = a.Y + a.Height * 0.5;
            var bx = b.X + b.Width * 0.5; var by = b.Y + b.Height * 0.5;
            var dx = ax - bx; var dy = ay - by;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static Rect ExpandRect(Rect r, OpenCvSharp.Size s, double scale)
        {
            double cx = r.X + r.Width / 2.0;
            double cy = r.Y + r.Height / 2.0;
            int w = (int)Math.Round(r.Width * scale);
            int h = (int)Math.Round(r.Height * scale);
            int x = (int)Math.Round(cx - w / 2.0);
            int y = (int)Math.Round(cy - h / 2.0);
            var R = new Rect(x, y, w, h);
            return ClampRect(R, s);
        }
        static Rect DeflateClamp(Rect r, int sx, int sy, OpenCvSharp.Size imgSize)
        {
            // 1) Kutunun sıfıra düşmesini önle (genişliğin/yüksekliğin en az 1 kalması)
            //    soldan+sağdan toplam 2*sx, üstten+alttan 2*sy daralıyoruz.
            int maxSx = Math.Max(0, (r.Width - 1) / 2);
            int maxSy = Math.Max(0, (r.Height - 1) / 2);
            sx = Math.Min(Math.Max(0, sx), maxSx);
            sy = Math.Min(Math.Max(0, sy), maxSy);

            // 2) İçten kırpılmış koordinatları (x1,y1,x2,y2) hesapla
            int x1 = r.X + sx;
            int y1 = r.Y + sy;
            int x2 = r.X + r.Width - sx;   // sağ sınır (exclusive gibi düşünebilirsin)
            int y2 = r.Y + r.Height - sy;   // alt  sınır

            // 3) Görüntü sınırlarına clamp et (x2/y2 en az x1+1/y1+1 olsun ki width/height >= 1 kalsın)
            x1 = Math.Clamp(x1, 0, Math.Max(0, imgSize.Width - 1));
            y1 = Math.Clamp(y1, 0, Math.Max(0, imgSize.Height - 1));
            x2 = Math.Clamp(x2, x1 + 1, imgSize.Width);
            y2 = Math.Clamp(y2, y1 + 1, imgSize.Height);

            // 4) Yeni boyutları oluştur
            int w = Math.Max(1, x2 - x1);
            int h = Math.Max(1, y2 - y1);

            return new Rect(x1, y1, w, h);
        }

        public static Rect ClampRect(Rect r, OpenCvSharp.Size s)
        {
            int x = Math.Max(0, Math.Min(r.X, s.Width - r.Width));
            int y = Math.Max(0, Math.Min(r.Y, s.Height - r.Height));
            int w = Math.Min(r.Width, s.Width);
            int h = Math.Min(r.Height, s.Height);
            return new Rect(x, y, w, h);
        }

        public static Rect InflateClamp(Rect r, int growX, int growY, OpenCvSharp.Size imgSize)
        {
            int x = Math.Max(0, r.X - growX);
            int y = Math.Max(0, r.Y - growY);
            int w = Math.Min(imgSize.Width - x, r.Width + 2 * growX);
            int h = Math.Min(imgSize.Height - y, r.Height + 2 * growY);
            if (w < 1) w = 1;
            if (h < 1) h = 1;
            return new Rect(x, y, w, h);
        }

        public static Rect Union(Rect a, Rect b)
        {
            int x1 = Math.Min(a.X, b.X);
            int y1 = Math.Min(a.Y, b.Y);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }


        //    static Rect Clip(Rect r, int W, int H) =>
        //new Rect(Math.Clamp(r.X, 0, Math.Max(0, W - 1)),
        //         Math.Clamp(r.Y, 0, Math.Max(0, H - 1)),
        //         Math.Clamp(r.Width, 1, W - r.X),
        //         Math.Clamp(r.Height, 1, H - r.Y));


        public static Rect Clip(Rect r, int W, int H)
        {
            int x = Math.Clamp(r.X, 0, Math.Max(0, W - 1));
            int y = Math.Clamp(r.Y, 0, Math.Max(0, H - 1));
            int w = Math.Clamp(r.Width, 1, Math.Max(0, W - x));
            int h = Math.Clamp(r.Height, 1, Math.Max(0, H - y));
            return new Rect(x, y, w, h);
        }

        //static Rect GrowRect(Rect r, int gx, int gy, int W, int H) =>
        //    new Rect(Math.Max(0, r.X - gx), Math.Max(0, r.Y - gy),
        //             Math.Min(W - Math.Max(0, r.X - gx), r.Width + 2 * gx),
        //             Math.Min(H - Math.Max(0, r.Y - gy), r.Height + 2 * gy));

        public static Rect GrowRect(Rect r, int gx, int gy, int W, int H)
        {
            // Sol/üst kenarı gx,gy kadar dışa taşı
            int x = Math.Max(0, r.X - gx);
            int y = Math.Max(0, r.Y - gy);

            // Sağ/alt kenarı gx,gy kadar dışa taşı ve sınırla
            int right = Math.Min(W, r.X + r.Width + gx);
            int bottom = Math.Min(H, r.Y + r.Height + gy);

            // En az 1 piksellik kutu garantisi
            int w = Math.Max(1, right - x);
            int h = Math.Max(1, bottom - y);

            return new Rect(x, y, w, h);
        }

        public static Rect GrowRectAdaptive(Rect r, OpenCvSharp.Size frameSize)
        {
            double ratio = (double)r.Width / Math.Max(1, r.Height); // plaka genelde ≥3
            double kx = (ratio >= 3.0) ? 0.08 : 0.06;  // yatay yüzde
            double ky = 0.05;                          // dikey yüzde

            int gx = (int)Math.Round(Math.Max(kx * r.Width, 6));  // alt sınır ~6–7 px
            int gy = (int)Math.Round(Math.Max(ky * r.Height, 5)); // alt sınır ~5–6 px

            gx = Math.Clamp(gx, 6, 24);
            gy = Math.Clamp(gy, 4, 16);

            return GrowRect(r, gx, gy, frameSize.Width, frameSize.Height);
        }




        public struct GrowMargins
        {
            public int Left, Right, Top, Bottom;
        }

        static Rect GrowRectAdaptive(Rect r, OpenCvSharp.Size frameSize, out GrowMargins eff)
        {
            // 1) adaptif yüzde/alt sınır
            double ratio = (double)r.Width / Math.Max(1, r.Height); // plaka genelde ≥3
            double kx = (ratio >= 3.0) ? 0.08 : 0.06;  // yatay yüzde
            double ky = 0.05;                          // dikey yüzde

            int gx = (int)Math.Round(Math.Max(kx * r.Width, 6));  // alt sınır ~6–7 px
            int gy = (int)Math.Round(Math.Max(ky * r.Height, 5)); // alt sınır ~5–6 px

            gx = Math.Clamp(gx, 6, 24);
            gy = Math.Clamp(gy, 4, 16);

            // 2) büyüt + clip
            int x = Math.Max(0, r.X - gx);
            int y = Math.Max(0, r.Y - gy);
            int right = Math.Min(frameSize.Width, r.X + r.Width + gx);
            int bottom = Math.Min(frameSize.Height, r.Y + r.Height + gy);

            // 3) ETKİN (clip sonrası) büyütme miktarları
            eff.Left = r.X - x;
            eff.Right = right - (r.X + r.Width);
            eff.Top = r.Y - y;
            eff.Bottom = bottom - (r.Y + r.Height);

            return new Rect(x, y, Math.Max(1, right - x), Math.Max(1, bottom - y));
        }

    }
}
