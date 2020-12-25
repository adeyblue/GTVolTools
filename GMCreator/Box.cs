using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Serialization;
using System.Threading;
using System.Text;

namespace GMCreator
{
    class Box : IBox
    {
        private Rectangle location;
        private Rectangle hardcodedRenderLoc;
        private Rectangle centralAnchor;
        private Rectangle[] cornerAnchors;
        private IBoxContentRenderer renderer;
        private GTMP.GMFile.BoxItem content;
        private GTMP.GMFile.InfoBoxAttributes infoAttrs;
        private GTMP.GMFile.QueryAttributes queryAttrs;
        private sbyte prizeMoneyPosition;
        private string name;
        private Color outline;
        private bool innerContent;
        private bool isSelected;
        private bool customOutlineColour;

        private static int INDEX = 0;

        // Deserialising from JSon requires a constructor it can use
        // it can't pass the proper arguments to the others so it uses this one
        private Box()
        {
            // deserialising calls the location property, which needs these to exist
            cornerAnchors = new Rectangle[4];
        }

        public Box(Rectangle loc)
        {
            location = loc;
            ShowInnerContent = true;
            isSelected = false;
            customOutlineColour = false;
            hardcodedRenderLoc = Rectangle.Empty;
            outline = Globals.App.DefaultOutlineColour;
            content = GTMP.GMFile.BoxItem.None;
            infoAttrs = GTMP.GMFile.InfoBoxAttributes.None;
            queryAttrs = GTMP.GMFile.QueryAttributes.None;
            PrizeMoneyPosition = -1;
            LinkToScreen = -1;
            ArrowEnabler = GTMP.GMFile.ShowArrowIfLicense.None;
            name = String.Format("Box{0}", Interlocked.Increment(ref INDEX));
            cornerAnchors = new Rectangle[4];
            UpdateAnchorPositions(loc);
            DoAfterConstruction();
        }

        private void DoAfterConstruction()
        {
            RefreshRenderer();
            Globals.App.SettingsChanged += new EventHandler(App_SettingsChanged);
        }

        [OnDeserialized]
        private void AfterDeserialisation(StreamingContext sc)
        {
            DoAfterConstruction();
        }

        [Category("A - Required")]
        [Description("Where on screen this box should be")]
        public Rectangle Location
        {
            get
            {
                return location;
            }
            set
            {
                Rectangle preMove = Location;
                Rectangle toSet = value;
                Size setSize = value.Size;
                Point topLeft = value.Location;
                if (
                    (toSet.Bottom > MainForm.CANVAS_HEIGHT) || 
                    (toSet.Right > MainForm.CANVAS_WIDTH) ||
                    (topLeft.X < 0) ||
                    (topLeft.Y < 0)
                )
                {
                    return;
                }
                int diff = toSet.Bottom - MainForm.CANVAS_HEIGHT;
                if (diff > 0)
                {
                    setSize.Height -= diff;
                }
                diff = toSet.Right - MainForm.CANVAS_WIDTH;
                if (diff > 0)
                {
                    setSize.Width -= diff;
                }
                toSet.Location = topLeft;
                toSet.Size = setSize;
                location = toSet;
                if (preMove != location)
                {
                    // the bounding boxes to erase must include the corner anchors
                    // since they jut out and won't be deleted if we just erase the drawn bounds
                    Rectangle preMoveBounding = Rectangle.Union(cornerAnchors[0], cornerAnchors[3]);
                    UpdateAnchorPositions(toSet);
                    Rectangle postMoveBounding = Rectangle.Union(cornerAnchors[0], cornerAnchors[3]);
                    if ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.UseBigFont) == 0)
                    {
                        // definitely a hack. Any text in the small font that is bottom aligned can extend below the box
                        // if it has any character with a underhang like g or a comma. This ensures that it's is both
                        // erased and painted correctly
                        preMoveBounding.Height += 5;
                        postMoveBounding.Height += 5;
                    }
                    RaiseDisplayChanged(preMoveBounding, postMoveBounding);
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

        [Category("C - Non-Game")]
        [Description("Colour to draw the outline of the box")]
        public Color OutlineColour
        {
            get { return outline; }
            set
            {
                if (outline != value)
                {
                    customOutlineColour = true;
                    outline = value;
                    RefreshRenderer();
                }
            }
        }

        [Category("A - Required")]
        [Description("What this box does or displays")]
        public GTMP.GMFile.BoxItem Contents
        {
            get
            {
                return content;
            }
            set
            {
                if (content != value)
                {
                    content = value;
                    RefreshRenderer();
                    RaiseDescriptionChanged();
                }
            }
        }

        [Category("B - Optional")]
        [Description("Modifies or adds to what this box contains or does")]
        [Editor(typeof(FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public GTMP.GMFile.InfoBoxAttributes BehaviourAttributes
        {
            get
            {
                return infoAttrs;
            }
            set
            {
                if (infoAttrs != value)
                {
                    infoAttrs = value;
                    RefreshRenderer();
                }
            }
        }

        [Category("B - Optional")]
        [Description("If BehaviourAttributes includes RaceMode, the race info to render")]
        [Editor(typeof(FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public GTMP.GMFile.QueryAttributes QueryAttributes
        {
            get
            {
                return queryAttrs;
            }
            set
            {
                queryAttrs = value;
                RefreshRenderer();
            }
        }

        [Category("C - Non-Game")]
        [Description("Whether to render the contents of the box on the GMCreator preview window")]
        public bool ShowInnerContent
        {
            get
            {
                return innerContent;
            }
            set
            {
                innerContent = value;
                Rectangle loc = Location;
                RaiseDisplayChanged(loc, loc);
            }
        }

        [Category("C - Non-Game")]
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

        [Category("B - Optional")]
        [Description("The index of the GM / gtmenu.dat screen this button should go to when clicked")]
        public int LinkToScreen
        {
            get;
            set;
        }

        [Category("B - Optional")]
        [Description("The Race to query attributes for, the id of the Wheel to fit, or the Car to show the price of")]
        public string RaceOrWheelOrCarId
        {
            get;
            set;
        }

        [Category("B - Optional")]
        [Description("If RaceMode BehaviourAttribute & PrizeMoney QueryAttribute are set, the position to display prize money for")]
        public sbyte PrizeMoneyPosition
        {
            get { return prizeMoneyPosition; }
            set
            {
                if (value <= -1)
                {
                    value = -1;
                }
                else if(value >= 6)
                {
                    value = 6;
                }
                prizeMoneyPosition = value;
            }
        }

        [Category("B - Optional")]
        [Description("If Contents is None, and QueryAttributes includes ShowArrowIf, getting this license will show a clickable arrow button")]
        public GTMP.GMFile.ShowArrowIfLicense ArrowEnabler
        {
            get;
            set;
        }

        public override void Draw(Graphics g, Rectangle clipRect, bool drawOutline)
        {
            Rectangle loc = Location;
            if (loc.IntersectsWith(clipRect) || hardcodedRenderLoc.IntersectsWith(clipRect))
            {
                Color usedColour = isSelected ? Globals.App.SelectedOutlineColour : outline;
                using(Pen p = new Pen(usedColour))
                {
                    if (drawOutline)
                    {
                        g.DrawRectangle(p, loc);
                    }
                    if (ShowInnerContent && Globals.App.ShowInnerContent)
                    {
                        renderer.Render(g, loc);
                    }
                }
                Brush anchor = Globals.App.AnchorBrush;
                g.FillRectangle(anchor, centralAnchor);
                g.FillRectangles(anchor, cornerAnchors);
            }
        }

        private void RefreshRenderer()
        {
            IBoxContentRenderer oldRenderer = renderer;
            renderer = BoxContentsRenderer.Create(Contents, BehaviourAttributes, QueryAttributes, OutlineColour);
            if(oldRenderer != null)
            {
                oldRenderer.Dispose();
            }
            if (!renderer.HardcodedRenderLocation(ref hardcodedRenderLoc))
            {
                hardcodedRenderLoc = Rectangle.Empty;
            }
            Rectangle loc = Rectangle.Union(Location, hardcodedRenderLoc);
            RaiseDisplayChanged(loc, loc);
        }

        public override string ToString()
        {
            string contentName = Enum.GetName(typeof(GTMP.GMFile.BoxItem), Contents);
            return String.Format("{0}, {1}, {2}", Name, contentName, Location.ToString());
        }

        public override void Serialize(System.IO.BinaryWriter stream)
        {
            CheckBoxState();
            Rectangle bounds = Location;
            Point topLeft = bounds.Location;
            Size boundsSize = bounds.Size;
            string carRaceWheelId = RaceOrWheelOrCarId;
            int screenLink = LinkToScreen;
            // bounding box
            stream.Write((ushort)topLeft.X); // 0
            stream.Write((ushort)topLeft.Y); // 2
            stream.Write((ushort)(topLeft.X + boundsSize.Width)); // 4
            stream.Write((ushort)(topLeft.Y + boundsSize.Height)); // 6
            // contents
            stream.Write((ushort)content); // 8
            stream.Write((byte)queryAttrs); // 0xa
            stream.Write((byte)infoAttrs); // 0xb

            // extradata
            byte[] zeroes = new byte[0x40];
            int bytesWritten = 0xc;

            if (((infoAttrs & (GTMP.GMFile.InfoBoxAttributes.FitWheel | GTMP.GMFile.InfoBoxAttributes.RaceMode)) != 0) ||
                (content == GTMP.GMFile.BoxItem.ChampionshipBonusPrize)
            )
            {
                bool isAWheel = (infoAttrs & GTMP.GMFile.InfoBoxAttributes.FitWheel) != 0;
                byte[] data = Encoding.ASCII.GetBytes(isAWheel ? carRaceWheelId.ToLowerInvariant() : carRaceWheelId.ToUpperInvariant());
                stream.Write(data, 0, data.Length); // 0xc or 0x10
                bytesWritten += data.Length;
            }
            else
            {
                stream.Write((screenLink == -1) ? 0 : screenLink); // 0xc
                bytesWritten += 4;
                if (content == GTMP.GMFile.BoxItem.DealershipNewCarBox)
                {
                    int carId = Int32.Parse(carRaceWheelId, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
                    byte[] data = BitConverter.GetBytes(carId);
                    stream.Write(data, 0, data.Length); // 0x10
                    bytesWritten += data.Length;
                }
            }
            // I wonder why the extradata section is 0x40 bytes big, when at most, 
            // only about 10 can be used at once
            int zeroesToWrite = 0x4a - bytesWritten;
            stream.Write(zeroes, 0, zeroesToWrite);
            bytesWritten += zeroesToWrite;

            // last but one byte
            Debug.Assert(bytesWritten == 0x4a);
            if ((queryAttrs & GTMP.GMFile.QueryAttributes.ShowArrowIf) != 0)
            {
                stream.Write((byte)ArrowEnabler);
            }
            else
            {
                stream.Write(zeroes[0]);
            }
            ++bytesWritten;
            Debug.Assert(bytesWritten == 0x4b);
            // this is the last byte
            if ((queryAttrs & GTMP.GMFile.QueryAttributes.PrizeMoney) != 0)
            {
                stream.Write((byte)prizeMoneyPosition);
            }
            else
            {
                stream.Write(zeroes[0]);
            }
            ++bytesWritten;
            Debug.Assert(bytesWritten == 0x4c);
        }

        public override IBox Clone()
        {
            Box newBox = new Box(this.Location);
            newBox.Contents = Contents;
            newBox.BehaviourAttributes = BehaviourAttributes;
            newBox.OutlineColour = OutlineColour;
            newBox.PrizeMoneyPosition = PrizeMoneyPosition;
            newBox.QueryAttributes = QueryAttributes;
            newBox.RaceOrWheelOrCarId = RaceOrWheelOrCarId;
            newBox.LinkToScreen = LinkToScreen;
            newBox.ShowInnerContent = ShowInnerContent;
            return newBox;
        }

        public override BoxHitTest HitTest(Point p)
        {
            BoxHitTest hitLocation = BoxHitTest.None;
            if (centralAnchor.Contains(p))
            {
                hitLocation = BoxHitTest.AnchorPoint;
            }
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
            return hitLocation;
        }

        public override void Move(Point delta)
        {
            Rectangle newLoc = Location;
            newLoc.Offset(delta);
            Location = newLoc;
        }

        public override void Resize(BoxHitTest corner, Point delta)
        {
            Debug.Assert(corner != BoxHitTest.AnchorPoint);
            Rectangle origLoc = Location;
            int top, left, right, bottom = origLoc.Bottom;
            if(((int)corner & ResizePointTopBit) != 0)
            {
                top = origLoc.Top + delta.Y;
                bottom = origLoc.Bottom;
            }
            else
            {
                bottom = origLoc.Bottom + delta.Y;
                top = origLoc.Top;
            }
            if(((int)corner & ResizePointLeftBit) != 0)
            {
                left = origLoc.Left + delta.X;
                right = origLoc.Right;
            }
            else
            {
                right = origLoc.Right + delta.X;
                left = origLoc.Left;
            }
            Rectangle newLoc = new Rectangle(left, top, right - left, bottom - top);
            Location = newLoc;
        }

        public override void Select(bool state)
        {
            isSelected = state;
            Rectangle loc = Location;
            Size locSize = loc.Size;
            locSize.Height += 1;
            locSize.Width += 1;
            loc.Size = locSize;
            RaiseDisplayChanged(loc, loc);
        }

        private void App_SettingsChanged(object sender, EventArgs e)
        {
            Rectangle curBounds = Location;
            if (!customOutlineColour)
            {
                outline = Globals.App.DefaultOutlineColour;
            }
            UpdateAnchorPositions(curBounds);
            RaiseDisplayChanged(curBounds, curBounds);
        }

        public static void ResetIndexCount()
        {
            INDEX = 0;
        }

        private void ThrowInvalidState(string reason)
        {
            string message = String.Format("Box '{0}' has an invalid state: {1}", Name, reason);
            DebugLogger.Log("Box", "Throwing invalid box exception: " + message);
            throw new InvalidBoxStateException(message, this);
        }

        private void CheckBoxState()
        {
            int screenLink = LinkToScreen;
            string carRaceOrWheelId = RaceOrWheelOrCarId;
            // contents
            if (content == GTMP.GMFile.BoxItem.ChampionshipBonusPrize)
            {
                if (((queryAttrs & GTMP.GMFile.QueryAttributes.PrizeMoney) == 0) || ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.RaceMode) == 0) || (prizeMoneyPosition != 0))
                {
                    ThrowInvalidState("ChampionshipBonusPrize Content requires the PrizeMoney QueryAttribute, RaceMode BehaviourAttribute and PrizeMoneyPosition to be 0");
                }
            }
            // these are the only two contents that require extradata
            if ((content == GTMP.GMFile.BoxItem.DealershipNewCarBox) ||
               (content == GTMP.GMFile.BoxItem.ChampionshipBonusPrize)
            )
            {
                if (String.IsNullOrEmpty(carRaceOrWheelId))
                {
                    ThrowInvalidState("DealershipNewCarBox and ChampionshipBonusPrize contents require a car/race id");
                }
            }
            // query attributes
            if ((queryAttrs & GTMP.GMFile.QueryAttributes.ShowArrowIf) != 0)
            {
                if (content != GTMP.GMFile.BoxItem.None)
                {
                    ThrowInvalidState("ShowArrowIf can only be used with a Content of None");
                }
                if(ArrowEnabler == GTMP.GMFile.ShowArrowIfLicense.None)
                {
                    ThrowInvalidState("ShowArrowIf QueryAttribute is set, but ArrowEnabler is invalid");
                }
            }
            const GTMP.GMFile.QueryAttributes allExceptArrow = GTMP.GMFile.QueryAttributes.AllTypes & ~GTMP.GMFile.QueryAttributes.ShowArrowIf;
            if ((queryAttrs & allExceptArrow) != 0)
            {
                if (String.IsNullOrEmpty(carRaceOrWheelId))
                {
                    ThrowInvalidState("No RaceId was given but QueryAttributes for race data are");
                }
                // this is ok, set queryattributes are just ignored if RaceMode isn't set
                // but if you've set them, you probably want them to show up
                if ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.RaceMode) == 0)
                {
                    ThrowInvalidState("QueryAttributes are set but not the RaceMode BehaviourAttribute");
                }
                if ((queryAttrs & GTMP.GMFile.QueryAttributes.PrizeMoney) != 0)
                {
                    if (prizeMoneyPosition == -1)
                    {
                        ThrowInvalidState("PrizeMoney QueryAttribute is set, but PrizeMoneyPosition is invalid");
                    }
                }
            }
            // info attributes
            if((infoAttrs & (GTMP.GMFile.InfoBoxAttributes.FitWheel | GTMP.GMFile.InfoBoxAttributes.RaceMode)) != 0)
            {
                if(String.IsNullOrEmpty(carRaceOrWheelId))
                {
                    ThrowInvalidState("FitWheel or RaceMode BehaviourAttribute is set, but CarOrRaceOrWheelId is blank");
                }
                if (screenLink != -1)
                {
                    ThrowInvalidState("FitWheel or RaceMode BehaviourAttribute cannot be used with LinkToScreen/navigation buttons");
                }
            }
            if ((infoAttrs & (GTMP.GMFile.InfoBoxAttributes.RaceMode | GTMP.GMFile.InfoBoxAttributes.ShowRacePreview)) == GTMP.GMFile.InfoBoxAttributes.ShowRacePreview)
            {
                ThrowInvalidState("ShowRacePreview BehaviourAttribute is set, but not the RaceMode attribute");
            }
            if ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.DontRenderDealerPrice) != 0)
            {
                if (content != GTMP.GMFile.BoxItem.DealershipNewCarBox)
                {
                    ThrowInvalidState("DontRenderDealerPrince BehaviourAttribute is set, but Contents is not a DealershipNewCarBox");
                }
            }
            // properties
            if (!String.IsNullOrEmpty(carRaceOrWheelId))
            {
                int maxLength = 8;
                int minLength = 5;
                string validEntryRegex = "^[A-Z0-9a-z]+$";
                if(content == GTMP.GMFile.BoxItem.DealershipNewCarBox)
                {
                    // car ids are 4 byte hex numbers, so 8 (or 7 with a starting 0) characters in length
                    // they can also only comprise of hex digits 
                    minLength = 7;
                    validEntryRegex = "^[A-Fa-f0-9]+$";
                }
                else if ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.FitWheel) != 0)
                {
                    validEntryRegex = "^[A-Z0-9a-z\\-]+$";
                    // wheel names are only 8 characters long
                    minLength = 8;
                }
                else if ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.RaceMode) != 0)
                {
                    // race names are 7-13 characters long
                    // (7LN0001 - FREECHAMP0501)
                    // but licences are 5 (LJA00) and the 400m speedtest is 4 (G400)
                    // and they share this
                    minLength = 4;
                    maxLength = 13;
                }
                else
                {
                    ThrowInvalidState("CarOrRaceOrWheelId is set but no Contents or BehaviourAttributes that would uae it");
                }
                int idLength = carRaceOrWheelId.Length;
                if ((idLength < minLength) || (idLength > maxLength))
                {
                    ThrowInvalidState(String.Format("RaceOrWheelOrCarId is invalid. It should be between {0}-{1} characters long", minLength, maxLength));
                }
                if (!System.Text.RegularExpressions.Regex.Match(carRaceOrWheelId, validEntryRegex).Success)
                {
                    ThrowInvalidState(
                        String.Format(
                            "RaceOrWheelOrCarId is invalid. It contains invalid characters (Allowed: {0})",
                            validEntryRegex.Substring(2, validEntryRegex.Length - 5)
                        )                        
                    );
                }
            }
            if(prizeMoneyPosition != -1)
            {
                if (((infoAttrs & GTMP.GMFile.InfoBoxAttributes.RaceMode) == 0) || ((queryAttrs & GTMP.GMFile.QueryAttributes.PrizeMoney) == 0))
                {
                    ThrowInvalidState("PrizeMoneyPosition is set but neither the RaceMode BehaviourAttribute or the PrizeMoney QueryAttribute is");
                }
            }
            if ((screenLink != 0) && (screenLink < -1))
            {
                if (screenLink < 0)
                {
                    ThrowInvalidState("LinkToScreen cannot be negative, except for -1 for no link");
                }
                if ((infoAttrs & GTMP.GMFile.InfoBoxAttributes.CanCursor) == 0)
                {
                    ThrowInvalidState("Has a LinkToScreen but box doesn't have the CanCursor BehaviourAttribute");
                }
            }
        }
    }
}
