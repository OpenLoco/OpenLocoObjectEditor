using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.Web;

namespace ObjectService.TableHandlers
{
	public class LicenceRequestHandler : BaseReferenceDataTableRequestHandler<DtoLicenceEntry, TblLicence>
	{
		public override string BaseRoute
			=> Routes.Licences;

		protected override DbSet<TblLicence> GetTable(LocoDb db)
			=> db.Licences;

		protected override void UpdateFunc(DtoLicenceEntry request, TblLicence row)
			=> row.Name = request.Name;

		protected override TblLicence ToRowFunc(DtoLicenceEntry request)
			=> request.ToTable();

		protected override DtoLicenceEntry ToDtoFunc(TblLicence table)
			=> table.ToDtoEntry();

		protected override bool TryValidateCreate(DtoLicenceEntry request, LocoDb db, out IResult? result)
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
