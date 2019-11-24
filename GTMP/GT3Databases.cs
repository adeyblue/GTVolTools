//#define RAW_STRINGS // - outputs only hex and any strings the hex corresponds to
//#define PRINT_UNKS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;
//using GT2MultiEdit;

// This file parses out (un)known data from the various files in the database folder
// of an exploded GT3 vol. These files are to GT3 what gtmode_data.dat is to GT2.
// They define all the information about how the cars and their parts/upgrades work
// the race rules (opponents, entry requirements, length)
// Unlike GT2, prize cars are set in a separate file (the gameconf\product_(region).gcf file)
// The results are human readable text files
//    please don't waste time editing any of the text files hoping to change the game, it won't work
//
// The code can handle compressed files itself so you don't need
// to do any decompressing beforehand. It can also handle the databases for the GT3 based Concept game
// titles which have some slightly differently formatted data.
// The demo databases are slightly different in format again, and most cause this code to crash

namespace GTMP
{
    static class GT3DB
    {
        static readonly string[] g_tuningRestrictionDesc = { String.Empty, "Race Cars Only", "Tuned Cars Only", "Non-Tuned Cars Only" };
        static readonly string[] g_aspRestrictionDesc = { String.Empty, "NA Cars Only", "Turbo Cars Only" };
        static readonly string[] g_driveRestrictionDesc = { String.Empty, "FF Cars Only", "FR Cars Only", "4WD Cars Only", "MR Cars Only", "RR Cars Only" };
        static readonly string[] g_driveTypes = { "FR", "FF", "4WD", "MR", "RR" };
        static readonly string[] g_startTypes = {"Immediate", "3 Second Countdown", "Pan & Countdown", "5 Second Auto-drive"};
        static List<string> g_paramUnistrDB = null;
        static List<string> g_paramStrDB = null;
        static List<string> g_carColorDB = null;

        public const uint ISNT_DEMO = 0;
        public const uint IS_DEMO = 1;
        static List<string> ReadIdDbStrFile(string file)
        {
            List<string> strings = null;
            if(!File.Exists(file)) return null;
            using (FileStream strFile = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                Stream stream = GetReadableStream(strFile);
                BinaryReader br = new BinaryReader(stream, Encoding.UTF8);
                string header = new string(br.ReadChars(4));
                if (header != "STDB")
                {
                    Console.WriteLine("{0} isn't a string db file!", Path.GetFileName(file));
                    return null;
                }
                uint numEntries = br.ReadUInt32();
                strings = new List<string>((int)numEntries);
                br.ReadUInt32(); // unk, always 1
                uint endOfFile = br.ReadUInt32();
                for (uint i = 0; i < numEntries; ++i)
                {
                    string str = String.Empty;
                    uint offset = br.ReadUInt32();
                    long curPos = br.BaseStream.Position;
                    if (offset < endOfFile)
                    {
                        br.BaseStream.Seek(offset, SeekOrigin.Begin);
                        ushort len = br.ReadUInt16();
                        str = new string(br.ReadChars((int)len));
                    }
                    strings.Add(str);
                    br.BaseStream.Seek(curPos, SeekOrigin.Begin);
                }
            }
            return strings;
        }

        static void DumpStrDB(string dbDir, string fileName, Encoding textEncoding, List<string> strDbContents)
        {
            using (FileStream db = new FileStream(Path.Combine(dbDir, fileName), FileMode.Open, FileAccess.Read))
            {
                Encoding euc = textEncoding;
                Stream stream = GetReadableStream(db);
                BinaryReader br = new BinaryReader(stream, euc);
                string id = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (id != "STDB")
                {
                    Console.WriteLine("{0} doesn''t start STDB!", fileName);
                    return;
                }
                uint numStr = br.ReadUInt32();
                Console.WriteLine("{0} - found {1} strings", fileName, numStr);
                br.ReadUInt32(); // unk
                uint fileSize = br.ReadUInt32(); // file size
                char[] nullChar = new char[] { '\0' };
                using (StreamWriter sw = new StreamWriter(Path.Combine(dbDir, fileName + ".txt"), false, Encoding.Unicode))
                {
                    for (uint i = 0; i < numStr; ++i)
                    {
                        int strFilePos = br.ReadInt32();
                        long curPos = br.BaseStream.Position;
                        if (strFilePos < fileSize)
                        {
                            br.BaseStream.Seek(strFilePos, SeekOrigin.Begin);
                            short numChars = br.ReadInt16();
                            byte[] bytes = br.ReadBytes(numChars);
                            string toWrite = euc.GetString(bytes);
                            toWrite = toWrite.TrimEnd(nullChar);
                            strDbContents.Add(toWrite);
                            string line = String.Format("{0}({0:x}) - {1:x} - '{2}'", i + 1, strFilePos, toWrite);
                            sw.WriteLine(line);
                        }
                        br.BaseStream.Seek(curPos, SeekOrigin.Begin);
                    }
                }
            }
        }

        static void DumpCarList(string outputFile, BinaryReader br, Dictionary<ulong, string> idstoString, uint fileSize)
        {
            uint readSoFar = 16;
            using (StreamWriter sw = new StreamWriter(outputFile, false, Encoding.Unicode))
            {
                int stringsRead = 0;
                while (readSoFar < fileSize)
                {
                    ulong textId = br.ReadUInt64();
                    readSoFar += 8;
                    string text = String.Empty;
                    if (idstoString.TryGetValue(textId, out text))
                    {
                        sw.Write("{0}", text);
                        ++stringsRead;
                        if ((stringsRead % 3) == 0)
                        {
                            sw.WriteLine();
                        }
                        else sw.Write("\t");
                    }
                }
            }
        }

        delegate void DumpGTDTEntry(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        );

        static void NoSpecialDumping(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
        }

        static void DumpBrakes(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] brakeTypes = new string[] { "Stock", "Sports" };
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_brakes.txt"))
            {
                byte[] unkBytes = new byte[3];
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    br.Read(unkBytes, 0, 3);
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, brakes - {1}", carName, partName);
                    sw.WriteLine("Price - {0}, type - {1}", price, brakeTypes[stage]);
                    sw.WriteLine();

                    GT3CarInfo.Brakes brakes = new GTMP.GT3CarInfo.Brakes();
                    brakes.name = partName;
                    brakes.price = price;
                    brakes.type = brakeTypes[stage];
                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.brakes.Add(brakes);
                }
            }
        }

        static void DumpBrakeControllers(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_brakeControllers.txt"))
            {
                byte[] unkBytes = new byte[8];
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    // unk bytes
                    byte avail = br.ReadByte();
                    byte maxFrontLevel = br.ReadByte();
                    ushort unk1 = br.ReadUInt16();
                    byte defaultFrontLevelOnEquip = br.ReadByte();
                    byte maxRearLevel = br.ReadByte();
                    ushort unk2 = br.ReadUInt16();
                    byte defaultRearLevelOnEquip = br.ReadByte();
                    byte[] unk3 = br.ReadBytes(3);
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, brake controller - {1}, avail - {2}", carName, partName, avail);
                    sw.WriteLine("Default Level, Front - {0}, Rear - {1}", defaultFrontLevelOnEquip, defaultRearLevelOnEquip);
                    sw.WriteLine("Max Level Front - {0}, Rear - {1}", maxFrontLevel, maxRearLevel);
                    sw.WriteLine("Price - {0}", price);
                    sw.WriteLine();

                    GT3CarInfo.BrakeBalanceController controller = new GTMP.GT3CarInfo.BrakeBalanceController();
                    controller.name = partName;
                    controller.price = price;
                    controller.avail = avail;
                    controller.defaultLevelFront = defaultFrontLevelOnEquip;
                    controller.defaultLevelRear = defaultRearLevelOnEquip;
                    controller.maxLevelFront = maxFrontLevel;
                    controller.maxLevelRear = maxRearLevel;
                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.brakeController = controller;
                }
            }
        }

        static void DumpBasicDimensions(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            Debug.Assert((entrySize == 32) || (entrySize == 40));
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_basicDimensions.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    ushort unk = br.ReadUInt16();
                    ushort unk2 = br.ReadUInt16();
                    ushort length = br.ReadUInt16();
                    ushort width = br.ReadUInt16();
                    ushort wheelbase = br.ReadUInt16();
                    ushort weight = br.ReadUInt16();
                    ushort unk5 = br.ReadUInt16();
                    ushort unk6 = br.ReadUInt16();
                    if (entrySize == 40)
                    {
                        ulong unk7 = br.ReadUInt64();
                    }

                    sw.WriteLine("Car - {0}, basic dimensions - {1}", carName, partName);
                    sw.WriteLine("Length - {0}mm, width - {1}mm, wheelbase - {2}mm", length, width, wheelbase);
                    sw.WriteLine("Weight - {0}kg", weight);
                    sw.WriteLine();

                    GT3CarInfo.Dimenesions dims = new GTMP.GT3CarInfo.Dimenesions();
                    dims.length = length;
                    dims.name = partName;
                    dims.weight = weight;
                    dims.wheelbase = wheelbase;
                    dims.width = width;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.basicDimensions = dims;
                }
            }
        }

        static void DumpWeightReductions(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_weightReductions.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte unk2 = br.ReadByte();
                    ushort weightReductPercent = br.ReadUInt16();
                    uint price = br.ReadUInt32();                  

                    sw.WriteLine("Car - {0}, weight reduction - {1}", carName, partName);
                    sw.WriteLine("Weight % of original - {0}", weightReductPercent / 10);
                    sw.WriteLine("Price - {0}, stage - {1}", price, stage);
                    sw.WriteLine();

                    GT3CarInfo.WeightReduction red = new GTMP.GT3CarInfo.WeightReduction();
                    red.name = partName;
                    red.percentageOfOriginal = weightReductPercent / 10;
                    red.price = price;
                    red.stage = stage;
                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.weightReductions.Add(red);
                }
            }
        }

        static void DumpCarData(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_dimensions.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    long entryStart = br.BaseStream.Position;
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    ulong unk = br.ReadUInt64();
                    ulong modelFileId = br.ReadUInt64();
                    string modelFile = idsToString[modelFileId];
                    ulong unk2 = br.ReadUInt64();
                    ulong unk3 = br.ReadUInt64();
                    ushort weightCoeff = br.ReadUInt16();
                    ushort trackFront = br.ReadUInt16(); // mm
                    ushort trackRear = br.ReadUInt16(); // mm
                    ushort width = br.ReadUInt16(); // mm
                    ulong unk4 = br.ReadUInt64();
                    ulong unk5 = br.ReadUInt64();
                    long entryEnd = br.BaseStream.Position;
                    int actualEntrySize = (int)(entryEnd - entryStart);

                    sw.WriteLine("Car - {0}, dimensions - {1}", carName, partName);
                    sw.WriteLine("Model file - {0}", modelFile);
                    sw.WriteLine("Weight coeff - {0}", weightCoeff);
                    sw.WriteLine("Track front - {0}mm, rear - {1}mm", trackFront, trackRear);
                    sw.WriteLine("Width - {0}mm", width);
                    sw.WriteLine();

                    if (actualEntrySize != entrySize)
                    {
                        br.BaseStream.Seek(entryStart + entrySize, SeekOrigin.Begin);
                    }

                    GT3CarInfo.OtherDimensions dims = new GTMP.GT3CarInfo.OtherDimensions();
                    dims.modelFile = modelFile;
                    dims.name = partName;
                    dims.trackFront = trackFront;
                    dims.trackRear = trackRear;
                    dims.weightCoefficient = weightCoeff;
                    dims.width = width;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.otherDimensions = dims;
                }
            }
        }

        static void DumpEngines(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            Debug.Assert((entrySize == 88) || (entrySize == 96));
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_engines.txt"))
            {
                uint[] emptyUintArray = new uint[0];
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    ushort engineDesc1 = br.ReadUInt16(); // 4
                    ushort engineDesc2 = br.ReadUInt16(); // 6
                    ushort aspirationDesc = br.ReadUInt16(); // 8
                    ushort unk = br.ReadUInt16(); // 0xa
                    ushort[] rpmVals = new ushort[16]; // 0xc-0x2c
                    for (int k = 0; k < 16; ++k)
                    {
                        rpmVals[k] = br.ReadUInt16();
                    }
                    ushort displacementDesc = br.ReadUInt16(); // 0x2c
                    ushort psDesc = br.ReadUInt16(); // 0x2e
                    ushort powerRPM = br.ReadUInt16(); // 0x30
                    ushort torqueKGM = br.ReadUInt16(); // 0x32
                    ushort torqueRPM = br.ReadUInt16(); // 0x34
                    byte[] baseUnits = br.ReadBytes(5); // 0x36
                    byte[] hpVals = new byte[16]; // 0x3b-0x4b
                    for (int k = 0; k < 16; ++k)
                    {
                        hpVals[k] = br.ReadByte();
                    }
                    byte numGoodArrayVals = br.ReadByte(); // 0x4c
                    if (entrySize == 96)
                    {
                        ulong unk5 = br.ReadUInt64(); // 0x50
                    }

                    //CarEngineData ced = new CarEngineData();
                    //ced.bandAcceleration = rpmVals;
                    //ced.bandRPMs = hpVals;
                    //ced.significantUnks = numGoodArrayVals;
                    //ced.baseUnits = baseUnits;
                    //uint realPS = DataParser.CalculateRealPS(ced, emptyUintArray, 0);
                    //uint realHP = DataParser.PSToHp(realPS);

                    string engineDescStr = String.Format("{0} {1}", g_paramUnistrDB[engineDesc1], g_paramUnistrDB[engineDesc2]);
                    string aspStr = g_paramUnistrDB[aspirationDesc];
                    string dispStr = g_paramStrDB[displacementDesc];
                    string torqueRPMStr = g_paramUnistrDB[torqueRPM];
                    string listedPowerRPMStr = g_paramUnistrDB[powerRPM];


                    sw.WriteLine("Car - {0}, dimensions - {1}", carName, partName);
                    sw.WriteLine("Engine - {0}", engineDescStr);
                    sw.WriteLine("Aspiration - {0}", aspStr);
                    sw.WriteLine("Displacement - {0}", dispStr);
                    sw.WriteLine("Listed Power PS/RPM - {0}ps/{1}", psDesc, listedPowerRPMStr);
                    sw.WriteLine("Torque KGM/RPM - {0:f2}kgm/{1}", torqueKGM / 100.0, torqueRPMStr);
                    sw.Write("HP Calc Values ({0}):", numGoodArrayVals);
                    for (int k = 0; k < numGoodArrayVals; ++k)
                    {
                        sw.Write(" {0:x}", hpVals[k]);
                    }
                    sw.WriteLine();
                    sw.WriteLine("Base Units: {0:x} {1:x} {2:x} {3:x} {4:x}", baseUnits[0], baseUnits[1], baseUnits[2], baseUnits[3], baseUnits[4]);
                    sw.Write("RPM Acceleration Values:");
                    for (int k = 0; k < numGoodArrayVals; ++k)
                    {
                        sw.Write(" {0:x}", rpmVals[k]);
                    }
                    sw.WriteLine();

                    GT3CarInfo.Engine engine = new GTMP.GT3CarInfo.Engine();
                    engine.aspiration = aspStr;
                    engine.baseVals = baseUnits;
                    engine.displacement = dispStr;
                    engine.engineType = engineDescStr;
                    engine.hpCalcVals = new byte[numGoodArrayVals];
                    Buffer.BlockCopy(hpVals, 0, engine.hpCalcVals, 0, numGoodArrayVals);
                    engine.listedPs = psDesc;
                    engine.listedPsRPM = listedPowerRPMStr;
                    engine.name = partName;
                    engine.rpmAccelValues = new byte[numGoodArrayVals];
                    Buffer.BlockCopy(rpmVals, 0, engine.rpmAccelValues, 0, numGoodArrayVals);

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.engine = engine;
                }
            }
        }

        static void DumpPortPolishes(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_portPolish.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte powerAdjustor = br.ReadByte();
                    byte powerAdjustor2 = br.ReadByte();
                    byte unk2 = br.ReadByte();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, polish - {1}", carName, partName);
                    sw.WriteLine("Price - {0}", price);
                    sw.WriteLine("Power Adjustment - 0x{0:x}, 0x{1:x}", powerAdjustor, powerAdjustor2);
                    sw.WriteLine();

                    GT3CarInfo.PortPolish pol = new GTMP.GT3CarInfo.PortPolish();
                    pol.name = partName;
                    pol.powerAdjustor = powerAdjustor;
                    pol.powerAdjustor2 = powerAdjustor2;
                    pol.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.portPolish = pol;
                }
            }
        }

        static void DumpEngineBalancing(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_engineBalancing.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte unk = br.ReadByte();
                    byte unk2 = br.ReadByte();
                    byte powerAdjustor = br.ReadByte();
                    byte powerAdjustor2 = br.ReadByte();
                    byte[] unk4 = br.ReadBytes(3);
                    uint unk5 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, balance - {1}", carName, partName);
                    sw.WriteLine("Price - {0}", price);
                    sw.WriteLine("Power Adjustment - 0x{0:x}, 0x{1:x}", powerAdjustor, powerAdjustor2);
                    sw.WriteLine();

                    GT3CarInfo.EngineBalancing pol = new GTMP.GT3CarInfo.EngineBalancing();
                    pol.name = partName;
                    pol.powerAdjustor = powerAdjustor;
                    pol.powerAdjustor2 = powerAdjustor2;
                    pol.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.engineBalancing = pol;
                }
            }
        }

        static void DumpEngineDisplacement(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_engineDisplacement.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte powerAdjustor = br.ReadByte();
                    byte powerAdjustor2 = br.ReadByte();
                    byte unk2 = br.ReadByte();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, displacement - {1}", carName, partName);
                    sw.WriteLine("Price - {0}", price);
                    sw.WriteLine("Power Adjustment - 0x{0:x}, 0x{1:x}", powerAdjustor, powerAdjustor2);
                    sw.WriteLine();

                    GT3CarInfo.EngineDisplacement pol = new GTMP.GT3CarInfo.EngineDisplacement();
                    pol.name = partName;
                    pol.powerAdjustor = powerAdjustor;
                    pol.powerAdjustor2 = powerAdjustor2;
                    pol.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.displacement = pol;
                }
            }
        }

        static void DumpRomChips(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_romChips.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte powerAdjustor = br.ReadByte();
                    byte powerAdjustor2 = br.ReadByte();
                    byte unk2 = br.ReadByte();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, rom - {1}", carName, partName);
                    sw.WriteLine("Price - {0}", price);
                    sw.WriteLine("Power Adjustment - 0x{0:x}, 0x{1:x}", powerAdjustor, powerAdjustor2);
                    sw.WriteLine();

                    GT3CarInfo.SportsROM pol = new GTMP.GT3CarInfo.SportsROM();
                    pol.name = partName;
                    pol.powerAdjustor = powerAdjustor;
                    pol.powerAdjustor2 = powerAdjustor2;
                    pol.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.romChip = pol;
                }
            }
        }

        static void DumpNATuning(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_NATuning.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte powerAdjustor = br.ReadByte();
                    byte powerAdjustor2 = br.ReadByte();
                    byte powerAdjustor3 = br.ReadByte();
                    byte powerAdjustor4 = br.ReadByte();
                    byte[] unk = br.ReadBytes(3);
                    uint unk2 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, NA tune - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, Price - {0}", stage, price);
                    sw.WriteLine("Power Adjustment - 0x{0:x}, 0x{1:x}, 0x{2:x}, 0x{3:x}", powerAdjustor, powerAdjustor2, powerAdjustor3, powerAdjustor4);
                    sw.WriteLine();

                    GT3CarInfo.NATuning nat = new GTMP.GT3CarInfo.NATuning();
                    nat.name = partName;
                    nat.powerAdjustors = new byte[] { powerAdjustor, powerAdjustor2, powerAdjustor3, powerAdjustor4 };
                    nat.stage = stage;
                    nat.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.naTunings.Add(nat);
                }
            }
        }

        static void DumpTurboKits(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_turboKits.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte unk1 = br.ReadByte();
                    byte powerAdjustor = br.ReadByte();
                    byte powerAdjustor2 = br.ReadByte();
                    byte powerAdjustor3 = br.ReadByte();
                    byte[] unk2 = br.ReadBytes(3);
                    byte powerAdjustor4 = br.ReadByte();
                    byte powerAdjustor5 = br.ReadByte();
                    byte unk3 = br.ReadByte();
                    byte powerAdjustor6 = br.ReadByte();
                    byte powerAdjustor7 = br.ReadByte();
                    byte powerAdjustor8 = br.ReadByte();
                    ushort unk4 = br.ReadUInt16();
                    uint unk5 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, Turbo kit - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, Price - {0}", stage, price);
                    sw.WriteLine(
                        "Power Adjustment - 0x{0:x}, 0x{1:x}, 0x{2:x}, 0x{3:x}, 0x{4:x}, 0x{5:x}, 0x{6:x}, 0x{7:x}", 
                        powerAdjustor, 
                        powerAdjustor2, 
                        powerAdjustor3, 
                        powerAdjustor4,
                        powerAdjustor4, 
                        powerAdjustor5, 
                        powerAdjustor6, 
                        powerAdjustor7
                    );
                    sw.WriteLine();

                    GT3CarInfo.TurboKit nat = new GTMP.GT3CarInfo.TurboKit();
                    nat.name = partName;
                    nat.powerAdjustors = new byte[] { 
                        powerAdjustor, powerAdjustor2, 
                        powerAdjustor3, powerAdjustor4,
                        powerAdjustor5, powerAdjustor6,
                        powerAdjustor7, powerAdjustor8
                    };
                    nat.stage = stage;
                    nat.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.turboKits.Add(nat);
                }
            }
        }

        static void DumpDrivetrains(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_drivetrains.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte vcdAvailable = br.ReadByte();
                    byte[] unk = br.ReadBytes(3);
                    byte drivetrainType = br.ReadByte();
                    byte unk2 = br.ReadByte();
                    ushort unk3 = br.ReadUInt16();
                    ushort frontWheelPower = br.ReadUInt16(); // lower = better
                    ushort backWheelPower = br.ReadUInt16();
                    byte awdPercentage = br.ReadByte(); // this is a guess
                    byte[] unk5 = br.ReadBytes(3);
                    ushort gearChangeDelay = br.ReadUInt16();
                    ushort unk6 = br.ReadUInt16();
                    uint vcdPrice = br.ReadUInt32();

                    string driveTypeStr = g_driveTypes[drivetrainType];

                    sw.WriteLine("Car - {0}, drivetrain - {1}", carName, partName);
                    sw.WriteLine("Drivetrain - {0}", driveTypeStr);
                    sw.WriteLine("VCD - {0}, price - {1}", vcdAvailable, vcdPrice);
                    sw.WriteLine("Gear Change Delay - {0}", gearChangeDelay);
                    sw.WriteLine("Front wheel power - {0}, rear - {1}", frontWheelPower, backWheelPower);
                    sw.WriteLine("Power distribution% - {0}", awdPercentage);
                    sw.WriteLine();

                    GT3CarInfo.Drivetrain dt = new GTMP.GT3CarInfo.Drivetrain();
                    dt.awdPowerDistribution = awdPercentage;
                    dt.frontWheelPower = frontWheelPower;
                    dt.rearWheelPower = backWheelPower;
                    dt.gearChangeDelay = gearChangeDelay;
                    dt.hasVCD = vcdAvailable;
                    dt.name = partName;
                    dt.type = driveTypeStr;
                    dt.vcdPrice = vcdPrice;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.drivetrain = dt;
                }
            }
        }

        static void DumpFlywheels(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] flywheelDescs = new string[] { "Stock", "Sports", "Semi-Racing", "Racing"};
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_flywheels.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte[] unk = br.ReadBytes(7);
                    uint unk2 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    string flyTypeStr = flywheelDescs[stage];

                    sw.WriteLine("Car - {0}, flywheel - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, price - {1}", flyTypeStr, price);
                    sw.WriteLine("Unks - 0x{0:x}, 0x{1:x}. 0x{2:x}, 0x{3:x}", unk[0], unk[1], unk[2], unk[3]);
                    sw.WriteLine();

                    GT3CarInfo.Flywheel fly = new GTMP.GT3CarInfo.Flywheel();
                    fly.name = partName;
                    fly.price = price;
                    fly.type = flyTypeStr;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }

                    carInf.flywheels.Add(fly);
                }
            }
        }

        static void DumpClutches(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] clutchDescs = new string[] { "Stock", "Single", "Twin Plate", "Triple Plate" };
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_clutches.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte[] unk = br.ReadBytes(7);
                    uint unk2 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    string clutchDescStr = clutchDescs[stage];

                    sw.WriteLine("Car - {0}, clutches - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, price - {1}", clutchDescStr, price);
                    sw.WriteLine("Unks - 0x{0:x}, 0x{1:x}. 0x{2:x}, 0x{3:x}, 0x{4:x}", unk[0], unk[1], unk[2], unk[3], unk[4]);
                    sw.WriteLine();

                    GT3CarInfo.Clutch fly = new GTMP.GT3CarInfo.Clutch();
                    fly.name = partName;
                    fly.price = price;
                    fly.type = clutchDescStr;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.clutches.Add(fly);
                }
            }
        }

        static void DumpPropshafts(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_propshafts.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte[] unk = br.ReadBytes(7);
                    uint unk2 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    sw.WriteLine("Car - {0}, propshaft - {1}", carName, partName);
                    sw.WriteLine("Price - {0}", price);
                    sw.WriteLine("Unks - 0x{0:x}, 0x{1:x}. 0x{2:x}, 0x{3:x}, 0x{4:x}", unk[0], unk[1], unk[2], unk[3], unk[4]);
                    sw.WriteLine();

                    GT3CarInfo.Propshaft fly = new GTMP.GT3CarInfo.Propshaft();
                    fly.name = partName;
                    fly.price = price;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.propshaft = fly;
                }
            }
        }

        static void DumpGearboxes(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            Debug.Assert((entrySize == 48) || (entrySize == 56));
            string[] gearboxDescs = new string[] {"Stock", "Close", "Semi-Close", "Full"};
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_gearboxes.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte numGears = br.ReadByte();
                    ushort unk = br.ReadUInt16();
                    ushort[] gearRatios = new ushort[7];
                    for (int j = 0; j < 7; ++j)
                    {
                        gearRatios[j] = br.ReadUInt16();
                    }
                    ushort finalGearRatio = br.ReadUInt16();
                    ushort unk2 = br.ReadUInt16();
                    ushort unk3 = br.ReadUInt16();
                    byte unk4 = br.ReadByte();
                    byte autoGearSetting = br.ReadByte();
                    ushort maxRatioSetting = br.ReadUInt16(); // this is a guess
                    uint price = br.ReadUInt32();
                    if (entrySize == 56)
                    {
                        ulong unk5 = br.ReadUInt64();
                    }

                    string gearboxDesc = gearboxDescs[stage];

                    float finalGearRatioFloat = (float)(finalGearRatio / 1000.0);
                    float maxGearRatioFloat = (float)(maxRatioSetting / 1000.0);
                    float[] floatRatios = new float[numGears];

                    sw.WriteLine("Car - {0}, gearbox - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, price - {1}", gearboxDesc, price);
                    sw.Write("Num Gears - {0}, Ratios:", numGears);
                    for(int j = 0; j < numGears; ++j)
                    {
                        sw.Write(" {0}", floatRatios[j] = (float)(gearRatios[j] / 1000.0));
                    }
                    sw.WriteLine();
                    sw.WriteLine("Final gear: {0}", finalGearRatioFloat);
                    sw.WriteLine("Max ratio setting: {0}", maxGearRatioFloat);
                    sw.WriteLine("Auto gear setting: {0}", autoGearSetting);
                    sw.WriteLine();

                    GT3CarInfo.Gearbox fly = new GTMP.GT3CarInfo.Gearbox();
                    fly.name = partName;
                    fly.price = price;
                    fly.type = gearboxDesc;
                    fly.autoGearSetting = autoGearSetting;
                    fly.finalGearRatio = finalGearRatioFloat;
                    fly.maxRatioSetting = maxGearRatioFloat;
                    fly.gearRatios = floatRatios;
                    fly.gears = numGears;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.gearboxes.Add(fly);
                }
            }
        }

        static void DumpSuspensions(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] suspensionDescs = new string[] { "Stock", "Sports", "Semi-Racing", "Fully Customised" };
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_suspensions.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte type = br.ReadByte();
                    byte[] unk = br.ReadBytes(7);
                    ulong unk2 = br.ReadUInt64();
                    ulong unk3 = br.ReadUInt64();
                    byte[] unk4 = br.ReadBytes(3);
                    byte frontShockMaxLevel = br.ReadByte();
                    uint unk5 = br.ReadUInt32();
                    ulong unk6 = br.ReadUInt64();
                    byte unk7 = br.ReadByte();
                    byte rearShockMaxLevel = br.ReadByte();
                    byte[] unk8 = br.ReadBytes(6);
                    ulong unk9 = br.ReadUInt64();
                    ulong unk10 = br.ReadUInt64();
                    ulong unk11 = br.ReadUInt64();
                    ulong unk12 = br.ReadUInt64();
                    ulong unk13 = br.ReadUInt64();
                    ulong unk14 = br.ReadUInt64();
                    ulong unk15 = br.ReadUInt64();
                    uint unk16 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    string suspensionDesc = suspensionDescs[type];

                    sw.WriteLine("Car - {0}, suspension - {1}", carName, partName);
                    sw.WriteLine("Type - {0}, price - {1}", suspensionDesc, price);
                    sw.WriteLine("Max front shock level - {0}, rear - {1}", frontShockMaxLevel, rearShockMaxLevel);
                    sw.WriteLine();

                    GT3CarInfo.Suspension susp = new GTMP.GT3CarInfo.Suspension();
                    susp.maxFrontShockLevel = frontShockMaxLevel;
                    susp.maxRearShockLevel = rearShockMaxLevel;
                    susp.name = partName;
                    susp.price = price;
                    susp.type = suspensionDesc;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.suspensions.Add(susp);
                }
            }
        }

        static void DumpIntercoolers(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] intercoolerTypes = new string[] { "Stock", "Sports", "Racing" };
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_intercoolers.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte powerAdjustment1 = br.ReadByte();
                    byte powerAdjustment2 = br.ReadByte();
                    byte unk = br.ReadByte();
                    uint price = br.ReadUInt32();

                    string intercoolerDesc = intercoolerTypes[stage];

                    sw.WriteLine("Car - {0}, intercooler - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, Price - {1}", intercoolerDesc, price);
                    sw.WriteLine("Power Adjustment 1 - {0}, 2 - {1}", powerAdjustment1, powerAdjustment2);
                    sw.WriteLine();

                    GT3CarInfo.Intercooler pol = new GTMP.GT3CarInfo.Intercooler();
                    pol.name = partName;
                    pol.powerAdjustor = powerAdjustment1;
                    pol.powerAdjustor2 = powerAdjustment2;
                    pol.price = price;
                    pol.type = intercoolerDesc;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.intercoolers.Add(pol);
                }
            }
        }

        static void DumpMufflers(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] mufflerTypes = new string[] { "Stock", "Sports", "Semi-racing", "Racing" };
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_mufflers.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte stage = br.ReadByte();
                    byte powerAdjustment1 = br.ReadByte();
                    byte powerAdjustment2 = br.ReadByte();
                    byte unk = br.ReadByte();
                    uint price = br.ReadUInt32();

                    string mufflerDesc = mufflerTypes[stage];

                    sw.WriteLine("Car - {0}, muffler - {1}", carName, partName);
                    sw.WriteLine("Stage - {0}, price - {1}", mufflerDesc, price);
                    sw.WriteLine("Power Adjustment 1 - {0}, 2 - {1}", powerAdjustment1, powerAdjustment2);
                    sw.WriteLine();

                    GT3CarInfo.Muffler pol = new GTMP.GT3CarInfo.Muffler();
                    pol.name = partName;
                    pol.powerAdjustor = powerAdjustment1;
                    pol.powerAdjustor2 = powerAdjustment2;
                    pol.price = price;
                    pol.type = mufflerDesc;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.mufflers.Add(pol);
                }
            }
        }

        static void DumpLsds(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            string[] lsdTypes = new string[] {"Stock", "1 way", "2 way", "1.5 way", "Full", "YAW Control"};
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_lsd.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    byte type = br.ReadByte();
                    byte[] unk = br.ReadBytes(7);
                    ulong unk2 = br.ReadUInt64();
                    ulong unk3 = br.ReadUInt64();
                    uint unk4 = br.ReadUInt32();
                    uint price = br.ReadUInt32();

                    string lsdTypeDesc = lsdTypes[type];

                    sw.WriteLine("Car - {0}, LSD - {1}", carName, partName);
                    sw.WriteLine("Type - {0}, price - {1}", lsdTypeDesc, price);
                    sw.WriteLine();

                    GT3CarInfo.LSD lsd = new GTMP.GT3CarInfo.LSD();
                    lsd.name = partName;
                    lsd.price = price;
                    lsd.type = lsdTypeDesc;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.lsds.Add(lsd);
                }
            }
        }

        static void DumpFrontTyres(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_frontTyres.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    uint type = br.ReadUInt32();
                    uint price = br.ReadUInt32();
                    ulong wheelId = br.ReadUInt64();
                    string wheelName = idsToString[wheelId];
                    ulong tyreId = br.ReadUInt64(); // this repeated 4 times
                    string tyreTypeName = idsToString[tyreId];
                    tyreId = br.ReadUInt64();
                    tyreId = br.ReadUInt64();
                    tyreId = br.ReadUInt64();
                    ulong unk = br.ReadUInt64(); // this repeated 4 times
                    unk = br.ReadUInt64();
                    unk = br.ReadUInt64();
                    unk = br.ReadUInt64();

                    sw.WriteLine("Car - {0}, front tyres - {1}", carName, partName);
                    sw.WriteLine("Type - {0}, price - {1}", tyreTypeName, price);
                    sw.WriteLine("Wheel name - {0}", wheelName);
                    sw.WriteLine();

                    GT3CarInfo.FrontTyres ft = new GTMP.GT3CarInfo.FrontTyres();
                    ft.name = partName;
                    ft.price = price;
                    ft.type = tyreTypeName;
                    ft.wheelName = wheelName;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.frontTyres.Add(ft);
                }
            }
        }

        static void DumpRearTyres(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_rearTyres.txt"))
            {
                for (ushort i = 0; i < entries; ++i)
                {
                    ulong partId = br.ReadUInt64();
                    string partName = idsToString[partId];
                    ulong carId = br.ReadUInt64();
                    string carName = idsToString[carId];
                    uint type = br.ReadUInt32();
                    uint price = br.ReadUInt32();
                    ulong wheelId = br.ReadUInt64();
                    string wheelName = idsToString[wheelId];
                    ulong tyreId = br.ReadUInt64(); // this repeated 4 times
                    string tyreTypeName = idsToString[tyreId];
                    tyreId = br.ReadUInt64();
                    tyreId = br.ReadUInt64();
                    tyreId = br.ReadUInt64();
                    ulong unk = br.ReadUInt64(); // this repeated 4 times
                    unk = br.ReadUInt64();
                    unk = br.ReadUInt64();
                    unk = br.ReadUInt64();

                    sw.WriteLine("Car - {0}, rear tyres - {1}", carName, partName);
                    sw.WriteLine("Type - {0}, price - {1}", tyreTypeName, price);
                    sw.WriteLine("Wheel name - {0}", wheelName);
                    sw.WriteLine();

                    GT3CarInfo.RearTyres ft = new GTMP.GT3CarInfo.RearTyres();
                    ft.name = partName;
                    ft.price = price;
                    ft.type = tyreTypeName;
                    ft.wheelName = wheelName;

                    GT3CarInfo.CarInfo carInf = null;
                    if (!carDb.TryGetValue(carName, out carInf))
                    {
                        carInf = new GTMP.GT3CarInfo.CarInfo();
                        carDb.Add(carName, carInf);
                    }
                    carInf.rearTyres.Add(ft);
                }
            }
        }

        static string GetIdOrEmpty(BinaryReader br, Dictionary<ulong, string> idsToString)
        {
            string retStr = String.Empty;
            ulong id = br.ReadUInt64();
            if (id > 0)
            {
                retStr = idsToString[id];
            }
            return retStr;
        }

        static void WriteNonEmptyString(StreamWriter sw, string formatStr, string dataStr)
        {
            if (!String.IsNullOrEmpty(dataStr))
            {
                sw.WriteLine(formatStr, dataStr);
            }
        }

        static void DumpSetups(
            StreamWriter sw, 
            BinaryReader br, 
            Dictionary<ulong, string> idsToString, 
            ushort entries,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            for (ushort i = 0; i < entries; ++i)
            {
                ulong carId = br.ReadUInt64();
                string carName = idsToString[carId];
                ulong brakePartId = br.ReadUInt64();
                string brakePart = idsToString[brakePartId];
                ulong unkPart = br.ReadUInt64();
                ulong unkPart2 = br.ReadUInt64();
                ulong basicDimensionPartId = br.ReadUInt64();
                string basicDimensionPart = idsToString[basicDimensionPartId];
                string lightweightPart = GetIdOrEmpty(br, idsToString);
                ulong nameToModelId = br.ReadUInt64();
                string nameToModelConversion = idsToString[nameToModelId];
                ulong engineId = br.ReadUInt64();
                string enginePart = idsToString[engineId];
                byte[] always0 = br.ReadBytes(sizeof(ulong) * 5); // always 0 (port-polishes, eng balancing, displacement, racing chip)
                ulong turboKitId = br.ReadUInt64();
                string turboKitPart = idsToString[turboKitId];
                ulong driveTrainId = br.ReadUInt64();
                string driveTrainPart = idsToString[driveTrainId];
                string flywheelPart = GetIdOrEmpty(br, idsToString);
                string clutchPart = GetIdOrEmpty(br, idsToString);
                ulong psPartId = br.ReadUInt64(); // always 0 (maybe propshaft)
                string lsdPart = GetIdOrEmpty(br, idsToString);
                string gearboxPart = GetIdOrEmpty(br, idsToString);
                string suspensionPart = GetIdOrEmpty(br, idsToString);
                string intercoolerPart = GetIdOrEmpty(br, idsToString);
                string mufflerPart = GetIdOrEmpty(br, idsToString);
                string frontTyres = GetIdOrEmpty(br, idsToString);
                string rearTyres = GetIdOrEmpty(br, idsToString);
                byte[] unk2 = br.ReadBytes(sizeof(ulong) * 3); // 3 unk parts
                ushort unk = br.ReadUInt16();
                ushort manufacturerId = br.ReadUInt16();
                byte[] unk3 = br.ReadBytes(4); // unk
                ushort carLogoId = br.ReadUInt16();
                ushort year = br.ReadUInt16();
                uint newPrice = br.ReadUInt32();
                ushort unk5 = br.ReadUInt16();
                ushort uiCarNameIndex = br.ReadUInt16();
                string uiCarName = g_paramUnistrDB[uiCarNameIndex];
                uint unk6 = br.ReadUInt32();
                ulong unk7 = br.ReadUInt64();
                ushort englishUICarNameIndex = br.ReadUInt16();
                ushort powerMultiplier = br.ReadUInt16();
                int manufacturerNameIndex = br.ReadInt32();

                string manufacturerName = g_paramUnistrDB[manufacturerNameIndex];
                string englishUICarName = g_paramUnistrDB[englishUICarNameIndex];

                string uiCarNameToUse = (englishUICarNameIndex > 0) ? englishUICarName : uiCarName;

                sw.WriteLine("Car - {0}", carName);
                sw.WriteLine("Price - {0}, Manufacturer ID - {1}", newPrice, manufacturerId);
                sw.WriteLine("UI car name - {0}, car logo - {1}", uiCarNameToUse, carLogoId);
                sw.WriteLine("Car Year - {0}", year);
                sw.WriteLine("Brakes - {0}", brakePart);
                sw.WriteLine("Basic Dimensions - {0}", basicDimensionPart);
                sw.WriteLine("Name To Model/Other Dims - {0}", nameToModelConversion);
                sw.WriteLine("Engine - {0}", enginePart);
                sw.WriteLine("Turbo kit - {0}", turboKitPart);
                sw.WriteLine("Drivetrain - {0}", driveTrainPart);
                WriteNonEmptyString(sw, "Lightweight part - {0}", lightweightPart);
                WriteNonEmptyString(sw, "Flywheel - {0}", flywheelPart);
                WriteNonEmptyString(sw, "Clutch - {0}", clutchPart);
                WriteNonEmptyString(sw, "Limited Slip - {0}", lsdPart);
                WriteNonEmptyString(sw, "Gearbox - {0}", gearboxPart);
                WriteNonEmptyString(sw, "Suspension - {0}", suspensionPart);
                WriteNonEmptyString(sw, "Intercooler - {0}", intercoolerPart);
                WriteNonEmptyString(sw, "Muffler - {0}", mufflerPart);
                sw.WriteLine("Front tyres - {0}", frontTyres);
                sw.WriteLine("Rear tyres - {0}", rearTyres);
                sw.WriteLine();

                GT3CarInfo.DefaultSetup defSet = new GTMP.GT3CarInfo.DefaultSetup();
                defSet.carLogoId = carLogoId;
                defSet.carUIName = uiCarNameToUse;
                defSet.manufacturer = manufacturerName;
                defSet.name = carName;
                defSet.price = newPrice;
                defSet.year = year;

                GT3CarInfo.CarInfo carInf = null;
                if (!carDb.TryGetValue(carName, out carInf))
                {
                    carInf = new GTMP.GT3CarInfo.CarInfo();
                    carDb.Add(carName, carInf);
                }
                carInf.defaultSetup = defSet;
            }
        }

        static void DumpOppSetups(StreamWriter sw, BinaryReader br, Dictionary<ulong, string> idsToString, ushort entries)
        {
            for (ushort i = 0; i < entries; ++i)
            {
                string opponentSetup = String.Empty;
                ulong setupId = br.ReadUInt64();
                opponentSetup = idsToString[setupId];
                ulong carId = br.ReadUInt64();
                string carName = idsToString[carId];
                ulong brakePartId = br.ReadUInt64();
                string brakePart = idsToString[brakePartId];
                ulong unkPart = br.ReadUInt64();
                ulong unkPart2 = br.ReadUInt64();
                ulong basicDimensionPartId = br.ReadUInt64();
                string basicDimensionPart = idsToString[basicDimensionPartId];
                string lightweightPart = GetIdOrEmpty(br, idsToString);
                ulong nameToModelId = br.ReadUInt64();
                string nameToModelConversion = idsToString[nameToModelId];
                ulong engineId = br.ReadUInt64();
                string enginePart = idsToString[engineId];
                byte[] always0 = br.ReadBytes(sizeof(ulong) * 5); // always 0 (port-polishes, eng balancing, displacement, racing chip)
                ulong turboKitId = br.ReadUInt64();
                string turboKitPart = idsToString[turboKitId];
                ulong driveTrainId = br.ReadUInt64();
                string driveTrainPart = idsToString[driveTrainId];
                string flywheelPart = GetIdOrEmpty(br, idsToString);
                string clutchPart = GetIdOrEmpty(br, idsToString);
                ulong psPartId = br.ReadUInt64(); // always 0 (maybe propshaft)
                string lsdPart = GetIdOrEmpty(br, idsToString);
                string gearboxPart = GetIdOrEmpty(br, idsToString);
                string suspensionPart = GetIdOrEmpty(br, idsToString);
                string intercoolerPart = GetIdOrEmpty(br, idsToString);
                string mufflerPart = GetIdOrEmpty(br, idsToString);
                string frontTyres = GetIdOrEmpty(br, idsToString);
                string rearTyres = GetIdOrEmpty(br, idsToString);
                byte[] unk2 = br.ReadBytes(sizeof(ulong) * 3); // 3 unk parts

                ushort unk3 = br.ReadUInt16();
                ushort unk4 = br.ReadUInt16();
                uint unk5 = br.ReadUInt32();
                ushort unk6 = br.ReadUInt16();
                ushort unk7 = br.ReadUInt16();
                uint unk8 = br.ReadUInt32();
                ushort unk9 = br.ReadUInt16();
                byte frontCamber = br.ReadByte(); // maybe
                byte rearCamber = br.ReadByte();
                int frontToeAngle = br.ReadByte();
                int rearToeAngle = br.ReadByte(); // 0 = outwards, ff = inwards
                ushort unk11 = br.ReadUInt16();
                ushort frontRideHeight = br.ReadUInt16();
                ushort rearRideHeight = br.ReadUInt16();
                ulong unk12 = br.ReadUInt32();
                ushort unk13 = br.ReadUInt16(); // two single bytes
                ushort powerMultiplier = br.ReadUInt16();
                uint unk14 = br.ReadUInt32(); // one 16, last 16 isn't used

                sw.Write("Opponent Setup - {0} ", opponentSetup);
                sw.WriteLine("Car - {0}", carName);
                sw.WriteLine("Brakes - {0}", brakePart);
                sw.WriteLine("Basic Dimensions - {0}", basicDimensionPart);
                sw.WriteLine("Name To Model/Other Dims - {0}", nameToModelConversion);
                sw.WriteLine("Engine - {0}", enginePart);
                sw.WriteLine("Turbo kit - {0}", turboKitPart);
                sw.WriteLine("Drivetrain - {0}", driveTrainPart);
                WriteNonEmptyString(sw, "Lightweight part - {0}", lightweightPart);
                WriteNonEmptyString(sw, "Flywheel - {0}", flywheelPart);
                WriteNonEmptyString(sw, "Clutch - {0}", clutchPart);
                WriteNonEmptyString(sw, "Limited Slip - {0}", lsdPart);
                WriteNonEmptyString(sw, "Gearbox - {0}", gearboxPart);
                WriteNonEmptyString(sw, "Suspension - {0}", suspensionPart);
                WriteNonEmptyString(sw, "Intercooler - {0}", intercoolerPart);
                WriteNonEmptyString(sw, "Muffler - {0}", mufflerPart);
                sw.WriteLine("Front tyres - {0}", frontTyres);
                sw.WriteLine("Rear tyres - {0}", rearTyres);
                sw.WriteLine("Power percentage - {0}", powerMultiplier / 10);
                sw.WriteLine("Ride height - front {0}mm, rear {1}mm", frontRideHeight, rearRideHeight);
                sw.WriteLine("Camber - front {0}, rear - {1}", frontCamber / 10.0, rearCamber / 10.0);
                sw.WriteLine("Toe angle - front {0}, rear - {1}", (frontToeAngle -0x80) / 10.0, (rearToeAngle - 0x80) / 10.0);
                sw.WriteLine();
            }
        }

        static void DumpDefaultSetups(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_setups.txt", false, Encoding.Unicode))
            {
                DumpSetups(sw, br, idsToString, entries, carDb);
            }
        }

        static void DumpOpponentSetups(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_opponentSetups.txt", false, Encoding.Unicode))
            {
                DumpOppSetups(sw, br, idsToString, entries);
            }
        }

        static string GetStringFromId(Dictionary<ulong, string> idsToString, ulong id)
        {
            string text = String.Empty;
            idsToString.TryGetValue(id, out text);
            return text;
        }

        static void DumpRaceList(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            ulong nextId = 0;
            string text = null;
            SortedDictionary<string, string> sortedRacelist = new SortedDictionary<string, string>();
            using (StreamWriter swFile = new StreamWriter(paramDbFile + "_raceList.txt", false, Encoding.Unicode))
            {
                for (int e = 0; e < entries; ++e)
                {
                    MemoryStream msw = new MemoryStream(512);
                    StreamWriter sw = new StreamWriter(msw, Encoding.Unicode);
                    string raceName = GetStringFromId(idsToString, br.ReadUInt64());
                    sw.WriteLine("Name - {0}", raceName);
                    sw.WriteLine("Track - {0}", GetStringFromId(idsToString, br.ReadUInt64()));
                    sw.WriteLine("Possible Opponents");
                    for (int i = 0; i < 32; ++i)
                    {
                        string carId = GetStringFromId(idsToString, br.ReadUInt64());
                        if (!(String.IsNullOrEmpty(carId) || (carId == "-")))
                        {
                            sw.WriteLine("\t{0}", carId);
                        }
                    }
                    for (int i = 0; i < 16; ++i)
                    {
                        nextId = br.ReadUInt64();
                        if (nextId != UInt64.MaxValue)
                        {
                            text = GetStringFromId(idsToString, nextId);
                            sw.WriteLine("{0:x} - {1}", nextId, text);
                        }
                    }
                    nextId = br.ReadUInt64();
                    text = GetStringFromId(idsToString, nextId);
                    if (!(String.IsNullOrEmpty(text) || text == "-"))
                    {
                        sw.WriteLine("Allowable entrants list: {0}", text);
                    }
                    byte startSpeed = br.ReadByte();
                    byte laps = br.ReadByte();
                    byte[] unk8Bytes = new byte[8];
                    const string unk8Fmt = "Unk: 0x{0:x}, 0x{1:x}, 0x{2:x}, 0x{3:x}, 0x{4:x}, 0x{5:x}, 0x{6:x}, 0x{7:x}";
                    const string unk4Fmt = "Unk: 0x{0:x}, 0x{1:x}, 0x{2:x}, 0x{3:x}";
                    // 2
                    sw.WriteLine("Start Speed - {0}", startSpeed);
                    sw.WriteLine("Laps - {0}", laps);
                    // 4
                    unk8Bytes = new byte[8];
                    br.Read(unk8Bytes, 0, 2);
#if PRINT_UNKS
                    sw.WriteLine("Unk - 0x{0:x}, 0x{1:x}", unk8Bytes[0], unk8Bytes[1]);
#endif
                    // 8 - first byte seems to be downforce? Set to 1 makes cars unable to turn
                    br.Read(unk8Bytes, 0, 4);
#if PRINT_UNKS
                    sw.WriteLine(unk4Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3]);
#endif
                    // 12
                    byte[] tyreGrip = new byte[4];
                    br.Read(tyreGrip, 0, 4);
                    sw.WriteLine("AI Tyre Grip% - {0}, {1}, {2}, {3}", tyreGrip[0], tyreGrip[1], tyreGrip[2], tyreGrip[3]);
                    // 16
                    byte[] power = new byte[4];
                    br.Read(power, 0, 4);
                    sw.WriteLine("AI Power% - {0}, {1}, {2}, {3}", power[0], power[1], power[2], power[3]);
                    // 20
                    br.Read(unk8Bytes, 0, 4);
#if PRINT_UNKS
                    sw.WriteLine(unk4Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3]);
#endif
                    // 24
                    byte[] cornerSpeedBytes = new byte[4];
                    br.Read(cornerSpeedBytes, 0, 4);
                    sw.WriteLine("AI CornerSpeed% - {0}, {1}, {2}, {3}", cornerSpeedBytes[0], cornerSpeedBytes[1], cornerSpeedBytes[2], cornerSpeedBytes[3]);
                    // 32
                    br.Read(unk8Bytes, 0, 8);
#if PRINT_UNKS // 0x14s
                    sw.WriteLine(unk8Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3], unk8Bytes[4], unk8Bytes[5], unk8Bytes[6], unk8Bytes[7]);
#endif
                    // 40
                    br.Read(unk8Bytes, 0, 8);
#if PRINT_UNKS // 0x14 0x6
                    sw.WriteLine(unk8Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3], unk8Bytes[4], unk8Bytes[5], unk8Bytes[6], unk8Bytes[7]);
#endif
                    // 48
                    br.Read(unk8Bytes, 0, 4);
#if PRINT_UNKS
                    sw.WriteLine(unk4Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3]);
#endif
                    // 2nd 4 bytes of this start the license times
                    br.Read(unk8Bytes, 0, 4);
                    bool isALicense = false;
                    if ((unk8Bytes[0] + unk8Bytes[1] + unk8Bytes[2] + unk8Bytes[3]) > 0)
                    {
                        isALicense = true;
                        ushort ms = BitConverter.ToUInt16(unk8Bytes, 2);
                        sw.WriteLine("Gold Time - {0}:{1:D2}.{2:D3}", unk8Bytes[0], unk8Bytes[1], ms);
                    }
                    // 56 - these 8 are silver and bronze times
                    br.Read(unk8Bytes, 0, 8);
                    if (isALicense)
                    {
                        ushort ms = BitConverter.ToUInt16(unk8Bytes, 2);
                        sw.WriteLine("Silver Time - {0}:{1:D2}.{2:D3}", unk8Bytes[0], unk8Bytes[1], ms);
                        ms = BitConverter.ToUInt16(unk8Bytes, 6);
                        sw.WriteLine("Bronze Time - {0}:{1:D2}.{2:D3}", unk8Bytes[4], unk8Bytes[5], ms);
                    }
                    else
                    {
#if PRINT_UNKS
                    sw.WriteLine(unk8Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3], unk8Bytes[4], unk8Bytes[5], unk8Bytes[6], unk8Bytes[7]);
#endif
                    }
                    // 64
                    br.Read(unk8Bytes, 0, 2);
#if PRINT_UNKS
                    sw.WriteLine("Unk: 0x{0:x}, 0x{1:x}", unk8Bytes[0], unk8Bytes[1]);
#endif
                    // 66
                    byte reqTyres = br.ReadByte();
                    if (reqTyres > 0)
                    {
                        sw.WriteLine("Required tyres: {0}", reqTyres == 0x9 ? "Rally" : "Road");
                    }
                    // 67
                    unk8Bytes[0] = br.ReadByte();
#if PRINT_UNKS
                    sw.WriteLine("Unk: 0x{0:x}", unk8Bytes[0]);
#endif
                    // 68
                    int[] prizeMonies = new int[] { 
                        br.ReadUInt16() * 100, 
                        br.ReadUInt16() * 100, 
                        br.ReadUInt16() * 100, 
                        br.ReadUInt16() * 100, 
                        br.ReadUInt16() * 100, 
                        br.ReadUInt16() * 100 
                    };
                    sw.WriteLine("Prize Money: 1st - {0}, 2nd - {1}, 3rd - {2}, 4th - {3}, 5th - {4}, 6th - {5}", prizeMonies[0], prizeMonies[1], prizeMonies[2], prizeMonies[3], prizeMonies[4], prizeMonies[5]);
                    // 80
                    br.Read(unk8Bytes, 0, 8);
#if PRINT_UNKS
                    sw.WriteLine(unk8Fmt, unk8Bytes[0], unk8Bytes[1], unk8Bytes[2], unk8Bytes[3], unk8Bytes[4], unk8Bytes[5], unk8Bytes[6], unk8Bytes[7]);
#endif
                    // 88
                    nextId = br.ReadUInt64();
                    // 96
                    text = GetStringFromId(idsToString, nextId);
                    sw.WriteLine("Race Mode: {0}", text);
                    // 100
                    br.Read(unk8Bytes, 0, 3);
                    byte startType = br.ReadByte();
                    // 4,5 (as 0)
                    // 3 = 5 second auto-drive
                    // 2 = normal cinematic start
                    // 1 = start at countdown,
                    // 0 = straight start (like a rolling one)
#if PRINT_UNKS
                    sw.WriteLine("Unk: 0x{0:x}, 0x{1:x}, 0x{2:x}", unk8Bytes[0], unk8Bytes[1], unk8Bytes[2]);
#endif
                    Debug.Assert(startType < g_startTypes.Length);
                    sw.WriteLine("Start Type: {0}", g_startTypes[startType]);
                    // 104
                    byte tuningRestrictValue = br.ReadByte();
                    byte aspRestrictValue = br.ReadByte();
                    byte driveRestrictValue = br.ReadByte();
                    byte unkRestrictValue = br.ReadByte();
                    if ((tuningRestrictValue | aspRestrictValue | driveRestrictValue) != 0)
                    {
                        sw.WriteLine("Restrictions:");
                        Debug.Assert(tuningRestrictValue < g_tuningRestrictionDesc.Length);
                        if (tuningRestrictValue > 0)
                        {
                            sw.WriteLine("\t{0}", g_tuningRestrictionDesc[tuningRestrictValue]);
                        }
                        Debug.Assert(aspRestrictValue < g_aspRestrictionDesc.Length);
                        if (aspRestrictValue > 0)
                        {
                            sw.WriteLine("\t{0}", g_aspRestrictionDesc[aspRestrictValue]);
                        }
                        Debug.Assert(driveRestrictValue < g_driveRestrictionDesc.Length);
                        if (driveRestrictValue > 0)
                        {
                            sw.WriteLine("\t{0}", g_driveRestrictionDesc[driveRestrictValue]);
                        }
                    }
#if PRINT_UNKS
                    sw.WriteLine("Unk: 0x{0:x}", unkRestrictValue);
#endif
                    sw.WriteLine();
                    sw.Flush();
                    msw.Seek(0, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(msw, Encoding.Unicode))
                    {
                        sortedRacelist.Add(raceName, sr.ReadToEnd());
                    }
                }
                using (StreamWriter swTitles = new StreamWriter(paramDbFile + "_raceNames.txt"))
                {
                    foreach (KeyValuePair<string, string> items in sortedRacelist)
                    {
                        swTitles.WriteLine(items.Key);
                        swFile.Write(items.Value);
                    }
                }
            }
        }

        static void DumpAllowedEntrants(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_entrantList.txt", false, Encoding.Unicode))
            {
                for (int e = 0; e < entries; ++e)
                {
                    ulong entrantListId = br.ReadUInt64();
                    string entrantList = idsToString[entrantListId];

                    sw.WriteLine("List - {0}", entrantList);
                    for (int j = 0; j < 32; ++j)
                    {
                        ulong setupId = br.ReadUInt64();
                        string setupName = idsToString[setupId];
                        if (setupName != "-----")
                        {
                            sw.WriteLine("\t{0}", setupName);
                        }
                    }
                    sw.WriteLine();
                }
            }
        }

        static void DumpArcadeSetups(
            string paramDbFile,
            BinaryReader br,
            Dictionary<ulong, string> idsToString,
            ushort entries,
            ushort entrySize,
            uint fileSize,
            Dictionary<string, GT3CarInfo.CarInfo> carDb
        )
        {
            using (StreamWriter sw = new StreamWriter(paramDbFile + "_arcadeSetups.txt", false, Encoding.Unicode))
            {
                DumpOppSetups(sw, br, idsToString, entries);
            }
        }
                

        static DumpGTDTEntry[] g_dumpers = new DumpGTDTEntry[] { 
            DumpBrakes, DumpBrakeControllers, NoSpecialDumping, DumpBasicDimensions, // 1-4
            DumpWeightReductions, DumpCarData, DumpEngines, DumpPortPolishes, // 5-8
            DumpEngineBalancing, DumpEngineDisplacement, DumpRomChips, DumpNATuning, // 9-12
            DumpTurboKits, DumpDrivetrains, DumpFlywheels, DumpClutches, // 13-16
            DumpPropshafts, DumpGearboxes, DumpSuspensions, DumpIntercoolers, // 17-20
            DumpMufflers, DumpLsds, NoSpecialDumping, NoSpecialDumping, // 21-24
            NoSpecialDumping, NoSpecialDumping, NoSpecialDumping, NoSpecialDumping, // 25-28
            DumpFrontTyres, DumpRearTyres, DumpDefaultSetups, DumpOpponentSetups, // 29-32
            DumpRaceList, DumpAllowedEntrants, NoSpecialDumping, DumpArcadeSetups // 33-36
        };

        static DumpGTDTEntry[] g_secondGenDumpers = new DumpGTDTEntry[] { 
            DumpBrakes, DumpBrakeControllers, NoSpecialDumping, DumpBasicDimensions, // 1-4
            DumpWeightReductions, DumpCarData, DumpEngines, DumpPortPolishes, // 5-8
            DumpEngineBalancing, DumpEngineDisplacement, DumpRomChips, DumpNATuning, // 9-12
            DumpTurboKits, DumpDrivetrains, DumpFlywheels, DumpClutches, // 13-16
            DumpPropshafts, DumpGearboxes, DumpSuspensions, DumpIntercoolers, // 17-20
            DumpMufflers, DumpLsds, NoSpecialDumping, NoSpecialDumping, // 21-24
            NoSpecialDumping, NoSpecialDumping, NoSpecialDumping, NoSpecialDumping, // 25-28
            DumpFrontTyres, DumpRearTyres, DumpOpponentSetups, DumpRaceList, // 29-32
            DumpAllowedEntrants, NoSpecialDumping, DumpArcadeSetups, DumpDefaultSetups // 33-36
        };

        static Stream GetReadableStream(FileStream fs)
        {
            Stream toUse = fs;
            byte[] headBytes = new byte[3];
            fs.Read(headBytes, 0, 3);
            fs.Seek(0, SeekOrigin.Begin);
            // this is compressed, decompress
            if((headBytes[0] == 0x1f) && (headBytes[1] == 0x8B) && (headBytes[2] == 0x08))
            {
                MemoryStream ms = new MemoryStream((int)(fs.Length * 3));
                GZipInputStream gzIn = new GZipInputStream(fs);
                byte[] buffer = new byte[32768];
                ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(gzIn, ms, buffer);
                ms.Seek(0, SeekOrigin.Begin);
                toUse = ms;
            }
            return toUse;
        }

        static void DumpCarDB(string paramDbFile, Dictionary<string, GT3CarInfo.CarInfo> carDb)
        {
            string carListFile = paramDbFile + "_carList-Prices.txt";
            using(StreamWriter partsPrices = new StreamWriter(paramDbFile + "_cars-parts-prices.txt"))
            using (StreamWriter sw = new StreamWriter(carListFile))
            {
                sw.WriteLine("CarID\tManufacturer\tName\tPrice\tDrive\tHP\tWeight\tListed Asp\tReal Asp");
                foreach (KeyValuePair<string, GT3CarInfo.CarInfo> car in carDb)
                {
                    GT3CarInfo.CarInfo inf = car.Value;
                    if ((inf.defaultSetup != null) && (inf.engine != null))
                    {
                        string uiName = inf.defaultSetup.carUIName;
                        string realAsp;
                        if (inf.naTunings.Count > 0 && inf.turboKits.Count > 0)
                        {
                            realAsp = "Turbo/NA";
                        }
                        else if (inf.naTunings.Count > 0)
                        {
                            realAsp = "NA";
                        }
                        else if (inf.turboKits.Count > 0)
                        {
                            realAsp = "Turbo";
                        }
                        else
                        {
                            realAsp = "None";
                        }
                        uint hp = 0; // DataParser.PSToHp(inf.engine.listedPs);
                        sw.WriteLine(
                            "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                            car.Key,
                            inf.defaultSetup.manufacturer,
                            inf.defaultSetup.carUIName,
                            inf.defaultSetup.price,
                            inf.drivetrain != null ? inf.drivetrain.type : "??",
                            hp,
                            inf.basicDimensions != null ? inf.basicDimensions.weight.ToString() : "??",
                            inf.engine.aspiration,
                            realAsp
                        );
                        partsPrices.WriteLine("{0} {1} Upgrades:", inf.defaultSetup.manufacturer, uiName);
                        if (inf.displacement != null)
                        {
                            partsPrices.WriteLine(
                                "\tDisplacement - {0} (adjustors - {1}, {2})",
                                inf.displacement.price,
                                inf.displacement.powerAdjustor,
                                inf.displacement.powerAdjustor2
                            );
                        }
                        if (inf.engineBalancing != null)
                        {
                            partsPrices.WriteLine(
                                "\tEngine Balancing - {0} (adjustors - {1}, {2})",
                                inf.engineBalancing.price,
                                inf.engineBalancing.powerAdjustor,
                                inf.engineBalancing.powerAdjustor2
                            );
                        }
                        inf.intercoolers.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.Intercooler c in inf.intercoolers)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine(
                                    "\tIntercooler {0} - {1}  (adjustors - {2}, {3})",
                                    c.type,
                                    c.price,
                                    c.powerAdjustor,
                                    c.powerAdjustor2
                                );
                            }
                        }
                        inf.mufflers.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.Muffler c in inf.mufflers)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine(
                                    "\tMuffler {0} - {1}  (adjustors - {2}, {3})",
                                    c.type,
                                    c.price,
                                    c.powerAdjustor,
                                    c.powerAdjustor2
                                );
                            }
                        }
                        inf.naTunings.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.NATuning c in inf.naTunings)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine(
                                    "\tNATune Stage {0} - {1}  (adjustors - {2}, {3}, {4}, {5})",
                                    c.stage,
                                    c.price,
                                    c.powerAdjustors[0],
                                    c.powerAdjustors[1],
                                    c.powerAdjustors[2],
                                    c.powerAdjustors[3]
                                );
                            }
                        }
                        if (inf.portPolish != null)
                        {
                            partsPrices.WriteLine(
                                "\tPort Polish - {0} (adjustors - {1}, {2})",
                                inf.portPolish.price,
                                inf.portPolish.powerAdjustor,
                                inf.portPolish.powerAdjustor2
                            );
                        }
                        if (inf.romChip != null)
                        {
                            partsPrices.WriteLine(
                                "\tROM Chip - {0} (adjustors - {1}, {2})",
                                inf.romChip.price,
                                inf.romChip.powerAdjustor,
                                inf.romChip.powerAdjustor2
                            );
                        }
                        inf.turboKits.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.TurboKit c in inf.turboKits)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine(
                                    "\tTurbo Stage {0} - {1}  (adjustors - {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
                                    c.stage,
                                    c.price,
                                    c.powerAdjustors[0],
                                    c.powerAdjustors[1],
                                    c.powerAdjustors[2],
                                    c.powerAdjustors[3],
                                    c.powerAdjustors[4],
                                    c.powerAdjustors[5],
                                    c.powerAdjustors[6],
                                    c.powerAdjustors[7]
                                );
                            }
                        }
                        inf.weightReductions.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        if (inf.basicDimensions != null)
                        {
                            int carWeight = inf.basicDimensions.weight;
                            foreach (GT3CarInfo.WeightReduction c in inf.weightReductions)
                            {
                                if (c.price > 0)
                                {
                                    int newCarWeight = (int)((inf.basicDimensions.weight / 100.0f) * c.percentageOfOriginal);
                                    partsPrices.WriteLine(
                                        "\tWeight Reduction Stage {0} - {1} (new weight {2})",
                                        c.stage,
                                        c.price,
                                        newCarWeight
                                    );
                                }
                            }
                        }
                        int numTyreSets = inf.frontTyres.Count;
                        inf.frontTyres.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        for (int i = 0; i < numTyreSets; ++i)
                        {
                            if (inf.frontTyres[i].price > 0)
                            {
                                partsPrices.WriteLine(
                                    "\tTyres {0} - {1} (wheel name: {2})",
                                    inf.frontTyres[i].type,
                                    inf.frontTyres[i].price,
                                    inf.frontTyres[i].name
                                );
                            }
                        }
                        inf.brakes.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach(GT3CarInfo.Brakes b in inf.brakes)
                        {
                            if (b.price > 0)
                            {
                                partsPrices.WriteLine("\tBrakes: {0} - {1}", b.type, b.price);
                            }
                        }
                        if (inf.brakeController != null)
                        {
                            partsPrices.WriteLine("\tBrake Controller - {0}", inf.brakeController.price);
                        }
                        inf.clutches.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.Clutch c in inf.clutches)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine("\tClutch: {0} - {1}", c.type, c.price);
                            }
                        }
                        inf.flywheels.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.Flywheel c in inf.flywheels)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine("\tFlywheel {0} - {1}", c.type, c.price);
                            }
                        }
                        inf.gearboxes.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.Gearbox c in inf.gearboxes)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine("\tGearbox {0} - {1} ({2} gears)", c.type, c.price, c.gears);
                            }
                        }
                        inf.lsds.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.LSD c in inf.lsds)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine("\tLSD {0} - {1}", c.type, c.price);
                            }
                        }
                        
                        if (inf.propshaft != null)
                        {
                            partsPrices.WriteLine("\tPropshaft - {0}", inf.propshaft.price);
                        }
                        inf.suspensions.Sort((x, y) => { return (x.price.CompareTo(y.price)); });
                        foreach (GT3CarInfo.Suspension c in inf.suspensions)
                        {
                            if (c.price > 0)
                            {
                                partsPrices.WriteLine(
                                    "\tSuspension {0} - {1}",
                                    c.type,
                                    c.price
                                );
                            }
                        }
                        partsPrices.WriteLine();
                    }
                }
            }
            string modelToNamesFile = paramDbFile + "_modelfileNames.txt";
            using (StreamWriter carModels = new StreamWriter(modelToNamesFile))
            {
                List<string> data = new List<string>();
                foreach (KeyValuePair<string, GT3CarInfo.CarInfo> car in carDb)
                {
                    GT3CarInfo.CarInfo inf = car.Value;
                    if(inf.otherDimensions != null)
                    {
                        string uiName = inf.defaultSetup.carUIName;
                        string carBase = inf.defaultSetup.name;
                        data.Add(String.Format("{0} - {1} - {2} {3}", inf.otherDimensions.modelFile, carBase, inf.defaultSetup.manufacturer, uiName));
                    }
                }
                data.Sort();
                foreach (string s in data)
                {
                    carModels.WriteLine(s);
                }
            }
        }

        static void DumpParamDb(string paramDbFile, Dictionary<ulong, string> idsToString)
        {
            using (FileStream pdb = new FileStream(paramDbFile, FileMode.Open, FileAccess.Read))
            {
                Stream stream = GetReadableStream(pdb);
                BinaryReader br = new BinaryReader(stream);
                string str = new string(br.ReadChars(4));
                if (str != "GTAR")
                {
                    Console.WriteLine("{0} isn't a paramdb file starting GTAR!", paramDbFile);
                    return;
                }
                uint numGtdts = br.ReadUInt32();
                uint firstOffset = br.ReadUInt32();
                br.ReadUInt32(); // unk
                uint[] gtdtOffsets = new uint[numGtdts]; // these are added to firstOffset to find the gtdt offsets
                for(uint i = 0; i < numGtdts; ++i)
                {
                    gtdtOffsets[i] = br.ReadUInt32();
                }
                DumpGTDTEntry[] dumpersArray = g_dumpers;
                Dictionary<string, GT3CarInfo.CarInfo> carDb = new Dictionary<string,GTMP.GT3CarInfo.CarInfo>(600);
                for (uint i = 0; i < numGtdts; ++i)
                {
                    br.BaseStream.Seek(gtdtOffsets[i] + firstOffset, SeekOrigin.Begin);
                    string header = new string(br.ReadChars(4));
                    if (header != "GTDT")
                    {
                        Console.WriteLine("GTDT {0} of {1} doesn't start with GTDT!", i, paramDbFile);
                        continue;
                    }
                    uint unk1 = br.ReadUInt32(); 
                    ushort entries = br.ReadUInt16();
                    ushort entrySize = br.ReadUInt16();
                    uint fileSize = br.ReadUInt32();

                    // in the second gen files, GTDT 31 has moved to 36 and the 
                    // ones after have been pushed up one. The 3 data differences between
                    // the gens have been accounted for in the dumpers
                    // (They're the ones which have the Debug.Assert in them)
                    if ((i == 30) && (entrySize == 272))
                    {
                        dumpersArray = g_secondGenDumpers;
                    }

#if !RAW_STRINGS
                    dumpersArray[i](paramDbFile, br, idsToString, entries, entrySize, fileSize, carDb);
#else
                    long curPos = br.BaseStream.Position;
                    br.BaseStream.Seek(curPos, SeekOrigin.Begin);
                    string gtdtTextSuffix = String.Format("_gtdt{0}_strings.txt", i + 1);
                    using (StreamWriter sw = new StreamWriter(paramDbFile + gtdtTextSuffix, false, Encoding.Unicode))
                    {
                        sw.WriteLine("Header: {0:x}, entries = 0x{1:x}, entrySize = {2}, size = {3}", unk1, entries, entrySize, fileSize);
                        uint readSoFar = 16;
                        while (readSoFar < fileSize)
                        {
                            ulong textId;
                            string text = GetStringFromId(idsToString, textId = br.ReadUInt64());
                            readSoFar += 8;
                            if (!String.IsNullOrEmpty(text))
                            {
                                sw.WriteLine("{0:x} - {1}", textId, text);
                            }
                            else
                            {
                                byte[] textBytes = BitConverter.GetBytes(textId);
                                sw.WriteLine("{0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}",
                                    textBytes[0], textBytes[1], textBytes[2], textBytes[3],
                                    textBytes[4], textBytes[5], textBytes[6], textBytes[7]
                                );
                            }
                        }
                    }
#endif
                }
                DumpCarDB(paramDbFile, carDb);
            }
        }

        static void DumpRaceDb(string file, string outputFile, Dictionary<ulong, string> idstoString)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                Stream stream = GetReadableStream(fs);
                BinaryReader br = new BinaryReader(stream);
                string header = new string(br.ReadChars(4));
                if (header != "GTDT")
                {
                    Console.WriteLine("RaceDB {0} doesn't start with GTDT!", file);
                    return;
                }
                // next two entries are unk
                uint unk1 = br.ReadUInt32();
                uint unk2 = br.ReadUInt32();
                uint fileSize = br.ReadUInt32();
                using (StreamWriter sw = new StreamWriter(outputFile, false, Encoding.Unicode))
                {
                    sw.WriteLine("Header: {0:x}, {1:x}, size = {2}", unk1, unk2, fileSize);
                    uint readSoFar = 16;
                    while (readSoFar < fileSize)
                    {
                        ulong textId = br.ReadUInt64();
                        readSoFar += 8;
                        string text = String.Empty;
                        idstoString.TryGetValue(textId, out text);
                        sw.WriteLine("{0:x} - {1}", textId, text);
                    }
                }
            }
        }

        static void DumpIDDB(string baseDir, string idxDb, string idxStrDb)
        {
            Dictionary<ulong, string> idToString = new Dictionary<ulong,string>();
            idxStrDb = Path.Combine(baseDir, idxStrDb);
            List<string> idDbStrings = ReadIdDbStrFile(idxStrDb);
            if (idDbStrings == null) return;

            string iddbFile = Path.Combine(baseDir, idxDb);
            if (!File.Exists(iddbFile)) return;

            string dbOutFile = Path.Combine(baseDir, idxDb + ".txt");
            string addedPattern = idxDb.Replace(".id_db_idx", "");
            string paramDbFile = Path.Combine(baseDir, "paramdb" + addedPattern);
            using (FileStream fs = new FileStream(
                    iddbFile,
                    FileMode.Open,
                    FileAccess.Read
                )
            )
            {
                Stream readableStream = GetReadableStream(fs);
                BinaryReader br = new BinaryReader(readableStream);
                string str = new string(br.ReadChars(4));
                if (str != "IDDB")
                {
                    Console.WriteLine("{0} isn't a db file!", iddbFile);
                    return;
                }
                uint numIds = br.ReadUInt32();
                Console.WriteLine("Found {0} items in {1}", numIds, iddbFile);
                using (StreamWriter sw = new StreamWriter(dbOutFile, false, Encoding.UTF8))
                {
                    for (uint i = 0; i < numIds; ++i)
                    {
                        ulong id = br.ReadUInt64();
                        int index = br.ReadInt32();
                        br.ReadInt32(); // skip upper int
                        string name = "Unk";
                        if (index < idDbStrings.Count)
                        {
                            name = idDbStrings[index];
                        }
                        idToString.Add(id, name);
                        sw.WriteLine("{0} - {1:x} = '{2}'", index, id, name);
                    }
                }
            }
            DumpParamDb(paramDbFile, idToString);
            // this doesn't have any strings in it
            //DumpRaceDb(Path.Combine(baseDir, "racedetail.db"), Path.Combine(baseDir, "racedetail.db.txt"), idToString);
            try
            {
                DumpRaceDb(Path.Combine(baseDir, "racemode.db"), Path.Combine(baseDir, "racemode.db.txt"), idToString);
            }
            catch (FileNotFoundException)
            {
                // no race.db for demos;
            }
        }

        static private void DumpAStrDB(string file, List<string> strings, Encoding enc)
        {
            if (File.Exists(file))
            {
                DumpStrDB(Path.GetDirectoryName(file), Path.GetFileName(file), enc, strings);
            }
        }

        private static void DumpGenevaConceptDBIDs(string dbPath, Encoding textEncoding)
        {
            string paramStrs350 = Path.Combine(dbPath, "paramstr_gtc_350z.db");
            string paramStrsEu = Path.Combine(dbPath, "paramstr_gtc_eu.db");
            string paramUniStrs350 = Path.Combine(dbPath, "paramunistr_gtc_350z.db");
            string paramUniStrsEu = Path.Combine(dbPath, "paramunistr_gtc_eu.db");
            string paramStrsUS = Path.Combine(dbPath, "paramstr_gtc_us.db");
            string paramUniStrsUS = Path.Combine(dbPath, "paramunistr_gtc_us.db");
            
            List<string> strs350 = new List<string>();
            List<string> strsEu = new List<string>();
            List<string> uniStrs350 = new List<string>();
            List<string> uniStrsEu = new List<string>();
            List<string> strsUs = new List<string>();
            List<string> uniStrsUs = new List<string>();

            DumpAStrDB(paramStrs350, strs350, textEncoding);
            DumpAStrDB(paramStrsEu, strsEu, textEncoding);
            DumpAStrDB(paramStrsUS, strsUs, textEncoding);
            DumpAStrDB(paramUniStrsUS, uniStrsUs, textEncoding);
            DumpAStrDB(paramUniStrs350, uniStrs350, textEncoding);
            DumpAStrDB(paramUniStrsEu, uniStrsEu, textEncoding);

            g_carColorDB = new List<string>();
            DumpStrDB(dbPath, "carcolor.sdb", textEncoding, g_carColorDB);

            string idDb350 = ".id_db_idx_gtc_350z.db";
            string idDbEu = ".id_db_idx_gtc_eu.db";
            string idDbUs = ".id_db_idx_gtc_us.db";

            g_paramStrDB = strs350;
            g_paramUnistrDB = uniStrs350;
            DumpIDDB(dbPath, idDb350, idDb350.Replace("idx", "str"));

            g_paramStrDB = strsEu;
            g_paramUnistrDB = uniStrsEu;
            DumpIDDB(dbPath, idDbEu, idDbEu.Replace("idx", "str"));

            g_paramStrDB = strsUs;
            g_paramUnistrDB = uniStrsUs;
            DumpIDDB(dbPath, idDbUs, idDbUs.Replace("idx", "str"));
        }

        private static void DumpConceptDBIds(string dbPath, Encoding textEncoding)
        {
            g_carColorDB = new List<string>();
            DumpStrDB(dbPath, "carcolor.sdb", textEncoding, g_carColorDB);

            string paramStrsBS = Path.Combine(dbPath, "paramstr_bs.db");
            string paramUniStrsBS = Path.Combine(dbPath, "paramunistr_bs.db");
            string paramStrsTmv = Path.Combine(dbPath, "paramstr_tmv.db");
            string paramUniStrsTmv = Path.Combine(dbPath, "paramunistr_tmv.db");

            List<string> strsBS = new List<string>();
            List<string> strsTmv = new List<string>();
            List<string> uniStrsBS = new List<string>();
            List<string> uniStrsTmv = new List<string>();

            DumpAStrDB(paramStrsBS, strsBS, textEncoding);
            DumpAStrDB(paramStrsTmv, strsTmv, textEncoding);
            DumpAStrDB(paramUniStrsBS, uniStrsBS, textEncoding);
            DumpAStrDB(paramUniStrsTmv, uniStrsTmv, textEncoding);

            // this is 2002 Seoul
            // kr and bs use the 2nd version of the files, tmv uses the first
            string paramStrsKR = Path.Combine(dbPath, "paramstr_kr.db");
            if(File.Exists(paramStrsKR))
            {
                string paramUniStrsKR = Path.Combine(dbPath, "paramunistr_kr.db");
                g_paramStrDB = new List<string>();
                g_paramUnistrDB = new List<string>();
                string idDbKR = ".id_db_idx_kr.db";
                DumpAStrDB(paramStrsKR, g_paramStrDB, textEncoding);
                DumpAStrDB(paramUniStrsKR, g_paramUnistrDB, textEncoding);
                DumpIDDB(dbPath, idDbKR, idDbKR.Replace("idx", "str"));
            }

            string idDbBS = ".id_db_idx_bs.db";
            string idDbTmv = ".id_db_idx_tmv.db";

            g_paramStrDB = strsBS;
            g_paramUnistrDB = uniStrsBS;
            DumpIDDB(dbPath, idDbBS, idDbBS.Replace("idx", "str"));

            g_paramStrDB = strsTmv;
            g_paramUnistrDB = uniStrsTmv;
            DumpIDDB(dbPath, idDbTmv, idDbTmv.Replace("idx", "str"));
        }

        public static void DumpDBIDsAndNames(string baseDir, uint demoStatus)
        {
            // concept 2001 Tokyo - has _bs and _tmv
            // concept 2002 Tokyo-Seoul has _kr
            // concept 2002 Tokyo-Geneva has _gtc_350z, _gtc_eu, gtc_kr, _gtc_tw, _gtc_us
            string volName = Path.GetDirectoryName(baseDir);
            string dbPath = baseDir + "database";
            string dirStem = dbPath + Path.DirectorySeparatorChar;
            Encoding toUse = (demoStatus == ISNT_DEMO ? Encoding.GetEncoding("EUC-JP") : Encoding.Unicode);
            if (File.Exists(dirStem + "paramdb_gtc_350z.db"))
            {
                DumpGenevaConceptDBIDs(dbPath, toUse);
                return;
            }
            if(File.Exists(dirStem + "paramdb_bs.db"))
            {
                DumpConceptDBIds(dbPath, toUse);
                return;
            }
            // otherwise  it's a standard gt3 db
            string jpParamStrs = Path.Combine(dbPath, "paramstr.db");
            string usParamStrs = Path.Combine(dbPath, "paramstr_us.db");
            string euParamStrs = Path.Combine(dbPath, "paramstr_eu.db");
            string jpParamUniStrs = Path.Combine(dbPath, "paramunistr.db");
            string usParamUniStrs = Path.Combine(dbPath, "paramunistr_us.db");
            string euParamUniStrs = Path.Combine(dbPath, "paramunistr_eu.db");
            List<string> jpParamStrDb = new List<string>();
            List<string> usParamStrDb = new List<string>();
            List<string> euParamStrDb = new List<string>();
            List<string> jpParamUniStrDb = new List<string>();
            List<string> usParamUniStrDb = new List<string>();
            List<string> euParamUniStrDb = new List<string>();
            DumpAStrDB(jpParamStrs, jpParamStrDb, toUse);
            DumpAStrDB(usParamStrs, usParamStrDb, toUse);
            DumpAStrDB(euParamStrs, euParamStrDb, toUse);
            DumpAStrDB(jpParamUniStrs, jpParamUniStrDb, toUse);
            DumpAStrDB(usParamUniStrs, usParamUniStrDb, toUse);
            DumpAStrDB(euParamUniStrs, euParamUniStrDb, toUse);

            g_carColorDB = new List<string>();
            DumpStrDB(dbPath, "carcolor.sdb", toUse, g_carColorDB);

            string jpIdDb = ".id_db_idx.db";
            string usIdDb = ".id_db_idx_us.db";
            string euIdDb = ".id_db_idx_eu.db";

            g_paramStrDB = jpParamStrDb;
            g_paramUnistrDB = jpParamUniStrDb;
            DumpIDDB(dbPath, jpIdDb, jpIdDb.Replace("idx", "str"));

            g_paramStrDB = usParamStrDb;
            g_paramUnistrDB = usParamUniStrDb;
            DumpIDDB(dbPath, usIdDb, usIdDb.Replace("idx", "str"));

            g_paramStrDB = euParamStrDb;
            g_paramUnistrDB = euParamUniStrDb;
            DumpIDDB(dbPath, euIdDb, euIdDb.Replace("idx", "str"));
        }
    }
}

/*
 * This info might not be exactly synchronized with the parsing above
 * ie, if I find something out, I'll code it in but may not update this
unzipped paramdb.db
GTAR
int numEntries? // 36
int firstOffset / headerSize
int ?
int entryOffsets[entries]; // add firstOffset to these for absolute location

Each individual GTDT in the GTAR:
GTDT
int unk
int unk
int fileSizeOfThisGTDT
 
 This list for GT3 JP
GTDT1 - (br)akes = 0x18 in size
8 bytes = part name id (lookup in list)
8 bytes = car name id  (lookup in list)
4 bytes = price
3 bytes = unk
last byte is stage, (0 = default, 1 = racing)
 
GTDT2 - (bc) brake controller? - 0x20 in size
8 bytes = part name id (lookup in list)
8 bytes = car name id  (lookup in list)
1 byte = avail (0 = unbuyable/unfittable)
1 byte = max front level
2 bytes = unk
1 byte = default front level on equip
1 byte = max rear level
2 bytes = unk
1 byte - default rear level on equip
3 bytes - unk
4 bytes = price
 
GTDT3 - unk, no real parts in it
 
GTDT4 - (ch) wheelbase
8 bytes - part id
8 bytes - car id
 * 
2 bytes - unk
2 bytes - unk
2 bytes - length (mm)
2 bytes - height (mm)
 * 
2 bytes - wheelbase (mm)
byte - unk
byte - unk
2 bytes - unk
2 bytes - unk
 * 
8 bytes - unk (only in GT Concept Tokyo Geneva gtc_ databases)
 
GTDT5 (lw) weight reductions - 0x18 size
 8 bytes - part id
 8 bytes - car id
 * 
 1 byte stage
 1 byte - unk
 2 bytes - weight% (* 10, so 97% is 970)
 4 bytes - price
 
 Demio GL-X
 Orig weight = 960kg
 Stage 1 - 931kg (abt 97%)
 
 Mira - orig weight = 700kg
 Stage 1 weight - 679kg (100,000 cr in jp, stored as 1,000)
 In strings.txt
 first 2 bytes is price (* 100 for JP)
 next 2 bytes are the % of orig weight (* 100, so 97% is stored as 970)
 next 2 bytes are ?
 last byte is weight stage (1, 2, 3)
  
 Mira comes in three colours!
GTDT6 - name to model conversion, plus other data
8 bytes - part name
8 bytes - car id
8 bytes - unk
8 bytes - model file name (lookup in list)
8 bytes - unk
8 bytes - unk
2 bytes - something to do with weight (1000 for Demio, but that is 970kg)
2 bytes - track(front)
2 bytes - track(rear)
2 bytes - width mm
8 bytes - unk
8 bytes - unk

GTDT7 - (en)gines - Apart from the two ids at the beginning, this is laid out the same as the GT2 structure
8 bytes - part name
8 bytes - car id
 * 
2 bytes - engine descriptor 1 (index into paramunistr.db)
2 bytes - engine descriptor 2 (index into paramunistr.db)
2 bytes - Aspiration descriptor (index into paramunistr.db)
2 bytes - unk
 *
16 * 2 bytes - power band values
 * 
2 bytes - displacement descriptor (index into paramstr.db NOT paramstruni.db)
2 bytes - dealer/listed ps
2 bytes - dealer/listed power RPM (index into paramunistr.db)
2 bytes - Max Torque, kgm value (divided by 1000, for instance 13.00kgm is stored as 1300)
 *
2 bytes - Max Torque, rpm value (index into paramunistr.db)
5 bytes - baseUnits
byte - start of other values
 * 
14 bytes - rest of other values
byte - number of significant values in both power band values and other values
 * 
8 bytes - unk (only GT Concept Tokyo Geneva gtc_ databases, seems to 1 for every car apart from the NSX proto that has it as 0)
 
GTDT8 - (pp) Port Polish
8 bytes - part name
8 bytes - car id
 * 
4 bytes - price
byte - unk
byte - unk
byte - power modifier
byte - avaiability
 
GTDT9 - (eb) Engine balancing
8 bytes - part name
8 bytes - car id
 * 
byte - availability/stage
byte - unk
byte - unk
byte - power adjustment
byte - power adjustment 2
3 bytes - unk
 * 
4 bytes - unk
4 bytes - price

GTDT10 - (ds) Engine displacement
8 bytes - part name
8 bytes - car id
 * 
byte - availability/stage
byte - power adjustment
byte - power adjustment 2
byte - unk
4 bytes - price
 
GTDT11 - (co) chip/ROM
8 bytes - part name
8 bytes - car id
 * 
byte - availability/stage
byte - power adjustment
byte - power adjustment 2
byte - unk
 * 
4 bytes - price
 
GTDT12 - (nt) NA Tuning?
8 bytes - part name
8 bytes - car id
 * 
byte - stage
byte - power adjustment
byte - power adjustment 2
byte - power adjustment 3
byte - power adjustment 4
3 byte - unk
 * 
4 bytes - unk
4 bytes - price
 
GTDT13 - (tk) Turbo Kit
8 bytes - part name
8 bytes - car id
 * 
byte - stage
byte - unk
byte - power adjustment
byte - power adjustment 2
byte - power adjustment 3
3 bytes - unk
 * 
byte - power adjustment 4
byte - power adjustment 5
byte - unk
byte - power adjustment 6
byte - power adjustment 7
byte - power adjustment 8
2 bytes - unk
 * 
4 bytes - unk
4 bytes - price

GTDT14 - (dt) VCD - Drivetrain
8 bytes - part name
8 bytes - car id
 * 
byte - VCD available
2 bytes - unk
byte - drivetrain type (1 = FF, 2 = FR, etc)
3 bytes - unk
byte - unk
 * 
2 bytes - front wheel power? - lower is more
2 bytes - rear wheel power?
byte - unk (power distribution%?)
3 bytes - unk
 * 
2 bytes - gear change delay
2 bytes - unk
4 bytes - VCD Price

GTDT15 - (fw) flywheels
8 bytes - part name
8 bytes - car id
 * 
byte - stage (1=sports, 2=semi-racing, 3=racing)
7 bytes - unk
* 
4 bytes - unk
4 bytes - price

GTDT16 - (cl) clutches
8 bytes - part name
8 bytes - car id
 * 
byte - stage (1=sports, 2=semi-racing, 3=racing)
7 bytes - unk
* 
4 bytes - unk
4 bytes - price

GTDT17 - (ps) propshaft (as clutches)

GTDT18 - (ge) gearbox
8 bytes - part name
8 bytes - car id
 * 
byte - stage
byte - num gears
2 bytes - unk
2 bytes * 2 - gear ratios (* 1000, so 0.775 is stored at 775)
 *
2 bytes * 4 - more gear ratios
 * 
2 bytes - last normal gear ratio
2 bytes * 3 - final gear ratio
 * 
byte - unk
byte - auto gear setting
8 bytes - unk
 * 
4 bytes - unk
4 bytes - price
*
8 bytes - unk (only GT Concept Tokyo Geneva gtc_ databases, seems to 0 for all cars except the Honda Fit where it's 1)

GTDT19 - (su) suspension
8 bytes - part name
8 bytes - car id
 * 
byte - stage - 3
7 bytes - unk
 * 
8 bytes - unk - 4
 * 
8 bytes - unk - 5
 * 
3 bytes - unk - 6
byte - front shock absorber max level
4 bytes - unk
 * 
8 bytes - unk - 7
 * 
byte - unk - 8
byte - rear shock absorber max level
6 bytes - unk
 * 
8 bytes - unk - 9
 *
8 bytes - unk - 10
 * 
8 bytes - unk - 11
 * 
8 bytes - unk - 12
 *
8 bytes - unk - 13
 * 
4 bytes - unk
4 bytes - price

GTDT20 - (ic) intercoolers
8 bytes - part name
8 bytes - car id
 * 
byte - stage
byte - power adjustment
byte - power adjustment 2
byte - unk
4 bytes - price

GTDT21 - (mu) muffler
8 bytes - part name
8 bytes - car id
 * 
byte - stage
byte - power adjustment
byte - power adjustment 2
byte - unk
4 bytes - price

GTDT22 - (ls) lsd
8 bytes - part name
8 bytes - car id
 * 
byte - stage
7 bytes - unk
 * 
8 bytes - unk
 * 
8 bytes - unk
 *
4 bytes - unk
4 bytes - price

GTDT23, 24, 27 - unk

GTDT25 - wheel shop wheels

GTDT26 - sz list
8 byte - szName
 * 
byte - rim size
byte - unk
byte - tyre size
5 bytes - unk

GTDT28 - tyre / arc_ info

GTDT29 - front tyres
8 bytes - part name
8 bytes - car id
 * 
4 bytes - stage/type
4 bytes - price
 * 
8 bytes - rim/wheel type id
 * 
8 bytes - wheel types (* 4)
 * 
8 bytes - unk (* 4)
first line first word = price
first line last byte = stage (0=normal, 1=sports, 2=super hard. 3=hard, 4=medium hard,
                              5=medium, 6=medium soft, 7=soft, 8=super soft, 9=control)
 
GTDT30 - rear tyres

GTDT31 - default setups? 
moves to GTDT36 in GT Concept Tokyo Geneva gtc_ databases
every subsequent section moves up one. So 32 becomes 31, 33 becomes 32 etc

GTDT32 - opponent setups?
2nd word of last line = power multiplier * 1000

GTDT33 - Race / license definitions
reg_ strings are for allowed entrants
Byte before 05 and prize money in Gsunday_e_0001 = restricted tyres (9 = rally cars, 1-8 = tyre level?)
Last 4 bytes = restrictions 
 * low byte (in memory, not text file)- Tuning = 1 = (racing cars only), 2 = (tuned cars only), 3 = (non tuned cars only), 4+ (blank)
 * next one up - Aspiration = 1 = NA Tuning, 2 = Turbo only, 3+ (blank)
 * next one up - Drivetrain = 1 = FF, 2 = FR, 3 = 4WD, 4 = MR, 5 = RR, 6+ blank
 * highest (last byte of race data) = No effect

6464646400000500 - This 5 is the lap counter
6464646464646464 - (Some of) first 4 64's have something to do with power - (Some of) The second 4 64's have something to do with tyre grippiness
6464646464646464 - First 4 64's downforce/cornering speed? (Last 4 64's, unk)
500066400000000 - 

 * AEST0001 - Arcade Easy Track Races Upper S Class (opps 85% HP)
 * AEST0002 - Arcade Easy Track Races Lower S Class (opps 85% HP)
 * ANST - Arcade Normal Track Races (opps 95% HP)
 * AHST - Arcade Hard Track Races (opps 100% HP)
 * APST - Arcade Pro Track Races (opps 100% HP)
 * AEAD - Arcade Easy Dirt Races (opps 75% HP)
 * ANAD - Arcade Normal Dirt Races (opps 85% HP)
 * AHAD - Arcade Hard Dirt Races (opps 100% HP)
 * APAD - Arcade Pro Dirt Races (opps 100% HP)

GTDT34 - Allowed entrants
 
GTDT35 - Something for the tracks

GTDT36 - Arcade car setups
  
 
 
 ro0003 = duplicate tommy kaira with butterfly wheels
paramstr.db
Format:
STDB
int numStrings?
int unk
int fileSize


CarColor.db
GT2K\0\0\0\0 
int numStructEntries
int secondStuffOffset
int thirdStuffOffset
int fileSize
// firstStuff
CarAndColourCountData [numStructEntries] = 
{
    ulong carId;
    uint numColours; // numColurs?
    uint colorArrayIndex; // index into next section?
}
// second stuff
int numEntries
int ids[numEntries] // ids. IDs in the third section, ie 0x2d here matches id 0x2d in third list. Not an index or offset
// thirdStuff
int numEnties
ColorInfo[numEntries]
{
    int id; //not sequential
    int unk2; // not always the same as unk3
    int dealershipNamePos; // position of name in new car / dealership list
    int argb32; // might be abgr32. Byte order are r, g, b, a
}

carcolor.sdb (string database for car colours) - just a normal stdb

In game
GTConcept Tokyo Geneva PAL uses the gtc_eu databases
*/

/*
 * Core.gt3 decomp & recomping
byte[] b = File.ReadAllBytes(@"I:\core.gt3");
            MemoryStream ms = new MemoryStream(b);
            MemoryStream msOut = new MemoryStream(BitConverter.ToInt32(b, 2));
            ms.Position = 6;
            int bufSize = 16384;
            byte[] buf = new byte[bufSize];
            using (DeflateStream def = new DeflateStream(ms, CompressionMode.Decompress))
            {
                int read = 0;
                while ((read = def.Read(buf, 0, bufSize)) > 0)
                {
                    msOut.Write(buf, 0, read);
                }
            }
            ms = new MemoryStream(b.Length);
            msOut.Position = 0;
            Deflater def2 = new Deflater(8);
            def2.SetInput(msOut.ToArray());
            def2.Finish();
            int wrote = 0;
            while ((wrote = def2.Deflate(buf)) > 0)
            {
                ms.Write(buf, 0, wrote);
            }
            File.WriteAllBytes(@"T:\recomp-core.gt3", ms.ToArray());
*/