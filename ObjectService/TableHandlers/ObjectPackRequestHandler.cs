using Definitions.Database.Objects;
using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.SourceData;
using OpenLoco.Definitions.Web;

namespace ObjectService.TableHandlers
{
	public class ObjectPackRequestHandler : BaseTableRequestHandler<DtoItemPackDescriptor<DtoObjectEntry>>
	{
		public override string BaseRoute
			=> Routes.ObjectPacks;

		public override async Task<IResult> CreateAsync(DtoItemPackDescriptor<DtoObjectEntry> request, LocoDb db)
			=> throw new NotImplementedException();

		public override async Task<IResult> ReadAsync(int id, LocoDb db)
			=> Results.Ok(
				(await db.ObjectPacks
					.Where(x => x.Id == id)
					.Include(l => l.Licence)
					.Select(x => new ExpandedTblPack<TblLocoObjectPack, TblLocoObject>(x, x.Objects, x.Authors, x.Tags))
					.ToListAsync())
				.Select(x => x.ToDtoDescriptor())
				.OrderBy(x => x.Name));

		public override async Task<IResult> UpdateAsync(DtoItemPackDescriptor<DtoObjectEntry> request, LocoDb db)
			=> throw new NotImplementedException();

		public override async Task<IResult> DeleteAsync(int id, LocoDb db)
			=> throw new NotImplementedException();

		public override async Task<IResult> ListAsync(LocoDb db)
			=> Results.Ok(
				(await db.ObjectPacks
					.Include(l => l.Licence)
					.ToListAsync())
				.Select(x => x.ToDtoEntry())
				.OrderBy(x => x.Name));
	}
}
