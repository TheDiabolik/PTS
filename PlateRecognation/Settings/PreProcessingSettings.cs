using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class PreProcessingSettings
    {
        #region General
        public string m_ReadPlateFromImagePath { get; set; }
        public string m_ReadPlateFromVideoPath { get; set; }

        public bool m_ShowPlate { get; set; }

        public int m_MajorityVoiting { get; set; }

        public SerializableDictionary<string, Enums.OCRWorkingType> m_OCRWorkingType { get; set; } = new SerializableDictionary<string, Enums.OCRWorkingType>();


        //public Enums.OCRWorkingType m_OCRWorkingType { get; set; }

        #endregion


        #region PreProcessing
        public int m_GaussianBlurKernel { get; set; }

        public Enums.PreProcessingType m_preProcessingType { get; set; }

        #region AdaptiveThreshould
        public int m_adaptiveThreshouldBlock { get; set; }
        public int m_adaptiveThreshouldC { get; set; }
        public Enums.AdaptiveThreshouldType m_AdaptiveThreshouldType { get; set; }
        #endregion
        #endregion

        #region ImageAnalysis
        #region Plate
        public int m_plateMinWidth { get; set; }
        public int m_plateMaxWidth { get; set; }

        public int m_plateMinHeight { get; set; }
        public int m_plateMaxHeight { get; set; }

        public double m_plateMinAspectRatio { get; set; }
        public double m_plateMaxAspectRatio { get; set; }

        public int m_plateMinArea { get; set; }
        public int m_plateMaxArea { get; set; }
        #endregion

        #region Characters
        public int m_characterMinWidth { get; set; }
        public int m_characterMaxWidth { get; set; }

        public int m_characterMinHeight { get; set; }
        public int m_characterMaxHeight { get; set; }

        public double m_characterMinAspectRatio { get; set; }
        public double m_characterMaxAspectRatio { get; set; }

        public int m_characterMinArea { get; set; }
        public int m_characterMaxArea { get; set; }

        public double m_characterMinDiagonalLength { get; set; }
        public double m_characterMaxDiagonalLength { get; set; }
        #endregion
        #endregion


        #region Plate Reading

        public bool m_AutoWhiteBalance { get; set; }
        public bool m_AutoLightControl { get; set; }
        public bool m_FindMovementPlate { get; set; }

        public Enums.PlateType m_PlateType { get; set; }

        public bool m_FixOCRErrors { get; set; }

        public bool m_SelectBestResult { get; set; }

        #endregion














        public PreProcessingSettings()
        {

        }

        private static PreProcessingSettings m_preProcessingSettings = new PreProcessingSettings();
        public static PreProcessingSettings Singleton()
        {
            return m_preProcessingSettings;
        }


        public void Serialize(PreProcessingSettings imageProcessingSettings)
        {
            Serialization.Serialize(SerializationPaths.PreprocessingSettings, imageProcessingSettings);
        }
        public PreProcessingSettings DeSerialize(PreProcessingSettings imageProcessingSettings)
        {
            CheckSerializationFile();
            return Serialization.DeSerialize(SerializationPaths.PreprocessingSettings, imageProcessingSettings);
        }

        public void CheckSerializationFile()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(SerializationPaths.PreprocessingSettings)))
                    Directory.CreateDirectory(Path.GetDirectoryName(SerializationPaths.PreprocessingSettings));

                //xmlserilization dosyasını kontrol ediyoruz
                if (!File.Exists(SerializationPaths.PreprocessingSettings))
                {
                    #region General
                    PreProcessingSettings.Singleton().m_ReadPlateFromImagePath = "";
                    PreProcessingSettings.Singleton().m_ReadPlateFromVideoPath = "";
                    PreProcessingSettings.Singleton().m_ShowPlate= false;
                    PreProcessingSettings.Singleton().m_MajorityVoiting = 1;

                    PreProcessingSettings.Singleton().m_OCRWorkingType.Add("1", Enums.OCRWorkingType.Continuous);
                    #endregion



                    #region Preprocessing
                    #region AdaptiveThreshould
                    PreProcessingSettings.Singleton().m_adaptiveThreshouldBlock = 9;
                    PreProcessingSettings.Singleton().m_adaptiveThreshouldC = 3;
                    PreProcessingSettings.Singleton().m_AdaptiveThreshouldType = Enums.AdaptiveThreshouldType.Gaussian;
                    #endregion

                    PreProcessingSettings.Singleton().m_GaussianBlurKernel = 5;

                    PreProcessingSettings.Singleton().m_preProcessingType = Enums.PreProcessingType.BlurCLAHEOtsu;

                    #endregion



                    #region Plate
                    PreProcessingSettings.Singleton().m_plateMinWidth = 60;
                    PreProcessingSettings.Singleton().m_plateMaxWidth = 150;
                    PreProcessingSettings.Singleton().m_plateMinHeight = 20;
                    PreProcessingSettings.Singleton().m_plateMaxHeight = 100;
                    PreProcessingSettings.Singleton().m_plateMinAspectRatio = 2;
                    PreProcessingSettings.Singleton().m_plateMaxAspectRatio = 5;
                    PreProcessingSettings.Singleton().m_plateMinArea = 1000;
                    PreProcessingSettings.Singleton().m_plateMaxArea = 4000;
                    #endregion

                    #region Characters
                    PreProcessingSettings.Singleton().m_characterMinWidth = 3;
                    PreProcessingSettings.Singleton().m_characterMaxWidth = 20;
                    PreProcessingSettings.Singleton().m_characterMinHeight = 9;
                    PreProcessingSettings.Singleton().m_characterMaxHeight = 26;
                    PreProcessingSettings.Singleton().m_characterMinAspectRatio = 0.12;
                    PreProcessingSettings.Singleton().m_characterMaxAspectRatio = 1.0;
                    PreProcessingSettings.Singleton().m_characterMinArea = 1000;
                    PreProcessingSettings.Singleton().m_characterMaxArea = 4000;
                    PreProcessingSettings.Singleton().m_characterMinDiagonalLength = 17;
                    PreProcessingSettings.Singleton().m_characterMaxDiagonalLength = 30;
                    #endregion


                    #region Plate Reading
                    PreProcessingSettings.Singleton().m_AutoLightControl = false;
                    PreProcessingSettings.Singleton().m_AutoWhiteBalance = false;
                    PreProcessingSettings.Singleton().m_FindMovementPlate = false;

                    PreProcessingSettings.Singleton().m_PlateType = Enums.PlateType.Turkish;
                    PreProcessingSettings.Singleton().m_FixOCRErrors = true;
                    PreProcessingSettings.Singleton().m_SelectBestResult = true;

                    #endregion



                    PreProcessingSettings.Singleton().Serialize(PreProcessingSettings.Singleton());
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ExceptionMessages.CheckSerilizationFileExceptionMessage, ex);
            }
        }
    }
}
