using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace GMCreator
{
    public partial class SettingsForm : Form
    {
        private int[] customColours;
        private bool textSetFromColourPicker;
        private RadioButton[] versionButtons;

        public SettingsForm()
        {
            InitializeComponent();
            versionButtons = new RadioButton[9];
            anchorColourHex.Tag = anchorColourSwatch;
            anchorColourSwatch.Tag = anchorColourHex;
            drawBoxColourHex.Tag = drawBoxColourSwatch;
            drawBoxColourSwatch.Tag = drawBoxColourHex;
            selectedBoxColourHex.Tag = selectedBoxColourSwatch;
            selectedBoxColourSwatch.Tag = selectedBoxColourHex;
            customColours = new int[16];
            versionButtons[(int)IconImgType.JP10] = gt2VersionJP10;
            versionButtons[(int)IconImgType.JP11] = gt2VersionJP11;
            versionButtons[(int)IconImgType.US10] = gt2VersionUS10;
            versionButtons[(int)IconImgType.US12] = gt2VersionUS12;
            versionButtons[(int)IconImgType.PALEng] = gt2VersionPALEng;
            versionButtons[(int)IconImgType.PALFra] = gt2VersionPALFra;
            versionButtons[(int)IconImgType.PALGer] = gt2VersionPALGer;
            versionButtons[(int)IconImgType.PALIta] = gt2VersionPALIta;
            versionButtons[(int)IconImgType.PALSpa] = gt2VersionPALSpa;
            LoadSettings();
            NeedsHardcodedRefresh = false;
        }

        private void LoadSettings()
        {
            textSetFromColourPicker = true;
            try
            {
                AppSettings settings = Globals.App;
                centralAnchorWSize.Value = settings.CentralAnchorWidth;
                centralAnchorHSize.Value = settings.CentralAnchorHeight;

                cornerAnchorWSize.Value = settings.ResizeAnchorWidth;
                cornerAnchorHSize.Value = settings.ResizeAnchorHeight;

                anchorColourSwatch.BackColor = settings.AnchorColour;
                anchorColourHex.Text = FormatColorString(settings.AnchorColour);

                drawBoxColourSwatch.BackColor = settings.DefaultOutlineColour;
                drawBoxColourHex.Text = FormatColorString(settings.DefaultOutlineColour);

                selectedBoxColourSwatch.BackColor = settings.SelectedOutlineColour;
                selectedBoxColourHex.Text = FormatColorString(settings.SelectedOutlineColour);

                compressionLevelNumber.Value = settings.CompressionLevel;

                if (settings.GT2Version != IconImgType.Invalid)
                {
                    versionButtons[(int)settings.GT2Version].Checked = true;
                    okSettingsButton.Enabled = true;
                }
                else
                {
                    versionSettingsBox.ForeColor = Color.Red;
                }
            }
            finally
            {
                textSetFromColourPicker = false;
            }
        }

        private void SaveSettings()
        {
            AppSettings settings = Globals.App;
            settings.CentralAnchorWidth = (int)centralAnchorWSize.Value;
            settings.CentralAnchorHeight = (int)centralAnchorHSize.Value;

            settings.ResizeAnchorWidth = (int)cornerAnchorWSize.Value;
            settings.ResizeAnchorHeight = (int)cornerAnchorHSize.Value;

            settings.AnchorColour = anchorColourSwatch.BackColor;

            settings.DefaultOutlineColour = drawBoxColourSwatch.BackColor;

            settings.SelectedOutlineColour = selectedBoxColourSwatch.BackColor;

            settings.CompressionLevel = (int)compressionLevelNumber.Value;

            IconImgType origVersion = settings.GT2Version;
            for (int i = 0; i < versionButtons.Length; ++i)
            {
                if (versionButtons[i].Checked)
                {
                    settings.GT2Version = (IconImgType)i;
                    break;
                }
            }

            NeedsHardcodedRefresh = (origVersion != settings.GT2Version);

            settings.RefreshNonSerialised();
            settings.FireSettingsChanged();
        }

        private static string FormatColorString(Color c)
        {
            return String.Format("#{0:X}", c.ToArgb());
        }

        private void ParseHexColourAndSetSwatch(object sender, EventArgs e)
        {
            if (!textSetFromColourPicker)
            {
                TextBox tb = (TextBox)sender;
                string tbText = tb.Text;
                if (tbText.Length > 0)
                {
                    int color;
                    if(tbText[0] == '#')
                    {
                        tbText = tbText.Substring(1);
                    }
                    if (Int32.TryParse(tbText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color))
                    {
                        Color c = Color.FromArgb(color);
                        PictureBox pbRelated = (PictureBox)tb.Tag;
                        pbRelated.BackColor = c;
                        pbRelated.Invalidate();
                    }
                }
            }
        }

        private void ShowColourBoxAndSetText(object sender, EventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            using (ColorDialog cd = new ColorDialog())
            {
                cd.AllowFullOpen = true;
                cd.CustomColors = customColours;
                cd.Color = pb.BackColor;
                if (cd.ShowDialog() != DialogResult.Cancel)
                {
                    TextBox relatedText = (TextBox)pb.Tag;
                    pb.BackColor = cd.Color;
                    textSetFromColourPicker = true;
                    relatedText.Text = FormatColorString(cd.Color);
                    textSetFromColourPicker = false;
                    customColours = cd.CustomColors;
                }
            }
        }

        private void okSettingsButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void gt2Version_CheckedChanged(object sender, EventArgs e)
        {
            okSettingsButton.Enabled = true;
            versionSettingsBox.ForeColor = exportSettingsBox.ForeColor;
        }

        public bool NeedsHardcodedRefresh
        {
            get;
            set;
        }
    }
}
