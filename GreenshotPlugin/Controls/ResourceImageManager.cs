using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GreenshotPlugin.Gfx;

namespace GreenshotPlugin.Controls
{
	/// <summary>
	/// Wrap a ComponentResourceManager for images
	/// </summary>
	public class ResourceImageManager : IDisposable
	{
		private readonly System.ComponentModel.ComponentResourceManager _resources;
		private readonly IList<Bitmap> _images = new List<Bitmap>();
		public ResourceImageManager(Type resourceType)
		{
			_resources = new System.ComponentModel.ComponentResourceManager(resourceType);
		}

		/// <summary>
		/// Get icons for displaying
		/// </summary>
		/// <param name="imageName">string with the name</param>
		/// <returns>Bitmap</returns>
		public Bitmap GetIcon(string imageName)
		{
			var image = (Bitmap)_resources.GetObject(imageName);
			bool newImage;
			var result = (Bitmap)image.ScaleIconForDisplaying(out newImage);
			if (!newImage)
			{
				return image;
			}
			_images.Add(result);
			return result;
		}

		private void ReleaseUnmanagedResources()
		{
			foreach (var bitmap in _images.ToList())
			{
				_images.Remove(bitmap);
				bitmap.Dispose();
			}
		}

		public void Dispose()
		{
			ReleaseUnmanagedResources();
		}

		~ResourceImageManager()
		{
			ReleaseUnmanagedResources();
		}
	}
}
