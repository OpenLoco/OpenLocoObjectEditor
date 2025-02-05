using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class RoadViewModel : LocoObjectViewModel<RoadObject>
	{
		[Reactive, EnumProhibitValues<RoadObjectFlags>(RoadObjectFlags.None)] public RoadObjectFlags Flags { get; set; }
		[Reactive, EnumProhibitValues<RoadTraitFlags>(RoadTraitFlags.None)] public RoadTraitFlags RoadPieces { get; set; }
		[Reactive] public Speed16 MaxSpeed { get; set; }
		[Reactive] public uint8_t PaintStyle { get; set; }
		[Reactive] public uint8_t DisplayOffset { get; set; }
		[Reactive] public TownSize TargetTownSize { get; set; }
		[Reactive] public S5HeaderViewModel Tunnel { get; set; }
		[Reactive, Category("Cost")] public int16_t BuildCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t SellCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t TunnelCostFactor { get; set; }
		[Reactive, Category("Cost")] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Bridges")] public BindingList<S5HeaderViewModel> Bridges { get; set; }
		[Reactive, Category("Stations")] public BindingList<S5HeaderViewModel> Stations { get; set; }
		[Reactive, Category("Mods")] public BindingList<S5HeaderViewModel> Mods { get; set; }
		[Reactive, Category("Compatible")] public BindingList<S5HeaderViewModel> Compatible { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_2F { get; set; }

		public RoadViewModel(RoadObject ro)
		{
			Flags = ro.Flags;
			RoadPieces = ro.RoadPieces;
			MaxSpeed = ro.MaxSpeed;
			PaintStyle = ro.PaintStyle;
			DisplayOffset = ro.DisplayOffset;
			TargetTownSize = ro.TargetTownSize;
			BuildCostFactor = ro.BuildCostFactor;
			SellCostFactor = ro.SellCostFactor;
			TunnelCostFactor = ro.TunnelCostFactor;
			CostIndex = ro.CostIndex;
			Tunnel = new(ro.Tunnel);
			Compatible = new(ro.Compatible.ConvertAll(x => new S5HeaderViewModel(x)));
			Mods = new(ro.Mods.ConvertAll(x => new S5HeaderViewModel(x)));
			Bridges = new(ro.Bridges.ConvertAll(x => new S5HeaderViewModel(x)));
			Stations = new(ro.Stations.ConvertAll(x => new S5HeaderViewModel(x)));
			var_2F = ro.var_2F;
		}

		public override RoadObject GetAsStruct(RoadObject ro)
			=> ro with
			{
				Flags = Flags,
				RoadPieces = RoadPieces,
				MaxSpeed = MaxSpeed,
				PaintStyle = PaintStyle,
				DisplayOffset = DisplayOffset,
				TargetTownSize = TargetTownSize,
				BuildCostFactor = BuildCostFactor,
				SellCostFactor = SellCostFactor,
				TunnelCostFactor = TunnelCostFactor,
				CostIndex = CostIndex,
				NumCompatible = (uint8_t)Compatible.Count,
				Compatible = Compatible.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				NumMods = (uint8_t)Mods.Count,
				Mods = Mods.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				Tunnel = Tunnel.GetAsUnderlyingType(),
				NumBridges = (uint8_t)Bridges.Count,
				Bridges = Bridges.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				NumStations = (uint8_t)Stations.Count,
				Stations = Stations.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				var_2F = var_2F,
			};
	}
}
