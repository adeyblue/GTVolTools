using System;
using System.Collections.Generic;
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
        const string APP_TITLE = "GT2 GMCreator";
        private void InitializeGlobals(string appDir)
        {
            Globals.Load(appDir);
            if (Globals.App.GT2Version == IconImgType.Invalid)
            {
                using (SettingsForm sf = new SettingsForm())
                {
                    while (Globals.App.GT2Version == IconImgType.Invalid)
                    {
                        sf.ShowDialog();
                    }
                }
            }
            ThreadPool.QueueUserWorkItem(new WaitCallback(RefreshHardcodedData), appDir);
        }

        private void RefreshHardcodedData(object o)
        {
            Hardcoded.Refresh((string)o);
            Dictionary<string, int> ids = Hardcoded.IconImages.GetNamesAndIndexes();
            List<ToolStripMenuItem> insertItems = new List<ToolStripMenuItem>(ids.Count);
            EventHandler clickHandler = new EventHandler(InsertBoxMenuItemClick);
            List<char> mnemonicKeys = new List<char>(insertItems.Count);

            List<string> names = new List<string>(ids.Keys);
            names.Sort();
            foreach (string name in names)
            {
                string toUse = name;
                int nameLen = name.Length;
                int numMnenonics = mnemonicKeys.Count;
                for (int i = 0; i < nameLen; ++i)
                {
                    char thisLowerChar = Char.ToLowerInvariant(name[i]);
                    int j = 0;
                    for (; j < numMnenonics; ++j)
                    {
                        if (thisLowerChar == mnemonicKeys[j])
                        {
                            break;
                        }
                    }
                    if (j == numMnenonics)
                    {
                        mnemonicKeys.Add(thisLowerChar);
                        toUse = name.Insert(i, "&");
                        break;
                    }
                }
                ToolStripMenuItem menuItem = new ToolStripMenuItem(toUse);
                menuItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
                menuItem.Click += clickHandler;
                menuItem.Tag = ids[name];
                insertItems.Add(menuItem);
            }
            mainMenu.Invoke(new Action<List<ToolStripMenuItem>>(RefreshInsertMenuAndCanvas), insertItems);
        }

        private void RefreshInsertMenuAndCanvas(List<ToolStripMenuItem> newItems)
        {
            insertToolStripMenuItem.DropDownItems.Clear();
            insertToolStripMenuItem.DropDownItems.AddRange(newItems.ToArray());
            UpdateDirtyRect(Point.Empty);
            UpdateDirtyRect(new Point(CANVAS_WIDTH, CANVAS_HEIGHT));
            ResetToNormalCanvas();
        }

        private void UpdateCanvasMouseCoords(int x, int y)
        {
            string coordSring = String.Format("{0}x{1}", x, y);
            mouseXYText.Text = coordSring;
        }

        private void UpdateCanvasCursorRectText(Point oneCorner, Point otherCorner)
        {
            cursorRectText.Visible = true;
            int xDist = Math.Abs(oneCorner.X - otherCorner.X);
            int yDist = Math.Abs(oneCorner.Y - otherCorner.Y);
            string coordString = String.Format("Rect: {0}x{1}", xDist, yDist);
            cursorRectText.Text = coordString;
        }

        private void RedrawCanvas(Rectangle r)
        {
            canvas.Invalidate(r);
            canvas.Update();
        }

        private void RedrawCanvas()
        {
            RedrawCanvas(canvas.ClientRectangle);
        }

        private void RedrawCanvasNoUpdate(Rectangle r)
        {
            canvas.Invalidate(r);
        }

        private void HideCursorRectText()
        {
            cursorRectText.Visible = false;
        }

        private Rectangle MakeGoodRectangle(Point p1, Point p2)
        {
            Rectangle r = new Rectangle(
                new Point(
                    Math.Min(p1.X, p2.X),
                    Math.Min(p1.Y, p2.Y)
                ),
                new Size(
                    Math.Abs(p1.X - p2.X),
                    Math.Abs(p1.Y - p2.Y)
                )
            );
            return r;
        }

        private void LoadAndDisplayFile(PostImageLoad afterLoadFn)
        {
            const string imageFilter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg|GT2 Files (*.gtmp, *.gm, *.gz)|*.gtmp;*.gm;*.gz|All Files (*.*)|*.*";
            string fileName = GetOpenFileName(this, "Open an image", imageFilter, null);
            if (String.IsNullOrEmpty(fileName))
            {
                return;
            }
            LoadAndDisplaySelectedFile(fileName, afterLoadFn);
        }

        private void LoadAndDisplaySelectedFile(string fileName, PostImageLoad afterLoadFn)
        {
            DebugLogger.Log("Main", "Loading {0} with afterloader {1}...", fileName, afterLoadFn.Method.Name);
            try
            {
                Images.ImageLoadResult imageload = Images.LoadFile(fileName);
                DebugLogger.Log("Main", "...Image was a {0}", imageload.type.ToString());
                Bitmap image = imageload.image;
                if ((image.Width != CANVAS_WIDTH) || (image.Height != CANVAS_HEIGHT))
                {
                    DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Warning, "'{0}' was loaded, but it's the wrong size {1}. Should be {2}x{3}", Path.GetFileName(fileName), image.Size, CANVAS_WIDTH, CANVAS_HEIGHT);
                    image.Dispose();
                    return;
                }
                afterLoadFn(fileName, imageload);
            }
            catch (Exception e)
            {
                DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Error, "Failed to load {0} because of error {1}", fileName, e.Message);
            }
        }

        public static DialogResult DisplayMsgBox(MessageBoxButtons buttons, MessageBoxIcon icon, string messageFmt, params object[] p)
        {
            string message = String.Format(messageFmt, p);
            DebugLogger.Log("Main", message);
            return MessageBox.Show(message, "GMCreator", buttons, icon);
        }

        //private void BGImageLoad(string fileName, Images.ImageLoadResult info)
        //{
        //    Image origBG = canvas.BackgroundImage;
        //    DebugLogger.Log("Main", "Loaded BG of type {0} from {1}", info.type.ToString(), fileName);
        //    if (DebugLogger.DoDebugActions())
        //    {
        //        string newBGName = Globals.MakeDebugSaveName(true, Path.GetFileName(fileName));
        //        File.Copy(fileName, newBGName, true);
        //        string newConvertedBG = Globals.MakeDebugSaveName(false, "newconvertedbg.png");
        //        info.image.Save(newConvertedBG, System.Drawing.Imaging.ImageFormat.Png);
        //    }
        //    canvas.BackgroundImage = info.image;
        //    if(origBG != null)
        //    {
        //        if (DebugLogger.DoDebugActions())
        //        {

        //            string previousBG = Globals.MakeDebugSaveName(false, "previousbg.png");
        //            origBG.Save(previousBG, System.Drawing.Imaging.ImageFormat.Png);
        //        }
        //        origBG.Dispose();
        //    }
        //}

        private void RecompositeCanvasImage()
        {
            Bitmap bm = new Bitmap(CANVAS_WIDTH, CANVAS_HEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            LayerStateManager.Layers fgbg = layerState.Query(LayerStateManager.Layers.Background | LayerStateManager.Layers.Foreground);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.Clear(Color.Black);
                if ((canvasBgImage != null) && ((fgbg & LayerStateManager.Layers.Background) != 0))
                {
                    g.DrawImage(canvasBgImage, Point.Empty);
                }
                if ((canvasFgImage != null) && ((fgbg & LayerStateManager.Layers.Foreground) != 0))
                {
                    g.DrawImage(canvasFgImage, Point.Empty);
                }
            }
            if (DebugLogger.DoDebugActions())
            {
                string compositedName = Globals.MakeDebugSaveName(false, "newcanvas.png");
                bm.Save(compositedName, System.Drawing.Imaging.ImageFormat.Png);
            }
            Image oldImage = canvas.Image;
            canvas.Image = bm;
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        private void BGImageLoad(string fileName, Images.ImageLoadResult info)
        {
            Image origBG = canvasBgImage;
            DebugLogger.Log("Main", "Loaded BG of type {0} from {1}", info.type.ToString(), fileName);
            if (DebugLogger.DoDebugActions())
            {
                string newBGName = Globals.MakeDebugSaveName(true, Path.GetFileName(fileName));
                File.Copy(fileName, newBGName, true);
                string newConvertedBG = Globals.MakeDebugSaveName(false, "newconvertedbg.png");
                info.image.Save(newConvertedBG, System.Drawing.Imaging.ImageFormat.Png);
            }
            layerState.On(LayerStateManager.Layers.Background);
            canvasBgImage = info.image;
            RecompositeCanvasImage();
            if (origBG != null)
            {
                if (DebugLogger.DoDebugActions())
                {
                    string previousBG = Globals.MakeDebugSaveName(false, "previousbg.png");
                    origBG.Save(previousBG, System.Drawing.Imaging.ImageFormat.Png);
                }
                origBG.Dispose();
            }
        }

        private List<IBox> ConvertGMFileBoxes(List<GTMP.GMFile.DrawRectInfo> boxes)
        {
            List<IBox> newBoxes = new List<IBox>(boxes.Count);
            Dictionary<string, int> iconImages = Hardcoded.IconImages.GetNamesAndIndexes();
            foreach (GTMP.GMFile.DrawRectInfo dri in boxes)
            {
                IBox toAdd;
                if (dri.infoBox != null)
                {
                    GTMP.GMFile.InfoBox infoBox = dri.infoBox;
                    Box newBox = new Box(dri.rect);
                    newBox.BehaviourAttributes = infoBox.attributes;
                    newBox.LinkToScreen = infoBox.GetScreenLink();
                    newBox.PrizeMoneyPosition = infoBox.GetSpecificPlaceNumber();
                    newBox.RaceOrWheelOrCarId = infoBox.GetWheelRaceOrCarIdName();
                    newBox.Contents = infoBox.contents;
                    newBox.QueryAttributes = infoBox.queryType;
                    newBox.ArrowEnabler = infoBox.GetArrowEnablingLicense();
                    toAdd = newBox;
                }
                else
                {
                    Debug.Assert(dri.iconImgBox != null);
                    GTMP.GMFile.IconImageBox imgBox = dri.iconImgBox;
                    IconImgEntry foundIcon = Hardcoded.IconImages.FindFromData(imgBox.imgMapX, imgBox.imgMapY);
                    if (foundIcon != null)
                    {
                        IconImgBox newBox = new IconImgBox(foundIcon);
                        newBox.Location = new Point(imgBox.screenX, imgBox.screenY);
                        toAdd = newBox;
                    }
                    else
                    {
                        DebugLogger.Log("GMFileLoader", "Couldn't find iconimg with location {0}x{1}, not adding", imgBox.imgMapX, imgBox.imgMapY);
                        DisplayMsgBox(
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning,
                            "Loading the foreground file has stopped because it contained an unknown icon image at image map location {0}. " +
                            "Did you forget to change the GT2 Version in Settings before loading?",
                            new Point(imgBox.imgMapX, imgBox.imgMapY)
                        );
                        break;
                    }
                }
                toAdd.DisplayChanged += new BoxDisplayChange(BoxInvalidation);
                newBoxes.Add(toAdd);
            }
            return newBoxes;
        }

        private void ReplaceForegroundImage(byte[] foregroundGMLL, Bitmap image)
        {
            Image origFG = canvasFgImage;
            if (DebugLogger.DoDebugActions())
            {
                string origImageName = Globals.MakeDebugSaveName(false, "newFG.png");
                image.Save(origImageName, System.Drawing.Imaging.ImageFormat.Png);
            }
            canvasFgImage = image;
            RecompositeCanvasImage();
            if (origFG != null)
            {
                if (DebugLogger.DoDebugActions())
                {
                    string saveFile = Globals.MakeDebugSaveName(false, "origFG.png");
                    origFG.Save(saveFile, System.Drawing.Imaging.ImageFormat.Png);
                }
                origFG.Dispose();
            }
            currentForegroundGMLLData = foregroundGMLL;
        }

        private void FGImageLoad(string fileName, Images.ImageLoadResult info)
        {
            if (DebugLogger.DoDebugActions())
            {
                string fgImageName = Globals.MakeDebugSaveName(true, Path.GetFileName(fileName));
                File.Copy(fileName, fgImageName, true);
            }
            if(info.type != Images.ImageType.GM)
            {
                byte[] newForegroundData;
                if (info.type != Images.ImageType.GMLL)
                {
                    newForegroundData = Tools.CheckConvertImageTo(info.image, Tools.ConvertType.GM);
                    if (newForegroundData == null)
                    {
                        info.image.Dispose();
                        return;
                    }
                }
                else
                {
                    newForegroundData = File.ReadAllBytes(fileName);
                }
                DebugLogger.Log("Main", "Got {0} bytes of non-GM GMLL data", newForegroundData.Length);
                if (DebugLogger.DoDebugActions())
                {
                    string nonGMDataFile = Globals.MakeDebugSaveName(false, "{0}nongm-fg.gmll", Path.GetFileNameWithoutExtension(fileName));
                    File.WriteAllBytes(nonGMDataFile, newForegroundData);
                }
                layerState.On(LayerStateManager.Layers.Foreground);
                ReplaceForegroundImage(newForegroundData, info.image);
            }
            else // (info.type == Images.ImageType.GM)
            {
                // This is a hack since CloseCurrentFile() resets everything
                // and disposes the images, but when we're reloading the foreground
                // only, we should preserve the background, so lets do this
                // rather than bool-ing it up just for this one case
                Image bgBackup = null;
                if (canvasBgImage != null)
                {
                    bgBackup = (Image)canvasBgImage.Clone();
                }
                if (!CloseCurrentFile())
                {
                    // didn't need it anyway
                    if (bgBackup != null)
                    {
                        bgBackup.Dispose();
                    }
                    info.image.Dispose();
                    return;
                }
                canvasBgImage = bgBackup;
                layerState.On(LayerStateManager.Layers.Foreground);
                byte[] gmllData = Images.LoadGMLLData(fileName);
                ReplaceForegroundImage(gmllData, info.image);
                GTMP.GMFile.GMFileInfo fileInf = info.gmInfo;
                DebugLogger.Log("Main", "Got {0} bytes of GM-GMLL data from {1}, file had {2} boxes", gmllData.Length, fileName, fileInf.Boxes == null ? -1 : fileInf.Boxes.Count);
                if (fileInf.Boxes != null)
                {
                    boxList.BeginUpdate();
                    try
                    {
                        allBoxes.Clear();
                        List<IBox> boxes = ConvertGMFileBoxes(fileInf.Boxes);
                        allBoxes.Load(boxes);
                    }
                    finally
                    {
                        boxList.EndUpdate();
                    }
                }
                metadataPropertyList.SelectedObject = fileInf.Metadata;
            }
            SetTitleFileName(fileName);
        }

        private void SaveChanges(string fileName)
        {
            GMProject.Save(
                fileName, 
                canvas.Image, 
                currentForegroundGMLLData, 
                new List<IBox>(allBoxes.Items), 
                (GTMP.GMFile.GMMetadata)metadataPropertyList.SelectedObject,
                Globals.App.GT2Version
            );
            ClearUnsavedChanges();
            SetTitleFileName(fileName);
        }

        private bool HasUnsavedChanges()
        {
            return unsavedChanges;
        }

        private void SetTitleFileName(string fileName)
        {
            string windowTitle = APP_TITLE;
            if (!String.IsNullOrEmpty(fileName))
            {
                windowTitle += " - " + fileName;
            }
            this.Text = windowTitle;
        }

        private void ClearUnsavedChanges()
        {
            unsavedChanges = false;
        }

        private void SetUnsavedChanges()
        {
            unsavedChanges = true;
            string windowTitle = this.Text;
            if (!windowTitle.EndsWith(" *"))
            {
                windowTitle += " *";
                this.Text = windowTitle;
            }
        }

        private Point dirtyTopLeft;
        private Point dirtyBottomRight;

        private void UpdateDirtyRect(Point pt)
        {
            int ptX = pt.X;
            int ptY = pt.Y;
            if (ptX < dirtyTopLeft.X)
            {
                dirtyTopLeft.X = ptX;
            }
            else if (ptX > dirtyBottomRight.X)
            {
                dirtyBottomRight.X = ptX;
            }
            if (ptY < dirtyTopLeft.Y)
            {
                dirtyTopLeft.Y = ptY;
            }
            else if (ptY > dirtyBottomRight.Y)
            {
                dirtyBottomRight.Y = ptY;
            }
        }

        private Rectangle GetDirtyRect()
        {
            Rectangle dirtyRect = new Rectangle(
                dirtyTopLeft, 
                new Size((dirtyBottomRight.X - dirtyTopLeft.X), (dirtyBottomRight.Y - dirtyTopLeft.Y))
            );
            // inflate the rect to cover the pen lines we've been drawing,
            // that are one pixel wide
            dirtyRect.Inflate(2, 2);
            return dirtyRect;
        }

        private void ClearDirtyRect()
        {
            dirtyBottomRight = Point.Empty;
            dirtyTopLeft = new Point(600, 600);
        }

        private void ResetToNormalCanvas()
        {
            lastSizingCursorMove = Point.Empty;
            mouseState = MouseState.Normal;
            HideCursorRectText();
            RedrawCanvas(GetDirtyRect());
            ClearDirtyRect();
        }

        private void CloneCurrentBox()
        {
            if (boxList.SelectedIndex != -1)
            {
                IBox origBox = (IBox)boxList.SelectedItem;
                IBox newBox = origBox.Clone();
                newBox.DisplayChanged += new BoxDisplayChange(BoxInvalidation);
                allBoxes.Add(newBox);
                SetUnsavedChanges();
            }
        }

        private void RemoveCurrentBox()
        {
            if (boxList.SelectedIndex != -1)
            {
                IBox origBox = (IBox)boxList.SelectedItem;
                allBoxes.Remove(origBox);
                SetUnsavedChanges();
                RedrawCanvas();
            }
        }

        private void DoHitTest(Point mouseLoc)
        {
            IBox hitBox = null;
            IBox.BoxHitTest hitRes = allBoxes.HitTest(mouseLoc, ref hitBox);
            Cursor toSet = Cursors.Default;
            switch(hitRes)
            {
                case IBox.BoxHitTest.AnchorPoint:
                {
                    toSet = Cursors.SizeAll;
                }
                break;
                case IBox.BoxHitTest.ResizePointTL:
                case IBox.BoxHitTest.ResizePointBR:
                {
                    toSet = Cursors.SizeNWSE;
                }
                break;
                case IBox.BoxHitTest.ResizePointTR:
                case IBox.BoxHitTest.ResizePointBL:
                {
                    toSet = Cursors.SizeNESW;
                }
                break;
            }
            currentMovingOrSizing = hitBox;
            hitType = hitRes;
            canvas.Cursor = toSet;
        }

        private void DragBox(Point newLoc)
        {
            Point startPoint = anchorClick;
            Point delta = new Point(newLoc.X - startPoint.X, newLoc.Y - startPoint.Y);
            currentMovingOrSizing.Move(delta);
            anchorClick = newLoc;
            if (delta != Point.Empty)
            {
                SetUnsavedChanges();
            }
        }

        private void ResizeBox(Point newLoc)
        {
            Point startPoint = anchorClick;
            Point delta = new Point(newLoc.X - startPoint.X, newLoc.Y - startPoint.Y);
            currentMovingOrSizing.Resize(hitType, delta);
            anchorClick = newLoc;
            if (delta != Point.Empty)
            {
                SetUnsavedChanges();
            }
        }

        private void ChangeSelectedBox(IBox b)
        {
            boxList.BeginUpdate();
            // when there are no items in the list. Then one is added
            // the SelectedIndex_Change isn't fired when that item is selected
            // this is a workaround.
            boxList.SelectedIndex = -1;
            boxList.SelectedItem = b;
            boxList.EndUpdate();
        }

        private void CommonProjectSave(string fileName)
        {
            string origFilename = fileName;
            if (String.IsNullOrEmpty(fileName))
            {
                fileName = GetSaveFileName(this, "Save Project file", "GMCreator Project Files|*.gmproj", null);
            }
            SaveChanges(fileName);
            ClearUnsavedChanges();
            if (fileName != origFilename)
            {
                SetTitleFileName(fileName);
                currentProjectFileName = fileName;
            }
        }

        public static string GetSaveFileName(IWin32Window parent, string title, string filter, string startDir)
        {
            string fileName = null;
            using(SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.AutoUpgradeEnabled = true;
                sfd.CheckPathExists = true;
                sfd.OverwritePrompt = true;
                sfd.ValidateNames = true;
                if (!String.IsNullOrEmpty(title))
                {
                    sfd.Title = title;
                }
                sfd.Filter = filter;
                sfd.FilterIndex = 0;
                if (!String.IsNullOrEmpty(startDir))
                {
                    sfd.InitialDirectory = startDir;
                }
                if (sfd.ShowDialog(parent) == DialogResult.OK)
                {
                    fileName = sfd.FileName;
                }
            }
            return fileName;
        }

        public static string GetOpenFileName(IWin32Window parent, string title, string filter, string startDir)
        {
            string fileName = null;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.AutoUpgradeEnabled = true;
                ofd.CheckPathExists = true;
                ofd.ValidateNames = true;
                ofd.CheckFileExists = true;
                if (!String.IsNullOrEmpty(title))
                {
                    ofd.Title = title;
                }
                ofd.Filter = filter;
                ofd.FilterIndex = 0;
                if (!String.IsNullOrEmpty(startDir))
                {
                    ofd.InitialDirectory = startDir;
                }
                if (ofd.ShowDialog(parent) == DialogResult.OK)
                {
                    fileName = ofd.FileName;
                }
            }
            return fileName;
        }

        public static string PickFolder(IWin32Window parent, string title, string startDir)
        {
            string folder = null;
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = title;
                fbd.ShowNewFolderButton = true;
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                if (!String.IsNullOrEmpty(startDir))
                {
                    fbd.SelectedPath = startDir;
                }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    folder = fbd.SelectedPath;
                }
            }
            return folder;
        }

        private void ProjectLoad()
        {
            string fileName = GetOpenFileName(this, "Open Project file", "GMCreator Project Files|*.gmproj", null);
            if (String.IsNullOrEmpty(fileName))
            {
                return;
            }
            Bitmap fg, bg;
            List<IBox> boxes;
            GTMP.GMFile.GMMetadata metadata;
            byte[] gmllData = null;
            if (!GMProject.Load(fileName, out bg, out fg, out gmllData, out boxes, out metadata))
            {
                return;
            }
            if (!CloseCurrentFile())
            {
                return;
            }
            if (fg != null)
            {
                ReplaceForegroundImage(gmllData, fg);
            }
            if(bg != null)
            {
                Images.ImageLoadResult imageInfo = new Images.ImageLoadResult(bg);
                BGImageLoad(null, imageInfo);
            }
            allBoxes.Load(boxes);
            metadataPropertyList.SelectedObject = metadata;
            ClearUnsavedChanges();
            SetTitleFileName(fileName);
            currentProjectFileName = fileName;
        }

        private bool CloseCurrentFile()
        {
            if (HasUnsavedChanges())
            {
                DialogResult res = DisplayMsgBox(
                    MessageBoxButtons.YesNoCancel, 
                    MessageBoxIcon.Question, 
                    "There are unsaved changes in the {0}file. Save before closing?",
                    String.IsNullOrEmpty(currentProjectFileName) ? String.Empty : (currentProjectFileName + ' ')
                );
                if(res == DialogResult.Yes)
                {
                    CommonProjectSave(currentProjectFileName);
                }
                else if (res == DialogResult.Cancel)
                {
                    return false;
                }
            }
            ResetEverything();
            return true;
        }

        private void ResetEverything()
        {
            allBoxes.Clear();
            Box.ResetIndexCount();
            Image fgImage = canvasFgImage, bgImage = canvasBgImage;
            Image canvasImage = canvas.Image;
            canvasFgImage = null;
            canvasBgImage = null;
            RecompositeCanvasImage();
            if (bgImage != null)
            {
                bgImage.Dispose();
            }
            if (fgImage != null)
            {
                fgImage.Dispose();
            }
            metadataPropertyList.SelectedObject = new GTMP.GMFile.GMMetadata();
            currentMovingOrSizing = null;
            currentProjectFileName = null;
            currentForegroundGMLLData = null;
            ResetToNormalCanvas();
            ClearUnsavedChanges();
            SetTitleFileName(null);
        }

        private void ExportToGMFile()
        {
            string outFileName = GetSaveFileName(this, "Save Foreground GM File", "GT2 GM File (*.gm)|*.gm|All Files (*.*)|*.*", null);
            if (String.IsNullOrEmpty(outFileName))
            {
                return;
            }
            GMProject.ExportGM(
                outFileName, 
                new List<IBox>(allBoxes.Items), 
                (GTMP.GMFile.GMMetadata)metadataPropertyList.SelectedObject,
                currentForegroundGMLLData
            );
        }

        // https://stackoverflow.com/a/439606
        private static Control FindFocusedControl(Control control)
        {
            var container = control as IContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as IContainerControl;
            }
            return control;
        }


#if DEBUG
        private void AddDebugMenu()
        {
            ToolStripMenuItem debugMenu = new ToolStripMenuItem("Debug");
            ToolStripMenuItem dumpCanvas = new ToolStripMenuItem("Dump Canvas");
            dumpCanvas.Click += new EventHandler(dumpCanvas_Click);
            debugMenu.DropDownItems.Add(dumpCanvas);
            ToolStripMenuItem clearChanged = new ToolStripMenuItem("Clear Change Status");
            clearChanged.Click += new EventHandler(clearChange_Click);
            debugMenu.DropDownItems.Add(clearChanged);
            ToolStripMenuItem checkGameFileValidity = new ToolStripMenuItem("Check GT2 GM File Validity");
            checkGameFileValidity.Click += new EventHandler(checkGameFileValidity_Click);
            debugMenu.DropDownItems.Add(checkGameFileValidity);
            ToolStripMenuItem recompressFiles = new ToolStripMenuItem("Recompress All GT2 GM Files");
            recompressFiles.Click += new EventHandler(recompressFiles_Click);
            debugMenu.DropDownItems.Add(recompressFiles);
            this.mainMenu.Items.Add(debugMenu);
        }

        class RecompressThreadArgs
        {
            public WaitDlg dlg;
            public string inDir;
            public string outDir;
        }

        void RecompressGMFiles(object o)
        {
            RecompressThreadArgs rta = (RecompressThreadArgs)o;
            string inDir = rta.inDir;
            string[] allFiles = Directory.GetFiles(inDir);
            int numFiles = allFiles.Length;
            WaitDlg dlg = rta.dlg;
            dlg.UpdateStatus("Recompressing {0} files", numFiles);
            string outDir = rta.outDir;
            for (int i = 0; i < numFiles; ++i)
            {
                Box.ResetIndexCount();
                string currentFile = allFiles[i];
                byte[] gmFileData;
                MemoryStream decompGM;
                using(FileStream fs = File.OpenRead(currentFile))
                {
                    decompGM = Compress.GZipDecompress(fs);
                    gmFileData = decompGM.ToArray();
                }
                GTMP.GMFile.GMFileInfo fi = GTMP.GMFile.Parse(decompGM);
                List<IBox> boxes = ConvertGMFileBoxes(fi.Boxes);

                try
                {
                    byte[] gmllData = Images.TrimToGMLLData(gmFileData);
                    string newFileName = Path.Combine(outDir, Path.GetFileName(currentFile));
                    GMProject.ExportGM(newFileName, boxes, fi.Metadata, gmllData);
                }
                catch (InvalidBoxStateException ibse)
                {
                    DebugLogger.Log("Debug", "Exception was from {0}", currentFile);
                    dlg.UpdateStatus("Caught invalid box exception {0} for {1} in file {2}", ibse.Message, ibse.InvalidBox.Name, currentFile);
                }

                if ((i != 0) && ((i % 10) == 0))
                {
                    dlg.UpdateStatus(String.Format("Tested {0} files", i));
                }
            }
            dlg.AllowClose("All converted");
        }

        private void recompressFiles_Click(object sender, EventArgs e)
        {
            string inDir = @"C:\Users\Adrian\Downloads\Fantavision (Japan)\T";
            string outDir = @"C:\Users\Adrian\Downloads\Fantavision (Japan)\recompressedGMs";
            using (WaitDlg dlg = new WaitDlg("Checking GM Files"))
            {
                RecompressThreadArgs rta = new RecompressThreadArgs();
                rta.dlg = dlg;
                rta.inDir = inDir;
                rta.outDir = outDir;
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(RecompressGMFiles), rta);
                dlg.ShowDialog(this);
            }
        }

        private void clearChange_Click(object sender, EventArgs e)
        {
            ClearUnsavedChanges();
            SetTitleFileName(null);
        }

        class ValidityThreadArgs
        {
            public WaitDlg dlg;
            public string dir;
        }

        void CheckGMFileValidity(object o)
        {
            ValidityThreadArgs vta = (ValidityThreadArgs)o;
            string dir = vta.dir;
            string[] allFiles = Directory.GetFiles(dir);
            int numFiles = allFiles.Length;
            WaitDlg dlg = vta.dlg;
            dlg.UpdateStatus("Checking {0} files", numFiles);
            int exceptions = 0;
            for (int i = 0; i < numFiles; ++i)
            {
                Box.ResetIndexCount();
                string currentFile = allFiles[i];
                GTMP.GMFile.GMFileInfo fi = GTMP.GMFile.Parse(currentFile);
                List<IBox> boxes = ConvertGMFileBoxes(fi.Boxes);

                try
                {
                    MemoryStream ms = new MemoryStream(boxes.Count * 0x4c);
                    using(BinaryWriter bw = new BinaryWriter(ms))
                    {
                        foreach(IBox b in boxes)
                        {
                            b.Serialize(bw);
                        }
                    }
                }
                catch(InvalidBoxStateException ibse)
                {
                    if (++exceptions == 50)
                    {
                        break;
                    }
                    DebugLogger.Log("Debug", "Exception was from {0}", currentFile);
                    dlg.UpdateStatus("Caught invalid box exception {0} for {1} in file {2}", ibse.Message, ibse.InvalidBox.Name, currentFile);
                }

                if ((i != 0) && ((i % 10) == 0))
                {
                    dlg.UpdateStatus(String.Format("Tested {0} files", i));
                }
            }
            dlg.AllowClose("All checked");
        }

        void checkGameFileValidity_Click(object sender, EventArgs e)
        {
            //string dir = PickFolder(this, "Open dir with GM files");
            string dir = @"T:\gt2\gtmenu\pics\GTMenuDatDecomp";
            if (!String.IsNullOrEmpty(dir))
            {
                using (WaitDlg dlg = new WaitDlg("Checking GM Files"))
                {
                    ValidityThreadArgs vta = new ValidityThreadArgs();
                    vta.dlg = dlg;
                    vta.dir = dir;
                    System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(CheckGMFileValidity), vta);
                    dlg.ShowDialog(this);
                }
            }
        }
#endif
    }
}
