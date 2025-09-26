namespace PlateRecognation
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            m_dataGridViewPossiblePlateRegions = new DataGridView();
            m_columnPlate = new DataGridViewImageColumn();
            m_columnRead = new DataGridViewTextBoxColumn();
            Column1 = new DataGridViewTextBoxColumn();
            m_mainMenu = new MenuStrip();
            m_settingsPopup = new ToolStripMenuItem();
            m_preprocessingItem = new ToolStripMenuItem();
            m_imageAnalysisItem = new ToolStripMenuItem();
            m_listBoxPath = new ListBox();
            m_pictureBoxCharacterSegmented = new PictureBox();
            pictureBox3 = new PictureBox();
            m_panelFindPlateFromImage = new Panel();
            m_buttonFindPlateFromImage = new Button();
            pictureBoxFrame = new PictureBox();
            m_pictureBoxPlateTrack = new PictureBox();
            pictureBox1 = new PictureBox();
            m_pictureBoxSVM1 = new PictureBox();
            m_pictureBoxSVM2 = new PictureBox();
            m_pictureBoxSVM3 = new PictureBox();
            m_pictureBoxSVM = new PictureBox();
            m_pictureBoxPlateSeed = new PictureBox();
            m_pictureBoxNotPlate = new PictureBox();
            pictureBox2 = new PictureBox();
            pictureBox4 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)m_dataGridViewPossiblePlateRegions).BeginInit();
            m_mainMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxCharacterSegmented).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            m_panelFindPlateFromImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxFrame).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxPlateTrack).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxPlateSeed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxNotPlate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            SuspendLayout();
            // 
            // m_dataGridViewPossiblePlateRegions
            // 
            m_dataGridViewPossiblePlateRegions.AllowUserToAddRows = false;
            m_dataGridViewPossiblePlateRegions.AllowUserToDeleteRows = false;
            m_dataGridViewPossiblePlateRegions.AllowUserToResizeColumns = false;
            m_dataGridViewPossiblePlateRegions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            m_dataGridViewPossiblePlateRegions.Columns.AddRange(new DataGridViewColumn[] { m_columnPlate, m_columnRead, Column1 });
            m_dataGridViewPossiblePlateRegions.Location = new Point(1355, 27);
            m_dataGridViewPossiblePlateRegions.MultiSelect = false;
            m_dataGridViewPossiblePlateRegions.Name = "m_dataGridViewPossiblePlateRegions";
            m_dataGridViewPossiblePlateRegions.RowTemplate.Height = 30;
            m_dataGridViewPossiblePlateRegions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            m_dataGridViewPossiblePlateRegions.Size = new Size(474, 406);
            m_dataGridViewPossiblePlateRegions.TabIndex = 4;
            m_dataGridViewPossiblePlateRegions.CellClick += m_dataGridViewPossiblePlateRegions_CellClick;
            // 
            // m_columnPlate
            // 
            m_columnPlate.FillWeight = 200F;
            m_columnPlate.HeaderText = "Plaka";
            m_columnPlate.Name = "m_columnPlate";
            m_columnPlate.SortMode = DataGridViewColumnSortMode.Automatic;
            m_columnPlate.Width = 150;
            // 
            // m_columnRead
            // 
            m_columnRead.HeaderText = "Okuma";
            m_columnRead.Name = "m_columnRead";
            // 
            // Column1
            // 
            Column1.HeaderText = "Doğruluk(%)";
            Column1.Name = "Column1";
            // 
            // m_mainMenu
            // 
            m_mainMenu.Items.AddRange(new ToolStripItem[] { m_settingsPopup });
            m_mainMenu.Location = new Point(0, 0);
            m_mainMenu.Name = "m_mainMenu";
            m_mainMenu.Size = new Size(1904, 24);
            m_mainMenu.TabIndex = 5;
            m_mainMenu.Text = "menuStrip1";
            // 
            // m_settingsPopup
            // 
            m_settingsPopup.DropDownItems.AddRange(new ToolStripItem[] { m_preprocessingItem, m_imageAnalysisItem });
            m_settingsPopup.Name = "m_settingsPopup";
            m_settingsPopup.Size = new Size(56, 20);
            m_settingsPopup.Text = "&Ayarlar";
            // 
            // m_preprocessingItem
            // 
            m_preprocessingItem.Name = "m_preprocessingItem";
            m_preprocessingItem.Size = new Size(141, 22);
            m_preprocessingItem.Text = "&Ön İşleme";
            m_preprocessingItem.Click += m_preprocessingItem_Click;
            // 
            // m_imageAnalysisItem
            // 
            m_imageAnalysisItem.Name = "m_imageAnalysisItem";
            m_imageAnalysisItem.Size = new Size(141, 22);
            m_imageAnalysisItem.Text = "&Resim Analiz";
            // 
            // m_listBoxPath
            // 
            m_listBoxPath.FormattingEnabled = true;
            m_listBoxPath.ItemHeight = 15;
            m_listBoxPath.Location = new Point(21, 6);
            m_listBoxPath.Name = "m_listBoxPath";
            m_listBoxPath.Size = new Size(307, 484);
            m_listBoxPath.TabIndex = 6;
            m_listBoxPath.SelectedIndexChanged += m_listBoxPath_SelectedIndexChanged;
            // 
            // m_pictureBoxCharacterSegmented
            // 
            m_pictureBoxCharacterSegmented.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxCharacterSegmented.Location = new Point(1384, 827);
            m_pictureBoxCharacterSegmented.Name = "m_pictureBoxCharacterSegmented";
            m_pictureBoxCharacterSegmented.Size = new Size(253, 120);
            m_pictureBoxCharacterSegmented.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxCharacterSegmented.TabIndex = 7;
            m_pictureBoxCharacterSegmented.TabStop = false;
            // 
            // pictureBox3
            // 
            pictureBox3.BorderStyle = BorderStyle.FixedSingle;
            pictureBox3.Location = new Point(1696, 817);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(253, 120);
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox3.TabIndex = 8;
            pictureBox3.TabStop = false;
            // 
            // m_panelFindPlateFromImage
            // 
            m_panelFindPlateFromImage.BorderStyle = BorderStyle.FixedSingle;
            m_panelFindPlateFromImage.Controls.Add(m_buttonFindPlateFromImage);
            m_panelFindPlateFromImage.Controls.Add(m_listBoxPath);
            m_panelFindPlateFromImage.Location = new Point(497, 155);
            m_panelFindPlateFromImage.Name = "m_panelFindPlateFromImage";
            m_panelFindPlateFromImage.Size = new Size(331, 498);
            m_panelFindPlateFromImage.TabIndex = 15;
            // 
            // m_buttonFindPlateFromImage
            // 
            m_buttonFindPlateFromImage.Anchor = AnchorStyles.Left;
            m_buttonFindPlateFromImage.FlatStyle = FlatStyle.Flat;
            m_buttonFindPlateFromImage.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 162);
            m_buttonFindPlateFromImage.Location = new Point(0, 39);
            m_buttonFindPlateFromImage.Name = "m_buttonFindPlateFromImage";
            m_buttonFindPlateFromImage.Size = new Size(19, 34);
            m_buttonFindPlateFromImage.TabIndex = 5;
            m_buttonFindPlateFromImage.Text = ">";
            m_buttonFindPlateFromImage.UseMnemonic = false;
            m_buttonFindPlateFromImage.UseVisualStyleBackColor = true;
            m_buttonFindPlateFromImage.Click += m_buttonFindPlateFromImage_Click;
            // 
            // pictureBoxFrame
            // 
            pictureBoxFrame.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxFrame.Location = new Point(1355, 439);
            pictureBoxFrame.Name = "pictureBoxFrame";
            pictureBoxFrame.Size = new Size(160, 120);
            pictureBoxFrame.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxFrame.TabIndex = 16;
            pictureBoxFrame.TabStop = false;
            // 
            // m_pictureBoxPlateTrack
            // 
            m_pictureBoxPlateTrack.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxPlateTrack.Location = new Point(1716, 761);
            m_pictureBoxPlateTrack.Name = "m_pictureBoxPlateTrack";
            m_pictureBoxPlateTrack.Size = new Size(144, 33);
            m_pictureBoxPlateTrack.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxPlateTrack.TabIndex = 16;
            m_pictureBoxPlateTrack.TabStop = false;
            // 
            // pictureBox1
            // 
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.Location = new Point(1355, 672);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(320, 240);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 17;
            pictureBox1.TabStop = false;
            // 
            // m_pictureBoxSVM1
            // 
            m_pictureBoxSVM1.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxSVM1.Location = new Point(1760, 526);
            m_pictureBoxSVM1.Name = "m_pictureBoxSVM1";
            m_pictureBoxSVM1.Size = new Size(144, 33);
            m_pictureBoxSVM1.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxSVM1.TabIndex = 18;
            m_pictureBoxSVM1.TabStop = false;
            m_pictureBoxSVM1.Tag = "1";
            // 
            // m_pictureBoxSVM2
            // 
            m_pictureBoxSVM2.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxSVM2.Location = new Point(1760, 565);
            m_pictureBoxSVM2.Name = "m_pictureBoxSVM2";
            m_pictureBoxSVM2.Size = new Size(144, 33);
            m_pictureBoxSVM2.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxSVM2.TabIndex = 19;
            m_pictureBoxSVM2.TabStop = false;
            m_pictureBoxSVM2.Tag = "2";
            // 
            // m_pictureBoxSVM3
            // 
            m_pictureBoxSVM3.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxSVM3.Location = new Point(1760, 604);
            m_pictureBoxSVM3.Name = "m_pictureBoxSVM3";
            m_pictureBoxSVM3.Size = new Size(144, 33);
            m_pictureBoxSVM3.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxSVM3.TabIndex = 20;
            m_pictureBoxSVM3.TabStop = false;
            m_pictureBoxSVM3.Tag = "3";
            // 
            // m_pictureBoxSVM
            // 
            m_pictureBoxSVM.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxSVM.Location = new Point(1583, 526);
            m_pictureBoxSVM.Name = "m_pictureBoxSVM";
            m_pictureBoxSVM.Size = new Size(144, 33);
            m_pictureBoxSVM.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxSVM.TabIndex = 22;
            m_pictureBoxSVM.TabStop = false;
            // 
            // m_pictureBoxPlateSeed
            // 
            m_pictureBoxPlateSeed.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxPlateSeed.Location = new Point(1288, 574);
            m_pictureBoxPlateSeed.Name = "m_pictureBoxPlateSeed";
            m_pictureBoxPlateSeed.Size = new Size(144, 33);
            m_pictureBoxPlateSeed.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxPlateSeed.TabIndex = 21;
            m_pictureBoxPlateSeed.TabStop = false;
            // 
            // m_pictureBoxNotPlate
            // 
            m_pictureBoxNotPlate.BorderStyle = BorderStyle.FixedSingle;
            m_pictureBoxNotPlate.Location = new Point(1583, 620);
            m_pictureBoxNotPlate.Name = "m_pictureBoxNotPlate";
            m_pictureBoxNotPlate.Size = new Size(144, 33);
            m_pictureBoxNotPlate.SizeMode = PictureBoxSizeMode.StretchImage;
            m_pictureBoxNotPlate.TabIndex = 23;
            m_pictureBoxNotPlate.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.BorderStyle = BorderStyle.FixedSingle;
            pictureBox2.Location = new Point(1438, 574);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(144, 33);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 24;
            pictureBox2.TabStop = false;
            // 
            // pictureBox4
            // 
            pictureBox4.BorderStyle = BorderStyle.FixedSingle;
            pictureBox4.Location = new Point(1518, 439);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(144, 33);
            pictureBox4.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox4.TabIndex = 25;
            pictureBox4.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1904, 1041);
            Controls.Add(pictureBox4);
            Controls.Add(pictureBox2);
            Controls.Add(m_pictureBoxNotPlate);
            Controls.Add(m_pictureBoxSVM);
            Controls.Add(m_pictureBoxPlateSeed);
            Controls.Add(m_pictureBoxSVM3);
            Controls.Add(m_pictureBoxSVM2);
            Controls.Add(m_pictureBoxSVM1);
            Controls.Add(pictureBox1);
            Controls.Add(pictureBoxFrame);
            Controls.Add(m_dataGridViewPossiblePlateRegions);
            Controls.Add(m_pictureBoxPlateTrack);
            Controls.Add(pictureBox3);
            Controls.Add(m_pictureBoxCharacterSegmented);
            Controls.Add(m_mainMenu);
            Controls.Add(m_panelFindPlateFromImage);
            MainMenuStrip = m_mainMenu;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MainForm";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)m_dataGridViewPossiblePlateRegions).EndInit();
            m_mainMenu.ResumeLayout(false);
            m_mainMenu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxCharacterSegmented).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            m_panelFindPlateFromImage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxFrame).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxPlateTrack).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM1).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM2).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM3).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxSVM).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxPlateSeed).EndInit();
            ((System.ComponentModel.ISupportInitialize)m_pictureBoxNotPlate).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        public DataGridView m_dataGridViewPossiblePlateRegions;
        private MenuStrip m_mainMenu;
        private ToolStripMenuItem m_settingsPopup;
        private ToolStripMenuItem m_preprocessingItem;
        private ToolStripMenuItem m_imageAnalysisItem;
        public ListBox m_listBoxPath;
        public PictureBox m_pictureBoxCharacterSegmented;
        public PictureBox pictureBox3;
        private Panel m_panelFindPlateFromImage;
        private Button m_buttonFindPlateFromImage;
        private DataGridViewImageColumn m_columnPlate;
        private DataGridViewTextBoxColumn m_columnRead;
        private DataGridViewTextBoxColumn Column1;
        public PictureBox pictureBoxFrame;
        public PictureBox m_pictureBoxPlateTrack;
        public PictureBox pictureBox1;
        public PictureBox m_pictureBoxSVM1;
        public PictureBox m_pictureBoxSVM2;
        public PictureBox m_pictureBoxSVM3;
        public PictureBox m_pictureBoxSVM;
        public PictureBox m_pictureBoxPlateSeed;
        public PictureBox m_pictureBoxNotPlate;
        public PictureBox pictureBox2;
        public PictureBox pictureBox4;
    }
}
