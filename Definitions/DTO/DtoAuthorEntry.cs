using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoAuthorEntry(
		int Id,
		string Name) : IHasId;
}
