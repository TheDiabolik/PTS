using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class  PlateOCRResultEventArgs : EventArgs
    {
        //public int CameraId { get; set; }
        public string PlateText { get; set; }
        public int DisplayDurationMs { get; set; }



        public override string ToString()
        {
            //return $"Kanal: {CameraId} - Plaka: {PlateText}";

            return $"Plaka: {PlateText}";
        }
    }
}
