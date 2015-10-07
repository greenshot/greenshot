/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;

namespace GreenshotImgurPlugin
{
	/// <summary>
	/// Description of ImageInfo.
	/// </summary>
	public class ImageInfo : IDisposable
	{
		public string Id
		{
			get;
			set;
		}

		public string DeleteHash
		{
			get;
			set;
		}

		public string Title
		{
			get;
			set;
		}

		public DateTimeOffset Timestamp
		{
			get;
			set;
		}

		public Uri Original
		{
			get;
			set;
		}

		public Uri Page
		{
			get;
			set;
		}

		public Uri SmallSquare
		{
			get;
			set;
		}

		private Image image;

		public Image Image
		{
			get
			{
				return image;
			}
			set
			{
				if (image != null)
				{
					image.Dispose();
				}
				image = value;
			}
		}

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (image != null)
				{
					image.Dispose();
				}
			}
			image = null;
		}
	}
}