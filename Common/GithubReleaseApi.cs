using System.Text.Json.Serialization;

namespace OpenLoco.Common
{
	public class GithubReleaseApi
	{
		[JsonPropertyName("tag_name")]
		public string TagName { get; set; }

		[JsonPropertyName("assets")]
		public List<GithubReleaseApiAsset> Assets { get; set; }
	}

	public class GithubReleaseApiAsset
	{
		[JsonPropertyName("browser_download_url")]
		public string BrowserDownloadURL { get; set; }
	}
}
