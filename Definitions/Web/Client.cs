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
			=> await SendRequestAsync<IEnumerable<DtoObjectDescriptor>?>(client, Routes.Objects, ReadJsonContentAsync<IEnumerable<DtoObjectDescriptor>?>, logger) ?? [];

		//public static async Task<DtoObjectDescriptorWithMetadata?> GetDatAsync(HttpClient client, string objectName, uint checksum, ILogger? logger = null)
		//	=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, OldRoutes.GetDat + $"?{nameof(objectName)}={objectName}&{nameof(checksum)}={checksum}", logger);

		public static async Task<DtoObjectDescriptorWithMetadata?> GetObjectAsync(HttpClient client, int id, ILogger? logger = null)
			=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, Routes.Objects + $"/{id}", ReadJsonContentAsync<DtoObjectDescriptorWithMetadata?>, logger);

		//public static async Task<DtoObjectDescriptorWithMetadata?> GetDatFileAsync(HttpClient client, string objectName, uint checksum, ILogger? logger = null)
		//	=> await SendRequestAsync<DtoObjectDescriptorWithMetadata?>(client, OldRoutes.GetDatFile + $"?{nameof(objectName)}={objectName}&{nameof(checksum)}={checksum}", logger);

		public static async Task<byte[]?> GetObjectFileAsync(HttpClient client, int id, ILogger? logger = null)
			=> await SendRequestAsync<byte[]?>(client, Routes.Objects + $"/{id}/file", ReadBinaryContentAsync, logger);

		async static Task<T?> ReadJsonContentAsync<T>(HttpContent content)
			=> await content.ReadFromJsonAsync<T?>();

		async static Task<byte[]?> ReadBinaryContentAsync(HttpContent content)
		{
			await using (var stream = await content.ReadAsStreamAsync())
			await using (var memoryStream = new MemoryStream())
			{
				await stream.CopyToAsync(memoryStream);  // Efficiently copy to memory stream
				return memoryStream.ToArray(); // Get the byte array
			}
		}

		static async Task<T?> SendRequestAsync<T>(HttpClient client, string route, Func<HttpContent, Task<T?>> ContentReaderFunc, ILogger? logger = null)
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

				var data = await ContentReaderFunc(response.Content); // response.Content.ReadFromJsonAsync<T?>();
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
				var route = $"{client.BaseAddress?.OriginalString}{Routes.Objects}";
				logger.Debug($"Posting {filename} to {route}");
				var request = new DtoUploadDat(Convert.ToBase64String(datFileBytes), creationDate);
				var response = await client.PostAsJsonAsync(Routes.Objects, request);

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
