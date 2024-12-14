using Avalonia.Controls.Selection;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Types;
using OpenLoco.Gui.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Image = SixLabors.ImageSharp.Image;

namespace OpenLoco.Gui.ViewModels
{
	public class ImageTableViewModel : ReactiveObject, IExtraContentViewModel
	{
		readonly IHasG1Elements G1Provider;
		readonly IImageTableNameProvider NameProvider;
		readonly ILogger Logger;

		public ColourSwatches[] ColourSwatchesArr { get; } = (ColourSwatches[])Enum.GetValues(typeof(ColourSwatches));
		[Reactive] public ColourSwatches SelectedColourSwatch { get; set; } = ColourSwatches.PrimaryRemap;

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
			_ = this.WhenAnyValue(o => o.Zoom)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(Images)));
			_ = this.WhenAnyValue(o => o.SelectedImageIndex)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedG1Element)));
			_ = this.WhenAnyValue(o => o.SelectedBitmapPreview)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedBitmapPreviewBorder)));
			_ = this.WhenAnyValue(o => o.Images)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(Images)));
			_ = this.WhenAnyValue(o => o.AnimationSpeed)
				.Subscribe(_ => UpdateAnimationSpeed());

			ImportImagesCommand = ReactiveCommand.Create(ImportImages);
			ExportImagesCommand = ReactiveCommand.Create(ExportImages);

			SelectionModel = new SelectionModel<Bitmap>
			{
				SingleSelect = false
			};
			SelectionModel.SelectionChanged += SelectionChanged;

			// Set up the animation timer
			animationTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(25) // Adjust animation speed as needed
			};
			animationTimer.Tick += AnimationTimer_Tick;
			animationTimer.Start();
			Logger = logger;
		}

		readonly DispatcherTimer animationTimer;
		int currentFrameIndex;

		public IList<Bitmap> SelectedBitmaps { get; set; }

		[Reactive] public Bitmap SelectedBitmapPreview { get; set; }
		public Avalonia.Size SelectedBitmapPreviewBorder => SelectedBitmapPreview == null ? new Avalonia.Size() : new Avalonia.Size(SelectedBitmapPreview.Size.Width + 2, SelectedBitmapPreview.Size.Height + 2);

		[Reactive] public int AnimationWindowHeight { get; set; }

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
			AnimationWindowHeight = (int)SelectedBitmaps.Max(x => x.Size.Height) * 2;
		}

		[Reactive]
		public int AnimationSpeed { get; set; } = 40;

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
			if (SelectedBitmaps == null || SelectedBitmaps.Count == 0 || SelectionModel.SelectedIndexes.Count == 0)
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

		[Reactive]
		public PaletteMap PaletteMap { get; set; }

		[Reactive]
		public ICommand ImportImagesCommand { get; set; }

		[Reactive]
		public ICommand ExportImagesCommand { get; set; }

		[Reactive]
		public int Zoom { get; set; } = 1;

		// where the actual image data is stored
		[Reactive]
		public IList<Image<Rgba32>> Images { get; set; }

		// what is displaying on the ui
		public IList<Bitmap?> Bitmaps => G1ImageConversion.CreateAvaloniaImages(Images).ToList();

		[Reactive]
		public int SelectedImageIndex { get; set; } = -1;

		[Reactive]
		public SelectionModel<Bitmap> SelectionModel { get; set; }

		public UIG1Element32? SelectedG1Element
			=> SelectedImageIndex == -1 || G1Provider.G1Elements.Count == 0
			? null
			: new UIG1Element32(SelectedImageIndex, GetImageName(NameProvider, SelectedImageIndex), G1Provider.G1Elements[SelectedImageIndex]);

		//todo: second half should be model
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
					var offsetsFile = Path.Combine(dirPath, "sprites.json");
					if (File.Exists(offsetsFile))
					{
						// found blender folder
						var offsets = JsonSerializer.Deserialize<ICollection<SpriteOffset>>(File.ReadAllText(offsetsFile)); // sprites.json is an unnamed array so we need ICollection here, not IEnumerable
						ArgumentNullException.ThrowIfNull(offsets);
						Logger.Debug("Found sprites.json file, using that");

						if (offsets.Count != G1Provider.G1Elements.Count)
						{
							Logger.Warning($"Expected {G1Provider.G1Elements.Count} offsets, got {offsets.Count} offsets. Continue at your peril.");
						}

						foreach (var offset in offsets)
						{
							var filename = Path.Combine(dirPath, offset.Path);
							LoadSprite(filename, offset);
						}
					}
					else
					{
						Logger.Debug("No sprites.json file found");
						var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

						if (files.Length != G1Provider.G1Elements.Count)
						{
							Logger.Warning($"Expected {G1Provider.G1Elements.Count} images, got {files.Length} images. Continue at your peril.");
						}

						foreach (var filename in files)
						{
							LoadSprite(filename);
						}
					}

					Logger.Debug("Import successful");
					this.RaisePropertyChanged(nameof(Bitmaps));
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			}

			animationTimer.Start();
		}

		void LoadSprite(string filename, SpriteOffset? offset = null)
		{
			if (!Path.Exists(filename))
			{
				Logger.Error($"File doesn't exist: \"{filename}\"");
				return;
			}

			var match = Regex.Match(Path.GetFileNameWithoutExtension(filename), @".*?(\d+).*?");
			if (!match.Success)
			{
				Logger.Error($"Couldn't parse sprite index from filename: \"{filename}\"");
				return;
			}

			var index = int.Parse(match.Groups[1].Value);
			var img = Image.Load<Rgba32>(filename);

			if (index >= G1Provider.G1Elements.Count)
			{
				var newElement = new G1Element32(0, (int16_t)img.Width, (int16_t)img.Height, 0, 0, G1ElementFlags.None, 0)
				{
					ImageData = PaletteMap.ConvertRgba32ImageToG1Data(img, G1ElementFlags.None)
				};
				G1Provider.G1Elements.Insert(index, newElement);
				Images.Insert(index, img); // update the UI
			}
			else
			{
				var currG1 = G1Provider.G1Elements[index];
				currG1 = currG1 with
				{
					Width = (int16_t)img.Width,
					Height = (int16_t)img.Height,
					Flags = currG1.Flags & ~G1ElementFlags.IsRLECompressed, // SawyerStreamWriter::SaveImageTable does this anyways
					ImageData = PaletteMap.ConvertRgba32ImageToG1Data(img, currG1.Flags),
					XOffset = offset?.X ?? currG1.XOffset,
					YOffset = offset?.Y ?? currG1.YOffset
				};
				G1Provider.G1Elements[index] = currG1;
				Images[index] = img; // update the UI
			}
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
			foreach (var image in Images)
			{
				var imageName = counter++.ToString(); // todo: maybe use image name provider below (but number must still exist)
				var path = Path.Combine(dir.Path.LocalPath, $"{imageName}.png");
				await image.SaveAsPngAsync(path);
			}
		}

		public static string GetImageName(IImageTableNameProvider nameProvider, int counter)
			=> nameProvider.TryGetImageName(counter, out var value) && !string.IsNullOrEmpty(value)
				? value
				: counter.ToString();
	}
}
