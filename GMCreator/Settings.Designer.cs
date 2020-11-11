namespace GMCreator
{
    partial class SettingsForm
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
            this.centralAnchorLabel = new System.Windows.Forms.Label();
            this.exportSettingsBox = new System.Windows.Forms.GroupBox();
            this.compressionLevelNumber = new System.Windows.Forms.NumericUpDown();
            this.compressionLabel = new System.Windows.Forms.Label();
            this.centralAnchorWSize = new System.Windows.Forms.NumericUpDown();
            this.centralAnchorHSize = new System.Windows.Forms.NumericUpDown();
            this.centralAnchorPixelsLabel = new System.Windows.Forms.Label();
            this.centralAnchorXLabel = new System.Windows.Forms.Label();
            this.cornerAnchorXLabel = new System.Windows.Forms.Label();
            this.cornerAnchorPixelsLabel = new System.Windows.Forms.Label();
            this.cornerAnchorHSize = new System.Windows.Forms.NumericUpDown();
            this.cornerAnchorWSize = new System.Windows.Forms.NumericUpDown();
            this.cornerAnchorLabel = new System.Windows.Forms.Label();
            this.anchorGroupBox = new System.Windows.Forms.GroupBox();
            this.anchorColourSwatch = new System.Windows.Forms.PictureBox();
            this.anchorColourHex = new System.Windows.Forms.TextBox();
            this.anchorColourLabel = new System.Windows.Forms.Label();
            this.boxColoursBox = new System.Windows.Forms.GroupBox();
            this.selectedBoxColourSwatch = new System.Windows.Forms.PictureBox();
            this.selectedBoxColourHex = new System.Windows.Forms.TextBox();
            this.selectedBoxColourLabel = new System.Windows.Forms.Label();
            this.drawBoxColourSwatch = new System.Windows.Forms.PictureBox();
            this.drawBoxColourHex = new System.Windows.Forms.TextBox();
            this.drawBoxColourLabel = new System.Windows.Forms.Label();
            this.okSettingsButton = new System.Windows.Forms.Button();
            this.cancelSettingsButton = new System.Windows.Forms.Button();
            this.versionSettingsBox = new System.Windows.Forms.GroupBox();
            this.gt2VersionJP11 = new System.Windows.Forms.RadioButton();
            this.gt2VersionPALSpa = new System.Windows.Forms.RadioButton();
            this.gt2VersionPALFra = new System.Windows.Forms.RadioButton();
            this.gt2VersionPALGer = new System.Windows.Forms.RadioButton();
            this.gt2VersionPALIta = new System.Windows.Forms.RadioButton();
            this.gt2VersionUS12 = new System.Windows.Forms.RadioButton();
            this.gt2VersionJP10 = new System.Windows.Forms.RadioButton();
            this.gt2VersionUS10 = new System.Windows.Forms.RadioButton();
            this.gt2VersionPALEng = new System.Windows.Forms.RadioButton();
            this.exportSettingsBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.compressionLevelNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.centralAnchorWSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.centralAnchorHSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cornerAnchorHSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cornerAnchorWSize)).BeginInit();
            this.anchorGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.anchorColourSwatch)).BeginInit();
            this.boxColoursBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.selectedBoxColourSwatch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.drawBoxColourSwatch)).BeginInit();
            this.versionSettingsBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // centralAnchorLabel
            // 
            this.centralAnchorLabel.AutoSize = true;
            this.centralAnchorLabel.Location = new System.Drawing.Point(6, 25);
            this.centralAnchorLabel.Name = "centralAnchorLabel";
            this.centralAnchorLabel.Size = new System.Drawing.Size(66, 13);
            this.centralAnchorLabel.TabIndex = 0;
            this.centralAnchorLabel.Text = "Central Size:";
            // 
            // exportSettingsBox
            // 
            this.exportSettingsBox.Controls.Add(this.compressionLevelNumber);
            this.exportSettingsBox.Controls.Add(this.compressionLabel);
            this.exportSettingsBox.Location = new System.Drawing.Point(12, 198);
            this.exportSettingsBox.Name = "exportSettingsBox";
            this.exportSettingsBox.Size = new System.Drawing.Size(256, 58);
            this.exportSettingsBox.TabIndex = 0;
            this.exportSettingsBox.TabStop = false;
            this.exportSettingsBox.Text = "GM Export Settings";
            // 
            // compressionLevelNumber
            // 
            this.compressionLevelNumber.Location = new System.Drawing.Point(157, 24);
            this.compressionLevelNumber.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.compressionLevelNumber.Name = "compressionLevelNumber";
            this.compressionLevelNumber.Size = new System.Drawing.Size(50, 20);
            this.compressionLevelNumber.TabIndex = 7;
            // 
            // compressionLabel
            // 
            this.compressionLabel.AutoSize = true;
            this.compressionLabel.Location = new System.Drawing.Point(6, 26);
            this.compressionLabel.Name = "compressionLabel";
            this.compressionLabel.Size = new System.Drawing.Size(96, 13);
            this.compressionLabel.TabIndex = 0;
            this.compressionLabel.Text = "Compression Level";
            // 
            // centralAnchorWSize
            // 
            this.centralAnchorWSize.Location = new System.Drawing.Point(90, 23);
            this.centralAnchorWSize.Name = "centralAnchorWSize";
            this.centralAnchorWSize.Size = new System.Drawing.Size(50, 20);
            this.centralAnchorWSize.TabIndex = 0;
            // 
            // centralAnchorHSize
            // 
            this.centralAnchorHSize.Location = new System.Drawing.Point(157, 23);
            this.centralAnchorHSize.Name = "centralAnchorHSize";
            this.centralAnchorHSize.Size = new System.Drawing.Size(50, 20);
            this.centralAnchorHSize.TabIndex = 1;
            // 
            // centralAnchorPixelsLabel
            // 
            this.centralAnchorPixelsLabel.AutoSize = true;
            this.centralAnchorPixelsLabel.Location = new System.Drawing.Point(213, 25);
            this.centralAnchorPixelsLabel.Name = "centralAnchorPixelsLabel";
            this.centralAnchorPixelsLabel.Size = new System.Drawing.Size(33, 13);
            this.centralAnchorPixelsLabel.TabIndex = 4;
            this.centralAnchorPixelsLabel.Text = "pixels";
            // 
            // centralAnchorXLabel
            // 
            this.centralAnchorXLabel.AutoSize = true;
            this.centralAnchorXLabel.Location = new System.Drawing.Point(142, 25);
            this.centralAnchorXLabel.Name = "centralAnchorXLabel";
            this.centralAnchorXLabel.Size = new System.Drawing.Size(12, 13);
            this.centralAnchorXLabel.TabIndex = 5;
            this.centralAnchorXLabel.Text = "x";
            // 
            // cornerAnchorXLabel
            // 
            this.cornerAnchorXLabel.AutoSize = true;
            this.cornerAnchorXLabel.Location = new System.Drawing.Point(142, 53);
            this.cornerAnchorXLabel.Name = "cornerAnchorXLabel";
            this.cornerAnchorXLabel.Size = new System.Drawing.Size(12, 13);
            this.cornerAnchorXLabel.TabIndex = 10;
            this.cornerAnchorXLabel.Text = "x";
            // 
            // cornerAnchorPixelsLabel
            // 
            this.cornerAnchorPixelsLabel.AutoSize = true;
            this.cornerAnchorPixelsLabel.Location = new System.Drawing.Point(213, 53);
            this.cornerAnchorPixelsLabel.Name = "cornerAnchorPixelsLabel";
            this.cornerAnchorPixelsLabel.Size = new System.Drawing.Size(33, 13);
            this.cornerAnchorPixelsLabel.TabIndex = 9;
            this.cornerAnchorPixelsLabel.Text = "pixels";
            // 
            // cornerAnchorHSize
            // 
            this.cornerAnchorHSize.Location = new System.Drawing.Point(157, 51);
            this.cornerAnchorHSize.Name = "cornerAnchorHSize";
            this.cornerAnchorHSize.Size = new System.Drawing.Size(50, 20);
            this.cornerAnchorHSize.TabIndex = 3;
            // 
            // cornerAnchorWSize
            // 
            this.cornerAnchorWSize.Location = new System.Drawing.Point(90, 51);
            this.cornerAnchorWSize.Name = "cornerAnchorWSize";
            this.cornerAnchorWSize.Size = new System.Drawing.Size(50, 20);
            this.cornerAnchorWSize.TabIndex = 2;
            // 
            // cornerAnchorLabel
            // 
            this.cornerAnchorLabel.AutoSize = true;
            this.cornerAnchorLabel.Location = new System.Drawing.Point(6, 53);
            this.cornerAnchorLabel.Name = "cornerAnchorLabel";
            this.cornerAnchorLabel.Size = new System.Drawing.Size(64, 13);
            this.cornerAnchorLabel.TabIndex = 7;
            this.cornerAnchorLabel.Text = "Corner Size:";
            // 
            // anchorGroupBox
            // 
            this.anchorGroupBox.Controls.Add(this.anchorColourSwatch);
            this.anchorGroupBox.Controls.Add(this.anchorColourHex);
            this.anchorGroupBox.Controls.Add(this.anchorColourLabel);
            this.anchorGroupBox.Controls.Add(this.cornerAnchorXLabel);
            this.anchorGroupBox.Controls.Add(this.centralAnchorLabel);
            this.anchorGroupBox.Controls.Add(this.cornerAnchorPixelsLabel);
            this.anchorGroupBox.Controls.Add(this.centralAnchorWSize);
            this.anchorGroupBox.Controls.Add(this.cornerAnchorHSize);
            this.anchorGroupBox.Controls.Add(this.centralAnchorHSize);
            this.anchorGroupBox.Controls.Add(this.cornerAnchorWSize);
            this.anchorGroupBox.Controls.Add(this.centralAnchorPixelsLabel);
            this.anchorGroupBox.Controls.Add(this.cornerAnchorLabel);
            this.anchorGroupBox.Controls.Add(this.centralAnchorXLabel);
            this.anchorGroupBox.Location = new System.Drawing.Point(12, 0);
            this.anchorGroupBox.Name = "anchorGroupBox";
            this.anchorGroupBox.Size = new System.Drawing.Size(256, 109);
            this.anchorGroupBox.TabIndex = 11;
            this.anchorGroupBox.TabStop = false;
            this.anchorGroupBox.Text = "Anchors";
            // 
            // anchorColourSwatch
            // 
            this.anchorColourSwatch.AccessibleDescription = "Opens the colour dialog to change the colour of the draggable anchors";
            this.anchorColourSwatch.AccessibleName = "Choose Anchor Colour";
            this.anchorColourSwatch.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.anchorColourSwatch.Location = new System.Drawing.Point(213, 77);
            this.anchorColourSwatch.Name = "anchorColourSwatch";
            this.anchorColourSwatch.Size = new System.Drawing.Size(32, 20);
            this.anchorColourSwatch.TabIndex = 12;
            this.anchorColourSwatch.TabStop = false;
            this.anchorColourSwatch.Click += new System.EventHandler(this.ShowColourBoxAndSetText);
            // 
            // anchorColourHex
            // 
            this.anchorColourHex.Location = new System.Drawing.Point(90, 77);
            this.anchorColourHex.MaxLength = 9;
            this.anchorColourHex.Name = "anchorColourHex";
            this.anchorColourHex.Size = new System.Drawing.Size(117, 20);
            this.anchorColourHex.TabIndex = 4;
            this.anchorColourHex.TextChanged += new System.EventHandler(this.ParseHexColourAndSetSwatch);
            // 
            // anchorColourLabel
            // 
            this.anchorColourLabel.AutoSize = true;
            this.anchorColourLabel.Location = new System.Drawing.Point(6, 80);
            this.anchorColourLabel.Name = "anchorColourLabel";
            this.anchorColourLabel.Size = new System.Drawing.Size(40, 13);
            this.anchorColourLabel.TabIndex = 0;
            this.anchorColourLabel.Text = "Colour:";
            // 
            // boxColoursBox
            // 
            this.boxColoursBox.Controls.Add(this.selectedBoxColourSwatch);
            this.boxColoursBox.Controls.Add(this.selectedBoxColourHex);
            this.boxColoursBox.Controls.Add(this.selectedBoxColourLabel);
            this.boxColoursBox.Controls.Add(this.drawBoxColourSwatch);
            this.boxColoursBox.Controls.Add(this.drawBoxColourHex);
            this.boxColoursBox.Controls.Add(this.drawBoxColourLabel);
            this.boxColoursBox.Location = new System.Drawing.Point(12, 115);
            this.boxColoursBox.Name = "boxColoursBox";
            this.boxColoursBox.Size = new System.Drawing.Size(256, 77);
            this.boxColoursBox.TabIndex = 12;
            this.boxColoursBox.TabStop = false;
            this.boxColoursBox.Text = "Box Colours";
            // 
            // selectedBoxColourSwatch
            // 
            this.selectedBoxColourSwatch.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.selectedBoxColourSwatch.Location = new System.Drawing.Point(213, 48);
            this.selectedBoxColourSwatch.Name = "selectedBoxColourSwatch";
            this.selectedBoxColourSwatch.Size = new System.Drawing.Size(32, 20);
            this.selectedBoxColourSwatch.TabIndex = 18;
            this.selectedBoxColourSwatch.TabStop = false;
            this.selectedBoxColourSwatch.Click += new System.EventHandler(this.ShowColourBoxAndSetText);
            // 
            // selectedBoxColourHex
            // 
            this.selectedBoxColourHex.Location = new System.Drawing.Point(90, 48);
            this.selectedBoxColourHex.MaxLength = 9;
            this.selectedBoxColourHex.Name = "selectedBoxColourHex";
            this.selectedBoxColourHex.Size = new System.Drawing.Size(117, 20);
            this.selectedBoxColourHex.TabIndex = 6;
            this.selectedBoxColourHex.TextChanged += new System.EventHandler(this.ParseHexColourAndSetSwatch);
            // 
            // selectedBoxColourLabel
            // 
            this.selectedBoxColourLabel.AutoSize = true;
            this.selectedBoxColourLabel.Location = new System.Drawing.Point(6, 51);
            this.selectedBoxColourLabel.Name = "selectedBoxColourLabel";
            this.selectedBoxColourLabel.Size = new System.Drawing.Size(49, 13);
            this.selectedBoxColourLabel.TabIndex = 16;
            this.selectedBoxColourLabel.Text = "Selected";
            // 
            // drawBoxColourSwatch
            // 
            this.drawBoxColourSwatch.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.drawBoxColourSwatch.Location = new System.Drawing.Point(213, 22);
            this.drawBoxColourSwatch.Name = "drawBoxColourSwatch";
            this.drawBoxColourSwatch.Size = new System.Drawing.Size(32, 20);
            this.drawBoxColourSwatch.TabIndex = 15;
            this.drawBoxColourSwatch.TabStop = false;
            this.drawBoxColourSwatch.Click += new System.EventHandler(this.ShowColourBoxAndSetText);
            // 
            // drawBoxColourHex
            // 
            this.drawBoxColourHex.Location = new System.Drawing.Point(90, 22);
            this.drawBoxColourHex.MaxLength = 9;
            this.drawBoxColourHex.Name = "drawBoxColourHex";
            this.drawBoxColourHex.Size = new System.Drawing.Size(117, 20);
            this.drawBoxColourHex.TabIndex = 5;
            this.drawBoxColourHex.TextChanged += new System.EventHandler(this.ParseHexColourAndSetSwatch);
            // 
            // drawBoxColourLabel
            // 
            this.drawBoxColourLabel.AutoSize = true;
            this.drawBoxColourLabel.Location = new System.Drawing.Point(6, 25);
            this.drawBoxColourLabel.Name = "drawBoxColourLabel";
            this.drawBoxColourLabel.Size = new System.Drawing.Size(46, 13);
            this.drawBoxColourLabel.TabIndex = 13;
            this.drawBoxColourLabel.Text = "Drawing";
            // 
            // okSettingsButton
            // 
            this.okSettingsButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okSettingsButton.Enabled = false;
            this.okSettingsButton.Location = new System.Drawing.Point(168, 367);
            this.okSettingsButton.Name = "okSettingsButton";
            this.okSettingsButton.Size = new System.Drawing.Size(99, 28);
            this.okSettingsButton.TabIndex = 8;
            this.okSettingsButton.Text = "&OK";
            this.okSettingsButton.UseVisualStyleBackColor = true;
            this.okSettingsButton.Click += new System.EventHandler(this.okSettingsButton_Click);
            // 
            // cancelSettingsButton
            // 
            this.cancelSettingsButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelSettingsButton.Location = new System.Drawing.Point(11, 367);
            this.cancelSettingsButton.Name = "cancelSettingsButton";
            this.cancelSettingsButton.Size = new System.Drawing.Size(99, 28);
            this.cancelSettingsButton.TabIndex = 9;
            this.cancelSettingsButton.Text = "&Cancel";
            this.cancelSettingsButton.UseVisualStyleBackColor = true;
            // 
            // versionSettingsBox
            // 
            this.versionSettingsBox.Controls.Add(this.gt2VersionJP11);
            this.versionSettingsBox.Controls.Add(this.gt2VersionPALSpa);
            this.versionSettingsBox.Controls.Add(this.gt2VersionPALFra);
            this.versionSettingsBox.Controls.Add(this.gt2VersionPALGer);
            this.versionSettingsBox.Controls.Add(this.gt2VersionPALIta);
            this.versionSettingsBox.Controls.Add(this.gt2VersionUS12);
            this.versionSettingsBox.Controls.Add(this.gt2VersionJP10);
            this.versionSettingsBox.Controls.Add(this.gt2VersionUS10);
            this.versionSettingsBox.Controls.Add(this.gt2VersionPALEng);
            this.versionSettingsBox.Location = new System.Drawing.Point(14, 262);
            this.versionSettingsBox.Name = "versionSettingsBox";
            this.versionSettingsBox.Size = new System.Drawing.Size(253, 92);
            this.versionSettingsBox.TabIndex = 13;
            this.versionSettingsBox.TabStop = false;
            this.versionSettingsBox.Text = "GT2 Version";
            // 
            // gt2VersionJP11
            // 
            this.gt2VersionJP11.AutoSize = true;
            this.gt2VersionJP11.Location = new System.Drawing.Point(86, 22);
            this.gt2VersionJP11.Name = "gt2VersionJP11";
            this.gt2VersionJP11.Size = new System.Drawing.Size(55, 17);
            this.gt2VersionJP11.TabIndex = 8;
            this.gt2VersionJP11.TabStop = true;
            this.gt2VersionJP11.Text = "JP 1.1";
            this.gt2VersionJP11.UseVisualStyleBackColor = true;
            this.gt2VersionJP11.CheckedChanged += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionPALSpa
            // 
            this.gt2VersionPALSpa.AutoSize = true;
            this.gt2VersionPALSpa.Location = new System.Drawing.Point(166, 68);
            this.gt2VersionPALSpa.Name = "gt2VersionPALSpa";
            this.gt2VersionPALSpa.Size = new System.Drawing.Size(67, 17);
            this.gt2VersionPALSpa.TabIndex = 7;
            this.gt2VersionPALSpa.TabStop = true;
            this.gt2VersionPALSpa.Text = "PAL-Spa";
            this.gt2VersionPALSpa.UseVisualStyleBackColor = true;
            this.gt2VersionPALSpa.Enter += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionPALFra
            // 
            this.gt2VersionPALFra.AutoSize = true;
            this.gt2VersionPALFra.Location = new System.Drawing.Point(166, 45);
            this.gt2VersionPALFra.Name = "gt2VersionPALFra";
            this.gt2VersionPALFra.Size = new System.Drawing.Size(63, 17);
            this.gt2VersionPALFra.TabIndex = 6;
            this.gt2VersionPALFra.TabStop = true;
            this.gt2VersionPALFra.Text = "PAL-Fra";
            this.gt2VersionPALFra.UseVisualStyleBackColor = true;
            this.gt2VersionPALFra.CheckedChanged += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionPALGer
            // 
            this.gt2VersionPALGer.AutoSize = true;
            this.gt2VersionPALGer.Location = new System.Drawing.Point(86, 69);
            this.gt2VersionPALGer.Name = "gt2VersionPALGer";
            this.gt2VersionPALGer.Size = new System.Drawing.Size(65, 17);
            this.gt2VersionPALGer.TabIndex = 5;
            this.gt2VersionPALGer.TabStop = true;
            this.gt2VersionPALGer.Text = "PAL-Ger";
            this.gt2VersionPALGer.UseVisualStyleBackColor = true;
            this.gt2VersionPALGer.Enter += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionPALIta
            // 
            this.gt2VersionPALIta.AutoSize = true;
            this.gt2VersionPALIta.Location = new System.Drawing.Point(12, 69);
            this.gt2VersionPALIta.Name = "gt2VersionPALIta";
            this.gt2VersionPALIta.Size = new System.Drawing.Size(60, 17);
            this.gt2VersionPALIta.TabIndex = 4;
            this.gt2VersionPALIta.TabStop = true;
            this.gt2VersionPALIta.Text = "PAL-Ita";
            this.gt2VersionPALIta.UseVisualStyleBackColor = true;
            this.gt2VersionPALIta.Enter += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionUS12
            // 
            this.gt2VersionUS12.AutoSize = true;
            this.gt2VersionUS12.Location = new System.Drawing.Point(12, 45);
            this.gt2VersionUS12.Name = "gt2VersionUS12";
            this.gt2VersionUS12.Size = new System.Drawing.Size(58, 17);
            this.gt2VersionUS12.TabIndex = 3;
            this.gt2VersionUS12.TabStop = true;
            this.gt2VersionUS12.Text = "US 1.2";
            this.gt2VersionUS12.UseVisualStyleBackColor = true;
            this.gt2VersionUS12.Enter += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionJP10
            // 
            this.gt2VersionJP10.AutoSize = true;
            this.gt2VersionJP10.Location = new System.Drawing.Point(12, 22);
            this.gt2VersionJP10.Name = "gt2VersionJP10";
            this.gt2VersionJP10.Size = new System.Drawing.Size(55, 17);
            this.gt2VersionJP10.TabIndex = 2;
            this.gt2VersionJP10.TabStop = true;
            this.gt2VersionJP10.Text = "JP 1.0";
            this.gt2VersionJP10.UseVisualStyleBackColor = true;
            this.gt2VersionJP10.CheckedChanged += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionUS10
            // 
            this.gt2VersionUS10.AutoSize = true;
            this.gt2VersionUS10.Location = new System.Drawing.Point(166, 22);
            this.gt2VersionUS10.Name = "gt2VersionUS10";
            this.gt2VersionUS10.Size = new System.Drawing.Size(76, 17);
            this.gt2VersionUS10.TabIndex = 1;
            this.gt2VersionUS10.TabStop = true;
            this.gt2VersionUS10.Text = "US 1.0-1.1";
            this.gt2VersionUS10.UseVisualStyleBackColor = true;
            this.gt2VersionUS10.CheckedChanged += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // gt2VersionPALEng
            // 
            this.gt2VersionPALEng.AutoSize = true;
            this.gt2VersionPALEng.Location = new System.Drawing.Point(86, 45);
            this.gt2VersionPALEng.Name = "gt2VersionPALEng";
            this.gt2VersionPALEng.Size = new System.Drawing.Size(67, 17);
            this.gt2VersionPALEng.TabIndex = 0;
            this.gt2VersionPALEng.TabStop = true;
            this.gt2VersionPALEng.Text = "PAL-Eng";
            this.gt2VersionPALEng.UseVisualStyleBackColor = true;
            this.gt2VersionPALEng.CheckedChanged += new System.EventHandler(this.gt2Version_CheckedChanged);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(276, 407);
            this.Controls.Add(this.versionSettingsBox);
            this.Controls.Add(this.cancelSettingsButton);
            this.Controls.Add(this.okSettingsButton);
            this.Controls.Add(this.boxColoursBox);
            this.Controls.Add(this.exportSettingsBox);
            this.Controls.Add(this.anchorGroupBox);
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "GMCreator Settings";
            this.exportSettingsBox.ResumeLayout(false);
            this.exportSettingsBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.compressionLevelNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.centralAnchorWSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.centralAnchorHSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cornerAnchorHSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cornerAnchorWSize)).EndInit();
            this.anchorGroupBox.ResumeLayout(false);
            this.anchorGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.anchorColourSwatch)).EndInit();
            this.boxColoursBox.ResumeLayout(false);
            this.boxColoursBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.selectedBoxColourSwatch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.drawBoxColourSwatch)).EndInit();
            this.versionSettingsBox.ResumeLayout(false);
            this.versionSettingsBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label centralAnchorLabel;
        private System.Windows.Forms.GroupBox exportSettingsBox;
        private System.Windows.Forms.NumericUpDown centralAnchorWSize;
        private System.Windows.Forms.NumericUpDown centralAnchorHSize;
        private System.Windows.Forms.Label centralAnchorPixelsLabel;
        private System.Windows.Forms.Label centralAnchorXLabel;
        private System.Windows.Forms.Label cornerAnchorXLabel;
        private System.Windows.Forms.Label cornerAnchorPixelsLabel;
        private System.Windows.Forms.NumericUpDown cornerAnchorHSize;
        private System.Windows.Forms.NumericUpDown cornerAnchorWSize;
        private System.Windows.Forms.Label cornerAnchorLabel;
        private System.Windows.Forms.GroupBox anchorGroupBox;
        private System.Windows.Forms.PictureBox anchorColourSwatch;
        private System.Windows.Forms.TextBox anchorColourHex;
        private System.Windows.Forms.Label anchorColourLabel;
        private System.Windows.Forms.GroupBox boxColoursBox;
        private System.Windows.Forms.PictureBox drawBoxColourSwatch;
        private System.Windows.Forms.TextBox drawBoxColourHex;
        private System.Windows.Forms.Label drawBoxColourLabel;
        private System.Windows.Forms.PictureBox selectedBoxColourSwatch;
        private System.Windows.Forms.TextBox selectedBoxColourHex;
        private System.Windows.Forms.Label selectedBoxColourLabel;
        private System.Windows.Forms.Label compressionLabel;
        private System.Windows.Forms.NumericUpDown compressionLevelNumber;
        private System.Windows.Forms.Button okSettingsButton;
        private System.Windows.Forms.Button cancelSettingsButton;
        private System.Windows.Forms.GroupBox versionSettingsBox;
        private System.Windows.Forms.RadioButton gt2VersionJP10;
        private System.Windows.Forms.RadioButton gt2VersionUS10;
        private System.Windows.Forms.RadioButton gt2VersionPALEng;
        private System.Windows.Forms.RadioButton gt2VersionUS12;
        private System.Windows.Forms.RadioButton gt2VersionPALSpa;
        private System.Windows.Forms.RadioButton gt2VersionPALFra;
        private System.Windows.Forms.RadioButton gt2VersionPALGer;
        private System.Windows.Forms.RadioButton gt2VersionPALIta;
        private System.Windows.Forms.RadioButton gt2VersionJP11;
    }
}