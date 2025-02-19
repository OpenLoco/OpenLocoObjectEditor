using Common.Json;
using Definitions.Database.Objects;
using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.SourceData;
using System.Text.Json;

var builder = new DbContextOptionsBuilder<LocoDb>();
const string connectionString = "Data Source=Q:\\Games\\Locomotion\\Database\\loco.db";
_ = builder.UseSqlite(connectionString);
var db = new LocoDb(builder.Options);

Console.WriteLine("loading");

var authors = JsonSerializer.Serialize<IEnumerable<string>>(db.Authors.Select(a => a.Name).ToList().Order(), JsonFile.SerializerOptions);
var tags = JsonSerializer.Serialize<IEnumerable<string>>(db.Tags.Select(t => t.Name).ToList().Order(), JsonFile.SerializerOptions);
var licences = JsonSerializer.Serialize<IEnumerable<LicenceJsonRecord>>(db.Licences.Select(l => new LicenceJsonRecord(l.Name, l.Text)).ToList().OrderBy(l => l.Name), JsonFile.SerializerOptions);

#region SC5 Files

var sc5Files = new List<SC5FileJsonRecord>();

foreach (var o in db.SC5Files
		.Include(l => l.Licence)
		.Select(x => new ExpandedTbl<TblSC5File, TblSC5FilePack>(x, x.Authors, x.Tags, x.SC5FilePacks))
		.ToList()
		.OrderBy(x => x.Object.Name))
{
	var obj = new SC5FileJsonRecord(
		o.Object.Name,
		o.Object.Description,
		o.Authors.Select(a => a.Name).ToList(),
		o.Tags.Select(t => t.Name).ToList(),
		o.Object.Licence?.Name,
		o.Object.CreationDate,
		o.Object.LastEditDate,
		o.Object.UploadDate,
		o.Object.ObjectSource);

	sc5Files.Add(obj);
}

var sc5FilesJson = JsonSerializer.Serialize<IEnumerable<SC5FileJsonRecord>>(sc5Files, JsonFile.SerializerOptions);

#endregion

#region SC5 Packs

var sc5FilePacks = new List<SC5FilePackJsonRecord>();

foreach (var o in db.SC5FilePacks
		.Include(l => l.Licence)
		.Select(x => new ExpandedTblPack<TblSC5FilePack, TblSC5File>(x, x.SC5Files, x.Authors, x.Tags))
		.ToList()
		.OrderBy(x => x.Pack.Name))
{
	var obj = new SC5FilePackJsonRecord(
		o.Pack.Name,
		o.Pack.Description,
		o.Authors.Select(a => a.Name).ToList(),
		o.Tags.Select(t => t.Name).ToList(),
		o.Pack.Licence?.Name,
		o.Pack.CreationDate,
		o.Pack.LastEditDate,
		o.Pack.UploadDate);

	sc5FilePacks.Add(obj);
}

var sc5FilePacksJson = JsonSerializer.Serialize<IEnumerable<SC5FilePackJsonRecord>>(sc5FilePacks, JsonFile.SerializerOptions);

#endregion

#region Object Packs

var objectPacks = new List<ObjectPackJsonRecord>();

foreach (var o in db.ObjectPacks
		.Include(l => l.Licence)
		.Select(x => new ExpandedTblPack<TblLocoObjectPack, TblLocoObject>(x, x.Objects, x.Authors, x.Tags))
		.ToList()
		.OrderBy(x => x.Pack.Name))
{
	var objPack = new ObjectPackJsonRecord(
		o.Pack.Name,
		o.Pack.Description,
		o.Authors.Select(a => a.Name).ToList(),
		o.Tags.Select(t => t.Name).ToList(),
		o.Pack.Licence?.Name,
		o.Pack.CreationDate,
		o.Pack.LastEditDate,
		o.Pack.UploadDate);

	objectPacks.Add(objPack);
}

var objectPacksJson = JsonSerializer.Serialize<IEnumerable<ObjectPackJsonRecord>>(objectPacks, JsonFile.SerializerOptions);

#endregion

#region Objects

var objects = new List<ObjectMetadata>();

foreach (var o in db.Objects
		.Include(l => l.Licence)
		.Select(x => new ExpandedTbl<TblLocoObject, TblLocoObjectPack>(x, x.Authors, x.Tags, x.ObjectPacks))
		.ToList()
		.OrderBy(x => x.Object.Name))
{
	var obj = new ObjectMetadata(
		o.Object.Name,
		o.Object.DatName,
		o.Object.DatChecksum,
		o.Object.Description,
		o.Authors.Select(a => a.Name).ToList(),
		o.Tags.Select(t => t.Name).ToList(),
		o.Packs.Select(m => m.Name).ToList(),
		o.Object.Licence?.Name,
		o.Object.Availability,
		o.Object.CreationDate,
		o.Object.LastEditDate,
		o.Object.UploadDate,
		o.Object.ObjectSource);
	objects.Add(obj);
}

var objectsJson = JsonSerializer.Serialize<IEnumerable<ObjectMetadata>>(objects, JsonFile.SerializerOptions);

#endregion

Console.WriteLine("writing");

File.WriteAllText("Q:\\Games\\Locomotion\\Database\\authors.json", authors);
File.WriteAllText("Q:\\Games\\Locomotion\\Database\\tags.json", tags);
File.WriteAllText("Q:\\Games\\Locomotion\\Database\\licences.json", licences);
File.WriteAllText("Q:\\Games\\Locomotion\\Database\\objectPacks.json", objectPacksJson);
File.WriteAllText("Q:\\Games\\Locomotion\\Database\\objectMetadata.json", objectsJson);
File.WriteAllText("Q:\\Games\\Locomotion\\Database\\sc5Files.json", sc5FilesJson);
File.WriteAllText("Q:\\Games\\Locomotion\\Database\\sc5FilePacks.json", sc5FilePacksJson);

Console.WriteLine("done");
