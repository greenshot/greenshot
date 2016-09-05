using System.Drawing;
using System.Drawing.Imaging;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Wrap an image, make it resizeable
	/// </summary>
	public class ImageWrapper : IImage
	{
		// Underlying image, is used to generate a resized version of it when needed
		private readonly Image _image;
		private Image _imageClone;

		/// <summary>
		/// Factory method
		/// </summary>
		/// <param name="image">Image</param>
		/// <returns>IImage</returns>
		public static IImage FromImage(Image image)
		{
			return image == null ? null : new ImageWrapper(image);
		}

		public ImageWrapper(Image image)
		{
			// Make sure the orientation is set correctly so Greenshot can process the image correctly
			ImageHelper.Orientate(image);
			_image = image;
			Width = _image.Width;
			Height = _image.Height;
		}

		public void Dispose()
		{
			_image.Dispose();
			_imageClone?.Dispose();
		}

		/// <summary>
		/// Height of the image, can be set to change
		/// </summary>
		public int Height { get; set; }

		/// <summary>
		/// Width of the image, can be set to change.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		/// Size of the image
		/// </summary>
		public Size Size => new Size(Width, Height);

		/// <summary>
		/// Pixelformat of the underlying image
		/// </summary>
		public PixelFormat PixelFormat => Image.PixelFormat;

		public float HorizontalResolution => Image.HorizontalResolution;
		public float VerticalResolution => Image.VerticalResolution;

		public Image Image
		{
			get
			{
				if (_imageClone == null)
				{
					if (_image.Height == Height && _image.Width == Width)
					{
						return _image;
					}
				}
				if (_imageClone?.Height == Height && _imageClone?.Width == Width)
				{
					return _imageClone;
				}
				// Calculate new image clone
				_imageClone?.Dispose();
				_imageClone = ImageHelper.ResizeImage(_image, false, Width, Height, null);
				return _imageClone;
			}
		}

	}
}
