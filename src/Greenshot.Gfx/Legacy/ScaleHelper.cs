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
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.Legacy
{
    /// <summary>
    ///     Offers a few helper functions for scaling/aligning an element with another element
    /// </summary>
    public static partial class ScaleHelper
	{

        /// <summary>
        ///     calculates the Size an element must be resized to, in order to fit another element, keeping aspect ratio
        /// </summary>
        /// <param name="currentSize">the size of the element to be resized</param>
        /// <param name="targetSize">the target size of the element</param>
        /// <param name="crop">
        ///     in case the aspect ratio of currentSize and targetSize differs: shall the scaled size fit into
        ///     targetSize (i.e. that one of its dimensions is smaller - false) or vice versa (true)
        /// </param>
        /// <returns>a new NativeSizeFloat object indicating the width and height the element should be scaled to</returns>
        public static NativeSizeFloat GetScaledSize(SizeF currentSize, SizeF targetSize, bool crop)
		{
			var wFactor = targetSize.Width / currentSize.Width;
			var hFactor = targetSize.Height / currentSize.Height;

			var factor = crop ? Math.Max(wFactor, hFactor) : Math.Min(wFactor, hFactor);
			return new NativeSizeFloat(currentSize.Width * factor, currentSize.Height * factor);
		}

		/// <summary>
		///     calculates the position of an element depending on the desired alignment within a NativeRectFloat
		/// </summary>
		/// <param name="currentRect">the bounds of the element to be aligned</param>
		/// <param name="targetRect">the rectangle reference for aligment of the element</param>
		/// <param name="alignment">
		///     the System.Drawing.ContentAlignment value indicating how the element is to be aligned should
		///     the width or height differ from targetSize
		/// </param>
		/// <returns>a new NativeRectFloat object with Location aligned aligned to targetRect</returns>
		public static NativeRectFloat GetAlignedRectangle(NativeRectFloat currentRect, NativeRectFloat targetRect, ContentAlignment alignment)
		{
		    var x = targetRect.Location.X;
		    var y = targetRect.Location.Y;

 			switch (alignment)
			{
				case ContentAlignment.TopCenter:
					x = (targetRect.Width - currentRect.Width) / 2;
					break;
				case ContentAlignment.TopRight:
					x = targetRect.Width - currentRect.Width;
					break;
				case ContentAlignment.MiddleLeft:
					y = (targetRect.Height - currentRect.Height) / 2;
					break;
				case ContentAlignment.MiddleCenter:
					y = (targetRect.Height - currentRect.Height) / 2;
					x = (targetRect.Width - currentRect.Width) / 2;
					break;
				case ContentAlignment.MiddleRight:
					y = (targetRect.Height - currentRect.Height) / 2;
					x = targetRect.Width - currentRect.Width;
					break;
				case ContentAlignment.BottomLeft:
					y = targetRect.Height - currentRect.Height;
					break;
				case ContentAlignment.BottomCenter:
					y = targetRect.Height - currentRect.Height;
					x = (targetRect.Width - currentRect.Width) / 2;
					break;
				case ContentAlignment.BottomRight:
					y = targetRect.Height - currentRect.Height;
					x = targetRect.Width - currentRect.Width;
					break;
			}
			return new NativeRectFloat(x,y, targetRect.Size);
		}

		/// <summary>
		///     calculates the Rectangle an element must be resized an positioned to, in ordder to fit another element, keeping
		///     aspect ratio
		/// </summary>
		/// <param name="currentRect">the rectangle of the element to be resized/repositioned</param>
		/// <param name="targetRect">the target size/position of the element</param>
		/// <param name="crop">
		///     in case the aspect ratio of currentSize and targetSize differs: shall the scaled size fit into
		///     targetSize (i.e. that one of its dimensions is smaller - false) or vice versa (true)
		/// </param>
		/// <param name="alignment">
		///     the System.Drawing.ContentAlignment value indicating how the element is to be aligned should
		///     the width or height differ from targetSize
		/// </param>
		/// <returns>
		///     a new NativeRectFloat object indicating the width and height the element should be scaled to and the position that
		///     should be applied to it for proper alignment
		/// </returns>
		public static NativeRectFloat GetScaledRectangle(NativeRectFloat currentRect, NativeRectFloat targetRect, bool crop, ContentAlignment alignment)
		{
			var newSize = GetScaledSize(currentRect.Size, targetRect.Size, crop);
			var newRect = new NativeRectFloat(new Point(0, 0), newSize);
			return GetAlignedRectangle(newRect, targetRect, alignment);
		}

		public static void RationalScale(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, PointF resizeHandleCoords)
		{
			Scale(ref originalRectangle, resizeHandlePosition, resizeHandleCoords, ScaleOptions.Rational);
		}

		public static void CenteredScale(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, PointF resizeHandleCoords)
		{
			Scale(ref originalRectangle, resizeHandlePosition, resizeHandleCoords, ScaleOptions.Centered);
		}

		public static void Scale(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, PointF resizeHandleCoords)
		{
			Scale(ref originalRectangle, resizeHandlePosition, resizeHandleCoords, null);
		}

		/// <summary>
		///     Calculates target size of a given rectangle scaled by dragging one of its handles (corners)
		/// </summary>
		/// <param name="originalRectangle">bounds of the current rectangle, scaled values will be written to this reference</param>
		/// <param name="resizeHandlePosition">
		///     position of the handle/gripper being used for resized, see constants in Gripper.cs,
		///     e.g. Gripper.POSITION_TOP_LEFT
		/// </param>
		/// <param name="resizeHandleCoords">coordinates of the used handle/gripper</param>
		/// <param name="options">ScaleOptions to use when scaling</param>
		public static void Scale(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, PointF resizeHandleCoords, ScaleOptions? options)
		{
			if (options == null)
			{
				options = GetScaleOptions();
			}

			if ((options & ScaleOptions.Rational) == ScaleOptions.Rational)
			{
				AdjustCoordsForRationalScale(originalRectangle, resizeHandlePosition, ref resizeHandleCoords);
			}

			if ((options & ScaleOptions.Centered) == ScaleOptions.Centered)
			{
				// store center coordinates of rectangle
				var rectCenterX = originalRectangle.Left + originalRectangle.Width / 2;
				var rectCenterY = originalRectangle.Top + originalRectangle.Height / 2;
				// scale rectangle using handle coordinates
				ScaleInternal(ref originalRectangle, resizeHandlePosition, resizeHandleCoords);
				// mirror handle coordinates via rectangle center coordinates
				resizeHandleCoords.X -= 2 * (resizeHandleCoords.X - rectCenterX);
				resizeHandleCoords.Y -= 2 * (resizeHandleCoords.Y - rectCenterY);
				// scale again with opposing handle and mirrored coordinates
				resizeHandlePosition = (Positions) (((int) resizeHandlePosition + 4) % 8);
				ScaleInternal(ref originalRectangle, resizeHandlePosition, resizeHandleCoords);
			}
			else
			{
				ScaleInternal(ref originalRectangle, resizeHandlePosition, resizeHandleCoords);
			}
		}

		/// <summary>
		///     Calculates target size of a given rectangle scaled by dragging one of its handles (corners)
		/// </summary>
		/// <param name="originalRectangle">bounds of the current rectangle, scaled values will be written to this reference</param>
		/// <param name="resizeHandlePosition">
		///     position of the handle/gripper being used for resized, see constants in Gripper.cs,
		///     e.g. Gripper.POSITION_TOP_LEFT
		/// </param>
		/// <param name="resizeHandleCoords">coordinates of the used handle/gripper</param>
		private static void ScaleInternal(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, PointF resizeHandleCoords)
		{
		    var x = originalRectangle.X;
		    var y = originalRectangle.Y;
		    var width = originalRectangle.Width;
		    var height = originalRectangle.Height;

            switch (resizeHandlePosition)
			{
				case Positions.TopLeft:
					width = originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X;
					height = originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y;
					x = resizeHandleCoords.X;
					y = resizeHandleCoords.Y;
					break;

				case Positions.TopCenter:
					height = originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y;
					y = resizeHandleCoords.Y;
					break;

				case Positions.TopRight:
					width = resizeHandleCoords.X - originalRectangle.Left;
					height = originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y;
					y = resizeHandleCoords.Y;
					break;

				case Positions.MiddleLeft:
					width = originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X;
					x = resizeHandleCoords.X;
					break;

				case Positions.MiddleRight:
					width = resizeHandleCoords.X - originalRectangle.Left;
					break;

				case Positions.BottomLeft:
					width = originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X;
					height = resizeHandleCoords.Y - originalRectangle.Top;
					x = resizeHandleCoords.X;
					break;

				case Positions.BottomCenter:
					height = resizeHandleCoords.Y - originalRectangle.Top;
					break;

				case Positions.BottomRight:
					width = resizeHandleCoords.X - originalRectangle.Left;
					height = resizeHandleCoords.Y - originalRectangle.Top;
					break;

				default:
					throw new ArgumentException("Position cannot be handled: " + resizeHandlePosition);
			}
            originalRectangle = new NativeRectFloat(x,y,width,height);
		}

		/// <summary>
		///     Adjusts resizeHandleCoords so that aspect ratio is kept after resizing a given rectangle with provided arguments
		/// </summary>
		/// <param name="originalRectangle">bounds of the current rectangle</param>
		/// <param name="resizeHandlePosition">position of the handle/gripper being used for resized, see Position</param>
		/// <param name="resizeHandleCoords">
		///     coordinates of the used handle/gripper, adjusted coordinates will be written to this
		///     reference
		/// </param>
		private static void AdjustCoordsForRationalScale(NativeRectFloat originalRectangle, Positions resizeHandlePosition, ref PointF resizeHandleCoords)
		{
			var originalRatio = originalRectangle.Width / originalRectangle.Height;
			float newWidth, newHeight, newRatio;
			switch (resizeHandlePosition)
			{
				case Positions.TopLeft:
					newWidth = originalRectangle.Right - resizeHandleCoords.X;
					newHeight = originalRectangle.Bottom - resizeHandleCoords.Y;
					newRatio = newWidth / newHeight;
					if (newRatio > originalRatio)
					{
						// FIXME
						resizeHandleCoords.X = originalRectangle.Right - newHeight * originalRatio;
					}
					else if (newRatio < originalRatio)
					{
						resizeHandleCoords.Y = originalRectangle.Bottom - newWidth / originalRatio;
					}
					break;

				case Positions.TopRight:
					newWidth = resizeHandleCoords.X - originalRectangle.Left;
					newHeight = originalRectangle.Bottom - resizeHandleCoords.Y;
					newRatio = newWidth / newHeight;
					if (newRatio > originalRatio)
					{
						// FIXME
						resizeHandleCoords.X = newHeight * originalRatio + originalRectangle.Left;
					}
					else if (newRatio < originalRatio)
					{
						resizeHandleCoords.Y = originalRectangle.Bottom - newWidth / originalRatio;
					}
					break;

				case Positions.BottomLeft:
					newWidth = originalRectangle.Right - resizeHandleCoords.X;
					newHeight = resizeHandleCoords.Y - originalRectangle.Top;
					newRatio = newWidth / newHeight;
					if (newRatio > originalRatio)
					{
						resizeHandleCoords.X = originalRectangle.Right - newHeight * originalRatio;
					}
					else if (newRatio < originalRatio)
					{
						resizeHandleCoords.Y = newWidth / originalRatio + originalRectangle.Top;
					}
					break;

				case Positions.BottomRight:
					newWidth = resizeHandleCoords.X - originalRectangle.Left;
					newHeight = resizeHandleCoords.Y - originalRectangle.Top;
					newRatio = newWidth / newHeight;
					if (newRatio > originalRatio)
					{
						resizeHandleCoords.X = newHeight * originalRatio + originalRectangle.Left;
					}
					else if (newRatio < originalRatio)
					{
						resizeHandleCoords.Y = newWidth / originalRatio + originalRectangle.Top;
					}
					break;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boundsBeforeResize"></param>
        /// <param name="cursorX"></param>
        /// <param name="cursorY"></param>
        /// <param name="boundsAfterResize"></param>
		public static void Scale(Rectangle boundsBeforeResize, int cursorX, int cursorY, ref NativeRectFloat boundsAfterResize)
		{
			Scale(boundsBeforeResize, cursorX, cursorY, ref boundsAfterResize, null);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boundsBeforeResize"></param>
        /// <param name="cursorX"></param>
        /// <param name="cursorY"></param>
        /// <param name="boundsAfterResize"></param>
        /// <param name="angleRoundBehavior"></param>
		public static void Scale(Rectangle boundsBeforeResize, int cursorX, int cursorY, ref NativeRectFloat boundsAfterResize, IDoubleProcessor angleRoundBehavior)
		{
			Scale(boundsBeforeResize, Positions.TopLeft, cursorX, cursorY, ref boundsAfterResize, angleRoundBehavior);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boundsBeforeResize"></param>
        /// <param name="gripperPosition"></param>
        /// <param name="cursorX"></param>
        /// <param name="cursorY"></param>
        /// <param name="boundsAfterResize"></param>
        /// <param name="angleRoundBehavior"></param>
		public static void Scale(Rectangle boundsBeforeResize, Positions gripperPosition, int cursorX, int cursorY, ref NativeRectFloat boundsAfterResize,
			IDoubleProcessor angleRoundBehavior)
		{
			var opts = GetScaleOptions();

			var rationalScale = (opts & ScaleOptions.Rational) == ScaleOptions.Rational;
			var centeredScale = (opts & ScaleOptions.Centered) == ScaleOptions.Centered;

			if (rationalScale)
			{
				var angle = GeometryHelper.Angle2D(boundsBeforeResize.X, boundsBeforeResize.Y, cursorX, cursorY);

				if (angleRoundBehavior != null)
				{
					angle = angleRoundBehavior.Process(angle);
				}

				var dist = GeometryHelper.Distance2D(boundsBeforeResize.X, boundsBeforeResize.Y, cursorX, cursorY);

			    boundsAfterResize = boundsAfterResize.Resize((float)Math.Round(dist * Math.Cos(angle / 180 * Math.PI)), (float)Math.Round(dist * Math.Sin(angle / 180 * Math.PI)));
			}

			if (centeredScale)
			{
				var wdiff = boundsAfterResize.Width - boundsBeforeResize.Width;
				var hdiff = boundsAfterResize.Height - boundsBeforeResize.Height;
                boundsAfterResize = boundsAfterResize.Offset(-wdiff, -hdiff).Resize(boundsAfterResize.Width + wdiff, boundsAfterResize.Height + hdiff);
			}
		}

		/// <returns>the current ScaleOptions depending on modifier keys held down</returns>
		public static ScaleOptions GetScaleOptions()
		{
			var anchorAtCenter = (Control.ModifierKeys & Keys.Control) != 0;
			var maintainAspectRatio = (Control.ModifierKeys & Keys.Shift) != 0;
			var opts = ScaleOptions.Default;
			if (anchorAtCenter)
			{
				opts |= ScaleOptions.Centered;
			}
			if (maintainAspectRatio)
			{
				opts |= ScaleOptions.Rational;
			}
			return opts;
		}


		/*public static int FindGripperPostition(float anchorX, float anchorY, float gripperX, float gripperY) {
			if(gripperY > anchorY) {
				if(gripperX > anchorY) return Gripper.POSITION_BOTTOM_RIGHT;
				else return Gripper.POSITION_BOTTOM_LEFT;
			} else {
				if(gripperX > anchorY) return Gripper.POSITION_TOP_RIGHT;
				else return Gripper.POSITION_TOP_LEFT;
			}
		}*/
	}
}