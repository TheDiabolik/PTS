using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public interface IPlateReadingStrategy
    {
        event EventHandler<PlateOCRResultEventArgs> PlateResultReady;

        event EventHandler<PlateImageEventArgs> PlateImageReady;


        //void Configure(CameraConfiguration cameraConfiguration);
        void Configure(CameraConfiguration cameraConfiguration, IOCRImageAnalyzer analyzer);
        //void RegisterCallbacks(Action<string, int> onPlateResultReady);
        void Start();
        void Stop();

        void SetCameraChannel(CameraChannel channel);
    }
}
