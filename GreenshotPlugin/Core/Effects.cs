/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using System.Windows.Forms;

using Greenshot.Plugin.Drawing;
using System.IO;
using System.Collections.Generic;
using GreenshotPlugin.Core;

namespace Greenshot.Core {
	/// <summary>
	/// Interface to describe an effect
	/// </summary>
	public interface IEffect {
		Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange);
	}

	/// <summary>
	/// DropShadowEffect
	/// </summary>
	public class DropShadowEffect : IEffect {
		public DropShadowEffect() {
			Darkness = 1f;
			ShadowSize = 9;
			ShadowOffset = new Point(-1, -1);
		}
		public float Darkness {
			get;
			set;
		}
		public int ShadowSize {
			get;
			set;
		}
		public Point ShadowOffset {
			get;
			set;
		}
		public virtual Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			return ImageHelper.CreateShadow(sourceBitmap, Darkness, ShadowSize, ShadowOffset, out offsetChange, PixelFormat.Format32bppArgb); //Image.PixelFormat);
		}
	}

	/// <summary>
	/// TornEdgeEffect extends on DropShadowEffect
	/// </summary>
	public class TornEdgeEffect : DropShadowEffect {
		public TornEdgeEffect() : base() {
			ShadowSize = 7;
			ToothHeight = 12;
			HorizontalToothRange = 20;
			VerticalToothRange = 20;
		}
		public int ToothHeight {
			get;
			set;
		}
		public int HorizontalToothRange {
			get;
			set;
		}
		public int VerticalToothRange {
			get;
			set;
		}
		public override Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			using (Bitmap tmpTornImage = ImageHelper.CreateTornEdge(sourceBitmap, ToothHeight, HorizontalToothRange, VerticalToothRange)) {
				return ImageHelper.CreateShadow(tmpTornImage, Darkness, ShadowSize, ShadowOffset, out offsetChange, PixelFormat.Format32bppArgb);
			}
		}
	}

	/// <summary>
	/// GrayscaleEffect
	/// </summary>
	public class GrayscaleEffect : IEffect {
		public Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.CreateGrayscale(sourceBitmap);
		}
	}

	/// <summary>
	/// InvertEffect
	/// </summary>
	public class InvertEffect : IEffect {
		public Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.CreateNegative(sourceBitmap);
		}
	}

	/// <summary>
	/// BorderEffect
	/// </summary>
	public class BorderEffect : IEffect {
		public BorderEffect() {
			Width = 2;
			Color = Color.Black;
		}
		public Color Color {
			get;
			set;
		}
		public int Width {
			get;
			set;
		}
		public Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			return ImageHelper.CreateBorder(sourceBitmap, Width, Color, sourceBitmap.PixelFormat, out offsetChange);
		}
	}

	/// <summary>
	/// RotateEffect
	/// </summary>
	public class RotateEffect : IEffect {
		public RotateEffect(int angle) {
			Angle = angle;
		}
		public int Angle {
			get;
			set;
		}
		public Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			offsetChange = Point.Empty;
			RotateFlipType flipType;
			if (Angle == 90) {
				flipType = RotateFlipType.Rotate90FlipNone;
			} else if (Angle == -90 || Angle == 270) {
				flipType = RotateFlipType.Rotate270FlipNone;
			} else {
				throw new NotSupportedException("Currently only an angle of 90 or -90 (270) is supported.");
			}
			return ImageHelper.RotateFlip(sourceBitmap, flipType);
		}
	}

	/// <summary>
	/// ResizeEffect
	/// </summary>
	public class ResizeEffect : IEffect {
		public ResizeEffect(int width, int height, bool maintainAspectRatio) {
			Width = width;
			Height = height;
			MaintainAspectRatio = maintainAspectRatio;
		}
		public int Width {
			get;
			set;
		}
		public int Height {
			get;
			set;
		}
		public bool MaintainAspectRatio {
			get;
			set;
		}
		public Bitmap Apply(Bitmap sourceBitmap, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.ResizeBitmap(sourceBitmap, MaintainAspectRatio, Width, Height);
		}
	}
}