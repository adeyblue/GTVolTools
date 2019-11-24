using System;
using System.Collections.Generic;

namespace GTMP
{
    namespace GT3CarInfo
    {
        class CarInfo
        {
            public List<Brakes> brakes;
            public BrakeBalanceController brakeController;
            public Dimenesions basicDimensions;
            public List<WeightReduction> weightReductions;
            public OtherDimensions otherDimensions;
            public Engine engine;
            public PortPolish portPolish;
            public EngineBalancing engineBalancing;
            public EngineDisplacement displacement;
            public SportsROM romChip;
            public List<NATuning> naTunings;
            public List<TurboKit> turboKits;
            public Drivetrain drivetrain;
            public List<Flywheel> flywheels;
            public List<Clutch> clutches;
            public Propshaft propshaft;
            public List<Gearbox> gearboxes;
            public List<Suspension> suspensions;
            public List<Intercooler> intercoolers;
            public List<Muffler> mufflers;
            public List<LSD> lsds;
            public List<FrontTyres> frontTyres;
            public List<RearTyres> rearTyres;
            public DefaultSetup defaultSetup;

            public CarInfo()
            {
                brakes = new List<Brakes>(2);
                weightReductions = new List<WeightReduction>(4);
                naTunings = new List<NATuning>(4);
                turboKits = new List<TurboKit>(5);
                flywheels = new List<Flywheel>(4);
                clutches = new List<Clutch>(4);
                gearboxes = new List<Gearbox>(4);
                suspensions = new List<Suspension>(4);
                intercoolers = new List<Intercooler>(2);
                mufflers = new List<Muffler>(3);
                lsds = new List<LSD>(6);
                frontTyres = new List<FrontTyres>(26);
                rearTyres = new List<RearTyres>(26);
            }
        }

        class DefaultSetup // ie data for how the car is setup as you buy it
        {
            public string name;
            public uint price;
            public string manufacturer;
            public uint carLogoId;
            public string carUIName;
            public ushort year;
        }

        class RearTyres
        {
            public string name;
            public string type;
            public uint price;
            public string wheelName;
        }

        class FrontTyres
        {
            public string name;
            public string type;
            public uint price;
            public string wheelName;
        }

        class LSD
        {
            public string name;
            public string type;
            public uint price;
        }

        class Muffler
        {
            public string name;
            public string type;
            public uint price;
            public byte powerAdjustor;
            public byte powerAdjustor2;
        }

        class Intercooler
        {
            public string name;
            public string type;
            public uint price;
            public byte powerAdjustor;
            public byte powerAdjustor2;
        }

        class Suspension
        {
            public string name;
            public uint price;
            public string type; // sports, semi-racing
            public int maxFrontShockLevel;
            public int maxRearShockLevel;
        }

        class Gearbox
        {
            public string name;
            public uint price;
            public string type; // sports, semi-racing
            public int gears;
            public float[] gearRatios;
            public float finalGearRatio;
            public float maxRatioSetting;
            public int autoGearSetting;
        }

        class Propshaft
        {
            public string name;
            public uint price;
        }

        class Clutch
        {
            public string name;
            public uint price;
            public string type; // sports, semi-racing
        }

        class Flywheel
        {
            public string name;
            public uint price;
            public string type; // sports, semi-racing
        }

        class Drivetrain
        {
            public string name;
            public string type; // FF, FR etc
            public byte hasVCD;
            public uint vcdPrice;
            public int gearChangeDelay;
            public int frontWheelPower;
            public int rearWheelPower;
            public int awdPowerDistribution;
        }

        class TurboKit
        {
            public string name;
            public uint price;
            public int stage;
            public byte[] powerAdjustors;
        }

        class NATuning
        {
            public string name;
            public uint price;
            public int stage;
            public byte[] powerAdjustors;
        }

        class SportsROM
        {
            public string name;
            public uint price;
            public byte powerAdjustor;
            public byte powerAdjustor2;
        }

        class EngineDisplacement
        {
            public string name;
            public uint price;
            public byte powerAdjustor;
            public byte powerAdjustor2;
        }

        class EngineBalancing
        {
            public string name;
            public uint price;
            public byte powerAdjustor;
            public byte powerAdjustor2;
        }

        class PortPolish
        {
            public string name;
            public uint price;
            public byte powerAdjustor;
            public byte powerAdjustor2;
        }

        class Engine
        {
            public string name;
            public string engineType;
            public string aspiration;
            public string displacement;
            public ushort listedPs;
            public string listedPsRPM;
            public byte[] hpCalcVals;
            public byte[] baseVals;
            public byte[] rpmAccelValues;
        }

        class OtherDimensions
        {
            public string name;
            public string modelFile;
            public int weightCoefficient;
            public int trackFront;
            public int trackRear;
            public int width;
        }

        class WeightReduction
        {
            public string name;
            public uint price;
            public int percentageOfOriginal; // so 98 = 98% of the original weight, eg 2% reductions
            public int stage;
        }

        class Brakes
        {
            public string name;
            public string type;
            public uint price;
        }

        class BrakeBalanceController
        {
            public string name;
            public uint price;
            public byte avail;
            public int defaultLevelFront;
            public int defaultLevelRear;
            public int maxLevelFront;
            public int maxLevelRear;
        }

        class Dimenesions
        {
            public string name;
            public int length;
            public int width;
            public int wheelbase;
            public int weight;
        }
    }
}