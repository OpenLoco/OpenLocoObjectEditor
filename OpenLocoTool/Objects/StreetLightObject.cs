﻿using System.ComponentModel;
using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;

namespace OpenLocoTool.Objects
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[LocoStructSize(0x0C)]
	[LocoStructType(ObjectType.StreetLight)]
	public record StreetLightObject(
		[property: LocoStructOffset(0x00), LocoStructSkipRead, LocoString, Browsable(false)] string_id Name,
		[property: LocoStructOffset(0x02), LocoArrayLength(StreetLightObject.DesignedYearLength)] uint16_t[] DesignedYear,
		[property: LocoStructOffset(0x08), LocoStructSkipRead, Browsable(false)] uint32_t Image
		) : ILocoStruct
	{
		public const int DesignedYearLength = 3;
	}
}
