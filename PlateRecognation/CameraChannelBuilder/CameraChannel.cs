using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class CameraChannel : ILightAdjustmentState
    {
        public string ChannelName { get; private set; }
        public IPlateReadingStrategy OcrStrategy { get; private set; }

        public ICameraReader Reader { get; private set; }
        //public IImageEnhancer Enhancer { get; private set; }

     
        // Uncle Bob damarına ithafen interface üzerinden gelen state'ler
        public Scalar PreviousMeanA { get; set; } = new Scalar(-1);
        public Scalar PreviousMeanB { get; set; } = new Scalar(-1);
        public double PreviousBrightness { get; set; } = -1;


        public event EventHandler<PlateOCRResultEventArgs> PlateTextReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;


        //ctor
        private CameraChannel() { }


        public void Start()
        {
            Reader?.Start();
            OcrStrategy?.Start();
        }

        public void Stop()
        {
            Reader?.Stop();
            OcrStrategy?.Stop();
        }

        public void RaisePlateResult(string plateText, int durationMs)
        {
            PlateTextReady?.Invoke(this, new PlateOCRResultEventArgs
            {
                PlateText = plateText,
                DisplayDurationMs = durationMs
            });
        }


        //public void RaisePlateImageResult(Bitmap frame, Bitmap plateImage, string result, float probability)
        //{
        //    PlateImageReady?.Invoke(this, new PlateImageEventArgs
        //    {
        //        Frame = frame,
        //        PlateImage = plateImage,
        //        ReadingResult = result,
        //        Probability = probability
        //    });
        //}

        public void RaisePlateImageResult(Bitmap frame, Bitmap plateImage, string result, double probability)
        {
            PlateImageReady?.Invoke(this, new PlateImageEventArgs
            {
                Frame = frame,
                PlateImage = plateImage,
                ReadingResult = result,
                Probability = probability
            });
        }


        public void ddd(PlateOCRResultEventArgs args)
        {
            PlateTextReady?.Invoke(this, args);
        }


        public void ResetProcessingState()
        {
            PreviousBrightness = -1;
            PreviousMeanA = new Scalar(-1);
            PreviousMeanB = new Scalar(-1);

            ////Debug.WriteLine("---İşlem durumu sıfırlandı---");
        }

        public Mat NewProcessFrame(Mat frame, bool autoLightControl, bool autoWhiteBalance)
        {
            Mat balancedFrame = frame.Clone();

            if (autoWhiteBalance && FrameProcessingHelper.ShouldApplyWhiteBalance(balancedFrame, this))
            {

                balancedFrame = ImageEnhancementHelper.AutoAdjustWhiteBalance(balancedFrame);
            }

            if (autoLightControl)
            {
                balancedFrame = SceneEnhancementHelper.ApplySmartEnhancementPipelineForBetaTest(balancedFrame);
            }

            return balancedFrame;
        }

        public class Builder
        {
            private readonly CameraChannel _channel = new CameraChannel();

            public Builder WithName(string name)
            {
                _channel.ChannelName = name;
                return this;
            }

            public Builder WithOcrStrategy(IPlateReadingStrategy strategy)
            {
                _channel.OcrStrategy = strategy;

                // Bağlama burada yapılmalı:
                //strategy.PlateResultReady += (s, e) => _channel.PlateResultReady?.Invoke(_channel, e);


                //bunun yerine anonim delege bildirimi yapacağız
                strategy.PlateResultReady += (s, e) => _channel.RaisePlateResult(e.PlateText, e.DisplayDurationMs);
                strategy.PlateImageReady += (s, e) => _channel.RaisePlateImageResult(e.Frame, e.PlateImage, e.ReadingResult, e.Probability);


              

                //strategy.PlateResultReady += (s, e) => _channel.ddd(e);

                strategy.SetCameraChannel(_channel);

                return this;
            }

            public Builder WithReader(ICameraReader reader)
            {
                _channel.Reader = reader;
                return this;
            }
        
            public CameraChannel Build()
            {
                if (_channel.OcrStrategy == null)
                    throw new InvalidOperationException("Strategy and Enhancer are required");


                // 🔥 Event bağlama burada değil, Start() içinde yapılacak.
                return _channel;
            }
        }
    }
}
