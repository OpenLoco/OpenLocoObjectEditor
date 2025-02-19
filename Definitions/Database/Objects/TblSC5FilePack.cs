using OpenLoco.Definitions.Database;

namespace Definitions.Database.Objects
{
	public class TblSC5FilePack : DbCoreObject
	{
		public ICollection<TblSC5File> SC5Files { get; set; } = [];
	}
}
