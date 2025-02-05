using OpenLoco.Dat.Data;
using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using PropertyModels.Extensions;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class BuildingViewModel : LocoObjectViewModel<BuildingObject>
	{
		[Reactive, EnumProhibitValues<BuildingObjectFlags>(BuildingObjectFlags.None)] public BuildingObjectFlags Flags { get; set; }
		[Reactive] public uint32_t Colours { get; set; } // bitset
		[Reactive] public Colour ScaffoldingColour { get; set; }
		[Reactive] public uint8_t ScaffoldingSegmentType { get; set; }
		[Reactive] public uint8_t GeneratorFunction { get; set; }
		[Reactive] public uint8_t AverageNumberOnMap { get; set; }
		[Reactive, Category("Stats")] public int16_t DemolishRatingReduction { get; set; }
		[Reactive, Category("Stats")] public uint16_t DesignedYear { get; set; }
		[Reactive, Category("Stats")] public uint16_t ObsoleteYear { get; set; }
		[Reactive, Category("Cost")] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Cost")] public uint16_t SellCostFactor { get; set; }
		[Reactive, Category("Production"), Length(0, BuildingObject.MaxProducedCargoType)] public BindingList<S5HeaderViewModel> ProducedCargo { get; set; }
		[Reactive, Category("Production"), Length(0, BuildingObject.MaxProducedCargoType)] public BindingList<S5HeaderViewModel> RequiredCargo { get; set; }
		[Reactive, Category("Production"), Length(1, BuildingObject.MaxProducedCargoType)] public BindingList<uint8_t> ProducedQuantity { get; set; }
		[Reactive, Category("Building"), Length(1, BuildingObject.BuildingVariationCount)] public BindingList<BindingList<uint8_t>> BuildingVariations { get; set; } // NumBuildingVariations
		[Reactive, Category("Building"), Length(1, BuildingObject.BuildingHeightCount)] public BindingList<uint8_t> BuildingHeights { get; set; } // NumBuildingParts
		[Reactive, Category("Building"), Length(1, BuildingObject.BuildingAnimationCount)] public BindingList<BuildingPartAnimation> BuildingAnimations { get; set; } // NumBuildingParts

		// note: these height sequences are massive. BLDCTY28 has 2 sequences, 512 in length and 1024 in length. Avalonia PropertyGrid takes 30+ seconds to render this. todo: don't use property grid in future
		//[Reactive, Category("Building"), Length(1, BuildingObject.MaxElevatorHeightSequences), Browsable(false)] public BindingList<BindingList<uint8_t>> ElevatorHeightSequences { get; set; } // NumElevatorSequences

		[Reactive, Category("<unknown>"), Length(2, 2)] public BindingList<uint8_t> var_A6 { get; set; }
		[Reactive, Category("<unknown>"), Length(2, 2)] public BindingList<uint8_t> var_A8 { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_AC { get; set; }

		public BuildingViewModel(BuildingObject bo)
		{
			Flags = bo.Flags;
			Colours = bo.Colours;
			ScaffoldingColour = bo.ScaffoldingColour;
			ScaffoldingSegmentType = bo.ScaffoldingSegmentType;
			GeneratorFunction = bo.GeneratorFunction;
			AverageNumberOnMap = bo.AverageNumberOnMap;
			DemolishRatingReduction = bo.DemolishRatingReduction;
			DesignedYear = bo.DesignedYear;
			ObsoleteYear = bo.ObsoleteYear;
			CostIndex = bo.CostIndex;
			SellCostFactor = bo.SellCostFactor;
			ProducedCargo = new(bo.ProducedCargo.ConvertAll(x => new S5HeaderViewModel(x)));
			RequiredCargo = new(bo.RequiredCargo.ConvertAll(x => new S5HeaderViewModel(x)));
			ProducedQuantity = [.. bo.ProducedQuantity];
			BuildingHeights = new(bo.BuildingHeights);
			BuildingAnimations = new(bo.BuildingAnimations);
			BuildingVariations = new(bo.BuildingVariations.Select(x => new BindingList<uint8_t>(x)).ToBindingList());
			//ElevatorHeightSequences = new(bo.ElevatorHeightSequences.Select(x => new BindingList<uint8_t>(x)).ToBindingList());
			var_A6 = new(bo.var_A6);
			var_A8 = new(bo.var_A8);
			var_AC = bo.var_AC;
		}

		// validation:
		// BuildingVariationHeights.Count MUST equal BuildingVariationAnimations.Count
		public override BuildingObject GetAsStruct(BuildingObject bo)
			=> bo with
			{
				Flags = Flags,
				Colours = Colours,
				ScaffoldingColour = ScaffoldingColour,
				ScaffoldingSegmentType = ScaffoldingSegmentType,
				GeneratorFunction = GeneratorFunction,
				AverageNumberOnMap = AverageNumberOnMap,
				DemolishRatingReduction = DemolishRatingReduction,
				DesignedYear = DesignedYear,
				ObsoleteYear = ObsoleteYear,
				CostIndex = CostIndex,
				SellCostFactor = SellCostFactor,
				var_AC = var_AC,
				NumBuildingParts = (uint8_t)BuildingAnimations.Count,
				NumBuildingVariations = (uint8_t)BuildingVariations.Count,
				ProducedQuantity = [.. ProducedQuantity],
				ProducedCargo = ProducedCargo.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				RequiredCargo = RequiredCargo.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				//NumElevatorSequences = (uint8_t)bo.ElevatorHeightSequences.Count,
			};
	}
}
