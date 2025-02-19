using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoLicenceEntry(
		int Id,
		string Name,
		string LicenceText) : IHasId;
}
