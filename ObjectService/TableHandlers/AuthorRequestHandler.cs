using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.Web;

namespace ObjectService.TableHandlers
{
	[Tags("AuthorRequestHandler")]
	public class AuthorRequestHandler : BaseReferenceDataTableRequestHandler<DtoAuthorEntry, TblAuthor>
	{
		public override string BaseRoute
			=> Routes.Authors;

		protected override DbSet<TblAuthor> GetTable(LocoDb db)
			=> db.Authors;

		protected override void UpdateFunc(DtoAuthorEntry request, TblAuthor row)
			=> row.Name = request.Name;

		protected override TblAuthor ToRowFunc(DtoAuthorEntry request)
			=> request.ToTable();

		protected override DtoAuthorEntry ToDtoFunc(TblAuthor table)
			=> table.ToDtoEntry();

		protected override bool TryValidateCreate(DtoAuthorEntry request, LocoDb db, out IResult? result)
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
