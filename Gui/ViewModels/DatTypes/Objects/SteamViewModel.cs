using OpenLoco.Dat.Objects;
using PropertyModels.ComponentModel.DataAnnotations;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel;
using System.Linq;

namespace OpenLoco.Gui.ViewModels
{
	public class SteamViewModel : LocoObjectViewModel<SteamObject>
	{
		[Reactive] public uint8_t NumStationaryTicks { get; set; }
		[Reactive, EnumProhibitValues<SteamObjectFlags>(SteamObjectFlags.None)] public SteamObjectFlags Flags { get; set; }
		[Reactive] public uint32_t var_0A { get; set; }
		[Reactive] public BindingList<S5HeaderViewModel> SoundEffects { get; set; }
		[Reactive] public BindingList<ImageAndHeight> FrameInfoType0 { get; set; } // may need viewmodel for ImageAndHeight
		[Reactive] public BindingList<ImageAndHeight> FrameInfoType1 { get; set; }

		public SteamViewModel(SteamObject so)
		{
			NumStationaryTicks = so.NumStationaryTicks;
			Flags = so.Flags;
			var_0A = so.var_0A;
			SoundEffects = new(so.SoundEffects.ConvertAll(x => new S5HeaderViewModel(x)));
			FrameInfoType0 = new(so.FrameInfoType0);
			FrameInfoType1 = new(so.FrameInfoType1);
		}

		public override SteamObject GetAsStruct(SteamObject so)
			=> so with
			{
				NumStationaryTicks = NumStationaryTicks,
				Flags = Flags,
				var_0A = var_0A,
				NumSoundEffects = (uint8_t)SoundEffects.Count,
				SoundEffects = SoundEffects.ToList().ConvertAll(x => x.GetAsUnderlyingType()),
				_TotalNumFramesType0 = (uint8_t)FrameInfoType0.Count,
				FrameInfoType0 = [.. FrameInfoType0],
				_TotalNumFramesType1 = (uint8_t)FrameInfoType1.Count,
				FrameInfoType1 = [.. FrameInfoType1],
			};
	}
}
