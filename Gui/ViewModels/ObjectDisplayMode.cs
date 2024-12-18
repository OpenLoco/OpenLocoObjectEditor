using System;

namespace OpenLoco.Gui.ViewModels
{
	[Flags]
	public enum ObjectDisplayMode
	{
		None = 0 << 0,
		Vanilla = 1 << 0,
		Custom = 1 << 1,
		OpenLoco = 1 << 2
	}
}
