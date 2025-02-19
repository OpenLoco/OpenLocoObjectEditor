using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace OpenLoco.Definitions.Database
{
	[Index(nameof(Name), IsUnique = true)]
	public abstract class DbCoreObject : DbReferenceObject, IDbStandardData, IDbMetadata
	{
		#region IDbStandardInfo

		public string? Description { get; set; }

		#endregion

		#region IDbMetadata

		public TblLicence? Licence { get; set; }

		public ICollection<TblAuthor> Authors { get; set; } = [];

		public ICollection<TblTag> Tags { get; set; } = [];

		public DateTimeOffset? CreationDate { get; set; }

		public DateTimeOffset? LastEditDate { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Computed), NotNull]
		public DateTimeOffset UploadDate { get; set; }

		#endregion
	}
}
