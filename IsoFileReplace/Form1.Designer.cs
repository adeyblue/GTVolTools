namespace IsoFileReplace
{
    partial class MainForm
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
            this.isoFileLabel = new System.Windows.Forms.Label();
            this.isoFileName = new System.Windows.Forms.TextBox();
            this.selectIsoButton = new System.Windows.Forms.Button();
            this.insertFileLabel = new System.Windows.Forms.Label();
            this.newFilePath = new System.Windows.Forms.TextBox();
            this.newFileButton = new System.Windows.Forms.Button();
            this.replaceFileLabel = new System.Windows.Forms.Label();
            this.replacementFilePath = new System.Windows.Forms.TextBox();
            this.startButton = new System.Windows.Forms.Button();
            this.outputLabel = new System.Windows.Forms.Label();
            this.outputText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // isoFileLabel
            // 
            this.isoFileLabel.AutoSize = true;
            this.isoFileLabel.Location = new System.Drawing.Point(11, 14);
            this.isoFileLabel.Name = "isoFileLabel";
            this.isoFileLabel.Size = new System.Drawing.Size(41, 13);
            this.isoFileLabel.TabIndex = 0;
            this.isoFileLabel.Text = "Iso/Bin";
            // 
            // isoFileName
            // 
            this.isoFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.isoFileName.Location = new System.Drawing.Point(76, 11);
            this.isoFileName.Name = "isoFileName";
            this.isoFileName.Size = new System.Drawing.Size(179, 20);
            this.isoFileName.TabIndex = 1;
            this.isoFileName.TextChanged += new System.EventHandler(this.isoFileName_TextChanged);
            // 
            // selectIsoButton
            // 
            this.selectIsoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.selectIsoButton.Location = new System.Drawing.Point(261, 6);
            this.selectIsoButton.Name = "selectIsoButton";
            this.selectIsoButton.Size = new System.Drawing.Size(109, 28);
            this.selectIsoButton.TabIndex = 2;
            this.selectIsoButton.Text = "&Select Iso/Bin...";
            this.selectIsoButton.UseVisualStyleBackColor = true;
            this.selectIsoButton.Click += new System.EventHandler(this.selectIsoButton_Click);
            // 
            // insertFileLabel
            // 
            this.insertFileLabel.AutoSize = true;
            this.insertFileLabel.Location = new System.Drawing.Point(11, 46);
            this.insertFileLabel.Name = "insertFileLabel";
            this.insertFileLabel.Size = new System.Drawing.Size(61, 13);
            this.insertFileLabel.TabIndex = 0;
            this.insertFileLabel.Text = "Use this file";
            // 
            // newFilePath
            // 
            this.newFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.newFilePath.Location = new System.Drawing.Point(76, 43);
            this.newFilePath.Name = "newFilePath";
            this.newFilePath.Size = new System.Drawing.Size(179, 20);
            this.newFilePath.TabIndex = 3;
            this.newFilePath.TextChanged += new System.EventHandler(this.newFilePath_TextChanged);
            // 
            // newFileButton
            // 
            this.newFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.newFileButton.Location = new System.Drawing.Point(261, 40);
            this.newFileButton.Name = "newFileButton";
            this.newFileButton.Size = new System.Drawing.Size(109, 28);
            this.newFileButton.TabIndex = 4;
            this.newFileButton.Text = "Select &New File...";
            this.newFileButton.UseVisualStyleBackColor = true;
            this.newFileButton.Click += new System.EventHandler(this.newFileButton_Click);
            // 
            // replaceFileLabel
            // 
            this.replaceFileLabel.AutoSize = true;
            this.replaceFileLabel.Location = new System.Drawing.Point(11, 77);
            this.replaceFileLabel.Name = "replaceFileLabel";
            this.replaceFileLabel.Size = new System.Drawing.Size(82, 13);
            this.replaceFileLabel.TabIndex = 0;
            this.replaceFileLabel.Text = "Replace this file";
            // 
            // replacementFilePath
            // 
            this.replacementFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.replacementFilePath.Location = new System.Drawing.Point(105, 74);
            this.replacementFilePath.Name = "replacementFilePath";
            this.replacementFilePath.Size = new System.Drawing.Size(265, 20);
            this.replacementFilePath.TabIndex = 5;
            this.replacementFilePath.TextChanged += new System.EventHandler(this.replacementFilePath_TextChanged);
            // 
            // startButton
            // 
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.startButton.Enabled = false;
            this.startButton.Location = new System.Drawing.Point(268, 104);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(101, 29);
            this.startButton.TabIndex = 6;
            this.startButton.Text = "&Go";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // outputLabel
            // 
            this.outputLabel.AutoSize = true;
            this.outputLabel.Location = new System.Drawing.Point(12, 120);
            this.outputLabel.Name = "outputLabel";
            this.outputLabel.Size = new System.Drawing.Size(39, 13);
            this.outputLabel.TabIndex = 0;
            this.outputLabel.Text = "Output";
            // 
            // outputText
            // 
            this.outputText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.outputText.Location = new System.Drawing.Point(14, 146);
            this.outputText.Multiline = true;
            this.outputText.Name = "outputText";
            this.outputText.ReadOnly = true;
            this.outputText.Size = new System.Drawing.Size(355, 172);
            this.outputText.TabIndex = 7;
            this.outputText.Text = "WARNING! This will modify the Iso/Bin, make a copy first if you may need the orig" +
                "inal file in future";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(378, 333);
            this.Controls.Add(this.outputText);
            this.Controls.Add(this.outputLabel);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.replacementFilePath);
            this.Controls.Add(this.replaceFileLabel);
            this.Controls.Add(this.newFileButton);
            this.Controls.Add(this.newFilePath);
            this.Controls.Add(this.insertFileLabel);
            this.Controls.Add(this.selectIsoButton);
            this.Controls.Add(this.isoFileName);
            this.Controls.Add(this.isoFileLabel);
            this.Name = "MainForm";
            this.Text = "Replace Iso/Bin File";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label isoFileLabel;
        private System.Windows.Forms.TextBox isoFileName;
        private System.Windows.Forms.Button selectIsoButton;
        private System.Windows.Forms.Label insertFileLabel;
        private System.Windows.Forms.TextBox newFilePath;
        private System.Windows.Forms.Button newFileButton;
        private System.Windows.Forms.Label replaceFileLabel;
        private System.Windows.Forms.TextBox replacementFilePath;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label outputLabel;
        private System.Windows.Forms.TextBox outputText;
    }
}

