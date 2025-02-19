using Definitions.Database.Objects;
using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.SourceData;
using OpenLoco.Definitions.Web;

namespace ObjectService.TableHandlers
{
	public class SC5FilePackRequestHandler : BaseTableRequestHandler<DtoItemPackDescriptor<DtoScenarioEntry>>
	{
		public override string BaseRoute
			=> Routes.SC5FilePacks;

		public override async Task<IResult> CreateAsync(DtoItemPackDescriptor<DtoScenarioEntry> request, LocoDb db)
			=> await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));

		public override async Task<IResult> ReadAsync(int id, LocoDb db)
			=> Results.Ok(
				(await db.SC5FilePacks
					.Where(x => x.Id == id)
					.Include(l => l.Licence)
					.Select(x => new ExpandedTblPack<TblSC5FilePack, TblSC5File>(x, x.SC5Files, x.Authors, x.Tags))
					.ToListAsync())
				.Select(x => x.ToDtoDescriptor())
				.OrderBy(x => x.Name));

		public override async Task<IResult> UpdateAsync(DtoItemPackDescriptor<DtoScenarioEntry> request, LocoDb db)
			=> await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));

		public override async Task<IResult> DeleteAsync(int id, LocoDb db)
			=> await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));

		public override async Task<IResult> ListAsync(LocoDb db)
			=> Results.Ok(
				(await db.SC5FilePacks
					.Include(l => l.Licence)
					.ToListAsync())
				.Select(x => x.ToDtoEntry())
				.OrderBy(x => x.Name));
	}
}
