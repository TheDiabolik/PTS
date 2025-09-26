using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class PlateImageEventArgs : EventArgs
    {
        public Bitmap Frame { get; set; }
        public Bitmap PlateImage { get; set; }
        public string ReadingResult { get; set; }
        public double Probability { get; set; }
    }
}
