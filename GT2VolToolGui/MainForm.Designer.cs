namespace GT2VolToolGui
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
            this.browseVolButton = new System.Windows.Forms.Button();
            this.volNameTextBox = new System.Windows.Forms.TextBox();
            this.volNameLabel = new System.Windows.Forms.Label();
            this.directoryLabel = new System.Windows.Forms.Label();
            this.dirNameTextBox = new System.Windows.Forms.TextBox();
            this.browseDirectoryButton = new System.Windows.Forms.Button();
            this.volToDirButton = new System.Windows.Forms.Button();
            this.dirToVolButton = new System.Windows.Forms.Button();
            this.gt2kVolToDir = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.gt3VolToDir = new System.Windows.Forms.Button();
            this.decompCheckBox = new System.Windows.Forms.CheckBox();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // browseVolButton
            // 
            this.browseVolButton.Location = new System.Drawing.Point(335, 21);
            this.browseVolButton.Name = "browseVolButton";
            this.browseVolButton.Size = new System.Drawing.Size(74, 25);
            this.browseVolButton.TabIndex = 0;
            this.browseVolButton.Text = "Browse VOL";
            this.browseVolButton.UseVisualStyleBackColor = true;
            this.browseVolButton.Click += new System.EventHandler(this.browseVolButton_Click);
            // 
            // volNameTextBox
            // 
            this.volNameTextBox.Location = new System.Drawing.Point(12, 24);
            this.volNameTextBox.Name = "volNameTextBox";
            this.volNameTextBox.Size = new System.Drawing.Size(317, 20);
            this.volNameTextBox.TabIndex = 1;
            this.volNameTextBox.TextChanged += new System.EventHandler(this.volNameTextBox_TextChanged);
            // 
            // volNameLabel
            // 
            this.volNameLabel.AutoSize = true;
            this.volNameLabel.Location = new System.Drawing.Point(12, 8);
            this.volNameLabel.Name = "volNameLabel";
            this.volNameLabel.Size = new System.Drawing.Size(50, 13);
            this.volNameLabel.TabIndex = 2;
            this.volNameLabel.Text = "VOL File:";
            // 
            // directoryLabel
            // 
            this.directoryLabel.AutoSize = true;
            this.directoryLabel.Location = new System.Drawing.Point(13, 55);
            this.directoryLabel.Name = "directoryLabel";
            this.directoryLabel.Size = new System.Drawing.Size(52, 13);
            this.directoryLabel.TabIndex = 3;
            this.directoryLabel.Text = "Directory:";
            // 
            // dirNameTextBox
            // 
            this.dirNameTextBox.Location = new System.Drawing.Point(12, 71);
            this.dirNameTextBox.Name = "dirNameTextBox";
            this.dirNameTextBox.Size = new System.Drawing.Size(317, 20);
            this.dirNameTextBox.TabIndex = 5;
            this.dirNameTextBox.TextChanged += new System.EventHandler(this.dirNameTextBox_TextChanged);
            // 
            // browseDirectoryButton
            // 
            this.browseDirectoryButton.Location = new System.Drawing.Point(335, 68);
            this.browseDirectoryButton.Name = "browseDirectoryButton";
            this.browseDirectoryButton.Size = new System.Drawing.Size(74, 25);
            this.browseDirectoryButton.TabIndex = 4;
            this.browseDirectoryButton.Text = "Browse &Dir";
            this.browseDirectoryButton.UseVisualStyleBackColor = true;
            this.browseDirectoryButton.Click += new System.EventHandler(this.browseDirectoryButton_Click);
            // 
            // volToDirButton
            // 
            this.volToDirButton.Enabled = false;
            this.volToDirButton.Location = new System.Drawing.Point(11, 100);
            this.volToDirButton.Name = "volToDirButton";
            this.volToDirButton.Size = new System.Drawing.Size(98, 38);
            this.volToDirButton.TabIndex = 6;
            this.volToDirButton.Text = "&Explode GT2 VOL to Directory";
            this.volToDirButton.UseVisualStyleBackColor = true;
            this.volToDirButton.Click += new System.EventHandler(this.volToDirButton_Click);
            // 
            // dirToVolButton
            // 
            this.dirToVolButton.Enabled = false;
            this.dirToVolButton.Location = new System.Drawing.Point(113, 100);
            this.dirToVolButton.Name = "dirToVolButton";
            this.dirToVolButton.Size = new System.Drawing.Size(98, 38);
            this.dirToVolButton.TabIndex = 7;
            this.dirToVolButton.Text = "&Make GT2 VOL from Directory";
            this.dirToVolButton.UseVisualStyleBackColor = true;
            this.dirToVolButton.Click += new System.EventHandler(this.dirToVolButton_Click);
            // 
            // gt2kVolToDir
            // 
            this.gt2kVolToDir.Enabled = false;
            this.gt2kVolToDir.Location = new System.Drawing.Point(215, 100);
            this.gt2kVolToDir.Name = "gt2kVolToDir";
            this.gt2kVolToDir.Size = new System.Drawing.Size(98, 38);
            this.gt2kVolToDir.TabIndex = 8;
            this.gt2kVolToDir.Text = "Explode GT&2000 VOL to Directory";
            this.gt2kVolToDir.UseVisualStyleBackColor = true;
            this.gt2kVolToDir.Click += new System.EventHandler(this.gt2kVolToDir_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusText});
            this.statusStrip.Location = new System.Drawing.Point(0, 167);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(421, 22);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 9;
            // 
            // statusText
            // 
            this.statusText.Name = "statusText";
            this.statusText.Size = new System.Drawing.Size(38, 17);
            this.statusText.Text = "Hello!";
            // 
            // gt3VolToDir
            // 
            this.gt3VolToDir.Enabled = false;
            this.gt3VolToDir.Location = new System.Drawing.Point(318, 100);
            this.gt3VolToDir.Name = "gt3VolToDir";
            this.gt3VolToDir.Size = new System.Drawing.Size(98, 38);
            this.gt3VolToDir.TabIndex = 10;
            this.gt3VolToDir.Text = "Explode GT&3 VOL to Directory";
            this.gt3VolToDir.UseVisualStyleBackColor = true;
            this.gt3VolToDir.Click += new System.EventHandler(this.gt3VolToDir_Click);
            // 
            // decompCheckBox
            // 
            this.decompCheckBox.AutoSize = true;
            this.decompCheckBox.Location = new System.Drawing.Point(14, 145);
            this.decompCheckBox.Name = "decompCheckBox";
            this.decompCheckBox.Size = new System.Drawing.Size(153, 17);
            this.decompCheckBox.TabIndex = 11;
            this.decompCheckBox.Text = "Decompress e&xtracted files";
            this.decompCheckBox.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(421, 189);
            this.Controls.Add(this.decompCheckBox);
            this.Controls.Add(this.gt3VolToDir);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.gt2kVolToDir);
            this.Controls.Add(this.dirToVolButton);
            this.Controls.Add(this.volToDirButton);
            this.Controls.Add(this.dirNameTextBox);
            this.Controls.Add(this.browseDirectoryButton);
            this.Controls.Add(this.directoryLabel);
            this.Controls.Add(this.volNameLabel);
            this.Controls.Add(this.volNameTextBox);
            this.Controls.Add(this.browseVolButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "GTVolTool";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button browseVolButton;
        private System.Windows.Forms.TextBox volNameTextBox;
        private System.Windows.Forms.Label volNameLabel;
        private System.Windows.Forms.Label directoryLabel;
        private System.Windows.Forms.TextBox dirNameTextBox;
        private System.Windows.Forms.Button browseDirectoryButton;
        private System.Windows.Forms.Button volToDirButton;
        private System.Windows.Forms.Button dirToVolButton;
        private System.Windows.Forms.Button gt2kVolToDir;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusText;
        private System.Windows.Forms.Button gt3VolToDir;
        private System.Windows.Forms.CheckBox decompCheckBox;
    }
}

