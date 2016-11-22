using System.Windows.Forms;

namespace Greenshot.Addon.Editor.Interfaces.Drawing
{
	public interface ICursorContainer : IDrawableContainer
	{
		Cursor Cursor { get; set; }

		void Load(string filename);
	}
}