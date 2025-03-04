using Microsoft.EntityFrameworkCore;

namespace OpenLoco.Definitions.Database
{
	[Index(nameof(Name), IsUnique = true)]
	public class TblTag
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public ICollection<TblLocoObject> Objects { get; set; }
		public ICollection<TblLocoObjectPack> ObjectPacks { get; set; }
		public ICollection<TblSC5File> SC5Files { get; set; }
		public ICollection<TblSC5FilePack> SC5FilePacks { get; set; }
	}
}
