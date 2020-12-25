using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace GMCreator
{
    class IconImgBox : IBox
    {
        private Point screenTopLeft;
        private Rectangle centralAnchor;
        // Top left, Top right, Bottom left, Bottom right
        private Rectangle[] cornerAnchors;
        private IBoxContentRenderer renderer;
        [Newtonsoft.Json.JsonProperty]
        private int hardcodedImgDataIndex;
        private string name;

        // Deserialising from JSon requires a constructor it can use
        // it can't pass the proper arguments to the others so it uses this one
        private IconImgBox()
        {
            // deserialising calls the location property, which needs these to exist
            cornerAnchors = new Rectangle[4];
        }

        public IconImgBox(string initialName, int hardcodedIndex)
            : this(Hardcoded.IconImages.Get(hardcodedIndex))
        {
            name = initialName;
        }

        public IconImgBox(IconImgEntry imgEntry)
        {
            screenTopLeft = Point.Empty;
            name = imgEntry.Name;
            hardcodedImgDataIndex = imgEntry.Id;
            Size imgSize = imgEntry.ImageSize;
            cornerAnchors = new Rectangle[4];
            UpdateAnchorPositions(Bounds);
            DoAfterConstruction();
        }

        private void DoAfterConstruction()
        {
            Globals.App.SettingsChanged += new EventHandler(App_SettingsChanged);
            renderer = new TopLeftIconImageRenderer(hardcodedImgDataIndex);
        }

        [OnDeserialized]
        private void AfterDeserialisation(StreamingContext sc)
        {
            DoAfterConstruction();
        }

        [Category("B - Non-Game")]
        [Description("Friendly name of this box")]
        public override string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                {
                    name = value;
                    RaiseDescriptionChanged();
                }
            }
        }

        [Browsable(false)]
        internal Rectangle Bounds
        {
            get
            {
                IconImgEntry img = Hardcoded.IconImages.Get(hardcodedImgDataIndex);
                return new Rectangle(screenTopLeft, img.ImageSize);
            }
            private set
            {
                ;
            }
        }

        [Category("A - Required")]
        [Description("The position of the top left of this icon image")]
        public Point Location
        {
            get
            {
                return screenTopLeft;
            }
            set
            {
                Rectangle preLoc = Bounds;
                Point p = value;
                if (
                    (p.X < 0) || 
                    (p.Y < 0) ||
                    ((p.X + preLoc.Width) > MainForm.CANVAS_WIDTH) ||
                    ((p.X + preLoc.Height) > MainForm.CANVAS_HEIGHT)
                )
                {
                    return;
                }
                screenTopLeft = p;
                Rectangle postLoc = Bounds;
                if (preLoc != postLoc)
                {
                    UpdateAnchorPositions(postLoc);
                    RaiseDisplayChanged(preLoc, postLoc);
                    RaiseDescriptionChanged();
                }
            }
        }

        private void UpdateAnchorPositions(Rectangle newBounds)
        {
            int moveAnchorW = Globals.App.CentralAnchorWidth;
            int moveAnchorH = Globals.App.CentralAnchorHeight;
            int locWidth = newBounds.Width;
            int locHeight = newBounds.Height;
            int locLeft = newBounds.Left;
            int locTop = newBounds.Top;
            centralAnchor = new Rectangle(
                locLeft + ((locWidth - moveAnchorW) / 2),
                locTop + ((locHeight - moveAnchorH) / 2),
                moveAnchorW,
                moveAnchorH
            );
            int resizeAnchorW = Globals.App.ResizeAnchorWidth;
            int resizeAnchorH = Globals.App.ResizeAnchorHeight;
            int halfAnchorWidth = resizeAnchorW / 2;
            int halfAnchorHeight = resizeAnchorH / 2;
            cornerAnchors[0] = new Rectangle(locLeft - halfAnchorWidth, locTop - halfAnchorHeight, resizeAnchorW, resizeAnchorH);
            cornerAnchors[1] = new Rectangle((locLeft + locWidth) - halfAnchorWidth, locTop - halfAnchorHeight, resizeAnchorW, resizeAnchorH);
            cornerAnchors[2] = new Rectangle(locLeft - halfAnchorWidth, (locTop + locHeight) - halfAnchorHeight, resizeAnchorW, resizeAnchorH);
            cornerAnchors[3] = new Rectangle((locLeft + locWidth) - halfAnchorWidth, (locTop + locHeight) - halfAnchorHeight, resizeAnchorW, resizeAnchorH);
        }

        public override void Serialize(BinaryWriter stream)
        {
            IconImgEntry img = Hardcoded.IconImages.Get(hardcodedImgDataIndex);
            stream.Write((ushort)screenTopLeft.X);
            stream.Write((ushort)screenTopLeft.Y);
            stream.Write((byte)img.ImageLocation.X);
            stream.Write((byte)img.ImageLocation.Y);
            stream.Write((byte)img.ImageSize.Width);
            stream.Write((byte)img.ImageSize.Height);
            stream.Write(img.Unk3);
            stream.Write(img.Mod1);
        }

        public override void Draw(Graphics g, Rectangle r, bool drawOutline)
        {
            Rectangle bounds = Bounds;
            if (bounds.IntersectsWith(r))
            {
                renderer.Render(g, bounds);
                Brush anchor = Globals.App.AnchorBrush;
                g.FillRectangle(anchor, centralAnchor);
#if RESIZABLE_ICONIMGS
                g.FillRectangles(anchor, cornerAnchors);
#endif
            }
        }

        public override IBox Clone()
        {
            IconImgEntry img = Hardcoded.IconImages.Get(hardcodedImgDataIndex);
            IconImgBox box = new IconImgBox(img.Name, hardcodedImgDataIndex);
            Point loc = box.Location;
            loc.Offset(10, 10);
            box.Location = loc;
            return box;
        }

        public override BoxHitTest HitTest(Point p)
        {
            BoxHitTest hitLocation = BoxHitTest.None;
            if (centralAnchor.Contains(p))
            {
                hitLocation = BoxHitTest.AnchorPoint;
            }
#if RESIZABLE_ICONIMGS
            else if(cornerAnchors[0].Contains(p))
            {
                hitLocation = BoxHitTest.ResizePointTL;
            }
            else if (cornerAnchors[1].Contains(p))
            {
                hitLocation = BoxHitTest.ResizePointTR;
            }
            else if (cornerAnchors[2].Contains(p))
            {
                hitLocation = BoxHitTest.ResizePointBL;
            }
            else if(cornerAnchors[3].Contains(p))
            {
                hitLocation = BoxHitTest.ResizePointBR;
            }
#endif
            return hitLocation;
        }

        public override void Move(Point delta)
        {
            Rectangle origBounds = Bounds;
            Point tl = Location;
            tl.Offset(delta);
            Location = tl;
        }

        public override void Resize(BoxHitTest corner, Point delta)
        {
#if RESIZABLE_ICONIMGS
            throw new NotImplementedException("No support for resizing IconImgs.");
#endif
        }

        public override string ToString()
        {
            return String.Format("IconImg: {0}, {1}", Name, Location.ToString());
        }

        private void App_SettingsChanged(object sender, EventArgs e)
        {
            Rectangle curBounds = Bounds;
            UpdateAnchorPositions(curBounds);
            RaiseDisplayChanged(curBounds, curBounds);
        }

        public override void Select(bool state)
        {
            // intentionally doesn't do anything
        }
    }
}
