using OpenLoco.Dat.FileParsing;

namespace Core.Types.SCV5
{
	public class Animation
	{
		[LocoArrayLength(0x06)] public uint8_t[] pad_0 { get; set; }
	};
}
