using System;
using System.IO;
using Dapplo.Log;
using Greenshot.Addon.Interfaces;
using Greenshot.Core.Gfx;
using System.Drawing;
using Greenshot.Addon.Editor.Interfaces;

namespace Greenshot.Addon.Editor.Extensions
{
	public static class SurfaceExtensions
	{
		private static readonly LogSource Log = new LogSource();
		/// <summary>
		///     Load a Greenshot surface
		/// </summary>
		/// <param name="fullPath"></param>
		/// <param name="returnSurface"></param>
		/// <returns></returns>
		public static ISurface LoadGreenshotSurface(string fullPath, ISurface returnSurface)
		{
			if (string.IsNullOrEmpty(fullPath))
			{
				return null;
			}
			Image fileImage;
			Log.Info().WriteLine("Loading image from file {0}", fullPath);
			// Fixed lock problem Bug #3431881
			using (Stream surfaceFileStream = File.OpenRead(fullPath))
			{
				// And fixed problem that the bitmap stream is disposed... by Cloning the image
				// This also ensures the bitmap is correctly created

				// We create a copy of the bitmap, so everything else can be disposed
				surfaceFileStream.Position = 0;
				using (Image tmpImage = Image.FromStream(surfaceFileStream, true, true))
				{
					Log.Debug().WriteLine("Loaded {0} with Size {1}x{2} and PixelFormat {3}", fullPath, tmpImage.Width, tmpImage.Height, tmpImage.PixelFormat);
					fileImage = ImageHelper.Clone(tmpImage);
				}
				// Start at -14 read "GreenshotXX.YY" (XX=Major, YY=Minor)
				const int markerSize = 14;
				surfaceFileStream.Seek(-markerSize, SeekOrigin.End);
				string greenshotMarker;
				using (StreamReader streamReader = new StreamReader(surfaceFileStream))
				{
					greenshotMarker = streamReader.ReadToEnd();
					if (!greenshotMarker.StartsWith("Greenshot"))
					{
						throw new ArgumentException(string.Format("{0} is not a Greenshot file!", fullPath));
					}
					Log.Info().WriteLine("Greenshot file format: {0}", greenshotMarker);
					const int filesizeLocation = 8 + markerSize;
					surfaceFileStream.Seek(-filesizeLocation, SeekOrigin.End);
					using (BinaryReader reader = new BinaryReader(surfaceFileStream))
					{
						long bytesWritten = reader.ReadInt64();
						surfaceFileStream.Seek(-(bytesWritten + filesizeLocation), SeekOrigin.End);
						returnSurface.LoadElementsFromStream(surfaceFileStream);
					}
				}
			}
			if (fileImage != null)
			{
				returnSurface.Image = fileImage;
				Log.Info().WriteLine("Information about file {0}: {1}x{2}-{3} Resolution {4}x{5}", fullPath, fileImage.Width, fileImage.Height, fileImage.PixelFormat, fileImage.HorizontalResolution, fileImage.VerticalResolution);
			}
			return returnSurface;
		}
	}
}
