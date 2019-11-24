#pragma managed(off)

#define _CRT_SECURE_NO_WARNINGS
#include <fstream>
#include <iostream>
#include <cstdio>
#include <string>
#include <vector>
#include <set>
#include <map>
#include <sstream>
#include <iomanip>
#include <functional>
#include <algorithm>
#include <direct.h>
#include "structs.h"

typedef long long int64_t;

typedef void (*GTModeDataSectionParser)(std::ifstream& file, int size, CarDB& cp);

#define CreateParser(thing, cpEntry) \
	void Parse##thing(std::ifstream& file, int size, CarDB& cp) \
	{ \
		std::vector<thing>& data = cp.cpEntry; \
		thing loaded = {0}; \
		data.reserve(size / sizeof(loaded)); \
		while(size > 0) \
		{ \
			file.read((char*)&loaded, sizeof(loaded)); \
			data.push_back(loaded); \
			size -= sizeof(loaded); \
		} \
	}

#define CreateMapParser(thing, cpEntry) \
	void Parse##thing(std::ifstream& file, int size, CarDB& cp) \
	{ \
		std::map<int, thing>& data = cp.cpEntry; \
		thing loaded = {0}; \
		while(size > 0) \
		{ \
			file.read((char*)&loaded, sizeof(loaded)); \
			data.insert(std::make_pair(loaded.carId, loaded)); \
			size -= sizeof(loaded); \
		} \
	}

CreateParser(CarBrakes, brakes);
CreateParser(CarBrakeBalanceController, controllers);
CreateMapParser(CarDimensions, dimensions);
CreateParser(CarWeightReduction, weightReds);
CreateParser(CarRacingModification, racingMods);
CreateMapParser(CarEngineData, engines);
CreateParser(CarPortGrinding, portGrinds);
CreateParser(CarEngineBalancing, engineBalances);
CreateParser(CarDisplacementIncrease, displacements);
CreateParser(CarChip, chips);
CreateParser(CarNATuning, naTunes);
CreateParser(CarTurboTuning, turboTunes);
CreateMapParser(CarDrivetrain, drivetrains);
CreateParser(CarFlywheel, flywheels);
CreateParser(CarClutch, clutches);
CreateParser(CarPropShaft, propShafts);
CreateParser(CarGearbox, gearboxes);
CreateParser(CarSuspension, suspensions);
CreateParser(CarIntercooler, intercoolers);
CreateParser(CarMuffler, mufflers);
CreateParser(CarLSD, slipDiffs);
CreateParser(CarTyres, frontTyres);
CreateParser(CarRearTyres, rearTyres);

void ParseCars(std::ifstream& file, int size, CarDB& cp)
{
	std::map<int, CarDataWithName>& data = cp.cars;
	CarDataWithName loaded = {0};
	while(size > 0)
	{
		file.read((char*)&loaded.cd, sizeof(loaded.cd));
		data.insert(std::make_pair(loaded.cd.carId, loaded));
		size -= sizeof(loaded.cd);
	}
}

void ParseNothing(std::ifstream&, int, CarDB&)
{
	// do nothing
}

// ParseNothing is where I don't know what the data is, or there's only a single default part
GTModeDataSectionParser g_sectionParsers[] = {
	ParseCarBrakes, ParseCarBrakeBalanceController, ParseNothing, ParseCarDimensions,
	ParseCarWeightReduction, ParseCarRacingModification, ParseCarEngineData, ParseCarPortGrinding, 
	ParseCarEngineBalancing, ParseCarDisplacementIncrease, ParseCarChip, ParseCarNATuning, ParseCarTurboTuning, 
	ParseCarDrivetrain, ParseCarFlywheel, ParseCarClutch, ParseCarPropShaft, ParseCarGearbox, ParseCarSuspension, 
	ParseCarIntercooler, ParseCarMuffler, ParseCarLSD, ParseCarTyres, ParseCarRearTyres, 
	ParseNothing, ParseNothing, ParseNothing, ParseNothing, ParseNothing, ParseNothing, ParseCars
};

struct FindStage3Weight
{
	int id;
	FindStage3Weight(int carId) : id(carId) {}
	bool operator()(const CarWeightReduction& weight) const
	{
		return (weight.carId == id) && (weight.stage == 3);
	}
};

unsigned PSToHp(unsigned ps);

unsigned CalculateRealPS(
	const unsigned short* pEngineData,
	unsigned numShorts, 
	const unsigned char* pBaseUnits,
	const unsigned char* pEngineUnkBytes,
	const unsigned* pPowerAdjustments,
	unsigned numPowerAdjustments,
	unsigned sumOfEngineModifiers
);

void WorkOutRealData(CarDB& carDb)
{
	std::map<int, CarRealValues> realValueDb;
	const std::vector<CarRacingModification>& cm = carDb.racingMods;
	const std::map<int, CarDimensions>& dm = carDb.dimensions;
	const std::vector<CarWeightReduction>& weightReds = carDb.weightReds;
	std::vector<CarWeightReduction>::const_iterator weightRedSearchStart = weightReds.begin(), weightRedsEnd = weightReds.end();
	for(size_t i = 0; i < cm.size(); ++i)
	{
		const CarRacingModification& crm = cm[i];
		if(!crm.available)
		{
			continue;
		}
		std::vector<CarWeightReduction>::const_iterator stage3Weight = std::find_if(weightRedSearchStart, weightRedsEnd, FindStage3Weight(crm.carId));
		int normalRMId = (crm.carId - (crm.carId & 0x3));
		int nonRmId = (crm.carId - (crm.carId & 0x7));
		if(stage3Weight == weightRedsEnd)
		{
			stage3Weight = std::find_if(weightRedSearchStart, weightRedsEnd, FindStage3Weight(normalRMId));
			if(stage3Weight == weightRedsEnd)
			{
				stage3Weight = std::find_if(weightRedSearchStart, weightRedsEnd, FindStage3Weight(nonRmId));
			}
		}
		unsigned newWeight = 0;
		if(stage3Weight != weightRedsEnd)
		{
			std::map<int, CarDimensions>::const_iterator carIter = dm.find(crm.carId);
			if(carIter == dm.end())
			{
				if((carIter = dm.find(normalRMId)) == dm.end())
				{
					carIter = dm.find(nonRmId);
				}
			}
			weightRedSearchStart = stage3Weight;
			unsigned intermed = (stage3Weight->newWeight * carIter->second.weight) / 1000;
			newWeight = (((crm.weight * 10) * intermed) / 1000);
		}
		else
		{
			std::map<int, CarDimensions>::const_iterator carIter = dm.find(crm.carId);
			if(carIter == dm.end())
			{
				carIter = dm.find(normalRMId);
			}
			newWeight = carIter->second.weight;
		}
		CarRealValues crv = {crm.carId};
		crv.absoluteRMWeight = newWeight;
		realValueDb[crm.carId] = crv;
	}
	std::map<int, CarEngineData>::const_iterator engIter = carDb.engines.begin(), engEnd = carDb.engines.end();
	while(engIter != engEnd)
	{
		const CarEngineData& ced = engIter->second;
		const unsigned unkArraySize = ced.significantUnks;

		unsigned realPs = CalculateRealPS(ced.bandAcceleration, unkArraySize, ced.baseUnits, ced.bandRPMs, NULL, 0, 0);
		unsigned realHp = PSToHp(realPs);
		std::map<int, CarRealValues>::iterator crvIter = realValueDb.find(ced.carId);
		CarRealValues crv = {ced.carId};
		if(crvIter != realValueDb.end())
		{
			crv = crvIter->second;
		}
		crv.realPs = realPs;
		crv.realHp = realHp;
		realValueDb[ced.carId] = crv;
		++engIter;
	}
	carDb.realValues.swap(realValueDb);
}

void ExtractCars(const std::wstring& gtModeDataFile, CarDB& carDb)
{
	// gtdt format - numbers at top are pairs of offsets + sizes
	std::ifstream str(gtModeDataFile.c_str(), std::ios::binary);
	if(!str)
	{
		std::wcout << L"Couldn't open " << gtModeDataFile << L". Car information will not be dumped\n";
		return;
	}
	char header[6];
	str.read(header, sizeof(header)); // GTDT1\0
	short numShorts = 0;
	str.read((char*)&numShorts, sizeof(numShorts)); // number of individual offset & size ints, not number of pairs
	short numSections = numShorts / 2;
	for(short i = 0; i < numSections; ++i)
	{
		int offset = 0, size = 0;
		str.read((char*)&offset, sizeof(offset));
		str.read((char*)&size, sizeof(size));
		std::streamoff curPos = str.tellg();
		str.seekg(offset);
		g_sectionParsers[i](str, size, carDb);
		str.seekg(curPos);
	}
	WorkOutRealData(carDb);
}

typedef std::vector<std::wstring> WideStringCollection;

void ExtractUniStrDb(const std::wstring& uniStrDb, WideStringCollection& stringList)
{
	std::ifstream str(uniStrDb.c_str(), std::ios::binary);
	int fileSize;
	str.read((char*)&fileSize, sizeof(int));
	char header[4];
	str.read(header, sizeof(header)); // WSDB
	short numStrings = 0;
	str.read((char*)&numStrings, sizeof(numStrings));
	stringList.reserve(numStrings);
	for(short i = 0; i < numStrings; ++i)
	{
		short chars = 0;
		str.read((char*)&chars, sizeof(chars));
		if(chars > 0)
		{
			std::wstring thisStr(chars, 0);
			str.read((char*)&thisStr[0], chars * 2);
			stringList.push_back(thisStr);
		}
		else
		{
			stringList.push_back(std::wstring(L"-"));
		}
		str.read(header, 2); // the null
	}
}

void GetAllUniStringLists(
	const std::wstring& volDir,
	WideStringCollection& japStrings,
	WideStringCollection& engStrings, 
	WideStringCollection& usaStrings
)
{
	std::wstring decompDir = volDir + L"\\carparam\\decomp\\";
	ExtractUniStrDb(decompDir + L"jpn_unistrdb.dat", japStrings);
	ExtractUniStrDb(decompDir + L"eng_unistrdb.dat", engStrings);
	ExtractUniStrDb(decompDir + L"usa_unistrdb.dat", usaStrings);
}

void ExtractEntrants(const std::string& volDir)
{
	std::ifstream str((volDir + "\\carparam\\decomp\\gtmode_race.dat").c_str(), std::ios::binary);
	str.seekg(0x25d18);
	std::map<int, std::string> cars;
	{
		std::ifstream carNames("C:\\Users\\Adrian\\Downloads\\GT2\\carcodes-0.txt");
		std::string id, name;
		int hex;
		while(carNames >> id >> std::hex >> hex)
		{
			std::getline(carNames, name);
			cars[hex] = name;
		}
	}
	const int nextBlockSize = 0x80;
	char nextBlock[nextBlockSize];
	int thisBlockRead = 0;
	int carHex = 0;
	bool haveSeen = false;
	std::ofstream carEntry("C:\\Users\\Adrian\\Downloads\\GT2\\carEntrants-jp1.0.txt");
	while(str.read((char*)&carHex, sizeof(carHex)))
	{
		thisBlockRead += sizeof(carHex);
		if(carHex != 0)
		{
			std::map<int, std::string>::iterator it = cars.find(carHex);
			if(it != cars.end())
			{
				if(!haveSeen)
				{
					carEntry << "Cars for this race:\n";
					haveSeen = true;
				}
				carEntry << it->second << '\n';
			}
			else carEntry << "Invalid id: " << std::hex << carHex << '\n';
		}
		else
		{
moveToNextBlock:
			str.read(nextBlock, nextBlockSize - thisBlockRead);
			carEntry << '\n' << std::endl;
			haveSeen = false;
			thisBlockRead = 0;
		}
	}
}

void DumpText(const std::wstring& file, const WideStringCollection& strings, CarDB& carDb)
{
	typedef std::map<int, CarDataWithName> CarList;
	CarList::iterator iter = carDb.cars.begin(), end = carDb.cars.end();
	std::ofstream txtFile(file.c_str(), std::ios::binary);
	const wchar_t headers[] = L"CarId\tFirstName\tSecondName\n";
	txtFile.write("\xFF\xFE", 2);
	txtFile.write((char*)headers, sizeof(headers) - 2);
	while(iter != end)
	{
		std::wostringstream str;
		CarData& cd = iter->second.cd;
		str << std::hex << iter->first << L'\t';
		const std::wstring& firstName = strings[cd.nameFirstPart];
		std::wstring secondName = strings[cd.nameSecondPart];
		if(secondName == L"-")
		{
			secondName = L"";
		}
		iter->second.firstName = firstName;
		iter->second.secondName = secondName;
		const std::wstring& hex = str.str();
		txtFile.write((char*)&hex[0], hex.length() * 2);
		txtFile.write((char*)&firstName[0], firstName.length() * 2);
		txtFile.write((char*)L"\t", 2);
		txtFile.write((char*)&secondName[0], secondName.length() * 2);
		txtFile.write((char*)L"\n", 2);
		++iter;
	}
}

void DumpCarBrakes(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* brakeDB = _wfopen(outputFile.c_str(), L"wt");
	if(brakeDB)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk2\tUnk3\n", brakeDB);
		const std::vector<CarBrakes>& cb = carDb.brakes;
		const char* types[] = {"Stock", "Sports"};
		for(size_t i = 0; i < cb.size(); ++i)
		{
			const CarBrakes& cbb = cb[i];
			fprintf(
				brakeDB, 
				"%lu\t%x\t%lu\t%s\t%#x\t%#hx\n",
				i, cbb.carId, cbb.price, types[cbb.stage], cbb.unk2, cbb.unk3
			);
		}
		fclose(brakeDB);
	}
}

void DumpCarBrakeControllers(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* bbcDB = _wfopen(outputFile.c_str(), L"wt");
	if(bbcDB)
	{
		fputs("ID\tCarId\tPrice\tAvailable\tUnk2\tUnk3\tUnk4\n", bbcDB);
		const std::vector<CarBrakeBalanceController>& cb = carDb.controllers;
		for(size_t i = 0; i < cb.size(); ++i)
		{
			const CarBrakeBalanceController& cbb = cb[i];
			fprintf(
				bbcDB, 
				"%lu\t%x\t%lu\t%d\t%#x\t%#hx\t%#x\n", 
				i, cbb.carId, cbb.price, cbb.stage, cbb.unk2, cbb.unk3, cbb.unk4
			);
		}
		fclose(bbcDB);
	}
}

void DumpCarDimensions(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* dimsDB = _wfopen(outputFile.c_str(), L"wt");
	if(dimsDB)
	{
		fputs("ID\tCarId\tUnk1\tLength\tHeight\tUnk2\tWeightKg\tWeightLb\tRMWeightMultiplier\tUnk4\tUnk5\n", dimsDB);
		std::map<int, CarDimensions>::const_iterator iter = carDb.dimensions.begin(), end = carDb.dimensions.end();
		size_t i = 0;
		while(iter != end)
		{
			unsigned KGToLbs(unsigned kg);
			const CarDimensions& cd = iter->second;
			unsigned lbs = KGToLbs(cd.weight);
			fwprintf(
				dimsDB, 
				L"%lu\t%x\t%#x\t%hu\t%hu\t%#hx\t%hu\t%hu\t%hu\t%#x\t%#hx\n",
				i, cd.carId, cd.unk, cd.length, cd.height, cd.unk2, cd.weight, lbs, cd.rmWeightMultipier, cd.unk4, cd.unk5
			);
			++i;
			++iter;
		}
		fclose(dimsDB);
	}
}

void DumpCarWeightReduction(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* weightDB = _wfopen(outputFile.c_str(), L"wt");
	if(weightDB)
	{
		fputs("ID\tCarId\tPrice\tNewWeight\tUnk\tStage\n", weightDB);
		const std::vector<CarWeightReduction>& cw = carDb.weightReds;
		const char* types[] = {"Stock", "Stage 1", "Stage 2", "Stage 3"};
		for(size_t i = 0; i < cw.size(); ++i)
		{
			const CarWeightReduction& cwr = cw[i];
			fprintf(
				weightDB, 
				"%lu\t%x\t%lu\t%hu\t%#x\t%s\n",
				i, cwr.carId, cwr.price, cwr.newWeight, cwr.unk, types[cwr.stage]
			);
		}
		fclose(weightDB);
	}
}

void DumpCarRacingModification(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* modsDB = _wfopen(outputFile.c_str(), L"wt");
	if(modsDB)
	{
		fputs("ID\tCarId\tPrice\tRMBody\tNewWeight\tUnk\tAvailable\tFrontDownforce\tRearDownforce\tUnk2\tUnk3\tUnk4\tUnk5\tUnk6\tUnk7\tWidth\n", modsDB);
		const std::vector<CarRacingModification>& cm = carDb.racingMods;
		const std::map<int, CarRealValues>& crvMap = carDb.realValues;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarRacingModification& crm = cm[i];
			std::map<int, CarRealValues>::const_iterator crvIter = crvMap.find(crm.carId);
			const CarRealValues& crv = crvIter->second;
			fprintf(
				modsDB, 
				"%lu\t%x\t%lu\t%x\t%lu\t%#x\t%d\t%.2f\t%.2f\t%#x\t%#x\t%#x\t%#x\t%#x\t%#x\t%hu\n", 
				i, crm.carId, crm.price, crm.rmBodyId, crv.absoluteRMWeight, crm.unk,
				crm.available, float(crm.defaultFrontDownforce) / 100, float(crm.defaultRearDownforce) / 100, 
				crm.unk2, crm.unk3, crm.unk4, crm.unk5, crm.unk6, crm.unk7, crm.width
			);
		}
		fclose(modsDB);
	}
}

union Register
{
	unsigned u;
	int s;
};

unsigned PSToHp(unsigned ps)
{
	// convert from PS to HP, at 8/9001c998 in US exe (not in disasm?);
	long long multiplicand = 0x2050c9f9;
	Register v0 = {ps};
	Register a2 = {v0.u << 5};
	a2.u -= v0.u;
	a2.u <<= 2;
	a2.u += v0.u;
	a2.u <<= 3;
	long long mulRes = a2.s * multiplicand;
	a2.s >>= 31;
	long highRes = mulRes >> 32;
	v0.s = highRes >> 7;
	v0.u -= a2.u;
	return v0.u;
}

unsigned KGToLbs(unsigned kg)
{
	// calculation at 8001c924 in debugger
	long long multiplicand = 0x68db8bad;
	Register v0 = {kg};
	Register a2 = {v0.u << 1};
	a2.u += v0.u;
	a2.u <<= 2;
	a2.u -= v0.u;
	a2.u <<= 2;
	a2.u -= v0.u;
	a2.u <<= 4;
	a2.u += v0.u;
	a2.u <<= 4;
	a2.u -= v0.u;
	a2.u <<= 1;
	long long mulRes = a2.s * multiplicand;
	Register t1 = {mulRes >> 32};
	v0.s = t1.s >> 12;
	a2.s >>= 31;
	return v0.u - a2.u;
}

Register sub_900755E4(Register a0, Register a1)
{
	long long multRes = long long(a0.s) * a1.s;
	a0.s ^= a1.s;
	a1.s >>= 31;
	a1.s &= 0xfff;
	a0.u = (multRes >> 32);
	unsigned resLo = multRes & 0xFFFFFFFF;
	unsigned resLowThreeBytes = resLo & 0xFFF;
	a0.u <<= 20;
	resLo >>= 12;
	resLo |= a0.u;
	Register temp = {resLowThreeBytes + a1.u};
	temp.s >>= 12;
	return (temp.u += resLo), temp;
}

static const unsigned char g_indexTable[] = {
	// 1 0, 1 1, 2 2's, 4 3's, 8 4's, 16 5's, 32 6's 64 7's, 128 8's
	0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 
	6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 
	7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 
	7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 
	8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 
	8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 
	8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 
	8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
};

Register sub_90085BA8(Register a0, Register a1, Register a2, Register a3, Register* pV1Out)
{
	Register t0, t1, t2, t3, t4, t5, t6, t7, t8 = {0};
	Register v0, v1;
	Register s2;
	bool tempBool;
	if(a1.s < 0)
	{
		t8.u = 0xFFFFFFFF;
		t0.s = -a0.s;
		v1.s = -a1.s;
		v0.u = 0 < t0.u;
		t1.u = v1.u - v0.u;
		a0 = t0;
		a1 = t1;
	}
	t1 = a2;
	if(a3.s < 0)
	{
		t8.u = ~(0 | t8.u);
		t2.s = -a2.s;
		v1.s = -a3.s;
		v0.u = 0 < t2.u;
		t3.u = v1.u - v0.u;
		a2 = t2;
		a3 = t3;
		t1 = a2;
	}
	t0 = a3;
	t3 = a0;
	a0 = a1;
	if(t0.s != 0) // true code at 90085F8C
	{
		v0.u = a0.u < t0.u;
		tempBool = (v0.u != 0);
		v0.s = 0xFFFF;
		if(tempBool)
		{
			a2.u = 0;
			// jump to 90086144,duplicated code so I can jump to 9008614C
			t2.u = 0;
			t6 = a2;
			goto loc_9008614C;
		}
		// 90085FA0
		v0.u = v0.u < t0.u;
		v1 = t0;
		if(v0.u == 0)
		{
			v0.u = t1.u < 0x100;
			v0.u ^= 1;
			a2.u = v0.u << 3; // exits to 90085FD4
		}
		else // this code at 90085FBC
		{
			v0.u = 0xFFFFFFu;
			v0.u = v0.u < t0.u;
			t0.u = ((v0.u != 0) ? 0x18 : 0x10);
		}
		const unsigned char* pIndexes = g_indexTable; // pIndexes is v0
		v1.u >>= a2.u;
		v0.u = pIndexes[v1.u];
		v1.u = 0x20;
		v0.u += a2.u;
		a2.u = v1.u - v0.u;
		a1.u = v1.u - a2.u;
		if(a2.u != 0) // code at 9008601C
		{
			v1.u = t0.u << a2.u;
			v0.u = t1.u >> a1.u;
			t0.u = v0.u | v1.u;
			t1.u <<= a2.u;
			a3.u = a0.u >> a1.u;
			v1.u = a0.u << a2.u;
			v0.u = t3.u >> a1.u;
			a0.u = v1.u | v0.u;
			t3.u <<= a2.u;
			a2.u = t0.u >> 16;
			t2.u = a3.u / a2.u;
			v1.u = a3.u % a2.u;
			a1.u = t0.u & 0xFFFF;
			a3.s = t2.s * a1.s;
			v0.u = a0.u >> 16;
			v1.u <<= 16;
			v1.u |= v0.u;
			v0.u = v1.u < a3.u;
			if(v0.u != 0)
			{
				v1.u += t0.u;
				--t2.u;
				v0.u = v1.u < t0.u;
				if(v0.u == 0)
				{
					v0.u = v1.u < a3.u;
					if(v0.u != 0)
					{
						--t2.u;
						v1.u += t0.u;
					}
				}
			}
			// 900860A4
			v1.u -= a3.u;
			unsigned tempQuotient = v1.u / a2.u;
			v1.u = v1.u % a2.u;
			a2.u = tempQuotient;
			a3.u = a2.u * a1.u;
			v0.u = a0.u & 0xFFFF;
			v1.u <<= 16;
			v1.u |= v0.u;
			v0.u = v1.u < a3.u;
			if(v0.u != 0)
			{
				v1.u += t0.u;
				v0.u = v1.u < t0.u;
				--a2.u;
				if(v0.u == 0)
				{
					v0.u = v1.u < a3.u;
					if(v0.u != 0)
					{
						--a2.u;
						v1.u += t0.u;
					}
				}
			}
			// 90086108
			v0.u = t2.u << 16;
			a2.u |= v0.u;
			a0.u = v1.u - a3.u;
			unsigned long long mulRet = unsigned long long(a2.u) * t1.u;
			v1.u = (mulRet >> 32);
			v0.u = a0.u < v1.u;
			t0.u = mulRet & 0xFFFFFFFF;
			if(v0.u != 0)
			{
				--a2.u; // 90086140
				t2.u = 0;
			}
			else
			{
				t2.u = 0;
				if(v1.u == a0.u)
				{
					v0.u = t3.u < t0.u;
					t6 = a2;
					if(v0.u == 0)
					{
						goto loc_9008614C;
					}
					--a2.u; // 90086140
					t2.u = 0;
				}
			}
			// exits to 90086148
		}
		else
		{
			v0.u = t0.u < a0.u;
			a2.u = 1;
			if(v0.u == 0)
			{
				v0.u = t3.u < t1.u;
				a2.u = 0;
				if(v0.u == 0)
				{
					a2.u = 1;
				}
			}
			t2.u = 0;
			t6 = a2;
			// exits to 90086148
		}
	}
	else
	{
		bool tempBool = a0.u < t1.u;
		v0.u = 0xFFFF;
		if(tempBool == true) // false code at 90085D4C
		{
			v0.u = v0.u < t1.u;
			v1 = a2;
			if(v0.u == 0)
			{
				v0.u = t1.u < 0x100;
				v0.u ^= 1;
				t0.u = v0.u << 3; // exits to 90085c44
			}
			else // this code at 90085C2C
			{
				v0.u = 0xFFFFFFu;
				v0.u = v0.u < t1.u;
				t0.u = ((v0.u != 0) ? 0x18 : 0x10);
			}
			// code here is 90085c44
			const unsigned char* pIndexes = g_indexTable; // pIndexes is v0
			v1.u >>= t0.u;
			v0.u = pIndexes[v1.u];
			a1.u = 0x20;
			v0.u += t0.u;
			a2.u = a1.u - v0.u;
			v1.u = a0.u << a2.u;
			if(a2.u != 0)
			{
				t1.u <<= a2.u;
				v0.u = a1.u - a2.u;
				v0.u = t3.u >> v0.u;
				a0.u = v1.u | v0.u;
				t3.u <<= a2.u;
			}
			a2.u = t1.u >> 16;
			a3.u = a0.u / a2.u;
			v1.u = a0.u % a2.u;
			t0.u = t1.u & 0xFFFF;
			a1.s = a3.s * t0.s;
			v0.u = t3.u >> 16;
			v1.u <<= 16;
			v1.u |= v0.u;
			v0.u = v1.u < a1.u;
			if(v0.u != 0)
			{
				v1.u += t1.u;
				v0.u = v1.u < t1.u;
				--a3.u;
				if(v0.u == 0)
				{
					v0.u = v1.u < a1.u;
					if(v0.u != 0)
					{
						--a3.u;
						v1.u += t1.u;
					}
				}
			}
			// 90085CE4
			v1.u -= a1.u;
			a0.u = v1.u / a2.u;
			v1.u = v1.u % a2.u;
			a1.s = a0.s * t0.s;
			v0.u = t3.u & 0xFFFF;
			v1.u <<= 16;
			v1.u |= v0.u;
			v0.u = v1.u < a1.u;
			v1.u += t1.u;
			if(v0.u != 0)
			{
				v0.u = v1.u < t1.u;
				--a0.u;
				if(v0.u == 0)
				{
					tempBool = (v1.u < a1.u);
					if(tempBool)
					{
						--a0.u;
					}
				}
			}
			v0.u = a3.u << 16;
			a2.u = v0.u | a0.u;
			t2.u = 0;
			// code jumps to 90086148 here
		}
		else // 90085D4C
		{
			v0.u = v0.u < t1.u;
			if(t1.u == 0)
			{
				v0.u = 1;
				t1.u = v0.u / t0.u;
				v0.u = 0xFFFF < t1.u;
			}
			v1 = t1;
			if(v0.u == 0)
			{
				v0.u = t1.u < 0x100;
				v0.u = v0.u ^ 1;
				a2.u = v0.u << 3;
				// jumps to 90085da0 here
			}
			else // 90085D88
			{
				v0.u = 0xFFFFFF < t1.u;
				s2.u = ((v0.u != 0) ? 0x18 : 0x10);
			}
			// 90085DA0
			const unsigned char* pIndexes = g_indexTable; // pIndexes is v0
			v1.u >>= a2.u;
			v0.u = pIndexes[v1.u];
			v1.u = 0x20;
			v0.u += a2.u;
			a2.u = v1.u - v0.u;
			a1.u = v1.u - a2.u;
			if(a2.u != 0)
			{
				// 90085DD4
				t1.u <<= a2.u;
				a3.u = a0.u >> a1.u;
				v1.u = a0.u << a2.u;
				v0.u = t3.u >> a1.u;
				a0.u = v0.u | v1.u;
				t3.u <<= a2.u;
				a2.u = t1.u >> 16;
				t0.u = a3.u / a2.u;
				v1.u = a3.u % a2.u;
				a1.u = t1.u & 0xFFFF;
				a3.s = t0.s * a1.s;
				v0.u = a0.u >> 16;
				v1.u <<= 16;
				v1.u |= v0.u;
				v0.u = v1.u < a3.u;
				if(v0.u != 0)
				{
					v1.u += t1.u;
					v0.u = v1.u < t1.u;
					--t0.u;
					if(v0.u == 0)
					{
						v0.u = v1.u < a3.u;
						if(v0.u != 0)
						{
							--t0.u;
							v1.u += t1.u;
						}
					}
				}
				// 90085E50
				v1.u -= a3.u;
				unsigned tempQuotient = v1.u / a2.u;
				v1.u = v1.u % a2.u;
				a2.u = tempQuotient;
				a3.s = a2.s * a1.s;
				v0.u = a0.u & 0xFFFF;
				v1.u <<= 16;
				v1.u |= v0.u;
				v0.u = v1.u < a3.u;
				if(v0.u != 0) // 90085E88
				{
					v1.u += t1.u;
					v0.u = v1.u < t1.u;
					--a2.u;
					if(v0.u == 0)
					{
						v0.u = v1.u < a3.u;
						if(v0.u != 0)
						{
							--a2.u;
							v1.u += t1.u;
						}
					}
				}
				v0.u = t0.u << 16; // 90085EB4
				t2.u = v0.u | a2.u; // 90085EB8
				a0.u = v1.u - a3.u;
			}
			else
			{
				a0.u -= t1.u; // 90085DC8
				t2.u = 1;
			}
			// 90085EC0
			a2.u = t1.u >> 16;
			a3.u = a0.u / a2.u;
			v1.u = a0.u % a2.u;
			t0.u = t1.u & 0xFFFF;
			a1.s = a3.s * t0.s;
			v0.u = t3.u >> 16;
			v1.u <<= 16;
			v1.u |= v0.u;
			v0.u = v1.u < a1.u;
			if(v0.u != 0) // 90085E88
			{
				v1.u += t1.u;
				v0.u = v1.u < t1.u;
				--a3.u;
				if(v0.u == 0)
				{
					v0.u = v1.u < a1.u;
					if(v0.u != 0)
					{
						--a3.u;
						v1.u += t1.u;
					}
				}
			}
			// 90085F24
			v1.u -= a1.u;
			a0.u = v1.u / a2.u;
			v1.u = v1.u % a2.u;
			a1.s = a0.s * t0.s;
			v0.u = t3.u & 0xFFFF;
			v1.u <<= 16;
			v1.u |= v0.u;

			v0.u = v1.u < a1.u;
			v1.u += t1.u;
			if(v0.u != 0) // 90085F5C
			{
				v0.u = v1.u < t1.u;
				--a0.u;
				if(v0.u == 0)
				{
					v0.u = v1.u < a1.u;
					if(v0.u != 0)
					{
						--a0.u;
					}
				}
			}
			v0.u = a3.u << 16;
			a2.u = v0.u | a0.u;
			// jump to 90086148
		}
	}
	// 90086148
	t6 = a2;
loc_9008614C:
	t7 = t2;
	v0 = t6;
	v1 = t7;
	if(t8.u != 0)
	{
		t4.s = -v0.s;
		v1.s = -v1.s;
		v0.u = 0 < t4.u;
		t5.u = v1.u - v0.u;
		v0 = t4;
		v1 = t5;
	}
	*pV1Out = v1;
	return v0;
}

Register sub_90075A18(Register a0, Register a1, Register a2, Register* pV1Out)
{
	Register v0 = a0;
	Register t0 = a1;
	Register v1 = {v0.s >> 31};
	Register t1 = {t0.s >> 31};
	a2.u += 0xc;
	Register a3 = {a2.u << 26};
	if(a3.s >= 0)
	{
		a1.u = v1.u << a2.u;
		if(a3.s < 0)
		{
			a3.s = -a2.s;
			a3.u = v0.u >> a3.u;
			a1.u |= a3.u;
		}
		a0.u = v0.u << a2.u;
	}
	else
	{
		a1.u = v0.u << a2.u;
		a0.u = 0;
	}
	// loc_90075A60
	a2 = t0;
	a3 = t1;
	return sub_90085BA8(a0, a1, a2, a3, pV1Out);
}

Register sub_900758B4(unsigned* pSaveArray, unsigned* pUnkBytesCalcArray, unsigned short lastByteOfEngineData, Register a1)
{
	// sub_900758B4(pSaveArray, pUnkBytesCalcArray, pEngineUnkBytes[21], a1);
	// t4 = pSaveArray
	// t1 = pUnkBytesCalcArray
	Register a0;
	Register v0 = {lastByteOfEngineData};
	Register t2 = {v0.u - 1};
	Register t3 = {t2.u & 0xFFFF};
	Register a2 = {t3.u << 2};
	Register t0 = {pUnkBytesCalcArray[0]};
	Register v1 = {pUnkBytesCalcArray[a2.u / 4]};
	v0.u = t0.s < a1.s;
	if(v0.u == 0)
	{
		v0.u = *pSaveArray;
		return v0;
	}
	v0.u = a1.u < v1.u;
	// 900758FC
	Register a3 = {0};
	if(v0.u == 0)
	{
loc_90075904:
		v0.u = pSaveArray[a2.u / 4];
		return v0;
	}
	// 90075914
	v0.u = t3.s < 4;
	t0 = t2;
	if(v0.u == 0)
	{
		Register v0Loop = v0;
		v0.u = t0.u & 0xffff;
		v1.u = a3.u & 0xffff;
		do // 90075928
		{
			v0.u += v1.u;
			a0.s = v0.s >> 1;
			a2.u = a0.u << 2; // calcing byte index, don't need it
			v1.u = pUnkBytesCalcArray[a2.u / 4];
			v0.u = a1.s < v1.s;
			if(v0.u == 0)
			{
				a3 = a0;
				if(a1.u == v1.u)
				{
					goto loc_90075904;
				}
			}
			else t0 = a0;
			// loc_9007595C
			v0.u = t0.u & 0xffff;
			v1.u = a3.u & 0xffff;
			v0Loop.u = v0.u - v1.u;
			v0.u = t0.u & 0xffff;
		}
		while(v0Loop.u >= 4);
	}
	// else at 90075974
	v1 = a3;
	a0.u = v1.u & 0xffff;
	a2.u = t0.u & 0xffff;
	int tempBool = a0.s < a2.s;
	v0.u = v1.u & 0xffff;
	
	if(tempBool != 0)
	{
		Register v0Loop = {0};
		do
		{
			v0.u = a0.u << 2; // not needed for array index
			v0.u = pUnkBytesCalcArray[v0.u / 4];
			tempBool = a1.s < v0.s;
			v0.u = v1.u & 0xffff;
			if(tempBool != 0)
			{
				goto outLoopElse;
			}
			++v1.u;
			a0.u = v1.u & 0xffff;
			v0Loop.u = a0.u < a2.u;
		}
		while(v0Loop.u != 0);
		v0.u = v1.u & 0xffff;
	}
outLoopElse:
	// else at 900759C0
	// v0.u <<= 2; not needed for index calculation
	unsigned index = v0.u;
	Register s0 = {pSaveArray[index - 1]};
	v0.u = pSaveArray[index];
	t0.u = pUnkBytesCalcArray[index - 1];
	v0.u -= s0.u;
	v1.u = a1.u - t0.u;
	long long mulRes = long long(v0.s) * v1.s;
	v1.u = pUnkBytesCalcArray[index];
	v0.u = v1.u - t0.u;
	a2 = v0;
	a1.u = mulRes >> 32;
	a0.u = mulRes & 0xffffffff;
	a3.u = v0.u >> 31;
	v0 = sub_90085BA8(a0, a1, a2, a3, &v1);
	v0.u += s0.u;
	return v0;
}

void sub_90077320(unsigned a0, unsigned engineModifier, unsigned char* pUpgradedUnkByte)
{
	unsigned v0 = 2;
	bool tempBool = (a0 == v0);
	v0 = a0 < 3;
	if(tempBool)
	{
		unsigned short* pUs = reinterpret_cast<unsigned short*>(pUpgradedUnkByte); // 90077370
		*pUs += engineModifier;
	}
	else if(!v0)
	{
		// 90077344
		if(a0 == 4)
		{
			unsigned* pUl = reinterpret_cast<unsigned*>(pUpgradedUnkByte);
			*pUl += engineModifier; // 
		}
	}
	else if(a0 == 1)
	{
		*pUpgradedUnkByte += engineModifier; // 90077358
	}
}

void sub_900773A0(unsigned a0, unsigned numElems, unsigned engineModifier, unsigned char* pUpgradedUnkBytes)
{
	if(a0 != 0)
	{
		for(unsigned i = 0; i < numElems; ++i)
		{
			sub_90077320(a0, engineModifier, pUpgradedUnkBytes + i);
		}
	}
}

unsigned CalculateRealPS(
	const unsigned short* pEngineData,
	unsigned numShorts, 
	const unsigned char* pBaseUnits,
	const unsigned char* pBandRPMs,
	const unsigned* pPowerAdjustments,
	unsigned numPowerAdjustments,
	unsigned sumOfEngineModifiers
)
{
	// pEngineUnkBytes = 0x3b into this car's CarEngineData
	// sorry most of these have useless names, but I don't know what they represent
	// just that they're used
	//unsigned baseMultiplier = 1000;
	unsigned* pSaveArray = (unsigned*)_alloca(numShorts * sizeof(*pSaveArray));
	unsigned* pUnkBytesCalcArray = (unsigned*)_alloca(numShorts * sizeof(*pUnkBytesCalcArray));
	unsigned char* pUpgradedUnkBytes = (unsigned char*)_alloca(numShorts * sizeof(*pUpgradedUnkBytes));
	memcpy(pUpgradedUnkBytes, pBandRPMs, numShorts);
	// this need to be done if NA tuning or Engine Balancing are included
	if(sumOfEngineModifiers)
	{
		sub_900773A0(1, 0x10, sumOfEngineModifiers /* engine modifier of part upgrade */, pUpgradedUnkBytes);
	}
	pBandRPMs = pUpgradedUnkBytes;
	unsigned short* pModifiedUnkBytes = (unsigned short*)_alloca(numShorts * sizeof(*pModifiedUnkBytes));
	unsigned char baseRPM = pBandRPMs[0]; // loaded at 90074FE8
	/* adjusted power multiplier = (1000 + (Sum(part power multipliers) * 10))
		So RX-7 RB with just Turbo 1 = 1000 + (26 * 10) = 1260
		A stock normal car always = 1000
	*/
	unsigned adjustedPowerMultiplier = 1000;
	// loop body at 90076E58, adjustedPowerMultiplier = s6
	for(unsigned i = 0; i < numPowerAdjustments; ++i)
	{
		unsigned thisPower = 100 + pPowerAdjustments[i]; // s1
		long long mulRes = long long(adjustedPowerMultiplier) * thisPower; // mult    $s6, $s1
		Register v1 = {mulRes & 0xFFFFFFFF};
		mulRes = v1.s * long long(0x51EB851F);
		v1.s >>= 31;
		Register a3 = {mulRes >> 32}; // mfhi a3
		Register v0 = {a3.s >> 5};
		adjustedPowerMultiplier = v0.u - v1.u;
	}
	unsigned otherEngineBit = pBaseUnits[3]; // this value is 0x39 into car engine data
	{
		// this is pBaseUnits[3] * 100, but using the PlayStation method
		// calculated at 90074F20
		unsigned temp = otherEngineBit << 1;
		temp += otherEngineBit;
		temp <<= 3;
		temp += otherEngineBit;
		otherEngineBit = temp << 2;
		// following at 90074F38
		unsigned otherByte = pBaseUnits[4]; // guess where this comes from
		Register v1 = {otherByte << 1};
		v1.u += otherByte;
		v1.u <<= 3;
		v1.u += otherByte;
		v1.u <<= 2;
		v1.u += 0x1f4;
		if(otherEngineBit < v1.s)
		{
			otherEngineBit = v1.u;
		}
	}
	unsigned unk2_zeroindexCalc = 0;
	{
		unk2_zeroindexCalc = pBaseUnits[0] * 10; // calculated at 90077024
		int v0 = unk2_zeroindexCalc < 0x100;
		if(unk2_zeroindexCalc == 0)
		{
			unk2_zeroindexCalc = 1000;
			v0 = unk2_zeroindexCalc < 0x100;
		}
		if(v0 != 0)
		{
			unsigned temp = unk2_zeroindexCalc << 2;
			temp += unk2_zeroindexCalc;
			temp <<= 1;
			unk2_zeroindexCalc = temp;
		}
	}
	//Register fp = {0x50};
	Register fp = {pBandRPMs[numShorts - 1]};
	unsigned i = 0;
	for(; i < numShorts; ++i)
	{
		{
			Register s1 = {100}; // calculated at 90074FA4
			Register v0 = {s1.u << 2};
			v0.u += s1.u;
			s1.u = v0.u << 1;
			Register v1 = {adjustedPowerMultiplier};
			if(v1.u == 0)
			{
				v1.u = 1000;
			}
			v1.u -= s1.u; // 90074FD4 - sw      $v1, 0x40+var_30($sp)

			// unk byte calculation starts at 9007502C in JP disasm
			unsigned char thisRPM = pBandRPMs[i]; // lbu     $v1, 0x34($v0) (this starts 0x3b into this car's CarEngineData)
			v0.u = thisRPM - baseRPM; //subu    $v0, $v1, $s6
			Register t0 = v1;
			Register t1 = {t0.s * v0.s}; // mult    $t0, $v0, mflo    $t1
			Register a1 = {fp.u - baseRPM}; // subu    $v0, $v1, $s6
			a1.s = t1.s / a1.s; // div     $t1, $a1, mflo    $a1
			v0.u = thisRPM << 1; // sll     $v0, $v1, 1
			v0.u += thisRPM; // addu    $v0, $v1
			v0.u <<= 3; // sll     $v0, 3
			v0.u += thisRPM; // addu    $v0, $v1
			v0.u <<= 14; // sll     $v0, 14
			long long mulRes = long long(v0.s) * ((int)0x88888889); // li      $a0, 0x88888889, mult    $v0, $a0

			t1.s = mulRes >> 32; // mfhi    $t1
			v1.u = t1.u + v0.u; // addu    $v1, $t1, $v0
			v1.s >>= 5; // sra     $v1, 5
			v0.s >>= 31; // sra     $v0, 31
			v1.u -= v0.u; // subu    $v1, $v0
			pUnkBytesCalcArray[i] = v1.u; // sw      $v1, 0xC($s0)
			;
			;
			// calculation code starting 90075088 in JP disasm
			// - 90075544 in US disasm
			Register hpWord = {pEngineData[i]};
			a1.u = s1.u + a1.u;
			Register a2 = {unk2_zeroindexCalc}; // loaded at 90075000, that half calculated at 90077024
			t1.s = a2.s * a1.s;

			Register baseHpCalc = {hpWord.u << 3}; // sll     $v0, $a0, 3
			baseHpCalc.u -= hpWord.u; // subu    $v0, $a0
			baseHpCalc.u <<= 5; // sll     $v0, 5
			baseHpCalc.u -= hpWord.u; // subu    $v0, $a0
			Register baseHpCalcHelper = {baseHpCalc.u << 2}; // sll     $v1, $v0, 2
			baseHpCalcHelper.u -= baseHpCalc.u; // subu    $v1, $v0
			baseHpCalc.u = baseHpCalcHelper.u << 4; // sll     $v0, $v1, 4
			baseHpCalc.u -= baseHpCalcHelper.u; // subu    $v0, $v1
			baseHpCalc.u <<= 2; // sll     $v0, 2
			baseHpCalc.u += hpWord.u; // addu    $v0, $a0
			baseHpCalc.u <<= 2; // sll     $v0, 2
			baseHpCalc.s >>= 12; // sra     $v0, 12

			mulRes = long long(baseHpCalc.s) * t1.s;
			Register a0 = {mulRes & 0xFFFFFFFF};
			a1.s = mulRes >> 32;
			a2.u = 0x5F5E100;
			Register a3 = {0};
			v0 = sub_90085BA8(a0, a1, a2, a3, &v1);
			pSaveArray[i] = v0.u;
			// result of calculation used at 900751fc in JP disasm
			a0.u = 0x1000;
			a1.u = pSaveArray[i];
			v0 = sub_900755E4(a0, a1); 
			pSaveArray[i] = v0.u;
		}
		{
			// calculation done at 900752B0
			Register v1 = {pUnkBytesCalcArray[i]};
			Register v0 = {v1.u << 4};
			v0.u -= v1.u;
			v0.u <<= 2;
			if(v0.s < 0)
			{
				v0.s = 0xFFF;
			}
			Register s0 = {v0.s >> 12};
			v0.u = s0.u + 50;
			long long mulRes = v0.s * 0x51EB851FLL;
			v0.s >>= 31;
			Register t2 = {mulRes >> 32};
			v1.s = t2.s >> 5;
			v1.u -= v0.u;
			v0.u = v1.u << 1;
			v0.u += v1.u;
			v0.u <<= 3;
			v0.u += v1.u;
			s0.u = v0.u << 2;
			/*if(a0 > 0) // at 900752FC
			{
				v0.u = s3.u + a1.u;
				;
				;
			}*/
			pModifiedUnkBytes[i] = s0.u;
		}
	}
	fp.u = 0;
	Register s0 = {pModifiedUnkBytes[0]};
	Register s4 = {otherEngineBit};
	Register s7 = {0}; // for third part, set at 90075264
	Register s6 = {0}; // for third part, set at 9007525c
	i = 0;
	while(s4.s >= s0.s) // slt     $v0, $s4, $s0, bnez    $v0, loc_9007546C
	{
		Register v0 = {s0.u << 12}; // 90075394
		long long multiplicand = (int)0x88888889; // lw      $t2, 0x48+var_34($sp)
		long long mulRes = long long(v0.s) * multiplicand;
		Register t2 = {mulRes >> 32};
		//Register a0 = s5; // s5 = stack buffer
		Register a1 = {t2.u + v0.u};
		a1.s >>= 5;
		v0.s >>= 31;
		a1.u -= v0.u;
		// first res for RX-7 Type RB = 0x230
		// result of that calculation loaded at 900758f0 and returned to calc at 900753C0

		// 900753C0
		// a0 is used for three pointers, need to figure out and pass those
		Register hpCalcRet = sub_900758B4(pSaveArray, pUnkBytesCalcArray, numShorts, a1); // returns the value from pSaveArray
		Register hpCalc = {hpCalcRet.u << 2}; // hp calc res * 4
		hpCalc.u += hpCalcRet.u; // calc res * 5
		hpCalc.u <<= 1; // calc res * 10
		multiplicand = int(0xD20D20D3);
		mulRes = hpCalc.s * multiplicand; // mult    $v1, $t2
		Register a0 = hpCalcRet;
		a1.u = 0x9ccd;
		Register a2 = {0};
		t2.s = mulRes >> 32;
		v0.u = t2.u + hpCalc.u;
		v0.s >>= 5;
		hpCalc.s >>= 31;
		Register s1 = {v0.u - hpCalc.u};
		Register v1 = {0};
		v0 = sub_90075A18(a0, a1, a2, &v1);
		a0 = v0;
		a1 = s0;
		v0 = sub_900755E4(a0, a1);
		a0 = v0;
		a1.u = 0xb30;
		a2.u = 0;
		v0 = sub_90075A18(a0, a1, a2, &v1);
		//unsigned v1 = 0; // lh      $v1, 0xC($s2), value doesn't matter for now
		a0 = v0;
		//if(v1 == s0)
		//{
		//	// sh      $a0, 0x2C($s2)
		//	// sh      $s1, 0x4C($s2)
		//	// addiu   $s2, 2
		//}
		v0.u = otherEngineBit; // lhu     $v0, 8($s3)
		bool tempBool = v0.s < s0.s;
		v0.u = s7.s < s1.s;
		if(tempBool == false)
		{
			tempBool = !!v0.u;
			v0.u = s6.s < a0.s;
			if(tempBool)
			{
				s7 = s1; // 9007544C
				fp = s0;
			}
			// 90075454
			if(v0.u != 0)
			{
				s6 = a0;
			}
		}
		// 90075464
		s0.u += 100; // addiu   $s0, 0x64
		//tempBool = !!v0;
		//v0 = s6.s < a0.s; //  slt     $v0, $s6, $a0
		//if(tempBool == false)
		//{
		//	if(v0 != 0) // 90075454
		//	{
		//		realHp = a0; // move    $s6, $a0
		//	}
		//	s0 += 100;
		//	continue;
		//}
	}
	return s6.u;
}

void DumpCarEngines(const std::wstring& outputFile, const CarDB& carDb, const WideStringCollection& strings)
{
	FILE* engineDB = _wfopen(outputFile.c_str(), L"wt, ccs=UNICODE");
	if(engineDB)
	{
		const wchar_t* pHeaders = L"ID\tCarId\tName1\tName2\tCC\tDealerPS\tRealPS\tRealHP\tMaxPowerRPM\tTorqueKGM\tTorqueRPM\tBaseUnits\tSignificantUnks\tBandAcceleration\tBandRPMs\tUnk3\tUnk4\n";
		fputws(pHeaders, engineDB);
		std::map<int, CarEngineData>::const_iterator iter = carDb.engines.begin(), end = carDb.engines.end();
		std::map<int, CarRealValues>::const_iterator crvIter;
		size_t i = 0;
		while(iter != end)
		{
			const CarEngineData& ced = iter->second;
			crvIter = carDb.realValues.find(ced.carId);
			const unsigned unkArraySize = sizeof(ced.bandAcceleration) / sizeof(*ced.bandAcceleration);
			wchar_t unkBuffer[(unkArraySize * 6) + 1] = {0};
			wchar_t unk2Buffer[sizeof(ced.bandRPMs) * 6 + 6] = {0};
			wchar_t baseUnitString[sizeof(ced.baseUnits) * 4 + 4] = {0};
			for(int j = 0; j < unkArraySize; ++j)
			{
				if(ced.bandAcceleration[j] != 0xFFFF)
				{
					swprintf(&unkBuffer[j * 5], 6, L"%04x ", ced.bandAcceleration[j]);
				}
			}
			int nextUnk2BufferLoc = 0;
			for(int k = 0; k < sizeof(ced.bandRPMs); ++k)
			{
				if(ced.bandRPMs[k] != 0xFF)
				{
					int wrote = swprintf(&unk2Buffer[nextUnk2BufferLoc], 6, L"%d ", ced.bandRPMs[k] * 100);
					if(wrote > 0) nextUnk2BufferLoc += wrote;
				}
			}
			for(int l = 0; l < sizeof(ced.baseUnits); ++l)
			{
				swprintf(&baseUnitString[l * 3], 4, L"%02x ", ced.baseUnits[l]);
			}
			fwprintf(
				engineDB, 
				L"%lu\t%x\t%ws\t%ws\t%hu\t%hu\t%hu\t%hu\t%lu\t%.1f\t%ws\t%ws\t%hu\t%ws\t%ws\t%hu\t%hu\n",
				i, ced.carId, strings[ced.engineTypeName].c_str(), strings[ced.engineTypeName2].c_str(), ced.engineCc, ced.ps, crvIter->second.realPs, 
				crvIter->second.realHp, ced.maxPowerRPM * 10, ced.torqueKGM / 10.f, strings[ced.torqueRPM].c_str(), baseUnitString, ced.significantUnks,
				unkBuffer, unk2Buffer, ced.unk3, ced.unk4
			);
			++i;
			++iter;
		}
		fclose(engineDB);
	}
}

#define DumpPowerIncreaserPart(Thing, part) \
	void Dump##Thing(const std::wstring& outputFile, const CarDB& carDb) \
	{ \
		FILE* db = _wfopen(outputFile.c_str(), L"wt"); \
		if(db) \
		{ \
			fputs("ID\tCarId\tPrice\tAvailable\tPowerAdjustment\tUnk\tUnk2\n", db); \
			const std::vector<Thing>& cm = carDb.part; \
			for(size_t i = 0; i < cm.size(); ++i) \
			{ \
				const Thing& crm = cm[i]; \
				fprintf( \
					db, \
					"%lu\t%x\t%lu\t%lu\t%#x\t%#x\t%#x\n", \
					i, crm.carId, crm.price, crm.available, crm.powerAdjustor1, crm.powerAdjustor2, crm.powerAdjustor3 \
				); \
			} \
			fclose(db); \
		} \
	}

DumpPowerIncreaserPart(CarPortGrinding, portGrinds);
DumpPowerIncreaserPart(CarDisplacementIncrease, displacements);
DumpPowerIncreaserPart(CarChip, chips);

void DumpCarEngineBalancing(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tAvailable\tPowerAdjustment\tEngineModifier\tUnk\n", db);
		const std::vector<CarEngineBalancing>& cm = carDb.engineBalances;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarEngineBalancing& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%lu\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, crm.available, crm.powerAdjustor, crm.engineModifier, crm.unk
			);
		}
		fclose(db);
	}
}


#undef DumpPowerIncreaserPart

void DumpCarNATuning(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tStage\tPowerAdjustment\tEngineModifier\tUnk\n", db);
		const std::vector<CarNATuning>& cm = carDb.naTunes;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarNATuning& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%lu\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, crm.stage, crm.powerAdjustor, crm.engineModifier, crm.unk
			);
		}
		fclose(db);
	}
}

void DumpCarTurboTuning(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tStage\tPowerAdjustment\tUnk1\tUnk2\tUnk3\tUnk4\tUnk5\tUnk6\tUnk7\n", db);
		const std::vector<CarTurboTuning>& cm = carDb.turboTunes;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarTurboTuning& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%lu\t%#x\t%#x\t%#x\t%#x\t%#x\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, crm.stage, crm.powerAdjustor, crm.unk1, crm.unk2, crm.unk3,
				crm.unk4, crm.unk5, crm.unk6, crm.unk7
			);
		}
		fclose(db);
	}
}

void DumpCarDrivetrain(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tUnk1\tDriveType\tUnk2\tUnk3\tUnk4\n", db);
		std::map<int, CarDrivetrain>::const_iterator iter = carDb.drivetrains.begin(), end = carDb.drivetrains.end();
		size_t i = 0;
		const char* driveTypes[] = {"FR", "FF", "4WD", "MR", "RR"};
		while(iter != end)
		{
			const CarDrivetrain& ced = iter->second;
			fprintf(
				db, 
				"%lu\t%x\t%#x\t%s\t%#x\t%#x\t%#x\n",
				i, ced.carId, ced.unk, driveTypes[ced.driveType], ced.unk2, ced.unk3, ced.unk4
			);
			++i;
			++iter;
		}
		fclose(db);
	}
}

void DumpCarFlywheel(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk1\tUnk2\tUnk3\n", db);
		const char* types[] = {"Stock", "Sports", "Semi-Racing", "Racing"};
		const std::vector<CarFlywheel>& cm = carDb.flywheels;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarFlywheel& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, types[crm.stage], crm.unk1, crm.unk2, crm.unk3
			);
		}
		fclose(db);
	}
}

void DumpCarClutch(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk1\tUnk2\tUnk3\tUnk4\n", db);
		const char* types[] = {"Stock", "Single", "Twin", "Triple"};
		const std::vector<CarClutch>& cm = carDb.clutches;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarClutch& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, types[crm.stage], crm.unk1, crm.unk2, crm.unk3, crm.unk4
			);
		}
		fclose(db);
	}
}

void DumpCarPropShaft(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tAvailable\tUnk1\tUnk2\tUnk3\n", db);
		const std::vector<CarPropShaft>& cm = carDb.propShafts;
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarPropShaft& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%lu\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, crm.available, crm.unk1, crm.unk2, crm.unk3
			);
		}
		fclose(db);
	}
}

void DumpCarGearbox(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk1\tUnk2\tFinalGear\tUnk3\tUnk4\n", db);
		const std::vector<CarGearbox>& cm = carDb.gearboxes;
		const char* types[] = {"Stock", "Close", "Super Close", "Fully Customised"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarGearbox& crm = cm[i];
			char unk2Buffer[sizeof(crm.unk2) * 4 + 4] = {0};
			for(int j = 0; j < sizeof(crm.unk2); ++j)
			{
				sprintf(&unk2Buffer[j * 3], "%02x ", crm.unk2[j]);
			}
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%lu\t%#x\t%#x\n",
				i, crm.carId, crm.price, types[crm.type], crm.unk1, unk2Buffer, crm.finalGear, crm.unk3, crm.unk4
			);
		}
		fclose(db);
	}
}

void DumpCarSuspension(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk1\tUnk2\tUnk3\n", db);
		const std::vector<CarSuspension>& cm = carDb.suspensions;
		const char* types[] = {"Stock", "Sports", "Semi-Racing", "Professional"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarSuspension& crm = cm[i];
			char unk3Buffer[sizeof(crm.unk3) * 4 + 4] = {0};
			for(int j = 0; j < sizeof(crm.unk3); ++j)
			{
				sprintf(&unk3Buffer[j * 3], "%02x ", crm.unk3[j]);
			}
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%s\n",
				i, crm.carId, crm.price, types[crm.stage], crm.unk1, crm.unk2, unk3Buffer
			);
		}
		fclose(db);
	}
}

void DumpCarIntercooler(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tPowerAdjustor1\tPowerAdjustor2\tPowerAdjustor3\n", db);
		const std::vector<CarIntercooler>& cm = carDb.intercoolers;
		const char* type[] = {"Stock", "Sports", "Racing"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarIntercooler& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, type[crm.stage], crm.powerAdjustor1, crm.powerAdjustor2, crm.powerAdjustor3
			);
		}
		fclose(db);
	}
}

void DumpCarMuffler(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tPowerAdjustor1\tPowerAdjustor2\tPowerAdjustor3\n", db);
		const std::vector<CarMuffler>& cm = carDb.mufflers;
		const char* type[] = {"Stock", "Sports", "Semi-Racing", "Racing"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarMuffler& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, type[crm.stage], crm.powerAdjustor1, crm.powerAdjustor2, crm.powerAdjustor3
			);
		}
		fclose(db);
	}
}

void DumpCarLSD(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk1\tUnk2\tUnk3\n", db);
		const std::vector<CarLSD>& cm = carDb.slipDiffs;
		const char* type[] = {"Stock", "1 Way", "2 Way", "1.5 Way", "Fully Customised", "YAW"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarLSD& crm = cm[i];
			char unk3Buffer[sizeof(crm.unk3) * 4 + 4] = {0};
			for(int j = 0; j < sizeof(crm.unk3) / sizeof(crm.unk3[0]); ++j)
			{
				sprintf(&unk3Buffer[j * 3], "%02x ", crm.unk3[j]);
			}
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%s\n",
				i, crm.carId, crm.price, type[crm.stage], crm.unk1, crm.unk2, unk3Buffer
			);
		}
		fclose(db);
	}
}

void DumpCarTyres(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tPrice\tType\tUnk1\tUnk2\tUnk3\n", db);
		const std::vector<CarTyres>& cm = carDb.frontTyres;
		const char* type[] = {"Stock", "Sports", "Hard", "Medium", "Soft", "Super Soft", "Sim", "Dirt"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarTyres& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%lu\t%s\t%#x\t%#x\t%#x\n",
				i, crm.carId, crm.price, type[crm.type], crm.unk1, crm.unk2, crm.unk3
			);
		}
		fclose(db);
	}
}

void DumpCarRearTyres(const std::wstring& outputFile, const CarDB& carDb)
{
	FILE* db = _wfopen(outputFile.c_str(), L"wt");
	if(db)
	{
		fputs("ID\tCarId\tType\tUnk1\tUnk2\tUnk3\n", db);
		const std::vector<CarRearTyres>& cm = carDb.rearTyres;
		const char* type[] = {"Stock", "Sports", "Hard", "Medium", "Soft", "Super Soft", "Sim", "Dirt"};
		for(size_t i = 0; i < cm.size(); ++i)
		{
			const CarRearTyres& crm = cm[i];
			fprintf(
				db,
				"%lu\t%x\t%s\t%#x\t%#x\t%#x\n",
				i, crm.carId, type[crm.type], crm.unk1, crm.unk2, crm.unk3
			);
		}
		fclose(db);
	}
}

void DumpCarData(const std::wstring& outputStem, const CarDB& carDb, const WideStringCollection& strings)
{
	std::wstring dbFile = outputStem + L"data.txt";
	FILE* carDbF = _wfopen(dbFile.c_str(), L"wt");
	if(carDbF)
	{
		fputs("CarId\tBrakePart\tSpecialCar\tWeightPart\tUnk3\tWeightDist\tEngineID\tUnk4\tUnk5\tNATunePart\tTurboPart\tDrivetrainPart\tFlywheelPart\tClutchPart\t"
		"Unk8\tDifferentialPart\tTransmissionPart\tSuspensionPart\tUnk9\tFrontTyres\tRearTyres\tUnk10\tRimsCode3\tManufacturerID\tFirstNameIndex\tSecondNameIndex\tPadding\t"
		"Year\tUnk14\tNewPrice\n", carDbF);
		typedef std::map<int, CarDataWithName> CarList;
		CarList::const_iterator iter = carDb.cars.begin(), end = carDb.cars.end();
		while(iter != end)
		{
			const CarData& cd = iter->second.cd;
			fprintf(carDbF, "%x\t%lu\t%lu\t%lu\t%#x\t%#x\t%lu\t%#x\t%#x\t%lu\t%lu\t%lu\t%lu\t%lu\t%#x\t%lu\t%lu\t%lu\t%#x\t%lu\t%lu\t%#x\t%#x\t%lu\t%lu\t%lu\t%d\t%d\t%#x\t%lu\n", 
				cd.carId, cd.brakePart, cd.specialCar, cd.weightPartId, cd.unk3, cd.weightDistribution, cd.enginePartId, cd.unk4, cd.unk5, cd.naPart, cd.turboPart, cd.drivetrainPart,
				cd.flywheel, cd.clutch, cd.unk8, cd.differential, cd.transmission, cd.suspension, cd.unk9, cd.frontTyres, cd.rearTyres, cd.unk10, cd.rimsCode3, cd.manufacturerID, 
				cd.nameFirstPart, cd.nameSecondPart, cd.padding, cd.year, cd.unk14, cd.newPrice
			);
			++iter;
		}
		fclose(carDbF);
	}
#define DumpThing(Thing, filePart) \
	std::wstring Thing##File = outputStem + L#filePart L".txt"; \
	Dump##Thing(Thing##File, carDb)

	DumpThing(CarBrakes, brakes);
	DumpThing(CarBrakeControllers, brake_controllers);
	DumpThing(CarDimensions, dimensions);
	DumpThing(CarWeightReduction, weight_reds);
	DumpThing(CarRacingModification, mods);
	DumpThing(CarPortGrinding, portGrinds);
	DumpThing(CarEngineBalancing, balancing);
	DumpThing(CarDisplacementIncrease, displacements);
	DumpThing(CarChip, chips);
	DumpThing(CarNATuning, na_tunes);
	DumpThing(CarTurboTuning, turbo_tunes);
	DumpThing(CarDrivetrain, drivetrains);
	DumpThing(CarFlywheel, flywheels);
	DumpThing(CarClutch, clutches);
	DumpThing(CarPropShaft, propshafts);
	DumpThing(CarGearbox, gearboxes);
	DumpThing(CarSuspension, suspensions);
	DumpThing(CarIntercooler, coolers);
	DumpThing(CarMuffler, mufflers);
	DumpThing(CarLSD, slipDiffs);
	DumpThing(CarTyres, frontTyres);
	DumpThing(CarRearTyres, rearTyres);

	std::wstring enginesFile = outputStem + L"engines.txt";
	DumpCarEngines(enginesFile, carDb, strings);
}

void DumpDataFiles(
	CarDB& carDb, 
	const std::wstring& outputDir,
	const std::wstring& label,
	const std::wstring& region,
	const WideStringCollection& strings
)
{
	if(carDb.cars.empty()) return;
	std::wostringstream stem;
	stem << outputDir << L'\\' << label;
	DumpText(stem.str() + (std::wstring(L"_name_") + region) + L".txt", strings, carDb);
	DumpCarData(stem.str() + region + L"_car_", carDb, strings);
}

//void DumpDiffs(const std::string& outputDir, const CarDB& carDbJap, CarDB carDbEng, CarDB carDbUsa)
//{
//	if(carDbJap.empty() || carDbEng.empty() || carDbUsa.empty())
//	{
//		return;
//	}
//	std::ofstream diffFile((outputDir + "\\car_data_diffs.txt").c_str());
//	CarDB::const_iterator iter = carDbJap.begin(), end = carDbJap.end();
//	while(iter != end)
//	{
//		bool hasEng = false, hasUsa = false;
//		const CarData& cdJp = iter->second.cd;
//		CarDB::iterator engIter = carDbEng.find(iter->first);
//		CarDB::iterator usaIter = carDbUsa.find(iter->first);
//		if(engIter == carDbEng.end())
//		{
//			diffFile << std::hex << iter->first << " doesn't exist in Eng car data\n";
//		}
//		else
//		{
//			++hasEng;
//			engIter->second.cd.nameFirstPart = iter->second.cd.nameFirstPart;
//			engIter->second.cd.nameSecondPart = iter->second.cd.nameSecondPart;
//			if(memcmp(&engIter->second, &cdJp, sizeof(cdJp)))
//			{
//				diffFile << std::hex << iter->first << " is different in Eng car data than JP\n";
//			}
//		}
//		if(usaIter == carDbUsa.end())
//		{
//			diffFile << std::hex << iter->first << " doesn't exist in Usa car data\n";
//		}
//		else
//		{
//			++hasUsa;
//			usaIter->second.cd.nameFirstPart = iter->second.cd.nameFirstPart;
//			usaIter->second.cd.nameSecondPart = iter->second.cd.nameSecondPart;
//			if(memcmp(&usaIter->second.cd, &cdJp, sizeof(cdJp)))
//			{
//				diffFile << std::hex << iter->first << " is different in Usa car data than JP\n";
//			}
//		}
//		if(hasEng && hasUsa)
//		{
//			if(memcmp(&usaIter->second.cd, &engIter->second, sizeof(cdJp)))
//			{
//				diffFile << std::hex << iter->first << " is different in Usa car data than Eng\n";
//			}
//		}
//		carDbEng.erase(iter->first);
//		carDbUsa.erase(iter->first);
//		++iter;
//	}
//	CarDB::iterator leftIter = carDbEng.begin(), leftEnd = carDbEng.end();
//	while(leftIter != leftEnd)
//	{
//		diffFile << std::hex << leftIter->first << " doesn't exist in Jap car data but is in Eng\n";
//		++leftIter;
//	}
//	leftIter = carDbUsa.begin(), leftEnd = carDbUsa.end();
//	while(leftIter != leftEnd)
//	{
//		diffFile << std::hex << leftIter->first << " doesn't exist in Usa car data but is in Eng\n";
//		++leftIter;
//	}
//}

struct TrackInfo
{
	int nameOffset; // byte that starts the null terminated name
	unsigned int trackId; // id in hex, this = short name hashed by HashTrackName below
	unsigned short unk;
	unsigned short unk2;
	unsigned short unk3;
	unsigned short unk4;
	unsigned short unk5;
	unsigned short unk6;
	unsigned short unk7;
	unsigned short unk8;
};

struct TrackInfoFile
{
	char header[4]; // CRS
	short unk;
	short numTracks;
	TrackInfo ti[0];
};

unsigned int HashTrackName(const std::string& str)
{
	size_t len = str.length();
	unsigned int hash = 0;
	for(size_t i = 0; i < len; ++i)
	{
		char ch = str[i];
		unsigned int temp = hash << 6;
		unsigned int temp2 = hash >> 26;
		hash = temp | temp2;
		hash += ch;
	}
	return hash;
}

unsigned int HashTrackName(const std::wstring& str)
{
	size_t len = str.length();
	unsigned int hash = 0;
	for(size_t i = 0; i < len; ++i)
	{
		unsigned char ch = str[i] & 0xFF;
		unsigned int temp = hash << 6;
		unsigned int temp2 = hash >> 26;
		hash = temp | temp2;
		hash += ch;
	}
	return hash;
}

void ExtractTrackInfo(const std::wstring& extractedVolDir, std::vector<TrackDetails>& trackList)
{
	std::ifstream crsMap((extractedVolDir + L"\\.crsinfo").c_str(), std::ios::binary);
	if(!crsMap)
	{
		std::cout << "Couldn't open .crsInfo. Track Info will not be dumped\n";
		return;
	}
	TrackInfoFile f = {0};
	crsMap.read((char*)&f, sizeof(f));
	std::vector<TrackInfo> tracks(f.numTracks);
	crsMap.read((char*)&tracks[0], f.numTracks * sizeof(TrackInfo));
	for(unsigned i = 0; i < f.numTracks; ++i)
	{
		TrackInfo& ti = tracks[i];
		crsMap.seekg(ti.nameOffset);
		std::string trackName;
		std::getline(crsMap, trackName, static_cast<char>(0));
		TrackDetails td = {trackName, ti.trackId};
		memcpy(&td.unk, &ti.unk, sizeof(ti) - offsetof(TrackInfo, unk));
		trackList.push_back(td);
		/*courseInf << std::hex << std::setfill('0') << std::setw(8) << ti.trackId << '\t' << trackName << '\t' << 
			ti.unk << '\t' << ti.unk2 << '\t' << ti.unk3 << '\t' << ti.unk4 << '\t' <<
			ti.unk5 << '\t' << ti.unk6 << '\t' << ti.unk7 << '\t' << ti.unk8 << '\n';*/
	}
}

void DumpTrackInfo(const std::wstring& outputDir, const std::wstring& label, const std::vector<TrackDetails>& trackList)
{
	std::ofstream courseInf((outputDir + std::wstring(L"\\") + label + L"course_info.txt").c_str());
	courseInf << "Number of tracks: " << trackList.size() << '\n';
	for(unsigned i = 0; i < trackList.size(); ++i)
	{
		const TrackDetails& ti = trackList[i];
		courseInf << std::hex << std::setfill('0') << std::setw(8) << ti.hashId << '\t' << ti.name << '\t' << 
			ti.unk << '\t' << ti.unk2 << '\t' << ti.unk3 << '\t' << ti.unk4 << '\t' <<
			ti.unk5 << '\t' << ti.unk6 << '\t' << ti.unk7 << '\t' << ti.unk8 << '\n';
	}
}

//#define FILL_OUT_OPPONENT_NAME(num) \
//	if(ri.opponentIndex##num && (ri.opponentIndex##num <= opponentPoolSize)) \
//	{ \
//		int oppId = opponents[(ri.opponentIndex##num - 1)].carId; \
//		CarDB::const_iterator iter = cars.find(oppId); \
//		std::wstring fullName = (iter->second.firstName + L" ") + iter->second.secondName; \
//		opponentIds[num-1] = fullName; \
//	} \
//	else opponentIds[num-1] = L".";
//
//void FillOutOpponentNames(const RaceInfo& ri, std::wstring* opponentIds, const std::vector<OpponentInfo>& opponents, const CarDB& cars)
//{
//	size_t opponentPoolSize = opponents.size();
//	FILL_OUT_OPPONENT_NAME(1); FILL_OUT_OPPONENT_NAME(2);
//	FILL_OUT_OPPONENT_NAME(3); FILL_OUT_OPPONENT_NAME(4);
//	FILL_OUT_OPPONENT_NAME(5); FILL_OUT_OPPONENT_NAME(6);
//	FILL_OUT_OPPONENT_NAME(7); FILL_OUT_OPPONENT_NAME(8);
//	FILL_OUT_OPPONENT_NAME(9); FILL_OUT_OPPONENT_NAME(10);
//	FILL_OUT_OPPONENT_NAME(11); FILL_OUT_OPPONENT_NAME(12);
//	FILL_OUT_OPPONENT_NAME(13); FILL_OUT_OPPONENT_NAME(14);
//	FILL_OUT_OPPONENT_NAME(15);
//}

//#define FILL_OUT_OPPONENT_ID(num) \
//	if(ri.opponentIndex##num && (ri.opponentIndex##num <= opponentPoolSize)) \
//	{ \
//		int oppId = opponents[(ri.opponentIndex##num - 1)].carId; \
//		opponentIds[num-1] = oppId; \
//	} \
//	else opponentIds[num-1] = -1;
//
//void FillOutOpponentIds(const RaceInfo& ri, int* opponentIds, const std::vector<OpponentInfo>& opponents, const CarDB& cars)
//{
//	size_t opponentPoolSize = opponents.size();
//	FILL_OUT_OPPONENT_ID(1); FILL_OUT_OPPONENT_ID(2);
//	FILL_OUT_OPPONENT_ID(3); FILL_OUT_OPPONENT_ID(4);
//	FILL_OUT_OPPONENT_ID(5); FILL_OUT_OPPONENT_ID(6);
//	FILL_OUT_OPPONENT_ID(7); FILL_OUT_OPPONENT_ID(8);
//	FILL_OUT_OPPONENT_ID(9); FILL_OUT_OPPONENT_ID(10);
//	FILL_OUT_OPPONENT_ID(11); FILL_OUT_OPPONENT_ID(12);
//	FILL_OUT_OPPONENT_ID(13); FILL_OUT_OPPONENT_ID(14);
//	FILL_OUT_OPPONENT_ID(15);
//}

void ParseRaces(const std::wstring& file, RaceDB& raceDb)
{
	std::ifstream data(file.c_str(), std::ios::binary);
	char header[8];
	data.read(header, sizeof(header));

	int raceOffset = 0, raceSize = 0;
	data.read((char*)&raceOffset, sizeof(raceOffset));
	data.read((char*)&raceSize, sizeof(raceSize));

	int oppOffset = 0, oppSize = 0;
	data.read((char*)&oppOffset, sizeof(oppOffset));
	data.read((char*)&oppSize, sizeof(oppSize));

	int restrictedCarOffset = 0, restrictedCarSize = 0;
	data.read((char*)&restrictedCarOffset, sizeof(restrictedCarOffset));
	data.read((char*)&restrictedCarSize, sizeof(restrictedCarSize));

	int strOffset = 0, strSize = 0;
	data.read((char*)&strOffset, sizeof(strOffset));
	data.read((char*)&strSize, sizeof(strSize));
	data.seekg(strOffset);

	short numStrings = 0;
	data.read((char*)&numStrings, sizeof(numStrings));
	raceDb.raceStrings.resize(numStrings);
	for(short i = 0; i < numStrings; ++i)
	{
		unsigned char chars = 0;
		data.read((char*)&chars, sizeof(chars));
		std::string thisStr(chars, 0);
		data.read((char*)&thisStr[0], chars);
		raceDb.raceStrings[i] = thisStr;
		data.read(header, 1); // the null
	}
	unsigned numOpps = oppSize / sizeof(raceDb.opponents[0]);
	data.seekg(oppOffset);
	while(numOpps-- > 0)
	{
		OpponentInfo oi = {0};
		data.read((char*)&oi, sizeof(oi));
		raceDb.opponents[oi.opponentId] = oi;
		raceDb.unusedOpponentIds.insert(oi.opponentId);
	}
	data.seekg(raceOffset);
	unsigned numRaces = raceSize / sizeof(raceDb.races[0]);
	raceDb.races.reserve(numRaces);
	while(numRaces-- > 0)
	{
		RaceInfo ri = {0};
		data.read((char*)&ri, sizeof(ri));
		raceDb.races.push_back(ri);

		raceDb.unusedOpponentIds.erase(ri.opponentIndex1 & 0xFFFFFF);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex2); raceDb.unusedOpponentIds.erase(ri.opponentIndex3);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex4); raceDb.unusedOpponentIds.erase(ri.opponentIndex5);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex6); raceDb.unusedOpponentIds.erase(ri.opponentIndex7);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex8); raceDb.unusedOpponentIds.erase(ri.opponentIndex9);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex10); raceDb.unusedOpponentIds.erase(ri.opponentIndex11);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex12); raceDb.unusedOpponentIds.erase(ri.opponentIndex13);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex14); raceDb.unusedOpponentIds.erase(ri.opponentIndex15);
		raceDb.unusedOpponentIds.erase(ri.opponentIndex16);
	}
	data.seekg(restrictedCarOffset);
	unsigned numRestricted = restrictedCarSize / 0x80;
	while(numRestricted-- > 0)
	{
		std::vector<int> restrictedIds;
		int restData[32];
		data.read((char*)restData, sizeof(restData));
		int* carIdIter = (int*)restData;
		while(*carIdIter)
		{
			restrictedIds.push_back(*carIdIter);
			++carIdIter;
		}
		raceDb.carRestrictions.push_back(restrictedIds);
	}
}

void DumpRaces(const std::wstring& outputDir, const std::wstring& label, const std::wstring& region, const CarDB& carDb, const RaceDB& raceDb)
{
	std::ofstream raceStrings(((outputDir + L"\\") + (label + L"_") + (region + L"_") + L"race_text.txt").c_str());
	unsigned numStrings = raceDb.raceStrings.size();
	for(unsigned i = 0; i < numStrings; ++i)
	{
		raceStrings << std::hex << i << ' ' << raceDb.raceStrings[i] << '\n';
	}

	FILE* opponentFile = _wfopen(((outputDir + L"\\") + (label + L"_") + (region + L"_") + L"race_opponents.txt").c_str(), L"wt");
	fputs("CarId\tBrakePart\tSpecialCar\tWeightPart\tUnk3\tWeightDist\tEngineID\tUnk4\tUnk5\tNATunePart\tTurboPart\tDrivetrainPart\tFlywheelPart\tClutchPart\t"
		"Unk8\tDifferentialPart\tTransmissionPart\tSuspensionPart\tUnk9\tFrontTyres\tRearTyres\tUnk10\tRimsCode3\tUnk11\tUnk12\tUnk13\tUnk14\t"
		"Unk15\tUnk15a\tUnk16\tUnk17\tUnk18\tUnk19\tUnk20\tUnk21\tUnk22\tUnk23\tUnk24\tPowerPercentage\tUnk25\tID\n", opponentFile);
	unsigned numOpponents = raceDb.opponents.size();
	std::map<unsigned, OpponentInfo>::const_iterator oppIter = raceDb.opponents.begin(), oppEnd = raceDb.opponents.end();
	while(oppIter != oppEnd)
	{
		const OpponentInfo& oi = oppIter->second;
		fprintf(
			opponentFile, 
			"%x\t%lu\t%lu\t%lu\t%#hx\t%lu\t%lu\t%#hx\t%#x\t%#x\t%lu\t%lu\t%lu\t%lu\t%#hx\t%lu\t"
			"%lu\t%lu\t%#x\t%lu\t%lu\t%#x\t%#hx\t%#hx\t%#hx\t%#hx\t%#x\t%#x\t%#x\t%#hx\t%#hx\t%#hx\t%#hx\t%#hx\t"
			"%#hx\t%#hx\t%#hx\t%#hx\t%#hu\t%#x\t%hu\n",
			oi.carId, oi.brakePart, oi.specialCar, oi.weightPartId, oi.unk3, oi.rmPartId, oi.enginePartId,
			oi.unk4, oi.unk5, oi.naPart, oi.turboPart, oi.drivetrainPart, oi.flywheel, oi.clutch, oi.unk8, oi.differential,
			oi.transmission, oi.suspension, oi.unk9, oi.frontTyres, oi.rearTyres, oi.unk10, oi.rimsCode3, oi.unk11, 
			oi.unk12, oi.unk13, oi.unk14, oi.unk15, oi.unk15a, oi.unk16, oi.unk17, oi.unk18, oi.unk19, oi.unk20, oi.unk21,
			oi.unk22, oi.unk23, oi.unk24, oi.powerPercentage, oi.unk25, oi.opponentId
		);
		++oppIter;
	}
	fclose(opponentFile);

	FILE* fOut = _wfopen(((outputDir + L"\\") + (label + L"_") + (region + L"_") + L"races.txt").c_str(), L"wt");
	fputs(
		"RaceName\tTrackName\tOpponent1\tOpponent2\tOpponent3\tOpponent4\tOpponent5\tOpponent6\tOpponent7\tOpponent8\tOpponent9\tOpponent10\tOpponent11\t"
		"Opponent12\tOpponent13\tOpponent14\tOpponent15\tOpponent16\tRollingStartSpeed\tLaps\tUnk17\tLicense\t64String\tUnk4\tUnk5\tUnk6\tUnk7\tUnk8\tUnk9\tUnk10\t"
		"Unk11\tUnk12\tUnk13\tUnk14\tUnk15\tRally\tAllowedEntrantsIndex\tDrivetrainFlags\t1stPrize\t2ndPrize\t3rdPrize\t4thPrize\t5thPrize\t6thPrize\tPrizeCar1\tPrizeCar2\t"
		"PrizeCar3\tPrizeCar4\tUnk16\tEntryHP\tSeriesBonus\tCarFlags\n", 
		fOut
	);
	const std::vector<std::string>& stringList = raceDb.raceStrings;
	unsigned numRaces = raceDb.races.size();
	for(unsigned i = 0; i < numRaces; ++i)
	{
		static const char* licChar[] = {"Free", "B", "A", "IC", "IB", "IA", "S"};
		//std::wstring prizeCars[4] = {L".", L".", L".", L"."};
		char unk3Str[17 * 3 + 2] = {0};
		char* unk3StrIter = unk3Str;
		const RaceInfo& ri = raceDb.races[i];
		for(int i = 0 ; i < 17; ++i)
		{
			sprintf(unk3StrIter, "%x ", ri.unk3[i]);
			unk3StrIter += 3;
		}
		/*for(int i = 0; i < 4; ++i)
		{
			if(ri.prizeCars[i] != 0)
			{
				CarList::const_iterator iter = cars.find(ri.prizeCars[i]);
				prizeCars[i] = (iter->second.firstName + L" ") + iter->second.secondName;
			}
		}*/
		/*std::wstring opponentNames[15] = {L""};
		FillOutOpponentNames(ri, opponentNames, opponents, cars);
		const wchar_t* opponentIds[15] = {NULL};
		std::transform(opponentNames, opponentNames + 15, opponentIds, std::mem_fun_ref(&std::wstring::c_str));*/
		/*int opponentIds[15] = {-1};
		FillOutOpponentIds(ri, opponentNames, opponents);*/
		unsigned hp = PSToHp(ri.hpRestriction); // /  1.01427772651f);
		fprintf(
			fOut, 
			"%s\t%s\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%lu\t%d\t%#x\t"
			"%s\t%s\t%#hx\t%#x\t%#hx\t%#x\t%#x\t%#hx\t%#hx\t%#x\t%#x\t%#x\t%#x\t%#x\t%hu\t%hu\t%x\t%hu\t%hu\t%hu\t%hu\t%hu\t%hu\t"
			"%x\t%x\t%x\t%x\t%#hx\t%hu\t%hu\t%#x\n",
			stringList[ri.raceNameIndex].c_str(), stringList[ri.trackNameId].c_str(), ri.opponentIndex1 & 0xFFFFFF, ri.opponentIndex2, ri.opponentIndex3, 
			ri.opponentIndex4, ri.opponentIndex5, ri.opponentIndex6, ri.opponentIndex7, ri.opponentIndex8, ri.opponentIndex9, ri.opponentIndex10,
			ri.opponentIndex11, ri.opponentIndex12, ri.opponentIndex13, ri.opponentIndex14, ri.opponentIndex15, ri.opponentIndex16, ri.rollingStartSpeed, ri.laps, ri.unk17, licChar[ri.licence],
			unk3Str, ri.unk4, ri.unk5, ri.unk6, ri.unk7, ri.unk8, ri.unk9, ri.unk10, ri.unk11, ri.unk12, ri.unk13, ri.unk14, ri.unk15, ri.rally, ri.allowedEntrantsId,
			ri.forcedDriveTrainFlags, ri.prizeMoney1st, ri.prizeMoney2nd, ri.prizeMoney3rd, ri.prizeMoney4th, ri.prizeMoney5th, ri.prizeMoney6th,
			ri.prizeCars[0], ri.prizeCars[1], ri.prizeCars[2], ri.prizeCars[3], ri.unk16, hp, ri.seriesChampBonus, ri.carFlags
		);
	}
	fclose(fOut);
	FILE* fUnusedOpps = _wfopen(((outputDir + L"\\") + (label + L"_") + (region + L"_") + L"unusedOpps.txt").c_str(), L"wt");
	std::set<unsigned>::const_iterator unusedOppsIter = raceDb.unusedOpponentIds.begin(), unusedOppsEnd = raceDb.unusedOpponentIds.end();
	while(unusedOppsIter != unusedOppsEnd)
	{
		fprintf(fUnusedOpps, "%lu\n", *unusedOppsIter);
		++unusedOppsIter;
	}
	fclose(fUnusedOpps);
}

std::string TranslateToMinSec(unsigned secs)
{
	std::ostringstream str;
	unsigned mins = secs / 100;
	if(mins)
	{
		str << mins << ':';
		secs %= 100;
	}
	str << std::setw(2) << std::setfill('0') << secs;
	return str.str();
}

void DumpLicenses(const std::wstring& outputDir, const std::wstring& label, const std::wstring& region, const std::wstring& licenseData)
{
	std::ifstream licData(licenseData.c_str(), std::ios::binary);
	char header[8];
	licData.read(header, sizeof(header));
	if(memcmp(header, "GTDT", 4) != 0)
	{
		return;
	}
	unsigned int firstOffset = 0, stringsOffset = 0, testDataOffset = 0, testDataSize = 0, offsetCount = 1;
	licData.read(reinterpret_cast<char*>(&firstOffset), sizeof(firstOffset));
	// this read is to skip past the 'size' value for the first offset
	licData.read(reinterpret_cast<char*>(&stringsOffset), sizeof(stringsOffset));
	while(licData.tellg() < firstOffset)
	{
		unsigned temp;
		licData.read(reinterpret_cast<char*>(&temp), sizeof(temp));
		if(temp) stringsOffset = temp;
		// test data is the 31st offset size pair
		if(++offsetCount == 31)
		{
			testDataOffset = stringsOffset;
			licData.read(reinterpret_cast<char*>(&testDataSize), sizeof(testDataSize));
		}
		else 
		{
			// intentional sizeof, we want to skip the next int
			licData.read(header, sizeof(stringsOffset));
		}
	}
	// read the strings
	licData.seekg(stringsOffset);
	unsigned short numStrings = 0;
	licData.read(reinterpret_cast<char*>(&numStrings), sizeof(numStrings));
	std::vector<std::string> licStrings(numStrings);
	for(short i = 0; i < numStrings; ++i)
	{
		unsigned char chars = 0;
		licData.read(reinterpret_cast<char*>(&chars), sizeof(chars));
		std::string& thisStr = licStrings[i];
		thisStr.resize(chars);
		licData.read(&thisStr[0], chars);
		licData.read(header, 1); // the null
	}
	FILE* fLicenses = _wfopen(((outputDir + L"\\") + (label + L"_") + (region + L"_") + L"licenses.txt").c_str(), L"wt");
	fputs(
		"LicName\tTrackName\tGold\tSilver\tBronze\tKids\tKidsAttempts\tRollingStart\tLaps\tLicense\tRally\tAllowedEntrants\t"
		"DriveTrainFlags\t1stPrize\t2ndPrize\t3rdPrize\t4thPrize\t5thPrize\t6thPrize\t"
		"PrizeCar1\tPrizeCar2\tPrizeCar3\tPrizeCar4\tEntryHP\tSeriesBonus\tRestrictionFlags",
		fLicenses
	);
	licData.clear();
	licData.seekg(testDataOffset);
	unsigned int testDataReadSoFar = 0;
	static const char* licChar[] = {"Free", "B", "A", "IC", "IB", "IA", "S"};
	while(testDataReadSoFar < testDataSize)
	{
		LicenseTestInfo test;
		licData.read(reinterpret_cast<char*>(&test), sizeof(test));
		testDataReadSoFar += sizeof(test);
		fprintf(
			fLicenses,
			"%s\t%s\t%s.%03lu\t%s.%03lu\t%s.%03lu\t%s.%03lu\t%lu\t"
			// row below starts at rollingStartSpeed
			"%lu\t%lu\t%s\t%lu\t%lu\t%hu\t%hu\t%hu\t%hu\t%hu\t%hu\t%hu\t"
			// row below starts at PrizeCar1
			"%x\t%x\t%x\t%x\t%hu\t%hu\t%#hx\n",
			licStrings[test.testNameIndex].c_str(), licStrings[test.trackNameId].c_str(), TranslateToMinSec(test.goldTime & 0xFF).c_str(), (test.goldTime >> 8) * 10, 
			TranslateToMinSec(test.silverTime & 0xFF).c_str(), (test.silverTime >> 8) * 10, TranslateToMinSec(test.bronzeTime & 0xFF).c_str(), (test.bronzeTime >> 8) * 10, 
			TranslateToMinSec(test.kidsTime & 0xFF).c_str(), (test.kidsTime >> 8) * 10, test.kidAttempts,
			// 
			test.rollingStartSpeed, test.laps, licChar[test.licence],
			test.rally, test.allowedEntrantsId, test.forcedDriveTrainFlags, test.prizeMoney1st, test.prizeMoney2nd, 
			test.prizeMoney3rd, test.prizeMoney4th, test.prizeMoney5th, test.prizeMoney6th, 
			//
			test.prizeCars[0], test.prizeCars[1], 
			test.prizeCars[2], test.prizeCars[3], test.hpRestriction, test.seriesChampBonus, test.carFlags
		);
	}
	fclose(fLicenses);
}

//void DumpLicenses(const std::wstring& outputDir, const std::wstring& label, const std::wstring& region, const std::wstring& licenseData)
//{
//	std::ifstream licData(licenseData.c_str(), std::ios::binary);
//	char header[8];
//	licData.read(header, sizeof(header));
//	if(memcmp(header, "GTDT", 4) != 0)
//	{
//		return;
//	}
//	unsigned int firstOffset = 0, stringsOffset = 0, testDataOffset = 0, testDataSize = 0, offsetCount = 1;
//	licData.read(reinterpret_cast<char*>(&firstOffset), sizeof(firstOffset));
//	// this read is to skip past the 'size' value for the first offset
//	licData.read(reinterpret_cast<char*>(&stringsOffset), sizeof(stringsOffset));
//	while(licData.tellg() < firstOffset)
//	{
//		unsigned temp;
//		licData.read(reinterpret_cast<char*>(&temp), sizeof(temp));
//		if(temp) stringsOffset = temp;
//		// test data is the 31st offset size pair
//		if(++offsetCount == 31)
//		{
//			testDataOffset = stringsOffset;
//			licData.read(reinterpret_cast<char*>(&testDataSize), sizeof(testDataSize));
//		}
//		else 
//		{
//			// intentional sizeof, we want to skip the next int
//			licData.read(header, sizeof(stringsOffset));
//		}
//	}
//	// read the strings
//	licData.seekg(stringsOffset);
//	unsigned short numStrings = 0;
//	licData.read(reinterpret_cast<char*>(&numStrings), sizeof(numStrings));
//	std::vector<std::string> licStrings(numStrings);
//	for(short i = 0; i < numStrings; ++i)
//	{
//		unsigned char chars = 0;
//		licData.read(reinterpret_cast<char*>(&chars), sizeof(chars));
//		std::string& thisStr = licStrings[i];
//		thisStr.resize(chars);
//		licData.read(&thisStr[0], chars);
//		licData.read(header, 1); // the null
//	}
//	FILE* fLicenses = _wfopen(((outputDir + L"\\") + (label + L"_") + (region + L"_") + L"licenses_tab.html").c_str(), L"wt");
//	fputs(
//		"<html><body><table class=\"innerTable\">\n<tr><th scope=\"col\">LicName</th><th scope=\"col\">Gold</th><th scope=\"col\">Silver</th><th scope=\"col\">Bronze</th><th scope=\"col\">Kids</th><th scope=\"col\">KidAttempts</th></tr>\n",
//		fLicenses
//	);
//	licData.clear();
//	licData.seekg(testDataOffset);
//	unsigned int testDataReadSoFar = 0;
//	static const char* licChar[] = {"Free", "B", "A", "IC", "IB", "IA", "S"};
//	while(testDataReadSoFar < testDataSize)
//	{
//		LicenseTestInfo test;
//		licData.read(reinterpret_cast<char*>(&test), sizeof(test));
//		testDataReadSoFar += sizeof(test);
//		fprintf(
//			fLicenses,
//			"<tr><td>%s</td><td>%s.%03lu</td><td>%s.%03lu</td><td>%s.%03lu</td><td>%s.%03lu</td><td>%lu</td></tr>\n",
//			licStrings[test.testNameIndex].c_str(), TranslateToMinSec(test.goldTime & 0xFF).c_str(), (test.goldTime >> 8) * 10, 
//			TranslateToMinSec(test.silverTime & 0xFF).c_str(), (test.silverTime >> 8) * 10, TranslateToMinSec(test.bronzeTime & 0xFF).c_str(), (test.bronzeTime >> 8) * 10, 
//			TranslateToMinSec(test.kidsTime & 0xFF).c_str(), (test.kidsTime >> 8) * 10, test.kidAttempts
//		);
//	}
//	fputs("</table></body></html>", fLicenses);
//	fclose(fLicenses);
//}

bool ReadAllFile(const std::wstring& fileName, std::vector<char>& buffer)
{
	std::ifstream file(fileName.c_str(), std::ios::binary);
	if(!file)
	{
		return false;
	}
	file.seekg(0, std::ios::end);
	size_t fileSize = file.tellg();
	buffer.resize(fileSize);
	file.seekg(0, std::ios::beg);
	file.read((char*)&buffer[0], fileSize);
	return true;
}

// .carinfo file structure
// 'Car\0' string (4 bytes)
// int numCarEntries (4 bytes)
// Car Entries (12 bytes)
// {
// int carId; 
// uint numColoursAndfileOffsetOfCarColourData; // offset is & 0x3ffff, numColours is (>> 12) + 1
// }
// struct CarColourData
// {
//    ushort[] bgr555Colours; // numColours In Length
//    byte[] singleByteColourIds; // numColours in Length. Index of colour byte here is added to 
//    string carName; // null terminated
// }
// Note, a car's index into the .carinfo file is also its index into the .carcolor file
// The .carcolour file is laud out as
// "CCOL " string (4 byte)
// int unk
// ushort[] carOffsets; // index is into this array. Values are byte offsets to within this file
// ushort[] carColours; // these are indexes into the .cclatain/.ccjapanese files
//
// .cclatain/.ccjapanese files are just an array of ushort
// each short is the file offset of a null terminated string with the name of the colour

struct CarColourHeaderEntries
{
	int carId;
	unsigned numColoursAndOffset; // offset is & 0x3ffff, numColours is (>> 12) + 1
};

typedef std::map<int, std::vector<CarColourInfo> > ColourMap;

void ParseColours(
	const std::wstring& extractedVolDir, 
	const std::wstring& carInfoFileName, 
	const std::wstring& langFileName, 
	ColourMap& colourMap,
	std::map<int, std::wstring>& carInfoRaceName
)
{
	std::wstring volPrefix = extractedVolDir + L'\\';
	std::vector<char> carInfoBuffer;
	if(!ReadAllFile(volPrefix + carInfoFileName, carInfoBuffer))
	{
		if(!ReadAllFile(volPrefix + carInfoFileName + L"j", carInfoBuffer))
		{
			return;
		}
	}
	std::vector<char> carColourBuffer;
	ReadAllFile(volPrefix + L".carcolor", carColourBuffer);
	std::vector<char> langFileBuffer;
	ReadAllFile(volPrefix + langFileName, langFileBuffer);
	bool isJp = (langFileName.find('j') != std::wstring::npos);
	char* pColourInfoBase = &carInfoBuffer[0];
	char* pCarColourBase = &carColourBuffer[0];
	unsigned short* pCarColourIndexBase = reinterpret_cast<unsigned short*>(pCarColourBase + 8);
	unsigned short* pColourNameBase = reinterpret_cast<unsigned short*>(&langFileBuffer[0]);
	char* pColourNameBaseChar = &langFileBuffer[0];
	int numEntries = *(reinterpret_cast<int*>(pColourInfoBase + 4));
	CarColourHeaderEntries* pColourHeaders = reinterpret_cast<CarColourHeaderEntries*>(pColourInfoBase + 8);
	ColourMap cm;
	for(int i = 0; i < numEntries; ++i)
	{
		const CarColourHeaderEntries& entry = pColourHeaders[i];
		std::vector<CarColourInfo>& colourInfo = cm[entry.carId];
		unsigned numColours = (entry.numColoursAndOffset >> 18) & 0x1f; 
		numColours += 1;
		unsigned carColoursOffset = entry.numColoursAndOffset & 0x3FFFF;
		unsigned short* pThisCarColourData = reinterpret_cast<unsigned short*>(pColourInfoBase + carColoursOffset);
		unsigned char* pThisCarSingleByteColourIds = reinterpret_cast<unsigned char*>(pThisCarColourData + numColours);
		char* pCarName = reinterpret_cast<char*>(pThisCarSingleByteColourIds + numColours + 1);
		std::string carName(pCarName);
		if(carName[0] == '\x7f')
		{
			carName.replace(0, 1, "[R]");
		}
		carInfoRaceName[entry.carId].assign(carName.begin(), carName.end());
		for(unsigned j = 0; j < numColours; ++j)
		{
			CarColourInfo cci;
			cci.carId = entry.carId;
			cci.byteId = pThisCarSingleByteColourIds[j];
			cci.mainColour = pThisCarColourData[j];
			unsigned carColourOffset = pCarColourIndexBase[i];
			unsigned langIndex = *(reinterpret_cast<unsigned short*>(pCarColourBase + carColourOffset + (j * 2)));
			unsigned langOffset = pColourNameBase[langIndex];
			char* pName = pColourNameBaseChar + langOffset;
			if(*pName)
			{
				if(isJp)
				{
					wchar_t* pColourName = reinterpret_cast<wchar_t*>(pName);
					cci.colourName = pColourName;
				}
				else
				{
					std::string name(pName);
					cci.colourName.assign(name.begin(), name.end());
				}
			}
			colourInfo.push_back(cci);
		}
	}
	colourMap.swap(cm);
}

void ExtractGTData(const std::wstring& extractedVolDir, const std::wstring& outputDir, const std::wstring& label)
{
	CarDB carDb;
	ExtractCars(extractedVolDir + L"\\carparam\\decomp\\gtmode_data.dat", carDb);
	ParseColours(extractedVolDir, L".carinfo", L".ccjapanese", carDb.carColours, carDb.carInfoRaceNames);
	CarDB carDbEng;
	ExtractCars(extractedVolDir + L"\\carparam\\decomp\\eng_gtmode_data.dat", carDbEng);
	ParseColours(extractedVolDir, L".carinfoe", L".cclatain", carDb.carColours, carDbEng.carInfoRaceNames);
	CarDB carDbUsa;
	ExtractCars(extractedVolDir + L"\\carparam\\decomp\\usa_gtmode_data.dat", carDbUsa);
	ParseColours(extractedVolDir, L".carinfoa", L".cclatain", carDb.carColours, carDbUsa.carInfoRaceNames);
	
	WideStringCollection engStrings, japStrings, usaStrings;
	GetAllUniStringLists(extractedVolDir, japStrings, engStrings, usaStrings);

	RaceDB jpRace;
	ParseRaces(extractedVolDir + L"\\carparam\\decomp\\gtmode_race.dat", jpRace);
	ExtractTrackInfo(extractedVolDir, jpRace.tracks);

	DumpDataFiles(carDb, outputDir, label, L"jap", japStrings);
	DumpRaces(outputDir, label, L"jp", carDb, jpRace);
	DumpLicenses(outputDir, label, L"jp", extractedVolDir + L"\\carparam\\decomp\\license_data.dat");
	if(!carDbEng.cars.empty())
	{
		DumpDataFiles(carDbEng, outputDir, label, L"eng", engStrings);
		RaceDB engRaces;
		ParseRaces(extractedVolDir + L"\\carparam\\decomp\\eng_gtmode_race.dat", engRaces);
		ExtractTrackInfo(extractedVolDir, engRaces.tracks);
		DumpRaces(outputDir, label, L"eng", carDbEng, engRaces);
		DumpLicenses(outputDir, label, L"eng", extractedVolDir + L"\\carparam\\decomp\\eng_license_data.dat");
	}
	if(!carDbUsa.cars.empty())
	{
		DumpDataFiles(carDbUsa, outputDir, label, L"usa", usaStrings);
		RaceDB usaRaces;
		ParseRaces(extractedVolDir + L"\\carparam\\decomp\\usa_gtmode_race.dat", usaRaces);
		ExtractTrackInfo(extractedVolDir, usaRaces.tracks);
		DumpRaces(outputDir, label, L"usa", carDbUsa, usaRaces);
		DumpLicenses(outputDir, label, L"usa", extractedVolDir + L"\\carparam\\decomp\\usa_license_data.dat");
	}
	//DumpDiffs(outputDir, carDb, carDbEng, carDbUsa);
}

// commonpic.dat format (for 23.dat specifically)

// palette starts at 0x1f84, can have multiple palettes each 0x200 in size
// 8 bit image data starts at 0x4000
// 329 is large image with single palette
// 66 - height is only ~240
// image data seems to be in 16x8 pixel tiles - 512 pixel fixed pitch
// layed out like:
// r1p1r1p2r1p3r1p4r1p5r1p6r1p7r1p8 - then +512 (504 really) bytes to next row
// r2p1r2p2r2p3r2p4r2p5r2p6r2p7r2p8
//
// 512 / 16 = 32 tiles per row
// 23.dat height = 504 / 8 = 63 tiles per column
// 32 x 63 =  2016 (0x7e0)
//
// data at 4 to 0x1f84 is in the format of 
//
//struct TileInfo
//{
//	unsigned char x; // * 16 for pixel pos
//	unsigned char y; // * 8 for pixel pos
//	unsigned short tileIndex; // high nibble = which palette
//};

// gtmenu / eng / gtmenudat.dat - individual files
// - 1.dat
// 4 bpp data starts at 0x1380
// BGR palette starts at 0xf7c
// tile info at 0x100

// - 901.dat
// 4 bpp data starts at 0x14a0
// BGR palette starts at 0x10a0
// tile info at 0xb4

int __cdecl wmain(int argc, wchar_t** argv)
{
	if(argc < 4)
	{
		std::cout << "Usage: GT2DataExploder <C:\\Path\\To\\Extracted\\Vol.Files> <OutputDir> <Label>\n\n"
			"Extracts data from various decompressed Gran Turismo 2 files into tabbed text\n"
			"files suitable for importing into a database etc. If present in the vol,\n"
			"it will parse ENG, USA, and original JP data. Other European language data\n"
			"isn't used.\n"
			"Note that this tool WILL NOT WORK ON DEMO versions, the file data is\n"
			"formatted differently.\n\n"
			"Example: GT2DataExploder C:\\GT2\\PalVol C:\\GT2\\PalVolData PalVol";
		return 1;
	}
	_wmkdir(argv[2]);
	ExtractGTData(argv[1], argv[2], argv[3]);
	return 0;
}