using System;

namespace Greenshot.Addon.Interfaces.Drawing
{
	/// <summary>
	///     Attribute for telling that a container has a certain meaning to the editor
	/// </summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Class)]
	public class FlagAttribute : Attribute
	{
		public FlagAttribute(ElementFlag flag)
		{
			Flag = flag;
		}

		/// <summary>
		///     Flag for the element
		/// </summary>
		public ElementFlag Flag { get; }
	}
}