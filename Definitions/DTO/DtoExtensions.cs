using Definitions.Database.Objects;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.SourceData;

namespace OpenLoco.Definitions.DTO
{
	public static class DtoExtensions
	{
		public static DtoObjectDescriptorWithMetadata ToDtoDescriptor(this ExpandedTbl<TblLocoObject, TblLocoObjectPack> x)
		{
			var obj = x!.Object;

			var dtoObject = new DtoObjectDescriptorWithMetadata(
				obj.Id,
				obj.Name,
				obj.DatName,
				obj.DatChecksum,
				obj.ObjectSource,
				obj.ObjectType,
				obj.VehicleType,
				obj.Description,
				x.Authors.Select(x => x.ToDtoEntry()).ToList(),
				obj.CreationDate,
				obj.LastEditDate,
				obj.UploadDate,
				x.Tags.Select(x => x.ToDtoEntry()).ToList(),
				x.Packs.Select(x => x.ToDtoEntry()).ToList(),
				obj.Availability,
				obj.Licence);

			return dtoObject;
		}

		public static DtoItemPackDescriptor<DtoObjectEntry> ToDtoDescriptor(this ExpandedTblPack<TblLocoObjectPack, TblLocoObject> x)
			=> new(
				x.Pack.Id,
				x.Pack.Name,
				x.Pack.Description,
				x.Items.Select(x => x.ToDtoEntry()).ToList(),
				x.Authors.Select(x => x.ToDtoEntry()).ToList(),
				x.Pack.CreationDate,
				x.Pack.LastEditDate,
				x.Pack.UploadDate,
				x.Tags.Select(x => x.ToDtoEntry()).ToList(),
				x.Pack.Licence?.ToDtoEntry());

		public static DtoItemPackDescriptor<DtoScenarioEntry> ToDtoDescriptor(this ExpandedTblPack<TblSC5FilePack, TblSC5File> x)
			=> new(
				x.Pack.Id,
				x.Pack.Name,
				x.Pack.Description,
				x.Items.Select(x => x.ToDtoEntry()).ToList(),
				x.Authors.Select(x => x.ToDtoEntry()).ToList(),
				x.Pack.CreationDate,
				x.Pack.LastEditDate,
				x.Pack.UploadDate,
				x.Tags.Select(x => x.ToDtoEntry()).ToList(),
				x.Pack.Licence?.ToDtoEntry());

		#region New

		public static DtoObjectDescriptor ToDtoDescriptor(this TblLocoObject table)
			=> new(table.Id, table.DatName, table.DatChecksum, table.ObjectSource, table.ObjectType, table.VehicleType, table.Availability, table.Name, table.Description, table.CreationDate, table.LastEditDate, table.UploadDate);

		public static DtoObjectEntry ToDtoEntry(this TblLocoObject table)
			=> new(table.Id, table.DatName, table.DatChecksum);

		public static TblLocoObject ToTable(this DtoObjectEntry dto)
			=> new() { Name = dto.DatName, DatName = dto.DatName, Id = dto.Id, DatChecksum = dto.DatChecksum };

		public static DtoScenarioEntry ToDtoEntry(this TblSC5File table)
			=> new(table.Id, table.Name);

		public static TblSC5File ToTable(this DtoScenarioEntry dto)
			=> new() { Name = dto.Name, Id = dto.Id };

		public static DtoAuthorEntry ToDtoEntry(this TblAuthor table)
			=> new(table.Id, table.Name);

		public static TblAuthor ToTable(this DtoAuthorEntry dto)
			=> new() { Name = dto.Name, Id = dto.Id };

		public static DtoTagEntry ToDtoEntry(this TblTag table)
			=> new(table.Id, table.Name);

		public static TblTag ToTable(this DtoTagEntry dto)
			=> new() { Name = dto.Name, Id = dto.Id };

		public static DtoLicenceEntry ToDtoEntry(this TblLicence table)
			=> new(table.Id, table.Name, table.Text);

		public static TblLicence ToTable(this DtoLicenceEntry dto)
			=> new() { Name = dto.Name, Id = dto.Id, Text = dto.LicenceText };

		public static DtoItemPackDescriptor<DtoObjectEntry> ToDtoEntry(this TblLocoObjectPack x)
			=> new(
				x.Id,
				x.Name,
				x.Description,
				x.Objects.Select(x => x.ToDtoEntry()).ToList(),
				x.Authors.Select(x => x.ToDtoEntry()).ToList(),
				x.CreationDate,
				x.LastEditDate,
				x.UploadDate,
				x.Tags.Select(x => x.ToDtoEntry()).ToList(),
				x.Licence?.ToDtoEntry());

		public static TblLocoObjectPack ToTable(this DtoItemPackDescriptor<DtoObjectEntry> x)
			=> new()
			{
				Id = x.Id,
				Name = x.Name,
				Description = x.Description,
				Objects = x.Items.Select(x => x.ToTable()).ToList(),
				Authors = x.Authors.Select(x => x.ToTable()).ToList(),
				CreationDate = x.CreationDate,
				LastEditDate = x.LastEditDate,
				UploadDate = x.UploadDate,
				Tags = x.Tags.Select(x => x.ToTable()).ToList(),
				Licence = x.Licence?.ToTable()
			};

		public static DtoItemPackDescriptor<DtoScenarioEntry> ToDtoEntry(this TblSC5FilePack x)
			=> new(
				x.Id,
				x.Name,
				x.Description,
				x.SC5Files.Select(x => x.ToDtoEntry()).ToList(),
				x.Authors.Select(x => x.ToDtoEntry()).ToList(),
				x.CreationDate,
				x.LastEditDate,
				x.UploadDate,
				x.Tags.Select(x => x.ToDtoEntry()).ToList(),
				x.Licence?.ToDtoEntry());

		public static TblSC5FilePack ToTable(this DtoItemPackDescriptor<DtoScenarioEntry> x)
			=> new()
			{
				Id = x.Id,
				Name = x.Name,
				Description = x.Description,
				SC5Files = x.Items.Select(x => x.ToTable()).ToList(),
				Authors = x.Authors.Select(x => x.ToTable()).ToList(),
				CreationDate = x.CreationDate,
				LastEditDate = x.LastEditDate,
				UploadDate = x.UploadDate,
				Tags = x.Tags.Select(x => x.ToTable()).ToList(),
				Licence = x.Licence?.ToTable()
			};

		#endregion
	}
}
