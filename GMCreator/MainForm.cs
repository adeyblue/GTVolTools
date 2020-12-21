using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GMCreator
{
    public partial class MainForm : Form
    {
        private MouseState mouseState;
        private bool unsavedChanges;

        private BoxList allBoxes;

        // Drawing new boxes stuff
        private Point anchorClick;
        private Point lastSizingCursorMove;

        // hit test/box size-move
        private IBox currentMovingOrSizing;
        private IBox.BoxHitTest hitType;

        // current project file we're manipulating
        private string currentProjectFileName;

        // the GMLL data of the foregrond image
        private byte[] currentForegroundGMLLData;

        public const int CANVAS_WIDTH = 512;
        public const int CANVAS_HEIGHT = 504;

        private delegate void PostImageLoad(string fileName, Images.ImageLoadResult image);

        public MainForm()
        {
            InitializeComponent();
            InitializeGlobals(Application.StartupPath);
            canvas.AllowDrop = true;
            allBoxes = new BoxList();
            boxList.DataSource = allBoxes.Items;
            // previous selected index
            boxList.Tag = -1;
            ResetEverything();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
#if DEBUG
            AddDebugMenu();
#endif
        }

        void SaveDebugLog(object sender, EventArgs e)
        {
            string debugFile = System.IO.Path.Combine(Application.StartupPath, "log.txt");
            System.IO.File.WriteAllText(debugFile, DebugLogger.GetContents());
        }

#region "Form Events"
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CloseCurrentFile())
            {
                e.Cancel = true;
            }
            else
            {
                Rectangle winBounds;
                if (WindowState == FormWindowState.Normal)
                {
                    winBounds = Bounds;
                }
                else
                {
                    winBounds = RestoreBounds;
                }
                Globals.Save(Application.StartupPath, winBounds);
                if (DebugLogger.DoDebugActions())
                {
                    string debugFile = Globals.MakeDebugSaveName(true, "log.txt");
                    System.IO.File.WriteAllText(debugFile, DebugLogger.GetContents());
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Globals.App.WindowLocation != Rectangle.Empty)
            {
                this.Bounds = Globals.App.WindowLocation;
            }
            toggleContentsToolStripMenuItem.Checked = Globals.App.ShowInnerContent;
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            Keys ctrlShift = Keys.Control | Keys.Shift;
            if ((e.KeyCode == Keys.F9) && ((e.Modifiers & ctrlShift) == ctrlShift))
            {
                string fileName = GetSaveFileName(this, "Save Log", "Log files (*.log, *.txt)|*.log,*.txt");
                if (String.IsNullOrEmpty(fileName))
                {
                    return;
                }
                System.IO.File.WriteAllText(fileName, DebugLogger.GetContents());
            }
        }

        private void canvasFocusTextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            object currentSelected = boxPropertyList.SelectedObject;
            Control activeControl = FindFocusedControl(ActiveControl);
            if ((currentSelected == null) || (activeControl != canvasFocusTextBox))
            {
                return;
            }
            int xDelta = 0, yDelta = 0;
            switch (e.KeyCode)
            {
                case Keys.Down: ++yDelta; break;
                case Keys.Up: --yDelta; break;
                case Keys.Left: --xDelta; break;
                case Keys.Right: ++xDelta; break;
                default: return;
            }
            IBox selectedBox = (IBox)currentSelected;
            selectedBox.Move(new Point(xDelta, yDelta));
            SetUnsavedChanges();
        }
#endregion

#region "Menu Events"
        private void openBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugLogger.Log("Main", "Opening background by menu");
            LoadAndDisplayFile(new PostImageLoad(BGImageLoad));
            RedrawCanvas();
        }

        private void openForegroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugLogger.Log("Main", "Opening foreground by menu");
            LoadAndDisplayFile(new PostImageLoad(FGImageLoad));
            RedrawCanvas();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void InsertBoxMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            DebugLogger.Log("Main", "Adding icon {0} by menu", item.Text);
            IconImgBox imb = new IconImgBox(item.Text.Replace("&", String.Empty), (int)item.Tag);
            allBoxes.Add(imb);
            imb.DisplayChanged += new BoxDisplayChange(BoxInvalidation);
            SetUnsavedChanges();
            RedrawCanvas(imb.Bounds);
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Clear all boxes, are you sure?", "GM Creator", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res != DialogResult.Yes)
            {
                return;
            }
            DebugLogger.Log("Main", "Clearing all boxes");
            allBoxes.Clear();
            Box.ResetIndexCount();
            RedrawCanvas();
            SetUnsavedChanges();
        }

        private void toggleContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem toggle = (ToolStripMenuItem)sender;
            Globals.App.ShowInnerContent = toggle.Checked;
            DebugLogger.Log("Main", "Global inner content toggle to {0}", toggle.Checked);
            RedrawCanvas();
        }

        private void saveGMProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonProjectSave(currentProjectFileName);
        }

        private void saveGMProjectAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonProjectSave(null);
        }

        private void loadGMProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectLoad();
        }

        private void exportGMFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToGMFile();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseCurrentFile();
        }

        private void convertImageToBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.ConvertImageToGTMP(this);
        }

        private void splitCommonpicdatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.SplitCommonPic(this);
        }

        private void splitGTMenuDatdatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.SplitGTMenuDat(this);
        }

        private void makeCommonPicdatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.MakeCommonPic(this);
        }

        private void makeGTMenuDatdatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.MakeGTMenuDat(this);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SettingsForm sf = new SettingsForm())
            {
                if (sf.ShowDialog(this) == DialogResult.OK)
                {
                    if (sf.NeedsHardcodedRefresh)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(RefreshHardcodedData), Application.StartupPath);
                    }
                }
            }
        }

#if DEBUG
        private void dumpCanvas_Click(object sender, EventArgs e)
        {
            using (Bitmap bm = new Bitmap(CANVAS_WIDTH, CANVAS_HEIGHT))
            {
                GraphicsUnit unit = GraphicsUnit.Pixel;
                canvas.DrawToBitmap(bm, Rectangle.Ceiling(bm.GetBounds(ref unit)));
                bm.Save(Path.Combine(Application.StartupPath, "canvas.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
#endif
#endregion

#region "Canvas Events"

        private void BoxInvalidation(object sender, BoxDisplayChangeEventArgs e)
        {
            // This is more efficient, but when things extend outside the 
            // box bounds this causes things outside to remain on the canvas
            // invalidating the whole canvas is a bit of a hack, so I don't
            // have to save and calculate where the things outside of the box are
            // so I can invalidate it properly
            //RedrawCanvasNoUpdate(e.InvalidatedArea);
            RedrawCanvasNoUpdate(canvas.ClientRectangle);
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateCanvasMouseCoords(e.X, e.Y);
            switch(mouseState)
            {
                case MouseState.DrawSizingRect:
                {
                    lastSizingCursorMove = e.Location;
                    Rectangle lastDrawnRect = GetDirtyRect();
                    UpdateDirtyRect(lastSizingCursorMove);
                    RedrawCanvas(lastDrawnRect);
                    UpdateCanvasCursorRectText(anchorClick, lastSizingCursorMove);
                }
                break;
                case MouseState.BoxMove:
                {
                    DragBox(e.Location);
                }
                break;
                case MouseState.BoxSize:
                {
                    ResizeBox(e.Location);
                }
                break;
                case MouseState.Normal:
                {
                    DoHitTest(e.Location);
                }
                break;
            }
        }

        private void canvas_MouseClick(object sender, MouseEventArgs e)
        {
            switch (mouseState)
            {
                case MouseState.Normal:
                {
                    if (e.Button != MouseButtons.Right)
                    {
                        anchorClick = e.Location;
                        mouseState = MouseState.DrawSizingRect;
                        DebugLogger.Log("Main", "Started drawing rectangle at {0}", anchorClick);
                    }
                }
                break;
                case MouseState.DrawSizingRect:
                {
                    // right click cancels any drawing
                    if (e.Button != MouseButtons.Right)
                    {
                        Box b = allBoxes.AddNewBox(MakeGoodRectangle(anchorClick, e.Location));
                        b.DisplayChanged += new BoxDisplayChange(BoxInvalidation);
                        ChangeSelectedBox(b);
                        SetUnsavedChanges();
                        DebugLogger.Log("Main", "Drew rectangle at {0}", b.Location);
                    }
                    else
                    {
                        UpdateDirtyRect(e.Location);
                        DebugLogger.Log("Main", "Reset rectangle drawing");
                    }
                    ResetToNormalCanvas();
                }
                break;
            }
        }

        private void DisplayDrawFailureMessage(string message)
        {
            DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Error, message);
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);
            Debug.Assert((canvas.Width == CANVAS_WIDTH) && (canvas.Height == CANVAS_HEIGHT), "Canvas is the wrong size!");
            Rectangle currentRect;
            if(mouseState == MouseState.DrawSizingRect)
            {
                currentRect = MakeGoodRectangle(anchorClick, lastSizingCursorMove);
            }
            else currentRect = Rectangle.Empty;
            try
            {
                allBoxes.Draw(e.Graphics, e.ClipRectangle, currentRect);
            }
            catch (Exception exc)
            {
                // do this in a seperate callback as doing it here may trigger an endless loop of 
                // message box->paint->exception->message box->Paint->exception etc
                string message = String.Format("Caught exception {0} while drawing boxes. This may be because this foreground has icon images that do not exist in this GT2 settings version", exc.Message);
                BeginInvoke(new Action<string>(DisplayDrawFailureMessage), message);
            }
        }

        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (
                (mouseState == MouseState.Normal) &&
                (e.Button == MouseButtons.Left) &&
                (hitType != IBox.BoxHitTest.None)
            )
            {
                mouseState = (hitType == IBox.BoxHitTest.AnchorPoint) ? MouseState.BoxMove : MouseState.BoxSize;
                anchorClick = e.Location;
                ChangeSelectedBox(currentMovingOrSizing);
                canvasFocusTextBox.Select();
            }
        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (
                (mouseState == MouseState.BoxMove) ||
                (mouseState == MouseState.BoxSize)
            )
            {
                mouseState = MouseState.Normal;
                anchorClick = Point.Empty;
            }
        }

        private void canvas_DragDrop(object sender, DragEventArgs e)
        {
            IDataObject obj = e.Data;
            if (obj.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] files = (string[])obj.GetData(DataFormats.FileDrop, true);
                if (files.Length >= 1)
                {
                    string loadedFile = files[0];
                    loadedFile = System.IO.Path.GetFullPath(loadedFile);
                    DebugLogger.Log("Main", "Loading file {0} by drag and drop", loadedFile);
                    LoadAndDisplaySelectedFile(loadedFile, new PostImageLoad(FGImageLoad));
                }
            }
            else e.Effect = DragDropEffects.None;
        }

        private void canvas_DragEnter(object sender, DragEventArgs e)
        {
            IDataObject obj = e.Data;
            e.Effect = (obj.GetDataPresent(DataFormats.FileDrop, true)) ? DragDropEffects.Copy : DragDropEffects.None;
        }
#endregion

#region "BoxList Events"
        private void boxList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int previousSelected = (int)boxList.Tag;
            if ((previousSelected > -1) && (previousSelected < allBoxes.Count))
            {
                allBoxes[previousSelected].Select(false);
            }
            boxPropertyList.SelectedObject = boxList.SelectedItem;
            int newSelected = boxList.SelectedIndex;
            if (newSelected != -1)
            {
                allBoxes[newSelected].Select(true);
            }
            boxList.Tag = newSelected;
        }

        private void boxCopyMenuItem_Click(object sender, EventArgs e)
        {
            CloneCurrentBox();
        }

        private void boxDeleteMenuItem_Click(object sender, EventArgs e)
        {
            RemoveCurrentBox();
        }

        private void boxList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.C))
            {
                CloneCurrentBox();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                RemoveCurrentBox();
            }
        }
#endregion

#region "Property List Events"
        // this is attached to both property lists!
        private void PropertyList_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            SetUnsavedChanges();
        }
#endregion
    }

    enum MouseState
    {
        Normal,
        DrawSizingRect,
        BoxMove,
        BoxSize
    }
}
