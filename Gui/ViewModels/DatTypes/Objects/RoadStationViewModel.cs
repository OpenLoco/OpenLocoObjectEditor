using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class RoadStationViewModel : LocoObjectViewModel<RoadStationObject>
	{
		[Reactive] public uint8_t PaintStyle { get; set; }
		[Reactive] public uint8_t Height { get; set; }
		[Reactive, EnumProhibitValues<RoadTraitFlags>(RoadTraitFlags.None)] public RoadTraitFlags RoadPieces { get; set; }
		[Reactive] public uint16_t DesignedYear { get; set; }
		[Reactive] public uint16_t ObsoleteYear { get; set; }
		[Reactive] public RoadStationObjectFlags Flags { get; set; }
		[Reactive] public BindingList<uint32_t> ImageOffsets { get; set; }
		[Reactive, Category("Cost")] public int16_t BuildCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t SellCostFactor { get; set; }
		[Reactive, Category("Cost")] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Compatible")] public BindingList<S5HeaderViewModel> Compatible { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_2D { get; set; }

		public RoadStationViewModel(RoadStationObject ro)
		{
			PaintStyle = ro.PaintStyle;
			Height = ro.Height;
			RoadPieces = ro.RoadPieces;
			DesignedYear = ro.DesignedYear;
			ObsoleteYear = ro.ObsoleteYear;
			BuildCostFactor = ro.BuildCostFactor;
			SellCostFactor = ro.SellCostFactor;
			CostIndex = ro.CostIndex;
			Flags = ro.Flags;
			ImageOffsets = new(ro.ImageOffsets);
			var_2D = ro.var_2D;
			Compatible = new(ro.Compatible.ConvertAll(x => new S5HeaderViewModel(x)));
		}

		public override RoadStationObject GetAsStruct(RoadStationObject ro)
			=> ro with
			{
				PaintStyle = PaintStyle,
				Height = Height,
				RoadPieces = RoadPieces,
				DesignedYear = DesignedYear,
				ObsoleteYear = ObsoleteYear,
				BuildCostFactor = BuildCostFactor,
				SellCostFactor = SellCostFactor,
				CostIndex = CostIndex,
				Flags = Flags,
				var_2D = var_2D,
				NumCompatible = (uint8_t)Compatible.Count,
				Compatible = Compatible.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
			};
	}
}
