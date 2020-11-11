using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace GMCreator
{
    internal class CarListMeasurements
    {
        public int ItemsToRender
        {
            get;
            set;
        }

        public int CursorFlashWidth
        {
            get;
            set;
        }

        public int ItemMargin
        {
            get;
            set;
        }

        public int ItemPadding
        {
            get;
            set;
        }

        public Point TopLeftRenderPoint
        {
            get;
            set;
        }

        public Size ItemBoxSize
        {
            get;
            set;
        }

        public Color TextColour
        {
            get;
            set;
        }

        public Color BorderColour
        {
            get;
            set;
        }

        public Font TextFont
        {
            get;
            set;
        }

        public Point SwatchPosition
        {
            get;
            set;
        }

        public Size SwatchSize
        {
            get;
            set;
        }

        public Point CarNameOffset
        {
            get;
            set;
        }
    }

    internal class CarPartsListMeasurements
    {
        public Point PartsListScreenOrigin
        {
            get;
            set;
        }

        public Point TopLeftItemLocation
        {
            get;
            set;
        }

        public Point TopRightItemLocation
        {
            get;
            set;
        }

        public Point BottomLeftItemLocation
        {
            get;
            set;
        }

        public Point BottomRightItemLocation
        {
            get;
            set;
        }

        public Font TextFont
        {
            get;
            set;
        }

        public Color TextColour
        {
            get;
            set;
        }
    }

    class IconImgEntry
    {
        public Point ImageLocation
        {
            get;
            set;
        }

        public Size ImageSize
        {
            get;
            set;
        }

        // absolute file offset
        public ushort PaletteLocation
        {
            get;
            set;
        }

        // in some weird way this indicates which palette to use
        // but I don't know how these values translate to the palette offset
        public ushort Mod1
        {
            get;
            set;
        }

        public ushort Unk3
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        // Not part of hardcoded file
        [JsonIgnore]
        public Bitmap Image
        {
            get;
            set;
        }

        // Not part of hardcoded file
        [JsonIgnore]
        public int Id
        {
            get;
            set;
        }
    }

    enum IconImgType
    {
        JP10,
        JP11,
        US10,
        US12,
        PALEng,
        PALFra,
        PALGer,
        PALIta,
        PALSpa,
        Invalid = -1
    };

    class IconImgFile
    {
        public IconImgEntry[] Entries
        {
            get;
            set;
        }

        public IconImgType Type
        {
            get;
            set;
        }
    }

    class IconImgData
    {
        public int ByteStride
        {
            get;
            set;
        }

        public List<IconImgFile> Versions
        {
            get;
            set;
        }
    }

    // disable
    // Field 'GMCreator.HardcodedData.usedCarList' is never assigned to, and will always have its default value null
#pragma warning disable 649
    // The serialised class
    class HardcodedData
    {
        public CarListMeasurements usedCarList;
        public CarListMeasurements garageCarList;
        public CarPartsListMeasurements installedCarPartsList;
        public Size colourSwatchSize;
        public Color upgradeHPColour;
        public Color dealershipPriceColour;
        public Color standardFontColour;
        public Color partFontColour;
        public Font standardFont;
        public Font bigFont;
        public Font partNameFont;
        public IconImgData iconImageData;
    }
#pragma warning restore 649
}