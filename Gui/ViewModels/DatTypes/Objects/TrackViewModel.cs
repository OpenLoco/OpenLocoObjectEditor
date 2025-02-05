using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class TrackViewModel : LocoObjectViewModel<TrackObject>
	{
		[Reactive, EnumProhibitValues<TrackObjectFlags>(TrackObjectFlags.None)] public TrackObjectFlags Flags { get; set; }
		[Reactive, EnumProhibitValues<TrackTraitFlags>(TrackTraitFlags.None)] public TrackTraitFlags TrackPieces { get; set; }
		[Reactive, EnumProhibitValues<TrackTraitFlags>(TrackTraitFlags.None)] public TrackTraitFlags StationTrackPieces { get; set; }
		[Reactive] public Speed16 CurveSpeed { get; set; }
		[Reactive] public uint8_t DisplayOffset { get; set; }
		[Reactive] public S5HeaderViewModel CompatibleTunnel { get; set; }
		[Reactive, Category("Cost")] public int16_t BuildCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t SellCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t TunnelCostFactor { get; set; }
		[Reactive, Category("Cost")] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Compatible")] public BindingList<S5HeaderViewModel> CompatibleVehicles { get; set; }
		[Reactive, Category("Mods")] public BindingList<S5HeaderViewModel> CompatibleTrackExtras { get; set; }
		[Reactive, Category("Signals")] public BindingList<S5HeaderViewModel> CompatibleSignals { get; set; }
		[Reactive, Category("Bridges")] public BindingList<S5HeaderViewModel> CompatibleBridges { get; set; }
		[Reactive, Category("Stations")] public BindingList<S5HeaderViewModel> CompatibleStations { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_06 { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_35 { get; set; }

		public TrackViewModel(TrackObject to)
		{
			Flags = to.Flags;
			TrackPieces = to.TrackPieces;
			StationTrackPieces = to.StationTrackPieces;
			CurveSpeed = to.CurveSpeed;
			DisplayOffset = to.DisplayOffset;
			BuildCostFactor = to.BuildCostFactor;
			SellCostFactor = to.SellCostFactor;
			TunnelCostFactor = to.TunnelCostFactor;
			CostIndex = to.CostIndex;
			CompatibleVehicles = new(to.Vehicles.ConvertAll(x => new S5HeaderViewModel(x)));
			CompatibleTrackExtras = new(to.TrackMods.ConvertAll(x => new S5HeaderViewModel(x)));
			CompatibleSignals = new(to.Signals.ConvertAll(x => new S5HeaderViewModel(x)));
			CompatibleTunnel = new(to.Tunnel);
			CompatibleBridges = new(to.Bridges.ConvertAll(x => new S5HeaderViewModel(x)));
			CompatibleStations = new(to.Stations.ConvertAll(x => new S5HeaderViewModel(x)));
			var_06 = to.var_06;
			var_35 = to.var_35;
		}

		// validation:
		// BuildingVariationHeights.Count MUST equal BuildingVariationAnimations.Count
		public override TrackObject GetAsStruct(TrackObject to)
			=> to with
			{
				Flags = Flags,
				TrackPieces = TrackPieces,
				StationTrackPieces = StationTrackPieces,
				CurveSpeed = CurveSpeed,
				DisplayOffset = DisplayOffset,
				BuildCostFactor = BuildCostFactor,
				SellCostFactor = SellCostFactor,
				TunnelCostFactor = TunnelCostFactor,
				CostIndex = CostIndex,
				NumCompatibleVehicles = (uint8_t)CompatibleVehicles.Count,
				Vehicles = CompatibleVehicles.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				NumMods = (uint8_t)CompatibleTrackExtras.Count,
				TrackMods = CompatibleTrackExtras.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				NumSignals = (uint8_t)CompatibleSignals.Count,
				Signals = CompatibleSignals.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				Tunnel = CompatibleTunnel.GetAsUnderlyingType(),
				NumBridges = (uint8_t)CompatibleBridges.Count,
				Bridges = CompatibleBridges.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				NumStations = (uint8_t)CompatibleStations.Count,
				Stations = CompatibleStations.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				var_06 = var_06,
				var_35 = var_35,
			};
	}
}
