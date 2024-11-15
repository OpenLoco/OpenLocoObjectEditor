using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using OpenLoco.Common;
using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Data;
using OpenLoco.Gui.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

#if !DEBUG
using OpenLoco.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
#endif

namespace OpenLoco.Gui.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public ObjectEditorModel Model { get; }

		public FolderTreeViewModel FolderTreeViewModel { get; }

		[Reactive]
		public ILocoFileViewModel? CurrentEditorModel { get; set; }

		public ObservableCollection<MenuItemViewModel> ObjDataItems { get; }

		public ObservableCollection<LogLine> Logs => Model.LoggerObservableLogs;

		public ReactiveCommand<Unit, Unit> OpenDownloadFolder { get; }
		public ReactiveCommand<Unit, Unit> OpenSettingsFolder { get; }
		public ReactiveCommand<Unit, Task> OpenSingleObject { get; }
		public ReactiveCommand<Unit, Task> OpenG1 { get; }
		public ReactiveCommand<Unit, Task> OpenSCV5 { get; }
		public ReactiveCommand<Unit, Task> OpenMusic { get; }
		public ReactiveCommand<Unit, Task> OpenSoundEffect { get; }
		public ReactiveCommand<Unit, Task> OpenTutorial { get; }
		public ReactiveCommand<Unit, Task> OpenScores { get; }
		public ReactiveCommand<Unit, Task> OpenLanguage { get; }
		public ReactiveCommand<Unit, Task> UseDefaultPalette { get; }
		public ReactiveCommand<Unit, Task> UseCustomPalette { get; }
		public ReactiveCommand<Unit, Unit> EditSettingsCommand { get; }
		public ReactiveCommand<Unit, Process?> OpenDownloadLink { get; }

		public const string GithubApplicationName = "ObjectEditor";
		public const string GithubIssuePage = "https://github.com/OpenLoco/ObjectEditor/issues";
		public const string GithubLatestReleaseDownloadPage = "https://github.com/OpenLoco/ObjectEditor/releases";
		public const string GithubLatestReleaseAPI = "https://api.github.com/repos/OpenLoco/ObjectEditor/releases/latest";

		public string WindowTitle => $"{ObjectEditorModel.ApplicationName} - {ApplicationVersion} ({LatestVersionText})";

		[Reactive]
		public Version ApplicationVersion { get; set; }

		[Reactive]
		public string LatestVersionText { get; set; } = "Development build";

		[Reactive]
		public bool IsUpdateAvailable { get; set; }

		const string DefaultPaletteImageString = "avares://ObjectEditor/Assets/palette.png";
		Image<Rgba32> DefaultPaletteImage { get; init; }

		public Interaction<EditorSettingsWindowViewModel, EditorSettingsWindowViewModel?> ShowDialog { get; }

		public MainWindowViewModel()
		{
			DefaultPaletteImage = Image.Load<Rgba32>(AssetLoader.Open(new Uri(DefaultPaletteImageString)));

			Model = new();
			Task.Run(LoadDefaultPalette);

			FolderTreeViewModel = new FolderTreeViewModel(Model);

			_ = FolderTreeViewModel.WhenAnyValue(o => o.CurrentlySelectedObject)
				.Subscribe((x) =>
				{
					if (x is FileSystemItemObject fsi)
					{
						SetObjectViewModel(fsi);
					}
				});

			ObjDataItems = new ObservableCollection<MenuItemViewModel>(Model.Settings.ObjDataDirectories
				.Select(x => new MenuItemViewModel(
					x,
					ReactiveCommand.Create(() => FolderTreeViewModel.CurrentLocalDirectory = x))));
			ObjDataItems.Insert(0, new MenuItemViewModel("Add new folder", ReactiveCommand.Create(SelectNewFolder)));
			ObjDataItems.Insert(1, new MenuItemViewModel("-", ReactiveCommand.Create(() => { })));

			OpenSingleObject = ReactiveCommand.Create(LoadSingleObject);
			OpenDownloadFolder = ReactiveCommand.Create(() => PlatformSpecific.FolderOpenInDesktop(Model.Settings.DownloadFolder));
			OpenSettingsFolder = ReactiveCommand.Create(() => PlatformSpecific.FolderOpenInDesktop(ObjectEditorModel.ProgramDataPath));
			OpenG1 = ReactiveCommand.Create(LoadG1);
			OpenSCV5 = ReactiveCommand.Create(LoadSCV5);
			OpenSoundEffect = ReactiveCommand.Create(LoadSoundEffects);
			OpenMusic = ReactiveCommand.Create(LoadMusic);

			UseDefaultPalette = ReactiveCommand.Create(LoadDefaultPalette);
			UseCustomPalette = ReactiveCommand.Create(LoadCustomPalette);

			ShowDialog = new();
			EditSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
			{
				var vm = new EditorSettingsWindowViewModel(Model.Settings);
				var result = await ShowDialog.Handle(vm);
				Model.Settings.Save(ObjectEditorModel.SettingsFile, Model.Logger);
			});

			OpenDownloadLink = ReactiveCommand.Create(() => Process.Start(new ProcessStartInfo(GithubLatestReleaseDownloadPage) { UseShellExecute = true }));

			#region Version

			_ = this.WhenAnyValue(o => o.ApplicationVersion)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));
			_ = this.WhenAnyValue(o => o.LatestVersionText)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));

			var assembly = Assembly.GetExecutingAssembly();

			ApplicationVersion = GetCurrentAppVersion(assembly);

#if !DEBUG

			try
			{
				var latestVersion = GetLatestAppVersion();
				if (latestVersion > ApplicationVersion)
				{
					LatestVersionText = $"newer version exists: {latestVersion}";
					IsUpdateAvailable = true;
				}
				else
				{
					LatestVersionText = "latest version";
					IsUpdateAvailable = false;
				}
			}
			catch (Exception ex)
			{
				Model.Logger.Error(ex);
			}
#endif
			#endregion
		}

		public async Task DownloadAndInstallAsync()
		{
			var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" :
				RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64" :
				RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux-x64" :
				throw new PlatformNotSupportedException();

			await DownloadAndExtractUpdateAsync(GithubLatestReleaseAPI, platform, "");

		}

		public async Task DownloadAndExtractUpdateAsync(string githubReleaseApi, string platformVersion, string extractPath)
		{
			try
			{
				// 1. Download API json
				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(GithubApplicationName, ApplicationVersion.ToString()));
					var response = client.GetAsync(GithubLatestReleaseAPI).Result;
					if (response.IsSuccessStatusCode)
					{
						var jsonResponse = response.Content.ReadAsStringAsync().Result;
						var githubReleaseApiResponse = JsonSerializer.Deserialize<GithubReleaseApi>(jsonResponse);

						ArgumentNullException.ThrowIfNull(githubReleaseApiResponse, nameof(githubReleaseApiResponse));

						var tagName = githubReleaseApiResponse.TagName;
						if (tagName != null)
						{
							var latestVersion = Version.Parse(tagName);
							if (latestVersion > ApplicationVersion)
							{
								LatestVersionText = $"newer version exists: {latestVersion}";
								IsUpdateAvailable = true;

								var downloadUrl = githubReleaseApiResponse.Assets
									.Where(x => x.BrowserDownloadURL.Contains(platformVersion))
									.Single().BrowserDownloadURL;

								using (var downloadMemoryStream = await DownloadUpdateAsync(downloadUrl))
								{
									// 2. Extract the update
									await Task.Run(() => ZipFile.ExtractToDirectory(downloadMemoryStream, Path.GetTempPath));

								}
							}
							else
							{
								LatestVersionText = "latest version";
								IsUpdateAvailable = false;
							}
						}
					}

					// 2. 

					// 1. Download the update


					// 2. Extract the update
					//await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractPath));

					// 3. (Optional) Delete the zip file
					//File.Delete(zipFilePath);
				}
			catch (Exception ex)
			{
				// Handle exceptions (e.g., network errors, invalid zip file)
				Console.WriteLine($"Error updating: {ex.Message}");
			}
		}

		async Task<MemoryStream> DownloadUpdateAsync(string updateUrl)
		{
			using (var client = new HttpClient())
			using (var response = await client.GetAsync(updateUrl, HttpCompletionOption.ResponseHeadersRead))
			{
				_ = response.EnsureSuccessStatusCode();

				await using (var stream = await response.Content.ReadAsStreamAsync())
				{
					var memoryStream = new MemoryStream();
					await stream.CopyToAsync(memoryStream);
					return memoryStream;
				}
			}
		}

		public static async Task<FileSystemItem?> GetFileSystemItemFromUser(IReadOnlyList<FilePickerFileType> filetypes)
		{
			var openFile = await PlatformSpecific.OpenFilePicker(filetypes);
			if (openFile == null)
			{
				return null;
			}

			var path = openFile.SingleOrDefault()?.Path.LocalPath;

			return path == null
				? null
				: new FileSystemItem(path, Path.GetFileName(path), FileLocation.Local);
		}

		async Task LoadDefaultPalette()
		{
			Model.PaletteMap = new PaletteMap(DefaultPaletteImage);
			if (CurrentEditorModel != null)
			{
				_ = await CurrentEditorModel.ReloadCommand.Execute();
			}
		}

		async Task LoadCustomPalette()
		{
			// file picker
			var openFile = await PlatformSpecific.OpenFilePicker(PlatformSpecific.PngFileTypes);
			if (openFile == null)
			{
				return;
			}

			var path = openFile.SingleOrDefault()?.Path.LocalPath;
			if (path == null)
			{
				return;
			}

			//
			Model.PaletteMap = new PaletteMap(path);
			// could use reactive here, but its simple for now so we won't. just reload the current model, which will in turn reload the images with the new palette
			if (CurrentEditorModel != null)
			{
				_ = await CurrentEditorModel.ReloadCommand.Execute();
			}
		}

		public async Task LoadSingleObject()
		{
			var fsi = await GetFileSystemItemFromUser(PlatformSpecific.DatFileTypes);
			if (fsi == null)
			{
				return;
			}

			if (Model.TryLoadObject(new FileSystemItem(fsi.Filename, Path.GetFileName(fsi.Filename), FileLocation.Local), out var uiLocoFile) && uiLocoFile != null)
			{
				Model.Logger.Warning($"Successfully loaded {fsi.Filename}");
				var source = OriginalObjectFiles.GetFileSource(uiLocoFile.DatFileInfo.S5Header.Name, uiLocoFile.DatFileInfo.S5Header.Checksum);
				var fsi2 = new FileSystemItemObject(fsi.Filename, uiLocoFile!.DatFileInfo.S5Header.Name, FileLocation.Local, source);
				SetObjectViewModel(fsi2);
			}
			else
			{
				Model.Logger.Warning($"Unable to load {fsi.Filename}");
			}
		}

		void SetObjectViewModel(FileSystemItemObject fsi)
			=> CurrentEditorModel = new DatObjectEditorViewModel(fsi, Model);

		public async Task LoadG1()
		{
			var fsi = await GetFileSystemItemFromUser(PlatformSpecific.DatFileTypes);
			if (fsi != null)
			{
				CurrentEditorModel = new G1ViewModel(fsi, Model);
			}
		}

		public async Task LoadSCV5()
		{
			var fsi = await GetFileSystemItemFromUser(PlatformSpecific.SCV5FileTypes);
			if (fsi != null)
			{
				CurrentEditorModel = new SCV5ViewModel(fsi, Model);
			}
		}

		async Task LoadMusic()
		{
			var fsi = await GetFileSystemItemFromUser(PlatformSpecific.DatFileTypes);
			if (fsi != null)
			{
				CurrentEditorModel = new MusicViewModel(fsi, Model);
			}
		}

		async Task LoadSoundEffects()
		{
			var fsi = await GetFileSystemItemFromUser(PlatformSpecific.DatFileTypes);
			if (fsi != null)
			{
				CurrentEditorModel = new SoundEffectsViewModel(fsi, Model);
			}
		}

		static Version GetCurrentAppVersion(Assembly assembly)
		{
			// grab current appl version from assembly
			const string versionFilename = "Gui.version.txt";
			using (var stream = assembly.GetManifestResourceStream(versionFilename))
			{
				var buf = new byte[5];
				var arr = stream!.Read(buf);
				return Version.Parse(Encoding.ASCII.GetString(buf));
			}
		}

#if !DEBUG
		// thanks for this one @IntelOrca, https://github.com/IntelOrca/PeggleEdit/blob/master/src/peggleedit/Forms/MainMDIForm.cs#L848-L861
		Version GetLatestAppVersion()
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(GithubApplicationName, ApplicationVersion.ToString()));
			var response = client.GetAsync(GithubLatestReleaseAPI).Result;
			if (response.IsSuccessStatusCode)
			{
				var jsonResponse = response.Content.ReadAsStringAsync().Result;
				var body = JsonSerializer.Deserialize<VersionCheckBody>(jsonResponse);
				var tagName = body?.TagName;
				if (tagName != null)
				{
					return Version.Parse(tagName);
				}
			}

#pragma warning disable CA2201 // Do not raise reserved exception types
			throw new Exception($"Unable to get latest version. Error={response.StatusCode}");
#pragma warning restore CA2201 // Do not raise reserved exception types
		}
#endif

		public async Task SelectNewFolder()
		{
			var folders = await PlatformSpecific.OpenFolderPicker();
			var dir = folders.FirstOrDefault();
			if (dir == null)
			{
				return;
			}

			var dirPath = dir.Path.LocalPath;
			if (Directory.Exists(dirPath) && !Model.Settings.ObjDataDirectories.Contains(dirPath))
			{
				FolderTreeViewModel.CurrentLocalDirectory = dirPath; // this will cause the reindexing
				var menuItem = new MenuItemViewModel(
					dirPath,
					ReactiveCommand.Create(() => FolderTreeViewModel.CurrentLocalDirectory = dirPath)
					/*ReactiveCommand.Create(() => ObjDataItems.RemoveAt(ObjDataItems.Count))*/);

				ObjDataItems.Add(menuItem);
			}
		}

		public static bool IsDarkTheme
		{
			get => Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
			set => Application.Current.RequestedThemeVariant = Application.Current.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark
				? Avalonia.Styling.ThemeVariant.Light
				: Avalonia.Styling.ThemeVariant.Dark;
		}
	}
}
