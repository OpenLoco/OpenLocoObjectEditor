using Definitions.Database.Objects;
using Microsoft.EntityFrameworkCore;

namespace OpenLoco.Definitions.Database
{
	[Index(nameof(Name), IsUnique = true)]
	public class TblAuthor : DbReferenceObject
	{
		public ICollection<TblLocoObject> Objects { get; set; } = [];
		public ICollection<TblLocoObjectPack> ObjectPacks { get; set; } = [];
		public ICollection<TblSC5File> SC5Files { get; set; } = [];
		public ICollection<TblSC5FilePack> SC5FilePacks { get; set; } = [];
	}
}
