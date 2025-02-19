using OpenLoco.Definitions.Database;

namespace Definitions.Database.Objects
{
	public class TblLocoObjectPack : DbCoreObject
	{
		public ICollection<TblLocoObject> Objects { get; set; } = [];
	}
}
