using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Data;
using OpenLoco.Dat.FileParsing;
using OpenLoco.Dat.Objects;
using OpenLoco.Definitions;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.SourceData;
using OpenLoco.Definitions.Web;
using SixLabors.ImageSharp;
using System.IO.Compression;

namespace OpenLoco.ObjectService
{
	public static class ContentTypes
	{
		public const string OctetStream = "application/octet-stream";
		public const string Zip = "application/zip";
	}

	public class Server
	{
		public Server(ServerSettings settings)
		{
			Settings = settings;
			ServerFolderManager = new ServerFolderManager(Settings.RootFolder)!;
			PaletteMap = new PaletteMap(Settings.PaletteMapFile);
		}

		public Server(IOptions<ServerSettings> options) : this(options.Value)
		{ }

		ServerSettings Settings { get; init; }

		ServerFolderManager ServerFolderManager { get; init; }

		PaletteMap PaletteMap { get; init; }

		Common.Logging.ILogger Logger { get; } = new Logger();

		#region GET

		// eg: https://localhost:7230/v1/objects/list
		public static async Task<IResult> ListObjects(
			[FromQuery] string? objectName,
			[FromQuery] uint? checksum,
			[FromQuery] string? description,
			[FromQuery] ObjectType? objectType,
			[FromQuery] VehicleType? vehicleType,
			[FromQuery] string? authorName,
			[FromQuery] string? tagName,
			[FromQuery] string? objectPackName,
			[FromQuery] ObjectSource? objectSource,
			LocoDb db)
		{
			var query = db.Objects.AsQueryable();

			#region Query Construction

			if (!string.IsNullOrEmpty(objectName))
			{
				query = query.Where(x => x.DatName.Contains(objectName));
			}

			if (checksum is not null and not 0)
			{
				query = query.Where(x => x.DatChecksum == checksum);
			}

			if (!string.IsNullOrEmpty(description))
			{
				query = query.Where(x => x.Description != null && x.Description.Contains(description));
			}

			if (objectType != null)
			{
				query = query.Where(x => x.ObjectType == objectType);
			}

			if (objectType == ObjectType.Vehicle && vehicleType != null)
			{
				// can only query vehicle type if it's a vehicle. if ObjectType is unspecified, that is fine
				if (objectType is not null and not ObjectType.Vehicle)
				{
					return Results.BadRequest("Cannot query for a Vehicle type on non-Vehicle objects");
				}

				query = query.Where(x => x.VehicleType == vehicleType);
			}

			if (!string.IsNullOrEmpty(authorName))
			{
				query = query.Where(x => x.Authors.Select(a => a.Name).Contains(authorName));
			}

			if (!string.IsNullOrEmpty(tagName))
			{
				query = query.Where(x => x.Tags.Select(t => t.Name).Contains(tagName));
			}

			if (objectSource != null)
			{
				query = query.Where(x => x.ObjectSource == objectSource);
			}

			#endregion

			try
			{
				var queryStringForDebug = query.ToQueryString();
				var result = await query
				.Select(x => new DtoObjectIndexEntry(
					x.Id,
					x.DatName,
					x.DatChecksum,
					x.ObjectSource,
					x.ObjectType,
					x.VehicleType,
					x.Availability,
					x.Name,
					x.Description,
					x.CreationDate,
					x.LastEditDate,
					x.UploadDate
					)).ToListAsync();
				return Results.Ok(result);
			}
			catch (Exception ex)
			{
				return Results.Problem(ex.Message);
			}
		}

		// eg: https://localhost:7230/v1/objects/getdat?objectName=114&checksum=123$returnObjBytes=false
		public async Task<IResult> GetDat([FromQuery] string objectName, [FromQuery] uint checksum, [FromQuery] bool? returnObjBytes, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			logger.LogInformation("Object [({ObjectName}, {Checksum})] requested", objectName, checksum);

			var eObj = await db.Objects
				.Where(x => x.DatName == objectName && x.DatChecksum == checksum)
				.Include(x => x.Licence)
				.Select(x => new ExpandedTbl<TblLocoObject, TblLocoObjectPack>(x, x.Authors, x.Tags, x.ObjectPacks))
				.SingleOrDefaultAsync();

			return await ReturnObject(returnObjBytes, logger, eObj);
		}

		// eg: http://localhost:7229/v1/objects/getobjectimages?uniqueObjectId=1
		public async Task<IResult> GetObjectImages(int uniqueObjectId, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			Console.WriteLine($"Object [{uniqueObjectId}] requested with images");

			var obj = await db.Objects.Where(x => x.Id == uniqueObjectId).SingleOrDefaultAsync();

			if (obj == null)
			{
				return Results.NotFound();
			}

			if (!ServerFolderManager.ObjectIndex.TryFind((obj.DatName, obj.DatChecksum), out var index))
			{
				return Results.NotFound();
			}

			var pathOnDisk = Path.Combine(ServerFolderManager.ObjectsFolder, index!.Filename); // handle windows paths by replacing path separator
			logger.LogInformation("Loading file from {PathOnDisk}", pathOnDisk);

			var fileExists = File.Exists(pathOnDisk);
			if (!fileExists)
			{
				logger.LogWarning("Indexed object had {PathOnDisk} but the file wasn't found there; suggest re-indexing the server object folder.", pathOnDisk);
				return Results.NotFound();
			}

			if (obj.ObjectSource is ObjectSource.LocomotionGoG or ObjectSource.LocomotionSteam)
			{
				logger.LogWarning("Indexed object is a vanilla object.");
				return Results.Forbid();
			}

			var dummyLogger = new Logger(); // todo: make both libraries and server use a single logging interface
			var locoObj = SawyerStreamReader.LoadFullObjectFromFile(pathOnDisk, dummyLogger, true);

			await using var memoryStream = new MemoryStream();
			using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
			{
				var count = 0;
				foreach (var g1 in locoObj!.Value!.LocoObject!.G1Elements)
				{
					if (!PaletteMap.TryConvertG1ToRgba32Bitmap(g1, out var image))
					{
						continue;
					}

					await using (var pngStream = new MemoryStream())
					{
						await image.SaveAsPngAsync(pngStream);
						pngStream.Position = 0;

						var zipEntry = zipArchive.CreateEntry(count++ + ".png", CompressionLevel.Optimal);
						await using (var zipEntryStream = zipEntry.Open())
						{
							await pngStream.CopyToAsync(zipEntryStream);
						}
					}
				}
			}

			memoryStream.Position = 0; // Reset stream position for reading
			var bytes = memoryStream.ToArray();
			return Results.File(bytes, ContentTypes.Zip, "images.zip");
		}

		// eg: https://localhost:7230/v1/objects/getobject?uniqueObjectId=246263256&returnObjBytes=false
		public async Task<IResult> GetObject([FromQuery] int uniqueObjectId, [FromQuery] bool? returnObjBytes, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			logger.LogInformation("Object [{UniqueObjectId}] requested", uniqueObjectId);

			var eObj = await db.Objects
				.Where(x => x.Id == uniqueObjectId)
				.Include(x => x.Licence)
				.Select(x => new ExpandedTbl<TblLocoObject, TblLocoObjectPack>(x, x.Authors, x.Tags, x.ObjectPacks))
				.SingleOrDefaultAsync();

			return await ReturnObject(returnObjBytes, logger, eObj);
		}

		async Task<IResult> ReturnObject(bool? returnObjBytes, ILogger<Server> logger, ExpandedTbl<TblLocoObject, TblLocoObjectPack>? eObj)
		{
			if (eObj == null || eObj.Object == null)
			{
				return Results.NotFound();
			}

			if (!ServerFolderManager.ObjectIndex.TryFind((eObj.Object.DatName, eObj.Object.DatChecksum), out var index))
			{
				return Results.NotFound();
			}

			var obj = eObj!.Object;

			var pathOnDisk = Path.Combine(ServerFolderManager.ObjectsFolder, index!.Filename); // handle windows paths by replacing path separator
			logger.LogInformation("Loading file from {PathOnDisk}", pathOnDisk);

			var fileExists = File.Exists(pathOnDisk);
			if (!fileExists)
			{
				logger.LogWarning("Indexed object had {PathOnDisk} but the file wasn't found there; suggest re-indexing the server object folder.", pathOnDisk);
			}

			var bytes = (returnObjBytes ?? false) && (obj.ObjectSource is ObjectSource.Custom or ObjectSource.OpenLoco) && fileExists
				? Convert.ToBase64String(await File.ReadAllBytesAsync(pathOnDisk))
				: null;

			var dtoObject = new DtoDatObjectWithMetadata(
				obj.Id,
				obj.Name,
				obj.DatName,
				obj.DatChecksum,
				bytes,
				obj.ObjectSource,
				obj.ObjectType,
				obj.VehicleType,
				obj.Description,
				eObj.Authors,
				obj.CreationDate,
				obj.LastEditDate,
				obj.UploadDate,
				eObj.Tags,
				eObj.Packs,
				obj.Availability,
				obj.Licence);

			return Results.Ok(dtoObject);
		}

		// eg: https://localhost:7230/v1/objects/originaldatfile?objectName=114&checksum=123
		public async Task<IResult> GetDatFile([FromQuery] string objectName, [FromQuery] uint checksum, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			var obj = await db.Objects
				.Where(x => x.DatName == objectName && x.DatChecksum == checksum)
				.SingleOrDefaultAsync();

			return ReturnFile(obj, logger);
		}

		// eg: https://localhost:7230/v1/objects/getobjectfile?objectName=114&checksum=123
		public async Task<IResult> GetObjectFile([FromQuery] int uniqueObjectId, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			var obj = await db.Objects
				.Where(x => x.Id == uniqueObjectId)
				.SingleOrDefaultAsync();

			return ReturnFile(obj, logger);
		}

		IResult ReturnFile(TblLocoObject? obj, ILogger<Server> logger)
		{
			if (obj == null)
			{
				return Results.NotFound();
			}

			if (obj.ObjectSource is ObjectSource.LocomotionGoG or ObjectSource.LocomotionSteam)
			{
				return Results.Forbid();
			}

			if (!ServerFolderManager.ObjectIndex.TryFind((obj.DatName, obj.DatChecksum), out var index))
			{
				return Results.NotFound();
			}

			var path = Path.Combine(ServerFolderManager.ObjectsFolder, index!.Filename);

			if (!File.Exists(path))
			{
				return Results.NotFound();
			}

			logger.LogInformation("File [{file}] requested", path);

			return Results.File(path, ContentTypes.OctetStream, Path.GetFileName(path));
		}

		// eg: https://localhost:7230/v1/scenarios/list
		public async Task<IResult> ListScenarios()
			=> await Task.Run(() =>
			{
				var files = Directory.GetFiles(ServerFolderManager.ScenariosFolder, "*.SC5", SearchOption.AllDirectories);
				var count = 0;
				var filenames = files.Select(x => new DtoScenarioIndexEntry(count++, Path.GetRelativePath(ServerFolderManager.ScenariosFolder, x)));
				return Results.Ok(filenames.ToList());
			});

		// eg: https://localhost:7230/v1/scenarios/getscenario?uniqueScenarioId=246263256&returnObjBytes=false
		public async Task<IResult> GetScenario([FromQuery] int uniqueScenarioId, [FromQuery] bool? returnObjBytes, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			logger.LogInformation("Scenario [{ScenarioId}] requested", uniqueScenarioId);
			return await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));
		}

		#endregion

		#region POST

		// eg: https://localhost:7230/v1/uploaddat/...
		public async Task<IResult> UploadDat(DtoUploadDat request, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			logger.LogInformation("Upload requested");

			if (string.IsNullOrEmpty(request.DatBytesAsBase64))
			{
				return Results.BadRequest($"{nameof(request.DatBytesAsBase64)} cannot be null - it must contain the valid bytes of a loco dat object.");
			}

			byte[]? datFileBytes;
			try
			{
				datFileBytes = Convert.FromBase64String(request.DatBytesAsBase64);
			}
			catch (FormatException ex)
			{
				return Results.BadRequest(ex.Message);
			}

			if (datFileBytes == null || datFileBytes.Length == 0)
			{
				return Results.BadRequest($"Unable to decode {nameof(request.DatBytesAsBase64)} - it must contain the valid bytes of a loco dat object.");
			}

			if (datFileBytes.Length > ServerLimits.MaximumUploadFileSize)
			{
				return Results.BadRequest("Unable to accept file sizes > 5MB");
			}

			var ssrLogger = new Logger();
			if (!SawyerStreamReader.TryGetHeadersFromBytes(datFileBytes, out var hdrs, ssrLogger))
			{
				return Results.BadRequest("Provided data had invalid dat file headers");
			}

			if (hdrs.S5.IsVanilla())
			{
				return Results.BadRequest("Nice try genius. Uploading vanilla objects is not allowed.");
			}

			if (!hdrs.S5.IsValid() || !hdrs.Obj.IsValid())
			{
				return Results.BadRequest("Invalid DAT file.");
			}

			if (db.DoesObjectExist(hdrs.S5, out var existingObject))
			{
				return Results.Accepted($"Object already exists in the database. DatName={hdrs.S5.Name} DatChecksum={hdrs.S5.Checksum} UploadDate={existingObject!.UploadDate}");
			}

			// at this stage, headers must be valid. we can add it to the object index/database, even if the remainder of the object is invalid

			var (_, LocoObject) = SawyerStreamReader.LoadFullObjectFromStream(datFileBytes, Logger);
			var uuid = Guid.NewGuid();
			var saveFileName = Path.Combine(ServerFolderManager.ObjectsCustomFolder, $"{uuid}.dat");
			File.WriteAllBytes(saveFileName, datFileBytes);

			logger.LogInformation("File accepted DatName={DatName} DatChecksum={DatChecksum} PathOnDisk={SaveFileName}", hdrs.S5.Name, hdrs.S5.Checksum, saveFileName);

			var creationTime = request.CreationDate;

			VehicleType? vehicleType = null;
			if (LocoObject?.Object is VehicleObject veh)
			{
				vehicleType = veh.Type;
			}

			var locoTbl = new TblLocoObject()
			{
				Name = $"{hdrs.S5.Name}_{hdrs.S5.Checksum}", // same as DB seeder name
				DatName = hdrs.S5.Name,
				DatChecksum = hdrs.S5.Checksum,
				ObjectSource = ObjectSource.Custom, // not possible to upload vanilla objects
				ObjectType = hdrs.S5.ObjectType,
				VehicleType = vehicleType,
				Description = string.Empty,
				Authors = [],
				CreationDate = creationTime,
				LastEditDate = null,
				UploadDate = DateTimeOffset.UtcNow,
				Tags = [],
				ObjectPacks = [],
				Availability = LocoObject == null ? ObjectAvailability.Unavailable : ObjectAvailability.AllGames,
				Licence = null,
			};

			ServerFolderManager.ObjectIndex.Objects.Add(new ObjectIndexEntry(saveFileName, locoTbl.DatName, locoTbl.DatChecksum, locoTbl.ObjectType, locoTbl.ObjectSource, locoTbl.VehicleType));

			_ = db.Objects.Add(locoTbl);
			_ = await db.SaveChangesAsync();

			return Results.Created($"Successfully added {locoTbl.Name} with unique id {locoTbl.Id}", locoTbl.Id);
		}

		public async Task<IResult> UploadObject([FromServices] ILogger<Server> logger)
		{
			logger.LogWarning("[UploadDat] - not implemented");
			return await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));
		}

		#endregion

		#region PATCH

		public async Task<IResult> UpdateDat([FromServices] ILogger<Server> logger)
		{
			logger.LogWarning("[UploadDat] - not implemented");
			return await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));
		}

		public async Task<IResult> UpdateObject([FromServices] ILogger<Server> logger)
		{
			logger.LogWarning("[UploadDat] - not implemented");
			return await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));
		}

		#endregion
	}
}
