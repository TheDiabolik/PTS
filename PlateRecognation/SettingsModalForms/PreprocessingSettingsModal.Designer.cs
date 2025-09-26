namespace PlateRecognation.SettingsModalForms
{
    partial class PreprocessingSettingsModal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            m_groupBoxPreprocessing = new GroupBox();
            m_textBoxGaussianBlurKernel = new TextBox();
            m_labelGaussianBlurKernel = new Label();
            m_labelAdaptiveThresholdBlock = new Label();
            m_labelAdaptiveThreshouldC = new Label();
            label4 = new Label();
            label5 = new Label();
            m_textBoxAdaptiveThresholdBlock = new TextBox();
            m_textBoxAdaptiveThreshouldC = new TextBox();
            textBox4 = new TextBox();
            textBox5 = new TextBox();
            m_radioButtonGaussian = new RadioButton();
            m_radioButtonMean = new RadioButton();
            m_groupBoxAdaptiveTreshould = new GroupBox();
            m_buttonApply = new Button();
            m_buttonSave = new Button();
            m_tabControlSettings = new TabControl();
            m_tabPageGeneral = new TabPage();
            m_groupBoxWorkingType = new GroupBox();
            m_comboBoxChannel = new ComboBox();
            m_radioButtonContinuous = new RadioButton();
            m_radioButtonMotionSensitive = new RadioButton();
            m_labelChannelNumber = new Label();
            m_numericUpDownMajorityVoiting = new NumericUpDown();
            m_labelMajorityVoiting = new Label();
            m_checkBoxSignPlate = new CheckBox();
            groupBox3 = new GroupBox();
            m_textBoxReadPlateFromVideoPath = new TextBox();
            m_labelReadPlateFromVideoPath = new Label();
            m_buttonReadPlateFromVideoPath = new Button();
            m_textBoxReadPlateFromImagePath = new TextBox();
            m_labelReadPlateFromImagePath = new Label();
            m_buttonReadPlateFromImagePath = new Button();
            m_tabPagePreprocessing = new TabPage();
            m_groupBoxProcessingType = new GroupBox();
            m_radioButtonBlurHistEqualizeAdaptive = new RadioButton();
            m_radioButtonBlurCLAHEOtsu = new RadioButton();
            m_radioButtonBlurHistEqualizeOtsu = new RadioButton();
            m_radioButtonBlurCLAHEAdaptive = new RadioButton();
            m_tabPageImageAnalysis = new TabPage();
            groupBox1 = new GroupBox();
            label1 = new Label();
            m_textBoxPlateMaxArea = new TextBox();
            m_textBoxPlateMinArea = new TextBox();
            label2 = new Label();
            label3 = new Label();
            m_textBoxPlateMaxAspectRatio = new TextBox();
            m_textBoxPlateMinAspectRatio = new TextBox();
            label6 = new Label();
            label7 = new Label();
            m_textBoxPlateMaxHeight = new TextBox();
            m_textBoxPlateMinHeight = new TextBox();
            label8 = new Label();
            label9 = new Label();
            m_textBoxPlateMaxWidth = new TextBox();
            m_textBoxPlateMinWidth = new TextBox();
            label10 = new Label();
            groupBox2 = new GroupBox();
            label11 = new Label();
            m_textBoxCharacterMaxDiagonalLength = new TextBox();
            m_textBoxCharacterMinDiagonalLength = new TextBox();
            label12 = new Label();
            label13 = new Label();
            m_textBoxCharacterMaxArea = new TextBox();
            m_textBoxCharacterMinArea = new TextBox();
            label14 = new Label();
            label15 = new Label();
            m_textBoxCharacterMaxAspectRatio = new TextBox();
            m_textBoxCharacterMinAspectRatio = new TextBox();
            label16 = new Label();
            label17 = new Label();
            m_textBoxCharacterMaxHeight = new TextBox();
            m_textBoxCharacterMinHeight = new TextBox();
            label18 = new Label();
            label19 = new Label();
            m_textBoxCharacterMaxWidth = new TextBox();
            m_textBoxCharacterMinWidth = new TextBox();
            label20 = new Label();
            m_tabPagePlate = new TabPage();
            m_groupBoxPlateType = new GroupBox();
            m_checkBoxSelectBestResult = new CheckBox();
            m_checkBoxFixOCRErrors = new CheckBox();
            m_radioButtonAllPlateType = new RadioButton();
            m_radioButtonTurkishPlate = new RadioButton();
            m_checkBoxFindMovementPlate = new CheckBox();
            m_checkBoxWhiteBalanced = new CheckBox();
            m_checkBoxAutoLightControl = new CheckBox();
            m_folderBrowserDialog = new FolderBrowserDialog();
            splitContainer1 = new SplitContainer();
            m_groupBoxPreprocessing.SuspendLayout();
            m_groupBoxAdaptiveTreshould.SuspendLayout();
            m_tabControlSettings.SuspendLayout();
            m_tabPageGeneral.SuspendLayout();
            m_groupBoxWorkingType.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)m_numericUpDownMajorityVoiting).BeginInit();
            groupBox3.SuspendLayout();
            m_tabPagePreprocessing.SuspendLayout();
            m_groupBoxProcessingType.SuspendLayout();
            m_tabPageImageAnalysis.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            m_tabPagePlate.SuspendLayout();
            m_groupBoxPlateType.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // m_groupBoxPreprocessing
            // 
            m_groupBoxPreprocessing.Controls.Add(m_textBoxGaussianBlurKernel);
            m_groupBoxPreprocessing.Controls.Add(m_labelGaussianBlurKernel);
            m_groupBoxPreprocessing.Location = new Point(8, 6);
            m_groupBoxPreprocessing.Name = "m_groupBoxPreprocessing";
            m_groupBoxPreprocessing.Size = new Size(324, 60);
            m_groupBoxPreprocessing.TabIndex = 0;
            m_groupBoxPreprocessing.TabStop = false;
            m_groupBoxPreprocessing.Text = "Ön İşleme";
            // 
            // m_textBoxGaussianBlurKernel
            // 
            m_textBoxGaussianBlurKernel.Location = new Point(192, 19);
            m_textBoxGaussianBlurKernel.Name = "m_textBoxGaussianBlurKernel";
            m_textBoxGaussianBlurKernel.Size = new Size(100, 23);
            m_textBoxGaussianBlurKernel.TabIndex = 5;
            // 
            // m_labelGaussianBlurKernel
            // 
            m_labelGaussianBlurKernel.AutoSize = true;
            m_labelGaussianBlurKernel.Location = new Point(25, 19);
            m_labelGaussianBlurKernel.Name = "m_labelGaussianBlurKernel";
            m_labelGaussianBlurKernel.Size = new Size(123, 15);
            m_labelGaussianBlurKernel.TabIndex = 0;
            m_labelGaussianBlurKernel.Text = "Gaussian Blur Kernel : ";
            // 
            // m_labelAdaptiveThresholdBlock
            // 
            m_labelAdaptiveThresholdBlock.AutoSize = true;
            m_labelAdaptiveThresholdBlock.Location = new Point(36, 27);
            m_labelAdaptiveThresholdBlock.Name = "m_labelAdaptiveThresholdBlock";
            m_labelAdaptiveThresholdBlock.Size = new Size(45, 15);
            m_labelAdaptiveThresholdBlock.TabIndex = 1;
            m_labelAdaptiveThresholdBlock.Text = "Block : ";
            // 
            // m_labelAdaptiveThreshouldC
            // 
            m_labelAdaptiveThreshouldC.AutoSize = true;
            m_labelAdaptiveThreshouldC.Location = new Point(56, 53);
            m_labelAdaptiveThreshouldC.Name = "m_labelAdaptiveThreshouldC";
            m_labelAdaptiveThreshouldC.Size = new Size(24, 15);
            m_labelAdaptiveThreshouldC.TabIndex = 2;
            m_labelAdaptiveThreshouldC.Text = "C : ";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(85, 272);
            label4.Name = "label4";
            label4.Size = new Size(38, 15);
            label4.TabIndex = 3;
            label4.Text = "label4";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(143, 272);
            label5.Name = "label5";
            label5.Size = new Size(38, 15);
            label5.TabIndex = 4;
            label5.Text = "label5";
            // 
            // m_textBoxAdaptiveThresholdBlock
            // 
            m_textBoxAdaptiveThresholdBlock.Location = new Point(125, 24);
            m_textBoxAdaptiveThresholdBlock.Name = "m_textBoxAdaptiveThresholdBlock";
            m_textBoxAdaptiveThresholdBlock.Size = new Size(174, 23);
            m_textBoxAdaptiveThresholdBlock.TabIndex = 6;
            // 
            // m_textBoxAdaptiveThreshouldC
            // 
            m_textBoxAdaptiveThreshouldC.Location = new Point(125, 50);
            m_textBoxAdaptiveThreshouldC.Name = "m_textBoxAdaptiveThreshouldC";
            m_textBoxAdaptiveThreshouldC.Size = new Size(174, 23);
            m_textBoxAdaptiveThreshouldC.TabIndex = 7;
            // 
            // textBox4
            // 
            textBox4.Location = new Point(146, 303);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(100, 23);
            textBox4.TabIndex = 8;
            // 
            // textBox5
            // 
            textBox5.Location = new Point(24, 303);
            textBox5.Name = "textBox5";
            textBox5.Size = new Size(100, 23);
            textBox5.TabIndex = 9;
            // 
            // m_radioButtonGaussian
            // 
            m_radioButtonGaussian.AutoSize = true;
            m_radioButtonGaussian.Location = new Point(125, 81);
            m_radioButtonGaussian.Name = "m_radioButtonGaussian";
            m_radioButtonGaussian.Size = new Size(72, 19);
            m_radioButtonGaussian.TabIndex = 10;
            m_radioButtonGaussian.TabStop = true;
            m_radioButtonGaussian.Text = "Gaussian";
            m_radioButtonGaussian.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonMean
            // 
            m_radioButtonMean.AutoSize = true;
            m_radioButtonMean.Location = new Point(247, 81);
            m_radioButtonMean.Name = "m_radioButtonMean";
            m_radioButtonMean.Size = new Size(55, 19);
            m_radioButtonMean.TabIndex = 11;
            m_radioButtonMean.TabStop = true;
            m_radioButtonMean.Text = "Mean";
            m_radioButtonMean.UseVisualStyleBackColor = true;
            // 
            // m_groupBoxAdaptiveTreshould
            // 
            m_groupBoxAdaptiveTreshould.Controls.Add(m_radioButtonMean);
            m_groupBoxAdaptiveTreshould.Controls.Add(m_labelAdaptiveThresholdBlock);
            m_groupBoxAdaptiveTreshould.Controls.Add(m_textBoxAdaptiveThreshouldC);
            m_groupBoxAdaptiveTreshould.Controls.Add(m_radioButtonGaussian);
            m_groupBoxAdaptiveTreshould.Controls.Add(m_textBoxAdaptiveThresholdBlock);
            m_groupBoxAdaptiveTreshould.Controls.Add(m_labelAdaptiveThreshouldC);
            m_groupBoxAdaptiveTreshould.Location = new Point(8, 72);
            m_groupBoxAdaptiveTreshould.Name = "m_groupBoxAdaptiveTreshould";
            m_groupBoxAdaptiveTreshould.Size = new Size(324, 117);
            m_groupBoxAdaptiveTreshould.TabIndex = 12;
            m_groupBoxAdaptiveTreshould.TabStop = false;
            m_groupBoxAdaptiveTreshould.Text = "Adaptive Treshould";
            m_groupBoxAdaptiveTreshould.Enter += m_groupBoxAdaptiveTreshould_Enter;
            // 
            // m_buttonApply
            // 
            m_buttonApply.Image = Properties.Resources.apply;
            m_buttonApply.ImageAlign = ContentAlignment.MiddleRight;
            m_buttonApply.Location = new Point(623, 288);
            m_buttonApply.Margin = new Padding(2);
            m_buttonApply.Name = "m_buttonApply";
            m_buttonApply.Size = new Size(83, 48);
            m_buttonApply.TabIndex = 23;
            m_buttonApply.Text = "Uygula";
            m_buttonApply.TextImageRelation = TextImageRelation.ImageBeforeText;
            m_buttonApply.UseVisualStyleBackColor = true;
            m_buttonApply.Click += m_buttonSave_Click;
            // 
            // m_buttonSave
            // 
            m_buttonSave.Image = Properties.Resources.save;
            m_buttonSave.Location = new Point(534, 288);
            m_buttonSave.Margin = new Padding(2);
            m_buttonSave.Name = "m_buttonSave";
            m_buttonSave.Size = new Size(83, 48);
            m_buttonSave.TabIndex = 22;
            m_buttonSave.Text = "Kaydet";
            m_buttonSave.TextAlign = ContentAlignment.MiddleRight;
            m_buttonSave.TextImageRelation = TextImageRelation.ImageBeforeText;
            m_buttonSave.UseVisualStyleBackColor = true;
            m_buttonSave.Click += m_buttonSave_Click;
            // 
            // m_tabControlSettings
            // 
            m_tabControlSettings.Controls.Add(m_tabPageGeneral);
            m_tabControlSettings.Controls.Add(m_tabPagePreprocessing);
            m_tabControlSettings.Controls.Add(m_tabPageImageAnalysis);
            m_tabControlSettings.Controls.Add(m_tabPagePlate);
            m_tabControlSettings.Location = new Point(3, 10);
            m_tabControlSettings.Name = "m_tabControlSettings";
            m_tabControlSettings.SelectedIndex = 0;
            m_tabControlSettings.Size = new Size(752, 259);
            m_tabControlSettings.TabIndex = 24;
            // 
            // m_tabPageGeneral
            // 
            m_tabPageGeneral.Controls.Add(m_groupBoxWorkingType);
            m_tabPageGeneral.Controls.Add(m_numericUpDownMajorityVoiting);
            m_tabPageGeneral.Controls.Add(m_labelMajorityVoiting);
            m_tabPageGeneral.Controls.Add(m_checkBoxSignPlate);
            m_tabPageGeneral.Controls.Add(groupBox3);
            m_tabPageGeneral.Location = new Point(4, 24);
            m_tabPageGeneral.Name = "m_tabPageGeneral";
            m_tabPageGeneral.Padding = new Padding(3);
            m_tabPageGeneral.Size = new Size(744, 231);
            m_tabPageGeneral.TabIndex = 2;
            m_tabPageGeneral.Text = "Genel";
            m_tabPageGeneral.UseVisualStyleBackColor = true;
            // 
            // m_groupBoxWorkingType
            // 
            m_groupBoxWorkingType.Controls.Add(m_comboBoxChannel);
            m_groupBoxWorkingType.Controls.Add(m_radioButtonContinuous);
            m_groupBoxWorkingType.Controls.Add(m_radioButtonMotionSensitive);
            m_groupBoxWorkingType.Controls.Add(m_labelChannelNumber);
            m_groupBoxWorkingType.Location = new Point(480, 6);
            m_groupBoxWorkingType.Name = "m_groupBoxWorkingType";
            m_groupBoxWorkingType.Size = new Size(258, 154);
            m_groupBoxWorkingType.TabIndex = 24;
            m_groupBoxWorkingType.TabStop = false;
            m_groupBoxWorkingType.Text = "Çalışma Ayarları";
            // 
            // m_comboBoxChannel
            // 
            m_comboBoxChannel.FormattingEnabled = true;
            m_comboBoxChannel.Items.AddRange(new object[] { "1", "2", "3", "4" });
            m_comboBoxChannel.Location = new Point(87, 27);
            m_comboBoxChannel.Name = "m_comboBoxChannel";
            m_comboBoxChannel.Size = new Size(121, 23);
            m_comboBoxChannel.TabIndex = 25;
            m_comboBoxChannel.SelectedIndexChanged += m_comboBoxChannel_SelectedIndexChanged;
            // 
            // m_radioButtonContinuous
            // 
            m_radioButtonContinuous.AutoSize = true;
            m_radioButtonContinuous.Location = new Point(148, 79);
            m_radioButtonContinuous.Name = "m_radioButtonContinuous";
            m_radioButtonContinuous.Size = new Size(60, 19);
            m_radioButtonContinuous.TabIndex = 26;
            m_radioButtonContinuous.TabStop = true;
            m_radioButtonContinuous.Text = "Sürekli";
            m_radioButtonContinuous.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonMotionSensitive
            // 
            m_radioButtonMotionSensitive.AutoSize = true;
            m_radioButtonMotionSensitive.ImageAlign = ContentAlignment.MiddleRight;
            m_radioButtonMotionSensitive.Location = new Point(17, 79);
            m_radioButtonMotionSensitive.Name = "m_radioButtonMotionSensitive";
            m_radioButtonMotionSensitive.Size = new Size(116, 19);
            m_radioButtonMotionSensitive.TabIndex = 26;
            m_radioButtonMotionSensitive.TabStop = true;
            m_radioButtonMotionSensitive.Text = "Hareket Algılama";
            m_radioButtonMotionSensitive.UseVisualStyleBackColor = true;
            // 
            // m_labelChannelNumber
            // 
            m_labelChannelNumber.AutoSize = true;
            m_labelChannelNumber.Location = new Point(17, 27);
            m_labelChannelNumber.Name = "m_labelChannelNumber";
            m_labelChannelNumber.Size = new Size(64, 15);
            m_labelChannelNumber.TabIndex = 0;
            m_labelChannelNumber.Text = "Kanal No : ";
            // 
            // m_numericUpDownMajorityVoiting
            // 
            m_numericUpDownMajorityVoiting.Location = new Point(172, 108);
            m_numericUpDownMajorityVoiting.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            m_numericUpDownMajorityVoiting.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            m_numericUpDownMajorityVoiting.Name = "m_numericUpDownMajorityVoiting";
            m_numericUpDownMajorityVoiting.Size = new Size(232, 23);
            m_numericUpDownMajorityVoiting.TabIndex = 23;
            m_numericUpDownMajorityVoiting.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // m_labelMajorityVoiting
            // 
            m_labelMajorityVoiting.AutoSize = true;
            m_labelMajorityVoiting.Location = new Point(17, 110);
            m_labelMajorityVoiting.Name = "m_labelMajorityVoiting";
            m_labelMajorityVoiting.Size = new Size(122, 15);
            m_labelMajorityVoiting.TabIndex = 22;
            m_labelMajorityVoiting.Text = "Plaka Tahmin Frame : ";
            // 
            // m_checkBoxSignPlate
            // 
            m_checkBoxSignPlate.AutoSize = true;
            m_checkBoxSignPlate.Location = new Point(17, 173);
            m_checkBoxSignPlate.Name = "m_checkBoxSignPlate";
            m_checkBoxSignPlate.Size = new Size(94, 19);
            m_checkBoxSignPlate.TabIndex = 21;
            m_checkBoxSignPlate.Text = "Plaka İşaretle";
            m_checkBoxSignPlate.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(m_textBoxReadPlateFromVideoPath);
            groupBox3.Controls.Add(m_labelReadPlateFromVideoPath);
            groupBox3.Controls.Add(m_buttonReadPlateFromVideoPath);
            groupBox3.Controls.Add(m_textBoxReadPlateFromImagePath);
            groupBox3.Controls.Add(m_labelReadPlateFromImagePath);
            groupBox3.Controls.Add(m_buttonReadPlateFromImagePath);
            groupBox3.Location = new Point(6, 6);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(468, 82);
            groupBox3.TabIndex = 20;
            groupBox3.TabStop = false;
            groupBox3.Text = "Dosya Yolu";
            // 
            // m_textBoxReadPlateFromVideoPath
            // 
            m_textBoxReadPlateFromVideoPath.Location = new Point(166, 45);
            m_textBoxReadPlateFromVideoPath.Name = "m_textBoxReadPlateFromVideoPath";
            m_textBoxReadPlateFromVideoPath.ReadOnly = true;
            m_textBoxReadPlateFromVideoPath.Size = new Size(232, 23);
            m_textBoxReadPlateFromVideoPath.TabIndex = 11;
            // 
            // m_labelReadPlateFromVideoPath
            // 
            m_labelReadPlateFromVideoPath.AutoSize = true;
            m_labelReadPlateFromVideoPath.Location = new Point(48, 48);
            m_labelReadPlateFromVideoPath.Name = "m_labelReadPlateFromVideoPath";
            m_labelReadPlateFromVideoPath.Size = new Size(46, 15);
            m_labelReadPlateFromVideoPath.TabIndex = 13;
            m_labelReadPlateFromVideoPath.Text = "Video : ";
            // 
            // m_buttonReadPlateFromVideoPath
            // 
            m_buttonReadPlateFromVideoPath.Location = new Point(423, 44);
            m_buttonReadPlateFromVideoPath.Name = "m_buttonReadPlateFromVideoPath";
            m_buttonReadPlateFromVideoPath.Size = new Size(29, 23);
            m_buttonReadPlateFromVideoPath.TabIndex = 12;
            m_buttonReadPlateFromVideoPath.Text = "...";
            m_buttonReadPlateFromVideoPath.UseVisualStyleBackColor = true;
            m_buttonReadPlateFromVideoPath.Click += m_buttons_Click;
            // 
            // m_textBoxReadPlateFromImagePath
            // 
            m_textBoxReadPlateFromImagePath.Location = new Point(166, 19);
            m_textBoxReadPlateFromImagePath.Name = "m_textBoxReadPlateFromImagePath";
            m_textBoxReadPlateFromImagePath.ReadOnly = true;
            m_textBoxReadPlateFromImagePath.Size = new Size(232, 23);
            m_textBoxReadPlateFromImagePath.TabIndex = 0;
            // 
            // m_labelReadPlateFromImagePath
            // 
            m_labelReadPlateFromImagePath.AutoSize = true;
            m_labelReadPlateFromImagePath.Location = new Point(36, 22);
            m_labelReadPlateFromImagePath.Name = "m_labelReadPlateFromImagePath";
            m_labelReadPlateFromImagePath.Size = new Size(61, 15);
            m_labelReadPlateFromImagePath.TabIndex = 10;
            m_labelReadPlateFromImagePath.Text = "Resimler : ";
            // 
            // m_buttonReadPlateFromImagePath
            // 
            m_buttonReadPlateFromImagePath.Location = new Point(423, 18);
            m_buttonReadPlateFromImagePath.Name = "m_buttonReadPlateFromImagePath";
            m_buttonReadPlateFromImagePath.Size = new Size(29, 23);
            m_buttonReadPlateFromImagePath.TabIndex = 1;
            m_buttonReadPlateFromImagePath.Text = "...";
            m_buttonReadPlateFromImagePath.UseVisualStyleBackColor = true;
            m_buttonReadPlateFromImagePath.Click += m_buttons_Click;
            // 
            // m_tabPagePreprocessing
            // 
            m_tabPagePreprocessing.Controls.Add(m_groupBoxProcessingType);
            m_tabPagePreprocessing.Controls.Add(m_groupBoxPreprocessing);
            m_tabPagePreprocessing.Controls.Add(m_groupBoxAdaptiveTreshould);
            m_tabPagePreprocessing.Location = new Point(4, 24);
            m_tabPagePreprocessing.Name = "m_tabPagePreprocessing";
            m_tabPagePreprocessing.Padding = new Padding(3);
            m_tabPagePreprocessing.Size = new Size(744, 231);
            m_tabPagePreprocessing.TabIndex = 0;
            m_tabPagePreprocessing.Text = "Ön İşleme";
            m_tabPagePreprocessing.UseVisualStyleBackColor = true;
            // 
            // m_groupBoxProcessingType
            // 
            m_groupBoxProcessingType.Controls.Add(m_radioButtonBlurHistEqualizeAdaptive);
            m_groupBoxProcessingType.Controls.Add(m_radioButtonBlurCLAHEOtsu);
            m_groupBoxProcessingType.Controls.Add(m_radioButtonBlurHistEqualizeOtsu);
            m_groupBoxProcessingType.Controls.Add(m_radioButtonBlurCLAHEAdaptive);
            m_groupBoxProcessingType.Location = new Point(338, 6);
            m_groupBoxProcessingType.Name = "m_groupBoxProcessingType";
            m_groupBoxProcessingType.Size = new Size(200, 183);
            m_groupBoxProcessingType.TabIndex = 38;
            m_groupBoxProcessingType.TabStop = false;
            m_groupBoxProcessingType.Text = "Ön İşleme Tarzı";
            // 
            // m_radioButtonBlurHistEqualizeAdaptive
            // 
            m_radioButtonBlurHistEqualizeAdaptive.AutoSize = true;
            m_radioButtonBlurHistEqualizeAdaptive.Location = new Point(38, 130);
            m_radioButtonBlurHistEqualizeAdaptive.Name = "m_radioButtonBlurHistEqualizeAdaptive";
            m_radioButtonBlurHistEqualizeAdaptive.Size = new Size(157, 19);
            m_radioButtonBlurHistEqualizeAdaptive.TabIndex = 50;
            m_radioButtonBlurHistEqualizeAdaptive.Text = "BlurHistEqualizeAdaptive";
            m_radioButtonBlurHistEqualizeAdaptive.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonBlurCLAHEOtsu
            // 
            m_radioButtonBlurCLAHEOtsu.AutoSize = true;
            m_radioButtonBlurCLAHEOtsu.Checked = true;
            m_radioButtonBlurCLAHEOtsu.Location = new Point(38, 33);
            m_radioButtonBlurCLAHEOtsu.Name = "m_radioButtonBlurCLAHEOtsu";
            m_radioButtonBlurCLAHEOtsu.Size = new Size(108, 19);
            m_radioButtonBlurCLAHEOtsu.TabIndex = 47;
            m_radioButtonBlurCLAHEOtsu.TabStop = true;
            m_radioButtonBlurCLAHEOtsu.Text = "BlurCLAHEOtsu";
            m_radioButtonBlurCLAHEOtsu.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonBlurHistEqualizeOtsu
            // 
            m_radioButtonBlurHistEqualizeOtsu.AutoSize = true;
            m_radioButtonBlurHistEqualizeOtsu.Location = new Point(38, 104);
            m_radioButtonBlurHistEqualizeOtsu.Name = "m_radioButtonBlurHistEqualizeOtsu";
            m_radioButtonBlurHistEqualizeOtsu.Size = new Size(135, 19);
            m_radioButtonBlurHistEqualizeOtsu.TabIndex = 49;
            m_radioButtonBlurHistEqualizeOtsu.Text = "BlurHistEqualizeOtsu";
            m_radioButtonBlurHistEqualizeOtsu.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonBlurCLAHEAdaptive
            // 
            m_radioButtonBlurCLAHEAdaptive.AutoSize = true;
            m_radioButtonBlurCLAHEAdaptive.Location = new Point(38, 57);
            m_radioButtonBlurCLAHEAdaptive.Name = "m_radioButtonBlurCLAHEAdaptive";
            m_radioButtonBlurCLAHEAdaptive.Size = new Size(130, 19);
            m_radioButtonBlurCLAHEAdaptive.TabIndex = 48;
            m_radioButtonBlurCLAHEAdaptive.Text = "BlurCLAHEAdaptive";
            m_radioButtonBlurCLAHEAdaptive.UseVisualStyleBackColor = true;
            // 
            // m_tabPageImageAnalysis
            // 
            m_tabPageImageAnalysis.Controls.Add(groupBox1);
            m_tabPageImageAnalysis.Controls.Add(groupBox2);
            m_tabPageImageAnalysis.Location = new Point(4, 24);
            m_tabPageImageAnalysis.Name = "m_tabPageImageAnalysis";
            m_tabPageImageAnalysis.Padding = new Padding(3);
            m_tabPageImageAnalysis.Size = new Size(744, 231);
            m_tabPageImageAnalysis.TabIndex = 1;
            m_tabPageImageAnalysis.Text = "Resim Analiz";
            m_tabPageImageAnalysis.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(m_textBoxPlateMaxArea);
            groupBox1.Controls.Add(m_textBoxPlateMinArea);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(m_textBoxPlateMaxAspectRatio);
            groupBox1.Controls.Add(m_textBoxPlateMinAspectRatio);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(m_textBoxPlateMaxHeight);
            groupBox1.Controls.Add(m_textBoxPlateMinHeight);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(m_textBoxPlateMaxWidth);
            groupBox1.Controls.Add(m_textBoxPlateMinWidth);
            groupBox1.Controls.Add(label10);
            groupBox1.Location = new Point(8, 15);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(333, 161);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Plaka";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(63, 111);
            label1.Name = "label1";
            label1.Size = new Size(40, 15);
            label1.TabIndex = 26;
            label1.Text = "Alan : ";
            // 
            // m_textBoxPlateMaxArea
            // 
            m_textBoxPlateMaxArea.Location = new Point(231, 108);
            m_textBoxPlateMaxArea.Name = "m_textBoxPlateMaxArea";
            m_textBoxPlateMaxArea.Size = new Size(69, 23);
            m_textBoxPlateMaxArea.TabIndex = 29;
            // 
            // m_textBoxPlateMinArea
            // 
            m_textBoxPlateMinArea.Location = new Point(134, 108);
            m_textBoxPlateMinArea.Name = "m_textBoxPlateMinArea";
            m_textBoxPlateMinArea.Size = new Size(64, 23);
            m_textBoxPlateMinArea.TabIndex = 28;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(207, 115);
            label2.Name = "label2";
            label2.Size = new Size(16, 15);
            label2.TabIndex = 27;
            label2.Text = "...";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(22, 85);
            label3.Name = "label3";
            label3.Size = new Size(86, 15);
            label3.TabIndex = 22;
            label3.Text = "En-Boy Oranı : ";
            // 
            // m_textBoxPlateMaxAspectRatio
            // 
            m_textBoxPlateMaxAspectRatio.Location = new Point(231, 82);
            m_textBoxPlateMaxAspectRatio.Name = "m_textBoxPlateMaxAspectRatio";
            m_textBoxPlateMaxAspectRatio.Size = new Size(69, 23);
            m_textBoxPlateMaxAspectRatio.TabIndex = 25;
            // 
            // m_textBoxPlateMinAspectRatio
            // 
            m_textBoxPlateMinAspectRatio.Location = new Point(134, 82);
            m_textBoxPlateMinAspectRatio.Name = "m_textBoxPlateMinAspectRatio";
            m_textBoxPlateMinAspectRatio.Size = new Size(64, 23);
            m_textBoxPlateMinAspectRatio.TabIndex = 24;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(207, 89);
            label6.Name = "label6";
            label6.Size = new Size(16, 15);
            label6.TabIndex = 23;
            label6.Text = "...";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(38, 59);
            label7.Name = "label7";
            label7.Size = new Size(65, 15);
            label7.TabIndex = 18;
            label7.Text = "Yükseklik : ";
            // 
            // m_textBoxPlateMaxHeight
            // 
            m_textBoxPlateMaxHeight.Location = new Point(231, 56);
            m_textBoxPlateMaxHeight.Name = "m_textBoxPlateMaxHeight";
            m_textBoxPlateMaxHeight.Size = new Size(69, 23);
            m_textBoxPlateMaxHeight.TabIndex = 21;
            // 
            // m_textBoxPlateMinHeight
            // 
            m_textBoxPlateMinHeight.Location = new Point(134, 56);
            m_textBoxPlateMinHeight.Name = "m_textBoxPlateMinHeight";
            m_textBoxPlateMinHeight.Size = new Size(64, 23);
            m_textBoxPlateMinHeight.TabIndex = 20;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(207, 63);
            label8.Name = "label8";
            label8.Size = new Size(16, 15);
            label8.TabIndex = 19;
            label8.Text = "...";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(47, 33);
            label9.Name = "label9";
            label9.Size = new Size(57, 15);
            label9.TabIndex = 12;
            label9.Text = "Genişlik : ";
            // 
            // m_textBoxPlateMaxWidth
            // 
            m_textBoxPlateMaxWidth.Location = new Point(231, 30);
            m_textBoxPlateMaxWidth.Name = "m_textBoxPlateMaxWidth";
            m_textBoxPlateMaxWidth.Size = new Size(69, 23);
            m_textBoxPlateMaxWidth.TabIndex = 15;
            // 
            // m_textBoxPlateMinWidth
            // 
            m_textBoxPlateMinWidth.Location = new Point(134, 30);
            m_textBoxPlateMinWidth.Name = "m_textBoxPlateMinWidth";
            m_textBoxPlateMinWidth.Size = new Size(64, 23);
            m_textBoxPlateMinWidth.TabIndex = 14;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(207, 37);
            label10.Name = "label10";
            label10.Size = new Size(16, 15);
            label10.TabIndex = 13;
            label10.Text = "...";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(m_textBoxCharacterMaxDiagonalLength);
            groupBox2.Controls.Add(m_textBoxCharacterMinDiagonalLength);
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(m_textBoxCharacterMaxArea);
            groupBox2.Controls.Add(m_textBoxCharacterMinArea);
            groupBox2.Controls.Add(label14);
            groupBox2.Controls.Add(label15);
            groupBox2.Controls.Add(m_textBoxCharacterMaxAspectRatio);
            groupBox2.Controls.Add(m_textBoxCharacterMinAspectRatio);
            groupBox2.Controls.Add(label16);
            groupBox2.Controls.Add(label17);
            groupBox2.Controls.Add(m_textBoxCharacterMaxHeight);
            groupBox2.Controls.Add(m_textBoxCharacterMinHeight);
            groupBox2.Controls.Add(label18);
            groupBox2.Controls.Add(label19);
            groupBox2.Controls.Add(m_textBoxCharacterMaxWidth);
            groupBox2.Controls.Add(m_textBoxCharacterMinWidth);
            groupBox2.Controls.Add(label20);
            groupBox2.Location = new Point(369, 15);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(333, 161);
            groupBox2.TabIndex = 18;
            groupBox2.TabStop = false;
            groupBox2.Text = "Karakterler";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(9, 137);
            label11.Name = "label11";
            label11.Size = new Size(98, 15);
            label11.TabIndex = 30;
            label11.Text = "Çapraz Uzunluk : ";
            // 
            // m_textBoxCharacterMaxDiagonalLength
            // 
            m_textBoxCharacterMaxDiagonalLength.Location = new Point(231, 134);
            m_textBoxCharacterMaxDiagonalLength.Name = "m_textBoxCharacterMaxDiagonalLength";
            m_textBoxCharacterMaxDiagonalLength.Size = new Size(69, 23);
            m_textBoxCharacterMaxDiagonalLength.TabIndex = 33;
            // 
            // m_textBoxCharacterMinDiagonalLength
            // 
            m_textBoxCharacterMinDiagonalLength.Location = new Point(134, 134);
            m_textBoxCharacterMinDiagonalLength.Name = "m_textBoxCharacterMinDiagonalLength";
            m_textBoxCharacterMinDiagonalLength.Size = new Size(64, 23);
            m_textBoxCharacterMinDiagonalLength.TabIndex = 32;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(207, 141);
            label12.Name = "label12";
            label12.Size = new Size(16, 15);
            label12.TabIndex = 31;
            label12.Text = "...";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(63, 111);
            label13.Name = "label13";
            label13.Size = new Size(40, 15);
            label13.TabIndex = 26;
            label13.Text = "Alan : ";
            // 
            // m_textBoxCharacterMaxArea
            // 
            m_textBoxCharacterMaxArea.Location = new Point(231, 108);
            m_textBoxCharacterMaxArea.Name = "m_textBoxCharacterMaxArea";
            m_textBoxCharacterMaxArea.Size = new Size(69, 23);
            m_textBoxCharacterMaxArea.TabIndex = 29;
            // 
            // m_textBoxCharacterMinArea
            // 
            m_textBoxCharacterMinArea.Location = new Point(134, 108);
            m_textBoxCharacterMinArea.Name = "m_textBoxCharacterMinArea";
            m_textBoxCharacterMinArea.Size = new Size(64, 23);
            m_textBoxCharacterMinArea.TabIndex = 28;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(207, 115);
            label14.Name = "label14";
            label14.Size = new Size(16, 15);
            label14.TabIndex = 27;
            label14.Text = "...";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(22, 85);
            label15.Name = "label15";
            label15.Size = new Size(86, 15);
            label15.TabIndex = 22;
            label15.Text = "En-Boy Oranı : ";
            // 
            // m_textBoxCharacterMaxAspectRatio
            // 
            m_textBoxCharacterMaxAspectRatio.Location = new Point(231, 82);
            m_textBoxCharacterMaxAspectRatio.Name = "m_textBoxCharacterMaxAspectRatio";
            m_textBoxCharacterMaxAspectRatio.Size = new Size(69, 23);
            m_textBoxCharacterMaxAspectRatio.TabIndex = 25;
            // 
            // m_textBoxCharacterMinAspectRatio
            // 
            m_textBoxCharacterMinAspectRatio.Location = new Point(134, 82);
            m_textBoxCharacterMinAspectRatio.Name = "m_textBoxCharacterMinAspectRatio";
            m_textBoxCharacterMinAspectRatio.Size = new Size(64, 23);
            m_textBoxCharacterMinAspectRatio.TabIndex = 24;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(207, 89);
            label16.Name = "label16";
            label16.Size = new Size(16, 15);
            label16.TabIndex = 23;
            label16.Text = "...";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(38, 59);
            label17.Name = "label17";
            label17.Size = new Size(65, 15);
            label17.TabIndex = 18;
            label17.Text = "Yükseklik : ";
            // 
            // m_textBoxCharacterMaxHeight
            // 
            m_textBoxCharacterMaxHeight.Location = new Point(231, 56);
            m_textBoxCharacterMaxHeight.Name = "m_textBoxCharacterMaxHeight";
            m_textBoxCharacterMaxHeight.Size = new Size(69, 23);
            m_textBoxCharacterMaxHeight.TabIndex = 21;
            // 
            // m_textBoxCharacterMinHeight
            // 
            m_textBoxCharacterMinHeight.Location = new Point(134, 56);
            m_textBoxCharacterMinHeight.Name = "m_textBoxCharacterMinHeight";
            m_textBoxCharacterMinHeight.Size = new Size(64, 23);
            m_textBoxCharacterMinHeight.TabIndex = 20;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(207, 63);
            label18.Name = "label18";
            label18.Size = new Size(16, 15);
            label18.TabIndex = 19;
            label18.Text = "...";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(47, 33);
            label19.Name = "label19";
            label19.Size = new Size(57, 15);
            label19.TabIndex = 12;
            label19.Text = "Genişlik : ";
            // 
            // m_textBoxCharacterMaxWidth
            // 
            m_textBoxCharacterMaxWidth.Location = new Point(231, 30);
            m_textBoxCharacterMaxWidth.Name = "m_textBoxCharacterMaxWidth";
            m_textBoxCharacterMaxWidth.Size = new Size(69, 23);
            m_textBoxCharacterMaxWidth.TabIndex = 15;
            // 
            // m_textBoxCharacterMinWidth
            // 
            m_textBoxCharacterMinWidth.Location = new Point(134, 30);
            m_textBoxCharacterMinWidth.Name = "m_textBoxCharacterMinWidth";
            m_textBoxCharacterMinWidth.Size = new Size(64, 23);
            m_textBoxCharacterMinWidth.TabIndex = 14;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(207, 37);
            label20.Name = "label20";
            label20.Size = new Size(16, 15);
            label20.TabIndex = 13;
            label20.Text = "...";
            // 
            // m_tabPagePlate
            // 
            m_tabPagePlate.Controls.Add(m_groupBoxPlateType);
            m_tabPagePlate.Controls.Add(m_checkBoxFindMovementPlate);
            m_tabPagePlate.Controls.Add(m_checkBoxWhiteBalanced);
            m_tabPagePlate.Controls.Add(m_checkBoxAutoLightControl);
            m_tabPagePlate.Location = new Point(4, 24);
            m_tabPagePlate.Name = "m_tabPagePlate";
            m_tabPagePlate.Padding = new Padding(3);
            m_tabPagePlate.Size = new Size(744, 231);
            m_tabPagePlate.TabIndex = 3;
            m_tabPagePlate.Text = "Plaka";
            m_tabPagePlate.UseVisualStyleBackColor = true;
            // 
            // m_groupBoxPlateType
            // 
            m_groupBoxPlateType.Controls.Add(m_checkBoxSelectBestResult);
            m_groupBoxPlateType.Controls.Add(m_checkBoxFixOCRErrors);
            m_groupBoxPlateType.Controls.Add(m_radioButtonAllPlateType);
            m_groupBoxPlateType.Controls.Add(m_radioButtonTurkishPlate);
            m_groupBoxPlateType.Location = new Point(282, 19);
            m_groupBoxPlateType.Name = "m_groupBoxPlateType";
            m_groupBoxPlateType.Size = new Size(376, 101);
            m_groupBoxPlateType.TabIndex = 3;
            m_groupBoxPlateType.TabStop = false;
            m_groupBoxPlateType.Text = "Plaka Formatı";
            // 
            // m_checkBoxSelectBestResult
            // 
            m_checkBoxSelectBestResult.AutoSize = true;
            m_checkBoxSelectBestResult.Checked = true;
            m_checkBoxSelectBestResult.CheckState = CheckState.Checked;
            m_checkBoxSelectBestResult.Location = new Point(179, 50);
            m_checkBoxSelectBestResult.Name = "m_checkBoxSelectBestResult";
            m_checkBoxSelectBestResult.Size = new Size(171, 19);
            m_checkBoxSelectBestResult.TabIndex = 3;
            m_checkBoxSelectBestResult.Text = "En İyi Sonucu Otomatik Seç";
            m_checkBoxSelectBestResult.UseVisualStyleBackColor = true;
            // 
            // m_checkBoxFixOCRErrors
            // 
            m_checkBoxFixOCRErrors.AutoSize = true;
            m_checkBoxFixOCRErrors.Enabled = false;
            m_checkBoxFixOCRErrors.Location = new Point(20, 50);
            m_checkBoxFixOCRErrors.Name = "m_checkBoxFixOCRErrors";
            m_checkBoxFixOCRErrors.Size = new Size(140, 19);
            m_checkBoxFixOCRErrors.TabIndex = 2;
            m_checkBoxFixOCRErrors.Text = "OCR Hatalarını Düzelt";
            m_checkBoxFixOCRErrors.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonAllPlateType
            // 
            m_radioButtonAllPlateType.AutoSize = true;
            m_radioButtonAllPlateType.Location = new Point(257, 25);
            m_radioButtonAllPlateType.Name = "m_radioButtonAllPlateType";
            m_radioButtonAllPlateType.Size = new Size(93, 19);
            m_radioButtonAllPlateType.TabIndex = 1;
            m_radioButtonAllPlateType.TabStop = true;
            m_radioButtonAllPlateType.Text = "Tüm Plakalar";
            m_radioButtonAllPlateType.UseVisualStyleBackColor = true;
            // 
            // m_radioButtonTurkishPlate
            // 
            m_radioButtonTurkishPlate.AutoSize = true;
            m_radioButtonTurkishPlate.Location = new Point(20, 25);
            m_radioButtonTurkishPlate.Name = "m_radioButtonTurkishPlate";
            m_radioButtonTurkishPlate.Size = new Size(95, 19);
            m_radioButtonTurkishPlate.TabIndex = 0;
            m_radioButtonTurkishPlate.TabStop = true;
            m_radioButtonTurkishPlate.Text = "Türk Plakaları";
            m_radioButtonTurkishPlate.UseVisualStyleBackColor = true;
            m_radioButtonTurkishPlate.CheckedChanged += m_radioButtonTurkishPlate_CheckedChanged;
            // 
            // m_checkBoxFindMovementPlate
            // 
            m_checkBoxFindMovementPlate.AutoSize = true;
            m_checkBoxFindMovementPlate.Location = new Point(33, 80);
            m_checkBoxFindMovementPlate.Name = "m_checkBoxFindMovementPlate";
            m_checkBoxFindMovementPlate.Size = new Size(189, 19);
            m_checkBoxFindMovementPlate.TabIndex = 2;
            m_checkBoxFindMovementPlate.Text = "Hareket Eden Plakaları Tespit Et";
            m_checkBoxFindMovementPlate.UseVisualStyleBackColor = true;
            // 
            // m_checkBoxWhiteBalanced
            // 
            m_checkBoxWhiteBalanced.AutoSize = true;
            m_checkBoxWhiteBalanced.Location = new Point(33, 55);
            m_checkBoxWhiteBalanced.Name = "m_checkBoxWhiteBalanced";
            m_checkBoxWhiteBalanced.Size = new Size(154, 19);
            m_checkBoxWhiteBalanced.TabIndex = 1;
            m_checkBoxWhiteBalanced.Text = "Otomatik Beyaz Dengesi";
            m_checkBoxWhiteBalanced.UseVisualStyleBackColor = true;
            // 
            // m_checkBoxAutoLightControl
            // 
            m_checkBoxAutoLightControl.AutoSize = true;
            m_checkBoxAutoLightControl.Location = new Point(33, 30);
            m_checkBoxAutoLightControl.Name = "m_checkBoxAutoLightControl";
            m_checkBoxAutoLightControl.Size = new Size(149, 19);
            m_checkBoxAutoLightControl.TabIndex = 0;
            m_checkBoxAutoLightControl.Text = "Otomatik Işık Düzeltme";
            m_checkBoxAutoLightControl.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            splitContainer1.BorderStyle = BorderStyle.FixedSingle;
            splitContainer1.Location = new Point(252, 271);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Size = new Size(266, 167);
            splitContainer1.SplitterDistance = 88;
            splitContainer1.TabIndex = 25;
            splitContainer1.Paint += splitContainer1_Paint;
            splitContainer1.Resize += splitContainer1_Resize;
            // 
            // PreprocessingSettingsModal
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(splitContainer1);
            Controls.Add(m_tabControlSettings);
            Controls.Add(textBox5);
            Controls.Add(m_buttonApply);
            Controls.Add(textBox4);
            Controls.Add(m_buttonSave);
            Controls.Add(label5);
            Controls.Add(label4);
            MaximizeBox = false;
            Name = "PreprocessingSettingsModal";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Ayarlar";
            FormClosing += PreprocessingSettingsModal_FormClosing;
            Load += PreprocessingSettingsModal_Load;
            m_groupBoxPreprocessing.ResumeLayout(false);
            m_groupBoxPreprocessing.PerformLayout();
            m_groupBoxAdaptiveTreshould.ResumeLayout(false);
            m_groupBoxAdaptiveTreshould.PerformLayout();
            m_tabControlSettings.ResumeLayout(false);
            m_tabPageGeneral.ResumeLayout(false);
            m_tabPageGeneral.PerformLayout();
            m_groupBoxWorkingType.ResumeLayout(false);
            m_groupBoxWorkingType.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)m_numericUpDownMajorityVoiting).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            m_tabPagePreprocessing.ResumeLayout(false);
            m_groupBoxProcessingType.ResumeLayout(false);
            m_groupBoxProcessingType.PerformLayout();
            m_tabPageImageAnalysis.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            m_tabPagePlate.ResumeLayout(false);
            m_tabPagePlate.PerformLayout();
            m_groupBoxPlateType.ResumeLayout(false);
            m_groupBoxPlateType.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion


        private System.Windows.Forms.GroupBox m_groupBoxPreprocessing;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.TextBox m_textBoxAdaptiveThreshouldC;
        private System.Windows.Forms.TextBox m_textBoxAdaptiveThresholdBlock;
        private System.Windows.Forms.TextBox m_textBoxGaussianBlurKernel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label m_labelAdaptiveThreshouldC;
        private System.Windows.Forms.Label m_labelAdaptiveThresholdBlock;
        private System.Windows.Forms.Label m_labelGaussianBlurKernel;
        private System.Windows.Forms.RadioButton m_radioButtonGaussian;
        private System.Windows.Forms.RadioButton m_radioButtonMean;
        private System.Windows.Forms.GroupBox m_groupBoxAdaptiveTreshould;
        private System.Windows.Forms.Button m_buttonApply;
        private System.Windows.Forms.Button m_buttonSave;
        private System.Windows.Forms.TabControl m_tabControlSettings;
        private System.Windows.Forms.TabPage m_tabPagePreprocessing;
        private System.Windows.Forms.TabPage m_tabPageImageAnalysis;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox m_textBoxPlateMaxArea;
        private System.Windows.Forms.TextBox m_textBoxPlateMinArea;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox m_textBoxPlateMaxAspectRatio;
        private System.Windows.Forms.TextBox m_textBoxPlateMinAspectRatio;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox m_textBoxPlateMaxHeight;
        private System.Windows.Forms.TextBox m_textBoxPlateMinHeight;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox m_textBoxPlateMaxWidth;
        private System.Windows.Forms.TextBox m_textBoxPlateMinWidth;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox m_textBoxCharacterMaxDiagonalLength;
        private System.Windows.Forms.TextBox m_textBoxCharacterMinDiagonalLength;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox m_textBoxCharacterMaxArea;
        private System.Windows.Forms.TextBox m_textBoxCharacterMinArea;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox m_textBoxCharacterMaxAspectRatio;
        private System.Windows.Forms.TextBox m_textBoxCharacterMinAspectRatio;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox m_textBoxCharacterMaxHeight;
        private System.Windows.Forms.TextBox m_textBoxCharacterMinHeight;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox m_textBoxCharacterMaxWidth;
        private System.Windows.Forms.TextBox m_textBoxCharacterMinWidth;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.GroupBox m_groupBoxProcessingType;
        private System.Windows.Forms.RadioButton m_radioButtonBlurHistEqualizeAdaptive;
        private System.Windows.Forms.RadioButton m_radioButtonBlurCLAHEOtsu;
        private System.Windows.Forms.RadioButton m_radioButtonBlurHistEqualizeOtsu;
        private System.Windows.Forms.RadioButton m_radioButtonBlurCLAHEAdaptive;
        private System.Windows.Forms.TabPage m_tabPageGeneral;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox m_textBoxReadPlateFromVideoPath;
        private System.Windows.Forms.Label m_labelReadPlateFromVideoPath;
        private System.Windows.Forms.Button m_buttonReadPlateFromVideoPath;
        private System.Windows.Forms.TextBox m_textBoxReadPlateFromImagePath;
        private System.Windows.Forms.Label m_labelReadPlateFromImagePath;
        private System.Windows.Forms.Button m_buttonReadPlateFromImagePath;
        private System.Windows.Forms.FolderBrowserDialog m_folderBrowserDialog;
        private SplitContainer splitContainer1;
        public CheckBox m_checkBoxSignPlate;
        private NumericUpDown m_numericUpDownMajorityVoiting;
        private Label m_labelMajorityVoiting;
        private TabPage m_tabPagePlate;
        private CheckBox m_checkBoxAutoLightControl;
        private CheckBox m_checkBoxWhiteBalanced;
        private CheckBox m_checkBoxFindMovementPlate;
        private GroupBox m_groupBoxPlateType;
        private RadioButton m_radioButtonAllPlateType;
        private RadioButton m_radioButtonTurkishPlate;
        private CheckBox m_checkBoxSelectBestResult;
        private CheckBox m_checkBoxFixOCRErrors;
        private GroupBox m_groupBoxWorkingType;
        private Label m_labelChannelNumber;
        private ComboBox m_comboBoxChannel;
        private RadioButton m_radioButtonContinuous;
        private RadioButton m_radioButtonMotionSensitive;
    }
}