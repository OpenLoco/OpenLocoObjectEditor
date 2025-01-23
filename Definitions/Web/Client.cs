using Microsoft.AspNetCore.Http;
using OpenLoco.Common.Logging;
using OpenLoco.Definitions.DTO;
using System.Net.Http.Json;

namespace OpenLoco.Definitions.Web
{
	public static class Client
	{
		public const string Version = "v1";

		public static async Task<IEnumerable<DtoObjectDescriptor>> GetObjectListAsync(HttpClient client, ILogger? logger = null)
			=> await SendRequestAsync<IEnumerable<DtoObjectDescriptor>?>(client, Routes.ListObjects, logger) ?? [];

		public static async Task<DtoObjectDescriptorWithMetadata?> GetDatAsync(HttpClient client, string objectName, uint checksum, bool returnObjBytes, ILogger? logger = null)
			=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, Routes.GetDat + $"?{nameof(objectName)}={objectName}&{nameof(checksum)}={checksum}&{nameof(returnObjBytes)}={returnObjBytes}", logger);

		public static async Task<DtoObjectDescriptorWithMetadata?> GetObjectAsync(HttpClient client, int id, bool returnObjBytes, ILogger? logger = null)
			=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, Routes.GetObject + $"?{nameof(id)}={id}&{nameof(returnObjBytes)}={returnObjBytes}", logger);

		public static async Task<DtoObjectDescriptorWithMetadata?> GetDatFileAsync(HttpClient client, string objectName, uint checksum, ILogger? logger = null)
			=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, Routes.GetDatFile + $"?{nameof(objectName)}={objectName}&{nameof(checksum)}={checksum}", logger);

		public static async Task<DtoObjectDescriptorWithMetadata?> GetObjectFileAsync(HttpClient client, int id, ILogger? logger = null)
			=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, Routes.GetDatFile + $"?{nameof(id)}={id}", logger);

		static async Task<T?> SendRequestAsync<T>(HttpClient client, string route, ILogger? logger = null)
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

				var data = await response.Content.ReadFromJsonAsync<T?>();
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

		public static async Task UploadDatFileAsync(HttpClient client, string filename, byte[] datFileBytes, DateTimeOffset creationDate, ILogger logger)
		{
			try
			{
				var route = $"{client.BaseAddress?.OriginalString}{Routes.UploadDat}";
				logger.Debug($"Posting {filename} to {route}");
				var request = new DtoUploadDat(Convert.ToBase64String(datFileBytes), creationDate);
				var response = await client.PostAsJsonAsync(Routes.UploadDat, request);

				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();

					if (string.IsNullOrEmpty(error))
					{
						logger.Error($"Posting {filename} failed. StatusCode={response.StatusCode} ReasonPhrase={response.ReasonPhrase}");
					}
					else
					{
						logger.Error($"Posting {filename} failed. Error={error}");
					}

					return;
				}

				logger.Debug($"Uploaded {filename} to main server successfully");
			}
			catch (Exception ex)
			{
				logger.Error(ex);
			}
		}
	}
}
