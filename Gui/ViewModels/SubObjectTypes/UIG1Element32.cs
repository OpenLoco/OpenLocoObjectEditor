using OpenLoco.Dat.Types;
using System.ComponentModel;

namespace AvaGui.ViewModels
{
	public record UIG1Element32(
		[Category("Image")] int ImageIndex,
		[Category("Image")] string ImageName,
		G1Element32 g1Element
	);
}
