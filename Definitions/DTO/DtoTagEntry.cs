using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoTagEntry(
		int Id,
		string Name) : IHasId;
}
