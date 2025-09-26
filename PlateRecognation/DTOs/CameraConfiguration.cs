using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public record CameraConfiguration
    {
        public int Id { get; set; }
        public string VideoSource { get; set; }
        public bool AutoLightControl { get; set; }
        public bool AutoWhiteBalance { get; set; }
        public List<bool> FramePattern { get; set; }
        public Enums.OCRWorkingType OCRType { get; set; }
    }
}
