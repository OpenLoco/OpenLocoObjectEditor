using OpenLoco.Dat.Data;
using OpenLoco.Dat.FileParsing;
using OpenLoco.Dat.Types;
using System.ComponentModel;

namespace OpenLoco.Dat.Objects
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[LocoStructSize(0xF4)]
	[LocoStructType(ObjectType.Industry)]
	[LocoStringTable("Name", "var_02", "<unused>", "NameClosingDown", "NameUpProduction", "NameDownProduction", "NameSingular", "NamePlural")]
	public record IndustryObject(
		[property: LocoStructOffset(0x00), LocoString, Browsable(false)] string_id Name,
		[property: LocoStructOffset(0x02), LocoString, Browsable(false)] string_id var_02,
		[property: LocoStructOffset(0x02), LocoString, Browsable(false)] string_id _unused,
		[property: LocoStructOffset(0x04), LocoString, Browsable(false)] string_id NameClosingDown,
		[property: LocoStructOffset(0x06), LocoString, Browsable(false)] string_id NameUpProduction,
		[property: LocoStructOffset(0x08), LocoString, Browsable(false)] string_id NameDownProduction,
		[property: LocoStructOffset(0x0A), LocoString, Browsable(false)] string_id NameSingular,
		[property: LocoStructOffset(0x0C), LocoString, Browsable(false)] string_id NamePlural,
		[property: LocoStructOffset(0x0E), Browsable(false)] image_id _var_0E, // shadows image id base
		[property: LocoStructOffset(0x12), Browsable(false)] image_id _var_12, // Base image id for building 0
		[property: LocoStructOffset(0x16), Browsable(false)] image_id _var_16,
		[property: LocoStructOffset(0x1A), Browsable(false)] image_id _var_1A,
		[property: LocoStructOffset(0x1E)] uint8_t NumBuildingParts,
		[property: LocoStructOffset(0x1F)] uint8_t NumBuildingVariations,
		[property: LocoStructOffset(0x20), LocoStructVariableLoad, LocoArrayLength(IndustryObject.BuildingHeightCount)] List<uint8_t> BuildingPartHeights,    // This is the height of a building image
		[property: LocoStructOffset(0x24), LocoStructVariableLoad, LocoArrayLength(IndustryObject.BuildingAnimationCount)] List<BuildingPartAnimation> BuildingPartAnimations,
		[property: LocoStructOffset(0x28), LocoStructVariableLoad, LocoArrayLength(IndustryObject.AnimationSequencesCount)] List<List<uint8_t>> AnimationSequences, // Access with getAnimationSequence helper method
		[property: LocoStructOffset(0x38), LocoStructVariableLoad] List<IndustryObjectUnk38> var_38,    // Access with getUnk38 helper method
		[property: LocoStructOffset(0x3C), LocoStructVariableLoad, LocoArrayLength(IndustryObject.BuildingVariationCount)] List<List<uint8_t>> BuildingVariationParts,  // Access with getBuildingParts helper method
		[property: LocoStructOffset(0xBC)] uint8_t MinNumBuildings,
		[property: LocoStructOffset(0xBD)] uint8_t MaxNumBuildings,
		[property: LocoStructOffset(0xBE), LocoStructVariableLoad] List<uint8_t> Buildings,
		[property: LocoStructOffset(0xC2)] uint32_t Colours,  // bitset
		[property: LocoStructOffset(0xC6)] uint32_t BuildingSizeFlags, // flags indicating the building types size 1:large4x4, 0:small1x1
		[property: LocoStructOffset(0xCA)] uint16_t DesignedYear,
		[property: LocoStructOffset(0xCC)] uint16_t ObsoleteYear,
		[property: LocoStructOffset(0xCE)] uint8_t TotalOfTypeInScenario, // Total industries of this type that can be created in a scenario Note: this is not directly comparable to total industries and varies based on scenario total industries cap settings. At low industries cap this value is ~3x the amount of industries in a scenario.
		[property: LocoStructOffset(0xCF)] uint8_t CostIndex,
		[property: LocoStructOffset(0xD0)] int16_t BuildCostFactor,
		[property: LocoStructOffset(0xD2)] int16_t SellCostFactor,
		[property: LocoStructOffset(0xD4)] uint8_t ScaffoldingSegmentType,
		[property: LocoStructOffset(0xD5)] Colour ScaffoldingColour,
		[property: LocoStructOffset(0xD6), LocoArrayLength(IndustryObject.InitialProductionRateCount)] IndustryObjectProductionRateRange[] InitialProductionRate,
		[property: LocoStructOffset(0xDE), LocoStructVariableLoad, LocoArrayLength(IndustryObject.MaxProducedCargoType)] List<S5Header> ProducedCargo,   // (0xFF = null)
		[property: LocoStructOffset(0xE0), LocoStructVariableLoad, LocoArrayLength(IndustryObject.MaxRequiredCargoType)] List<S5Header> RequiredCargo, // (0xFF = null)
		[property: LocoStructOffset(0xE3)] Colour MapColour,
		[property: LocoStructOffset(0xE4)] IndustryObjectFlags Flags,
		[property: LocoStructOffset(0xE8)] uint8_t var_E8,
		[property: LocoStructOffset(0xE9)] uint8_t var_E9,
		[property: LocoStructOffset(0xEA)] uint8_t var_EA,
		[property: LocoStructOffset(0xEB)] uint8_t var_EB,
		[property: LocoStructOffset(0xEC)] uint8_t var_EC,
		[property: LocoStructOffset(0xED), LocoStructVariableLoad, LocoArrayLength(IndustryObject.MaxWallTypeCount)] List<S5Header> WallTypes, // There can be up to 4 different wall types for an industry
		[property: LocoStructOffset(0xF1), LocoStructVariableLoad, Browsable(false)] object_id _BuildingWall, // Selection of wall types isn't completely random from the 4 it is biased into 2 groups of 2 (wall and entrance)
		[property: LocoStructOffset(0xF2), LocoStructVariableLoad, Browsable(false)] object_id _BuildingWallEntrance, // An alternative wall type that looks like a gate placed at random places in building perimeter
		[property: LocoStructOffset(0xF3)] uint8_t var_F3
		) : ILocoStruct, ILocoStructVariableData
	{
		public const int AnimationSequencesCount = 4;
		public const int BuildingVariationCount = 32;
		public const int BuildingHeightCount = 4;
		public const int BuildingAnimationCount = 2;
		public const int InitialProductionRateCount = 2;
		public const int MaxProducedCargoType = 2;
		public const int MaxRequiredCargoType = 3;
		public const int MaxWallTypeCount = 4;

		public S5Header? BuildingWall { get; set; }

		public S5Header? BuildingWallEntrance { get; set; }

		public image_id var_0E { get; private set; } // shadows image id base
		public image_id var_12 { get; private set; } // Base image id for building 0
		public image_id var_16 { get; private set; }
		public image_id var_1A { get; private set; }

		public ReadOnlySpan<byte> Load(ReadOnlySpan<byte> remainingData)
		{
			// part heights
			BuildingPartHeights.Clear();
			BuildingPartHeights.AddRange(ByteReaderT.Read_Array<uint8_t>(remainingData[..(NumBuildingParts * 1)], NumBuildingParts));
			remainingData = remainingData[(NumBuildingParts * 1)..]; // uint8_t*

			// part animations
			BuildingPartAnimations.Clear();
			var buildingAnimationSize = ObjectAttributes.StructSize<BuildingPartAnimation>();
			BuildingPartAnimations.AddRange(ByteReader.ReadLocoStructArray(remainingData[..(NumBuildingParts * buildingAnimationSize)], typeof(BuildingPartAnimation), NumBuildingParts, buildingAnimationSize)
				.Cast<BuildingPartAnimation>());
			remainingData = remainingData[(NumBuildingParts * 2)..]; // uint16_t*

			// animation sequences
			AnimationSequences.Clear();
			for (var i = 0; i < AnimationSequencesCount; ++i)
			{
				var size = remainingData[0];
				byte[] arr = [];
				if (size != 0)
				{
					arr = remainingData[1..size].ToArray();
				}

				AnimationSequences.Add(arr.ToList());
				remainingData = remainingData[(size + 1)..];
			}

			// unk animation related
			var_38.Clear();
			var structSize = ObjectAttributes.StructSize<IndustryObjectUnk38>();
			while (remainingData[0] != 0xFF)
			{
				var_38.Add(ByteReader.ReadLocoStruct<IndustryObjectUnk38>(remainingData[..structSize]));
				remainingData = remainingData[structSize..];
			}

			remainingData = remainingData[1..]; // skip final 0xFF byte

			// variation parts
			BuildingVariationParts.Clear();
			for (var i = 0; i < NumBuildingVariations; ++i)
			{
				var ptr_1F = 0;
				while (remainingData[++ptr_1F] != 0xFF)
				{
					;
				}

				BuildingVariationParts.Add(remainingData[..ptr_1F].ToArray().ToList());
				ptr_1F++;
				remainingData = remainingData[ptr_1F..];
			}

			// unk building data
			Buildings.Clear();
			Buildings.AddRange(remainingData[..MaxNumBuildings].ToArray());
			remainingData = remainingData[MaxNumBuildings..];

			// produced cargo
			ProducedCargo.Clear();
			ProducedCargo.AddRange(SawyerStreamReader.LoadVariableCountS5Headers(remainingData, MaxProducedCargoType));
			remainingData = remainingData[(S5Header.StructLength * MaxProducedCargoType)..];

			// required cargo
			RequiredCargo.Clear();
			RequiredCargo.AddRange(SawyerStreamReader.LoadVariableCountS5Headers(remainingData, MaxRequiredCargoType));
			remainingData = remainingData[(S5Header.StructLength * MaxRequiredCargoType)..];

			// wall types
			WallTypes.Clear();
			WallTypes.AddRange(SawyerStreamReader.LoadVariableCountS5Headers(remainingData, MaxWallTypeCount));
			remainingData = remainingData[(S5Header.StructLength * MaxWallTypeCount)..];

			// wall type
			if (remainingData[0] != 0xFF)
			{
				BuildingWall = S5Header.Read(remainingData[..S5Header.StructLength]);
			}

			remainingData = remainingData[S5Header.StructLength..]; // there's always a struct, its just whether its zeroed out or not

			// wall type entrance
			if (remainingData[0] != 0xFF)
			{
				BuildingWallEntrance = S5Header.Read(remainingData[..S5Header.StructLength]);
			}

			remainingData = remainingData[S5Header.StructLength..]; // there's always a struct, its just whether its zeroed out or not

			// image stuff, in openloco it happens after image table load, but only to get image offsets, which we can just set to 0 here
			var_0E = 0;
			var_12 = var_0E;
			if (Flags.HasFlag(IndustryObjectFlags.HasShadows))
			{
				var_12 += NumBuildingVariations * 4u;
			}

			var_16 = (NumBuildingParts * 4u) + var_12;
			var_1A = var_E9 * 21u;

			return remainingData;
		}

		public ReadOnlySpan<byte> Save()
		{
			using (var ms = new MemoryStream())
			{
				// part heights
				foreach (var x in BuildingPartHeights)
				{
					ms.WriteByte(x);
				}

				// part animations
				foreach (var x in BuildingPartAnimations)
				{
					ms.WriteByte(x.NumFrames);
					ms.WriteByte(x.AnimationSpeed);
				}

				// animation sequences
				foreach (var x in AnimationSequences)
				{
					ms.WriteByte((uint8_t)x.Count);
					ms.Write(x.ToArray());
				}

				// unk animation related
				foreach (var x in var_38)
				{
					ms.WriteByte(x.var_00);
					ms.WriteByte(x.var_01);
				}

				ms.WriteByte(0xFF);

				// variation parts
				foreach (var x in BuildingVariationParts)
				{
					ms.Write(x.ToArray());
					ms.WriteByte(0xFF);
				}

				// unk building data
				ms.Write(Buildings.ToArray());

				// for the next 3 fields, loco industry objects print zeroes for all these headers if they're unused! insane!

				// produced cargo
				foreach (var obj in ProducedCargo.Fill(MaxProducedCargoType, S5Header.NullHeader))
				{
					ms.Write(obj.Write());
				}

				// required cargo
				foreach (var obj in RequiredCargo.Fill(MaxRequiredCargoType, S5Header.NullHeader))
				{
					ms.Write(obj.Write());
				}

				// wall types
				foreach (var obj in WallTypes.Fill(MaxWallTypeCount, S5Header.NullHeader))
				{
					ms.Write(obj.Write());
				}

				// wall type
				if (BuildingWall != null)
				{
					ms.Write(BuildingWall.Write());
				}
				else
				{
					ms.Write(S5Header.NullHeader.Write());
				}

				// wall type entrance
				if (BuildingWallEntrance != null)
				{
					ms.Write(BuildingWallEntrance.Write());
				}
				else
				{
					ms.Write(S5Header.NullHeader.Write());
				}

				return ms.ToArray();
			}
		}

		public bool Validate()
		{
			if (NumBuildingParts == 0)
			{
				return false;
			}

			if (NumBuildingVariations is 0 or > 31)
			{
				return false;
			}

			if (MaxNumBuildings < MinNumBuildings)
			{
				return false;
			}

			if (TotalOfTypeInScenario is 0 or > 32)
			{
				return false;
			}

			// 230/256 = ~90%
			if (-SellCostFactor > BuildCostFactor * 230 / 256)
			{
				return false;
			}

			if (var_E8 > 8)
			{
				return false;
			}

			switch (var_E9)
			{
				case 1:
				case 2:
				case 4:
					break;
				default:
					return false;
			}

			if (var_EA is not 0xFF and > 7)
			{
				return false;
			}

			if (var_EC > 8)
			{
				return false;
			}

			if (InitialProductionRate[0].Min > 100)
			{
				return false;
			}

			return InitialProductionRate[1].Min <= 100;
		}
	}
}
