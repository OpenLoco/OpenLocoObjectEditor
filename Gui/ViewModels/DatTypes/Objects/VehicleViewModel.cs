using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class VehicleViewModel : LocoObjectViewModel<VehicleObject>
	{
		[Reactive, Category("Stats")] public TransportMode Mode { get; set; }
		[Reactive, Category("Stats")] public VehicleType Type { get; set; }
		[Reactive, Category("Stats")] public uint16_t Weight { get; set; }
		[Reactive, Category("Stats")] public uint16_t Power { get; set; }
		[Reactive, Category("Stats")] public Speed16 Speed { get; set; }
		[Reactive, Category("Stats")] public Speed16 RackSpeed { get; set; }
		[Reactive, Category("Stats")] public uint8_t RackRailType { get; set; }
		[Reactive, Category("Stats")] public uint16_t DesignedYear { get; set; }
		[Reactive, Category("Stats")] public uint16_t ObsoleteYear { get; set; }
		[Reactive, Category("Stats")] public uint8_t Reliability { get; set; }
		[Reactive, EnumProhibitValues<VehicleObjectFlags>(VehicleObjectFlags.None)] public VehicleObjectFlags Flags { get; set; }
		[Reactive] public S5HeaderViewModel? TrackType { get; set; }
		[Reactive] public S5HeaderViewModel? RackRail { get; set; }
		[Reactive, Length(0, 8)] public BindingList<S5HeaderViewModel> CompatibleVehicles { get; set; }
		[Reactive, Length(0, 4)] public BindingList<S5HeaderViewModel> RequiredTrackExtras { get; set; }
		[Reactive, Category("Cost"), Range(0, 32)] public uint8_t CostIndex { get; set; }
		[Reactive, Category("Cost"), Range(1, int16_t.MaxValue)] public int16_t CostFactor { get; set; }
		[Reactive, Category("Cost"), Range(0, 32)] public uint8_t RunCostIndex { get; set; }
		[Reactive, Category("Cost"), Range(0, int16_t.MaxValue)] public int16_t RunCostFactor { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_04 { get; set; }
		[Reactive, Category("<unknown>")] public uint8_t var_113 { get; set; }
		[Reactive, Category("Sprites")] public CompanyColourType SpecialColourSchemeIndex { get; set; } // called "ColourType" in the loco codebase
		[Reactive, Category("Sprites"), Editable(false)] public BindingList<VehicleObjectCar> CarComponents { get; set; }
		[Reactive, Category("Sprites"), Editable(false)] public BindingList<BodySprite> BodySprites { get; set; }
		[Reactive, Category("Sprites"), Editable(false)] public BindingList<BogieSprite> BogieSprites { get; set; }
		[Reactive, Category("Sprites"), Editable(false)] public BindingList<SimpleAnimation> Animation { get; set; }
		[Reactive, Category("Sprites")] public BindingList<S5HeaderViewModel> AnimationHeaders { get; set; }
		//[Reactive, Category("Cargo")] public BindingList<uint8_t> MaxCargo { get; set; }
		//[Reactive, Category("Cargo")] public BindingList<CargoCategory> CompatibleCargoCategories1 { get; set; }
		//[Reactive, Category("Cargo")] public BindingList<CargoCategory> CompatibleCargoCategories2 { get; set; }
		[Reactive, Category("Cargo")] public CompatibleCargo CompatibleCargo1 { get; set; }
		[Reactive, Category("Cargo")] public CompatibleCargo CompatibleCargo2 { get; set; }
		[Reactive, Category("Cargo"), Length(0, 32)] public BindingList<CargoTypeSpriteOffset> CargoTypeSpriteOffsets { get; set; } // this is a dictionary type
		[Reactive, Category("Sound")] public S5HeaderViewModel? Sound { get; set; }
		[Reactive, Category("Sound")] public DrivingSoundType SoundType { get; set; }
		// SoundPropertiesData
		// these next 3 properties are a union in the dat file
		[Reactive, Category("Sound")] public FrictionSound? FrictionSound { get; set; }
		[Reactive, Category("Sound")] public Engine1Sound? Engine1Sound { get; set; }
		[Reactive, Category("Sound")] public Engine2Sound? Engine2Sound { get; set; }
		[Reactive, Category("Sound")] public BindingList<S5HeaderViewModel> StartSounds { get; set; }

		public VehicleViewModel(VehicleObject vo)
		{
			Mode = vo.Mode;
			Type = vo.Type;
			var_04 = vo.var_04;
			TrackType = vo.TrackType == null ? null : new(vo.TrackType);
			CostIndex = vo.CostIndex;
			CostFactor = vo.CostFactor;
			Reliability = vo.Reliability;
			RunCostIndex = vo.RunCostIndex;
			RunCostFactor = vo.RunCostFactor;
			SpecialColourSchemeIndex = vo.SpecialColourSchemeIndex;
			CompatibleVehicles = new(vo.CompatibleVehicles.ConvertAll(x => new S5HeaderViewModel(x)));
			RequiredTrackExtras = new(vo.RequiredTrackExtras.ConvertAll(x => new S5HeaderViewModel(x)));
			CarComponents = new(vo.CarComponents);
			BodySprites = new(vo.BodySprites);
			BogieSprites = new(vo.BogieSprites);
			Power = vo.Power;
			Speed = vo.Speed;
			RackSpeed = vo.RackSpeed;
			Weight = vo.Weight;
			Flags = vo.Flags;
			CompatibleCargo1 = new(vo.MaxCargo[0], new(vo.CompatibleCargoCategories[0]));
			CompatibleCargo2 = new(vo.MaxCargo[1], new(vo.CompatibleCargoCategories[1]));
			CargoTypeSpriteOffsets = new(vo.CargoTypeSpriteOffsets.Select(x => new CargoTypeSpriteOffset(x.Key, x.Value)).ToList());
			Animation = new(vo.Animation);
			AnimationHeaders = new(vo.AnimationHeaders.ConvertAll(x => new S5HeaderViewModel(x)));
			var_113 = vo.var_113;
			DesignedYear = vo.DesignedYear;
			ObsoleteYear = vo.ObsoleteYear;
			RackRail = vo.RackRail == null ? null : new(vo.RackRail);
			Sound = vo.Sound == null ? null : new(vo.Sound);
			StartSounds = new(vo.StartSounds.ConvertAll(x => new S5HeaderViewModel(x)));
			SoundType = vo.DrivingSoundType;
			FrictionSound = vo.SoundPropertyFriction;
			Engine1Sound = vo.SoundPropertyEngine1;
			Engine2Sound = vo.SoundPropertyEngine2;
		}

		public override VehicleObject GetAsStruct(VehicleObject vo)
		{
			foreach (var ctso in CargoTypeSpriteOffsets)
			{
				vo.CargoTypeSpriteOffsets[ctso.CargoCategory] = ctso.Offset;
			}

			return vo with
			{
				Mode = Mode,
				Type = Type,
				var_04 = var_04,
				TrackType = TrackType?.GetAsUnderlyingType(),
				CostIndex = CostIndex,
				CostFactor = CostFactor,
				Reliability = Reliability,
				RunCostIndex = RunCostIndex,
				RunCostFactor = RunCostFactor,
				SpecialColourSchemeIndex = SpecialColourSchemeIndex,
				Power = Power,
				Speed = Speed,
				RackSpeed = RackSpeed,
				Weight = Weight,
				Flags = Flags,
				var_113 = var_113,
				DesignedYear = DesignedYear,
				ObsoleteYear = ObsoleteYear,
				RackRail = RackRail?.GetAsUnderlyingType(),
				Sound = Sound?.GetAsUnderlyingType(),
				StartSounds = StartSounds.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				CompatibleVehicles = CompatibleVehicles.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				RequiredTrackExtras = RequiredTrackExtras.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				AnimationHeaders = AnimationHeaders.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				DrivingSoundType = SoundType,
				SoundPropertyFriction = FrictionSound,
				SoundPropertyEngine1 = Engine1Sound,
				SoundPropertyEngine2 = Engine2Sound,
				NumCompatibleVehicles = (uint8_t)CompatibleVehicles.Count,
				NumRequiredTrackExtras = (uint8_t)RequiredTrackExtras.Count,
				NumStartSounds = (uint8_t)StartSounds.Count,
				MaxCargo = [CompatibleCargo1.MaxCargo, CompatibleCargo2.MaxCargo],
				CompatibleCargoCategories1 = [.. CompatibleCargo1.CompatibleCargoCategories],
				CompatibleCargoCategories2 = [.. CompatibleCargo2.CompatibleCargoCategories],
			};
		}
	}

	[TypeConverter(typeof(ExpandableObjectConverter))]
	public record CargoTypeSpriteOffset(CargoCategory CargoCategory, uint8_t Offset)
	{
		public CargoTypeSpriteOffset() : this(CargoCategory.NULL, 0)
		{ }
	}

	[TypeConverter(typeof(ExpandableObjectConverter))]
	public record CompatibleCargo(uint8_t MaxCargo, BindingList<CargoCategory> CompatibleCargoCategories);
}
