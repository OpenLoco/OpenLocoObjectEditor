using Definitions;
using OpenLoco.Dat.Data;
using OpenLoco.Dat.Objects;
using OpenLoco.Definitions.Database;

namespace OpenLoco.Definitions.DTO
{
	public record DtoObjectDescriptorWithMetadata(
		int Id,
		string UniqueName,
		string DatName,
		uint DatChecksum,
		ObjectSource ObjectSource,
		ObjectType ObjectType,
		VehicleType? VehicleType,
		string? Description,
		ICollection<DtoAuthorEntry> Authors,
		DateTimeOffset? CreationDate,
		DateTimeOffset? LastEditDate,
		DateTimeOffset UploadDate,
		ICollection<DtoTagEntry> Tags,
		ICollection<DtoItemPackDescriptor<DtoObjectEntry>> ObjectPacks,
		ObjectAvailability Availability,
		TblLicence? Licence) : IHasId;
}
