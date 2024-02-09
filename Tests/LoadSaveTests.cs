using Core.Objects;
using Core.Objects.Sound;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenLoco.ObjectEditor.Data;
using OpenLoco.ObjectEditor.DatFileParsing;
using OpenLoco.ObjectEditor.Headers;
using OpenLoco.ObjectEditor.Objects;
using OpenLoco.ObjectEditor.Types;
using Logger = OpenLoco.ObjectEditor.Logging.Logger;

namespace OpenLoco.ObjectEditor.Tests
{
	[TestFixture]
	public class LoadSaveTests
	{
		const string BaseObjDataPath = "Q:\\Steam\\steamapps\\common\\Locomotion\\ObjData\\";

		// TODO: find a way to not have to hardcode a path here (but this may be impossible as it will depend on a user's PC and Loco install path)
		// TODO: find a nicer (and more automated) way to check Name+Image fields, StringTable and G1Table

		static (ILocoObject, T) LoadObject<T>(string filename) where T : ILocoStruct
		{
			filename = Path.Combine(BaseObjDataPath, filename);
			var fileSize = new FileInfo(filename).Length;
			var logger = new Logger();
			var loaded = SawyerStreamReader.LoadFullObjectFromFile(filename, logger: logger);
			Assert.Multiple(() =>
			{
				Assert.That(loaded.LocoObject, Is.Not.Null);
				Assert.That(loaded.DatFileInfo.ObjectHeader.DataLength, Is.EqualTo(fileSize - S5Header.StructLength - ObjectHeader.StructLength), "ObjectHeader.Length didn't match actual size of struct");
			});
			return (loaded.LocoObject!, (T)loaded.LocoObject!.Object);
		}

		static (ILocoObject, T) LoadObject<T>(ReadOnlySpan<byte> data) where T : ILocoStruct
		{
			var logger = new Logger();
			var loaded = SawyerStreamReader.LoadFullObjectFromStream(data, logger: logger);

			Assert.That(loaded.LocoObject, Is.Not.Null);
			Assert.That(loaded.DatFileInfo.ObjectHeader.DataLength, Is.EqualTo(data.Length - S5Header.StructLength - ObjectHeader.StructLength), "ObjectHeader.Length didn't match actual size of struct");

			return (loaded.LocoObject!, (T)loaded.LocoObject!.Object);
		}

		public void LoadSaveGenericTest<T>(string objectName, Action<ILocoObject, T> assertFunc) where T : ILocoStruct
		{
			var (obj1, struc1) = LoadObject<T>(objectName);
			assertFunc(obj1, struc1);

			var bytes1 = SawyerStreamWriter.WriteLocoObject(objectName, obj1);

			var (obj2, struc2) = LoadObject<T>(bytes1);
			assertFunc(obj2, struc2);

			var bytes2 = SawyerStreamWriter.WriteLocoObject(objectName, obj2);

			// we could just simply combare byte arrays and be done, but i wanted something that makes it easier to diagnose problems

			// grab headers first
			var bytes1S5Header = S5Header.Read(bytes1[0..S5Header.StructLength]);
			var bytes2S5Header = S5Header.Read(bytes2[0..S5Header.StructLength]);

			var bytes1ObjHeader = ObjectHeader.Read(bytes1[S5Header.StructLength..(S5Header.StructLength + ObjectHeader.StructLength)]);
			var bytes2ObjHeader = ObjectHeader.Read(bytes2[S5Header.StructLength..(S5Header.StructLength + ObjectHeader.StructLength)]);

			// then grab object bytes
			var bytes1ObjArr = bytes1[21..].ToArray();
			var bytes2ObjArr = bytes2[21..].ToArray();

			Assert.Multiple(() =>
			{
				Assert.That(bytes2S5Header, Is.EqualTo(bytes1S5Header));
				Assert.That(bytes2ObjHeader, Is.EqualTo(bytes1ObjHeader));
				CollectionAssert.AreEqual(bytes1ObjArr.ToArray(), bytes2ObjArr.ToArray());
			});
		}

		[TestCase("AIRPORT1.DAT")]
		public void AirportObject(string objectName)
		{
			void assertFunc(ILocoObject obj, AirportObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0), nameof(struc.Name));
				Assert.That(struc.BuildCostFactor, Is.EqualTo(256), nameof(struc.BuildCostFactor));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-192), nameof(struc.SellCostFactor));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.Image, Is.EqualTo(0), nameof(struc.Image));
				Assert.That(struc.ImageOffset, Is.EqualTo(0), nameof(struc.ImageOffset));
				Assert.That(struc.AllowedPlaneTypes, Is.EqualTo(24), nameof(struc.AllowedPlaneTypes));
				Assert.That(struc.NumBuildingAnimations, Is.EqualTo(94), nameof(struc.NumBuildingAnimations));
				Assert.That(struc.NumBuildingVariations, Is.EqualTo(23), nameof(struc.NumBuildingVariations));

				//Assert.That(struc.var_14, Is.EqualTo(0), nameof(struc.var_14));
				//Assert.That(struc.var_18, Is.EqualTo(0), nameof(struc.var_18));
				//Assert.That(struc.var_1C, Is.EqualTo(0), nameof(struc.var_1C));
				//Assert.That(struc.var_9C, Is.EqualTo(0), nameof(struc.var_9C));

				Assert.That(struc.LargeTiles, Is.EqualTo(917759), nameof(struc.LargeTiles));
				Assert.That(struc.MinX, Is.EqualTo(-4), nameof(struc.MinX));
				Assert.That(struc.MinY, Is.EqualTo(-4), nameof(struc.MinY));
				Assert.That(struc.MaxX, Is.EqualTo(5), nameof(struc.MaxX));
				Assert.That(struc.MaxY, Is.EqualTo(5), nameof(struc.MaxY));
				Assert.That(struc.DesignedYear, Is.EqualTo(1970), nameof(struc.DesignedYear));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(65535), nameof(struc.ObsoleteYear));
				Assert.That(struc.NumMovementNodes, Is.EqualTo(26), nameof(struc.NumMovementNodes));
				Assert.That(struc.NumMovementEdges, Is.EqualTo(30), nameof(struc.NumMovementEdges));

				//Assert.That(struc.MovementNodes, Is.EqualTo(0), nameof(struc.MovementNodes));
				//Assert.That(struc.MovementEdges, Is.EqualTo(0), nameof(struc.MovementEdges));

				Assert.That(struc.pad_B6[0], Is.EqualTo(0), nameof(struc.pad_B6) + "[0]");
				Assert.That(struc.pad_B6[1], Is.EqualTo(19), nameof(struc.pad_B6) + "[1]");
				Assert.That(struc.pad_B6[2], Is.EqualTo(0), nameof(struc.pad_B6) + "[2]");
				Assert.That(struc.pad_B6[3], Is.EqualTo(0), nameof(struc.pad_B6) + "[3]");
			});
			LoadSaveGenericTest<AirportObject>(objectName, assertFunc);
		}

		[TestCase("BRDGBRCK.DAT")]
		public void BridgeObject(string objectName)
		{
			void assertFunc(ILocoObject obj, BridgeObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.NoRoof, Is.EqualTo(0), nameof(struc.NoRoof));

				// Assert.That(struc.pad_03[0], Is.EqualTo(0), nameof(struc.pad_03) + "[0]");
				// Assert.That(struc.pad_03[1], Is.EqualTo(0), nameof(struc.pad_03) + "[1]");
				// Assert.That(struc.pad_03[2], Is.EqualTo(0), nameof(struc.pad_03) + "[2]");

				Assert.That(struc.var_06, Is.EqualTo(16), nameof(struc.var_06));
				Assert.That(struc.SpanLength, Is.EqualTo(1), nameof(struc.SpanLength));
				Assert.That(struc.PillarSpacing, Is.EqualTo(255), nameof(struc.PillarSpacing));
				Assert.That(struc.MaxSpeed, Is.EqualTo(60), nameof(struc.MaxSpeed));
				Assert.That(struc.MaxHeight, Is.EqualTo(10), nameof(struc.MaxHeight));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.BaseCostFactor, Is.EqualTo(16), nameof(struc.BaseCostFactor));
				Assert.That(struc.HeightCostFactor, Is.EqualTo(8), nameof(struc.HeightCostFactor));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-12), nameof(struc.SellCostFactor));
				Assert.That(struc.DisabledTrackCfg, Is.EqualTo(0), nameof(struc.DisabledTrackCfg));
				//Assert.That(struc.TrackNumCompatible, Is.EqualTo(0), nameof(struc.TrackNumCompatible));
				//CollectionAssert.AreEqual(struc.TrackMods, Array.CreateInstance(typeof(byte), 7), nameof(struc.TrackMods));
				//Assert.That(struc.RoadNumCompatible, Is.EqualTo(0), nameof(struc.RoadNumCompatible));
				//CollectionAssert.AreEqual(struc.RoadMods, Array.CreateInstance(typeof(byte), 7), nameof(struc.RoadMods));
				Assert.That(struc.DesignedYear, Is.EqualTo(0), nameof(struc.DesignedYear));
			});
			LoadSaveGenericTest<BridgeObject>(objectName, assertFunc);
		}

		[TestCase("HQ1.DAT")]
		public void BuildingObject(string objectName)
		{
			void assertFunc(ILocoObject obj, BuildingObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0));
				Assert.That(struc.Image, Is.EqualTo(0));

				Assert.That(struc.NumVariations, Is.EqualTo(5), nameof(struc.NumVariations));
				// CollectionAssert.AreEqual(struc.VariationHeights, Array.CreateInstance(typeof(byte), 4), nameof(struc.VariationHeights));
				// VariationHeights
				// VariationAnimations
				// VariationParts
				Assert.That(struc.Colours, Is.EqualTo(0), nameof(struc.Colours));
				Assert.That(struc.DesignedYear, Is.EqualTo(0), nameof(struc.DesignedYear));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(65535), nameof(struc.ObsoleteYear));
				Assert.That(struc.Flags, Is.EqualTo(BuildingObjectFlags.LargeTile | BuildingObjectFlags.MiscBuilding | BuildingObjectFlags.IsHeadquarters), nameof(struc.Flags));
				Assert.That(struc.ClearCostIndex, Is.EqualTo(1), nameof(struc.ClearCostIndex));
				Assert.That(struc.ClearCostFactor, Is.EqualTo(0), nameof(struc.ClearCostFactor));
				Assert.That(struc.ScaffoldingSegmentType, Is.EqualTo(1), nameof(struc.ScaffoldingSegmentType));
				Assert.That(struc.ScaffoldingColour, Is.EqualTo(Colour.yellow), nameof(struc.ScaffoldingColour));
				Assert.That(struc.GeneratorFunction, Is.EqualTo(3), nameof(struc.GeneratorFunction));
				Assert.That(struc.var_9F, Is.EqualTo(3), nameof(struc.var_9F));
				// ProducedQuantity
				// ProducedCargoType
				// RequiredCargoType
				// CollectionAssert.AreEqual(struc.var_A6, Array.CreateInstance(typeof(byte), 2), nameof(struc.var_A6));
				// CollectionAssert.AreEqual(struc.var_A8, Array.CreateInstance(typeof(byte), 2), nameof(struc.var_A8));
				// CollectionAssert.AreEqual(struc.var_A4, Array.CreateInstance(typeof(byte), 2), nameof(struc.var_A4));
				Assert.That(struc.DemolishRatingReduction, Is.EqualTo(0), nameof(struc.DemolishRatingReduction));
				Assert.That(struc.var_AC, Is.EqualTo(255), nameof(struc.var_AC));
				Assert.That(struc.var_AD, Is.EqualTo(0), nameof(struc.var_AD));
			});
			LoadSaveGenericTest<BuildingObject>(objectName, assertFunc);
		}

		[TestCase("CHEMICAL.DAT")]
		public void CargoObject(string objectName)
		{
			void assertFunc(ILocoObject obj, CargoObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.var_02, Is.EqualTo(256), nameof(struc.var_02));
				Assert.That(struc.CargoTransferTime, Is.EqualTo(64), nameof(struc.CargoTransferTime));
				Assert.That(struc.UnitInlineSprite, Is.EqualTo(0), nameof(struc.UnitInlineSprite));
				Assert.That(struc.CargoCategory, Is.EqualTo(CargoCategory.liquids), nameof(struc.CargoCategory));
				Assert.That(struc.Flags, Is.EqualTo(CargoObjectFlags.Delivering), nameof(struc.Flags));
				Assert.That(struc.NumPlatformVariations, Is.EqualTo(1), nameof(struc.NumPlatformVariations));
				Assert.That(struc.var_14, Is.EqualTo(4), nameof(struc.var_14));
				Assert.That(struc.PremiumDays, Is.EqualTo(10), nameof(struc.PremiumDays));
				Assert.That(struc.MaxNonPremiumDays, Is.EqualTo(30), nameof(struc.MaxNonPremiumDays));
				Assert.That(struc.MaxPremiumRate, Is.EqualTo(128), nameof(struc.MaxPremiumRate));
				Assert.That(struc.PenaltyRate, Is.EqualTo(256), nameof(struc.PenaltyRate));
				Assert.That(struc.PaymentFactor, Is.EqualTo(62), nameof(struc.PaymentFactor));
				Assert.That(struc.PaymentIndex, Is.EqualTo(10), nameof(struc.PaymentIndex));
				Assert.That(struc.UnitSize, Is.EqualTo(10), nameof(struc.UnitSize));
			});
			LoadSaveGenericTest<CargoObject>(objectName, assertFunc);
		}

		[TestCase("LSBROWN.DAT")]
		public void CliffEdgeObject(string objectName)
		{
			void assertFunc(ILocoObject obj, CliffEdgeObject struc) => Assert.Multiple(() =>
			{
				var strTable = obj.StringTable;

				Assert.That(strTable.Table, Has.Count.EqualTo(1));
				Assert.That(strTable.Table.ContainsKey("Name"), Is.True);

				var entry = strTable.Table["Name"];

				Assert.That(entry[LanguageId.english_uk], Is.EqualTo("Brown Rock"));
				Assert.That(entry[LanguageId.english_us], Is.EqualTo("Brown Rock"));
			});
			LoadSaveGenericTest<CliffEdgeObject>(objectName, assertFunc);
		}

		[TestCase("CLIM1.DAT")]
		public void ClimateObject(string objectName)
		{
			void assertFunc(ILocoObject obj, ClimateObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.FirstSeason, Is.EqualTo(1), nameof(struc.FirstSeason));
				Assert.That(struc.SeasonLengths[0], Is.EqualTo(57), nameof(struc.SeasonLengths) + "[0]");
				Assert.That(struc.SeasonLengths[1], Is.EqualTo(80), nameof(struc.SeasonLengths) + "[1]");
				Assert.That(struc.SeasonLengths[2], Is.EqualTo(100), nameof(struc.SeasonLengths) + "[2]");
				Assert.That(struc.SeasonLengths[3], Is.EqualTo(80), nameof(struc.SeasonLengths) + "[3]");
				Assert.That(struc.WinterSnowLine, Is.EqualTo(48), nameof(struc.WinterSnowLine));
				Assert.That(struc.SummerSnowLine, Is.EqualTo(76), nameof(struc.SummerSnowLine));
				Assert.That(struc.pad_09, Is.EqualTo(0), nameof(struc.pad_09));
			});
			LoadSaveGenericTest<ClimateObject>(objectName, assertFunc);
		}

		[TestCase("COMP1.DAT")]
		public void CompetitorObject(string objectName)
		{
			void assertFunc(ILocoObject obj, CompetitorObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.var_04, Is.EqualTo(6672), nameof(struc.var_04));
				Assert.That(struc.var_08, Is.EqualTo(2053), nameof(struc.var_08));
				Assert.That(struc.Emotions, Is.EqualTo(255), nameof(struc.Emotions));
				CollectionAssert.AreEqual(struc.Images, Array.CreateInstance(typeof(byte), 9), nameof(struc.Images));
				Assert.That(struc.Intelligence, Is.EqualTo(7), nameof(struc.Intelligence));
				Assert.That(struc.Aggressiveness, Is.EqualTo(5), nameof(struc.Aggressiveness));
				Assert.That(struc.Competitiveness, Is.EqualTo(6), nameof(struc.Competitiveness));
				Assert.That(struc.var_37, Is.EqualTo(0), nameof(struc.var_37));
			});
			LoadSaveGenericTest<CompetitorObject>(objectName, assertFunc);
		}

		[TestCase("CURRDOLL.DAT")]
		public void CurrencyObject(string objectName)
		{
			void assertFunc(ILocoObject obj, CurrencyObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.ObjectIcon, Is.EqualTo(0), nameof(struc.ObjectIcon));
				Assert.That(struc.Separator, Is.EqualTo(0), nameof(struc.Separator));
				Assert.That(struc.Factor, Is.EqualTo(1), nameof(struc.Factor));
			});
			LoadSaveGenericTest<CurrencyObject>(objectName, assertFunc);
		}

		[TestCase("SHIPST1.DAT")]
		public void DockObject(string objectName)
		{
			void assertFunc(ILocoObject obj, DockObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.BuildCostFactor, Is.EqualTo(38), nameof(struc.BuildCostFactor));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-35), nameof(struc.SellCostFactor));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.var_07, Is.EqualTo(0), nameof(struc.var_07));
				Assert.That(struc.UnkImage, Is.EqualTo(0), nameof(struc.UnkImage));
				Assert.That(struc.Flags, Is.EqualTo(DockObjectFlags.None), nameof(struc.Flags));
				Assert.That(struc.NumAux01, Is.EqualTo(2), nameof(struc.NumAux01));
				Assert.That(struc.NumAux02Ent, Is.EqualTo(1), nameof(struc.NumAux02Ent));

				//Assert.That(struc.var_14, Is.EqualTo(1), nameof(struc.var_14));
				//Assert.That(struc.var_14, Is.EqualTo(1), nameof(struc.var_18));
				//Assert.That(struc.var_1C[0], Is.EqualTo(1), nameof(struc.var_1C[0]));

				Assert.That(struc.DesignedYear, Is.EqualTo(0), nameof(struc.DesignedYear));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(65535), nameof(struc.ObsoleteYear));
				Assert.That(struc.BoatPosition, Is.EqualTo(new Pos2(48, 0)), nameof(struc.BoatPosition));
			});
			LoadSaveGenericTest<DockObject>(objectName, assertFunc);
		}

		[TestCase("HS1.DAT")]
		public void HillShapesObject(string objectName)
		{
			void assertFunc(ILocoObject obj, HillShapesObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.HillHeightMapCount, Is.EqualTo(2), nameof(struc.HillHeightMapCount));
				Assert.That(struc.MountainHeightMapCount, Is.EqualTo(2), nameof(struc.MountainHeightMapCount));
				//Assert.That(struc.var_08, Is.EqualTo(0), nameof(struc.var_08));
				CollectionAssert.AreEqual(struc.pad_0C, Array.CreateInstance(typeof(byte), 2), nameof(struc.pad_0C));
			});
			LoadSaveGenericTest<HillShapesObject>(objectName, assertFunc);
		}

		[TestCase("BREWERY.DAT")]
		public void IndustryObject(string objectName)
		{
			void assertFunc(ILocoObject obj, IndustryObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0), nameof(struc.Name));

				// AnimationSequences
				Assert.That(struc.AvailableColours, Is.EqualTo(4), nameof(struc.AvailableColours));
				// Buildings
				Assert.That(struc.BuildingSizeFlags, Is.EqualTo(1), nameof(struc.BuildingSizeFlags));
				Assert.That(struc._BuildingWall, Is.EqualTo(0), nameof(struc._BuildingWall));
				Assert.That(struc._BuildingWallEntrance, Is.EqualTo(0), nameof(struc._BuildingWallEntrance));
				// BuildingVariationAnimations
				// BuildingVariationHeights
				// BuildingVariationParts
				Assert.That(struc.ClearCostFactor, Is.EqualTo(240), nameof(struc.ClearCostFactor));
				Assert.That(struc.CostFactor, Is.EqualTo(320), nameof(struc.CostFactor));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.DesignedYear, Is.EqualTo(0), nameof(struc.DesignedYear));
				Assert.That(struc.Flags, Is.EqualTo(IndustryObjectFlags.BuiltOnLowGround | IndustryObjectFlags.BuiltNearWater | IndustryObjectFlags.BuiltNearTown | IndustryObjectFlags.CanBeFoundedByPlayer | IndustryObjectFlags.BuiltNearDesert), nameof(struc.Flags));
				//Assert.That(struc.InitialProductionRate, Is.EqualTo(1), nameof(struc.InitialProductionRate));
				Assert.That(struc.MaxNumBuildings, Is.EqualTo(8), nameof(struc.MaxNumBuildings));
				Assert.That(struc.MinNumBuildings, Is.EqualTo(4), nameof(struc.MinNumBuildings));
				Assert.That(struc.NumBuildingAnimations, Is.EqualTo(4), nameof(struc.NumBuildingAnimations));
				Assert.That(struc.NumBuildingVariations, Is.EqualTo(2), nameof(struc.NumBuildingVariations));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(65535), nameof(struc.ObsoleteYear));
				Assert.That(struc.pad_E3, Is.EqualTo(4), nameof(struc.pad_E3));
				// ProducedCargo
				// RequiredCargo
				Assert.That(struc.ScaffoldingColour, Is.EqualTo(Colour.grey), nameof(struc.ScaffoldingColour));
				Assert.That(struc.ScaffoldingSegmentType, Is.EqualTo(0), nameof(struc.ScaffoldingSegmentType));
				Assert.That(struc.TotalOfTypeInScenario, Is.EqualTo(2), nameof(struc.TotalOfTypeInScenario));
				Assert.That(struc.var_E8, Is.EqualTo(1), nameof(struc.var_E8));
				Assert.That(struc.var_E9, Is.EqualTo(1), nameof(struc.var_E9));
				Assert.That(struc.var_EA, Is.EqualTo(0), nameof(struc.var_EA));
				Assert.That(struc.var_EB, Is.EqualTo(0), nameof(struc.var_EB));
				Assert.That(struc.var_EC, Is.EqualTo(0), nameof(struc.var_EC));
				Assert.That(struc.var_F3, Is.EqualTo(1), nameof(struc.var_F3));
				// WallTypes
			});
			LoadSaveGenericTest<IndustryObject>(objectName, assertFunc);
		}

		[TestCase("INTERDEF.DAT")]
		public void InterfaceSkinObject(string objectName)
		{
			void assertFunc(ILocoObject obj, InterfaceSkinObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0), nameof(struc.Name));
				Assert.That(struc.Image, Is.EqualTo(0), nameof(struc.Image));

				Assert.That(struc.Colour_06, Is.EqualTo(Colour.orange), nameof(struc.Colour_06));
				Assert.That(struc.Colour_07, Is.EqualTo(Colour.darkOrange), nameof(struc.Colour_07));
				Assert.That(struc.TooltipColour, Is.EqualTo(Colour.orange), nameof(struc.TooltipColour));
				Assert.That(struc.ErrorColour, Is.EqualTo(Colour.darkRed), nameof(struc.ErrorColour));
				Assert.That(struc.Colour_0A, Is.EqualTo(Colour.grey), nameof(struc.Colour_0A));
				Assert.That(struc.Colour_0B, Is.EqualTo(Colour.black), nameof(struc.Colour_0B));
				Assert.That(struc.Colour_0C, Is.EqualTo(Colour.grey), nameof(struc.Colour_0C));
				Assert.That(struc.Colour_0D, Is.EqualTo(Colour.brown), nameof(struc.Colour_0D));
				Assert.That(struc.Colour_0E, Is.EqualTo(Colour.brown), nameof(struc.Colour_0E));
				Assert.That(struc.Colour_0F, Is.EqualTo(Colour.mutedSeaGreen), nameof(struc.Colour_0F));
				Assert.That(struc.Colour_10, Is.EqualTo(Colour.blue), nameof(struc.Colour_10));
				Assert.That(struc.Colour_11, Is.EqualTo(Colour.black), nameof(struc.Colour_11));
				Assert.That(struc.Colour_12, Is.EqualTo(Colour.blue), nameof(struc.Colour_12));
				Assert.That(struc.Colour_13, Is.EqualTo(Colour.mutedSeaGreen), nameof(struc.Colour_13));
				Assert.That(struc.Colour_14, Is.EqualTo(Colour.brown), nameof(struc.Colour_14));
				Assert.That(struc.Colour_15, Is.EqualTo(Colour.grey), nameof(struc.Colour_15));
				Assert.That(struc.Colour_16, Is.EqualTo(Colour.grey), nameof(struc.Colour_16));
				Assert.That(struc.Colour_17, Is.EqualTo(Colour.grey), nameof(struc.Colour_17));
			});
			LoadSaveGenericTest<InterfaceSkinObject>(objectName, assertFunc);
		}

		[TestCase("GRASS1.DAT")]
		public void LandObject(string objectName)
		{
			void assertFunc(ILocoObject obj, LandObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.CostIndex, Is.EqualTo(2), nameof(struc.CostIndex));
				Assert.That(struc.var_03, Is.EqualTo(5), nameof(struc.var_03));
				Assert.That(struc.var_04, Is.EqualTo(1), nameof(struc.var_04));
				Assert.That(struc.Flags, Is.EqualTo(LandObjectFlags.unk0), nameof(struc.Flags));
				Assert.That(struc.CliffEdgeHeader1, Is.EqualTo(0), nameof(struc.CliffEdgeHeader1));
				Assert.That(struc.CliffEdgeHeader2, Is.EqualTo(0), nameof(struc.CliffEdgeHeader2));
				Assert.That(struc.CostFactor, Is.EqualTo(20), nameof(struc.CostFactor));
				Assert.That(struc.pad_09, Is.EqualTo(0), nameof(struc.pad_09));
				Assert.That(struc.var_0E, Is.EqualTo(0), nameof(struc.var_0E));
				Assert.That(struc.CliffEdgeImage, Is.EqualTo(0), nameof(struc.CliffEdgeImage));
				Assert.That(struc.MapPixelImage, Is.EqualTo(0), nameof(struc.MapPixelImage));
				Assert.That(struc.pad_1A, Is.EqualTo(0), nameof(struc.pad_1A));
				Assert.That(struc.NumVariations, Is.EqualTo(3), nameof(struc.NumVariations));
				Assert.That(struc.VariationLikelihood, Is.EqualTo(10), nameof(struc.VariationLikelihood));
				Assert.That(struc.pad_1D, Is.EqualTo(0), nameof(struc.pad_1D));
			});
			LoadSaveGenericTest<LandObject>(objectName, assertFunc);
		}

		[TestCase("LCROSS1.DAT")]
		public void LevelCrossingObject(string objectName)
		{
			void assertFunc(ILocoObject obj, LevelCrossingObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.CostFactor, Is.EqualTo(30), nameof(struc.CostFactor));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-10), nameof(struc.SellCostFactor));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));

				Assert.That(struc.AnimationSpeed, Is.EqualTo(3), nameof(struc.AnimationSpeed));
				Assert.That(struc.ClosingFrames, Is.EqualTo(4), nameof(struc.ClosingFrames));
				Assert.That(struc.ClosedFrames, Is.EqualTo(11), nameof(struc.ClosedFrames));

				Assert.That(struc.pad_0A[0], Is.EqualTo(3), nameof(struc.pad_0A) + "[0]");
				Assert.That(struc.pad_0A[1], Is.EqualTo(0), nameof(struc.pad_0A) + "[1]");

				Assert.That(struc.DesignedYear, Is.EqualTo(1955), nameof(struc.DesignedYear));
			});
			LoadSaveGenericTest<LevelCrossingObject>(objectName, assertFunc);
		}

		[TestCase("REGUK.DAT")]
		public void RegionObject(string objectName)
		{
			void assertFunc(ILocoObject obj, RegionObject struc) => Assert.Multiple(() =>
			{
				CollectionAssert.AreEqual(struc.pad_06, Array.CreateInstance(typeof(byte), 2), nameof(struc.pad_06));
				Assert.That(struc.RequiredObjectCount, Is.EqualTo(1), nameof(struc.RequiredObjectCount));
				//CollectionAssert.AreEqual(struc.requiredObjects, Array.CreateInstance(typeof(byte), 4), nameof(struc.requiredObjects));
				CollectionAssert.AreEqual(struc.pad_0D, Array.CreateInstance(typeof(byte), 5), nameof(struc.pad_0D));
			});
			LoadSaveGenericTest<RegionObject>(objectName, assertFunc);
		}

		[TestCase("ROADONE.DAT")]
		public void RoadObject(string objectName)
		{
			void assertFunc(ILocoObject obj, RoadObject struc) => Assert.Multiple(() =>
			{
				// Bridges
				Assert.That(struc.BuildCostFactor, Is.EqualTo(22), nameof(struc.BuildCostFactor));
				// Compatible
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.Flags, Is.EqualTo(RoadObjectFlags.unk_00 | RoadObjectFlags.unk_02 | RoadObjectFlags.unk_03 | RoadObjectFlags.unk_04 | RoadObjectFlags.IsRoad), nameof(struc.Flags));
				Assert.That(struc.MaxSpeed, Is.EqualTo(400), nameof(struc.MaxSpeed));
				// Mods
				Assert.That(struc.NumBridges, Is.EqualTo(5), nameof(struc.NumBridges));
				Assert.That(struc.NumCompatible, Is.EqualTo(1), nameof(struc.NumCompatible));
				Assert.That(struc.NumMods, Is.EqualTo(0), nameof(struc.NumMods));
				Assert.That(struc.PaintStyle, Is.EqualTo(0), nameof(struc.PaintStyle));
				Assert.That(struc.RoadPieces, Is.EqualTo(RoadObjectPieceFlags.OneWay | RoadObjectPieceFlags.Track | RoadObjectPieceFlags.Slope | RoadObjectPieceFlags.SteepSlope | RoadObjectPieceFlags.Intersection | RoadObjectPieceFlags.Overtake), nameof(struc.RoadPieces));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-20), nameof(struc.SellCostFactor));
				// Stations
				Assert.That(struc.TargetTownSize, Is.EqualTo(2), nameof(struc.TargetTownSize));
				Assert.That(struc.TunnelCostFactor, Is.EqualTo(27), nameof(struc.TunnelCostFactor));
			});
			LoadSaveGenericTest<RoadObject>(objectName, assertFunc);
		}

		[TestCase("RDEXCAT1.DAT")]
		public void RoadExtraObject(string objectName)
		{
			void assertFunc(ILocoObject obj, RoadExtraObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.BuildCostFactor, Is.EqualTo(4), nameof(struc.BuildCostFactor));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.PaintStyle, Is.EqualTo(1), nameof(struc.PaintStyle));
				Assert.That(struc.RoadPieces, Is.EqualTo(127), nameof(struc.RoadPieces));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-3), nameof(struc.SellCostFactor));
				Assert.That(struc.var_0E, Is.EqualTo(0), nameof(struc.var_0E));
			});
			LoadSaveGenericTest<RoadExtraObject>(objectName, assertFunc);
		}

		[TestCase("RDSTAT1.DAT")]
		public void RoadStationObject(string objectName)
		{
			void assertFunc(ILocoObject obj, RoadStationObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.BuildCostFactor, Is.EqualTo(24), nameof(struc.BuildCostFactor));
				// Cargo
				Assert.That(struc._CargoOffsetBytes, Is.All.EqualTo(0), nameof(struc._CargoOffsetBytes));
				// Compatible
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.DesignedYear, Is.EqualTo(0), nameof(struc.DesignedYear));
				Assert.That(struc.Flags, Is.EqualTo(RoadStationObjectFlags.Passenger | RoadStationObjectFlags.RoadEnd), nameof(struc.Flags));
				Assert.That(struc.NumCompatible, Is.EqualTo(0), nameof(struc.NumCompatible));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(1945), nameof(struc.ObsoleteYear));
				Assert.That(struc.PaintStyle, Is.EqualTo(0), nameof(struc.PaintStyle));
				Assert.That(struc.RoadPieces, Is.EqualTo(0), nameof(struc.RoadPieces));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-17), nameof(struc.SellCostFactor));
			});
			LoadSaveGenericTest<RoadStationObject>(objectName, assertFunc);
		}

		[TestCase("SCAFDEF.DAT")]
		public void ScaffoldingObject(string objectName)
		{
			void assertFunc(ILocoObject obj, ScaffoldingObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.SegmentHeights[0], Is.EqualTo(16), nameof(struc.SegmentHeights) + "[0]");
				Assert.That(struc.SegmentHeights[1], Is.EqualTo(16), nameof(struc.SegmentHeights) + "[1]");
				Assert.That(struc.SegmentHeights[2], Is.EqualTo(32), nameof(struc.SegmentHeights) + "[2]");

				Assert.That(struc.RoofHeights[0], Is.EqualTo(0), nameof(struc.RoofHeights) + "[0]");
				Assert.That(struc.RoofHeights[1], Is.EqualTo(0), nameof(struc.RoofHeights) + "[1]");
				Assert.That(struc.RoofHeights[2], Is.EqualTo(14), nameof(struc.RoofHeights) + "[2]");
			});
			LoadSaveGenericTest<ScaffoldingObject>(objectName, assertFunc);
		}

		[TestCase("STEX000.DAT")]
		public void ScenarioTextObject(string objectName)
		{
			void assertFunc(ILocoObject obj, ScenarioTextObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.pad_04, Is.EqualTo(0), nameof(struc.pad_04));
			});
			LoadSaveGenericTest<ScenarioTextObject>(objectName, assertFunc);
		}

		[TestCase("SNOW.DAT")]
		public void SnowObject(string objectName)
		{
			void assertFunc(ILocoObject obj, SnowObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0));
				Assert.That(struc.Image, Is.EqualTo(0));

				Assert.That(obj.StringTable.Table, Has.Count.EqualTo(1), nameof(obj.StringTable.Table));
				Assert.That(obj.G1Elements, Has.Count.EqualTo(139), nameof(obj.G1Elements));
			});
			LoadSaveGenericTest<SnowObject>(objectName, assertFunc);
		}

		[TestCase("SNDA1.DAT")]
		public void SoundObject(string objectName)
		{
			void assertFunc(ILocoObject obj, SoundObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0), nameof(struc.Name));
				Assert.That(struc.pad_07, Is.EqualTo(0), nameof(struc.pad_07));
				Assert.That(struc.var_06, Is.EqualTo(1), nameof(struc.var_06));
				Assert.That(struc.Volume, Is.EqualTo(0), nameof(struc.Volume));

				Assert.That(struc.PcmData, Has.Length.EqualTo(119666), nameof(struc.PcmData.Length));

				Assert.That(struc.SoundObjectData.Length, Is.EqualTo(119662), nameof(struc.SoundObjectData.Length));
				Assert.That(struc.SoundObjectData.Offset, Is.EqualTo(8), nameof(struc.SoundObjectData.Offset));
				Assert.That(struc.SoundObjectData.var_00, Is.EqualTo(1), nameof(struc.SoundObjectData.var_00));

				Assert.That(struc.SoundObjectData.PcmHeader.AverageBytesPerSecond, Is.EqualTo(44100), nameof(struc.SoundObjectData.PcmHeader.AverageBytesPerSecond));
				Assert.That(struc.SoundObjectData.PcmHeader.BitsPerSample, Is.EqualTo(4096), nameof(struc.SoundObjectData.PcmHeader.BitsPerSample));
				Assert.That(struc.SoundObjectData.PcmHeader.BlockAlign, Is.EqualTo(512), nameof(struc.SoundObjectData.PcmHeader.BlockAlign));
				Assert.That(struc.SoundObjectData.PcmHeader.CBSize, Is.EqualTo(0), nameof(struc.SoundObjectData.PcmHeader.CBSize));
				Assert.That(struc.SoundObjectData.PcmHeader.NumberOfChannels, Is.EqualTo(1), nameof(struc.SoundObjectData.PcmHeader.NumberOfChannels));
				Assert.That(struc.SoundObjectData.PcmHeader.SampleRate, Is.EqualTo(22050), nameof(struc.SoundObjectData.PcmHeader.SampleRate));
				Assert.That(struc.SoundObjectData.PcmHeader.WaveFormatTag, Is.EqualTo(1), nameof(struc.SoundObjectData.PcmHeader.WaveFormatTag));
			});
			LoadSaveGenericTest<SoundObject>(objectName, assertFunc);
		}

		[TestCase("STEAM.DAT")]
		public void SteamObject(string objectName)
		{
			void assertFunc(ILocoObject obj, SteamObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0), nameof(struc.Name));

				Assert.That(struc.Flags, Is.EqualTo(SteamObjectFlags.ApplyWind | SteamObjectFlags.DisperseOnCollision | SteamObjectFlags.unk2), nameof(struc.Flags));
				// FrameInfoType0 contents
				// FrameInfoType1 contents
				Assert.That(struc.FrameInfoType0, Has.Count.EqualTo(47), nameof(struc.FrameInfoType0));
				Assert.That(struc.FrameInfoType1, Has.Count.EqualTo(30), nameof(struc.FrameInfoType1));
				Assert.That(struc.NumImages, Is.EqualTo(57), nameof(struc.NumImages));
				Assert.That(struc.NumSoundEffects, Is.EqualTo(8), nameof(struc.NumSoundEffects));
				Assert.That(struc.NumStationaryTicks, Is.EqualTo(2), nameof(struc.NumStationaryTicks));

				// these aren't currently calculated in this tool
				//Assert.That(struc.SpriteWidth, Is.EqualTo(0), nameof(struc.SpriteWidth));
				//Assert.That(struc.SpriteHeightNegative, Is.EqualTo(0), nameof(struc.SpriteHeightNegative));
				//Assert.That(struc.SpriteHeightPositive, Is.EqualTo(0), nameof(struc.SpriteHeightPositive));

				Assert.That(struc._TotalNumFramesType0, Is.EqualTo(0), nameof(struc._TotalNumFramesType0));
				Assert.That(struc._TotalNumFramesType1, Is.EqualTo(0), nameof(struc._TotalNumFramesType1));
				Assert.That(struc.var_0A, Is.EqualTo(0), nameof(struc.var_0A));
				// SoundEffects
			});
			LoadSaveGenericTest<SteamObject>(objectName, assertFunc);
		}

		[TestCase("SLIGHT1.DAT")]
		public void StreetLightObject(string objectName)
		{
			void assertFunc(ILocoObject obj, StreetLightObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0));

				Assert.That(struc.DesignedYear[0], Is.EqualTo(1900), nameof(struc.DesignedYear) + "[0]");
				Assert.That(struc.DesignedYear[1], Is.EqualTo(1950), nameof(struc.DesignedYear) + "[1]");
				Assert.That(struc.DesignedYear[2], Is.EqualTo(1985), nameof(struc.DesignedYear) + "[2]");

				Assert.That(obj.StringTable["Name"][LanguageId.english_uk], Is.EqualTo("Street Lights"));
				Assert.That(obj.StringTable["Name"][LanguageId.english_us], Is.EqualTo("Street Lights"));
			});
			LoadSaveGenericTest<StreetLightObject>(objectName, assertFunc);
		}

		[TestCase("ATOWNNAM.DAT")]
		public void TownNamesObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TownNamesObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.UnknownTownNameStructs[0].Count, Is.EqualTo(2), nameof(struc.UnknownTownNameStructs) + "[0] Count");
				Assert.That(struc.UnknownTownNameStructs[0].Fill, Is.EqualTo(30), nameof(struc.UnknownTownNameStructs) + "[0] Fill");
				Assert.That(struc.UnknownTownNameStructs[0].Offset, Is.EqualTo(93), nameof(struc.UnknownTownNameStructs) + "[0] Offset");

				Assert.That(struc.UnknownTownNameStructs[1].Count, Is.EqualTo(94), nameof(struc.UnknownTownNameStructs) + "[1] Count");
				Assert.That(struc.UnknownTownNameStructs[1].Fill, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[1] Fill");
				Assert.That(struc.UnknownTownNameStructs[1].Offset, Is.EqualTo(110), nameof(struc.UnknownTownNameStructs) + "[1] Offset");

				Assert.That(struc.UnknownTownNameStructs[2].Count, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[2] Count");
				Assert.That(struc.UnknownTownNameStructs[2].Fill, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[2] Fill");
				Assert.That(struc.UnknownTownNameStructs[2].Offset, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[2] Offset");

				Assert.That(struc.UnknownTownNameStructs[3].Count, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[3] Count");
				Assert.That(struc.UnknownTownNameStructs[3].Fill, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[3] Fill");
				Assert.That(struc.UnknownTownNameStructs[3].Offset, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[3] Offset");

				Assert.That(struc.UnknownTownNameStructs[4].Count, Is.EqualTo(18), nameof(struc.UnknownTownNameStructs) + "[4] Count");
				Assert.That(struc.UnknownTownNameStructs[4].Fill, Is.EqualTo(0), nameof(struc.UnknownTownNameStructs) + "[4] Fill");
				Assert.That(struc.UnknownTownNameStructs[4].Offset, Is.EqualTo(923), nameof(struc.UnknownTownNameStructs) + "[4] Offset");

				Assert.That(struc.UnknownTownNameStructs[5].Count, Is.EqualTo(6), nameof(struc.UnknownTownNameStructs) + "[5] Count");
				Assert.That(struc.UnknownTownNameStructs[5].Fill, Is.EqualTo(20), nameof(struc.UnknownTownNameStructs) + "[5] Fill");
				Assert.That(struc.UnknownTownNameStructs[5].Offset, Is.EqualTo(1071), nameof(struc.UnknownTownNameStructs) + "[5] Offset");

				Assert.That(obj.StringTable["Name"][LanguageId.english_uk], Is.EqualTo("North-American style town names"));
				Assert.That(obj.StringTable["Name"][LanguageId.english_us], Is.EqualTo("North-American style town names"));
			});
			LoadSaveGenericTest<TownNamesObject>(objectName, assertFunc);
		}

		[TestCase("TRACKST.DAT")]
		public void TrackObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TrackObject struc) => Assert.Multiple(() =>
			{
				// Bridges
				Assert.That(struc.BuildCostFactor, Is.EqualTo(11), nameof(struc.BuildCostFactor));
				// Compatible
				Assert.That(struc._CompatibleRoads, Is.EqualTo(0), nameof(struc._CompatibleRoads));
				Assert.That(struc._CompatibleTracks, Is.EqualTo(0), nameof(struc._CompatibleTracks));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.CurveSpeed, Is.EqualTo(400), nameof(struc.CurveSpeed));
				Assert.That(struc.DisplayOffset, Is.EqualTo(3), nameof(struc.DisplayOffset));
				Assert.That(struc.Flags, Is.EqualTo(TrackObjectFlags.unk_00), nameof(struc.Flags));
				// Mods
				Assert.That(struc.NumBridges, Is.EqualTo(5), nameof(struc.NumBridges));
				Assert.That(struc.NumCompatible, Is.EqualTo(7), nameof(struc.NumCompatible));
				Assert.That(struc.NumMods, Is.EqualTo(2), nameof(struc.NumMods));
				Assert.That(struc.NumSignals, Is.EqualTo(10), nameof(struc.NumSignals));
				Assert.That(struc.NumStations, Is.EqualTo(5), nameof(struc.NumStations));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-10), nameof(struc.SellCostFactor));
				// Signals
				// Stations
				Assert.That(struc.StationTrackPieces, Is.EqualTo(525), nameof(struc.StationTrackPieces));
				Assert.That(struc.TrackPieces, Is.EqualTo(TrackObjectPieceFlags.Diagonal | TrackObjectPieceFlags.LargeCurve | TrackObjectPieceFlags.NormalCurve | TrackObjectPieceFlags.SmallCurve | TrackObjectPieceFlags.Slope | TrackObjectPieceFlags.SlopedCurve | TrackObjectPieceFlags.SBend | TrackObjectPieceFlags.Junction), nameof(struc.TrackPieces));
				Assert.That(struc.TunnelCostFactor, Is.EqualTo(24), nameof(struc.TunnelCostFactor));
				Assert.That(struc.var_06, Is.EqualTo(0), nameof(struc.var_06));

				Assert.That(obj.StringTable.Table, Has.Count.EqualTo(1), nameof(obj.StringTable.Table));
				Assert.That(obj.G1Elements, Is.Not.Null);
				Assert.That(obj.G1Elements, Has.Count.EqualTo(400), nameof(obj.G1Elements));
			});
			LoadSaveGenericTest<TrackObject>(objectName, assertFunc);
		}

		[TestCase("TREXCAT1.DAT")]
		public void TrackExtraObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TrackExtraObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.TrackPieces, Is.EqualTo(1023), nameof(struc.TrackPieces));
				Assert.That(struc.PaintStyle, Is.EqualTo(1), nameof(struc.PaintStyle));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.BuildCostFactor, Is.EqualTo(2), nameof(struc.BuildCostFactor));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-1), nameof(struc.SellCostFactor));
				Assert.That(struc.var_0E, Is.EqualTo(0), nameof(struc.var_0E));
			});
			LoadSaveGenericTest<TrackExtraObject>(objectName, assertFunc);
		}

		[TestCase("SIGSUS.DAT")]
		public void TrainSignalObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TrainSignalObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0));
				Assert.That(struc.Image, Is.EqualTo(0));

				Assert.That(struc.AnimationSpeed, Is.EqualTo(1), nameof(struc.AnimationSpeed));
				Assert.That(struc.CostFactor, Is.EqualTo(4), nameof(struc.CostFactor));
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.DesignedYear, Is.EqualTo(0), nameof(struc.DesignedYear));
				Assert.That(struc.Flags, Is.EqualTo(TrainSignalObjectFlags.IsLeft), nameof(struc.Flags));
				// Mods
				Assert.That(struc.NumCompatible, Is.EqualTo(0), nameof(struc.NumCompatible));
				Assert.That(struc.NumFrames, Is.EqualTo(7), nameof(struc.NumFrames));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(1955), nameof(struc.ObsoleteYear));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-3), nameof(struc.SellCostFactor));
				Assert.That(struc.var_0B, Is.EqualTo(0), nameof(struc.var_0B));
			});
			LoadSaveGenericTest<TrainSignalObject>(objectName, assertFunc);
		}

		[TestCase("TRSTAT1.DAT")]
		public void TrainStationObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TrainStationObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.BuildCostFactor, Is.EqualTo(7), nameof(struc.BuildCostFactor));
				// CargoOffsetBytes
				// Compatible
				Assert.That(struc.CostIndex, Is.EqualTo(1), nameof(struc.CostIndex));
				Assert.That(struc.DesignedYear, Is.EqualTo(1960), nameof(struc.DesignedYear));
				Assert.That(struc.Flags, Is.EqualTo(TrainStationObjectFlags.None), nameof(struc.Flags));
				// ManualPower
				Assert.That(struc.NumCompatible, Is.EqualTo(0), nameof(struc.NumCompatible));
				Assert.That(struc.ObsoleteYear, Is.EqualTo(65535), nameof(struc.ObsoleteYear));
				Assert.That(struc.PaintStyle, Is.EqualTo(0), nameof(struc.PaintStyle));
				Assert.That(struc.SellCostFactor, Is.EqualTo(-7), nameof(struc.SellCostFactor));
				Assert.That(struc.TrackPieces, Is.EqualTo(0), nameof(struc.TrackPieces));
				Assert.That(struc.var_03, Is.EqualTo(0), nameof(struc.var_03));
				Assert.That(struc.var_0B, Is.EqualTo(2), nameof(struc.var_0B));
				Assert.That(struc.var_0D, Is.EqualTo(0), nameof(struc.var_0D));
			});
			LoadSaveGenericTest<TrainStationObject>(objectName, assertFunc);
		}

		[TestCase("BEECH.DAT")]
		public void TreeObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TreeObject struc) => Assert.Multiple(() =>
			{
				//Assert.That(struc.var_02, Is.EqualTo(40), nameof(struc.var_02));
				Assert.That(struc.Height, Is.EqualTo(131), nameof(struc.Height));
				Assert.That(struc.var_04, Is.EqualTo(27), nameof(struc.var_04));
				Assert.That(struc.var_05, Is.EqualTo(83), nameof(struc.var_05));
				Assert.That(struc.NumRotations, Is.EqualTo(1), nameof(struc.NumRotations));
				Assert.That(struc.Growth, Is.EqualTo(4), nameof(struc.Growth));
				Assert.That(struc.Flags, Is.EqualTo(TreeObjectFlags.HighAltitude | TreeObjectFlags.RequiresWater | TreeObjectFlags.HasShadow), nameof(struc.Flags));
				CollectionAssert.AreEqual(struc.Sprites, Array.CreateInstance(typeof(byte), 6), nameof(struc.Sprites));
				CollectionAssert.AreEqual(struc.SnowSprites, Array.CreateInstance(typeof(byte), 6), nameof(struc.SnowSprites));
				Assert.That(struc.ShadowImageOffset, Is.EqualTo(0), nameof(struc.ShadowImageOffset));
				Assert.That(struc.var_3C, Is.EqualTo(15), nameof(struc.var_3C));
				Assert.That(struc.SeasonState, Is.EqualTo(3), nameof(struc.SeasonState));
				Assert.That(struc.var_3E, Is.EqualTo(2), nameof(struc.var_3E));
				Assert.That(struc.CostIndex, Is.EqualTo(3), nameof(struc.CostIndex));
				Assert.That(struc.BuildCostFactor, Is.EqualTo(8), nameof(struc.BuildCostFactor));
				Assert.That(struc.ClearCostFactor, Is.EqualTo(4), nameof(struc.ClearCostFactor));
				Assert.That(struc.Colours, Is.EqualTo(0), nameof(struc.Colours));
				Assert.That(struc.Rating, Is.EqualTo(10), nameof(struc.Rating));
				Assert.That(struc.DemolishRatingReduction, Is.EqualTo(-15), nameof(struc.DemolishRatingReduction));
			});
			LoadSaveGenericTest<TreeObject>(objectName, assertFunc);
		}

		[TestCase("TUNNEL1.DAT")]
		public void TunnelObject(string objectName)
		{
			void assertFunc(ILocoObject obj, TunnelObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.Name, Is.EqualTo(0));
				Assert.That(struc.Image, Is.EqualTo(0));
			});
			LoadSaveGenericTest<TunnelObject>(objectName, assertFunc);
		}

		[TestCase("707.DAT")]
		public void VehicleAircraftObject(string objectName)
		{
			void assertFunc(ILocoObject obj, VehicleObject struc) => Assert.Multiple(() =>
			{
				//var s5header = obj.S5Header;
				//Assert.That(s5header.Flags, Is.EqualTo(283680407), nameof(s5header.Flags));
				//Assert.That(s5header.Name, Is.EqualTo("707     "), nameof(s5header.Name));
				//Assert.That(s5header.Checksum, Is.EqualTo(1331114877), nameof(s5header.Checksum));
				//Assert.That(s5header.ObjectType, Is.EqualTo(ObjectType.Vehicle), nameof(s5header.ObjectType));

				//var objHeader = obj.ObjectHeader;
				//Assert.That(objHeader.Encoding, Is.EqualTo(SawyerEncoding.RunLengthSingle), nameof(objHeader.Encoding));
				//Assert.That(objHeader.DataLength, Is.EqualTo(159566), nameof(objHeader.DataLength));

				Assert.That(struc.Mode, Is.EqualTo(TransportMode.Air), nameof(struc.Mode));
				Assert.That(struc.Type, Is.EqualTo(VehicleType.Aircraft), nameof(struc.Type));
				Assert.That(struc.var_04, Is.EqualTo(1), nameof(struc.var_04));
				// Assert.That(struc.TrackType, Is.EqualTo(0xFF), nameof(struc.TrackType)); // is changed after load from 0 to 255
				Assert.That(struc.NumTrackExtras, Is.EqualTo(0), nameof(struc.NumTrackExtras));
				Assert.That(struc.CostIndex, Is.EqualTo(8), nameof(struc.CostIndex));
				Assert.That(struc.CostFactor, Is.EqualTo(345), nameof(struc.CostFactor));
				Assert.That(struc.Reliability, Is.EqualTo(88), nameof(struc.Reliability));
				Assert.That(struc.RunCostIndex, Is.EqualTo(4), nameof(struc.RunCostIndex));
				Assert.That(struc.RunCostFactor, Is.EqualTo(55), nameof(struc.RunCostFactor));
				Assert.That(struc.ColourType, Is.EqualTo(9), nameof(struc.ColourType));
				Assert.That(struc.NumCompatibleVehicles, Is.EqualTo(0), nameof(struc.NumCompatibleVehicles));
				//CollectionAssert.AreEqual(Enumerable.Repeat(0, 8).ToArray(), struc.CompatibleVehicles, nameof(struc.CompatibleVehicles));
				//CollectionAssert.AreEqual(Enumerable.Repeat(0, 4).ToArray(), struc.RequiredTrackExtras, nameof(struc.RequiredTrackExtras));
				//Assert.That(struc.var_24, Is.EqualTo(0), nameof(struc.var_24));
				//Assert.That(struc.BodySprites, Is.EqualTo(0), nameof(struc.BodySprites));
				//Assert.That(struc.BogieSprites, Is.EqualTo(1), nameof(struc.BogieSprites));
				Assert.That(struc.Power, Is.EqualTo(3000), nameof(struc.Power));
				Assert.That(struc.Speed, Is.EqualTo(604), nameof(struc.Speed));
				Assert.That(struc.RackSpeed, Is.EqualTo(120), nameof(struc.RackSpeed));
				Assert.That(struc.Weight, Is.EqualTo(141), nameof(struc.Weight));
				Assert.That(struc.Flags, Is.EqualTo((VehicleObjectFlags)16384), nameof(struc.Flags));
				// CollectionAssert.AreEqual(struc.MaxCargo, Enumerable.Repeat(0, 2).ToArray(), nameof(struc.MaxCargo)); // this is changed after load from 0 to 24
				//CollectionAssert.AreEqual(Enumerable.Repeat(0, 2).ToArray(), struc.CompatibleCargoCategories, nameof(struc.CompatibleCargoCategories));
				//CollectionAssert.AreEqual(Enumerable.Repeat(0, 32).ToArray(), struc.CargoTypeSpriteOffsets, nameof(struc.CargoTypeSpriteOffsets));
				Assert.That(struc.NumSimultaneousCargoTypes, Is.EqualTo(1), nameof(struc.NumSimultaneousCargoTypes));
				Assert.That(struc.Animation[0].ObjectId, Is.EqualTo(0), nameof(struc.Animation));
				Assert.That(struc.Animation[0].Height, Is.EqualTo(24), nameof(struc.Animation));
				Assert.That(struc.Animation[0].Type, Is.EqualTo(SimpleAnimationType.None), nameof(struc.Animation));
				Assert.That(struc.Animation[1].ObjectId, Is.EqualTo(0), nameof(struc.Animation));
				Assert.That(struc.Animation[1].Height, Is.EqualTo(0), nameof(struc.Animation));
				Assert.That(struc.Animation[1].Type, Is.EqualTo(SimpleAnimationType.None), nameof(struc.Animation));
				Assert.That(struc.var_113, Is.EqualTo(0), nameof(struc.var_113));
				Assert.That(struc.Designed, Is.EqualTo(1957), nameof(struc.Designed));
				Assert.That(struc.Obsolete, Is.EqualTo(1987), nameof(struc.Obsolete));
				Assert.That(struc.RackRailType, Is.EqualTo(0), nameof(struc.RackRailType));
				//Assert.That(struc.DrivingSoundType, Is.EqualTo(DrivingSoundType.Engine1), nameof(struc.DrivingSoundType));
				//Assert.That(struc.Sound, Is.EqualTo(0), nameof(struc.Sound));
				//Assert.That(struc.pad_135, Is.EqualTo(0), nameof(struc.pad_135));
				Assert.That(struc.NumStartSounds, Is.EqualTo(2), nameof(struc.NumStartSounds));

				Assert.That(struc.StartSounds[0].Name, Is.EqualTo("SNDTD1  "), nameof(struc.StartSounds) + "[0]Name");
				Assert.That(struc.StartSounds[0].Checksum, Is.EqualTo(0), nameof(struc.StartSounds) + "[0]Checksum");
				Assert.That(struc.StartSounds[0].Flags, Is.EqualTo(1), nameof(struc.StartSounds) + "[0]Flags");
				Assert.That(struc.StartSounds[0].SourceGame, Is.EqualTo(SourceGame.Custom), nameof(struc.StartSounds) + "[0]Checksum");
				Assert.That(struc.StartSounds[0].ObjectType, Is.EqualTo(ObjectType.Sound), nameof(struc.StartSounds) + "[0]Flags");

				Assert.That(struc.StartSounds[1].Name, Is.EqualTo("SNDTD2  "), nameof(struc.StartSounds) + "[1]Name");
				Assert.That(struc.StartSounds[1].Checksum, Is.EqualTo(0), nameof(struc.StartSounds) + "[1]Checksum");
				Assert.That(struc.StartSounds[1].Flags, Is.EqualTo(1), nameof(struc.StartSounds) + "[1]Flags");
				Assert.That(struc.StartSounds[1].SourceGame, Is.EqualTo(SourceGame.Custom), nameof(struc.StartSounds) + "[1]Checksum");
				Assert.That(struc.StartSounds[1].ObjectType, Is.EqualTo(ObjectType.Sound), nameof(struc.StartSounds) + "[1]Flags");
			});
			LoadSaveGenericTest<VehicleObject>(objectName, assertFunc);
		}

		[TestCase("FENCE1.DAT")]
		public void WallObject(string objectName)
		{
			void assertFunc(ILocoObject obj, WallObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.ToolId, Is.EqualTo(15), nameof(struc.ToolId));
				Assert.That(struc.Flags, Is.EqualTo(WallObjectFlags.None), nameof(struc.Flags));
				Assert.That(struc.Height, Is.EqualTo(2), nameof(struc.Height));
				Assert.That(struc.Flags2, Is.EqualTo(WallObjectFlags2.Opaque), nameof(struc.Flags2));
			});
			LoadSaveGenericTest<WallObject>(objectName, assertFunc);
		}

		[TestCase("WATER1.DAT")]
		public void WaterObject(string objectName)
		{
			void assertFunc(ILocoObject obj, WaterObject struc) => Assert.Multiple(() =>
			{
				Assert.That(struc.CostIndex, Is.EqualTo(2), nameof(struc.CostIndex));
				Assert.That(struc.var_03, Is.EqualTo(0), nameof(struc.var_03));
				Assert.That(struc.CostFactor, Is.EqualTo(51), nameof(struc.CostFactor));
				Assert.That(struc.var_05, Is.EqualTo(0), nameof(struc.var_05));
				//Assert.That(struc.var_0A, Is.EqualTo(0), nameof(struc.var_0A));
			});
			LoadSaveGenericTest<WaterObject>(objectName, assertFunc);
		}
	}
}
