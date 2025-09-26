using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class GeneralSettings
    {


        private static GeneralSettings m_generalSettings = new GeneralSettings();

        public static GeneralSettings Singleton()
        {
            return m_generalSettings;
        }


        public void Serialize(GeneralSettings generalSettings)
        {
            Serialization.Serialize(SerializationPaths.GeneralSettings, generalSettings);
        }
        public GeneralSettings DeSerialize(GeneralSettings generalSettings)
        {
            CheckSerializationFile();
            return Serialization.DeSerialize(SerializationPaths.GeneralSettings, generalSettings);
        }

        public void CheckSerializationFile()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(SerializationPaths.GeneralSettings)))
                    Directory.CreateDirectory(Path.GetDirectoryName(SerializationPaths.GeneralSettings));

                //xmlserilization dosyasını kontrol ediyoruz
                if (!File.Exists(SerializationPaths.GeneralSettings))
                {



                    GeneralSettings.Singleton().Serialize(GeneralSettings.Singleton());
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ExceptionMessages.CheckSerilizationFileExceptionMessage, ex);
            }
        }
    }
}
