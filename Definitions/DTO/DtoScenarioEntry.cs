using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoScenarioEntry(
		int Id,
		string Name) : IHasId;

	public record DtoScenarioDescriptor(
		int Id,
		string Name,
		string? Description) : IHasId;
}
