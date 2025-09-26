using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class ChannelManager
    {
        private readonly ConcurrentDictionary<string, CameraChannel> _channels;

        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;

        public ChannelManager()
        {
            _channels = new ConcurrentDictionary<string, CameraChannel>();
        }

        public bool IsChannelRunning(string channelId) => _channels.ContainsKey(channelId);

        public IEnumerable<string> GetAllRunningChannelIds() => _channels.Keys.ToList();

        //public void StartChannel(string channelId, CameraConfiguration config,
        //    Func<CameraConfiguration, IPlateReadingStrategy> strategyFactory,
        //    Action<Bitmap> onFrameReady,
        //    Action<string, int> onPlateResult)
        //{
        //    StopChannel(channelId); // varsa önce durdur

        //    var cameraReader = new CameraReader(config.VideoSource);

        //    var ddd = new ContinuousOCRImageAnalysis(config.Id);

        //    var strategy = strategyFactory(config);

        //    strategy.Configure(config,ddd);
            


        //    var channel = new CameraChannel.Builder()
        //        .WithName(channelId)
        //        .WithReader(cameraReader)
        //        .WithOcrStrategy(strategy)
        //       // .WithOnFrameCaptured(onFrameReady)
        //        .Build();

        //    // Event bağlama tam burada yapılmalı:
        //    channel.PlateResultReady += (s, e) => onPlateResult?.Invoke(e.PlateText, e.DisplayDurationMs);

       
        //    _channels[channelId] = channel;
        //    channel.Start();

        //    SetupCameraReaderCallbacks(cameraReader, onFrameReady);
        //}


        public void StartChannel(string channelId, CameraConfiguration config,
           Func<CameraConfiguration, IPlateReadingStrategy> strategyFactory,
           Action<Bitmap> onFrameReady,
            Action<string, int> onPlateTextResult,
    Action<Bitmap, Bitmap> onPlateImageResult,
            Action<Bitmap, Bitmap, string, double> ahmet)
        {
            StopChannel(channelId); // varsa önce durdur

            var cameraReader = new CameraReader(config.VideoSource);

            var ddd = new ContinuousOCRImageAnalysis(config.Id);

            var strategy = strategyFactory(config);

            strategy.Configure(config, ddd);



            var channel = new CameraChannel.Builder()
                .WithName(channelId)
                .WithReader(cameraReader)
                .WithOcrStrategy(strategy)
                // .WithOnFrameCaptured(onFrameReady)
                .Build();

            // Event bağlama tam burada yapılmalı:
            //channel.PlateResultReady += (s, e) => onPlateResult?.Invoke(e.PlateText, e.DisplayDurationMs);


            channel.PlateTextReady += (s, e) =>
            {
                onPlateTextResult?.Invoke(e.PlateText, e.DisplayDurationMs);
            };

            channel.PlateImageReady += (s, e) =>
            {
                onPlateImageResult?.Invoke(e.Frame, e.PlateImage);
            };

            channel.PlateImageReady += (s, e) =>
            {
                ahmet?.Invoke(e.Frame, e.PlateImage, e.ReadingResult,e.Probability);
            };

            _channels[channelId] = channel;
            channel.Start();

            SetupCameraReaderCallbacks(cameraReader, onFrameReady);
        }

        //public void StartChannel1(string channelId, CameraConfiguration config,
        //  Func<CameraConfiguration, IPlateReadingStrategy> strategyFactory,
        //  Action<Bitmap> onFrameReady,
        //  Action<string, int> onPlateResult)         
        //{
        //    StopChannel(channelId); // varsa önce durdur

        //    var cameraReader = new CameraReader(config.VideoSource);

        //    var strategy = strategyFactory(config);
        //    strategy.Configure(config);



        //    var channel = new CameraChannel.Builder()
        //        .WithName(channelId)
        //        .WithReader(cameraReader)
        //        .WithOcrStrategy(strategy)
        //        // .WithOnFrameCaptured(onFrameReady)
        //        .Build();

        //    // Event bağlama tam burada yapılmalı:
        //    //channel.PlateResultReady += (s, e) => onPlateResult?.Invoke(e.PlateText, e.DisplayDurationMs);

        //    channel.PlateResultReady += (sender, args) =>
        //    {
        //        onPlateResult?.Invoke(args.PlateText, args.DisplayDurationMs);
        //    };


        //    _channels[channelId] = channel;
        //    channel.Start();

        //    SetupCameraReaderCallbacks(cameraReader, onFrameReady);
        //}

        public void StopChannel(string channelId)
        {
            if (_channels.TryRemove(channelId, out var channel))
            {
                channel.Stop();
            }
        }

        public void StopAllChannels()
        {
            foreach (var kvp in _channels)
            {
                kvp.Value.Stop();
            }
            _channels.Clear();
        }


        private void SetupCameraReaderCallbacks(ICameraReader reader, Action<Bitmap> onFrameReady)
        {
            if (onFrameReady != null)
            {
                reader.OnFrameCaptured += (bitmap) =>
                {
                    try
                    {
                        var clone = (Bitmap)bitmap.Clone(); // UI'da kullanmak için kopya
                        onFrameReady(clone);
                    }
                    catch { /* Logla, yut */ }
                    finally
                    {
                        bitmap.Dispose();
                    }
                };
            }
        }
    }
}
