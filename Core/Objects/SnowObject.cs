using System.ComponentModel;
using OpenLoco.ObjectEditor.Data;
using OpenLoco.ObjectEditor.DatFileParsing;
using OpenLoco.ObjectEditor.Types;

namespace OpenLoco.ObjectEditor.Objects
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[LocoStructSize(0x06)]
	[LocoStructType(ObjectType.Snow)]
	[LocoStringTable("Name")]
	public record SnowObject(
		[property: LocoStructOffset(0x00), LocoString, Browsable(false)] string_id Name,
		[property: LocoStructOffset(0x02), Browsable(false)] image_id Image
		) : ILocoStruct, ILocoImageTableNames
	{
		public bool Validate() => true;

		public bool TryGetImageName(int id, out string? value)
			=> ImageIdNameMap.TryGetValue(id, out value);

		public static Dictionary<int, string> ImageIdNameMap = new()
		{
			{ 0, "surfaceEighthZoom" },
			{ 10, "outlineEighthZoom" },
			{ 19, "surfaceQuarterZoom" },
			{ 38, "outlineQuarterZoom" },
			{ 57, "surfaceHalfZoom" },
			{ 76, "outlineHalfZoom" },
			{ 95, "surfaceFullZoom" },
			{ 114, "outlineFullZoom" },
		};
	}
}
