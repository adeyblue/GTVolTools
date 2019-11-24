namespace GTMP
{
    partial class GTMPForm
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
            this.picBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picBox)).BeginInit();
            this.SuspendLayout();
            // 
            // picBox
            // 
            this.picBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picBox.Location = new System.Drawing.Point(0, 0);
            this.picBox.Name = "picBox";
            this.picBox.Size = new System.Drawing.Size(508, 384);
            this.picBox.TabIndex = 0;
            this.picBox.TabStop = false;
            this.picBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.picBox_DragDrop);
            this.picBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.picBox_DragEnter);
            // 
            // GTMPForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 384);
            this.Controls.Add(this.picBox);
            this.Name = "GTMPForm";
            this.Text = "GTMP (Drag + Drop)";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.GTMPForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.GTMPForm_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.picBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picBox;
    }
}

