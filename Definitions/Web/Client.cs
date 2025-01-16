using Microsoft.AspNetCore.Http;
using OpenLoco.Common.Logging;
using OpenLoco.Definitions.DTO;
using System.Net.Http.Json;

namespace OpenLoco.Definitions.Web
{
	public static class Client
	{
		public const string Version = "v1";

		public static class Get
		{
			public static async Task<IEnumerable<DtoObjectIndexEntry>> ObjectListAsync(HttpClient client, ILogger? logger = null)
				=> await SendJsonRequestAsync<IEnumerable<DtoObjectIndexEntry>?>(client, Routes.ListObjects, logger) ?? [];

			public static async Task<DtoDatObjectWithMetadata?> DatAsync(HttpClient client, string objectName, uint checksum, bool returnObjBytes, ILogger? logger = null)
				=> await SendJsonRequestAsync<DtoDatObjectWithMetadata?>(client, Routes.GetDat + $"?{nameof(objectName)}={objectName}&{nameof(checksum)}={checksum}&{nameof(returnObjBytes)}={returnObjBytes}", logger);

			public static async Task<DtoDatObjectWithMetadata?> ObjectAsync(HttpClient client, int uniqueObjectId, bool returnObjBytes, ILogger? logger = null)
				=> await SendJsonRequestAsync<DtoDatObjectWithMetadata?>(client, Routes.GetObject + $"?{nameof(uniqueObjectId)}={uniqueObjectId}&{nameof(returnObjBytes)}={returnObjBytes}", logger);

			public static async Task<DtoDatObjectWithMetadata?> DatFileAsync(HttpClient client, string objectName, uint checksum, ILogger? logger = null)
				=> await SendJsonRequestAsync<DtoDatObjectWithMetadata?>(client, Routes.GetDatFile + $"?{nameof(objectName)}={objectName}&{nameof(checksum)}={checksum}", logger);

			public static async Task<DtoDatObjectWithMetadata?> ObjectFileAsync(HttpClient client, int uniqueObjectId, ILogger? logger = null)
				=> await SendJsonRequestAsync<DtoDatObjectWithMetadata?>(client, Routes.GetDatFile + $"?{nameof(uniqueObjectId)}={uniqueObjectId}", logger);

			public static async Task<Stream?> ObjectImagesAsync(HttpClient client, int uniqueObjectId, ILogger? logger = null)
				=> await SendStreamRequestAsync(client, Routes.GetObjectImages + $"?{nameof(uniqueObjectId)}={uniqueObjectId}", logger);

			public static async Task<IEnumerable<DtoScenarioIndexEntry>> ScenarioListAsync(HttpClient client, ILogger? logger = null)
				=> await SendJsonRequestAsync<IEnumerable<DtoScenarioIndexEntry>?>(client, Routes.ListScenarios, logger) ?? [];
			public static async Task<DtoDatObjectWithMetadata?> ScenarioAsync(HttpClient client, string objectName, uint checksum, bool returnObjBytes, ILogger? logger = null)
				=> throw new NotImplementedException();

			static async Task<T?> SendJsonRequestAsync<T>(HttpClient client, string route, ILogger? logger = null)
				=> await SendRequestAsync(client, route, ReadJsonContent<T>, logger);

			static async Task<Stream?> SendStreamRequestAsync(HttpClient client, string route, ILogger? logger = null)
				=> await SendRequestAsync(client, route, ReadStreamContent, logger);

			static async Task<T?> ReadJsonContent<T>(HttpContent content)
				=> await content.ReadFromJsonAsync<T?>();

			static async Task<Stream?> ReadStreamContent(HttpContent content)
				=> await content.ReadAsStreamAsync();

			static async Task<T?> SendRequestAsync<T>(HttpClient client, string route, Func<HttpContent, Task<T?>> contentParserAsync, ILogger? logger = null)
			{
				try
				{
					route = string.Concat(Version, route);
					logger?.Debug($"Querying {client.BaseAddress}{route}");
					using var response = await client.GetAsync(route);

					if (!response.IsSuccessStatusCode)
					{
						logger?.Error($"Request failed: {response}");
						return default;
					}

					logger?.Debug("Main server queried successfully");

					var data = await contentParserAsync(response.Content);

					if (data == null)
					{
						logger?.Error($"Received data but couldn't parse it: {response}");
						return default;
					}

					return data;
				}
				catch (Exception ex)
				{
					logger?.Error(ex);
					return default;
				}
			}
		}

		public static class Post
		{
			public static async Task UploadObjectFileAsync(HttpClient client, string filename, byte[] datFileBytes, DateTimeOffset creationDate, ILogger logger)
				=> throw new NotImplementedException();

			public static async Task<bool> UploadDatFileAsync(HttpClient client, string filename, byte[] datFileBytes, DateTimeOffset creationDate, ILogger logger)
			{
				try
				{
					var route = $"{client.BaseAddress?.OriginalString}{Routes.UploadDat}";
					logger.Info($"Posting {filename} to {route}");
					var request = new DtoUploadDat(Convert.ToBase64String(datFileBytes), creationDate);
					var response = await client.PostAsJsonAsync(Routes.UploadDat, request);
					_ = response.EnsureSuccessStatusCode();
					logger.Info($"Uploaded {filename} to main server successfully");
					return true;
				}
				catch (Exception ex)
				{
					logger.Error(ex);
					return false;
				}
			}
		}
	}
}
