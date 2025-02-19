using Definitions;

namespace OpenLoco.Definitions.DTO
{
	public record DtoItemPackDescriptor<T>(
		int Id,
		string Name,
		string? Description,
		ICollection<T> Items,
		ICollection<DtoAuthorEntry> Authors,
		DateTimeOffset? CreationDate,
		DateTimeOffset? LastEditDate,
		DateTimeOffset UploadDate,
		ICollection<DtoTagEntry> Tags,
		DtoLicenceEntry? Licence) : IHasId;
}
