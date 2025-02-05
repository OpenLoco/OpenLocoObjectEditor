using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class TrainSignalViewModel : LocoObjectViewModel<TrainSignalObject>
	{
		[Reactive, EnumProhibitValues<TrainSignalObjectFlags>(TrainSignalObjectFlags.None)] public TrainSignalObjectFlags Flags { get; set; }
		[Reactive] public uint8_t AnimationSpeed { get; set; }
		[Reactive] public uint8_t NumFrames { get; set; }
		[Reactive, Category("Cost")] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Cost")] public int16_t BuildCostFactor { get; set; }
		[Reactive, Category("Cost")] public int16_t SellCostFactor { get; set; }
		[Reactive, Category("Stats")] public uint16_t DesignedYear { get; set; }
		[Reactive, Category("Stats")] public uint16_t ObsoleteYear { get; set; }
		[Reactive, Length(0, TrainSignalObject.ModsLength)] public BindingList<S5HeaderViewModel> Mods { get; set; }

		public TrainSignalViewModel(TrainSignalObject ro)
		{
			Flags = ro.Flags;
			AnimationSpeed = ro.AnimationSpeed;
			NumFrames = ro.NumFrames;
			CostIndex = ro.CostIndex;
			BuildCostFactor = ro.BuildCostFactor;
			SellCostFactor = ro.SellCostFactor;
			DesignedYear = ro.DesignedYear;
			ObsoleteYear = ro.ObsoleteYear;
			Mods = new(ro.Mods.ConvertAll(x => new S5HeaderViewModel(x)));
		}

		public override TrainSignalObject GetAsStruct(TrainSignalObject tso)
			=> tso with
			{
				Flags = Flags,
				AnimationSpeed = AnimationSpeed,
				NumFrames = NumFrames,
				CostIndex = CostIndex,
				BuildCostFactor = BuildCostFactor,
				SellCostFactor = SellCostFactor,
				DesignedYear = DesignedYear,
				ObsoleteYear = ObsoleteYear,
				Mods = Mods.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				NumCompatible = (uint8_t)Mods.Count,
			};
	}
}
