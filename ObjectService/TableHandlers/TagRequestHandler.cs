using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.Web;

namespace ObjectService.TableHandlers
{
	public class TagRequestHandler : BaseReferenceDataTableRequestHandler<DtoTagEntry, TblTag>
	{
		public override string BaseRoute
			=> Routes.Tags;

		protected override DbSet<TblTag> GetTable(LocoDb db)
			=> db.Tags;

		protected override void UpdateFunc(DtoTagEntry request, TblTag row)
			=> row.Name = request.Name;

		protected override TblTag ToRowFunc(DtoTagEntry request)
			=> request.ToTable();

		protected override DtoTagEntry ToDtoFunc(TblTag table)
			=> table.ToDtoEntry();

		protected override bool TryValidateCreate(DtoTagEntry request, LocoDb db, out IResult? result)
		{
			if (string.IsNullOrWhiteSpace(request.Name))
			{
				result = Results.BadRequest("Cannot add an empty or whitespace-only name.");
				return false;
			}

			result = null;
			return true;
		}
	}
}
