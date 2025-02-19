using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoObjectEntry(
		int Id,
		string DatName,
		uint DatChecksum) : IHasId;
}
