using System.Text.Json.Serialization;

namespace OpenLoco.Gui.ViewModels
{
	public record SpriteOffset(
		[property: JsonPropertyName("path")] string Path,
		[property: JsonPropertyName("x")] int16_t X,
		[property: JsonPropertyName("y")] int16_t Y);
}
