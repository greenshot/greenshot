using System;

namespace Greenshot.Addon.Interfaces.Drawing
{
	[Serializable]
	public enum EditStatus
	{
		UNDRAWN,
		DRAWING,
		MOVING,
		RESIZING,
		IDLE
	}
}