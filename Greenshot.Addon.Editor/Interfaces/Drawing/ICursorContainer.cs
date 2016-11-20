using System.Windows.Forms;

namespace Greenshot.Addon.Interfaces.Drawing
{
	public interface ICursorContainer : IDrawableContainer
	{
		Cursor Cursor { get; set; }

		void Load(string filename);
	}
}