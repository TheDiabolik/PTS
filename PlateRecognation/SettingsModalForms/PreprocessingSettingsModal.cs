using Accord.Statistics.Testing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlateRecognation.SettingsModalForms
{
    public partial class PreprocessingSettingsModal : Form
    {
        private static PreprocessingSettingsModal m_preprocessingSettingsModal;
        public MainForm m_mainForm;


        PreProcessingSettings m_preProcessing;


        public PreprocessingSettingsModal()
        {
            InitializeComponent();


            m_preProcessing = PreProcessingSettings.Singleton();
            m_preProcessing = m_preProcessing.DeSerialize(m_preProcessing);

            #region General
            m_textBoxReadPlateFromImagePath.Text = m_preProcessing.m_ReadPlateFromImagePath;
            m_textBoxReadPlateFromVideoPath.Text = m_preProcessing.m_ReadPlateFromVideoPath;

            m_checkBoxSignPlate.Checked = m_preProcessing.m_ShowPlate;

            m_numericUpDownMajorityVoiting.Value = m_preProcessing.m_MajorityVoiting;


            m_comboBoxChannel.SelectedIndex = 0;

            if (m_comboBoxChannel.SelectedIndex == 0)
            {
                if (m_preProcessing.m_OCRWorkingType.TryGetValue("1", out var workingType))
                {
                    m_radioButtonContinuous.Checked = (workingType == Enums.OCRWorkingType.Continuous);
                    m_radioButtonMotionSensitive.Checked = (workingType == Enums.OCRWorkingType.Motion);
                }
                else
                {
                    // Key yoksa varsayılan olarak Continuous seç
                    m_radioButtonContinuous.Checked = true;
                    m_radioButtonMotionSensitive.Checked = false;
                }
            }



            #endregion


            #region Preprocessing
            #region AdaptiveThreshold
            m_textBoxAdaptiveThresholdBlock.Text = m_preProcessing.m_adaptiveThreshouldBlock.ToCustomString();
            m_textBoxAdaptiveThreshouldC.Text = m_preProcessing.m_adaptiveThreshouldC.ToCustomString();

            if (m_preProcessing.m_AdaptiveThreshouldType == Enums.AdaptiveThreshouldType.Gaussian)
                m_radioButtonGaussian.Checked = true;
            else
                m_radioButtonMean.Checked = true;
            #endregion

            m_textBoxGaussianBlurKernel.Text = m_preProcessing.m_GaussianBlurKernel.ToCustomString();


            if (m_preProcessing.m_preProcessingType == Enums.PreProcessingType.BlurCLAHEOtsu)
                m_radioButtonBlurCLAHEOtsu.Checked = true;
            else if (m_preProcessing.m_preProcessingType == Enums.PreProcessingType.BlurCLAHEAdaptive)
                m_radioButtonBlurCLAHEAdaptive.Checked = true;
            else if (m_preProcessing.m_preProcessingType == Enums.PreProcessingType.BlurHistEqualizeOtsu)
                m_radioButtonBlurHistEqualizeOtsu.Checked = true;
            else
                m_radioButtonBlurHistEqualizeAdaptive.Checked = true;


            #endregion

            #region ImageAnalysis
            #region Plate
            m_textBoxPlateMinWidth.Text = m_preProcessing.m_plateMinWidth.ToCustomString();
            m_textBoxPlateMaxWidth.Text = m_preProcessing.m_plateMaxWidth.ToCustomString();

            m_textBoxPlateMinHeight.Text = m_preProcessing.m_plateMinHeight.ToCustomString();
            m_textBoxPlateMaxHeight.Text = m_preProcessing.m_plateMaxHeight.ToCustomString();

            m_textBoxPlateMinAspectRatio.Text = m_preProcessing.m_plateMinAspectRatio.ToFormattedString();
            m_textBoxPlateMaxAspectRatio.Text = m_preProcessing.m_plateMaxAspectRatio.ToFormattedString();

            m_textBoxPlateMinArea.Text = m_preProcessing.m_plateMinArea.ToCustomString();
            m_textBoxPlateMaxArea.Text = m_preProcessing.m_plateMaxArea.ToCustomString();

            #endregion

            #region Characters
            m_textBoxCharacterMinWidth.Text = m_preProcessing.m_characterMinWidth.ToCustomString();
            m_textBoxCharacterMaxWidth.Text = m_preProcessing.m_characterMaxWidth.ToCustomString();

            m_textBoxCharacterMinHeight.Text = m_preProcessing.m_characterMinHeight.ToCustomString();
            m_textBoxCharacterMaxHeight.Text = m_preProcessing.m_characterMaxHeight.ToCustomString();

            m_textBoxCharacterMinAspectRatio.Text = m_preProcessing.m_characterMinAspectRatio.ToFormattedString();
            m_textBoxCharacterMaxAspectRatio.Text = m_preProcessing.m_characterMaxAspectRatio.ToFormattedString();

            m_textBoxCharacterMinArea.Text = m_preProcessing.m_characterMinArea.ToCustomString();
            m_textBoxCharacterMaxArea.Text = m_preProcessing.m_characterMaxArea.ToCustomString();

            m_textBoxCharacterMinDiagonalLength.Text = m_preProcessing.m_characterMinDiagonalLength.ToFormattedString();
            m_textBoxCharacterMaxDiagonalLength.Text = m_preProcessing.m_characterMaxDiagonalLength.ToFormattedString();

            #endregion
            #endregion

            #region Plate Reading
            m_checkBoxAutoLightControl.Checked = m_preProcessing.m_AutoLightControl;
            m_checkBoxWhiteBalanced.Checked = m_preProcessing.m_AutoWhiteBalance;
            m_checkBoxFindMovementPlate.Checked = m_preProcessing.m_FindMovementPlate;

            if (m_preProcessing.m_PlateType == Enums.PlateType.Turkish)
                m_radioButtonTurkishPlate.Checked = true;
            else
                m_radioButtonAllPlateType.Checked = true;


            m_checkBoxFixOCRErrors.Checked = m_preProcessing.m_FixOCRErrors;
            m_checkBoxSelectBestResult.Checked = m_preProcessing.m_SelectBestResult;

            #endregion
        }

        private PreprocessingSettingsModal(MainForm mainForm)
    : this()
        {
            m_mainForm = mainForm;
        }

        public static PreprocessingSettingsModal Singleton(MainForm mainForm)
        {
            if (m_preprocessingSettingsModal == null)
                m_preprocessingSettingsModal = new PreprocessingSettingsModal(mainForm);

            return m_preprocessingSettingsModal;
        }

        private void PreprocessingSettingsModal_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_preprocessingSettingsModal = null;
        }

        private void m_buttons_Click(object sender, EventArgs e)
        {

            Button myButton = (Button)sender;

            if (myButton == m_buttonReadPlateFromImagePath)
            {
                if (m_folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    m_textBoxReadPlateFromImagePath.Text = m_folderBrowserDialog.SelectedPath;

                }
            }
            else
            {
                if (m_folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    m_textBoxReadPlateFromVideoPath.Text = m_folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void m_buttonSave_Click(object sender, EventArgs e)
        {
            #region General
            m_preProcessing.m_ReadPlateFromImagePath = m_textBoxReadPlateFromImagePath.Text;
            m_preProcessing.m_ReadPlateFromVideoPath = m_textBoxReadPlateFromVideoPath.Text;

            m_preProcessing.m_ShowPlate = m_checkBoxSignPlate.Checked;

            m_preProcessing.m_MajorityVoiting = (int)m_numericUpDownMajorityVoiting.Value;


            string channelName = m_comboBoxChannel.GetItemText(m_comboBoxChannel.SelectedItem);

            if (m_radioButtonContinuous.Checked)
            {
                m_preProcessing.m_OCRWorkingType[channelName] = Enums.OCRWorkingType.Continuous;
            }
            else
            {
                m_preProcessing.m_OCRWorkingType[channelName] = Enums.OCRWorkingType.Motion;
            }


            #endregion

            #region PreProcessing

            #region AdaptiveThreshold
            m_preProcessing.m_adaptiveThreshouldBlock = int.Parse(m_textBoxAdaptiveThresholdBlock.Text);
            m_preProcessing.m_adaptiveThreshouldC = int.Parse(m_textBoxAdaptiveThreshouldC.Text);

            if (m_radioButtonGaussian.Checked)
                m_preProcessing.m_AdaptiveThreshouldType = Enums.AdaptiveThreshouldType.Gaussian;
            else
                m_preProcessing.m_AdaptiveThreshouldType = Enums.AdaptiveThreshouldType.Mean;
            #endregion

            m_preProcessing.m_GaussianBlurKernel = int.Parse(m_textBoxGaussianBlurKernel.Text);


            if (m_radioButtonBlurCLAHEOtsu.Checked)
                m_preProcessing.m_preProcessingType = Enums.PreProcessingType.BlurCLAHEOtsu;
            else if (m_radioButtonBlurCLAHEAdaptive.Checked)
                m_preProcessing.m_preProcessingType = Enums.PreProcessingType.BlurCLAHEAdaptive;
            else if (m_radioButtonBlurHistEqualizeOtsu.Checked)
                m_preProcessing.m_preProcessingType = Enums.PreProcessingType.BlurHistEqualizeOtsu;
            else
                m_preProcessing.m_preProcessingType = Enums.PreProcessingType.BlurHistEqualizeAdaptive;
            #endregion

            #region ImageAnalysis

            #region Plate

            m_preProcessing.m_plateMinWidth = int.Parse(m_textBoxPlateMinWidth.Text);
            m_preProcessing.m_plateMaxWidth = int.Parse(m_textBoxPlateMaxWidth.Text);

            m_preProcessing.m_plateMinHeight = int.Parse(m_textBoxPlateMinHeight.Text);
            m_preProcessing.m_plateMaxHeight = int.Parse(m_textBoxPlateMaxHeight.Text);

            m_preProcessing.m_plateMinAspectRatio = double.Parse(m_textBoxPlateMinAspectRatio.Text);
            m_preProcessing.m_plateMaxAspectRatio = double.Parse(m_textBoxPlateMaxAspectRatio.Text);

            m_preProcessing.m_plateMinArea = int.Parse(m_textBoxPlateMinArea.Text);
            m_preProcessing.m_plateMaxArea = int.Parse(m_textBoxPlateMaxArea.Text);

            #endregion

            #region Characters

            m_preProcessing.m_characterMinWidth = int.Parse(m_textBoxCharacterMinWidth.Text);
            m_preProcessing.m_characterMaxWidth = int.Parse(m_textBoxCharacterMaxWidth.Text);

            m_preProcessing.m_characterMinHeight = int.Parse(m_textBoxCharacterMinHeight.Text);
            m_preProcessing.m_characterMaxHeight = int.Parse(m_textBoxCharacterMaxHeight.Text);

            m_preProcessing.m_characterMinAspectRatio = double.Parse(m_textBoxCharacterMinAspectRatio.Text);
            m_preProcessing.m_characterMaxAspectRatio = double.Parse(m_textBoxCharacterMaxAspectRatio.Text);

            m_preProcessing.m_characterMinArea = int.Parse(m_textBoxCharacterMinArea.Text);
            m_preProcessing.m_characterMaxArea = int.Parse(m_textBoxCharacterMaxArea.Text);

            m_preProcessing.m_characterMinDiagonalLength = double.Parse(m_textBoxCharacterMinDiagonalLength.Text);
            m_preProcessing.m_characterMaxDiagonalLength = double.Parse(m_textBoxCharacterMaxDiagonalLength.Text);

            #endregion
            #endregion


            #region Plate Reading
            m_preProcessing.m_AutoLightControl = m_checkBoxAutoLightControl.Checked;
            m_preProcessing.m_AutoWhiteBalance = m_checkBoxWhiteBalanced.Checked;
            m_preProcessing.m_FindMovementPlate = m_checkBoxFindMovementPlate.Checked;

            if (m_radioButtonTurkishPlate.Checked)
                m_preProcessing.m_PlateType = Enums.PlateType.Turkish;
            else
                m_preProcessing.m_PlateType = Enums.PlateType.All;


            m_preProcessing.m_FixOCRErrors = m_checkBoxFixOCRErrors.Checked;
            m_preProcessing.m_SelectBestResult = m_checkBoxSelectBestResult.Checked;

            #endregion


            m_preProcessing.Serialize(m_preProcessing);

            m_preProcessing = m_preProcessing.DeSerialize(m_preProcessing);



            //MainForm.m_mainForm.m_signPlate = m_preProcessing.m_ShowPlate;
            //MainForm.m_mainForm.maxFramesToAggregate = m_preProcessing.m_MajorityVoiting;

            Helper.LoadImage();



            Button button = (Button)sender;

            if (button == m_buttonApply)
            {
                this.Close();
            }
        }
        private void m_groupBoxAdaptiveTreshould_Enter(object sender, EventArgs e)
        {

        }

        private void PreprocessingSettingsModal_Load(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Paint(object sender, PaintEventArgs e)
        {
            Point[] p = new Point[3];

            p[0] = new Point(splitContainer1.SplitterDistance, splitContainer1.Height / 2 - splitContainer1.SplitterWidth * 2 / 3);
            p[1] = new Point(splitContainer1.SplitterDistance, splitContainer1.Height / 2 + splitContainer1.SplitterWidth * 2 / 3);
            p[2] = new Point(splitContainer1.SplitterDistance + splitContainer1.SplitterWidth, splitContainer1.Height / 2);

            e.Graphics.FillPolygon(Brushes.Gray, p);
        }

        private void splitContainer1_Resize(object sender, EventArgs e)
        {
            splitContainer1.Refresh();
        }

        private void m_radioButtonTurkishPlate_CheckedChanged(object sender, EventArgs e)
        {
            //if(m_radioButtonTurkishPlate.Checked) {
            m_checkBoxFixOCRErrors.Enabled = m_radioButtonTurkishPlate.Checked;
        }

        private void m_comboBoxChannel_SelectedIndexChanged(object sender, EventArgs e)
        {
            string channelName = m_comboBoxChannel.GetItemText(m_comboBoxChannel.SelectedItem);

            if (string.IsNullOrWhiteSpace(channelName))
                return;

            if (m_preProcessing.m_OCRWorkingType.TryGetValue(channelName, out var workingType))
            {
                if (channelName is "1" or "2" or "3" or "4")
                {
                    m_radioButtonContinuous.Checked = (workingType == Enums.OCRWorkingType.Continuous);
                    m_radioButtonMotionSensitive.Checked = !m_radioButtonContinuous.Checked;
                }
            }
        }



       
    }
}
