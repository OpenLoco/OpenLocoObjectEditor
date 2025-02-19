using Definitions.Database.Objects;
using Microsoft.EntityFrameworkCore;
using OpenLoco.Dat.Types;

namespace OpenLoco.Definitions.Database
{
	public class LocoDb : DbContext
	{
		#region ReferenceData

		public DbSet<TblAuthor> Authors => Set<TblAuthor>();
		public DbSet<TblTag> Tags => Set<TblTag>();
		public DbSet<TblLicence> Licences => Set<TblLicence>();

		#endregion

		#region UserData

		public DbSet<TblLocoObject> Objects => Set<TblLocoObject>();
		public DbSet<TblLocoObjectPack> ObjectPacks => Set<TblLocoObjectPack>();
		public DbSet<TblSC5File> SC5Files => Set<TblSC5File>();
		public DbSet<TblSC5FilePack> SC5FilePacks => Set<TblSC5FilePack>();

		#endregion

		public LocoDb()
		{ }

		public LocoDb(DbContextOptions<LocoDb> options) : base(options)
		{ }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				_ = optionsBuilder.UseSqlite("Data Source=Q:\\Games\\Locomotion\\Server\\loco.db");
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			_ = modelBuilder.Entity<TblLocoObject>()
				.Property(b => b.UploadDate)
				.HasDefaultValueSql("datetime(datetime('now', 'localtime'), 'utc')"); // this is necessary, it seems like a bug in sqlite
			_ = modelBuilder.Entity<TblSC5File>()
				.Property(b => b.UploadDate)
				.HasDefaultValueSql("datetime(datetime('now', 'localtime'), 'utc')"); // this is necessary, it seems like a bug in sqlite
			_ = modelBuilder.Entity<TblLocoObjectPack>()
				.Property(b => b.UploadDate)
				.HasDefaultValueSql("datetime(datetime('now', 'localtime'), 'utc')"); // this is necessary, it seems like a bug in sqlite
			_ = modelBuilder.Entity<TblSC5FilePack>()
				.Property(b => b.UploadDate)
				.HasDefaultValueSql("datetime(datetime('now', 'localtime'), 'utc')"); // this is necessary, it seems like a bug in sqlite
		}

		public bool DoesObjectExist(S5Header s5Header, out TblLocoObject? existingObject)
		 => DoesObjectExist(s5Header.Name, s5Header.Checksum, out existingObject);

		public bool DoesObjectExist(string datName, uint datChecksum, out TblLocoObject? existingObject)
		{
			// there's a unique constraint on the composite key index (DatName, DatChecksum), so check existence first so no exceptions
			// this isn't necessary since we're already filtering in LINQ, but if we were adding to a non-empty database, this would be necessary
			var existingEntityInDb = Objects
				.FirstOrDefault(e => e.DatName == datName && e.DatChecksum == datChecksum);

			var existingEntityInChangeTracker = ChangeTracker.Entries()
				.Where(e => e.State == EntityState.Added && e.Entity.GetType() == typeof(TblLocoObject))
				.Select(e => e.Entity as TblLocoObject)
				.FirstOrDefault(e => e!.DatName == datName && e.DatChecksum == datChecksum);

			existingObject = existingEntityInDb ?? existingEntityInChangeTracker;
			return existingObject != null;
		}
	}
}
