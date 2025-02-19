using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoItemPackEntry(
		int Id,
		string Name,
		string? Description,
		DateTimeOffset? CreationDate,
		DateTimeOffset? LastEditDate,
		DateTimeOffset UploadDate,
		DtoLicenceEntry? Licence) : IHasId;
}
