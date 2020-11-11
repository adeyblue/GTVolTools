using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using GMCreator;

// this needs to be compiled into a separate exe
// it's not part of GMCreate, it's just the code that
// creates the hardcoded.json file

namespace Hardcoded
{
    class MakeGMCreatorDataFile
    {
        static void Main()
        {
            HardcodedData m = new HardcodedData();
            CarListMeasurements used = new CarListMeasurements();
            // these data are in ovl5, offset 0x42920
            used.ItemPadding = 2;
            used.ItemsToRender = 9;
            used.CursorFlashWidth = 416;
            used.ItemMargin = 22;
            // The used car list is hardcoded to 
            // 48x156 - 464x370
            // 9 416x24 rectangles of car info
            //
            // central X-coord of used car list is the 0x100 at 0x1C3F4
            // Y-coord is the 0x9c at 0x1C3FC in US1.0
            used.ItemBoxSize = new Size(416, 24);
            used.TopLeftRenderPoint = new Point(48, 156);
            used.TextColour = Color.FromArgb(0x4a, 0x6b, 0x94);
            used.BorderColour = Color.FromArgb(0x52, 0x29, 0);
            used.TextFont = new Font("Arial", 14.0f, FontStyle.Regular);
            used.SwatchPosition = new Point(8, 9);
            used.SwatchSize = new Size(8, 10);
            used.CarNameOffset = new Point(23, 0);

            // these data are in ovl5,offset 0x428DC
            CarListMeasurements garage = new CarListMeasurements();
            garage.ItemPadding = 2;
            garage.ItemsToRender = 11;
            garage.CursorFlashWidth = 384;
            garage.ItemMargin = 18;
            // The garage car list is similarly hardcoded to
            // 11 384x20 items
            //
            // central X-coord of garage car list is the 0x100 at 0x1C3C8
            // Y-coord is the 0xb0 at 0x1C3D0 in US1.0
            garage.ItemBoxSize = new Size(384, 20);
            garage.TopLeftRenderPoint = new Point(64, 176);
            garage.TextColour = Color.FromArgb(0x4a, 0x6b, 0x94);
            garage.BorderColour = used.BorderColour;
            garage.TextFont = used.TextFont;
            garage.SwatchPosition = new Point(28, 5);
            garage.SwatchSize = new Size(8, 10);
            garage.CarNameOffset = new Point(41, 0);

            // the hardcoded location that the parts list text strings use as the origin
            // is in the instructions at 0x1C470 (the 0x20 (32) here is the x coord)
            // and 0x1C478 (the 0xA0 (160) is the y coord) (US1.0)
            // positions of each item at 0x50a34 in US1.0, 8 byte structs
            // {
            //    ushort upgradeIndicator; (if the car has this upgrade, the text will be rendered, idk if its a bit indicator or index)
            //    ushort textStringToRender; // index
            //    ushort xPos; // offset from the origin (so 0 would be 32 on the screen)
            //    ushort yPos; // offset from the origin (so 0 would be 160 on the screen)
            // }
            CarPartsListMeasurements cplm = new CarPartsListMeasurements();
            cplm.PartsListScreenOrigin = new Point(32, 160);
            cplm.TopLeftItemLocation = new Point(0, 0);
            cplm.TopRightItemLocation = new Point(0xf0, 0);
            cplm.BottomLeftItemLocation = new Point(0, 0xdc);
            cplm.BottomRightItemLocation = new Point(0xf0, 0xdc);
            cplm.TextColour = Color.FromArgb(0xbd, 0xbd, 0xbd);

            m.usedCarList = used;
            m.installedCarPartsList = cplm;
            m.garageCarList = garage;
            m.upgradeHPColour = Color.FromArgb(0xE8, 0xa8, 0x20);
            m.colourSwatchSize = new Size(12, 10);
            m.standardFont = new Font("Arial", 12.0f, FontStyle.Bold);
            m.bigFont = new Font("Arial", 19.0f, FontStyle.Bold);
            m.standardFontColour = Color.FromArgb(0xD0, 0xD0, 0xD0);
            m.dealershipPriceColour = Color.Black;
            m.partNameFont = cplm.TextFont = new Font("Arial", 14.0f, FontStyle.Bold);
            m.partFontColour = Color.FromArgb(0xC6, 0xC6, 0xC6);
            m.iconImageData = MakeIconImgData();

            string jsonData = JsonConvert.SerializeObject(m);
            File.WriteAllText("hardcoded.json", jsonData);
        }

        static IconImgData MakeIconImgData()
        {
            IconImgData imgData = new IconImgData();
            imgData.ByteStride = 128; // 256 image width, but it's 4bpp, so 128 bytes
            imgData.Versions = new List<IconImgFile>();
            imgData.Versions.Add(MakeJP10IconImgData());
            imgData.Versions.Add(MakeJP11IconImgData());
            // US10's icon file is the same as US 1.1
            imgData.Versions.Add(MakeUS10IconImgData());
            imgData.Versions.Add(MakeUS12IconImgData());
            imgData.Versions.Add(MakePalEngIconImgData());
            imgData.Versions.Add(MakePalFraIconImgData());
            imgData.Versions.Add(MakePalGerIconImgData());
            imgData.Versions.Add(MakePalItaIconImgData());
            imgData.Versions.Add(MakePalSpaIconImgData());
            if (
               (imgData.Versions[(int)IconImgType.JP10].Type != IconImgType.JP10) ||
               (imgData.Versions[(int)IconImgType.JP11].Type != IconImgType.JP11) ||
               (imgData.Versions[(int)IconImgType.US10].Type != IconImgType.US10) ||
               (imgData.Versions[(int)IconImgType.US12].Type != IconImgType.US12) ||
               (imgData.Versions[(int)IconImgType.PALEng].Type != IconImgType.PALEng) ||
               (imgData.Versions[(int)IconImgType.PALFra].Type != IconImgType.PALFra) ||
               (imgData.Versions[(int)IconImgType.PALGer].Type != IconImgType.PALGer) ||
               (imgData.Versions[(int)IconImgType.PALIta].Type != IconImgType.PALIta) ||
               (imgData.Versions[(int)IconImgType.PALSpa].Type != IconImgType.PALSpa)
            )
            {
                ConsoleColor front = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The icon image data versions aren't aligned with their region value. GMCreator relies on this");
                Console.Beep();
                Console.ForegroundColor = front;
            }
            return imgData;
        }

        static IconImgFile MakePalSpaIconImgData()
        {
            IconImgFile palSpaVersion = new IconImgFile();
            palSpaVersion.Type = IconImgType.PALSpa;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            entry.ImageLocation = new Point(124, 194);
            entry.ImageSize = iconSize;
            entry.Name = "A Competir";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7d80;

            entry = entries[1];
            entry.Name = "Estado del Juego";
            entry.ImageLocation = new Point(214, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2];
            entry.Name = "Mundo";
            entry.ImageLocation = new Point(98, 194);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3];
            entry.Name = "Dia";
            entry.ImageLocation = new Point(150, 214);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4];
            entry.Name = "Seleccion 4";
            entry.ImageLocation = new Point(156, 189);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5];
            entry.Name = "Seleccion 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6];
            entry.Name = "Seleccion";
            entry.ImageLocation = new Point(158, 73);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7];
            entry.Name = "Local";
            entry.ImageLocation = new Point(214, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7e20;

            entry = entries[8];
            entry.Name = "Comprar";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(214, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9];
            entry.Name = "Seleccion 1";
            entry.ImageLocation = new Point(156, 114);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10];
            entry.Name = "Seleccion 2";
            entry.ImageLocation = new Point(156, 139);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11];
            entry.Name = "Seleccion 3";
            entry.ImageLocation = new Point(156, 164);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            entry = entries[12]; // done
            entry.Name = "Seleccion de Ruedas";
            entry.ImageLocation = new Point(0, 94);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13];
            entry.Name = "Ruedas 1";
            entry.ImageLocation = new Point(0, 114);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            entry = entries[14];
            entry.Name = "Ruedas 2";
            entry.ImageLocation = new Point(0, 134);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;

            entry = entries[15];
            entry.Name = "Ruedas 3";
            entry.ImageLocation = new Point(0, 154);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;

            entry = entries[16];
            entry.Name = "Ruedas 4";
            entry.ImageLocation = new Point(0, 174);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;

            entry = entries[17];
            entry.Name = "Ruedas 5";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;

            palSpaVersion.Entries = entries;
            return palSpaVersion;
        }

        static IconImgFile MakePalItaIconImgData()
        {
            IconImgFile palItaVersion = new IconImgFile();
            palItaVersion.Type = IconImgType.PALIta;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0]; // done
            entry.ImageLocation = new Point(124, 194);
            entry.ImageSize = iconSize;
            entry.Name = "Gareggia";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7d80;

            entry = entries[1]; // done
            entry.Name = "Stato Gioco";
            entry.ImageLocation = new Point(214, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2]; // done
            entry.Name = "Mondo";
            entry.ImageLocation = new Point(98, 194);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3]; // done
            entry.Name = "Giorno";
            entry.ImageLocation = new Point(150, 214);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4]; // done
            entry.Name = "Scelta 4";
            entry.ImageLocation = new Point(156, 189);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5]; // done
            entry.Name = "Scelta 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6]; // done
            entry.Name = "Scelta";
            entry.ImageLocation = new Point(158, 73);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7]; // done
            entry.Name = "Locale";
            entry.ImageLocation = new Point(214, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7e20;

            entry = entries[8]; // done
            entry.Name = "Compra";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(214, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9]; // done
            entry.Name = "Scelta 1";
            entry.ImageLocation = new Point(156, 114);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10]; // done
            entry.Name = "Scelta 2";
            entry.ImageLocation = new Point(156, 139);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11]; // done
            entry.Name = "Scelta 3";
            entry.ImageLocation = new Point(156, 164);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            entry = entries[12]; // done
            entry.Name = "Scelta Gomme";
            entry.ImageLocation = new Point(0, 94);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13]; // done
            entry.Name = "Scelta Gomme 1";
            entry.ImageLocation = new Point(0, 114);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            entry = entries[14];
            entry.Name = "Scelta Gomme 2";
            entry.ImageLocation = new Point(0, 134);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;

            entry = entries[15];
            entry.Name = "Scelta Gomme 3";
            entry.ImageLocation = new Point(0, 154);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;

            entry = entries[16];
            entry.Name = "Scelta Gomme 4";
            entry.ImageLocation = new Point(0, 174);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;

            entry = entries[17];
            entry.Name = "Scelta Gomme 5";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;

            palItaVersion.Entries = entries;
            return palItaVersion;
        }

        static IconImgFile MakePalGerIconImgData()
        {
            IconImgFile palGerVersion = new IconImgFile();
            palGerVersion.Type = IconImgType.PALGer;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            entry.ImageLocation = new Point(214, 193);
            entry.ImageSize = iconSize;
            entry.Name = "Zum Rennen";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7d80;

            entry = entries[1];
            entry.Name = "Speil Status";
            entry.ImageLocation = new Point(214, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2];
            entry.Name = "Weltkarte";
            entry.ImageLocation = new Point(188, 193);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3];
            entry.Name = "TagText";
            entry.ImageLocation = new Point(214, 64);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4];
            entry.Name = "Auswahl 4";
            entry.ImageLocation = new Point(156, 168);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5];
            entry.Name = "Auswahl 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6]; // done
            entry.Name = "Auswahl";
            entry.ImageLocation = new Point(98, 193);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7];
            entry.Name = "Heim";
            entry.ImageLocation = new Point(214, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7e20;

            entry = entries[8];
            entry.Name = "Kauf";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(214, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9];
            entry.Name = "Auswahl 1";
            entry.ImageLocation = new Point(156, 93);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10];
            entry.Name = "Auswahl 2";
            entry.ImageLocation = new Point(156, 118);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11];
            entry.Name = "Auswahl 3";
            entry.ImageLocation = new Point(156, 143);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            entry = entries[12];
            entry.Name = "Reifenauswahl";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13];
            entry.Name = "Reifenauswahl 1";
            entry.ImageLocation = new Point(0, 93);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;

            entry = entries[14]; // done
            entry.Name = "Reifenauswahl 2";
            entry.ImageLocation = new Point(0, 113);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            entry = entries[15]; // done
            entry.Name = "Reifenauswahl 3";
            entry.ImageLocation = new Point(0, 133);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;

            entry = entries[16]; // done
            entry.Name = "Reifenauswahl 4";
            entry.ImageLocation = new Point(0, 153);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;

            entry = entries[17]; // done
            entry.Name = "Reifenauswahl 5";
            entry.ImageLocation = new Point(0, 173);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;

            palGerVersion.Entries = entries;
            return palGerVersion;
        }

        static IconImgFile MakePalFraIconImgData()
        {
            IconImgFile palFraVersion = new IconImgFile();
            palFraVersion.Type = IconImgType.PALFra;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            entry.ImageLocation = new Point(214, 193);
            entry.ImageSize = iconSize;
            entry.Name = "Com. Course";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7d80;

            entry = entries[1];
            entry.Name = "Etat Partie";
            entry.ImageLocation = new Point(214, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2];
            entry.Name = "Monde";
            entry.ImageLocation = new Point(188, 193);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3];
            entry.Name = "JourText";
            entry.ImageLocation = new Point(214, 64);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4];
            entry.Name = "Selection 4";
            entry.ImageLocation = new Point(156, 168);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5];
            entry.Name = "Selection 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6]; // done
            entry.Name = "Selection";
            entry.ImageLocation = new Point(98, 193);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7];
            entry.Name = "Accueil";
            entry.ImageLocation = new Point(214, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7e20;

            entry = entries[8];
            entry.Name = "Acheter";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(214, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9];
            entry.Name = "Selection 1";
            entry.ImageLocation = new Point(156, 93);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10];
            entry.Name = "Selection 2";
            entry.ImageLocation = new Point(156, 118);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11];
            entry.Name = "Selection 3";
            entry.ImageLocation = new Point(156, 143);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            entry = entries[12];
            entry.Name = "Selection des Roues";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13];
            entry.Name = "Selection des Roues 1";
            entry.ImageLocation = new Point(0, 93);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;

            entry = entries[14]; // done
            entry.Name = "Selection des Roues 2";
            entry.ImageLocation = new Point(0, 113);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            entry = entries[15]; // done
            entry.Name = "Selection des Roues 3";
            entry.ImageLocation = new Point(0, 133);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;

            entry = entries[16]; // done
            entry.Name = "Selection des Roues 4";
            entry.ImageLocation = new Point(0, 153);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;

            entry = entries[17]; // done
            entry.Name = "Selection des Roues 5";
            entry.ImageLocation = new Point(0, 173);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;

            palFraVersion.Entries = entries;
            return palFraVersion;
        }

        static IconImgFile MakePalEngIconImgData()
        {
            IconImgFile palEngVersion = new IconImgFile();
            palEngVersion.Type = IconImgType.PALEng;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            entry.ImageLocation = new Point(214, 193);
            entry.ImageSize = iconSize;
            entry.Name = "Go Race";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7d80;

            entry = entries[1];
            entry.Name = "Game Status";
            entry.ImageLocation = new Point(212, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2];
            entry.Name = "World Map";
            entry.ImageLocation = new Point(188, 193);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3];
            entry.Name = "DaysText";
            entry.ImageLocation = new Point(212, 64);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4];
            entry.Name = "Lineup 4";
            entry.ImageLocation = new Point(156, 168);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5];
            entry.Name = "Lineup 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6]; // done
            entry.Name = "Lineup";
            entry.ImageLocation = new Point(98, 193);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7];
            entry.Name = "Home";
            entry.ImageLocation = new Point(212, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7e20;

            entry = entries[8];
            entry.Name = "Buy";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(212, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9];
            entry.Name = "Lineup 1";
            entry.ImageLocation = new Point(156, 93);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10];
            entry.Name = "Lineup 2";
            entry.ImageLocation = new Point(156, 118);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11];
            entry.Name = "Lineup 3";
            entry.ImageLocation = new Point(156, 143);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            entry = entries[12];
            entry.Name = "Wheels Lineup";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13];
            entry.Name = "Wheels Lineup 1";
            entry.ImageLocation = new Point(0, 93);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;

            entry = entries[14]; // done
            entry.Name = "Wheels Lineup 2";
            entry.ImageLocation = new Point(0, 113);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            entry = entries[15]; // done
            entry.Name = "Wheels Lineup 3";
            entry.ImageLocation = new Point(0, 133);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;

            entry = entries[16]; // done
            entry.Name = "Wheels Lineup 4";
            entry.ImageLocation = new Point(0, 153);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;

            entry = entries[17]; // done
            entry.Name = "Wheels Lineup 5";
            entry.ImageLocation = new Point(0, 173);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;

            palEngVersion.Entries = entries;
            return palEngVersion;
        }

        static IconImgFile MakeJP11IconImgData()
        {
            IconImgFile jp11Version = new IconImgFile();
            jp11Version.Type = IconImgType.JP11;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            entry.ImageLocation = new Point(214, 193);
            entry.ImageSize = iconSize; // done
            entry.Name = "Go Race";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7e20;

            entry = entries[1]; // done
            entry.Name = "Game Status";
            entry.ImageLocation = new Point(212, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2]; // done
            entry.Name = "World Map";
            entry.ImageLocation = new Point(188, 193);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3]; // done
            entry.Name = "DaysText";
            entry.ImageLocation = new Point(212, 64);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4]; // done
            entry.Name = "Lineup 4";
            entry.ImageLocation = new Point(156, 168);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5]; // done
            entry.Name = "Lineup 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6]; // done
            entry.Name = "Lineup";
            entry.ImageLocation = new Point(98, 193);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7]; // done
            entry.Name = "Home";
            entry.ImageLocation = new Point(212, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7d80;

            entry = entries[8];
            entry.Name = "Buy"; // done
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(212, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9]; // done
            entry.Name = "Lineup 1";
            entry.ImageLocation = new Point(156, 93);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10]; // done
            entry.Name = "Lineup 2";
            entry.ImageLocation = new Point(156, 118);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11]; // done
            entry.Name = "Lineup 3";
            entry.ImageLocation = new Point(156, 143);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            // need to add entries for Wheels line up images
            entry = entries[12]; // done
            entry.Name = "Wheels Lineup";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7f00;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13]; // done
            entry.Name = "Wheels Lineup 1";
            entry.ImageLocation = new Point(0, 93);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7f20;

            entry = entries[14]; // done
            entry.Name = "Wheels Lineup 2";
            entry.ImageLocation = new Point(0, 113);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7f40;

            entry = entries[15]; // done
            entry.Name = "Wheels Lineup 3";
            entry.ImageLocation = new Point(0, 133);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7fa0;

            entry = entries[16]; // done
            entry.Name = "Wheels Lineup 4";
            entry.ImageLocation = new Point(0, 153);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7fc0;

            entry = entries[17]; // done
            entry.Name = "Wheels Lineup 5";
            entry.ImageLocation = new Point(0, 173);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7fe0;

            jp11Version.Entries = entries;
            return jp11Version;
        }

        static IconImgFile MakeJP10IconImgData()
        {
            IconImgFile jp10Version = new IconImgFile();
            jp10Version.Type = IconImgType.JP10;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[11];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0]; // done
            entry.ImageLocation = new Point(212, 32);
            entry.ImageSize = iconSize;
            entry.Name = "Go Race";
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[1]; // done
            entry.Name = "Game Status";
            entry.ImageLocation = new Point(196, 96);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7ec0;
            entry.Mod1 = 0x3f6e;

            entry = entries[2]; // done
            entry.Name = "World Map";
            entry.ImageLocation = new Point(212, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7f60;

            entry = entries[3]; // done
            entry.Name = "Lineup 4";
            entry.ImageLocation = new Point(98, 98);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;

            entry = entries[4]; // done
            entry.Name = "Lineup 5";
            entry.ImageLocation = new Point(0, 123);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;

            entry = entries[5]; // done
            entry.Name = "Lineup";
            entry.ImageLocation = new Point(98, 123);
            entry.ImageSize = new Size(90, 25);
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;

            entry = entries[6]; // done
            entry.Name = "Home";
            entry.ImageLocation = new Point(212, 64);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            entry = entries[7]; // done
            entry.Name = "Buy";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(212, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[8]; // done
            entry.Name = "Lineup 1";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;

            entry = entries[9]; // done
            entry.Name = "Lineup 2";
            entry.ImageLocation = new Point(98, 73);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;

            entry = entries[10]; // done
            entry.Name = "Lineup 3";
            entry.ImageLocation = new Point(0, 98);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            jp10Version.Entries = entries;
            return jp10Version;
        }

        static IconImgFile MakeUS12IconImgData()
        {
            IconImgFile us12Version = new IconImgFile();
            us12Version.Type = IconImgType.US12;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[18];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            entry.ImageLocation = new Point(214, 193);
            entry.ImageSize = iconSize; // done
            entry.Name = "Go Race";
            entry.Mod1 = 0x3eec;
            entry.PaletteLocation = 0x7e20;

            entry = entries[1]; // done
            entry.Name = "Game Status";
            entry.ImageLocation = new Point(212, 32);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7e40;
            entry.Mod1 = 0x3f2e;

            entry = entries[2]; // done
            entry.Name = "World Map";
            entry.ImageLocation = new Point(188, 193);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2f;
            entry.PaletteLocation = 0x7e60;

            entry = entries[3]; // done
            entry.Name = "DaysText";
            entry.ImageLocation = new Point(212, 64);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3eed;
            entry.PaletteLocation = 0x7da0;

            entry = entries[4]; // done
            entry.Name = "Lineup 4";
            entry.ImageLocation = new Point(156, 168);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[5]; // done
            entry.Name = "Lineup 5";
            entry.ImageLocation = new Point(0, 193);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;

            entry = entries[6]; // done
            entry.Name = "Lineup";
            entry.ImageLocation = new Point(98, 193);
            entry.ImageSize = new Size(90, 27);
            entry.Mod1 = 0x3f2c;
            entry.PaletteLocation = 0x7e00;

            entry = entries[7]; // done
            entry.Name = "Home";
            entry.ImageLocation = new Point(212, 0);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f2d;
            entry.PaletteLocation = 0x7d80;

            entry = entries[8];
            entry.Name = "Buy"; // done
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(212, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;

            entry = entries[9]; // done
            entry.Name = "Lineup 1";
            entry.ImageLocation = new Point(156, 93);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7ec0;

            entry = entries[10]; // done
            entry.Name = "Lineup 2";
            entry.ImageLocation = new Point(156, 118);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6c;
            entry.PaletteLocation = 0x7e80;

            entry = entries[11]; // done
            entry.Name = "Lineup 3";
            entry.ImageLocation = new Point(156, 143);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3f6d;
            entry.PaletteLocation = 0x7ea0;

            // need to add entries for Wheels line up images
            entry = entries[12]; // done
            entry.Name = "Wheels Lineup";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(158, 20);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7f00;

            Size wheelsSize = new Size(156, 20);
            entry = entries[13]; // done
            entry.Name = "Wheels Lineup 1";
            entry.ImageLocation = new Point(0, 93);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7f20;

            entry = entries[14]; // done
            entry.Name = "Wheels Lineup 2";
            entry.ImageLocation = new Point(0, 113);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7f40;

            entry = entries[15]; // done
            entry.Name = "Wheels Lineup 3";
            entry.ImageLocation = new Point(0, 133);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7fa0;

            entry = entries[16]; // done
            entry.Name = "Wheels Lineup 4";
            entry.ImageLocation = new Point(0, 153);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7fc0;

            entry = entries[17]; // done
            entry.Name = "Wheels Lineup 5";
            entry.ImageLocation = new Point(0, 173);
            entry.ImageSize = wheelsSize;
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7fe0;

            us12Version.Entries = entries;
            return us12Version;
        }

        static IconImgFile MakeUS10IconImgData()
        {
            IconImgFile us10Version = new IconImgFile();
            us10Version.Type = IconImgType.US10;
            Size iconSize = new Size(26, 32);
            IconImgEntry[] entries = new IconImgEntry[12];
            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new IconImgEntry();
                entries[i].Unk3 = 0xb; // this is the same for all
            }
            IconImgEntry entry = entries[0];
            // first entry is the go race flag image
            entry.ImageLocation = new Point(212, 0);
            entry.ImageSize = iconSize;
            entry.Name = "Go Race";
            entry.Mod1 = 0x3faf;
            entry.PaletteLocation = 0x7f60; // fieOffset, 16 colours in bgr555 (so 32 bytes)
            entry = entries[1];
            entry.Name = "Game Status";
            entry.ImageLocation = new Point(212, 64);
            entry.ImageSize = iconSize;
            entry.PaletteLocation = 0x7ea0;
            entry.Mod1 = 0x3f6d;
            entry = entries[2];
            entry.Name = "World Map";
            entry.ImageLocation = new Point(196, 96);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7ec0;
            entry = entries[3];
            entry.Name = "DaysText";
            entry.ImageLocation = new Point(222, 96);
            entry.ImageSize = new Size(24, 17);
            entry.Mod1 = 0x3f6f;
            entry.PaletteLocation = 0x7ee0;
            entry = entries[4];
            entry.Name = "Lineup 4";
            entry.ImageLocation = new Point(98, 98);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fac;
            entry.PaletteLocation = 0x7f00;
            entry = entries[5];
            entry.Name = "Lineup 5";
            entry.ImageLocation = new Point(0, 123);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fad;
            entry.PaletteLocation = 0x7f20;
            entry = entries[6];
            entry.Name = "Lineup";
            entry.ImageLocation = new Point(98, 123);
            entry.ImageSize = new Size(90, 25);
            entry.Mod1 = 0x3fae;
            entry.PaletteLocation = 0x7f40;
            entry = entries[7];
            entry.Name = "Home";
            entry.ImageLocation = new Point(212, 32);
            entry.ImageSize = iconSize;
            entry.Mod1 = 0x3f6e;
            entry.PaletteLocation = 0x7e80;
            entry = entries[8];
            entry.Name = "Buy";
            entry.ImageLocation = Point.Empty;
            entry.ImageSize = new Size(212, 73);
            entry.Mod1 = 0x3fec;
            entry.PaletteLocation = 0x7f80;
            entry = entries[9];
            entry.Name = "Lineup 1";
            entry.ImageLocation = new Point(0, 73);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fed;
            entry.PaletteLocation = 0x7fa0;
            entry = entries[10];
            entry.Name = "Lineup 2";
            entry.ImageLocation = new Point(98, 73);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fee;
            entry.PaletteLocation = 0x7fc0;
            entry = entries[11];
            entry.Name = "Lineup 3";
            entry.ImageLocation = new Point(0, 98);
            entry.ImageSize = new Size(98, 25);
            entry.Mod1 = 0x3fef;
            entry.PaletteLocation = 0x7fe0;

            us10Version.Entries = entries;
            return us10Version;
        }
    }
}
