// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization;
using Dapplo.Log;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of IconContainer.
	/// </summary>
	[Serializable]
	public class IconContainer : DrawableContainer, IIconContainer
	{
		private static readonly LogSource Log = new LogSource();

		protected Icon icon;

		public IconContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			Init();
		}

		public IconContainer(Surface parent, string filename, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			Load(filename);
		}

		public override bool HasDefaultSize => true;

		public override Size DefaultSize => icon.Size;

		public Icon Icon
		{
			set
			{
				icon?.Dispose();
				icon = (Icon) value.Clone();
				Width = value.Width;
				Height = value.Height;
			}
			get { return icon; }
		}

		public void Load(string filename)
		{
			if (!File.Exists(filename))
			{
				return;
			}
			using (var fileIcon = new Icon(filename))
			{
				Icon = fileIcon;
				Log.Debug().WriteLine("Loaded file: " + filename + " with resolution: " + Height + "," + Width);
			}
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

		/**
		 * This Dispose is called from the Dispose and the Destructor.
		 * When disposing==true all non-managed resources should be freed too!
		 */

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				icon?.Dispose();
			}
			icon = null;
			base.Dispose(disposing);
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			if (icon != null)
			{
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				graphics.CompositingQuality = CompositingQuality.Default;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawIcon(icon, Bounds);
			}
		}
	}
}