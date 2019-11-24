#ifndef GTEXP_STRUCTS_H
#define GTEXP_STRUCTS_H

#pragma once

#include <vector>
#include <map>
#include <set>

#define CONCAT2(a, b) a ## b
#define CONCAT(a, b) CONCAT2(a, b)
#define C_ASSERT(e) static const char CONCAT(n, __COUNTER__) [(e) ? 1 : -1];

/*
Structs in file order:
CarBrakes // 1st set of offset, size pairs, 0xc in size
CarBrakeBalanceController // 2nd set of offset, size pairs, 0x10 in size
// 3rd offset, size pair = Car Steering? one entry for all cars.
// set to all FF makes steering super responsive, set to all 0 = unable to turn
CarDimensions // 4th offset, size pair = this (not for every car, overrides for CarEngineData below?) size 0x14
CarWeightReduction // 5th offset, size pair = Weight reductions
CarRacingModification // 0x1c - 6th offset, size pair. Can be multiple per car
CarEngineData // 0x4c size, 7th pair of offset, size in gtmode_data.dat
CarPortGrinding // 0xc, 8th offset, size pairs
CarEngineBalancing // 0xc, 9th offset, size
CarDisplacementIncrease // 0xc size, 10th offset, size pair
CarChip // 0xc size. 11th offset, size pair
CarNATuning // 0xc size, 12th offset, size pair. Can have multiple entries per car
CarTurboTuning // 0x14 size, 13th offset, size pair.
CarDrivetrain // 14th pair of offset, size pairs in gtmode_data.dat. 0x10 size
CarFlywheel // 15th pair of offset, size pairs. 0xc size
CarClutch // 16th pair, 0x10 size. Can be multiple for each carCarPropShaft // 17th pair, 0xc size
CarGearbox // 18th, 0x24, can be more than 1 for each car
CarSuspension // 19th, 0x4c size
CarIntercooler // 20th, 0xc. Can have multiple per car
CarMuffler // 21st, 0xc.
CarLSD // 22nd, 0x20
CarTyres // 23rd, 0x10, multiple per car
CarRearTyres // 24th, 0xc can have multiple per car.
CarData // 0x48 bytes - last offset, size pair in the file
*/

#include <pshpack1.h>
struct CarBrakes // 1st set of offset, size pairs, 0xc in size
{
	int carId;
	int price; // sports brakes price, x100 for JP. 0 = not available
	unsigned char stage; // 1 = sports, 0 = stock. needs this to be 1 and a non-zero price to be purchased
	unsigned char unk2;
	unsigned short unk3;
};

C_ASSERT(sizeof(CarBrakes) == 0xc);

struct CarBrakeBalanceController // 2nd set of offset, size pairs, 0x10 in size
{
	int carId;
	int price; // controller price, x100 for JP. > 0x7fffffff or 0 = not available
	unsigned char stage; // 1 = bought, 0 = stock. needs this to be 1 and a non-zero price to be purchased
	unsigned char unk2;
	unsigned short unk3;
	unsigned int unk4;
};

C_ASSERT(sizeof(CarBrakeBalanceController) == 0x10);

// 3rd offset, size pair = Car Steering? one entry for all cars.
// set to all FF makes steering super responsive, set to all 0 = unable to turn

// 4th offset, size pair = this (not for every car, overrides for CarEngineData below?)
struct CarDimensions // 0x14
{
	int carId;
	int unk; // 0x4 (prob 2 shorts)
	unsigned short length; // 0x8
	unsigned short height; // 0xa
	unsigned short unk2; // 0xc // wheelbase in gt3
	unsigned short weight; // 0xe in kgs
	unsigned char rmWeightMultipier; // RM weight multiplier
	unsigned char unk4;
	unsigned short unk5;
};

C_ASSERT(sizeof(CarDimensions) == 0x14);

// 5th offset, size pair = Weight reductions
struct CarWeightReduction // 0xc - can have more than one per car
{
	int carId;
	int price; // x100 for JP, lsb must be non-zero to be installable, e.g. 0x1000 price = no, 0x1010 = yes
	unsigned short newWeight; // in kg
	unsigned char unk; // both bytes need to be non-zero to be installable
	unsigned char stage; // not necessarily in order. E.g. stage 3 followed by stage 1 then 2 etc
};

C_ASSERT(sizeof(CarWeightReduction) == 0xc);

// Prius data:
// 18 A7 0D 1E 00 00 00 00 18 A7 0D 1E 64 64 00 1E 
// 0A 0A 0A 10 10 10 C3 05 C8 05 9F 06
struct CarRacingModification // 0x1c - 6th offset, size pair. Can be multiple per car
{
	int carId;
	int price; // if 0 or low byte = 0, not possible
	int rmBodyId; // e.g. if car id ends with 0x8, rmCarId will end with 0xc, d, e etc
	// weight is a multiple of some car-indepenent value
	// for the Demio A-Spec it's multiples of 7.54 for final kg (truncate, don't round).
	// e.g. if the demio's rm weight is 754, this weight value will be 100
	unsigned char weight;
	unsigned char unk;
	unsigned char available;
	unsigned char unk2;
	unsigned short unk3;
	unsigned char defaultFrontDownforce;
	unsigned char unk4;
	unsigned char unk5;
	unsigned char defaultRearDownforce;
	unsigned short unk6;
	unsigned short unk7;
	unsigned short width; // rm car width - in mm
};

C_ASSERT(sizeof(CarRacingModification) == 0x1c);

// 0x1f7dc for Escudo in JP1.0, 0x4c size, 7th pair of offset, size in gtmode_data.dat
// RX-7 Type RB engine data (unk) at 800F1590
// copied to 801ffc08
// Demio A-Spec engine data = 
// D8 F5 00 0B 36 00 37 00 38 00 0A 00 87 03 D8 03
// 16 04 3B 04 54 04 95 04 D5 04 14 05 F8 04 DF 04
// AA 04 48 04 DD 03 FF FF FF FF FF FF DA 05 64 00
// 58 02 82 00 39 00 64 FA 3C 46 46 0A 0F 14 19 1E
// 23 28 2D 32 37 3C 41 46 FF FF FF 0D
struct CarEngineData
{
	int carId; // 0
	unsigned short engineTypeName; // V6 etc, index into unistringdb, 0x4
	unsigned short engineTypeName2; // DOHC etc, index into unistringdb, 0x6
	unsigned short unk3; // 
	unsigned short unk4; // 
	unsigned short bandAcceleration[(0x2c - 0xc) / 2]; // 0xc - all these take part in garage HP calculation
	unsigned short engineCc; // 0x2c - not quite for those that have numberx2 displacements
	unsigned short ps; // 0x2e - base ps. F. Ex. is 295hp for most of the rally cars, not 400+
	unsigned short maxPowerRPM; // 0x30 - multipled by 10 by the game. E.g. a value of 850 is displayed as 8500
	unsigned short torqueKGM; // 0x32 - divided by 10 by the game. e.g. a value of 950 is displayed as 95.0
	unsigned short torqueRPM; // 0x34 - index into unistringdb!
	unsigned char baseUnits[5]; // 0x36 - used in hp calculation
	unsigned char bandRPMs[0x4b - 0x3b]; // 0x3b - rpms that the bandAcceleration values are for
	unsigned char significantUnks; // the number of values in both unk arrays that are used in hp calxulations
};

C_ASSERT(sizeof(CarEngineData) == 0x4c);

struct CarPortGrinding // 0xc, 8th offset, size pairs
{
	int carId;
	int price; // low byte must be set
	unsigned char available; // must be 1 to be bought
	unsigned char powerAdjustor1; // same adjustments as engine balancing
	unsigned char powerAdjustor2; // unk
	unsigned char powerAdjustor3; // unk
};

C_ASSERT(sizeof(CarPortGrinding) == 0xc);

struct CarEngineBalancing // 0xc, 9th offset, size
{
	int carId;
	int price;
	unsigned char available;
	unsigned char engineModifier; // unk
	unsigned char unk; // unk
	unsigned char powerAdjustor; 
	// power adjustors are non-constant multipliers
	// e.g. a 10 for one car will give different power change than 10 for another
	// for example:
	// dodge stratus = .82
	// if set to 0, 0, 2, 100hp becomes 103
	// if set to 0, 0, 50, 100hp becomes 142
	// if set to 0, 0, 100, 100hp becomes 182
	// mazda demio a-spec = 1.03
	// if set to 0, 0, 10, 99hp becomes 109
	// if set to 0, 0, 25, 99hp becomes 125
	// if set to 0, 0, 50, 99hp becomes 151
	// if set to 0, 0, 100, 99hp becomes 202
	//
};

C_ASSERT(sizeof(CarEngineBalancing) == 0xc);

struct CarDisplacementIncrease // 0xc size, 10th offset, size pair
{
	int carId;
	int price;
	unsigned char available;
	unsigned char powerAdjustor1; // same adjustments as engine balancing
	unsigned char powerAdjustor2; // unk
	unsigned char powerAdjustor3; // unk
};

C_ASSERT(sizeof(CarDisplacementIncrease) == 0xc);

struct CarChip // 0xc size. 11th offset, size pair
{
	int carId;
	int price;
	unsigned char available;
	unsigned char powerAdjustor1; // same adjustments as engine balancing
	unsigned char powerAdjustor2; // unk
	unsigned char powerAdjustor3; // unk
};

C_ASSERT(sizeof(CarChip) == 0xc);

struct CarNATuning // 0xc size, 12th offset, size pair. Can have multiple entries per car
{
	int carId;
	int price;
	unsigned char stage; // 2 for stage 2, etc. If multiple of the same, first in file takes precendence
	unsigned char engineModifier;
	unsigned char unk;
	unsigned char powerAdjustor; // same as engine balancing
};

C_ASSERT(sizeof(CarNATuning) == 0xc);

struct CarTurboTuning // 0x14 size, 13th offset, size pair.
{
	int carId;
	int price;
	unsigned char stage; // 2 for stage 2, etc. If multiple of the same, first in file takes precendence
	unsigned char unk1;
	unsigned char unk2;
	unsigned char unk3;
	int unk4;
	unsigned char unk5;
	unsigned char unk6;
	unsigned char powerAdjustor; // same as engine balancing
	unsigned char unk7;
};

C_ASSERT(sizeof(CarTurboTuning) == 0x14);

struct CarDrivetrain // 14th pair of offset, size pairs in gtmode_data.dat. 0x10 size
{
	int carId;
	int unk; // prob 4 chars but whatevs
	unsigned char driveType; // 0 = FR, 1 = FF, 2 = 4WD, 3 = MR, 4 = RR
	unsigned char unk2;
	unsigned short unk3;
	unsigned int unk4;
};

C_ASSERT(sizeof(CarDrivetrain) == 0x10);

struct CarFlywheel // 15th pair of offset, size pairs. 0xc size
{
	int carId;
	int price;
	unsigned char stage; // 1 = sports, 2 = semi-racing, 3 = racing
	unsigned char unk1;
	unsigned char unk2;
	unsigned char unk3;
};

C_ASSERT(sizeof(CarFlywheel) == 0xc);

struct CarClutch // 16th pair, 0x10 size. Can be multiple for each car
{
	int carId;
	int price; // 0 = standard clutch
	unsigned char stage; // 0 = standard, 1 = single, 2 = twin, 3 = triple plate
	unsigned char unk1;
	unsigned char unk2;
	unsigned char unk3;
	unsigned int unk4;
};

C_ASSERT(sizeof(CarClutch) == 0x10);

struct CarPropShaft // 17th pair, 0xc size
{
	int carId;
	int price;
	unsigned char available; // available / stage 1
	unsigned char unk1; // probably acceleration / weight modifiers
	unsigned char unk2;
	unsigned char unk3;
};

C_ASSERT(sizeof(CarPropShaft) == 0xc);

struct CarGearbox // 18th, 0x24, can be more than 1 for each car
{
	int carId;
	int price;
	unsigned char type; // 0 = N/A / stock, 1 = close, 2 = super close, 3 = fully customised
	unsigned char unk1;
	unsigned char unk2[16]; // sometimes all 0xff
	unsigned short finalGear; // 0x1a
	int unk3; // 0x1c
	int unk4; // 0x20
};

C_ASSERT(sizeof(CarGearbox) == 0x24);

struct CarSuspension // 19th, 0x4c size
{
	int carId;
	int price;
	unsigned char stage; // 0 = NA / Stock, 1 = Sports, 2 = semi, 3 = professional
	unsigned char unk1;
	unsigned short unk2;
	unsigned char unk3[0x4c - 0xc]; // possible default settings? don't know how they're stored
};

C_ASSERT(sizeof(CarSuspension) == 0x4c);

struct CarIntercooler // 20th, 0xc. Can have multiple per car
{
	int carId;
	int price;
	unsigned char stage; // 1 = sports, 2 = racing
	unsigned char powerAdjustor1;
	unsigned char powerAdjustor2;
	unsigned char powerAdjustor3; // same as engine balancing
};

C_ASSERT(sizeof(CarIntercooler) == 0xc);

struct CarMuffler // 21st, 0xc.
{
	int carId;
	int price;
	unsigned char stage; // 1 = sports, 2 = semi-racing, 3 = racing
	unsigned char powerAdjustor1;
	unsigned char powerAdjustor2;
	unsigned char powerAdjustor3; // same as engine balancing
};

C_ASSERT(sizeof(CarMuffler) == 0xc);

struct CarLSD // 22nd, 0x20
{
	int carId;
	int price;
	unsigned char stage; // 0 = NA / Stock, 1 = 1 Way LSD, 2 = 2 Way LSD, 3 = 1.5 Way LSD, 4 = Fully Customised, 5 = YAW
	unsigned char unk1;
	unsigned short unk2;
	unsigned char unk3[0x14]; // unknown format
};

C_ASSERT(sizeof(CarLSD) == 0x20);

struct CarTyres // 23rd, 0x10, multiple per car
{
	int carId;
	int price;
	unsigned char type; // 0 = stock, 1 = sports, 2 = hard, 3 = medium, 4 = soft, 5 = super soft, 6 = sim, 7 = dirt
	unsigned char unk1; // always 0
	unsigned short unk2; // seems to be tyre size - 0x7fff = bigger tyres, 0xffff = no tires (can't turn)
	unsigned int unk3; // also tyre type. 0x10 for Sim, 0 for stock, 0x2 for sports, 4 for Hard, 6 for medium, 8 for soft, 0xa for super soft
};

C_ASSERT(sizeof(CarTyres) == 0x10);

struct CarRearTyres // 24th, 0xc can have multiple per car.
{
	int carId;
	unsigned char type;
	unsigned char unk1;
	unsigned short unk2;
	unsigned int unk3; // also tyre type. 0x10 for Sim, 0 for stock, 0x2 for sports, 4 for Hard, 6 for medium, 8 for soft, 0xa for super soft
};

C_ASSERT(sizeof(CarRearTyres) == 0xc);

// most of these names come from Pupik's site - http://pupik-gt2.tripod.com/hybrids/basecodes.htm
// since that's which parts of the car data they become when its in your garage
struct CarData // 0x48 bytes - last offset, size pair in the file
{
	int carId; // standard thing (0)
	short brakePart; // (4)
	int specialCar; // 1 for special cars, 0 for not
	unsigned short weightPartId; // (a)
	unsigned short unk3; // (c)
	unsigned short weightDistribution;// (e)
	unsigned short enginePartId; // (10)
	unsigned short unk4; // 12
	int unk5; // 16
	int naPart; // 1a - this and the last one are probably 4 shorts, but they're always 0 so it doesn't really matter
	unsigned short turboPart; // 1c
	unsigned short drivetrainPart; // 1e
	unsigned short flywheel; // 20
	unsigned short clutch; // 22
	unsigned short unk8; // 24
	unsigned short differential; // 26
	unsigned short transmission; // 28
	unsigned short suspension; // 2a
	unsigned int unk9; // 2c
	unsigned short frontTyres; // 30
	unsigned short rearTyres; // 32
	unsigned int unk10; // 34
	unsigned short rimsCode3; // 38
	unsigned short manufacturerID; // 0x3a
	unsigned short nameFirstPart; // 0x3c unistrdb index
	unsigned short nameSecondPart; // 0x3e unistrdb index
	unsigned char padding; // 0x40 (1 for special  cars, 0 for not)
	unsigned char year; // 0x41 
	unsigned short unk14; // 42
	unsigned newPrice; // 0x44
};

struct CarRealValues
{
	int carId;
	unsigned absoluteRMWeight;
	unsigned short realPs;
	unsigned short realHp;
};

C_ASSERT(sizeof(CarData) == 0x48);

#include <poppack.h>

struct CarDataWithName
{
	CarData cd;
	std::wstring firstName;
	std::wstring secondName;
};

struct CarColourInfo
{
	int carId;
	std::wstring colourName;
	unsigned char byteId;
	unsigned short mainColour; //BGR555
};

// All data in file
struct CarDB
{
	std::vector<CarBrakes> brakes;
	std::vector<CarBrakeBalanceController> controllers;
	std::map<int, CarDimensions> dimensions;
	std::vector<CarWeightReduction> weightReds;
	std::vector<CarRacingModification> racingMods;
	std::map<int, CarEngineData> engines;
	std::vector<CarPortGrinding> portGrinds;
	std::vector<CarEngineBalancing> engineBalances;
	std::vector<CarDisplacementIncrease> displacements;
	std::vector<CarChip> chips;
	std::vector<CarNATuning> naTunes;
	std::vector<CarTurboTuning> turboTunes;
	std::map<int, CarDrivetrain> drivetrains;
	std::vector<CarFlywheel> flywheels;
	std::vector<CarClutch> clutches;
	std::vector<CarPropShaft> propShafts;
	std::vector<CarGearbox> gearboxes;
	std::vector<CarSuspension> suspensions;
	std::vector<CarIntercooler> intercoolers;
	std::vector<CarMuffler> mufflers;
	std::vector<CarLSD> slipDiffs;
	std::vector<CarTyres> frontTyres;
	std::vector<CarRearTyres> rearTyres;
	std::map<int, CarDataWithName> cars;
	std::map<int, CarRealValues> realValues;
	std::map<int, std::vector<CarColourInfo> > carColours;
	std::map<int, std::wstring> carInfoRaceNames;
};

#include <pshpack1.h>
struct RaceInfo // 0x9c size
{
	unsigned short raceNameIndex; // 0
	unsigned short trackNameId; // 2
	unsigned int opponentIndex1; // 4
	unsigned int opponentIndex2; // 8
	unsigned int opponentIndex3; // 0xc
	unsigned int opponentIndex4; // 0x10
	unsigned int opponentIndex5; // 0x14
	unsigned int opponentIndex6; // 0x18
	unsigned int opponentIndex7; // 0x1c
	unsigned int opponentIndex8; // 0x20
	unsigned int opponentIndex9; // 0x24
	unsigned int opponentIndex10; // 0x28
	unsigned int opponentIndex11; // 0x2c
	unsigned int opponentIndex12; // 0x30
	unsigned int opponentIndex13; // 0x34
	unsigned int opponentIndex14; // 0x38
	unsigned int opponentIndex15; // 0x3c
	unsigned int opponentIndex16; // 0x40
	unsigned char rollingStartSpeed; // (0x44) - 0 = normal standing start
	unsigned char laps; // (0x45)
	unsigned char unk17; // (0x46) - 1 = no control of car (but camera change, pausing still work)
	unsigned char licence; // 1 = B, 2 = A, 3 = IC, 4 = IB, 5 = IA, 6 = S (0x47)
	unsigned char unk3[17]; // 17 0x64's (0x48), changing to all 0 = nothing
	unsigned short unk4; // (0x59) - changing to 0x100 didn't seem to do anything, likewise 0x1
	unsigned char unk5; // (0x5b) - changing  it to 0 or 0xff doesn't seem to do anything
	unsigned short unk6; // (0x5c)
	unsigned char unk7; // (0x5e) 
	unsigned char unk8; // (0x5f)
	unsigned short unk9; // (0x60)
	unsigned short unk10; // 0x6400 above (0x62) - changed unk4 to unk10 to FF, spaced out opponents (power related?), changed all to 0, same as ff really
	unsigned int unk11; // (0x64)
	unsigned int unk12; // (0x68)
	unsigned int unk13; // (0x6c)
	unsigned int unk14; // (0x70)
	unsigned char unk15; // (0x74)
	unsigned char rally; // 0x75 set to 1 for rally race - requires dirt tyres, only 1 opponent. Can award prize car
	unsigned char allowedEntrantsId; // (0x75) - index into allowable entrants list
	unsigned char forcedDriveTrainFlags; // (0x76) 1 = FF, 2 = FR, 3 = MR, 4 = RR, 5 = 4WD
	unsigned short prizeMoney1st; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x78)
	unsigned short prizeMoney2nd; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x7a)
	unsigned short prizeMoney3rd; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x7c)
	unsigned short prizeMoney4th; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x7e)
	unsigned short prizeMoney5th; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x80)
	unsigned short prizeMoney6th; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x82)
	unsigned int prizeCars[4]; // ids of cars (0x84)
	unsigned short unk16; // (0x94)
	unsigned short hpRestriction; // in ps units (hp = ps / 1.01427772651) (0x96)
	unsigned short seriesChampBonus; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x98)
	unsigned short carFlags; // (0x9a) flags to restrict the type of car you use for this race. 0x100 = non-race car, 0x200 = just race car
};

C_ASSERT(sizeof(RaceInfo) == 0x9c);

struct OpponentInfo // 0x60
{
	unsigned int carId; // standard thing (0)
	unsigned short brakePart; // (4)
	unsigned int specialCar; // 1 for special cars, 0 for not
	unsigned short weightPartId; // (a)
	unsigned short unk3; // (c)
	unsigned short rmPartId;// (e)
	unsigned short enginePartId; // (10)
	unsigned short unk4; // 12
	unsigned int unk5; // 16
	unsigned int naPart; // 1a
	unsigned short turboPart; // 1c
	unsigned short drivetrainPart; // 1e
	unsigned short flywheel; // 20
	unsigned short clutch; // 22
	unsigned short unk8; // 24
	unsigned short differential; // 26
	unsigned short transmission; // 28
	unsigned short suspension; // 2a
	unsigned int unk9; // 2c
	unsigned short frontTyres; // 30
	unsigned short rearTyres; // 32
	unsigned int unk10; // 34
	unsigned short rimsCode3; // 38
	unsigned short unk11; // 0x3a
	unsigned short unk12; // 0x3c
	unsigned short unk13; // 0x3e
	unsigned int unk14; // 0x40 - at least one byte of this seems to control colour
	unsigned short unk15; // 0x44
	unsigned short unk15a; // 0x46
	unsigned int unk16; // 0x48
	unsigned short unk17; // 0x4c
	unsigned short unk18; // 0x4e
	unsigned short unk19; // 0x50
	unsigned short unk20; // 0x52
	unsigned short unk21; // 0x54
	unsigned short unk22; // 0x56
	unsigned short unk23; // 0x58
	unsigned short unk24; // 0x5a
	unsigned char powerPercentage; // 0x5c
	unsigned char unk25; // 0x5d
	unsigned short opponentId; // 0x5e
};
C_ASSERT(sizeof(OpponentInfo) == 0x60);

struct TrackDetails
{
	std::string name;
	unsigned int hashId;
	unsigned short unk;
	unsigned short unk2;
	unsigned short unk3;
	unsigned short unk4;
	unsigned short unk5;
	unsigned short unk6;
	unsigned short unk7;
	unsigned short unk8;
};

#include <poppack.h>

struct RaceDB
{
	std::vector<RaceInfo> races;
	std::map<unsigned, OpponentInfo> opponents;
	std::vector<std::vector<int> > carRestrictions;
	std::vector<std::string> raceStrings;
	std::vector<TrackDetails> tracks;
	std::set<unsigned> unusedOpponentIds;
};

#include <pshpack1.h>
struct LicenseTestInfo // 0x9c size, 31st offset, size pair in (eng_)license_data.dat
{
	unsigned short testNameIndex; // 0
	unsigned short trackNameId; // 2
	// all these 'opponentIndex'-es are unknown. 
	// Some do have values, but zeroing them doesn't
	// seem to do anything
	unsigned int opponentIndex1; // 4
	unsigned int opponentIndex2; // 8
	unsigned int opponentIndex3; // 0xc
	unsigned int opponentIndex4; // 0x10
	unsigned int opponentIndex5; // 0x14
	unsigned int opponentIndex6; // 0x18
	unsigned int opponentIndex7; // 0x1c
	unsigned int opponentIndex8; // 0x20
	unsigned int opponentIndex9; // 0x24
	unsigned int opponentIndex10; // 0x28
	unsigned int opponentIndex11; // 0x2c
	unsigned int opponentIndex12; // 0x30
	unsigned int opponentIndex13; // 0x34
	unsigned int opponentIndex14; // 0x38
	unsigned int opponentIndex15; // 0x3c
	unsigned int opponentIndex16; // 0x40
	unsigned char rollingStartSpeed; // (0x44) - 0 = normal standing start
	unsigned char laps; // (0x45) - sometimes has a value, no effect
	unsigned char unk17; // (0x46) - 1 = no control of car (but camera change, pausing still work)
	unsigned char licence; // 1 = B, 2 = A, 3 = IC, 4 = IB, 5 = IA, 6 = S (0x47)
	unsigned char unk3[17]; // 17 0x64's (0x48), changing to all 0 = nothing
	unsigned short unk4; // (0x59) - changing to 0x100 didn't seem to do anything, likewise 0x1
	unsigned char unk5; // (0x5b) - changing  it to 0 or 0xff doesn't seem to do anything
	unsigned int unk11; // (0x5c)
	unsigned int unk12; // (0x60)
	unsigned int unk13; // (0x64)
	unsigned int unk14; // (0x68)
	unsigned short goldTime; // (0x6c) - BIG ENDIAN BCD!! top byte is whole seconds, lower is number of hundredths
	unsigned short silverTime; // (0x6e) 
	unsigned short bronzeTime; // (0x70)
	unsigned short kidsTime; // (0x72)
	unsigned char kidAttempts; // (0x74) number of times to fail to achieve the kid medal
	unsigned char rally; // 0x75 set to 1 for rally race - requires dirt tyres, only 1 opponent. Can award prize car
	unsigned char allowedEntrantsId; // (0x75) - index unsigned into allowable entrants list
	unsigned char forcedDriveTrainFlags; // (0x76) 1 = FF, 2 = FR, 3 = MR, 4 = RR, 5 = 4WD
	unsigned short prizeMoney1st; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x78)
	unsigned short prizeMoney2nd; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x7a)
	unsigned short prizeMoney3rd; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x7c)
	unsigned short prizeMoney4th; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x7e)
	unsigned short prizeMoney5th; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x80)
	unsigned short prizeMoney6th; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x82)
	unsigned int prizeCars[4]; // ids of cars (0x84)
	unsigned short unk16; // (0x94)
	unsigned short hpRestriction; // in ps units (hp = ps / 1.01427772651) (0x96)
	unsigned short seriesChampBonus; // multiply by 100 for non-JP / multiply by 10,000 for JP (0x98)
	unsigned short carFlags; // (0x9a) flags to restrict the type of car you use for this race. 0x100 = non-race car, 0x200 = just race car
};

#include <poppack.h>

#endif
