using System.Drawing;

namespace Greenshot.Addon.Interfaces.Drawing
{
	public interface IImageContainer : IDrawableContainer
	{
		Image Image { get; set; }

		void Load(string filename);
	}
}