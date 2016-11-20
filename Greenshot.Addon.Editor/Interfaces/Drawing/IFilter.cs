using System.Drawing;

namespace Greenshot.Addon.Interfaces.Drawing
{
	/// <summary>
	///     The IFilter is an interface for all our filters like blur etc.
	/// </summary>
	public interface IFilter : IFieldHolder
	{
		bool Invert { get; set; }

		IDrawableContainer Parent { get; set; }

		void Apply(Graphics graphics, Bitmap bmp, Rectangle rect, RenderMode renderMode);
	}
}