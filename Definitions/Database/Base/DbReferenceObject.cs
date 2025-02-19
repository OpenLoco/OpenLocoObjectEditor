using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OpenLoco.Definitions.Database
{
	[Index(nameof(Name), IsUnique = true)]
	public abstract class DbReferenceObject : IDbReferenceData
	{
		public int Id { get; set; }

		[Required]
		public required string Name { get; set; }
	}
}
