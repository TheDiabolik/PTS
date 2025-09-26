using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class ImageAnalysisSettings
    {








        public ImageAnalysisSettings()
        {

        }

        private static ImageAnalysisSettings m_imageAnalysisSettings = new ImageAnalysisSettings();
        public static ImageAnalysisSettings Singleton()
        {
            return m_imageAnalysisSettings;
        }


        public void Serialize(ImageAnalysisSettings imageAnalysisSettings)
        {
            Serialization.Serialize(SerializationPaths.ImageAnalysisSettings, imageAnalysisSettings);
        }
        public ImageAnalysisSettings DeSerialize(ImageAnalysisSettings imageAnalysisSettings)
        {
            CheckSerializationFile();
            return Serialization.DeSerialize(SerializationPaths.ImageAnalysisSettings, imageAnalysisSettings);
        }

        public void CheckSerializationFile()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(SerializationPaths.ImageAnalysisSettings)))
                    Directory.CreateDirectory(Path.GetDirectoryName(SerializationPaths.ImageAnalysisSettings));

                //xmlserilization dosyasını kontrol ediyoruz
                if (!File.Exists(SerializationPaths.ImageAnalysisSettings))
                {






                    ImageAnalysisSettings.Singleton().Serialize(ImageAnalysisSettings.Singleton());
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ExceptionMessages.CheckSerilizationFileExceptionMessage, ex);
            }
        }
    }
}
