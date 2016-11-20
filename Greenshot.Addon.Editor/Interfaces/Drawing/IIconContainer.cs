using System.Drawing;

namespace Greenshot.Addon.Interfaces.Drawing
{
	public interface IIconContainer : IDrawableContainer
	{
		Icon Icon { get; set; }

		void Load(string filename);
	}
}