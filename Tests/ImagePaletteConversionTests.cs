using NUnit.Framework;
using OpenLoco.Common.Logging;
using OpenLoco.Dat.FileParsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenLoco.Dat.Tests
{
	[TestFixture]
	public class ImagePaletteConversionTests
	{
		const string BaseObjDataPath = "Q:\\Games\\Locomotion\\OriginalObjects\\GoG\\";
		const string BaseImagePath = "Q:\\Games\\Locomotion\\ExportedImagesFromObjectEditor\\";
		const string BasePalettePath = "Q:\\Games\\Locomotion\\Palettes\\";
		readonly ILogger Logger = new Logger();

		[Test]
		public void TestWrite00000000ToIndex0()
		{
			var paletteFile = Path.Combine(BasePalettePath, "palette_32bpp3.png");
			var paletteMap = Image.Load<Rgba32>(paletteFile);
			paletteMap[0, 0] = Color.Transparent;
			paletteMap.SaveAsPng(paletteFile);
		}

		[Test]
		public void PaletteIndex0IsTransparent()
		{
			var paletteFile = Path.Combine(BasePalettePath, "palette_32bpp3.png");
			var paletteMap = new PaletteMap(paletteFile);

			Assert.That(paletteMap.Transparent.Color, Is.EqualTo(Color.Transparent));
		}

		[Test]
		public void PaletteHasUniqueColours()
		{
			var paletteFile = Path.Combine(BasePalettePath, "palette_32bpp3.png");
			var paletteMap = new PaletteMap(paletteFile);

			Assert.That(paletteMap.Transparent.Color, Is.EqualTo(Color.Transparent));
			Assert.That(paletteMap.PaletteColours.Length, Is.EqualTo(paletteMap.PaletteColours.ToHashSet().Count));
		}

		[TestCase("AIRPORT1.DAT")]
		[TestCase("BALDWIN1.DAT")]
		[TestCase("FACTORY.DAT")]
		//[TestCase("INTERDEF.DAT")] // these files use different palettes
		//[TestCase("WATER1.DAT")]   // these files use different palettes
		public void G1ElementToPNGAndBack(string objectSource)
		{
			var paletteFile = Path.Combine(BasePalettePath, "palette_32bpp3.png");
			var paletteMap = new PaletteMap(paletteFile);
			var obj = SawyerStreamReader.LoadFullObjectFromFile(Path.Combine(BaseObjDataPath, objectSource), Logger);

			// convert g1 data into an image, and then back

			Assert.Multiple(() =>
			{
				_ = Parallel.ForEach(obj.Value.LocoObject.G1Elements, (element, _, i) =>
				{
					var image0 = paletteMap.ConvertG1ToRgba32Bitmap(element);

					if (image0 != null)
					{
						var g1Bytes = paletteMap.ConvertRgba32ImageToG1Data(image0, element.Flags);
						Assert.That(g1Bytes, Is.EqualTo(element.ImageData), $"[{i}]");
					}
				});
			});
		}
	}
}
