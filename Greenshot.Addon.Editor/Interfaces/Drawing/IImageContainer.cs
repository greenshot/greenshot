using System.Drawing;

namespace Greenshot.Addon.Editor.Interfaces.Drawing
{
	public interface IImageContainer : IDrawableContainer
	{
		Image Image { get; set; }

		void Load(string filename);
	}
}