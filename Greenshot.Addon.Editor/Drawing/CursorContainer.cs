/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Greenshot.Addon.Interfaces.Drawing;
using Dapplo.Log.Facade;

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	/// Description of CursorContainer.
	/// </summary>
	[Serializable]
	public class CursorContainer : DrawableContainer, ICursorContainer
	{
		private static readonly LogSource Log = new LogSource();

		private Cursor _cursor;

		public CursorContainer(Surface parent) : base(parent)
		{
			Init();
		}

		protected override void OnDeserialized(StreamingContext streamingContext)
		{
			base.OnDeserialized(streamingContext);
			Init();
		}

		private void Init()
		{
			CreateDefaultAdorners();
		}

		public CursorContainer(Surface parent, string filename) : base(parent)
		{
			Load(filename);
		}

		public Cursor Cursor
		{
			set
			{
				if (_cursor != null)
				{
					_cursor.Dispose();
				}
				// Clone cursor (is this correct??)
				_cursor = new Cursor(value.CopyHandle());
				Width = value.Size.Width;
				Height = value.Size.Height;
			}
			get
			{
				return _cursor;
			}
		}

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_cursor != null)
				{
					_cursor.Dispose();
				}
			}
			_cursor = null;
			base.Dispose(disposing);
		}

		public void Load(string filename)
		{
			if (!File.Exists(filename))
			{
				return;
			}
			using (Cursor fileCursor = new Cursor(filename))
			{
				Cursor = fileCursor;
				Log.Debug().WriteLine("Loaded file: {0} with resolution: {1},{2}", filename, Height, Width);
			}
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			if (_cursor == null)
			{
				return;
			}
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.Default;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			_cursor.DrawStretched(graphics, Bounds);
		}

		public override Size DefaultSize
		{
			get
			{
				return _cursor.Size;
			}
		}
	}
}