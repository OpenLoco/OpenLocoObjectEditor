using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class TrainStationViewModel : LocoObjectViewModel<TrainStationObject>
	{
		[Reactive] public uint8_t PaintStyle { get; set; }
		[Reactive] public uint8_t Height { get; set; }
		[Reactive, EnumProhibitValues<TrackTraitFlags>(TrackTraitFlags.None)] public TrackTraitFlags TrackPieces { get; set; }
		[Reactive] public uint16_t DesignedYear { get; set; }
		[Reactive] public uint16_t ObsoleteYear { get; set; }
		[Reactive, EnumProhibitValues<TrainStationObjectFlags>(TrainStationObjectFlags.None)] public TrainStationObjectFlags Flags { get; set; }
		[Reactive] public BindingList<uint32_t> ImageOffsets { get; set; }
		[Reactive, Category("Cost")] public int16_t BuildCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t SellCostFactor { get; set; }
		[Reactive, Category("Cost")] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Compatible")] public BindingList<S5HeaderViewModel> Compatible { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_0B { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_0D { get; set; }

		//public uint8_t[][][] CargoOffsetBytes { get; set; }

		//public uint8_t[][] ManualPower { get; set; }

		public TrainStationViewModel(TrainStationObject tso)
		{
			PaintStyle = tso.PaintStyle;
			Height = tso.Height;
			TrackPieces = tso.TrackPieces;
			DesignedYear = tso.DesignedYear;
			ObsoleteYear = tso.ObsoleteYear;
			BuildCostFactor = tso.BuildCostFactor;
			SellCostFactor = tso.SellCostFactor;
			CostIndex = tso.CostIndex;
			Flags = tso.Flags;
			ImageOffsets = new(tso.ImageOffsets);
			var_0B = tso.var_0B;
			var_0D = tso.var_0D;
			Compatible = new(tso.Compatible.ConvertAll(x => new S5HeaderViewModel(x)));
		}

		// validation:
		// BuildingVariationHeights.Count MUST equal BuildingVariationAnimations.Count
		public override TrainStationObject GetAsStruct(TrainStationObject tso)
			=> tso with
			{
				PaintStyle = PaintStyle,
				Height = Height,
				TrackPieces = TrackPieces,
				DesignedYear = DesignedYear,
				ObsoleteYear = ObsoleteYear,
				BuildCostFactor = BuildCostFactor,
				SellCostFactor = SellCostFactor,
				CostIndex = CostIndex,
				Flags = Flags,
				var_0B = var_0B,
				var_0D = var_0D,
				NumCompatible = (uint8_t)Compatible.Count,
				Compatible = Compatible.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
			};
	}
}
