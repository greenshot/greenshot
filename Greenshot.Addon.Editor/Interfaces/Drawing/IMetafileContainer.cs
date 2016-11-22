using System.Drawing.Imaging;

namespace Greenshot.Addon.Editor.Interfaces.Drawing
{
	public interface IMetafileContainer : IDrawableContainer
	{
		Metafile Metafile { get; set; }

		void Load(string filename);
	}
}