using OpenLoco.Common.Logging;
using OpenLoco.Dat;
using OpenLoco.Dat.Data;
using OpenLoco.Dat.FileParsing;
using OpenLoco.Dat.Objects.Sound;
using OpenLoco.Dat.Types;
using OpenLoco.Gui.Models;
using PropertyModels.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OpenLoco.Gui.ViewModels
{
	public class DatObjectEditorViewModel : BaseLocoFileViewModel
	{
		[Reactive]
		public IObjectViewModel<ILocoStruct>? CurrentObjectViewModel { get; set; }

		[Reactive]
		public StringTableViewModel? StringTableViewModel { get; set; }

		[Reactive]
		public IExtraContentViewModel? ExtraContentViewModel { get; set; }

		[Reactive]
		public S5HeaderViewModel? S5HeaderViewModel { get; set; }

		[Reactive]
		public UiDatLocoFile? CurrentObject { get; private set; }

		[Reactive]
		public ObservableCollection<TreeNode> CurrentHexAnnotations { get; private set; }

		[Reactive]
		public TreeNode? CurrentlySelectedHexAnnotation { get; set; }

		[Reactive]
		public HexAnnotationLine[]? CurrentHexDumpLines { get; set; }

		byte[] currentByteList;

		Dictionary<string, (int Start, int End)> DATDumpAnnotationIdentifiers = [];
		const int bytesPerDumpLine = 32;
		const int addressStringSizeBytes = 8;
		//const int addressStringSizePrependBytes = addressStringSizeBytes + 2;
		//const int dumpWordSize = 4;

		public DatObjectEditorViewModel(FileSystemItemObject currentFile, ObjectEditorModel model)
			: base(currentFile, model)
		{
			_ = this.WhenAnyValue(o => o.CurrentlySelectedHexAnnotation)
				.Subscribe(_ => UpdateHexDumpView());

			Load();
		}

		public void UpdateHexDumpView()
			=> CurrentHexDumpLines = CurrentlySelectedHexAnnotation != null && DATDumpAnnotationIdentifiers.TryGetValue(CurrentlySelectedHexAnnotation.Title, out var positionValues)
				? GetDumpLines(currentByteList, positionValues.Start, positionValues.End).ToArray()
				: GetDumpLines(currentByteList, null, null).ToArray();

		public static IObjectViewModel<ILocoStruct> GetViewModelFromStruct(ILocoStruct locoStruct)
		{
			var asm = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.SingleOrDefault(type
					=> type.IsClass
					&& !type.IsAbstract
					&& type.BaseType?.IsGenericType == true
					&& type.BaseType.GetGenericTypeDefinition() == typeof(LocoObjectViewModel<>)
					&& type.BaseType.GenericTypeArguments.Single() == locoStruct.GetType());

			return asm == null
				? new GenericObjectViewModel() { Object = locoStruct }
				: (Activator.CreateInstance(asm, locoStruct) as IObjectViewModel<ILocoStruct>)!;
		}

		public override void Load()
		{
			// this stops any currently-playing sounds
			if (ExtraContentViewModel is SoundViewModel svm)
			{
				svm.Dispose();
			}

			Logger.Info($"Loading {CurrentFile.DisplayName} from {CurrentFile.Filename}");

			if (Model.TryLoadObject(CurrentFile, out var newObj))
			{
				CurrentObject = newObj;

				if (CurrentObject?.LocoObject != null)
				{
					CurrentObjectViewModel = GetViewModelFromStruct(CurrentObject.LocoObject.Object);

					//if (CurrentObjectViewModel is BuildingViewModel bvm)
					//{
					//	bvm.Images = G1ImageConversion.CreateAvaloniaImages(CurrentObject.Images).Cast<Avalonia.Media.IImage>().ToBindingList();
					//}

					StringTableViewModel = new(CurrentObject.LocoObject.StringTable);

					var imageNameProvider = (CurrentObject.LocoObject.Object is IImageTableNameProvider itnp)
						? itnp
						: new DefaultImageTableNameProvider();

					ExtraContentViewModel = CurrentObject.LocoObject.Object is SoundObject soundObject
						? new SoundViewModel(CurrentObject.DatFileInfo.S5Header.Name, soundObject.SoundObjectData.PcmHeader, soundObject.PcmData)
						: new ImageTableViewModel(CurrentObject.LocoObject, imageNameProvider, Model.PaletteMap, CurrentObject.Images, Model.Logger);

					var (treeView, annotationIdentifiers) = AnnotateFile(Path.Combine(Model.Settings.ObjDataDirectory, CurrentFile.Filename), Logger, false);
					CurrentHexAnnotations = new(treeView);
					DATDumpAnnotationIdentifiers = annotationIdentifiers;
				}
				else
				{
					StringTableViewModel = null;
					ExtraContentViewModel = null;
				}

				if (CurrentObject != null)
				{
					S5HeaderViewModel = new S5HeaderViewModel(CurrentObject.DatFileInfo.S5Header);
				}
			}
			else
			{
				// todo: show warnings here
				CurrentObject = null;
				CurrentObjectViewModel = null;
			}
		}

		public override void Delete()
		{
			if (CurrentFile.FileLocation != FileLocation.Local)
			{
				Logger.Error("Cannot delete non-local files");
				return;
			}

			// delete file
			var path = Path.Combine(Model.Settings.ObjDataDirectory, CurrentFile.Filename);
			if (File.Exists(path))
			{
				Logger.Info($"Deleting file \"{path}\"");
				File.Delete(path);
			}
			else
			{
				Logger.Info($"File already deleted \"{path}\"");
			}

			// remove from object index
			Model.ObjectIndex.Delete(x => x.Filename == CurrentFile.Filename);
		}

		public override void Save()
		{
			var savePath = CurrentFile.FileLocation == FileLocation.Local
				? Path.Combine(Model.Settings.ObjDataDirectory, CurrentFile.Filename)
				: Path.Combine(Model.Settings.DownloadFolder, Path.ChangeExtension(CurrentFile.DisplayName, ".dat"));
			SaveCore(savePath);
		}

		public override void SaveAs()
		{
			var saveFile = Task.Run(async () => await PlatformSpecific.SaveFilePicker(PlatformSpecific.DatFileTypes)).Result;
			if (saveFile != null)
			{
				SaveCore(saveFile.Path.LocalPath);
			}
		}

		void SaveCore(string filename)
		{
			if (CurrentObject?.LocoObject == null)
			{
				Logger.Error("Cannot save - loco object was null");
				return;
			}

			if (string.IsNullOrEmpty(filename))
			{
				Logger.Error("Cannot save - filename was empty");
				return;
			}

			var saveDir = Path.GetDirectoryName(filename);

			if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
			{
				Logger.Error("Cannot save - directory is invalid");
				return;
			}

			Logger.Info($"Saving {CurrentObject.DatFileInfo.S5Header.Name} to {filename}");
			StringTableViewModel?.WriteTableBackToObject();

			if (CurrentObjectViewModel is not null and not GenericObjectViewModel)
			{
				CurrentObject.LocoObject.Object = CurrentObjectViewModel.GetAsUnderlyingType(CurrentObject.LocoObject.Object);
			}

			SawyerStreamWriter.Save(filename,
				S5HeaderViewModel?.Name ?? CurrentObject.DatFileInfo.S5Header.Name,
				S5HeaderViewModel?.SourceGame ?? CurrentObject.DatFileInfo.S5Header.SourceGame,
				SawyerEncoding.Uncompressed, // todo: change based on what user selected
				CurrentObject.LocoObject,
				Logger,
				Model.Settings.AllowSavingAsVanillaObject);
		}

		(IList<TreeNode> treeView, Dictionary<string, (int, int)> annotationIdentifiers) AnnotateFile(string path, ILogger logger, bool isG1 = false)
		{
			if (!File.Exists(path))
			{
				return ([], []);
			}

			IList<HexAnnotation> annotations;
			currentByteList = File.ReadAllBytes(path);

			try
			{
				annotations = isG1
					? ObjectAnnotator.AnnotateG1Data(currentByteList)
					: ObjectAnnotator.Annotate(currentByteList);

				return AnnotateFileCore(annotations);
			}
			catch (Exception ex)
			{
				logger.Error(ex);
				return ([], []);
			}
		}

		static (IList<TreeNode> treeView, Dictionary<string, (int, int)> annotationIdentifiers) AnnotateFileCore(IList<HexAnnotation> annotations)
		{
			var treeView = new List<TreeNode>();
			var parents = new Dictionary<string, TreeNode>();
			Dictionary<string, TreeNode> imageHeaderIndexToNode = [];
			Dictionary<string, TreeNode> imageDataIndexToNode = [];
			Dictionary<string, (int, int)> datDumpAnnotationIdentifiers = [];

			foreach (var annotation in annotations)
			{
				var annotationText = annotation.Name;
				parents[annotationText] = new TreeNode(annotation.Name, annotation.OffsetText);
				datDumpAnnotationIdentifiers[annotationText] = (annotation.Start, annotation.End);
				if (annotation.Parent == null)
				{
					treeView.Add(parents[annotation.Name]);
				}
				else if (parents.TryGetValue(annotation.Parent.Name, out var parentNode))
				{
					parentNode.Nodes.Add(parents[annotationText]);

					if (annotation.Parent.Name == "Headers")
					{
						imageHeaderIndexToNode[annotation.Name] = parents[annotationText];
					}

					if (annotation.Parent.Name == "Images")
					{
						imageDataIndexToNode[annotation.Name] = parents[annotationText];
					}
				}
			}

			return (treeView, datDumpAnnotationIdentifiers);
		}

		static IEnumerable<HexAnnotationLine> GetDumpLines(byte[] byteList, int? selectionStart, int? selectionEnd)
		{
			if (byteList == null)
			{
				yield break;
			}

			var count = 0;

			foreach (var b in byteList.Chunk(bytesPerDumpLine))
			{
				int? sStart = selectionStart != null && (selectionStart >= count || selectionEnd > count)
					? (Math.Max(selectionStart.Value, count) * 2) - (count * 2) : null;

				int? sEnd = selectionEnd != null && sStart != null && selectionEnd >= count
					? (Math.Min(selectionEnd.Value, count + bytesPerDumpLine) * 2) - (count * 2) : null;

				yield return new HexAnnotationLine(
					string.Format("{0:X" + addressStringSizeBytes + "}", count),
					string.Join(" ", b.Chunk(4).Select(x => string.Concat(x.Select(y => y.ToString("X2"))))),
					sStart + (sStart / 8),
					sEnd + (sEnd / 8));

				count += bytesPerDumpLine;
			}
		}
	}

	public class TreeNode(string title, string offsetText, ObservableCollection<TreeNode> nodes)
	{
		public ObservableCollection<TreeNode> Nodes { get; } = nodes;
		public string Title { get; } = title;
		public string OffsetText { get; } = offsetText;
		public TreeNode() : this("<empty>", "<empty>") { }

		public TreeNode(string title, string offsetText) : this(title, offsetText, []) { }
	}
}
