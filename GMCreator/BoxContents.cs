using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using GTMP;

namespace GMCreator
{
    static class BoxContentsRenderer
    {
        internal static Dictionary<int, string> textsToRender = null;
        internal const int ATTRIBUTE_TEXT_OFFSET = 0x100;
        internal const int CARLIST_TEXT_OFFSET = 0x200;
        internal const int EQUIPPED_PARTS_TEXT_OFFSET = 0x300;
        private static void InitTexts()
        {
            if (textsToRender == null)
            {
                // these are the longest strings of each type (or thereabouts)
                textsToRender = new Dictionary<int, string>((int)GMFile.BoxItem.LastBoxItemValue);
                textsToRender[(int)GMFile.BoxItem.DealershipNewCarBox] = "10,953,000";
                textsToRender[(int)GMFile.BoxItem.CarColourName] = "Silver Pearl Metallic with Tuxedo Black";
                textsToRender[(int)GMFile.BoxItem.CurrentCarShortName] = "MotorsportElise";
                textsToRender[(int)GMFile.BoxItem.CurrentCarHP] =
                textsToRender[(int)GMFile.BoxItem.UpgradeCarHP] = 
                textsToRender[(int)GMFile.BoxItem.CarHP] = "1011hp";
                textsToRender[(int)GMFile.BoxItem.CarWeight] = "1520kg";
                textsToRender[(int)GMFile.BoxItem.CreditsBalance] = "1,496,420,600";
                textsToRender[(int)GMFile.BoxItem.Price] = "20,000,000";
                textsToRender[(int)GMFile.BoxItem.DayCount] = "372";
                textsToRender[(int)GMFile.BoxItem.UpgradeName] = "Suspension - Fully Customized Service";
                textsToRender[(int)GMFile.BoxItem.CarYear] = "'98";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarLength] = "3796mm";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarHeight] = "1200mm";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarWidth] = "1716mm";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarWeight] = "1543lb";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarEngineCapacity] = "654x2cc";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarDrivetrain] = "4WD";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarEngineType] = "Rotar2 Rotary";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarMaxPower] = "1011hp / 9000rpm";
                textsToRender[(int)GMFile.BoxItem.InfoScreenCarMaxTorque] = "975.5lb-ft / 7000rpm";
                textsToRender[(int)GMFile.BoxItem.StatusPercentageCompletion] = "40.63%";
                textsToRender[(int)GMFile.BoxItem.StatusTotalPrizeMoney] = "10,393,500";
                textsToRender[(int)GMFile.BoxItem.StatusTotalRaces] = "1529";
                textsToRender[(int)GMFile.BoxItem.StatusTotalWins] = "1129";
                textsToRender[(int)GMFile.BoxItem.StatusWinPercentage] = "73.84";
                textsToRender[(int)GMFile.BoxItem.StatusAvgRanking] = "2.50";
                textsToRender[(int)GMFile.BoxItem.StatusCarsOwned] = "100";
                textsToRender[(int)GMFile.BoxItem.StatusTotalCarValue] = "7,110,520,000";
                textsToRender[(int)GMFile.BoxItem.Best400MTimeTrial] = "Silvia Q's 1800cc (J) '88  0:14.390";
                textsToRender[(int)GMFile.BoxItem.HighestPoweredCarInGarage] = "Silvia Q's 1800cc (J) '88 121hp";
                textsToRender[(int)GMFile.BoxItem.BestMaxSpeedTest] = "Silvia Q's 1800cc (J) '88  140.60mph";
                textsToRender[(int)GMFile.BoxItem.StatusNumberOfLicenseGoldTrophies] =
                textsToRender[(int)GMFile.BoxItem.StatusNumberOfLicenseSilverTrophies] =
                textsToRender[(int)GMFile.BoxItem.StatusNumberOfLicenseBronzeTrophies] =
                textsToRender[(int)GMFile.BoxItem.StatusNumberOfLicenseKidsTrophies] = "50";
                textsToRender[(int)GMFile.BoxItem.ChampionshipBonusPrize] = "15,000,000";
                textsToRender[(int)GMFile.BoxItem.CarSaleValue] = "50,000,000";

                textsToRender[ATTRIBUTE_TEXT_OFFSET + (int)GMFile.QueryAttributes.PrizeMoney] = "10,000,000";
                textsToRender[ATTRIBUTE_TEXT_OFFSET + (int)GMFile.QueryAttributes.RaceResult] = "6";
                textsToRender[ATTRIBUTE_TEXT_OFFSET + (int)GMFile.QueryAttributes.PowerRestriction] = "~690hp";

                textsToRender[CARLIST_TEXT_OFFSET + 0] = "Mazda  Protege 4Door  Sedan(J)  '89";
                textsToRender[CARLIST_TEXT_OFFSET + 1] = "109,050";

                // top left, top right, bottom left, bottom right
                textsToRender[EQUIPPED_PARTS_TEXT_OFFSET + 0] = "Suspension - Fully Customized Service";
                textsToRender[EQUIPPED_PARTS_TEXT_OFFSET + 1] = "Engine Balancing";
                textsToRender[EQUIPPED_PARTS_TEXT_OFFSET + 2] = "Port & Polish";
                textsToRender[EQUIPPED_PARTS_TEXT_OFFSET + 3] = "1.5Way Limited Slip";
            }
        }

        internal delegate float AlignToRect(Rectangle r, SizeF size);

        static internal float AlignXLeft(Rectangle r, SizeF size)
        {
            return r.Left;
        }

        static internal float AlignYTop(Rectangle r, SizeF size)
        {
            return r.Top - (size.Height * 0.25f);
        }

        static internal float AlignYTopBig(Rectangle r, SizeF size)
        {
            return r.Top;
        }

        static internal float AlignXRight(Rectangle r, SizeF size)
        {
            return r.Right - size.Width;
        }

        static internal float AlignXCentre(Rectangle r, SizeF size)
        {
            return r.Left + ((r.Width - size.Width) / 2);
        }

        static internal float AlignYBottom(Rectangle r, SizeF size)
        {
            return r.Bottom - (size.Height * 0.75f);
        }

        static internal float AlignYBottomBig(Rectangle r, SizeF size)
        {
            return r.Bottom - size.Height;
        }

        static internal float AlignYMiddle(Rectangle r, SizeF size)
        {
            return r.Top + ((r.Height - size.Height) / 2);
        }

        public static IBoxContentRenderer Create(
            GMFile.BoxItem item, 
            GMFile.InfoBoxAttributes infoAttrs, 
            GMFile.QueryAttributes queryAttrs, 
            Color drawColour
        )
        {
            InitTexts();
            IBoxContentRenderer renderer = null;
            string renderText;
            if(!textsToRender.TryGetValue((int)item, out renderText))
            {
                 renderText = "TextMissing";
            }
            string itemName = Enum.GetName(typeof(GMFile.BoxItem), item);
            bool bigText = (infoAttrs & GMFile.InfoBoxAttributes.UseBigFont) != 0;
            Font usedFont = bigText ? Hardcoded.BigFont : Hardcoded.StandardFont;
            ProbeDataRenderer probeRenderer = new ProbeDataRenderer(infoAttrs, queryAttrs, usedFont);
            switch (item)
            {
                case GMFile.BoxItem.DealershipNewCarBox:
                    {
                        if ((infoAttrs & GMFile.InfoBoxAttributes.DontRenderDealerPrice) != 0)
                        {
                            renderer = new BlankRenderer("Blank - " + itemName, probeRenderer, drawColour);
                        }
                        else
                        {
                            renderer = new DealershipCarPriceRenderer(renderText, probeRenderer, usedFont, bigText);
                        }
                    }
                    break;
                case GMFile.BoxItem.CarColourName:
                case GMFile.BoxItem.CreditsBalance:
                case GMFile.BoxItem.Price:
                case GMFile.BoxItem.DayCount:
                case GMFile.BoxItem.ChampionshipBonusPrize:
                case GMFile.BoxItem.CarSaleValue:
                    {
                        AlignToRect bottom = bigText ? new AlignToRect(AlignYBottomBig) : new AlignToRect(AlignYBottom);
                        AlignToRect right = new AlignToRect(AlignXRight);
                        renderer = new AlignedTextRenderer(renderText, probeRenderer, right, bottom, usedFont);
                    }
                    break;
                case GMFile.BoxItem.CurrentCarHP:
                case GMFile.BoxItem.UpgradeCarHP:
                    {
                        AlignToRect bottom = bigText ? new AlignToRect(AlignYBottomBig) : new AlignToRect(AlignYBottom);
                        AlignToRect right = new AlignToRect(AlignXRight);
                        renderer = new AlignedTextRenderer(renderText, probeRenderer, right, bottom, usedFont, Hardcoded.UpgradeHPTextColour);
                    }
                    break;
                case GMFile.BoxItem.CurrentCarShortName:
                case GMFile.BoxItem.CarHP:
                case GMFile.BoxItem.CarWeight:
                case GMFile.BoxItem.CarYear:
                    {
                        AlignToRect bottom = bigText ? new AlignToRect(AlignYBottomBig) : new AlignToRect(AlignYBottom);
                        AlignToRect left = new AlignToRect(AlignXLeft);
                        renderer = new AlignedTextRenderer(renderText, probeRenderer, left, bottom, usedFont);
                    }
                    break;
                case GMFile.BoxItem.UpgradeName:
                    {
                        AlignToRect bottom = bigText ? new AlignToRect(AlignYBottomBig) : new AlignToRect(AlignYBottom);
                        AlignToRect centre = new AlignToRect(AlignXCentre);
                        renderer = new AlignedTextRenderer(renderText, probeRenderer, centre, bottom, Hardcoded.PartFont, Hardcoded.PartFontColour);
                    }
                    break;
                case GMFile.BoxItem.CarColourSwatches:
                    {
                        renderer = new SwatchRenderer(probeRenderer);
                    }
                    break;
                case GMFile.BoxItem.UsedCarList:
                case GMFile.BoxItem.GarageCarList:
                    {
                        renderer = new CarListRenderer(item, probeRenderer);
                    }
                    break;
                case GMFile.BoxItem.CarDisplay:
                    {
                        renderer = new CarDisplayRenderer(probeRenderer);
                    }
                    break;
                case GMFile.BoxItem.CarLogo:
                case GMFile.BoxItem.CarDrivetrainGraphic:
                    {
                        renderer = new CentralImageRenderer(
                            probeRenderer, 
                            (item == GMFile.BoxItem.CarLogo) ? Hardcoded.CarLogo : Hardcoded.DrivetrainGraphic
                        );
                    }
                    break;
                case GMFile.BoxItem.StatusHighestGainedLicense:
                    {
                        renderer = new CentralImageRenderer(
                            probeRenderer,
                            Hardcoded.LargeLicenseGraphic
                        );
                    }
                    break;
                case GMFile.BoxItem.CarEquippedPartsList:
                    {
                        renderer = new EquippedCarPartsRenderer(probeRenderer);
                    }
                    break;
                default:
                    if ((item >= GMFile.BoxItem.InfoScreenCarLength) && (item <= GMFile.BoxItem.InfoScreenCarMaxTorque))
                    {
                        AlignToRect bottom = bigText ? new AlignToRect(AlignYBottomBig) : new AlignToRect(AlignYBottom);
                        AlignToRect left = new AlignToRect(AlignXLeft);
                        renderer = new AlignedTextRenderer(renderText, probeRenderer, left, bottom, usedFont);
                    }
                    else if ((item >= GMFile.BoxItem.StatusPercentageCompletion) && (item <= GMFile.BoxItem.StatusNumberOfLicenseKidsTrophies))
                    {
                        AlignToRect bottom = bigText ? new AlignToRect(AlignYBottomBig) : new AlignToRect(AlignYBottom);
                        AlignToRect right = new AlignToRect(AlignXRight);
                        renderer = new AlignedTextRenderer(renderText, probeRenderer, right, bottom, usedFont);
                    }
                    else if ((item >= GMFile.BoxItem.IC1LicenseResult) && (item <= GMFile.BoxItem.S10LicenseResult))
                    {
                        renderer = new CentralImageRenderer(probeRenderer, Hardcoded.LicenseTrophyGraphic);
                    }
                    else
                    {
                        renderer = new BlankRenderer("Blank - " + itemName, probeRenderer, drawColour);
                    }
                    break;
            }
            return renderer;
        }
    }

    interface IBoxContentRenderer : IDisposable
    {
        void Render(Graphics g, Rectangle r);
        // This is for things that have don't respect the box's bounds
        // like the garage/used car list and equipped parts list
        // these things render to hardcoded screen locations, so if the user draws a box at
        // 0x0-2x2 and this thing is hardcoded to render at 32x160-512x384 then 
        // redrawing at the former location won't show this, and when the hardcoded bit
        // needs redrawing, our box's drawing code wouldn't redraw it
        // since the dirty rect wouldn't  be inside the boxes draw rect
        bool HardcodedRenderLocation(ref Rectangle renderLoc);

        // mainly for the Empty boxes and the ProbeRenderer
        // If this is true, the empty boxes won't display their 'Blank' text
        // otherwise they will
        bool HasRenderContent();
    }

    class NullRenderer : IBoxContentRenderer
    {
        public void Render(Graphics g, Rectangle r)
        {
            ;
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public void Dispose()
        {
            ;
        }

        public bool HasRenderContent()
        {
            return false;
        }
    }

    class ProbeDataRenderer : IBoxContentRenderer
    {
        private List<IBoxContentRenderer> childRenderers;
#if DEBUG
        bool hasDisposed;
#endif

        const GMFile.InfoBoxAttributes probeAttrs = /*GMFile.InfoBoxAttributes.ProbeCarInfo | */GMFile.InfoBoxAttributes.RaceMode;

        public ProbeDataRenderer(GMFile.InfoBoxAttributes infoAttrs, GMFile.QueryAttributes queryAttrs, Font font)
        {
            bool bigText = (infoAttrs & GMFile.InfoBoxAttributes.UseBigFont) != 0;
            BoxContentsRenderer.AlignToRect bottomAligner = bigText ? new BoxContentsRenderer.AlignToRect(BoxContentsRenderer.AlignYBottomBig) : new BoxContentsRenderer.AlignToRect(BoxContentsRenderer.AlignYBottom);
#if DEBUG
            hasDisposed = false;
#endif
            childRenderers = new List<IBoxContentRenderer>();
            if ((infoAttrs & GMFile.InfoBoxAttributes.RaceMode) != 0)
            {
                int attr;
                NullRenderer nullRender = new NullRenderer();
                if ((attr = (int)(queryAttrs & GMFile.QueryAttributes.PowerRestriction)) != 0)
                {
                    string text = BoxContentsRenderer.textsToRender[BoxContentsRenderer.ATTRIBUTE_TEXT_OFFSET + attr];
                    // bottom left
                    BoxContentsRenderer.AlignToRect left = new BoxContentsRenderer.AlignToRect(BoxContentsRenderer.AlignXLeft);
                    childRenderers.Add(new AlignedTextRenderer(text, nullRender, left, bottomAligner, font));
                }
                if ((attr = (int)(queryAttrs & GMFile.QueryAttributes.PrizeMoney)) != 0)
                {
                    // bottom right
                    BoxContentsRenderer.AlignToRect right = new BoxContentsRenderer.AlignToRect(BoxContentsRenderer.AlignXRight);
                    string text = BoxContentsRenderer.textsToRender[BoxContentsRenderer.ATTRIBUTE_TEXT_OFFSET + attr];
                    childRenderers.Add(new AlignedTextRenderer(text, nullRender, right, bottomAligner, font));
                }
                if ((attr = (int)(queryAttrs & GMFile.QueryAttributes.RaceResult)) != 0)
                {
                    // bottom right
                    BoxContentsRenderer.AlignToRect right = new BoxContentsRenderer.AlignToRect(BoxContentsRenderer.AlignXRight);
                    string text = BoxContentsRenderer.textsToRender[BoxContentsRenderer.ATTRIBUTE_TEXT_OFFSET + attr];
                    childRenderers.Add(new AlignedTextRenderer(text, nullRender, right, bottomAligner, font));
                }
            }
        }

        public void Dispose()
        {
            foreach (IBoxContentRenderer bcr in childRenderers)
            {
                bcr.Dispose();
            }
#if DEBUG
            hasDisposed = true;
#endif
        }

        public void Render(Graphics g, Rectangle r)
        {
#if DEBUG
            Debug.Assert(!hasDisposed);
#endif
            foreach (IBoxContentRenderer bcr in childRenderers)
            {
                bcr.Render(g, r);
            }
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return childRenderers.Count > 0;
        }
    }

    class BlankRenderer : IBoxContentRenderer
    {
        private string text;
        private Brush textColour;
        private Font genericSansFont;
        private RectangleF rectF;
        private IBoxContentRenderer probeRenderer;
        private StringFormat typeFormat;

        public BlankRenderer(string itemName, IBoxContentRenderer probeRender, Color drawColour)
        {
            text = itemName;
            textColour = new SolidBrush(drawColour);
            genericSansFont = new Font(FontFamily.GenericSansSerif, 12.0f);
            rectF = new RectangleF();
            probeRenderer = probeRender;
            typeFormat = StringFormat.GenericTypographic;
        }

        public void Render(Graphics g, Rectangle r)
        {
            probeRenderer.Render(g, r);
            if (!probeRenderer.HasRenderContent())
            {
                rectF.Location = new PointF(r.Left, r.Top);
                rectF.Size = new SizeF(r.Width, r.Height);
                g.DrawString(text, genericSansFont, textColour, rectF, typeFormat);
            }
        }

        public void Dispose()
        {
            probeRenderer.Dispose();
            textColour.Dispose();
            genericSansFont.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class DealershipCarPriceRenderer : IBoxContentRenderer
    {
        private string renderText;
        private Font font;
        private SizeF stringSize;
        private int xAdjustment;
        private int yAdjustment;
        private IBoxContentRenderer probeRenderer;
        private StringFormat typeFormat;

        public DealershipCarPriceRenderer(string text, IBoxContentRenderer probeRender, Font renderFont, bool isBigText)
        {
            probeRenderer = probeRender;
            font = renderFont;
            renderText = text;
            stringSize = new SizeF();
            if (isBigText)
            {
                xAdjustment = -37;
                yAdjustment = -16;
            }
            else
            {
                xAdjustment = yAdjustment = 0;
            }
            typeFormat = StringFormat.GenericTypographic;
        }

        public void Render(Graphics g, Rectangle r)
        {
            probeRenderer.Render(g, r);
            if (stringSize.IsEmpty)
            {
                stringSize = g.MeasureString(renderText, font);
            }
            // dealership prices are rendered 20 pixels below where they would be
            // if the bottom of the text touched the bottom of the bounding box
            // and 32 pixels to the left of the right side
            //
            // The left and top of the rect don't matter in the text positioning
            // even if the bounding box is a 1x1 square, the whole text is rendered
            float yPos = ((stringSize.Height - r.Bottom) + 20) + yAdjustment;
            float xPos = (r.Right - 32) + xAdjustment;
            PointF drawPoint = new PointF(xPos, yPos);
            g.DrawString(renderText, font, Hardcoded.DealershipPriceColour, drawPoint, typeFormat);
        }

        public void Dispose()
        {
            probeRenderer.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class AlignedTextRenderer : IBoxContentRenderer
    {
        private string textToRender;
        private IBoxContentRenderer probeRenderer;
        private BoxContentsRenderer.AlignToRect xAligner;
        private BoxContentsRenderer.AlignToRect yAligner;
        private SizeF textSize;
        private Brush brush;
        private Font font;
        private StringFormat typeFormat;
        
        public AlignedTextRenderer(
            string text,
            IBoxContentRenderer probeRender,
            BoxContentsRenderer.AlignToRect xAlignFn, 
            BoxContentsRenderer.AlignToRect yAlignFn,
            Font fontToUse
        )
            : this(text, probeRender, xAlignFn, yAlignFn, fontToUse, Hardcoded.StandardFontColour)
        {
        }

        public AlignedTextRenderer(
            string text,
            IBoxContentRenderer probeRender,
            BoxContentsRenderer.AlignToRect xAlignFn,
            BoxContentsRenderer.AlignToRect yAlignFn,
            Font fontToUse,
            Brush textColour
        )
        {
            textToRender = text;
            probeRenderer = probeRender;
            xAligner = xAlignFn;
            yAligner = yAlignFn;
            font = fontToUse;
            textSize = new SizeF();
            brush = textColour;
            typeFormat = StringFormat.GenericTypographic;
        }

        public void Dispose()
        {
            probeRenderer.Dispose();
        }

        public void Render(Graphics g, Rectangle r)
        {
            probeRenderer.Render(g, r);
            if (textSize.IsEmpty)
            {
                textSize = g.MeasureString(textToRender, font, PointF.Empty, typeFormat);
            }
            PointF textPos = new PointF(
                xAligner(r, textSize),
                yAligner(r, textSize)
            );
            g.DrawString(textToRender, font, brush, textPos, typeFormat);
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class SwatchRenderer : IBoxContentRenderer
    {
        private IBoxContentRenderer probeRenderer;
        private Size swatchSize;
        private Rectangle[] swatchRects;
        private Brush[] swatchColours;

        public SwatchRenderer(IBoxContentRenderer probeRender)
        {
            probeRenderer = probeRender;
            swatchSize = Hardcoded.ColourSwatchSize;
            swatchRects = new Rectangle[14];
            swatchColours = new Brush[]{Brushes.Yellow, Brushes.YellowGreen, Brushes.Wheat, Brushes.Tomato, Brushes.Brown, Brushes.BurlyWood, Brushes.GhostWhite};
        }

        public void Render(Graphics g, Rectangle r)
        {
            probeRenderer.Render(g, r);
            // swatches are aligned top right
            Point lastRect = r.Location;
            lastRect.Offset(r.Width, 0); // so the point starts at the r.Right, the firs rewind will then make it swatchSize.width away rom r.Right
            int numRects = swatchRects.Length;
            int numColours = swatchColours.Length;
            int rewindSize = -swatchSize.Width;
            for(int i = 0; i < numRects; ++i)
            {
                lastRect.Offset(rewindSize, 0);
                swatchRects[i] = new Rectangle(lastRect, swatchSize);
                g.FillRectangle(swatchColours[i % numColours], swatchRects[i]);
            }
            g.DrawRectangles(Pens.Gray, swatchRects);
        }

        public void Dispose()
        {
            probeRenderer.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class CarListRenderer : IBoxContentRenderer
    {
        private CarListMeasurements renderMeasurements;
        private Rectangle[] itemRects;
        private IBoxContentRenderer probeRenderer;
        private Brush textColour;
        private LinearGradientBrush swatchGradient;
        private Pen borderPen;
        private StringFormat typeFormat;
        private bool includePrice;

        public CarListRenderer(GMFile.BoxItem item, IBoxContentRenderer probeRender)
        {
            probeRenderer = probeRender;
            if (item == GMFile.BoxItem.UsedCarList)
            {
                renderMeasurements = Hardcoded.UsedCarListMeasurements;
                includePrice = true;
            }
            else
            {
                renderMeasurements = Hardcoded.GarageCarListMeasurements;
                includePrice = false;
            }
            textColour = new SolidBrush(renderMeasurements.TextColour);
            borderPen = new Pen(renderMeasurements.BorderColour);
            int numItems = renderMeasurements.ItemsToRender;
            itemRects = new Rectangle[numItems];
            Point topLeft = renderMeasurements.TopLeftRenderPoint;
            int boxHeight = renderMeasurements.ItemMargin + renderMeasurements.ItemPadding;
            Size itemSize = renderMeasurements.ItemBoxSize;
            // the item box is rendered middle aligned (vertically)
            // boxHeight - the total height from the top of one box space to another
            // itemSize - the size of the actual box content
            int yOffset = (boxHeight - itemSize.Height) / 2;
            for (int i = 0; i < numItems; ++i)
            {
                int yPos = topLeft.Y + yOffset;
                itemRects[i] = new Rectangle(topLeft.X, yPos, itemSize.Width, itemSize.Height);
                topLeft.Offset(0, boxHeight);
            }
            typeFormat = StringFormat.GenericTypographic;
            swatchGradient = new LinearGradientBrush(
                new Rectangle(Point.Empty, renderMeasurements.SwatchSize),
                Color.White, 
                Color.Blue, 
                LinearGradientMode.ForwardDiagonal
            );
        }

        public void Render(Graphics g, Rectangle r)
        {
            g.DrawRectangles(borderPen, itemRects);
            SizeF priceSize = SizeF.Empty;
            if (includePrice)
            {
                priceSize = g.MeasureString(
                    BoxContentsRenderer.textsToRender[BoxContentsRenderer.CARLIST_TEXT_OFFSET + 1],
                    renderMeasurements.TextFont,
                    PointF.Empty,
                    typeFormat
                );
            }
            Size swatchSize = renderMeasurements.SwatchSize;
            Point swatchPosition = renderMeasurements.SwatchPosition;
            Point carNameOffset = renderMeasurements.CarNameOffset;
            Font textFont = renderMeasurements.TextFont;
            foreach (Rectangle items in itemRects)
            {
                Point textLoc = items.Location;
                Point startLoc = textLoc;
                startLoc.Offset(swatchPosition);
                Rectangle swatchRect = new Rectangle(startLoc, swatchSize);
                g.FillRectangle(swatchGradient, swatchRect);
                textLoc.Offset(carNameOffset);
                g.DrawString(
                    BoxContentsRenderer.textsToRender[BoxContentsRenderer.CARLIST_TEXT_OFFSET + 0],
                    textFont,
                    textColour,
                    textLoc,
                    typeFormat
                );
                if (includePrice)
                {
                    // this is where the end of the text should be
                    Point pricePoint = new Point(items.Right - 12, items.Top);
                    // then we subtract the width to get the start point
                    pricePoint.Offset((int)-priceSize.Width, 0);
                    g.DrawString(
                        BoxContentsRenderer.textsToRender[BoxContentsRenderer.CARLIST_TEXT_OFFSET + 1],
                        textFont,
                        textColour,
                        pricePoint,
                        typeFormat
                    );
                }
            }
            // these render on top so it's here instead of the top
            probeRenderer.Render(g, r);
        }

        public void Dispose()
        {
            borderPen.Dispose();
            swatchGradient.Dispose();
            textColour.Dispose();
            probeRenderer.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            Rectangle lastRect = itemRects[itemRects.Length - 1];
            Point topLeft = renderMeasurements.TopLeftRenderPoint;
            Size rectSize = new Size(lastRect.Right - topLeft.X, lastRect.Bottom - topLeft.Y);
            renderLoc = new Rectangle(renderMeasurements.TopLeftRenderPoint, rectSize);
            return true;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class CarDisplayRenderer : IBoxContentRenderer
    {
        private IBoxContentRenderer probeRender;
        public CarDisplayRenderer(IBoxContentRenderer probeRenderer)
        {
            probeRender = probeRenderer;
        }

        public void Render(Graphics g, Rectangle r)
        {
            probeRender.Render(g, r);
            g.DrawImage(Hardcoded.CarPicture, r);
        }

        public void Dispose()
        {
            probeRender.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class CentralImageRenderer : IBoxContentRenderer
    {
        private IBoxContentRenderer probeRender;
        private Bitmap renderImage;

        public CentralImageRenderer(IBoxContentRenderer probeRenderer, Bitmap image)
        {
            probeRender = probeRenderer;
            renderImage = image;
        }

        public void Render(Graphics g, Rectangle r)
        {
            probeRender.Render(g, r);
            Point p = new Point(r.Left + ((r.Width - renderImage.Width) / 2), r.Top + ((r.Height - renderImage.Height) / 2));
            g.DrawImage(renderImage, p);
        }

        public void Dispose()
        {
            probeRender.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class EquippedCarPartsRenderer : IBoxContentRenderer
    {
        private IBoxContentRenderer probeRenderer;
        private StringFormat typeFormat;

        public EquippedCarPartsRenderer(IBoxContentRenderer probeRender)
        {
            probeRenderer = probeRender;
            typeFormat = StringFormat.GenericTypographic;
        }

        public void Render(Graphics g, Rectangle r)
        {
            // the ones on the right side are rendererd first, so that if the left text overlaps
            // you can see all of that
            CarPartsListMeasurements equipped = Hardcoded.EquippedPartsListMeasurements;
            Point[] points = { equipped.TopLeftItemLocation, equipped.TopRightItemLocation, equipped.BottomLeftItemLocation, equipped.BottomRightItemLocation };
            Point origin = equipped.PartsListScreenOrigin;
            int originX = origin.X;
            int originY = origin.Y;
            int iter = 0;
            foreach (Point p in points)
            {
                Point itemLoc = new Point(
                    originX + p.X,
                    originY + p.Y
                );
                string text = BoxContentsRenderer.textsToRender[BoxContentsRenderer.EQUIPPED_PARTS_TEXT_OFFSET + iter];
                g.DrawString(
                    text,
                    equipped.TextFont,
                    Hardcoded.EquippedPartFontColour,
                    itemLoc,
                    typeFormat
                );
                ++iter;
            }
            probeRenderer.Render(g, r);
        }

        public void Dispose()
        {
            probeRenderer.Dispose();
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            CarPartsListMeasurements equipped = Hardcoded.EquippedPartsListMeasurements;
            Point topLeft = equipped.PartsListScreenOrigin;
            Point bottomRight = topLeft;
            topLeft.Offset(equipped.TopLeftItemLocation);
            bottomRight.Offset(equipped.BottomRightItemLocation);
            bottomRight.Y += equipped.TextFont.Height;
            Size rectSize = new Size(MainForm.CANVAS_WIDTH - topLeft.X, bottomRight.Y +  - topLeft.Y);
            renderLoc = new Rectangle(equipped.TopLeftItemLocation, rectSize);
            return true;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }

    class TopLeftIconImageRenderer : IBoxContentRenderer
    {
        private int iconImageId;
        public TopLeftIconImageRenderer(int iconId)
        {
            iconImageId = iconId;
        }

        public void Render(Graphics g, Rectangle r)
        {
            Bitmap bm = Hardcoded.IconImages.Get(iconImageId).Image;
            g.DrawImage(bm, r.Location);
        }

        public void Dispose()
        {
            ;
        }

        public bool HardcodedRenderLocation(ref Rectangle renderLoc)
        {
            return false;
        }

        public bool HasRenderContent()
        {
            return true;
        }
    }
}
