using Common.Json;
using Definitions.Database.Objects;
using Microsoft.EntityFrameworkCore;
using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Data;
using OpenLoco.Definitions;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.SourceData;
using System.Text.Json;
using System.Text.Json.Serialization;

using var db = Seed();

Console.WriteLine("done");
Console.ReadLine();

static LocoDb Seed()
{
	var builder = new DbContextOptionsBuilder<LocoDb>();
	const string connectionString = "Data Source=Q:\\Games\\Locomotion\\Server\\loco.db";
	_ = builder.UseSqlite(connectionString);
	var db = new LocoDb(builder.Options);

	// Note: The database must exist before this script works
	Console.WriteLine($"Database path: {connectionString}");

	const bool seed = true;
	const bool DeleteExisting = true;

	if (seed)
	{
		SeedDb(db, DeleteExisting);
	}

	return db;
}

static void SeedDb(LocoDb db, bool deleteExisting)
{
	if (deleteExisting)
	{
		Console.WriteLine("Clearing database");
		_ = db.ObjectPacks.ExecuteDelete();
		_ = db.SC5FilePacks.ExecuteDelete();
		_ = db.SC5Files.ExecuteDelete();
		_ = db.Tags.ExecuteDelete();
		_ = db.Authors.ExecuteDelete();
		_ = db.Objects.ExecuteDelete();
		_ = db.Licences.ExecuteDelete();
		_ = db.SaveChanges(); // not necessary since ExecuteDelete auto-saves
	}

	const string objDirectory = "Q:\\Games\\Locomotion\\Server\\Objects";

	const string databaseDirectory = "Q:\\Games\\Locomotion\\Database";
	var authorsJson = Path.Combine(databaseDirectory, "authors.json");
	var tagsJson = Path.Combine(databaseDirectory, "tags.json");
	var licencesJson = Path.Combine(databaseDirectory, "licences.json");
	var sc5FilePacksJson = Path.Combine(databaseDirectory, "sc5FilePacks.json");
	var sc5FilesJson = Path.Combine(databaseDirectory, "sc5Files.json");
	var objectPacksJson = Path.Combine(databaseDirectory, "objectPacks.json");
	var objectMetadataJson = Path.Combine(databaseDirectory, "objectMetadata.json");

	Console.WriteLine("Seeding");
	var logger = new Logger();

	var jsonOptions = JsonFile.SerializerOptions;
	jsonOptions.Converters.Add(new JsonStringEnumConverter());

	// ...

	if (!db.Authors.Any())
	{
		Console.WriteLine("Seeding Authors");

		var authors = JsonSerializer.Deserialize<IEnumerable<string>>(File.ReadAllText(authorsJson), jsonOptions);
		if (authors != null)
		{
			db.AddRange(authors.Select(x => new TblAuthor()
			{
				Name = x
			}));

			_ = db.SaveChanges();
		}
	}

	// ...

	if (!db.Tags.Any())
	{
		Console.WriteLine("Seeding Tags");

		var tags = JsonSerializer.Deserialize<IEnumerable<string>>(File.ReadAllText(tagsJson), jsonOptions);
		if (tags != null)
		{
			db.AddRange(tags.Select(x => new TblTag()
			{
				Name = x
			}));

			_ = db.SaveChanges();
		}
	}

	// ...

	if (!db.Licences.Any())
	{
		Console.WriteLine("Seeding Licences");

		var licences = JsonSerializer.Deserialize<IEnumerable<LicenceJsonRecord>>(File.ReadAllText(licencesJson), jsonOptions);
		if (licences != null)
		{
			db.AddRange(licences.Select(x => new TblLicence()
			{
				Name = x.Name,
				Text = x.Text
			}));

			_ = db.SaveChanges();
		}
	}

	// ...

	if (!db.SC5FilePacks.Any())
	{
		Console.WriteLine("Seeding SC5FilePacks");

		var sc5FilePacks = JsonSerializer.Deserialize<IEnumerable<SC5FilePackJsonRecord>>(File.ReadAllText(sc5FilePacksJson), jsonOptions);
		if (sc5FilePacks != null)
		{
			db.AddRange(sc5FilePacks.Select(x => new TblSC5FilePack()
			{
				Name = x.Name,
				Description = x.Description,
				Authors = [.. db.Authors.Where(a => x.Authors.Contains(a.Name))],
				Tags = [.. db.Tags.Where(a => x.Tags.Contains(a.Name))],
				Licence = x.Licence == null ? null : db.Licences.Single(l => l.Name == x.Licence),
				CreationDate = x.CreationDate,
				LastEditDate = x.LastEditDate,
				UploadDate = x.UploadDate,
			}));

			_ = db.SaveChanges();
		}
	}

	// ...

	if (!db.SC5Files.Any())
	{
		Console.WriteLine("Seeding SC5Files");

		var sc5Files = JsonSerializer.Deserialize<IEnumerable<SC5FileJsonRecord>>(File.ReadAllText(sc5FilesJson), jsonOptions);
		if (sc5Files != null)
		{
			var sC5FilePacks = db.SC5FilePacks?.Where(x => x.SC5Files.Select(fp => fp.Name).Contains(x.Name)).ToList();
			db.AddRange(sc5Files.Select(x => new TblSC5File()
			{
				Name = x.Name,
				Description = x.Description,
				Authors = [.. db.Authors.Where(a => x.Authors.Contains(a.Name))],
				SC5FilePacks = sC5FilePacks ?? []
			}));

			_ = db.SaveChanges();
		}
	}

	// ...

	if (!db.ObjectPacks.Any())
	{
		Console.WriteLine("Seeding ObjectPacks");

		var objectPacks = JsonSerializer.Deserialize<IEnumerable<ObjectPackJsonRecord>>(File.ReadAllText(objectPacksJson), jsonOptions);
		if (objectPacks != null)
		{
			db.AddRange(objectPacks.Select(x => new TblLocoObjectPack()
			{
				Name = x.Name,
				Description = x.Description,
				Authors = [.. db.Authors.Where(a => x.Authors.Contains(a.Name))],
				Tags = [.. db.Tags.Where(a => x.Tags.Contains(a.Name))],
				Licence = x.Licence == null ? null : db.Licences.Single(l => l.Name == x.Licence),
				CreationDate = null,
				LastEditDate = null,
				UploadDate = DateTimeOffset.Now
			}));
			_ = db.SaveChanges();
		}
	}

	// ...

	if (!db.Objects.Any())
	{
		Console.WriteLine("Seeding Objects");

		var progress = new Progress<float>();
		var index = ObjectIndex.LoadOrCreateIndex(objDirectory, logger);
		var objectMetadata = JsonSerializer.Deserialize<IEnumerable<ObjectMetadata>>(File.ReadAllText(objectMetadataJson), jsonOptions);
		var objectMetadataDict = objectMetadata!.ToDictionary(x => (x.DatName, x.DatChecksum), x => x);
		var gameReleaseDate = new DateTimeOffset(2004, 09, 07, 0, 0, 0, TimeSpan.Zero);

		foreach (var objIndex in index!.Objects.DistinctBy(x => (x.DatName, x.DatChecksum)))
		{
			var metadataKey = (objIndex.DatName, objIndex.DatChecksum);
			if (!objectMetadataDict.TryGetValue(metadataKey, out var meta))
			{
				var newMetadata = new ObjectMetadata(Guid.NewGuid().ToString(), objIndex.DatName, objIndex.DatChecksum, null, [], [], [], null, ObjectAvailability.AllGames, DateTimeOffset.Now, null, DateTimeOffset.Now, ObjectSource.Custom);
				meta = newMetadata;
				objectMetadataDict.Add((objIndex.DatName, objIndex.DatChecksum), newMetadata);
			}

			var filename = Path.Combine(objDirectory, objIndex.Filename);
			var creationTime = objIndex.ObjectSource is ObjectSource.LocomotionSteam or ObjectSource.LocomotionGoG
				? gameReleaseDate
				: File.GetLastWriteTimeUtc(filename); // this is the "Modified" time as shown in Windows

			var authors = meta.Authors == null ? null : db.Authors.Where(x => meta.Authors.Contains(x.Name)).ToList();
			var tags = meta.Tags == null ? null : db.Tags.Where(x => meta.Tags.Contains(x.Name)).ToList();
			var objectPacks = meta.ObjectPacks == null ? null : db.ObjectPacks.Where(x => meta.ObjectPacks.Contains(x.Name)).ToList();
			var licence = meta.Licence == null ? null : db.Licences.Where(x => x.Name == meta.Licence).First();

			var tblLocoObject = new TblLocoObject()
			{
				Name = meta!.UniqueName,
				DatName = objIndex.DatName,
				DatChecksum = objIndex.DatChecksum,
				ObjectSource = objIndex.ObjectSource,
				ObjectType = objIndex.ObjectType,
				VehicleType = objIndex.VehicleType,
				Description = meta?.Description,
				Authors = authors ?? [],
				CreationDate = creationTime,
				LastEditDate = null,
				UploadDate = DateTimeOffset.Now,
				Tags = tags ?? [],
				ObjectPacks = objectPacks ?? [],
				Availability = meta!.ObjectAvailability,
				Licence = licence,
			};

			_ = db.Add(tblLocoObject);
		}

		_ = db.SaveChanges();
	}

	Console.WriteLine("Finished seeding");
}
