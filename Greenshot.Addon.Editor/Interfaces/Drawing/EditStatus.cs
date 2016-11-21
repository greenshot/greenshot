using System;

namespace Greenshot.Addon.Editor.Interfaces.Drawing
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