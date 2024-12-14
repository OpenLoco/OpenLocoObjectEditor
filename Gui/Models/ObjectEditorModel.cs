using Avalonia.Threading;
using DynamicData;
using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Data;
using OpenLoco.Dat.FileParsing;
using OpenLoco.Dat.Types;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenLoco.Gui.Models
{
	public class ObjectEditorModel
	{
		public EditorSettings Settings { get; private set; }

		public ILogger Logger;

		public ObjectIndex ObjectIndex { get; private set; }

		public ObjectIndex? ObjectIndexOnline { get; set; }

		public Dictionary<int, DtoDatObjectWithMetadata> OnlineCache { get; } = [];

		public PaletteMap PaletteMap { get; set; }

		public G1Dat? G1 { get; set; }

		public Dictionary<string, byte[]> Music { get; } = [];

		public Dictionary<string, byte[]> MiscellaneousTracks { get; } = [];

		public Dictionary<string, byte[]> SoundEffects { get; } = [];

		public Dictionary<string, byte[]> Tutorials { get; } = [];

		public Collection<string> MiscFiles { get; } = [];

		public const string ApplicationName = "OpenLoco Object Editor";
		public const string LoggingFileName = "objectEditor.log";

		// stores settings.json, objectEditor.log, etc
		public static string ProgramDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationName);
		public static string SettingsFile => Path.Combine(ProgramDataPath, Environment.GetEnvironmentVariable("ENV_SETTINGS_FILE") ?? EditorSettings.DefaultFileName);
		public static string LoggingFile => Path.Combine(ProgramDataPath, LoggingFileName);

		public ObservableCollection<LogLine> LoggerObservableLogs = [];

		public HttpClient? WebClient { get; }

		public ObjectEditorModel()
		{
			Logger = new Logger();
			LoggerObservableLogs = [];
			Logger.LogAdded += (sender, laea) => Dispatcher.UIThread.Post(() => LoggerObservableLogs.Insert(0, laea.Log));

			LoadSettings();
			InitialiseDownloadDirectory();

			var serverAddress = Settings!.UseHttps ? Settings.ServerAddressHttps : Settings.ServerAddressHttp;

			if (Uri.TryCreate(serverAddress, new(), out var serverUri))
			{
				WebClient = new HttpClient() { BaseAddress = serverUri, };
				Logger.Info($"Successfully registered object service with address \"{serverUri}\"");
			}
			else
			{
				Logger.Error($"Unable to parse object service address \"{serverAddress}\". Online functionality will work until the address is corrected and the editor is restarted.");
			}
		}

		void LoadSettings()
		{
			Settings = EditorSettings.Load(SettingsFile, Logger);

			if (Settings.Validate(Logger))
			{
				Logger.Info("Settings loaded and validated successfully.");
			}
			else
			{
				Logger.Error("Unable to validate settings file - please delete it and it will be recreated on next editor start-up.");
			}
		}

		void InitialiseDownloadDirectory()
		{
			if (string.IsNullOrEmpty(Settings.DownloadFolder))
			{
				Settings.DownloadFolder = Path.Combine(ProgramDataPath, "downloads");
			}

			if (!Directory.Exists(Settings.DownloadFolder))
			{
				Logger.Info($"Download folder doesn't exist; creating now at \"{Settings.DownloadFolder}\"");
				_ = Directory.CreateDirectory(Settings.DownloadFolder);
			}
		}

		public bool TryLoadObject(FileSystemItem filesystemItem, out UiDatLocoFile? uiLocoFile)
		{
			if (string.IsNullOrEmpty(filesystemItem.Filename))
			{
				uiLocoFile = null;
				return false;
			}

			DatFileInfo? fileInfo = null;
			ILocoObject? locoObject = null;
			MetadataModel? metadata = null;
			uiLocoFile = null;
			List<Image<Rgba32>> images = [];

			try
			{
				if (filesystemItem.FileLocation == FileLocation.Online)
				{
					var uniqueObjectId = int.Parse(filesystemItem.Filename);

					if (!OnlineCache.TryGetValue(uniqueObjectId, out var locoObj))
					{
						if (WebClient == null)
						{
							Logger.Error("Web client is null");
							return false;
						}

						Logger.Debug($"Didn't find object {filesystemItem.DisplayName} with unique id {uniqueObjectId} in cache - downloading it from {WebClient.BaseAddress}");
						locoObj = Task.Run(async () => await Client.GetObjectAsync(WebClient, uniqueObjectId, true)).Result;

						if (locoObj == null)
						{
							Logger.Error($"Unable to download object {filesystemItem.DisplayName} with unique id {uniqueObjectId} from online - received no data");
							return false;
						}
						else if (string.IsNullOrEmpty(locoObj.DatBytes))
						{
							Logger.Warning($"Unable to download object {filesystemItem.DisplayName} with unique id {uniqueObjectId} from online - received no DAT object data. Will still show metadata");
						}
						else if (locoObj.ObjectSource is ObjectSource.LocomotionSteam or ObjectSource.LocomotionGoG)
						{
							Logger.Warning($"Unable to download object {filesystemItem.DisplayName} with unique id {uniqueObjectId} from online - requested object is a vanilla object and it is illegal to distribute copyright material. Will still show metadata");
						}

						Logger.Info($"Downloaded object {filesystemItem.DisplayName} with unique id {uniqueObjectId} and added it to the local cache");
						Logger.Debug($"{filesystemItem.DisplayName} has authors=[{string.Join(", ", locoObj?.Authors?.Select(x => x.Name) ?? [])}], tags=[{string.Join(", ", locoObj?.Tags?.Select(x => x.Name) ?? [])}], objectpacks=[{string.Join(", ", locoObj?.ObjectPacks?.Select(x => x.Name) ?? [])}], licence={locoObj?.Licence}");
						OnlineCache.Add(uniqueObjectId, locoObj!);

						if (!string.IsNullOrEmpty(locoObj!.DatBytes))
						{
							var filename = Path.Combine(Settings.DownloadFolder, $"{locoObj.UniqueName}.dat");
							if (!File.Exists(filename))
							{
								File.WriteAllBytes(filename, Convert.FromBase64String(locoObj.DatBytes));
								Logger.Info($"Saved the downloaded object {filesystemItem.DisplayName} with unique id {uniqueObjectId} as {filename}");
							}
						}
					}
					else
					{
						Logger.Debug($"Found object {filesystemItem.DisplayName} with unique id {uniqueObjectId} in cache - reusing it");
					}

					if (locoObj != null)
					{
						if (locoObj.DatBytes?.Length > 0)
						{
							var obj = SawyerStreamReader.LoadFullObjectFromStream(Convert.FromBase64String(locoObj.DatBytes), Logger, $"{filesystemItem.Filename}-{filesystemItem.DisplayName}", true);
							fileInfo = obj.DatFileInfo;
							locoObject = obj.LocoObject;
							if (obj.LocoObject == null)
							{
								Logger.Warning($"Unable to load {filesystemItem.DisplayName} from the received DAT object data");
							}
						}

						metadata = new MetadataModel(locoObj.UniqueName, locoObj.DatName, locoObj.DatChecksum)
						{
							Description = locoObj.Description,
							Authors = locoObj.Authors,
							CreationDate = locoObj.CreationDate,
							LastEditDate = locoObj.LastEditDate,
							UploadDate = locoObj.UploadDate,
							Tags = locoObj.Tags,
							ObjectPacks = locoObj.ObjectPacks,
							Availability = locoObj.Availability,
							Licence = locoObj.Licence,
						};

						if (locoObject != null)
						{
							foreach (var i in locoObject.G1Elements)
							{
								if (PaletteMap.TryConvertG1ToRgba32Bitmap(i, out var image))
								{
									images.Add(image!);
								}
							}
						}
					}
				}
				else
				{
					var filename = Path.Combine(Settings.ObjDataDirectory, filesystemItem.Filename);
					var obj = SawyerStreamReader.LoadFullObjectFromFile(filename, logger: Logger);
					if (obj != null)
					{
						fileInfo = obj.Value.DatFileInfo;
						locoObject = obj.Value.LocoObject;
						metadata = null; // todo: look this up from internet anyways

						if (locoObject != null)
						{
							foreach (var i in locoObject.G1Elements)
							{
								if (PaletteMap.TryConvertG1ToRgba32Bitmap(i, out var image))
								{
									images.Add(image!);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Unable to load {filesystemItem.Filename}", ex);
				uiLocoFile = null;
				return false;
			}

			if (fileInfo == null)
			{
				Logger.Error($"Unable to load {filesystemItem.Filename}");
				uiLocoFile = null;
				return false;
			}

			uiLocoFile = new UiDatLocoFile() { DatFileInfo = fileInfo, LocoObject = locoObject, Metadata = metadata, Images = images };
			return true;
		}

		static Task? _indexerTask;
		static readonly SemaphoreSlim _taskLock = new(1, 1);

		public async Task LoadObjDirectoryAsync(string directory, IProgress<float> progress, bool useExistingIndex)
		{
			// Check if a task is already running WITHOUT waiting on the semaphore
			if (indexerTask?.IsCompleted == false)
			{
				// A task is already running, so just return the existing task
				return; // Or return _indexerTask if you need to await it elsewhere
			}

			// Only acquire the semaphore if no task is running
			await taskLock.WaitAsync();
			try
			{
				//Double check inside the lock
				if (indexerTask?.IsCompleted == false)
				{
					return;
				}

				indexerTask = Task.Run(async () => await LoadObjDirectoryAsyncCore(directory, progress, useExistingIndex));
			}
			finally
			{
				_ = _taskLock.Release();
			}

			await _indexerTask;
		}

		async Task LoadObjDirectoryAsyncCore(string directory, IProgress<float> progress, bool useExistingIndex)
		{
			if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory) || progress == null)
			{
				Logger.Error($"Couldn't start loading obj dir: {directory}");
				return;
			}

			Settings.ObjDataDirectory = directory;
			Settings.Save(SettingsFile, Logger);

			if (useExistingIndex && File.Exists(Settings.IndexFileName))
			{
				var exception = false;

				try
				{
					var index = await ObjectIndex.LoadIndexAsync(Settings.IndexFileName).ConfigureAwait(false);
					ArgumentNullException.ThrowIfNull(index, nameof(index));
					ObjectIndex = index;
					Logger.Info($"Loaded index for {directory} with {ObjectIndex.Objects.Count} objects.");
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
					exception = true;
				}

				if (exception || ObjectIndex?.Objects == null || ObjectIndex.Objects.Any(x => string.IsNullOrEmpty(x.Filename) || (x is ObjectIndexEntry xx && string.IsNullOrEmpty(xx.DatName))))
				{
					Logger.Warning("Index file format has changed or otherwise appears to be malformed - recreating now.");
					await RecreateIndex(directory, progress).ConfigureAwait(false);
					return;
				}

				var objectIndexFilenames = ObjectIndex.Objects.Select(x => x.Filename);
				var allFiles = SawyerStreamUtils.GetDatFilesInDirectory(directory).ToArray();
				if (objectIndexFilenames.Except(allFiles).Any() || allFiles.Except(objectIndexFilenames).Any())
				{
					Logger.Warning("Index file appears to be outdated - recreating now.");
					await RecreateIndex(directory, progress).ConfigureAwait(false);
				}
			}
			else
			{
				await RecreateIndex(directory, progress).ConfigureAwait(false);
			}

			async Task RecreateIndex(string directory, IProgress<float> progress)
			{
				Logger.Info($"Recreating index file for {directory}");
				ObjectIndex = await ObjectIndex.CreateIndexAsync(directory, Logger, progress).ConfigureAwait(false);

				if (ObjectIndex == null)
				{
					Logger.Error("Object Index was unable to be created.");
					return;
				}

				if (string.IsNullOrEmpty(Settings.IndexFileName))
				{
					Logger.Error("Index filename was null or empty.");
					return;
				}

				await ObjectIndex.SaveIndexAsync(Settings.IndexFileName).ConfigureAwait(false);
				Logger.Info($"New index was saved to {Settings.IndexFileName}");
			}
		}

		public async Task CheckForDatFilesNotOnServer()
		{
			if (ObjectIndex == null || ObjectIndexOnline == null || ObjectIndexOnline.Objects.Count == 0)
			{
				return;
			}

			Logger.Debug("Comparing local objects to object repository");

			var localButNotOnline = ObjectIndex.Objects.ExceptBy(ObjectIndexOnline.Objects.Select(
				x => (x.DatName, x.DatChecksum)),
				x => (x.DatName, x.DatChecksum)).ToList();

			if (localButNotOnline.Count != 0)
			{
				Logger.Info($"Found {localButNotOnline.Count} objects that aren't known to the object repository!");

				// would you like to upload?
				var isEnabledString = Settings.AutoObjectDiscoveryAndUpload ? "enabled" : "disabled";
				Logger.Info($"Automatic object discovery and upload to master service is {isEnabledString}");
				if (Settings.AutoObjectDiscoveryAndUpload)
				{
					foreach (var dat in localButNotOnline)
					{
						await UploadDatToServer(dat);
					}
				}
			}
			else
			{
				Logger.Debug("Found no new objects locally compared to the object repository.");
			}
		}

		public async Task UploadDatToServer(ObjectIndexEntry dat)
		{
			Logger.Info($"Uploading {dat.Filename} to object repository");
			var filename = Path.Combine(Settings.ObjDataDirectory, dat.Filename);
			var lastModifiedTime = File.GetLastWriteTimeUtc(filename); // this is the "Modified" time as shown in Windows
			if (WebClient == null)
			{
				Logger.Error("Web client is null");
				return;
			}

			await Client.UploadDatFileAsync(WebClient, dat.Filename, await File.ReadAllBytesAsync(filename), lastModifiedTime, Logger);
			await Task.Delay(100); // wait 100ms, ie don't DoS the server
		}
	}
}
