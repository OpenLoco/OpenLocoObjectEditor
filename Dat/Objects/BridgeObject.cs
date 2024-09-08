using OpenLoco.Dat.Data;
using OpenLoco.Dat.FileParsing;
using OpenLoco.Dat.Types;
using System.ComponentModel;

namespace OpenLoco.Dat.Objects
{
	[Flags]
	public enum BridgeDisabledTrackFlags : uint16_t
	{
		None = 0,
		Slope = 1 << 0,
		SteepSlope = 1 << 1,
		CurveSlope = 1 << 2,
		Diagonal = 1 << 3,
		VerySmallCurve = 1 << 4,
		SmallCurve = 1 << 5,
		Curve = 1 << 6,
		LargeCurve = 1 << 7,
		SBendCurve = 1 << 8,
		OneSided = 1 << 9,
		StartsAtHalfHeight = 1 << 10, // Not used. From RCT2
		Junction = 1 << 11,
	}

	[TypeConverter(typeof(ExpandableObjectConverter))]
	[LocoStructSize(0x2C)]
	[LocoStructType(ObjectType.Bridge)]
	[LocoStringTable("Name")]
	public record BridgeObject(
		[property: LocoStructOffset(0x00), LocoString, Browsable(false)] string_id Name,
		[property: LocoStructOffset(0x02)] uint8_t NoRoof,
		[property: LocoStructOffset(0x03), LocoStructVariableLoad, LocoArrayLength(BridgeObject._03PadSize), LocoPropertyMaybeUnused, Browsable(false)] uint8_t[] pad_03,
		[property: LocoStructOffset(0x06)] uint16_t var_06,
		[property: LocoStructOffset(0x08)] uint8_t SpanLength,
		[property: LocoStructOffset(0x09)] uint8_t PillarSpacing,
		[property: LocoStructOffset(0x0A)] Speed16 MaxSpeed,
		[property: LocoStructOffset(0x0C)] MicroZ MaxHeight,
		[property: LocoStructOffset(0x0D)] uint8_t CostIndex,
		[property: LocoStructOffset(0x0E)] int16_t BaseCostFactor,
		[property: LocoStructOffset(0x10)] int16_t HeightCostFactor,
		[property: LocoStructOffset(0x12)] int16_t SellCostFactor,
		[property: LocoStructOffset(0x14)] BridgeDisabledTrackFlags DisabledTrackFlags,
		[property: LocoStructOffset(0x16), Browsable(false)] image_id Image,
		[property: LocoStructOffset(0x1A)] uint8_t NumCompatibleTrackMods,
		[property: LocoStructOffset(0x1B), LocoStructVariableLoad, LocoArrayLength(BridgeObject.MaxNumTrackMods), LocoPropertyMaybeUnused, Browsable(false)] object_id[] TrackModHeaderIds,
		[property: LocoStructOffset(0x22)] uint8_t NumCompatibleRoadMods,
		[property: LocoStructOffset(0x23), LocoStructVariableLoad, LocoArrayLength(BridgeObject.MaxNumRoadMods), LocoPropertyMaybeUnused, Browsable(false)] object_id[] RoadModHeaderIds,
		[property: LocoStructOffset(0x2A)] uint16_t DesignedYear
		) : ILocoStruct, ILocoStructVariableData
	{
		public const int _03PadSize = 3;
		public const int MaxNumTrackMods = 7;
		public const int MaxNumRoadMods = 7;

		[LocoPropertyMaybeUnused]
		public List<S5Header> TrackCompatibleMods { get; set; } = [];

		[LocoPropertyMaybeUnused]
		public List<S5Header> RoadCompatibleMods { get; set; } = [];

		public ReadOnlySpan<byte> Load(ReadOnlySpan<byte> remainingData)
		{
			// compatible tracks
			TrackCompatibleMods = SawyerStreamReader.LoadVariableCountS5Headers(remainingData, NumCompatibleTrackMods);
			remainingData = remainingData[(S5Header.StructLength * NumCompatibleTrackMods)..];

			// compatible roads
			RoadCompatibleMods = SawyerStreamReader.LoadVariableCountS5Headers(remainingData, NumCompatibleRoadMods);
			remainingData = remainingData[(S5Header.StructLength * NumCompatibleRoadMods)..];

			return remainingData;
		}

		public ReadOnlySpan<byte> Save()
		{
			var headers = TrackCompatibleMods
				.Concat(RoadCompatibleMods);

			return headers.SelectMany(h => h.Write().ToArray()).ToArray();
		}

		public bool Validate()
		{
			if (CostIndex > 32)
			{
				return false;
			}

			if (-SellCostFactor > BaseCostFactor)
			{
				return false;
			}

			if (BaseCostFactor <= 0)
			{
				return false;
			}

			if (HeightCostFactor < 0)
			{
				return false;
			}

			if (var_06 is not 16 and not 32)
			{
				return false;
			}

			if (SpanLength is not 1 and not 2 and not 4)
			{
				return false;
			}

			if (NumCompatibleTrackMods > 7)
			{
				return false;
			}

			return NumCompatibleRoadMods <= 7;
		}
	}
}
