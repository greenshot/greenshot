using System;

namespace Greenshot.Addon.Interfaces.Drawing
{
	[Flags]
	[Serializable]
	public enum ElementFlag
	{
		NONE = 0,
		CONFIRMABLE = 1,
		COUNTER = 2
	}
}