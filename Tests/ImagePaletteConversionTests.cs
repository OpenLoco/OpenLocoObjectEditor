using NUnit.Framework;
using OpenLoco.Dat.FileParsing;
using SixLabors.ImageSharp;

namespace OpenLoco.Dat.Tests
{
	[TestFixture]
	public class ImagePaletteConversionTests
	{
		const string BaseObjDataPath = "Q:\\Games\\Locomotion\\OriginalObjects\\GoG\\";
		const string BaseImagePath = "Q:\\Games\\Locomotion\\ExportedImagesFromObjectEditor\\";
		const string BasePalettePath = "Q:\\Games\\Locomotion\\Palettes\\";

		[TestCase("AIRPORT1.DAT")]
		[TestCase("BALDWIN1.DAT")]
		[TestCase("FACTORY.DAT")]
		//[TestCase("INTERDEF.DAT")] // these files use different palettes
		//[TestCase("WATER1.DAT")]   // these files use different palettes
		public void G1ElementToPNGAndBack(string objectSource)
		{
			var paletteFile = Path.Combine(BasePalettePath, "palette_32bpp.png");
			var paletteMap = new PaletteMap(paletteFile);

			Assert.That(paletteMap.Transparent.Color, Is.EqualTo(Color.Transparent));

			var obj = SawyerStreamReader.LoadFullObjectFromFile(Path.Combine(BaseObjDataPath, objectSource));

			// convert g1 data into an image, and then back

			Assert.Multiple(() =>
			{
				_ = Parallel.ForEach(obj.Value.LocoObject.G1Elements, (element, _, i) =>
				{
					var image0 = paletteMap.ConvertG1ToRgb32Bitmap(element);

					if (image0 != null)
					{
						var g1Bytes = paletteMap.ConvertRgb32ImageToG1Data(image0);
						Assert.That(g1Bytes, Is.EqualTo(element.ImageData), $"[{i}]");
					}
				});
			});
		}
	}
}
