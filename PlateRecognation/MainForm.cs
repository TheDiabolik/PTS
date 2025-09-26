using System;
using System.IO;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using static System.Formats.Asn1.AsnWriter;
using PlateRecognation.SettingsModalForms;
using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using Accord.IO;
using Accord.MachineLearning;
using Accord.MachineLearning.VectorMachines;
using Accord.Neuro;
using Accord.Statistics.Kernels;
using ConvNetSharp.Core.Serialization;
using ConvNetSharp.Core;
using System.Threading;
using System.Diagnostics;
using ConvNetSharp.Volume;
using Microsoft.VisualBasic.Devices;
using PlateRecognation.Properties;
using System.Text.RegularExpressions;
using Accord.Imaging.Filters;
using System.Windows.Forms.VisualStyles;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using static PlateRecognation.Enums;
using System.Reflection.Emit;

namespace PlateRecognation
{
    public partial class MainForm : Form
    {
        public static MainForm m_mainForm;
        public Net<double> m_loadedCNN;

        public ConcurrentObservableCollection<PlateResult> m_plateResults;
        private const int pageSize = 10;
        int currentPageNumber = 1;

        private ChannelManager m_channelManager;


        FlowLayoutPanel m_flowPanelChannels;
        private List<PictureBox> m_pictureBoxes = new();
        private List<System.Windows.Forms.Label> m_labels = new();
        private List<System.Windows.Forms.Button> m_buttons = new();

        public MulticlassSupportVectorMachine<Linear> m_loadedSvmForPlateRegion;

        bool m_firstChannelButtonSwitch, m_secondChannelButtonSwitch, m_thirdChannelButtonSwitch, m_fourthChannelButtonSwitch;

        #region Settings
        //public GeneralSettings m_generalSettings;
        public PreProcessingSettings m_preProcessingSettings;
        //public ImageAnalysisSettings m_imageAnalysisSettings;
        #endregion


        public MainForm()
        {
            InitializeComponent();

            m_channelManager = new ChannelManager();

            m_mainForm = this;
          

            DisplayManager.SetDoubleBuffered(this);
            DisplayManager.SetDoubleBuffered(m_dataGridViewPossiblePlateRegions);
            DisplayManager.SetDoubleBuffered(m_listBoxPath);
          

            #region Settings
            m_preProcessingSettings = PreProcessingSettings.Singleton();
            m_preProcessingSettings = m_preProcessingSettings.DeSerialize(m_preProcessingSettings);

            //m_signPlate = m_preProcessingSettings.m_ShowPlate;
            #endregion

            // Eğer birden fazla ekran varsa
            if (Screen.AllScreens.Length > 1)
            {
                // 2. ekranı seç (index 1)
                Screen secondScreen = Screen.AllScreens[2];

                // Formun başlangıç pozisyonunu manuel ayarla
                this.StartPosition = FormStartPosition.Manual;

                // Formun sol üst köşesini ikinci ekranın sol üst köşesine taşı
                this.Location = secondScreen.WorkingArea.Location;
            }

            

            m_flowPanelChannels = new FlowLayoutPanel();
            m_flowPanelChannels.Size = new System.Drawing.Size(1300, 1000);

            m_flowPanelChannels.BackColor = System.Drawing.Color.White;
            m_flowPanelChannels.Location = new System.Drawing.Point(0, 27);

           

            this.Controls.Add(m_flowPanelChannels);
            m_flowPanelChannels.BringToFront();

            m_dataGridViewPossiblePlateRegions.Location = new System.Drawing.Point(m_flowPanelChannels.Width + 5, m_flowPanelChannels.Location.Y);
            pictureBoxFrame.Location = new System.Drawing.Point(m_flowPanelChannels.Width + 5, m_dataGridViewPossiblePlateRegions.Bottom + 5);
            //m_pictureBoxSeedPlate.Location = new System.Drawing.Point(m_flowPanelChannels.Width + 5 + pictureBoxFrame.Width + 5,
            //    m_dataGridViewPossiblePlateRegions.Bottom + 5);



            //AddChannel(1);
            //AddChannel(2);
            //AddChannel(3);
            AddChannel(4);


            m_plateResults = new ConcurrentObservableCollection<PlateResult>();
            //m_plateResults.CollectionChanged += PlateResults_CollectionChanged;
            //DataGridViewPagingHelper.LoadPagedData(currentPageNumber, pageSize, m_dataGridViewPossiblePlateRegions);
        }


        //protected override void OnLoad(EventArgs e)
        //{
        //    base.OnLoad(e);

        //    this.FormBorderStyle = FormBorderStyle.None;
        //    this.WindowState = FormWindowState.Normal;
        //    this.Bounds = Screen.AllScreens[2].Bounds;
        //    this.TopMost = true; // Formun her zaman en üstte kalmasını sağlar

           
        //}
        private void AddChannel(int channelID)
        {
            for (int i = 0; i < channelID; i++)
            {
                var pb = new PictureBox
                {
                    Name = $"m_pictureBoxChannel{(i+1).ToString()}",
                    Size = new System.Drawing.Size(640, 480),
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.AliceBlue,
                    Margin = new Padding(5)
                };

                var label = new System.Windows.Forms.Label
                {
                    Name = $"m_labelChannel{(i + 1).ToString()}",
                    Text = $"Kanal {(i + 1).ToString()}",
                    ForeColor = Color.Red,
                    BackColor = Color.Transparent,
                    Font = new Font("Arial", 24, FontStyle.Bold),
                    AutoSize = true,
                    Parent = pb,
                    Location = new System.Drawing.Point(5, 5)
                };

                var button = new System.Windows.Forms.Button
                {
                    Name = $"m_buttonChannel{(i + 1).ToString()}",
                    BackgroundImage = Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_start_256,
                    BackgroundImageLayout = ImageLayout.Stretch,
                    Size = new System.Drawing.Size(64, 64),
                    Parent = pb,
                    Location = new System.Drawing.Point(pb.Width - 64, pb.Height - 64),
                    AutoSize = true,
                };


                DisplayManager.SetDoubleBuffered(pb);
                DisplayManager.SetDoubleBuffered(label);
                DisplayManager.SetDoubleBuffered(button);





                button.Click += m_buttonVideo_Click;

                m_pictureBoxes.Add(pb);
                m_labels.Add(label);
                m_buttons.Add(button);


                m_flowPanelChannels.Controls.Add(pb);
            }




            
        }



        public void LoadSVMModelForPlateRegion() => m_loadedSvmForPlateRegion = Serializer.Load<MulticlassSupportVectorMachine<Linear>>("newresizestyleplatenewtrainset.bin");



        private void m_buttonVideo_Click(object sender, EventArgs e)
        {
            var button = sender as Button;

            if (button == null)
                return;

            string channelId = "";
            string videoName = "";
            PictureBox targetBox = null;
            System.Windows.Forms.Label targetLabel = null;
            ref bool buttonSwitch = ref m_firstChannelButtonSwitch; // default

            if (button == m_buttons[0])
            {
                channelId = "1";
                videoName = "edsvideo1.mkv";
                targetBox = m_pictureBoxes[0];
                targetLabel = m_labels[0];
                buttonSwitch = ref m_firstChannelButtonSwitch;
            }
            else if (button == m_buttons[1])
            {
                channelId = "2";
                videoName = "edsvideo2.mkv";
                targetBox = m_pictureBoxes[1];
                targetLabel = m_labels[1];
                buttonSwitch = ref m_secondChannelButtonSwitch;
            }
            else if (button == m_buttons[2])
            {
                channelId = "3";
                videoName = "edsvideo3.mkv";
                targetBox = m_pictureBoxes[2];
                targetLabel = m_labels[2];
                buttonSwitch = ref m_thirdChannelButtonSwitch;
            }
            else if (button == m_buttons[3])
            {
                channelId = "4";
                videoName = "edsvideo4.mkv";
                targetBox = m_pictureBoxes[3];
                targetLabel = m_labels[3];
                buttonSwitch = ref m_fourthChannelButtonSwitch;
            }

            m_preProcessingSettings = m_preProcessingSettings.DeSerialize(m_preProcessingSettings);

            buttonSwitch = !buttonSwitch;

            var icon = buttonSwitch
                ? Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_pause_256
                : Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_start_256;

            DisplayManager.ButtonBoxBackgroundImageInvoke(button, icon);

            UpdateCameraPipeline(channelId, buttonSwitch, videoName, targetBox, targetLabel);

        }



        private void UpdateCameraPipeline(string cameraId, bool isOn, string videoFileName, PictureBox pictureBox, System.Windows.Forms.Label labeeel)
        {
            // Kamera zaten çalışıyorsa durdur
            if (m_channelManager.IsChannelRunning(cameraId))
                m_channelManager.StopChannel(cameraId);

            if (!isOn)
                return;

            // Konfigürasyonu hazırla
            if (!m_preProcessingSettings.m_OCRWorkingType.TryGetValue(cameraId, out var workingType))
                workingType = Enums.OCRWorkingType.Continuous; // varsayılan

            var config = new CameraConfiguration()
            {
                Id = int.Parse(cameraId),
                VideoSource = videoFileName,
                AutoLightControl = m_preProcessingSettings.m_AutoLightControl,
                AutoWhiteBalance = m_preProcessingSettings.m_AutoWhiteBalance,
                //FramePattern = new List<bool> { true,true,false,true,false },
                FramePattern = new List<bool> { true},
                OCRType = workingType

            };


            //m_channelManager.StartChannel(
            //   cameraId,
            //   config,
            //   strategyFactory: strategy => PlateReadingStrategyFactory.Create(config),
            //   onFrameReady: bitmap => DisplayManager.PictureBoxInvoke(pictureBox, bitmap),
            //     onPlateResult: (plateText, durationMs, frame, plateImage, readingResult, probability) =>
            //     {
            //         var args = new PlateResultEventArgs
            //         {
            //             PlateText = plateText,
            //             DisplayDurationMs = durationMs,
            //             frame = frame,
            //             plate = plateImage,
            //             readingPlateResult = readingResult,
            //             readingPlateResultProbability = probability
            //         };

            //         OnPlateResultReceived(args);  // Label, PictureBox gibi her şey burada tetiklenir.
            //     }
            //     );


            //m_channelManager.PlateResultReady += OnPlateResultReceived;


            m_channelManager.StartChannel(
             cameraId,
             config,
             strategyFactory: strategy => PlateReadingStrategyFactory.Create(config),
             onFrameReady: bitmap => DisplayManager.PictureBoxInvoke(pictureBox, bitmap),
               onPlateTextResult: (plateText, durationMs) => DisplayManager.LabelInvoke(labeeel, plateText, durationMs),
               onPlateImageResult: (frame, plateImage) => DisplayManager.PictureBoxInvoke(pictureBoxFrame, frame, m_pictureBoxPlateTrack,  plateImage),
               ahmet: (frame, plateImage, plateResult, probability) => DisplayManager.DataGridViewAddRowPlateResultInvoke(m_dataGridViewPossiblePlateRegions,
               frame,plateImage, plateResult,probability)

               );


        }

        private void OnPlateResultReceived(PlateOCRResultEventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new Action(() => OnPlateResultReceived(sender, e)));
            //    return;
            //}


            //pictureBoxPlate.Image = e.PlateImage;
            //pictureBoxFrame.Image = frame;

        }

        private void m_preprocessingItem_Click(object sender, EventArgs e)
        {
            PreprocessingSettingsModal preprocessingSettingsModal = PreprocessingSettingsModal.Singleton(this);
            preprocessingSettingsModal.Owner = this;
            preprocessingSettingsModal.Show();
        }


        // Listeye eleman eklendiğinde DataGridView'i güncelleyen event handler
        private void PlateResults_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int totalItems = m_plateResults.Count;


            // Eğer toplam eleman sayısı sayfa limitini aştıysa yeni sayfaya geç
            //if (totalItems > currentPageNumber * pageSize)
            //{
            //    currentPageNumber++;  // Yeni sayfaya geç
            //    //LoadPagedData(currentPageNumber);
            //    DataGridViewPagingHelper.LoadPagedData(currentPageNumber, pageSize, m_dataGridViewPossiblePlateRegions);
            //}
            //else
            {
                // Eleman hâlâ mevcut sayfa içinde, sadece son elemanı seç
                //AddLastItemToGrid();
                DataGridViewPagingHelper.AddLastItemToGrid(m_dataGridViewPossiblePlateRegions);
            }


            //int maxItems = 50; // Koleksiyonun maksimum eleman sayısı
            //int totalItems = m_plateResults.Count;

            //// Eğer toplam eleman sayısı maksimum sınırı aşıyorsa, en eski elemanları temizle
            //if (totalItems > maxItems)
            //{
            //    m_plateResults.Clear(); 

            //    //int itemsToRemove = totalItems - maxItems;

            //    //for (int i = 0; i < itemsToRemove; i++)
            //    //{
            //    //    if (m_plateResults.Count > 0)
            //    //        m_plateResults.RemoveAt(0); // En eski elemanı sil
            //    //}

            //    //// Sayfa numarasını güncelle (Sayfa sayısını azaltmamız gerekecek)
            //    //int newTotalItems = m_plateResults.Count;
            //    //int newPageNumber = (int)Math.Ceiling((double)newTotalItems / pageSize);

            //    //// Eğer mevcut sayfa numarası yeni toplam sayfa sayısından büyükse, sayfa numarasını düzelt
            //    //currentPageNumber = Math.Min(currentPageNumber, newPageNumber);
            //}

            //// Eğer toplam eleman sayısı mevcut sayfa limitini aşıyorsa yeni sayfaya geç
            //if (totalItems > currentPageNumber * pageSize)
            //{
            //    currentPageNumber++;  // Yeni sayfaya geç
            //    DataGridViewPagingHelper.LoadPagedData(currentPageNumber, pageSize, m_dataGridViewPossiblePlateRegions);
            //}
            //else
            //{
            //    // Eleman hâlâ mevcut sayfa içinde, sadece son elemanı ekle
            //    DataGridViewPagingHelper.AddLastItemToGrid(m_dataGridViewPossiblePlateRegions);
            //}



            //var dsf = e.NewItems;
        }
      

        private readonly object _lock = new object();
        internal void m_dataGridViewPossiblePlateRegions_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //lock (_lock)
            //{
            ////try
            ////{
            #region Settings
            m_preProcessingSettings = m_preProcessingSettings.DeSerialize(m_preProcessingSettings);

            #endregion

            //if ((m_dataGridViewPossiblePlateRegions.Rows.Count !=0) && (m_possiblePlateRegion.Count <= m_dataGridViewPossiblePlateRegions.Rows.Count))
            if ((m_dataGridViewPossiblePlateRegions.Rows.Count != 0))

            {
                int selectedIndex = m_dataGridViewPossiblePlateRegions.SelectedRows[0].Index;

                //Mat possibleRegion = m_possiblePlateRegion[selectedIndex];

                Bitmap plate = (Bitmap)MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[selectedIndex].Cells[0].Value;
                Bitmap segmented = (Bitmap)MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[selectedIndex].Cells[1].Value;
                Bitmap threshould = (Bitmap)MainForm.m_mainForm.m_dataGridViewPossiblePlateRegions.Rows[selectedIndex].Cells[2].Value;



                //Mat odsf = RemoveBlueTRRegion(BitmapConverter.ToMat(plate));
                //  Cv2.Resize(odsf, odsf, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Lanczos4);

                //  Cv2.ImShow("dfsdf", odsf);


                //DisplayManager.PictureBoxInvoke(pictureBox6, plate);
                //DisplayManager.PictureBoxInvoke(pictureBox5, segmented);
                //DisplayManager.PictureBoxInvoke(pictureBox2, threshould);


                //m_characters = FindCharacterInPlateRegion(possibleRegion);

                if ((m_plateResults.Count > 0) && (m_plateResults[selectedIndex].m_characters.Count > 0))
                {
                    //m_numericUpDownCharacters.Value = 0;
                    //m_numericUpDownCharacters_ValueChanged(null, null);
                }

            }
            //}
            //catch
            //{

            //}
            //}
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Helper.LoadImage();

            LoadSVMModelForPlateRegion();

            LoadCNNModel();
        }

        private void m_listBoxPath_SelectedIndexChanged(object sender, EventArgs e)
        {

            #region Settings
            m_preProcessingSettings = m_preProcessingSettings.DeSerialize(m_preProcessingSettings);

            #endregion

            if (m_listBoxPath.SelectedItem != null)
            {
                string selectedFolderName = m_listBoxPath.SelectedItem.ToString();
                string parentDirectory = m_preProcessingSettings.m_ReadPlateFromImagePath;
                string fullPath = Path.Combine(parentDirectory, selectedFolderName);

                using (FileStream fs = new FileStream(fullPath, FileMode.Open))
                {
                    //DisplayManager.PictureBoxImageInvoke(m_pictureBoxFirstChannel, Bitmap.FromStream(fs));
                }



                Mat originalImage = new Mat(fullPath);

                //Mat gammaCorrected = new Mat();
                //originalImage.Clone().ConvertTo(gammaCorrected, MatType.CV_8UC1, alpha: 1, beta: -40);

                //DisplayManager.PictureBoxInvoke(MainForm.m_mainForm.pictureBox1, BitmapConverter.ToBitmap(gammaCorrected));


                ThreadPool.QueueUserWorkItem(OCRProcess.TEOkumaTestHibritTESTDetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages, (originalImage));
                //ThreadPool.QueueUserWorkItem(OCRProcess.NormalTESTDetectPlatesUsingMSERAndSobelOverGrayAndEdgeImages, (originalImage));
                //ThreadPool.QueueUserWorkItem(ImageAnalysisHelper.DetectPlates_MSERonGrayAndEdges_WithSobelContours, (originalImage));
                //ThreadPool.QueueUserWorkItem(ImageAnalysisHelper.DetectPlatesUsingSobel, (originalImage));



            }


        }
       

        public void LoadCNNModel()
        {

            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 20x20\\CNN BatchSize 64 Hidden Layer\\Epoch\\cnn_model_batchsize_64_epoch_10.json");
            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\Plaka Tanıma\\SVMTrain - CNN - Random Batch\\SVMTrain\\bin\\Debug\\CNN_32batchsize_1Conv_3x3-16_2Conv_3x3-128_Fully-64_epoch_15.json");
            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 20x20\\CNN-1.Conv_3x3-32,2.Conv_3x3-64,Fully 128-32 BatchSize\\SgdTrainer\\cnn_model_epoch_17.json");


            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 20x20\\CNN-1.Conv_3x3-32,2.Conv_3x3-64,Fully 128-32 BatchSize\\SgdTrainer\\cnn_model_epoch_28.json");

            //burası
            var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 20x20\\CNN-1.Conv_3x3-16,2.Conv_3x3-128,Fully 64-32 BatchSize\\SgdTrainer\\CNN_32batchsize_1Conv_3x3-16_2Conv_3x3-128_Fully-64_epoch_28.json");


            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 16x32 with Padding\\CNN-1.Conv_3x3-32,2.Conv_3x3-64,Fully 128-32 BatchSize\\SgdTrainer\\CNN 16x32_32batchsize_1Conv_3x3-32_2Conv_3x3-64_Fully-128_epoch_29.json");


            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 20x20\\CNN-1.Conv_3x3-48,2.Conv_3x3-96,Fully 192-32 BatchSize\\SgdTrainer\\CNN_32batchsize_1Conv_3x3-48_2Conv_3x3-96_Fully-192_epoch_29.json");


            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\PlateRecognation - Kopya\\PlateRecognation\\bin\\Debug\\net8.0-windows\\CNN 16x32_32batchsize_1Conv_3x3-32_2Conv_3x3-64_3Conv_3x3-128_Fully-128_epoch_4.json");


            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set 20x20\\CNN-1.Conv_3x3-32,2.Conv_3x3-64,3.Conv_3x3-128,Fully 128-32 BatchSize\\SgdTrainer\\CNN 20x20_32batchsize_1Conv_3x3-32_2Conv_3x3-64_3Conv_3x3-128_Fully-128_epoch_29.json");

            //var networkJSON = File.ReadAllText("D:\\Arge Projeler\\inputs and outputs\\2000 Characters Train Set\\CNN 12x20 - 1.Conv_3x3-32, 2.Conv_3x3-64, Fully 128-32 BatchSize\\SgdTrainer\\CNN_12x20_32batchsize_1Conv_3x3-32_2Conv_3x3-64_Fully-128_epoch_29.json");


            // JSON'u string türüne çevir
            //double deserializedString = JsonConvert.DeserializeObject<double>(networkJSON);

            m_loadedCNN = SerializationExtensions.FromJson<double>(networkJSON);
        }

     


        private void m_buttonFindPlateFromImage_Click(object sender, EventArgs e)
        {
            if (m_panelFindPlateFromImage.Width == 21)
            {
                m_panelFindPlateFromImage.Width = 331;
                m_buttonFindPlateFromImage.Text = "<";
            }
            else
            {
                m_panelFindPlateFromImage.Width = 21;
                m_buttonFindPlateFromImage.Text = ">";
            }

        }


      



    }



}
