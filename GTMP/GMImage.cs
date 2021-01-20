using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using GT2;

// this file has the code to display a decompressed GM "menu" gt2 image file. 
// These are the ones in the \gtmenu\<lang>\gtmenudat.dat.
// That file needs exploding into its constituent gzip archives and the archives need decompressing
// before being dragged here

namespace GTMP
{
    // Gm file format
    //    byte[4] sig; // GM\x3\x0
    //    uint subBlockSets; // number of sets of icons/boxes to draw or define
    //    for i = 0 to subBlockSets
    //        ushort numIconImageBoxes
    //        ushort numInfoBoxes;
    //        for j = 0 to numIconImageBoxes // 0xc in size
    //            struct IconInfoBox (see code below) // defines box that contains an image drawn from iconimg.dat
    //        end for
    //        for j = 0 to numInfoBoxes // 0x4c in size
    //            struct InfoBox // defines boxes that contain runtime info like displayed race info, current car etc
    //        end for
    //    end for
    //    uint numMainInfoBoxes
    //    for i = 0 to numMainInfoBoxes
    //        struct InfoBox // same struct as above
    //    end for
    //    sbyte manufacturerID;
    //    byte screenBehaviour; // is set to 2 on car rendering sceens, and 3 that need help to know where to go when pressing triangle
    //    short screenType // no idea what importance this has, getting it 'wrong' doesn't seem to matter, but the values deffo are those in the ScreenType enum
    //    short triangleLink; // the index of the GM screen you go to if you press triangle or square
    //    short zero;
    //    int backgroundGMFile; // the 0-based index of the background image for this top-layer in commonpic.dat (0 = first image, 1 = second, etc)
    //    byte[4] gmllString; // "GMLL"
    //    int numOfPositionEntries;
    //    int[numOfPositionEntries] positionEntries; // variable sized array of position entries, same as those in the GTMP files
    //    short[16][16] palettes; // 16 palettes of 16 BGR555 colours
    //    byte[] padding; // until 0x404 bytes after the start of the palettes
    //    byte[] pixels; // to the end of the file, 4bpp meaning two pixels per byte
    // }
    static class GMFile
    {
        //private static List<ImageSlice> ParseSlices(byte[] imageData, int numTiles)
        //{
        //    // tiles are 16 x 8 but 4bpp, so each byte is 2 pixels, so 8x8 in bytes
        //    // stride is always 512 bytes
        //    List<ImageSlice> slices = new List<ImageSlice>();
        //    int numBytes = imageData.Length;
        //    const int stride = 256; // in bytes, not pixels
        //    const int xTiles = stride / 8;
        //    int yTiles = numBytes / stride / 8;
        //    int curTiles = 0;
        //    for (int yTile = 0; yTile < yTiles; ++yTile)
        //    {
        //        int yPos = yTile * 8;
        //        for (int xTile = 0; (xTile < xTiles) && (curTiles < numTiles); xTile++, ++curTiles)
        //        {
        //            int xPos = xTile * 8; // also used as byte index
        //            ImageSlice s = new ImageSlice();
        //            for (int localIter = 0; localIter < (16 * 8); localIter += 2)
        //            {
        //                int localX = localIter % 16;
        //                int localY = localIter / 16;
        //                Debug.Assert((localY >= 0) && (localY < 8));
        //                int yPixOffset = (yPos + localY) * stride;
        //                byte twoPix = imageData[yPixOffset + xPos + (localX / 2)];
        //                s.rect[((localY * 16) + localX) + 1] = (byte)((twoPix & 0xF0) >> 4);
        //                s.rect[((localY * 16) + localX)] = (byte)(twoPix & 0xF);
        //            }
        //            slices.Add(s);
        //        }
        //    }
        //    return slices;
        //}

        private static List<ImageSlice> ParseSlices(byte[] imageData, int numTiles)
        {
            // tiles are 16 x 8 but 4bpp, so each byte is 2 pixels, so 8x8 in bytes
            // stride is always 512 bytes
            List<ImageSlice> slices = new List<ImageSlice>();
            const int stride = 512;
            int xPixel = 0;
            int yPixel = 0;

            for (int i = 0; i < numTiles; ++i)
            {
                if (xPixel == stride)
                {
                    xPixel = 0;
                    yPixel += 8;
                }
                byte[] tile = new byte[16 * 8];
                int tileY = 0;
                // +8 because there are 8 rows of 16 pixels per tile
                for (int y = yPixel; y < (yPixel + 8); ++y, ++tileY)
                {
                    // (/ 2) because each byte represents 2 pixels
                    int imageTileRow = ((y * stride) + xPixel) / 2;
                    int tileX = 0;
                    int tileRow = 16 * tileY;
                    // we deal with pixels per byte, so x < 8 rather than < 16
                    // and tileX += 2 for the same reason
                    for (int x = 0; x < 8; ++x, tileX += 2)
                    {
                        byte twoPixels = imageData[imageTileRow + x];
                        tile[tileRow + tileX] = (byte)(twoPixels & 0xf);
                        tile[tileRow + tileX + 1] = (byte)((twoPixels >> 4) & 0xf);
                    }
                }
                xPixel += 16;
                slices.Add(new ImageSlice(tile));
            }
            return slices;
        }

        public static List<GT2.PositionData> ParsePositionData(byte[] positionData, int maxDataSize)
        {
            // max of 0x1f80 bytes of position data
            // 4 is the size of the pos struct
            //struct TileInfo
            //{
            //	unsigned char x; // * 16 for pixel pos
            //	unsigned char y; // * 8 for pixel pos
            //	unsigned short tileIndex; // & 0x0FFF = which tile, & 0xF000 >> 12 = which palette
            //};
            int maxNumOfDataStructs = maxDataSize / 4;
            List<GT2.PositionData> pdList = new List<GT2.PositionData>();
            int posDataIter = 0;
            //List<ushort> rawColours = new List<ushort>();
            while (maxNumOfDataStructs > 0)
            {
                // these non-background pictures have 32 palettes instead of 16
                const ushort paletteMask = 0xF800;
                GT2.PositionData pd = new GT2.PositionData();
                byte x = positionData[posDataIter++];
                byte y = positionData[posDataIter++];
                // the x value is technically two bits of data
                // the low 5 bits are the x coordinate
                // the upper three (really just the high bit) are a flag
                // if this flag is set, then the tile index is actually a BGR555 solid colour
                // and is used for all 16 x 8 pixels instead of a tile
                byte xPos = (byte)(x & 0x1F);
                // apparently no files in the PAL version actually use the 
                // solid colour capability?
                byte isSolidColourFlag = (byte)((x & 0xE0) >> 5);
                // turn these into pixel coordinates instead of tile coordinates
                // to make it easier later
                pd.x = xPos * 16;
                pd.y = y * 8;
                ushort tileAndPal = BitConverter.ToUInt16(positionData, posDataIter);
                posDataIter += 2;
                if (isSolidColourFlag == 0)
                {
                    pd.tile = (ushort)(tileAndPal & 0x03FF);
                    pd.palette = (byte)((tileAndPal & paletteMask) >> 11);
                }
                else
                {
                    pd.tile = GT2.Palette.GMSwizzleColour(tileAndPal);
#if PRINT_PALETTES
                    Console.WriteLine("Solid tile of colour {0:x}=>{1:x} at {2}x{3}", tileAndPal, pd.tile, pd.x, pd.y);
#endif
                    pd.palette = 0xFF;
                }
                pdList.Add(pd);
                --maxNumOfDataStructs;
            }
            return pdList;
        }

        public enum BoxItem : ushort
        {
            None = 0,
            ClickButton = 1, // makes clickable button regardless of attrbutes?
            YesBuyButton = 2, // the confirm buy button 
            NoDontBuyButton = 3, // the reject buy button
            PrePurchaseCarButton = 4, // the buy buttno that goes to the 'Purchase? Yes-No' screen

            // show info for the current car in garage or dealership (width, height etc)
            // clicking seems to have special peroperty of causing the game to remembering 
            // which screen it came from so that triangle goes back to it
            // regardless of whether the BackLink metadata of the linked screen or the BackLinkToPrevious flag
            // are set
            CarInfoButton = 5,
            // the boxes you get after clicking New in a dealership.
            // Prints the price of a car in black UNDERNEATH the box in the centre
            // needs a car id passed in the extra data
            // if a link is also present, that comes first before the car id
            DealershipNewCarBox = 6,
            CarColourName = 7, // The name of the car colour

            GarageCarList = 9, // the list of cars in garage, hardcoded location
            CarDisplay = 0xa, // the spinning car display (like in the garage and buying screens)
            ExitToMainMenuButton = 0xb, // goes to the main menu when clicked (where Start Game, Load Game, Options are)

            DoCarWashButton = 0xd, // Does a car wash

            OKButton = 0x10, // The button on the 'you bought it' screen after buying a thing or in an error message screen
            CarLogo = 0x11, // the big central car logo like in the purchase screen, centre aligned
            ReturnToPreviousScreenButton = 0x12, // used for the back button on the garage car screen
            CarFlavourTextButton = 0x13, // goes to the speech about each car

            // Item Buy buttons - these are probably separate because their presence on a screen tells
            // the price and hp upgrade info fields which part they should hold info about
            ActiveStabilityControllerBuyButton = 0x14,
            SportsBrakesBuyButton = 0x15,
            BrakeBalanceControllerBuyButton = 0x16,
            SingleClutchBuyButton = 0x17,
            TwinClutchBuyButton = 0x18,
            TripleClutchBuyButton = 0x19,
            SportsRomBuyButton = 0x1a,
            DisplacementIncreaseBuyButton = 0x1b,

            EngineBalancingBuyButton = 0x1d,
            SportsFlywheelBuyButton = 0x1e,
            SemiRacingFlywheelBuyButton = 0x1f,
            RacingFlywheelBuyButton = 0x20,
            SportsTransmissionBuyButton = 0x21, // close transmission
            SemiRacingTransmissionBuyButton = 0x22, // super close transmission
            TransmissionFullCustomizeBuyButton = 0x23,
            SportsIntercoolerBuyButton = 0x24,
            RacingIntercoolerBuyButton = 0x25,
            WeightReduction1BuyButton = 0x26,
            WeightReduction2BuyButton = 0x27,
            WeightReduction3BuyButton = 0x28,
            LSD1WayBuyButton = 0x29,
            LSD2WayBuyButton = 0x2a,
            LSD15WayBuyButton = 0x2b,
            LSDFullCustBuyButton = 0x2c,
            YAWControlBuyButton = 0x2d,
            SportsMufflerBuyButton = 0x2e,
            SemiRacingMufflerBuyButton = 0x2f,
            RacingMufflerBuyButton = 0x30,
            NAStage1BuyButton = 0x31,
            NAStage2BuyButton = 0x32,
            NAStage3BuyButton = 0x33,
            PortPolishBuyButton = 0x34,
            DriveshaftBuyButton = 0x35,
            RacingModificationBuyButton = 0x36,
            SportsSuspensionBuyButton = 0x37,
            SemiRacingSuspensionBuyButton = 0x38,
            SuspensionFullCustomizationBuyButton = 0x39,
            TractionControlBuyButton = 0x3a,
            SportsTyresBuyButton = 0x3b,
            RacingHardTyresBuyButton = 0x3c,
            RacingMediumTyresBuyButton = 0x3d,
            RacingSoftTyresBuyButton = 0x3e,
            RacingSuperSoftTyresBuyButton = 0x3f,
            SimulationTyresBuyButton = 0x40,
            DirtTyresBuyButton = 0x41,
            TurboStage1BuyButton = 0x42,
            TurboStage2BuyButton = 0x43,
            TurboStage3BuyButton = 0x44,
            TurboStage4BuyButton = 0x45,

            CurrentCarShortName = 0x47, // the active car's short name, the one used at the bottom of every screen
            CurrentCarHP = 0x48, // orange, the car hp before the power upgrade is applied.
            UpgradeCarHP = 0x49, // orange, what the car hp will be after the power upgrade is applied.
            CarHP = 0x4a, // the HP shown on a garage or dealership screen

            CarWeight = 0x4c, // the weight (kg/lbs) shown on a garage or dealership screen
            CreditsBalance = 0x4d, // the credit number shown at the bottom of every screen
            Price = 0x4e, // car/wheeel/upgrade etc, defaults to bottom right of box
            DayCount = 0x4f, // the current game day, NUMBER ONLY, the Days text is a separate icon img
            UsedCarList = 0x50, // The list of used cars, I don't think this explanation needed to be written

            // License test results, will show nothing, kids prize, or a coloured trophy
            // note that you show these results with these box contents, rather than with
            // a RaceMode BehaviourAttribute with a RaceResult QueryAttribute
            IC1LicenseResult = 0x53,
            IC2LicenseResult,
            IC3LicenseResult,
            IC4LicenseResult,
            IC5LicenseResult,
            IC6LicenseResult,
            IC7LicenseResult,
            IC8LicenseResult,
            IC9LicenseResult,
            IC10LicenseResult,
            // gap at 0x5d
            B1LicenseResult = 0x5e,
            B2LicenseResult,
            B3LicenseResult,
            B4LicenseResult,
            B5LicenseResult,
            B6LicenseResult,
            B7LicenseResult,
            B8LicenseResult,
            B9LicenseResult,
            B10LicenseResult,
            // Gap at 0x68
            A1LicenseResult = 0x69,
            A2LicenseResult,
            A3LicenseResult,
            A4LicenseResult,
            A5LicenseResult,
            A6LicenseResult,
            A7LicenseResult,
            A8LicenseResult,
            A9LicenseResult,
            A10LicenseResult,
            // gap at 0x73
            IB1LicenseResult = 0x74,
            IB2LicenseResult,
            IB3LicenseResult,
            IB4LicenseResult,
            IB5LicenseResult,
            IB6LicenseResult,
            IB7LicenseResult,
            IB8LicenseResult,
            IB9LicenseResult,
            IB10LicenseResult,
            // gap at 0x7e
            IA1LicenseResult = 0x7f,
            IA2LicenseResult,
            IA3LicenseResult,
            IA4LicenseResult,
            IA5LicenseResult,
            IA6LicenseResult,
            IA7LicenseResult,
            IA8LicenseResult,
            IA9LicenseResult,
            IA10LicenseResult,
            // gap at 0x89
            S1LicenseResult = 0x8a,
            S2LicenseResult,
            S3LicenseResult,
            S4LicenseResult,
            S5LicenseResult,
            S6LicenseResult,
            S7LicenseResult,
            S8LicenseResult,
            S9LicenseResult,
            S10LicenseResult,
            CarColourSwatches = 0x94, // The small coloured rects that show the car colours when buying a new car

            CarColourChangeButton = 0x96, // the button which changes the car's colours and cycles through the swatches
            CarDrivetrainGraphic = 0x97, // The 4WD, FF, FR graphic. central, data for drawing these at 0x508d0 in US1.0 (5 structs of 0xc size, one for each graphic)
            // Checks the car is the correct manufacturer before entering, which is why it's a special value (probably)
            // the manufacturer ID is after all the box definitions and before the GMLL stuff
            DealershipTuneButton = 0x98,

            CarYear = 0x9a, // the model year in the dealership/garage
            UpgradeName = 0x9b, // The name of the currently active car part / last part that you saw a Buy button for - Port & Polish, Racing Muffler etc
            ShowRacingModificationButton = 0x9c, // The button that goes to the 'Buy Racing Modification' screen. Checks the car has one before entering the linked screen, or errors otherwise
            ChangeModificationTypeButton = 0x9d, // changes the racing mod type. If car doesn't have an alternative racing mod, the entire box is rendered black and made unclickable and uncursorable

            SellCarButton = 0xaa, // the button in the garage that sells the current car
            GetInCarButton = 0xab, // the button in the garage that changes the car you're in

            // these are items on the first screen/world map.
            // No idea why they're special contents values
            // they don't render anything nor seem to do anything special
            WorldMapCarWashButton = 0xad,
            WorldMapWheelShopButton = 0xae,
            WorldMapGoRaceButton = 0xaf,
            WorldMapLicensesButton = 0xb0,

            // all the bits that make up the car info screen
            // (the screen after you click the car button in the dealership/garage)
            InfoScreenCarLength = 0xb2, // bottom left
            InfoScreenCarHeight = 0xb3, // bottom left
            InfoScreenCarWidth = 0xb4, // bottom left
            InfoScreenCarWeight = 0xb5, // b l
            InfoScreenCarEngineCapacity = 0xb6, // the displacement/CC value, b l
            InfoScreenCarDrivetrain = 0xb7, // b l
            InfoScreenCarEngineType = 0xb8, // I4 DOHC etc, b l
            InfoScreenCarMaxPower = 0xb9, // 154hp / 6000rpm txt etc, b l
            InfoScreenCarMaxTorque = 0xba, // 138.1lb-ft / 3500rpm txt etc, b l

            // Bits of info on the Game Status screen
            StatusPercentageCompletion = 0xbc, // b r
            StatusTotalPrizeMoney = 0xbd, // b r
            StatusTotalRaces = 0xbe, // b r
            StatusTotalWins = 0xbf, // b r
            StatusWinPercentage = 0xc0, // b r
            StatusAvgRanking = 0xc1, // b r
            StatusCarsOwned = 0xc2, // b r
            StatusTotalCarValue = 0xc3, // b r
            BestMaxSpeedTest = 0xc4, // (not used by the game, Formatted as FullCarName - 121.0mph etc. Only if have done a max speed test)

            // This is car with the highest 2-byte value at 0x98 into the garage car data
            // Top two bits are ignored (value & 0x3fff) (because they're flags) the rest of the 
            // value is compared. 
            // The car printed by this box is the car with the highest of these values
            // It seems to be the PS power value of the cars. The HP Value printed by this is the this
            // PS value mutiplied by 0.985 
            HighestPoweredCarInGarage = 0xc5, // (not used by the game) 'Elise 190 184hp' etc, b r
            Best400MTimeTrial = 0xc6, // (not used by the game, Formatted as FullCarName - 0'22.115 etc. Only if have done a 400m test)

            // More bits ofrom the game status screen
            StatusNumberOfLicenseGoldTrophies = 0xc8, // b r
            StatusNumberOfLicenseSilverTrophies = 0xc9, // b r
            StatusNumberOfLicenseBronzeTrophies = 0xca, // b r
            StatusNumberOfLicenseKidsTrophies = 0xcb, // b r
            StatusHighestGainedLicense = 0xcc,
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
            CarEquippedPartsList = 0xcd, // the Equipped Car Parts list, hardcoded screen location

            ChampionshipBonusPrize = 0xcf, // Displays the championship bonus of the championship named in the extradata
            CarSaleValue = 0xd0, // The sale price of the car listed in the garage
            // 0xd0 is the hardcoded highest (at least for those in the render entry table at 80023D00 (US1.0))
            // so we haven't missed any above here
            [System.ComponentModel.Browsable(false)]
            LastBoxItemValue = 0xd0
        }

        // IconImg.dat palettes start at 0x7e80 - BGR555
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0xc)]
        public class IconImageBox // 0xc in size. Used to draw items with predefined graphics in icomimg.dat
        {
            public ushort screenX; // screen 
            public ushort screenY;
            public byte imgMapX;
            public byte imgMapY;
            public byte width;
            public byte height;
            public short unk3; // tiling flags? (goes up to 0x1f)
            public byte drawModifier1; // these two change how the image is drawn
            public byte drawModifier2; // Could be draw/transparency/saturation amount? two byte BGR555 invert colour?
        }

        // Values 1, 2, and 8 aren't used in any game files
        // so what they do, if anything, is unknown
        [Flags]
        public enum QueryAttributes : byte
        {
            /// <summary>
            /// No special query
            /// </summary>
            None = 0,

            // If contents is 0/None, displays a right pointing next arrow
            // the condition is defined by byte 0x3e in the extra data
            // 0 - HasIALicense, 1 = hasSuperLicense
            // these are hardcoded into game logic,
            /// <summary>
            /// Will conditionally show a clickable arrow depending on if the IA or S license is achieved
            /// Only works if box content is none
            /// </summary>
            ShowArrowIf = 0x4,

            /// <summary>
            /// Displays the license required for the race defined in racename
            /// </summary>
            LicenseRequiredGraphic = 0x10, // centre aligned
            /// <summary>
            /// Displays the HP requirement for the race defined in racename
            /// </summary>
            PowerRestriction = 0x20, // bottom left aligned
            // right aligned. Last ushort of extraData determines which place prize money is displayed
            // value must be 1-6 for those places, 0 = blank
            /// <summary>
            /// Displays the prize money for finishing in the position for the race defined in the racename
            /// </summary>
            PrizeMoney = 0x40, // bottom right 
            /// <summary>
            /// Displays the best result achieved for the race defined in the racename
            /// </summary>
            RaceResult = 0x80, // bottom left, Best result achieved in this race - gold, silver, bronze, 4, 5, 6
            [System.ComponentModel.Browsable(false)]
            AllTypes = LicenseRequiredGraphic | PowerRestriction | PrizeMoney | RaceResult | ShowArrowIf
        }

        public enum ShowArrowIfLicense : sbyte
        {
            None = -1,
            HasIALicense = 0,
            HasSLicense = 1
        }

        [Flags]
        public enum InfoBoxAttributes : byte
        {
            None = 0,
            CanCursor = 1, // can move cursor here with d-pad
            DefaultCursorPos = 2, // cursor goes here on screen entry

            // This doesn't seem to do anything
            // ProbeCarInfo = 4, // the extradata has a car id in addition to a link

            RaceMode = 8, // extra data is a race name to query for data

            // this button changes car wheels, the wheel to fit is named in the extradata, like for races
            FitWheel = 0x10,

            DontRenderDealerPrice = 0x20, // only used for DealershipCarBoxes, if set, price doesn't render

            // If ProbeRaceInfo is set, this causes the race preview of the race in the extra data
            // to happen rather than entering the race as a player
            ShowRacePreview = 0x40,

            UseBigFont = 0x80 // draw the text in a bigger font
        }

        // Not sure what the purpose of this is, but this seems to 
        // be what this part of the metadata means
        public enum ScreenType : ushort
        {
            CarWash = 0,
            EastCityEvent = 1,
            NorthCityEvent = 2,
            SouthCityEvent = 3,
            WestCityEvent = 4,
            GoRace = 5,
            Home = 6,
            WorldMap = 7,
            License = 8,
            Other = 0xff
        }

        public enum ManufacturerId : sbyte
        {
            None = -1,
            Acura = 0,
            AlfaRomeo,
            AstonMartin,
            Audi,
            BMW,
            Chevrolet,
            Citroen,
            Daihatsu,
            Dodge,
            Fiat,
            Ford,
            Honda,
            Jaguar = 13,
            Lancia,
            Lister = 16,
            Lotus,
            Mazda,
            Mercedes,
            MiniMGF = 21,
            Mitsubishi,
            Nissan,
            Opel,
            Peugeot,
            Plymouth,
            Renault,
            RUF,
            Shelby,
            Subaru,
            Suzuki,
            TommyKaira,
            Toyota,
            TVR,
            Vauxhall,
            Vector,
            Venturi,
            Volkswagen
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x4c, CharSet = CharSet.Ansi)]
        public class InfoBox
        {
            // bounding box
            public ushort x1;
            public ushort y1;
            public ushort x2;
            public ushort y2;
            public BoxItem contents; // what to render
            public QueryAttributes queryType; // if attributes defined a RaceMode, this defines what that does
            public InfoBoxAttributes attributes;
            // this extraData is many things depending on the attributes, like parameters
            // so when 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4c - 0xc)]
            public byte[] extraData;

            public int GetScreenLink()
            {
                if (
                    ((attributes & (InfoBoxAttributes.FitWheel | InfoBoxAttributes.RaceMode)) != 0) ||
                    (contents == BoxItem.ChampionshipBonusPrize)
                    )
                {
                    return -1;
                }
                return BitConverter.ToInt32(extraData, 0);
            }

            public sbyte GetSpecificPlaceNumber()
            {
                if ((attributes & InfoBoxAttributes.RaceMode) != 0)
                {
                    if ((queryType & QueryAttributes.PrizeMoney) != 0)
                    {
                        return (sbyte)extraData[extraData.Length - 1];
                    }
                }
                return (sbyte)-1;
            }

            public string GetWheelRaceOrCarIdName()
            {
                string name = String.Empty;
                // these are the only ones that needa car id
                if (contents == BoxItem.DealershipNewCarBox)
                {
                    uint carId = BitConverter.ToUInt32(extraData, 4);
                    name = carId.ToString("X");
                }
                // not allowed a screenlink with these attributes
                else if ((attributes & (InfoBoxAttributes.FitWheel | InfoBoxAttributes.RaceMode)) != 0)
                {
                    int lastCharIndex = Array.FindIndex(extraData,
                        (x) =>
                        {
                            return !(Char.IsLetterOrDigit((char)x) || (char)x == '-');
                        }
                    );
                    name = Encoding.ASCII.GetString(extraData, 0, lastCharIndex).TrimEnd('\0');
                }
                return name;
            }

            public ShowArrowIfLicense GetArrowEnablingLicense()
            {
                if ((contents == BoxItem.None) && ((queryType & QueryAttributes.ShowArrowIf) != 0))
                {
                    return (ShowArrowIfLicense)extraData[extraData.Length - 2];
                }
                else return ShowArrowIfLicense.None;
            }

            public override string ToString()
            {
                string specificInfo;
                if ((attributes & InfoBoxAttributes.RaceMode) != 0)
                {
                    string raceName = GetWheelRaceOrCarIdName();
                    string type;
                    if ((queryType == QueryAttributes.None) && (attributes & InfoBoxAttributes.ShowRacePreview) != 0)
                    {
                        type = "GoToPreview";
                    }
                    else
                    {
                        type = queryType.ToString();
                    }
                    short race = GetSpecificPlaceNumber();
                    // no place when querying prize money = championsip bonus
                    string specificInfoNoPlaceFormat = "{0} for {1}";
                    string specificInfoPlaceFormat = "{0} for {1} ({2} place)";
                    string formatToUse = (race > 0) ? specificInfoPlaceFormat : specificInfoNoPlaceFormat;
                    specificInfo = String.Format(formatToUse, type, raceName, race);
                }
                else
                {
                    int linkScreen = GetScreenLink();
                    specificInfo = String.Format("Link to: {0}/0x{0:x}", linkScreen);
                }
                return String.Format(
                    "{0}, Attrs: 0x{1:x}({1}), Contents: 0x{6:x}({6}), QueryType: 0x{7:x}({7}), ({2},{3})-({4},{5})",
                    specificInfo,
                    attributes,
                    x1,
                    y1,
                    x2,
                    y2,
                    contents,
                    queryType
                );
            }
        }

        internal class DrawRectInfo
        {
            public InfoBox infoBox;
            public IconImageBox iconImgBox;
            public Rectangle rect;
            public Pen rectDrawColour;
            public DrawRectInfo(InfoBox cpIn, Pen rectColour)
            {
                infoBox = cpIn;
                rect = new Rectangle(cpIn.x1, cpIn.y1, cpIn.x2 - cpIn.x1, cpIn.y2 - cpIn.y1);
                rectDrawColour = rectColour;
            }

            public DrawRectInfo(IconImageBox sbIn, Pen rectColour)
            {
                iconImgBox = sbIn;
                rect = new Rectangle(sbIn.screenX, sbIn.screenY, sbIn.width, sbIn.height);
                rectDrawColour = rectColour;
            }

            public override string ToString()
            {
                return String.Format("{0},{1}-{2},{3}", rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }

        public class GMMetadata
        {
            public GMMetadata()
            {
                BackLink = -1;
                ManufacturerID = ManufacturerId.None;
                BackgroundIndex = -1;
                BackLinkToPreviousScreen = false;
                ScreenType = ScreenType.Other;
            }

            [System.ComponentModel.Category("File Metadata")]
            [System.ComponentModel.Description("0-based GM/gtmenudat screen index to go to on triangle/square press, -1 to disable")]
            public short BackLink
            {
                get;
                set;
            }
            [System.ComponentModel.Category("File Metadata")]
            [System.ComponentModel.Description("Must be in a car from this manufacturer id to use any DealershipTuneButtons on the screen, -1 for none")]
            public ManufacturerId ManufacturerID
            {
                get;
                set;
            }
            [System.ComponentModel.Category("File Metadata")]
            [System.ComponentModel.Description("0-based index of the GTMP file in commonpic.dat to use as the background to this foreground")]
            public short BackgroundIndex
            {
                get;
                set;
            }

            [System.ComponentModel.Category("File Metadata")]
            [System.ComponentModel.Description("Only of importance is BackLink is -1. If this is true, Triangle will go to the screen you came to this one from. If false, Triangle won't do anything")]
            public bool BackLinkToPreviousScreen
            {
                get;
                set;
            }

            [System.ComponentModel.Category("File Metadata")]
            [System.ComponentModel.Description("Type of screen")]
            public ScreenType ScreenType
            {
                get;
                set;
            }

            public override string ToString()
            {
                return String.Format(
                    "Metadata: BackLink = {0}, ManuID = {1}, BGIndex = {2}, BLToPrev = {3}, ScreenType = {4}",
                    BackLink,
                    ManufacturerID,
                    BackgroundIndex,
                    BackLinkToPreviousScreen,
                    ScreenType
                );
            }
        }

        internal class GMFileInfo
        {
            public GMFileInfo()
                : this(null, null, null)
            {
            }

            public GMFileInfo(Bitmap bm, List<DrawRectInfo> rects, GMMetadata meta)
            {
                Image = bm;
                Boxes = rects;
                Metadata = meta;
            }

            public Bitmap Image
            {
                get;
                private set;
            }

            public List<DrawRectInfo> Boxes
            {
                get;
                private set;
            }

            public GMMetadata Metadata
            {
                get;
                private set;
            }
        }

        static int ParseSimpleBoxes(int numBoxes, List<DrawRectInfo> rects, long pFileData, Pen rectColour, StreamWriter sw, ref int byteIter)
        {
            int isInteresting = 0;
            Type simpleBoxType = typeof(IconImageBox);
            int simpleBoxSize = Marshal.SizeOf(simpleBoxType);
            int numPreviousBoxes = rects.Count;
            for (int i = numPreviousBoxes; i < numBoxes + numPreviousBoxes; ++i, byteIter += simpleBoxSize)
            {
                IntPtr pSimpleBox = new IntPtr(pFileData + byteIter);
                IconImageBox box = (IconImageBox)Marshal.PtrToStructure(pSimpleBox, simpleBoxType);
                DrawRectInfo dri = new DrawRectInfo(box, rectColour);
                rects.Add(dri);
                Debug.Assert(box.unk3 == 0xb);
#if !GMCREATOR
                sw.WriteLine(
                    "Index: {0}, ({1},{2})-({3},{4}) ImgMap: {5},{6} Unk3: 0x{7:x}, Mod1: {8:x}, Mod2: {9:x}",
                    i,
                    dri.rect.Left,
                    dri.rect.Top,
                    dri.rect.Right,
                    dri.rect.Bottom,
                    box.imgMapX,
                    box.imgMapY,
                    box.unk3,
                    box.drawModifier1,
                    box.drawModifier2
                );
                if((box.imgMapX == 0) && (box.imgMapY >= 192))
                {
                    isInteresting = 1;
                }
#endif
            }
            return isInteresting;
        }

        static int ParseInfoBoxes(uint numBoxes, List<DrawRectInfo> rects, long pFileData, Pen rectColour, StreamWriter sw, ref int byteIter)
        {
            Type infoBoxType = typeof(InfoBox);
            int infoBoxSize = Marshal.SizeOf(infoBoxType);
            int hadInterestingAttribute = 0;
            Type boxItemEnum = typeof(BoxItem);
            int numPreviousBoxes = rects.Count;
            for (int index = numPreviousBoxes; index < numBoxes + numPreviousBoxes; ++index)
            {
                IntPtr pInfoBox = new IntPtr(pFileData + byteIter);
                InfoBox cp = (InfoBox)Marshal.PtrToStructure(pInfoBox, infoBoxType);
                DrawRectInfo cri = new DrawRectInfo(cp, rectColour);
                rects.Add(cri);
#if !GMCREATOR
                sw.WriteLine(
                    "Index: {0}, {1}",
                    index,
                    cp
                );
                byte[] unk3 = cp.extraData;
                int numUnk3 = unk3.Length;
                for (int byteNum = 0; byteNum < numUnk3; ++byteNum)
                {
                    if ((byteNum > 0) && ((byteNum % 16) == 0))
                    {
                        sw.WriteLine();
                    }
                    sw.Write("{0:X2} ", unk3[byteNum]);
                }
                sw.WriteLine();
#endif
                byteIter += infoBoxSize;
            }
            return hadInterestingAttribute;
        }

        static List<DrawRectInfo> ParsePrePixelData(string fileName, byte[] fileData, out int byteIterAfter, out GMMetadata metadata)
        {
            GCHandle pinnedFileData = GCHandle.Alloc(fileData, GCHandleType.Pinned);
            long pFileData = pinnedFileData.AddrOfPinnedObject().ToInt64();
            List<DrawRectInfo> rects = new List<DrawRectInfo>();
            // number of 0xc byte blocks before the main 0x4c sized blocks
            int byteIter = 4;
            int blockGroups = BitConverter.ToInt32(fileData, byteIter); // this can be 0, but is 1 based if there are more than 1
            byteIter += 4;
            Pen[] rectGroupColours = new Pen[4] { Pens.YellowGreen, Pens.Red, Pens.Blue, Pens.Yellow };
            int interestingAttributes = 0;
#if !GMCREATOR
            string outputLogName = String.Format("T:\\attrs\\{0}.txt", fileName);
#if PRINTALLBOXES
            using (StreamWriter log = new StreamWriter(outputLogName, false, Encoding.UTF8))
#else
            using (StreamWriter log = new StreamWriter(new MemoryStream(5000), Encoding.UTF8))
#endif
#else // GMCREATOR
            StreamWriter log = null;
#endif
            {
                for (int i = 0; i < blockGroups; ++i)
                {
                    ushort numSimpleBoxes = BitConverter.ToUInt16(fileData, byteIter);
                    byteIter += 2;
                    ushort numInfoBoxes = BitConverter.ToUInt16(fileData, byteIter);
                    byteIter += 2;
                    interestingAttributes += ParseSimpleBoxes(numSimpleBoxes, rects, pFileData, rectGroupColours[i], log, ref byteIter);
                    interestingAttributes += ParseInfoBoxes(numInfoBoxes, rects, pFileData, rectGroupColours[i], log, ref byteIter);
                }
                // the others counts being 2-byte iconBoxes then 2-byte infoBoxes
                // makes this as a single 4-byte count of infoBoxes
                // seem suspiciously wrong, but it works so it probably is right
                uint numOfInfoBoxes = BitConverter.ToUInt32(fileData, byteIter);
                Debug.Assert((numOfInfoBoxes >> 16) == 0);
                byteIter += 4;
                Pen nonGroupRectColour = Pens.White;
                interestingAttributes += ParseInfoBoxes(numOfInfoBoxes, rects, pFileData, nonGroupRectColour, log, ref byteIter);
#if !GMCREATOR && !PRINTALLBOXES
                if (interestingAttributes > 0)
                {
                    log.Flush();
                    Console.WriteLine("Found interesting attribute, writing {0}", outputLogName);
                    MemoryStream ms = (MemoryStream)log.BaseStream;
                    File.WriteAllText(outputLogName, Encoding.UTF8.GetString(ms.ToArray()));
                    Console.ReadLine();
                }
#endif
            }
            metadata = new GMMetadata();
            // manu id
            metadata.ManufacturerID = (ManufacturerId)fileData[byteIter];
            ++byteIter;
            // screen behaviour
            byte screenBehaviour = fileData[byteIter];
            // these don't seem to be bit flags
            // This value is 2 on screens that have a CarDisplay box
            // and 3 on screens that have multiple entry routes (like the car history text screens, and so they don't know where to go back to)
            // there are screens where there is a need to go back, but no car is rendered
            // and those use 3 instead of 1 (no screen in the game has a screenBehaviour of 1. Only 0, 2 & 3)
            metadata.BackLinkToPreviousScreen = (screenBehaviour == 3);
            ++byteIter;
            // unk
            metadata.ScreenType = (ScreenType)BitConverter.ToInt16(fileData, byteIter);
            byteIter += 2;
            // back link
            metadata.BackLink = BitConverter.ToInt16(fileData, byteIter);
            byteIter += 4;
            // bg file
            metadata.BackgroundIndex = BitConverter.ToInt16(fileData, byteIter);
            byteIter += 4;
            pinnedFileData.Free();
            byteIterAfter = byteIter;
            return rects;
        }

        class DrawnPoint : IComparable<DrawnPoint>
        {
            public int x, y;
            public DrawnPoint(int inX, int inY)
            {
                x = inX;
                y = inY;
            }

            public int CompareTo(DrawnPoint other)
            {
                if (x < other.x) return -1;
                if (x > other.x) return 1;
                return y.CompareTo(other.y);
            }
        }

        static bool TestAndAddPoint(SortedList<DrawnPoint, int> drawnPoints, Rectangle drawRect)
        {
            bool ret = false;
            int[] changes = { 0, 1, -1 };
            int x = drawRect.Left;
            int y = drawRect.Top;

            foreach (int changeX in changes)
            {
                foreach (int changeY in changes)
                {
                    DrawnPoint dp = new DrawnPoint(x + changeX, y + changeY);
                    if (drawnPoints.ContainsKey(dp))
                    {
                        ret = true;
                        goto outLoop;
                    }
                }
            }
        outLoop:
            if (!ret)
            {
                drawnPoints.Add(new DrawnPoint(x, y), 0);
            }
            return ret;
        }

        private static byte[] ReadAllStream(Stream s)
        {
            byte[] data = new byte[65536];
            int len = data.Length;
            int read = 0;
            MemoryStream ms = new MemoryStream(128000);
            while ((read = s.Read(data, 0, len)) > 0)
            {
                ms.Write(data, 0, read);
            }
            return ms.ToArray();
        }

        public static GMFileInfo Parse(string file)
        {
            MemoryStream allStream = new MemoryStream(File.ReadAllBytes(file));
            return Parse(file, allStream);
        }

        public static GMFileInfo Parse(Stream fileData)
        {
            return Parse(String.Empty, fileData);
        }

        private static GMFileInfo Parse(string fileName, Stream fileStream)
        {
            MemoryStream ms = fileStream as MemoryStream;
            byte[] fileData = (ms != null) ? ms.ToArray() : ReadAllStream(fileStream);
            if (Encoding.ASCII.GetString(fileData, 0, 3) != "GM\x3")
            {
                return null;
            }
            int gmllHeaderPos;
            GMMetadata metadata;
            List<DrawRectInfo> clickablePos = ParsePrePixelData(Path.GetFileName(fileName), fileData, out gmllHeaderPos, out metadata);
#if !GMCREATOR && DO_GRAPHIC_DRAW
            return new GMFileInfo();
#else
            Bitmap bm = ParseGMLLData(fileData, gmllHeaderPos, clickablePos);
            return new GMFileInfo(bm, clickablePos, metadata);
#endif
        }

        public static Bitmap ParseGMLLData(byte[] fileData, int gmllPos, List<DrawRectInfo> clickablePos)
        {
            List<Palette> paletteList = new List<Palette>();
            int numPositionsEntries = BitConverter.ToInt32(fileData, gmllPos + 4);
            // first +4 for GMLL, second 4 for the number of entries
            int paletteOffset = gmllPos + 4 + 4 + (numPositionsEntries * 4);
            int pixelDataOffset = paletteOffset + 0x400;
            int numTiles = BitConverter.ToInt32(fileData, pixelDataOffset);
            pixelDataOffset += 4;
            int numImageBytes = (int)(fileData.Length - pixelDataOffset);
            byte[] imageArray = new byte[numImageBytes];
            Array.Copy(fileData, pixelDataOffset, imageArray, 0, numImageBytes);

            int palettePosIter = paletteOffset;
            for (int i = 0; i < 32; ++i)
            {
                Palette p = new Palette(16);
                for (int j = 0; j < p.colours.Length; ++j, palettePosIter += 2)
                {
                    // colours are in BGR format
                    ushort colour = BitConverter.ToUInt16(fileData, palettePosIter);
                    p.colours[j] = colour;
                }
                paletteList.Add(p);
                p.GMSwizzleColours();
#if PRINT_PALETTES
                Console.WriteLine("Palette {0}:", i + 1);
                int colNum = 0;
                for(int j = 0; j < p.colours.Length; ++j)
                {
                    ushort colour = p.colours[j];
                    if(colNum++ == 8)
                    {
                        colNum = 0;
                        Console.WriteLine();
                    }
                    Console.Write("{0:x} ", colour);
                }
                Console.WriteLine();
#endif
            }
            List<ImageSlice> slices = ParseSlices(imageArray, numTiles);
            Rectangle bmSize = new Rectangle(0, 0, 512, 504);

            Bitmap bm;
            using (Bitmap bmTemp = new Bitmap(512, 504, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmTemp))
                {
#if DRAW_FOUND_BOXES_ON_IMAGE
                    g.FillRectangle(Brushes.Black, bmSize);
#else
                    g.FillRectangle(Brushes.Transparent, bmSize);
#endif
                }
                bm = bmTemp.Clone(bmSize, PixelFormat.Format16bppArgb1555);
            }
            byte[] positionArray = new byte[numPositionsEntries * 4];
            Array.Copy(fileData, gmllPos + 8, positionArray, 0, positionArray.Length);
            List<GT2.PositionData> pd = ParsePositionData(positionArray, positionArray.Length);
            GT2.Common.ArrangeSlices(bm, slices, paletteList, pd);
#if DRAW_FOUND_BOXES_ON_IMAGE
            SortedList<DrawnPoint, int> drawnPoints = new SortedList<DrawnPoint, int>();
            if ((clickablePos != null) && (clickablePos.Count > 0))
            {
                using (Graphics g = Graphics.FromImage(bm))
                using (Font f = new Font(FontFamily.GenericMonospace, 10))
                {
                    int index = 0;
                    foreach (DrawRectInfo dri in clickablePos)
                    {
                        Rectangle rect = dri.rect;
                        DrawnPoint dp = new DrawnPoint(rect.Left, rect.Top);
                        PointF textPoint = new PointF(rect.Left + 2, rect.Top + 2);
                        // ensure overlapping rects can still draw all their ids
                        if (TestAndAddPoint(drawnPoints, rect))
                        {
                            textPoint.Y += 12;
                        }
                        g.DrawRectangle(dri.rectDrawColour, rect);
                        g.DrawString(index.ToString(), f, dri.rectDrawColour.Brush, textPoint);
                        ++index;
                    }
                }
            }
#endif
            return bm;
        }

        public static void DumpGMFile(string file, string outDir)
        {
            string justName = Path.GetFileName(file);
            Console.WriteLine("Processing '{0}'", justName);
            GMFileInfo fileInfo;
            try
            {
                using (FileStream fs = File.OpenRead(file))
                using (MemoryStream ms = new MemoryStream((int)(fs.Length * 2)))
                using (ICSharpCode.SharpZipLib.GZip.GZipInputStream gzi = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(fs))
                {
                    byte[] buffer = new byte[4096];
                    ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(gzi, ms, buffer);
                    ms.Position = 0;
                    fileInfo = Parse(file, ms);
                }
            }
            catch (Exception)
            {
                fileInfo = Parse(file);
            }
            using (Bitmap bm = fileInfo.Image)
            {
                string name = Path.GetFileName(file);
                string outputFile = Path.Combine(outDir, name + ".png");
                bm.Save(outputFile, ImageFormat.Png);
            }
        }

        public static void DumpGMDir(string dir, string outDir)
        {
            string[] files = Directory.GetFiles(dir);
            Directory.CreateDirectory(outDir);
            // max tiles in a file is 0x400, least is 0x24
            foreach (string file in files)
            {
                DumpGMFile(file, outDir);
            }
        }

        class FoundFileInfo
        {
            private string picDir;
            private string decompDir;
            private string outFile;
            private int fileNum;
            private byte[] fileData;
            private FileStream wholeFile;
            private RefCounter completionCount;
            private RefCounter extraCount;
            private SplitGTMenuFlags opFlags;

            public FoundFileInfo(
                int fNum, 
                byte[] data, 
                string pictureFolder, 
                string decompressFolder,
                SplitGTMenuFlags flags,
                string outputFile,
                RefCounter completeCount, 
                RefCounter exCount
            )
            {
                picDir = pictureFolder;
                decompDir = decompressFolder;
                opFlags = flags;
                fileNum = fNum;
                fileData = data;
                outFile = outputFile;
                completionCount = completeCount;
                extraCount = exCount;
            }

            public void WriteAndDecomp(object o)
            {
                wholeFile = new FileStream(outFile, FileMode.Create, FileAccess.Write, FileShare.None, UInt16.MaxValue, true);
                wholeFile.BeginWrite(fileData, 0, fileData.Length, new AsyncCallback(FinishWrite), null);
                if (opFlags != SplitGTMenuFlags.None)
                {
                    MemoryStream compStream = new MemoryStream(fileData);
                    MemoryStream decompStream = new MemoryStream(fileData.Length * 2);
                    byte[] buffer = new byte[UInt16.MaxValue];
                    using (ICSharpCode.SharpZipLib.GZip.GZipInputStream gzIn = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(compStream))
                    {
                        ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(gzIn, decompStream, buffer);
                    }
                    if ((opFlags & SplitGTMenuFlags.OutputDecompressed) != 0)
                    {
                        string decompFile = Path.Combine(decompDir, fileNum.ToString() + ".gm");
                        File.WriteAllBytes(decompFile, decompStream.ToArray());
                    }
                    if ((opFlags & SplitGTMenuFlags.OutputPngPicture) != 0)
                    {
                        decompStream.Position = 0;
                        GMFileInfo gmInf = GMFile.Parse(decompStream);
                        using (Bitmap bm = gmInf.Image)
                        {
                            if (bm != null)
                            {
                                bm.Save(Path.Combine(picDir, fileNum.ToString() + ".gm.png"), ImageFormat.Png);
                            }
                        }
                    }
                    extraCount.Increment();
                }
            }

            private void FinishWrite(IAsyncResult res)
            {
                completionCount.Increment();
                wholeFile.EndWrite(res);
                wholeFile.Dispose();
            }
        }

        [Flags]
        public enum SplitGTMenuFlags
        {
            None = 0,
            OutputDecompressed = 1,
            OutputPngPicture = 2
        }

        public static void SplitGTMenuDat(string datFile, string outDir, SplitGTMenuFlags opFlags)
        {
            string idxFileName = Path.ChangeExtension(datFile, ".idx");
            string pictureDir = null, decompDir = null;
            RefCounter completeCount = new RefCounter();
            RefCounter extraCount = null;
            if (opFlags != 0)
            {
                if ((opFlags & SplitGTMenuFlags.OutputPngPicture) != 0)
                {
                    pictureDir = Path.Combine(outDir, "pictures");
                    Directory.CreateDirectory(pictureDir);
                }
                if ((opFlags & SplitGTMenuFlags.OutputDecompressed) != 0)
                {
                    decompDir = Path.Combine(outDir, "decomp");
                    Directory.CreateDirectory(decompDir);
                }                
                extraCount = new RefCounter();
            }
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount);
            byte[] packFile = File.ReadAllBytes(datFile);
            int numIdxEntries = 0;
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            using (BinaryReader idxFile = new BinaryReader(File.OpenRead(idxFileName)))
            {
                long packFileSize = packFile.Length;
                numIdxEntries = idxFile.ReadInt32();

                int startPointWithZeroes = idxFile.ReadInt32();
                // the lower two bits o each index entry say how many padding bytes there
                // are at the end of the file to pad the next entry to a multiple of 
                // four bytes
                int startPoint = startPointWithZeroes & ~3;
                int numToSubtract = startPointWithZeroes & 3;
                StringBuilder outFileName = new StringBuilder(outDir);
                outFileName.Append(Path.DirectorySeparatorChar);
                int fileNameStart = outFileName.Length;
                for (int i = 0; i < numIdxEntries; ++i)
                {
                    int endPointWithZeroes = idxFile.ReadInt32();
                    int endPoint = (endPointWithZeroes & ~3);
                    int len = endPoint - startPoint - numToSubtract;
                    numToSubtract = endPointWithZeroes & 3;
                    byte[] buffer = new byte[len];
                    Buffer.BlockCopy(packFile, startPoint, buffer, 0, len);
                    outFileName.Length = fileNameStart;
                    outFileName.AppendFormat("{0}.gm", i);
                    FoundFileInfo ffi = new FoundFileInfo(i, buffer, pictureDir, decompDir, opFlags, outFileName.ToString(), completeCount, extraCount);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ffi.WriteAndDecomp));
                    startPoint = endPoint;
                }
            }
            while (!completeCount.HasReached(numIdxEntries))
            {
                Thread.Sleep(100);
            }
            if (extraCount != null)
            {
                while (!extraCount.HasReached(numIdxEntries))
                {
                    Thread.Sleep(100);
                }
            }
#if DEBUG
            sw.Stop();
            Debug.WriteLine(String.Format("GTMP Extract and decomp took {0:F2} seconds", sw.Elapsed.TotalSeconds));
#endif
        }
    }
}