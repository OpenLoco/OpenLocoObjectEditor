using AvaGui.Models;
using Avalonia.Controls;
using Dat;
using OpenLoco.Dat.Data;
using OpenLoco.Definitions.DTO;
using OpenLoco.Definitions.Web;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AvaGui.ViewModels
{
	public class FolderTreeViewModel : ReactiveObject
	{
		ObjectEditorModel Model { get; init; }

		[Reactive]
		public string CurrentLocalDirectory { get; set; } = string.Empty;
		public string CurrentDirectory => SelectedTabIndex == 0 ? CurrentLocalDirectory : "<online>";

		[Reactive]
		public FileSystemItemBase? CurrentlySelectedObject { get; set; }

		[Reactive]
		public string FilenameFilter { get; set; } = string.Empty;

		[Reactive]
		public ObjectDisplayMode DisplayMode { get; set; } = ObjectDisplayMode.All;

		[Reactive]
		List<FileSystemItemBase> LocalDirectoryItems { get; set; } = [];

		[Reactive]
		List<FileSystemItemBase> OnlineDirectoryItems { get; set; } = [];

		[Reactive]
		public ObservableCollection<FileSystemItemBase> DirectoryItems { get; set; }

		[Reactive]
		public float IndexOrDownloadProgress { get; set; }

		Progress<float> Progress { get; } = new();

		public ReactiveCommand<Unit, Task> RefreshDirectoryItems { get; }

		[Reactive]
		public ObservableCollection<ObjectDisplayMode> DisplayModeItems { get; set; } = [.. Enum.GetValues<ObjectDisplayMode>()];

		[Reactive]
		public int SelectedTabIndex { get; set; }

		public string RecreateText => SelectedTabIndex == 0 ? "Recreate index" : "Download object list";

		public string DirectoryFileCount
			=> $"Objects: {DirectoryItems.Sum(CountNodes)}";

		public static int CountNodes(FileSystemItemBase fib)
		{
			if (fib.SubNodes == null || fib.SubNodes.Count == 0)
			{
				return 0;
			}

			var count = 0;

			foreach (var node in fib.SubNodes)
			{
				if (node is FileSystemItem)
				{
					count++;
				}
				else
				{
					count += CountNodes(node);
				}
			}

			return count;
		}

		public FolderTreeViewModel(ObjectEditorModel model)
		{
			Model = model;
			Progress.ProgressChanged += (_, progress) => IndexOrDownloadProgress = progress;

			RefreshDirectoryItems = ReactiveCommand.Create(() => ReloadDirectoryAsync(false));

			_ = this.WhenAnyValue(o => o.CurrentLocalDirectory)
				.Subscribe(async _ => await ReloadDirectoryAsync(true));
			_ = this.WhenAnyValue(o => o.CurrentLocalDirectory)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentDirectory)));
			_ = this.WhenAnyValue(o => o.DisplayMode)
				.Throttle(TimeSpan.FromMilliseconds(1000))
				.Subscribe(async _ => await ReloadDirectoryAsync(true));
			_ = this.WhenAnyValue(o => o.FilenameFilter)
				.Throttle(TimeSpan.FromMilliseconds(500))
				.Subscribe(async _ => await ReloadDirectoryAsync(true));

			_ = this.WhenAnyValue(o => o.DirectoryItems)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(DirectoryFileCount)));
			_ = this.WhenAnyValue(o => o.DirectoryItems)
				.Subscribe(_ => CurrentlySelectedObject = null);

			_ = this.WhenAnyValue(o => o.SelectedTabIndex)
				.Subscribe(_ => SwitchDirectoryItemsView());
			_ = this.WhenAnyValue(o => o.SelectedTabIndex)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(RecreateText)));
			_ = this.WhenAnyValue(o => o.SelectedTabIndex)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(CurrentDirectory)));
			_ = this.WhenAnyValue(o => o.LocalDirectoryItems)
				.Subscribe(_ => SwitchDirectoryItemsView());
			_ = this.WhenAnyValue(o => o.OnlineDirectoryItems)
				.Subscribe(_ => SwitchDirectoryItemsView());

			// loads the last-viewed folder
			CurrentLocalDirectory = Model.Settings.ObjDataDirectory;
		}

		void SwitchDirectoryItemsView()
			=> DirectoryItems = SelectedTabIndex == 0
				? new(LocalDirectoryItems)
				: new(OnlineDirectoryItems);

		async Task ReloadDirectoryAsync(bool useExistingIndex)
		{
			if (SelectedTabIndex == 0)
			{
				// local
				await LoadObjDirectoryAsync(CurrentLocalDirectory, useExistingIndex);
			}
			else // remote
			{
				await LoadOnlineDirectoryAsync(useExistingIndex);
			}

			await Model.CheckForDatFilesNotOnServer();

		}

		async Task LoadObjDirectoryAsync(string directory, bool useExistingIndex)
		{
			if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
			{
				//LocalDirectoryItems = [];
				return;
			}

			await Model.LoadObjDirectoryAsync(directory, Progress, useExistingIndex);
			LocalDirectoryItems = ConstructTreeView(
				Model.ObjectIndex.Objects.Where(x => (int)x.ObjectType < Limits.kMaxObjectTypes),
				FilenameFilter,
				DisplayMode,
				FileLocation.Local);
		}

		readonly IEnumerable<DtoObjectIndexEntry>? cachedIndexFromServer;

		async Task LoadOnlineDirectoryAsync(bool useExistingIndex)
		{
			if (Design.IsDesignMode)
			{
				// DO NOT WEB QUERY AT DESIGN TIME
				return;
			}

			if (!useExistingIndex || cachedIndexFromServer == null)
			{
				Model.ObjectIndexOnline = new ObjectIndex()
				{
					Objects = (await Client.GetObjectListAsync(Model.WebClient, Model.Logger))
					.Select(x => new ObjectIndexEntry(x.UniqueId.ToString(), x.ObjectName, x.ObjectType, x.IsVanilla, x.Checksum, x.VehicleType))
					.ToList(),
					ObjectsFailed = []
				};
			}

			OnlineDirectoryItems = ConstructTreeView(
				Model.ObjectIndexOnline.Objects.Where(x => (int)x.ObjectType < Limits.kMaxObjectTypes),
				FilenameFilter,
				DisplayMode,
				FileLocation.Online);
		}

		static List<FileSystemItemBase> ConstructTreeView(IEnumerable<ObjectIndexEntryBase> index, string filenameFilter, ObjectDisplayMode displayMode, FileLocation fileLocation)
		{
			var result = new List<FileSystemItemBase>();

			var groupedObjects = index
				.OfType<ObjectIndexEntry>() // this won't show errored files - should we??
				.Where(o => (string.IsNullOrEmpty(filenameFilter) || o.ObjectName.Contains(filenameFilter, StringComparison.CurrentCultureIgnoreCase)) && (displayMode == ObjectDisplayMode.All || (displayMode == ObjectDisplayMode.Vanilla == o.IsVanilla)))
				.GroupBy(o => o.ObjectType)
				.OrderBy(fsg => fsg.Key.ToString());

			foreach (var objGroup in groupedObjects)
			{
				ObservableCollection<FileSystemItemBase> subNodes;
				if (objGroup.Key == ObjectType.Vehicle)
				{
					subNodes = [];
					foreach (var vg in objGroup
						.GroupBy(o => o.VehicleType)
						.OrderBy(vg => vg.Key.ToString()))
					{
						var vehicleSubNodes = new ObservableCollection<FileSystemItemBase>(vg
							.Select(o => new FileSystemItem(o.Filename, o.ObjectName, o.IsVanilla, fileLocation))
							.OrderBy(o => o.Name));

						if (vg.Key == null)
						{
							// this should be impossible - object says its a vehicle but doesn't have a vehicle type
							// todo: move validation into the loading stage or cstr of IndexObjectHeader
							continue;
						}

						subNodes.Add(new FileSystemVehicleGroup(
							string.Empty,
							vg.Key.Value,
							vehicleSubNodes));
					}
				}
				else
				{
					subNodes = new ObservableCollection<FileSystemItemBase>(objGroup
						.Select(o => new FileSystemItem(o.Filename, o.ObjectName, o.IsVanilla, fileLocation))
						.OrderBy(o => o.Name));
				}

				result.Add(new FileSystemItemGroup(
						string.Empty,
						objGroup.Key,
						subNodes));
			}

			return result;
		}
	}
}
