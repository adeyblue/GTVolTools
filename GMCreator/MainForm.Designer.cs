namespace GMCreator
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
            this.components = new System.ComponentModel.Container();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.canvasStatus = new System.Windows.Forms.StatusStrip();
            this.mouseText = new System.Windows.Forms.ToolStripStatusLabel();
            this.mouseXYText = new System.Windows.Forms.ToolStripStatusLabel();
            this.cursorRectText = new System.Windows.Forms.ToolStripStatusLabel();
            this.canvasFocusTextBox = new System.Windows.Forms.TextBox();
            this.propertyItemContainer = new System.Windows.Forms.SplitContainer();
            this.boxList = new System.Windows.Forms.ListBox();
            this.boxListRightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.boxCopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.boxDeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertyGridSplitContainer = new System.Windows.Forms.SplitContainer();
            this.boxPropertyList = new System.Windows.Forms.PropertyGrid();
            this.metadataPropertyList = new System.Windows.Forms.PropertyGrid();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.loadGMProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGMProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGMProjectAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.openBackgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openForegroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exportGMFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.boxesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.layersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggleBackgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggleForegroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggleBoxesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggleBoxContentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.convertImageToBackgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitCommonpicdatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitGTMenuDatdatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.makeCommonPicdatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.makeGTMenuDatdatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.canvasStatus.SuspendLayout();
            this.propertyItemContainer.Panel1.SuspendLayout();
            this.propertyItemContainer.Panel2.SuspendLayout();
            this.propertyItemContainer.SuspendLayout();
            this.boxListRightClickMenu.SuspendLayout();
            this.propertyGridSplitContainer.Panel1.SuspendLayout();
            this.propertyGridSplitContainer.Panel2.SuspendLayout();
            this.propertyGridSplitContainer.SuspendLayout();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer.IsSplitterFixed = true;
            this.splitContainer.Location = new System.Drawing.Point(0, 24);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.canvas);
            this.splitContainer.Panel1.Controls.Add(this.canvasStatus);
            this.splitContainer.Panel1.Controls.Add(this.canvasFocusTextBox);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.propertyItemContainer);
            this.splitContainer.Size = new System.Drawing.Size(771, 525);
            this.splitContainer.SplitterDistance = 512;
            this.splitContainer.TabIndex = 0;
            // 
            // canvas
            // 
            this.canvas.BackColor = System.Drawing.Color.Black;
            this.canvas.Location = new System.Drawing.Point(0, 0);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(512, 504);
            this.canvas.TabIndex = 1;
            this.canvas.TabStop = false;
            this.canvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseMove);
            this.canvas.DragDrop += new System.Windows.Forms.DragEventHandler(this.canvas_DragDrop);
            this.canvas.MouseClick += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseClick);
            this.canvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseDown);
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.canvas_Paint);
            this.canvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseUp);
            this.canvas.DragEnter += new System.Windows.Forms.DragEventHandler(this.canvas_DragEnter);
            // 
            // canvasStatus
            // 
            this.canvasStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mouseText,
            this.mouseXYText,
            this.cursorRectText});
            this.canvasStatus.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.canvasStatus.Location = new System.Drawing.Point(0, 503);
            this.canvasStatus.Name = "canvasStatus";
            this.canvasStatus.Size = new System.Drawing.Size(512, 22);
            this.canvasStatus.SizingGrip = false;
            this.canvasStatus.TabIndex = 0;
            this.canvasStatus.Text = "statusStrip1";
            // 
            // mouseText
            // 
            this.mouseText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.mouseText.Name = "mouseText";
            this.mouseText.Size = new System.Drawing.Size(46, 17);
            this.mouseText.Text = "Mouse:";
            // 
            // mouseXYText
            // 
            this.mouseXYText.Name = "mouseXYText";
            this.mouseXYText.Size = new System.Drawing.Size(24, 17);
            this.mouseXYText.Text = "0x0";
            // 
            // cursorRectText
            // 
            this.cursorRectText.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.cursorRectText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cursorRectText.Name = "cursorRectText";
            this.cursorRectText.Size = new System.Drawing.Size(37, 19);
            this.cursorRectText.Text = "Rect:";
            this.cursorRectText.Visible = false;
            // 
            // canvasFocusTextBox
            // 
            this.canvasFocusTextBox.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.canvasFocusTextBox.Location = new System.Drawing.Point(118, 160);
            this.canvasFocusTextBox.MaxLength = 100;
            this.canvasFocusTextBox.Name = "canvasFocusTextBox";
            this.canvasFocusTextBox.ReadOnly = true;
            this.canvasFocusTextBox.Size = new System.Drawing.Size(53, 20);
            this.canvasFocusTextBox.TabIndex = 2;
            this.canvasFocusTextBox.TabStop = false;
            this.canvasFocusTextBox.Text = "Hi, this is a hack to get keyboard control working in the picturebox which normal" +
                "ly doesn\'t support it. Please use the cursor keys to move the selected box";
            this.canvasFocusTextBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.canvasFocusTextBox_PreviewKeyDown);
            // 
            // propertyItemContainer
            // 
            this.propertyItemContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyItemContainer.Location = new System.Drawing.Point(0, 0);
            this.propertyItemContainer.Name = "propertyItemContainer";
            this.propertyItemContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // propertyItemContainer.Panel1
            // 
            this.propertyItemContainer.Panel1.Controls.Add(this.boxList);
            // 
            // propertyItemContainer.Panel2
            // 
            this.propertyItemContainer.Panel2.Controls.Add(this.propertyGridSplitContainer);
            this.propertyItemContainer.Size = new System.Drawing.Size(255, 525);
            this.propertyItemContainer.SplitterDistance = 107;
            this.propertyItemContainer.TabIndex = 0;
            // 
            // boxList
            // 
            this.boxList.ContextMenuStrip = this.boxListRightClickMenu;
            this.boxList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.boxList.HorizontalScrollbar = true;
            this.boxList.Location = new System.Drawing.Point(0, 0);
            this.boxList.Name = "boxList";
            this.boxList.Size = new System.Drawing.Size(255, 95);
            this.boxList.TabIndex = 1;
            this.boxList.SelectedIndexChanged += new System.EventHandler(this.boxList_SelectedIndexChanged);
            this.boxList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.boxList_KeyUp);
            // 
            // boxListRightClickMenu
            // 
            this.boxListRightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.boxCopyMenuItem,
            this.boxDeleteMenuItem});
            this.boxListRightClickMenu.Name = "boxListRightClickMenu";
            this.boxListRightClickMenu.ShowImageMargin = false;
            this.boxListRightClickMenu.Size = new System.Drawing.Size(120, 48);
            // 
            // boxCopyMenuItem
            // 
            this.boxCopyMenuItem.Name = "boxCopyMenuItem";
            this.boxCopyMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
            this.boxCopyMenuItem.Size = new System.Drawing.Size(119, 22);
            this.boxCopyMenuItem.Text = "&Copy";
            this.boxCopyMenuItem.Click += new System.EventHandler(this.boxCopyMenuItem_Click);
            // 
            // boxDeleteMenuItem
            // 
            this.boxDeleteMenuItem.Name = "boxDeleteMenuItem";
            this.boxDeleteMenuItem.ShortcutKeyDisplayString = "Del";
            this.boxDeleteMenuItem.Size = new System.Drawing.Size(119, 22);
            this.boxDeleteMenuItem.Text = "Delete";
            this.boxDeleteMenuItem.Click += new System.EventHandler(this.boxDeleteMenuItem_Click);
            // 
            // propertyGridSplitContainer
            // 
            this.propertyGridSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.propertyGridSplitContainer.Name = "propertyGridSplitContainer";
            this.propertyGridSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // propertyGridSplitContainer.Panel1
            // 
            this.propertyGridSplitContainer.Panel1.Controls.Add(this.boxPropertyList);
            // 
            // propertyGridSplitContainer.Panel2
            // 
            this.propertyGridSplitContainer.Panel2.Controls.Add(this.metadataPropertyList);
            this.propertyGridSplitContainer.Size = new System.Drawing.Size(255, 414);
            this.propertyGridSplitContainer.SplitterDistance = 251;
            this.propertyGridSplitContainer.TabIndex = 1;
            // 
            // boxPropertyList
            // 
            this.boxPropertyList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.boxPropertyList.Location = new System.Drawing.Point(0, 0);
            this.boxPropertyList.Name = "boxPropertyList";
            this.boxPropertyList.Size = new System.Drawing.Size(255, 251);
            this.boxPropertyList.TabIndex = 2;
            this.boxPropertyList.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.PropertyList_PropertyValueChanged);
            // 
            // metadataPropertyList
            // 
            this.metadataPropertyList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.metadataPropertyList.Location = new System.Drawing.Point(0, 0);
            this.metadataPropertyList.Name = "metadataPropertyList";
            this.metadataPropertyList.Size = new System.Drawing.Size(255, 159);
            this.metadataPropertyList.TabIndex = 3;
            this.metadataPropertyList.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.PropertyList_PropertyValueChanged);
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.insertToolStripMenuItem,
            this.boxesToolStripMenuItem,
            this.layersToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(771, 24);
            this.mainMenu.TabIndex = 1;
            this.mainMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.toolStripSeparator4,
            this.loadGMProjectToolStripMenuItem,
            this.saveGMProjectToolStripMenuItem,
            this.saveGMProjectAsToolStripMenuItem,
            this.toolStripSeparator3,
            this.openBackgroundToolStripMenuItem,
            this.openForegroundToolStripMenuItem,
            this.toolStripSeparator1,
            this.exportGMFileToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeyDisplayString = "";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(241, 6);
            // 
            // loadGMProjectToolStripMenuItem
            // 
            this.loadGMProjectToolStripMenuItem.Name = "loadGMProjectToolStripMenuItem";
            this.loadGMProjectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.loadGMProjectToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.loadGMProjectToolStripMenuItem.Text = "&Load GMCreator Project";
            this.loadGMProjectToolStripMenuItem.Click += new System.EventHandler(this.loadGMProjectToolStripMenuItem_Click);
            // 
            // saveGMProjectToolStripMenuItem
            // 
            this.saveGMProjectToolStripMenuItem.Name = "saveGMProjectToolStripMenuItem";
            this.saveGMProjectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveGMProjectToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.saveGMProjectToolStripMenuItem.Text = "&Save GMCreator Project";
            this.saveGMProjectToolStripMenuItem.Click += new System.EventHandler(this.saveGMProjectToolStripMenuItem_Click);
            // 
            // saveGMProjectAsToolStripMenuItem
            // 
            this.saveGMProjectAsToolStripMenuItem.Name = "saveGMProjectAsToolStripMenuItem";
            this.saveGMProjectAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.S)));
            this.saveGMProjectAsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.saveGMProjectAsToolStripMenuItem.Text = "Save Project &As...";
            this.saveGMProjectAsToolStripMenuItem.Click += new System.EventHandler(this.saveGMProjectAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(241, 6);
            // 
            // openBackgroundToolStripMenuItem
            // 
            this.openBackgroundToolStripMenuItem.Name = "openBackgroundToolStripMenuItem";
            this.openBackgroundToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
            this.openBackgroundToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.openBackgroundToolStripMenuItem.Text = "Load &Background Image";
            this.openBackgroundToolStripMenuItem.Click += new System.EventHandler(this.openBackgroundToolStripMenuItem_Click);
            // 
            // openForegroundToolStripMenuItem
            // 
            this.openForegroundToolStripMenuItem.Name = "openForegroundToolStripMenuItem";
            this.openForegroundToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.openForegroundToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.openForegroundToolStripMenuItem.Text = "Load &Foreground Image";
            this.openForegroundToolStripMenuItem.Click += new System.EventHandler(this.openForegroundToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(241, 6);
            // 
            // exportGMFileToolStripMenuItem
            // 
            this.exportGMFileToolStripMenuItem.Name = "exportGMFileToolStripMenuItem";
            this.exportGMFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
            this.exportGMFileToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.exportGMFileToolStripMenuItem.Text = "Export &GM File";
            this.exportGMFileToolStripMenuItem.Click += new System.EventHandler(this.exportGMFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(241, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeyDisplayString = "";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this.exitToolStripMenuItem.Text = "&Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // insertToolStripMenuItem
            // 
            this.insertToolStripMenuItem.Name = "insertToolStripMenuItem";
            this.insertToolStripMenuItem.Size = new System.Drawing.Size(74, 20);
            this.insertToolStripMenuItem.Text = "&Insert Icon";
            // 
            // boxesToolStripMenuItem
            // 
            this.boxesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearAllToolStripMenuItem});
            this.boxesToolStripMenuItem.Name = "boxesToolStripMenuItem";
            this.boxesToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.boxesToolStripMenuItem.Text = "&Boxes";
            // 
            // clearAllToolStripMenuItem
            // 
            this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
            this.clearAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.X)));
            this.clearAllToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.clearAllToolStripMenuItem.Text = "&Clear All";
            this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.clearAllToolStripMenuItem_Click);
            // 
            // layersToolStripMenuItem
            // 
            this.layersToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toggleBackgroundToolStripMenuItem,
            this.toggleForegroundToolStripMenuItem,
            this.toggleBoxesToolStripMenuItem,
            this.toggleBoxContentsToolStripMenuItem});
            this.layersToolStripMenuItem.Name = "layersToolStripMenuItem";
            this.layersToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.layersToolStripMenuItem.Text = "Layers";
            // 
            // toggleBackgroundToolStripMenuItem
            // 
            this.toggleBackgroundToolStripMenuItem.Checked = true;
            this.toggleBackgroundToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toggleBackgroundToolStripMenuItem.Name = "toggleBackgroundToolStripMenuItem";
            this.toggleBackgroundToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.toggleBackgroundToolStripMenuItem.Text = "Show Background";
            this.toggleBackgroundToolStripMenuItem.Click += new System.EventHandler(this.toggleLayerStateMenuClick);
            // 
            // toggleForegroundToolStripMenuItem
            // 
            this.toggleForegroundToolStripMenuItem.Checked = true;
            this.toggleForegroundToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toggleForegroundToolStripMenuItem.Name = "toggleForegroundToolStripMenuItem";
            this.toggleForegroundToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.toggleForegroundToolStripMenuItem.Text = "Show Foreground";
            this.toggleForegroundToolStripMenuItem.Click += new System.EventHandler(this.toggleLayerStateMenuClick);
            // 
            // toggleBoxesToolStripMenuItem
            // 
            this.toggleBoxesToolStripMenuItem.Checked = true;
            this.toggleBoxesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toggleBoxesToolStripMenuItem.Name = "toggleBoxesToolStripMenuItem";
            this.toggleBoxesToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.toggleBoxesToolStripMenuItem.Text = "Show Boxes";
            this.toggleBoxesToolStripMenuItem.Click += new System.EventHandler(this.toggleLayerStateMenuClick);
            // 
            // toggleBoxContentsToolStripMenuItem
            // 
            this.toggleBoxContentsToolStripMenuItem.Checked = true;
            this.toggleBoxContentsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toggleBoxContentsToolStripMenuItem.Name = "toggleBoxContentsToolStripMenuItem";
            this.toggleBoxContentsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.toggleBoxContentsToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.toggleBoxContentsToolStripMenuItem.Text = "Show Box Contents";
            this.toggleBoxContentsToolStripMenuItem.Click += new System.EventHandler(this.toggleBoxContentsToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.convertImageToBackgroundToolStripMenuItem,
            this.splitCommonpicdatToolStripMenuItem,
            this.splitGTMenuDatdatToolStripMenuItem,
            this.makeCommonPicdatToolStripMenuItem,
            this.makeGTMenuDatdatToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // convertImageToBackgroundToolStripMenuItem
            // 
            this.convertImageToBackgroundToolStripMenuItem.Name = "convertImageToBackgroundToolStripMenuItem";
            this.convertImageToBackgroundToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.convertImageToBackgroundToolStripMenuItem.Text = "&Convert Image to Background";
            this.convertImageToBackgroundToolStripMenuItem.Click += new System.EventHandler(this.convertImageToBackgroundToolStripMenuItem_Click);
            // 
            // splitCommonpicdatToolStripMenuItem
            // 
            this.splitCommonpicdatToolStripMenuItem.Name = "splitCommonpicdatToolStripMenuItem";
            this.splitCommonpicdatToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.splitCommonpicdatToolStripMenuItem.Text = "Split &CommonPic.dat";
            this.splitCommonpicdatToolStripMenuItem.Click += new System.EventHandler(this.splitCommonpicdatToolStripMenuItem_Click);
            // 
            // splitGTMenuDatdatToolStripMenuItem
            // 
            this.splitGTMenuDatdatToolStripMenuItem.Name = "splitGTMenuDatdatToolStripMenuItem";
            this.splitGTMenuDatdatToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.splitGTMenuDatdatToolStripMenuItem.Text = "&Split GTMenuDat.dat";
            this.splitGTMenuDatdatToolStripMenuItem.Click += new System.EventHandler(this.splitGTMenuDatdatToolStripMenuItem_Click);
            // 
            // makeCommonPicdatToolStripMenuItem
            // 
            this.makeCommonPicdatToolStripMenuItem.Name = "makeCommonPicdatToolStripMenuItem";
            this.makeCommonPicdatToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.makeCommonPicdatToolStripMenuItem.Text = "&Make CommonPic.dat";
            this.makeCommonPicdatToolStripMenuItem.Click += new System.EventHandler(this.makeCommonPicdatToolStripMenuItem_Click);
            // 
            // makeGTMenuDatdatToolStripMenuItem
            // 
            this.makeGTMenuDatdatToolStripMenuItem.Name = "makeGTMenuDatdatToolStripMenuItem";
            this.makeGTMenuDatdatToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.makeGTMenuDatdatToolStripMenuItem.Text = "Make &GTMenuDat.dat";
            this.makeGTMenuDatdatToolStripMenuItem.Click += new System.EventHandler(this.makeGTMenuDatdatToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(771, 549);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.mainMenu);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.Text = "GT2 GMCreator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.canvasStatus.ResumeLayout(false);
            this.canvasStatus.PerformLayout();
            this.propertyItemContainer.Panel1.ResumeLayout(false);
            this.propertyItemContainer.Panel2.ResumeLayout(false);
            this.propertyItemContainer.ResumeLayout(false);
            this.boxListRightClickMenu.ResumeLayout(false);
            this.propertyGridSplitContainer.Panel1.ResumeLayout(false);
            this.propertyGridSplitContainer.Panel2.ResumeLayout(false);
            this.propertyGridSplitContainer.ResumeLayout(false);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.StatusStrip canvasStatus;
        private System.Windows.Forms.ToolStripStatusLabel mouseText;
        private System.Windows.Forms.ToolStripStatusLabel cursorRectText;
        private System.Windows.Forms.ToolStripStatusLabel mouseXYText;
        private System.Windows.Forms.PictureBox canvas;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.SplitContainer propertyItemContainer;
        private System.Windows.Forms.PropertyGrid boxPropertyList;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openBackgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openForegroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadGMProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveGMProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem boxesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveGMProjectAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ListBox boxList;
        private System.Windows.Forms.ContextMenuStrip boxListRightClickMenu;
        private System.Windows.Forms.ToolStripMenuItem boxCopyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem insertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem boxDeleteMenuItem;
        private System.Windows.Forms.SplitContainer propertyGridSplitContainer;
        private System.Windows.Forms.PropertyGrid metadataPropertyList;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem convertImageToBackgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem splitCommonpicdatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem makeCommonPicdatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem splitGTMenuDatdatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem makeGTMenuDatdatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem exportGMFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.TextBox canvasFocusTextBox;
        private System.Windows.Forms.ToolStripMenuItem layersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleForegroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleBackgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleBoxesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggleBoxContentsToolStripMenuItem;

    }
}

