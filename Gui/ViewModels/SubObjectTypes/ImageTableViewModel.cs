using Avalonia.Controls.Selection;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Common.Json;
using DynamicData;
using OpenLoco.Common;
using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Types;
using OpenLoco.Gui.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Image = SixLabors.ImageSharp.Image;

namespace OpenLoco.Gui.ViewModels
{
	public record G1Element32Json(
		[property: JsonPropertyName("path")] string Path,
		[property: JsonPropertyName("x")] int16_t XOffset,
		[property: JsonPropertyName("y")] int16_t YOffset,
		[property: JsonPropertyName("zoomOffset")] int16_t? ZoomOffset,
		[property: JsonPropertyName("flags")] G1ElementFlags? Flags
		)
	{
		public G1Element32Json()
			: this("", 0, 0, null, null)
		{ }

		public G1Element32Json(string path, int16_t xOffset, int16_t yOffset)
			: this(path, xOffset, yOffset, null, null)
		{ }

		public G1Element32Json(string path, G1Element32 g1Element)
			: this(path, g1Element.XOffset, g1Element.YOffset, g1Element.ZoomOffset, g1Element.Flags)
		{ }

		public static G1Element32Json Zero
			=> new G1Element32Json(string.Empty, 0, 0, 0, G1ElementFlags.None);
	}

	public class ImageTableViewModel : ReactiveObject, IExtraContentViewModel
	{
		readonly IHasG1Elements G1Provider;
		readonly IImageTableNameProvider NameProvider;
		readonly ILogger Logger;

		public ColourRemapSwatch[] ColourSwatchesArr { get; } = Enum.GetValues<ColourRemapSwatch>();

		[Reactive]
		public ColourRemapSwatch SelectedPrimarySwatch { get; set; } = ColourRemapSwatch.PrimaryRemap;

		[Reactive]
		public ColourRemapSwatch SelectedSecondarySwatch { get; set; } = ColourRemapSwatch.SecondaryRemap;

		readonly DispatcherTimer animationTimer;
		int currentFrameIndex;

		public IList<Bitmap> SelectedBitmaps { get; set; }

		[Reactive] public Bitmap SelectedBitmapPreview { get; set; }
		public Avalonia.Size SelectedBitmapPreviewBorder
			=> SelectedBitmapPreview == null
				? new Avalonia.Size()
				: new Avalonia.Size(SelectedBitmapPreview.Size.Width + 2, SelectedBitmapPreview.Size.Height + 2);

		[Reactive]
		public int AnimationSpeed { get; set; } = 40;

		[Reactive]
		public PaletteMap PaletteMap { get; set; }

		[Reactive]
		public ICommand ReplaceImageCommand { get; set; }

		[Reactive]
		public ICommand ImportImagesCommand { get; set; }

		[Reactive]
		public ICommand ExportImagesCommand { get; set; }

		[Reactive]
		public ICommand CropAllImagesCommand { get; set; }

		// where the actual image data is stored
		[Reactive]
		public IList<Image<Rgba32>> Images { get; set; }

		// what is displaying on the ui
		[Reactive]
		public ObservableCollection<Bitmap?> Bitmaps { get; set; }

		[Reactive]
		public int SelectedImageIndex { get; set; } = -1;

		[Reactive]
		public SelectionModel<Bitmap> SelectionModel { get; set; }

		public UIG1Element32? SelectedG1Element
			=> SelectedImageIndex == -1 || G1Provider.G1Elements.Count == 0 || SelectedImageIndex >= G1Provider.G1Elements.Count
			? null
			: new UIG1Element32(SelectedImageIndex, GetImageName(NameProvider, SelectedImageIndex), G1Provider.G1Elements[SelectedImageIndex]);

		public Avalonia.Point SelectedG1ElementOffset
			=> SelectedG1Element == null
				? new Avalonia.Point()
				: new Avalonia.Point(-SelectedG1Element?.G1Element.XOffset ?? 0, -SelectedG1Element?.G1Element.YOffset ?? 0);
		public Avalonia.Size SelectedG1ElementSize
			=> SelectedG1Element == null
				? new Avalonia.Size()
				: new Avalonia.Size(SelectedG1Element?.G1Element.Width ?? 0, SelectedG1Element?.G1Element.Height ?? 0);

		public ImageTableViewModel(IHasG1Elements g1ElementProvider, IImageTableNameProvider imageNameProvider, PaletteMap paletteMap, IList<Image<Rgba32>> images, ILogger logger)
		{
			G1Provider = g1ElementProvider;
			NameProvider = imageNameProvider;
			PaletteMap = paletteMap;
			Images = images;
			Logger = logger;

			_ = this.WhenAnyValue(o => o.G1Provider)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(Images)));
			_ = this.WhenAnyValue(o => o.PaletteMap)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(Images)));
			_ = this.WhenAnyValue(o => o.SelectedPrimarySwatch).Skip(1)
				.Subscribe(_ => RecalcImages());
			_ = this.WhenAnyValue(o => o.SelectedSecondarySwatch).Skip(1)
				.Subscribe(_ => RecalcImages());
			_ = this.WhenAnyValue(o => o.Images)
				.Subscribe(_ => Bitmaps = [.. G1ImageConversion.CreateAvaloniaImages(Images)]);
			_ = this.WhenAnyValue(o => o.SelectedImageIndex)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedG1Element)));
			_ = this.WhenAnyValue(o => o.SelectedG1Element)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedG1ElementOffset)));
			_ = this.WhenAnyValue(o => o.SelectedG1Element)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedG1ElementSize)));
			_ = this.WhenAnyValue(o => o.SelectedBitmapPreview)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedBitmapPreviewBorder)));
			_ = this.WhenAnyValue(o => o.AnimationSpeed)
				.Subscribe(_ => UpdateAnimationSpeed());

			ImportImagesCommand = ReactiveCommand.CreateFromTask(ImportImages);
			ExportImagesCommand = ReactiveCommand.CreateFromTask(ExportImages);
			ReplaceImageCommand = ReactiveCommand.CreateFromTask(ReplaceImage);
			CropAllImagesCommand = ReactiveCommand.Create(CropAllImages);

			CreateSelectionModel();

			// Set up the animation timer
			animationTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(25) // Adjust animation speed as needed
			};
			animationTimer.Tick += AnimationTimer_Tick;
			animationTimer.Start();
			Logger = logger;
		}

		void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
		{
			var sm = (SelectionModel<Bitmap>)sender;

			if (sm.SelectedIndexes.Count > 0)
			{
				SelectedImageIndex = sm.SelectedIndex;
			}

			if (sm.SelectedItems.Count == 0)
			{
				return;
			}

			// ... handle selection changed
			SelectedBitmaps = sm.SelectedItems.Cast<Bitmap>().ToList();
		}

		void UpdateAnimationSpeed()
		{
			if (animationTimer == null)
			{
				return;
			}

			animationTimer.Interval = TimeSpan.FromMilliseconds(1000 / AnimationSpeed);
		}

		void AnimationTimer_Tick(object? sender, EventArgs e)
		{
			if (SelectionModel == null || SelectedBitmaps == null || SelectedBitmaps.Count == 0 || SelectionModel.SelectedIndexes.Count == 0)
			{
				return;
			}

			if (currentFrameIndex >= SelectedBitmaps.Count)
			{
				currentFrameIndex = 0;
			}

			// Update the displayed image
			SelectedBitmapPreview = SelectedBitmaps[currentFrameIndex];
			SelectedImageIndex = SelectionModel.SelectedIndexes[currentFrameIndex];

			// Move to the next frame, looping back to the beginning if necessary
			currentFrameIndex = (currentFrameIndex + 1) % SelectedBitmaps.Count;
		}

		void CreateSelectionModel()
		{
			SelectionModel = new SelectionModel<Bitmap>
			{
				SingleSelect = false
			};
			SelectionModel.SelectionChanged += SelectionChanged;
		}

		public async Task ImportImages()
		{
			animationTimer.Stop();

			var folders = await PlatformSpecific.OpenFolderPicker();
			using (var dir = folders.FirstOrDefault())
			{
				if (dir == null)
				{
					return;
				}

				var dirPath = dir.Path.LocalPath;
				if (!Directory.Exists(dirPath))
				{
					return;
				}

				try
				{
					Logger.Debug($"{G1Provider.G1Elements.Count} images in current object");

					// count files in dir and check naming
					var files = Directory.GetFiles(dirPath, "*.png", SearchOption.AllDirectories);

					Logger.Debug($"{files.Length} files in current directory");

					IEnumerable<G1Element32Json> offsets;

					// check for offsets file
					var offsetsFile = Path.Combine(dirPath, "sprites.json");
					if (File.Exists(offsetsFile))
					{
						offsets = JsonSerializer.Deserialize<ICollection<G1Element32Json>>(File.ReadAllText(offsetsFile)); // sprites.json is an unnamed array so we need ICollection here, not IEnumerable
						ArgumentNullException.ThrowIfNull(offsets);
						Logger.Debug("Found sprites.json file; using that");
					}
					else
					{
						offsets = G1Provider.G1Elements.Select((x, i) => new G1Element32Json($"{i}.png", x.XOffset, x.YOffset));
						Logger.Debug("Didn't find sprites.json; using existing G1Element32 offsets");
					}

					offsets = offsets.Fill(files.Length, G1Element32Json.Zero);

					// clear existing images
					Logger.Info("Clearing current G1Element32s and existing object images");
					G1Provider.G1Elements.Clear();
					Images.Clear();
					Bitmaps.Clear();

					// load files
					var offsetList = offsets.ToList();
					for (var i = 0; i < files.Length; ++i)
					{
						var offsetPath = offsetList[i].Path;
						var validPath = string.IsNullOrEmpty(offsetPath) ? $"{i}.png" : offsetPath;
						var filename = Path.Combine(dirPath, validPath);
						LoadSprite(filename, offsetList[i]);
					}

					Logger.Debug($"Imported {G1Provider.G1Elements.Count} images successfully");
					this.RaisePropertyChanged(nameof(Bitmaps));
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			}

			animationTimer.Start();

			void LoadSprite(string filename, G1Element32Json ele)
			{
				if (!File.Exists(filename))
				{
					Logger.Error($"File doesn't exist: \"{filename}\"");
					return;
				}

				var img = Image.Load<Rgba32>(filename);

				var flags = ele.Flags ?? G1ElementFlags.None;
				var newElement = new G1Element32(0, (int16_t)img.Width, (int16_t)img.Height, ele.XOffset, ele.YOffset, flags, ele.ZoomOffset ?? 0)
				{
					ImageData = PaletteMap.ConvertRgba32ImageToG1Data(img, flags, SelectedPrimarySwatch, SelectedSecondarySwatch)
				};

				G1Provider.G1Elements.Add(newElement);
				Images.Add(img);
				Bitmaps.Add(G1ImageConversion.CreateAvaloniaImage(img));
			}
		}

		public void RecalcImages()
		{
			// unfortunately no way to "copy" the selection from old to new
			SelectionModel.Clear();

			// clear existing images
			Logger.Info("Clearing current G1Element32s and existing object images");
			Images.Clear();
			Bitmaps.Clear();

			foreach (var g1 in G1Provider.G1Elements)
			{
				if (PaletteMap.TryConvertG1ToRgba32Bitmap(g1, SelectedPrimarySwatch, SelectedSecondarySwatch, out var img) && img != null)
				{
					Images.Add(img);
					Bitmaps.Add(G1ImageConversion.CreateAvaloniaImage(img));
				}
				else
				{
					Logger.Error("Unable to convert G1 to image");
				}
			}

			this.RaisePropertyChanged(nameof(Bitmaps));
			this.RaisePropertyChanged(nameof(Images));
			SelectedImageIndex = -1;
		}

		// todo: second half should be in model
		public async Task ExportImages()
		{
			var folders = await PlatformSpecific.OpenFolderPicker();
			var dir = folders.FirstOrDefault();
			if (dir == null)
			{
				return;
			}

			var dirPath = dir.Path.LocalPath;
			if (!Directory.Exists(dirPath))
			{
				return;
			}

			Logger.Info($"Saving images to {dirPath}");

			var counter = 0;
			var offsets = new List<G1Element32Json>();

			foreach (var image in Images)
			{
				var g1Element = G1Provider.G1Elements[counter];
				var imageName = counter.ToString(); // todo: maybe use image name provider below (but number must still exist)
				counter++;

				var fileName = $"{imageName}.png";
				var path = Path.Combine(dir.Path.LocalPath, fileName);
				await image.SaveAsPngAsync(path);

				offsets.Add(new G1Element32Json(fileName, g1Element));
			}

			var offsetsFile = Path.Combine(dir.Path.LocalPath, "sprites.json");
			Logger.Info($"Saving sprite offsets to {offsetsFile}");
			await JsonFile.SerializeToFileAsync(offsets, offsetsFile);
		}

		public void CropAllImages()
		{
			for (var i = 0; i < Images.Count; ++i)
			{
				var image = Images[i];

				var cropRegion = FindCropRegion(image);

				if (cropRegion.Width <= 0 || cropRegion.Height <= 0)
				{
					image.Mutate(i => i.Crop(new Rectangle(0, 0, 1, 1)));
					UpdateImage(image, i, 0, 0);
				}
				else
				{
					image.Mutate(i => i.Crop(cropRegion));
					var currG1 = G1Provider.G1Elements[i];

					// set to bitmaps
					UpdateImage(image, i, xOffset: (short)(currG1.XOffset + cropRegion.Left), yOffset: (short)(currG1.YOffset + cropRegion.Top));
				}
			}

			this.RaisePropertyChanged(nameof(Bitmaps));
			this.RaisePropertyChanged(nameof(SelectedG1Element));
		}

		static Rectangle FindCropRegion(Image<Rgba32> image)
		{
			var minX = image.Width;
			var maxX = 0;
			var minY = image.Height;
			var maxY = 0;

			for (var y = 0; y < image.Height; y++)
			{
				for (var x = 0; x < image.Width; x++)
				{
					var pixel = image[x, y];

					if (pixel.A > 0)
					{
						minX = Math.Min(minX, x);
						maxX = Math.Max(maxX, x);
						minY = Math.Min(minY, y);
						maxY = Math.Max(maxY, y);
					}
				}
			}

			// Calculate the crop area. Ensure it is within image bounds.
			var width = Math.Max(0, Math.Min(maxX - minX + 1, image.Width - minX));
			var height = Math.Max(0, Math.Min(maxY - minY + 1, image.Height - minY));
			return new Rectangle(minX, minY, width, height);
		}

		public async Task ReplaceImage()
		{
			if (SelectedImageIndex == -1)
			{
				return;
			}

			// file picker
			var openFile = await PlatformSpecific.OpenFilePicker(PlatformSpecific.PngFileTypes);
			if (openFile == null)
			{
				return;
			}

			var filename = openFile.SingleOrDefault()?.Path.LocalPath;
			if (filename == null)
			{
				return;
			}

			// load image
			UpdateImage(Image.Load<Rgba32>(filename), SelectedImageIndex);

			this.RaisePropertyChanged(nameof(Bitmaps));
			this.RaisePropertyChanged(nameof(SelectedG1Element));
		}

		void UpdateImage(Image<Rgba32> img, int index, G1Element32Json ele)
			=> UpdateImage(img, index, ele.Flags, ele.XOffset, ele.YOffset, ele.ZoomOffset);

		void UpdateImage(Image<Rgba32> img, int index, G1ElementFlags? flags = null, int16_t? xOffset = null, int16_t? yOffset = null, int16_t? zoomOffset = null)
		{
			if (index == -1)
			{
				return;
			}

			Images[index] = img;
			Bitmaps[index] = G1ImageConversion.CreateAvaloniaImage(img);

			var currG1 = G1Provider.G1Elements[index];
			G1Provider.G1Elements[index] = (currG1 with
			{
				ImageData = PaletteMap.ConvertRgba32ImageToG1Data(img, flags ?? currG1.Flags, SelectedPrimarySwatch, SelectedSecondarySwatch),
				Width = (int16_t)img.Width,
				Height = (int16_t)img.Height,
				Flags = flags ?? currG1.Flags,
				XOffset = xOffset ?? currG1.XOffset,
				YOffset = yOffset ?? currG1.YOffset,
				ZoomOffset = zoomOffset ?? currG1.ZoomOffset,
			});
		}

		public static string GetImageName(IImageTableNameProvider nameProvider, int counter)
			=> nameProvider.TryGetImageName(counter, out var value) && !string.IsNullOrEmpty(value)
				? value
				: counter.ToString();
	}
}
