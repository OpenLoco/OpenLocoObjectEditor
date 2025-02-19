using Definitions.Database.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using OpenLoco.ObjectService;
using SixLabors.ImageSharp;
using System.IO.Compression;

namespace ObjectService.TableHandlers
{
	public class ObjectRequestHandler : BaseTableRequestHandler<DtoObjectDescriptor>
	{
		ServerFolderManager ServerFolderManager { get; init; }
		PaletteMap PaletteMap { get; init; }

		public ObjectRequestHandler(ServerFolderManager sfm, PaletteMap paletteMap)
		{
			ServerFolderManager = sfm;
			PaletteMap = paletteMap;
		}

		public override string BaseRoute
			=> Routes.Objects;

		public override async Task<IResult> CreateAsync(DtoObjectDescriptor request, LocoDb db)
			=> await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));

		public override void MapAdditionalRoutes(IEndpointRouteBuilder parentRoute)
		{
			var resourceRoute = parentRoute.MapGroup(Routes.ResourceRoute);
			_ = resourceRoute.MapGet(Routes.File, GetObjectFile);
			_ = resourceRoute.MapGet(Routes.Images, GetObjectImages);
		}

		public override async Task<IResult> ReadAsync(int id, LocoDb db)
		{
			var eObj = await db.Objects
				.Where(x => x.Id == id)
				.Include(x => x.Licence)
				.Select(x => new ExpandedTbl<TblLocoObject, TblLocoObjectPack>(x, x.Authors, x.Tags, x.ObjectPacks))
				.SingleOrDefaultAsync();

			return ReturnObject(eObj);
		}

		public override async Task<IResult> UpdateAsync(DtoObjectDescriptor request, LocoDb db)
			=> await Task.Run(() => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));

		public override async Task<IResult> DeleteAsync(int id, LocoDb db)
			=> Results.Ok(await db.Objects
				.Select(x => x.ToDtoDescriptor())
				.ToListAsync());

		public override async Task<IResult> ListAsync(LocoDb db)
			=> Results.Ok(
				await db.Objects
					.Include(l => l.Licence)
					.Select(x => x.ToDtoDescriptor())
					.ToListAsync());

		public static IResult ReturnObject(ExpandedTbl<TblLocoObject, TblLocoObjectPack>? eObj)
			=> eObj == null || eObj.Object == null
				? Results.NotFound()
				: Results.Ok(eObj.ToDtoDescriptor());

		// eg: https://localhost:7230/v1/objects/list
		public static async Task<IResult> SearchObjects(
			[FromQuery] string? objectName,
			[FromQuery] uint? checksum,
			[FromQuery] string? description,
			[FromQuery] ObjectType? objectType,
			[FromQuery] VehicleType? vehicleType,
			[FromQuery] string? authorName,
			[FromQuery] string? tagName,
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
				var result = await query
					.Select(x => x.ToDtoDescriptor())
					.ToListAsync();

				return Results.Ok(result);
			}
			catch (Exception ex)
			{
				return Results.Problem(ex.Message);
			}
		}

		// eg: https://localhost:7230/v1/objects/getdat?objectName=114&checksum=123$returnObjBytes=false
		public async Task<IResult> GetDat([FromQuery] string objectName, [FromQuery] uint checksum, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			//logger.LogInformation("Object [({ObjectName}, {Checksum})] requested", objectName, checksum);

			var eObj = await db.Objects
				.Where(x => x.DatName == objectName && x.DatChecksum == checksum)
				.Include(x => x.Licence)
				.Select(x => new ExpandedTbl<TblLocoObject, TblLocoObjectPack>(x, x.Authors, x.Tags, x.ObjectPacks))
				.SingleOrDefaultAsync();

			return ReturnObject(eObj);
		}

		// eg: http://localhost:7229/v1/objects/getobjectimages?uniqueObjectId=1
		public async Task<IResult> GetObjectImages(int uniqueObjectId, LocoDb db, [FromServices] ILogger<Server> logger)
		{
			//Console.WriteLine($"Object [{uniqueObjectId}] requested with images");

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
																							   //logger.LogInformation("Loading file from {PathOnDisk}", pathOnDisk);

			var fileExists = File.Exists(pathOnDisk);
			if (!fileExists)
			{
				//logger.LogWarning("Indexed object had {PathOnDisk} but the file wasn't found there; suggest re-indexing the server object folder.", pathOnDisk);
				return Results.NotFound();
			}

			if (obj.ObjectSource is ObjectSource.LocomotionGoG or ObjectSource.LocomotionSteam)
			{
				//logger.LogWarning("Indexed object is a vanilla object.");
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
			return Results.File(bytes, "application/zip", "images.zip");
		}

		// eg: https://localhost:7230/v1/objects/originaldatfile?objectName=114&checksum=123
		public async Task<IResult> GetDatFile([FromQuery] string objectName, [FromQuery] uint checksum, LocoDb db)
		{
			var obj = await db.Objects
				.Where(x => x.DatName == objectName && x.DatChecksum == checksum)
				.SingleOrDefaultAsync();

			return ReturnFile(obj);
		}

		// eg: https://localhost:7230/v1/objects/getobjectfile?objectName=114&checksum=123
		public async Task<IResult> GetObjectFile([FromRoute] int id, LocoDb db)
		{
			var obj = await db.Objects
				.Where(x => x.Id == id)
				.SingleOrDefaultAsync();

			return ReturnFile(obj);
		}

		IResult ReturnFile(TblLocoObject? obj)
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

			const string contentType = "application/octet-stream";

			var path = Path.Combine(ServerFolderManager.ObjectsFolder, index!.Filename);
			return obj != null && File.Exists(path)
				? Results.File(path, contentType, Path.GetFileName(path))
				: Results.NotFound();
		}

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

			var (_, LocoObject) = SawyerStreamReader.LoadFullObjectFromStream(datFileBytes, ssrLogger);
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

	}
}
