﻿using System.ComponentModel;
using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;

namespace OpenLocoTool.Objects
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public record CliffEdgeObject(
		[property: LocoStructProperty(0x00)] string_id Name,
		[property: LocoStructProperty(0x02)] uint32_t Image
		) : ILocoStruct
	{
		public ObjectType ObjectType => ObjectType.cliffEdge;

		public static int ObjectStructSize => 0x6;

		public static ILocoStruct Read(ReadOnlySpan<byte> data)
		{
			throw new NotImplementedException("");
		}

		public ReadOnlySpan<byte> Write()
		{
			throw new NotImplementedException("");
		}
	}
}
