﻿namespace Core.Types.SCV5
{
	[Flags]
	enum ScenarioFlags : uint16_t
	{
		None = (byte)0U,
		LandscapeGenerationDone = (byte)(1U << 0),
		HillsEdgeOfMap = (byte)(1U << 1),
	}
}
