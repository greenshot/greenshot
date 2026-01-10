/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Helpers
{
    /// <summary>
    /// Offers a few helper functions for scaling/aligning an element with another element
    /// </summary>
    public static class ScaleHelper
    {
        /// <summary>
        /// calculates the Size an element must be resized to, in order to fit another element, keeping aspect ratio
        /// </summary>
        /// <param name="currentSize">the size of the element to be resized</param>
        /// <param name="targetSize">the target size of the element</param>
        /// <param name="crop">in case the aspect ratio of currentSize and targetSize differs: shall the scaled size fit into targetSize (i.e. that one of its dimensions is smaller - false) or vice versa (true)</param>
        /// <returns>NativeSizeFloat object indicating the width and height the element should be scaled to</returns>
        public static NativeSizeFloat GetScaledSize(NativeSizeFloat currentSize, NativeSizeFloat targetSize, bool crop)
        {
            float wFactor = targetSize.Width / currentSize.Width;
            float hFactor = targetSize.Height / currentSize.Height;

            float factor = crop ? Math.Max(wFactor, hFactor) : Math.Min(wFactor, hFactor);
            return new NativeSizeFloat(currentSize.Width * factor, currentSize.Height * factor);
        }

        /// <summary>
        /// calculates the position of an element depending on the desired alignment within a RectangleF
        /// </summary>
        /// <param name="currentRect">NativeRectFloat the bounds of the element to be aligned</param>
        /// <param name="targetRect">NativeRectFloat with the rectangle for alignment of the element</param>
        /// <param name="alignment">the System.Drawing.ContentAlignment value indicating how the element is to be aligned should the width or height differ from targetSize</param>
        /// <returns>NativeRectFloat object with Location aligned aligned to targetRect</returns>
        public static NativeRectFloat GetAlignedRectangle(NativeRectFloat currentRect, NativeRectFloat targetRect, ContentAlignment alignment)
        {
            var newRect = new NativeRectFloat(targetRect.Location, currentRect.Size);
            return alignment switch
            {
                // TODO: Can ContentAlignment be replaced with Positions?
                ContentAlignment.TopCenter => newRect.ChangeX((targetRect.Width - currentRect.Width) / 2),
                ContentAlignment.TopRight => newRect.ChangeX(targetRect.Width - currentRect.Width),
                ContentAlignment.MiddleLeft => newRect.ChangeY((targetRect.Height - currentRect.Height) / 2),
                ContentAlignment.MiddleCenter => newRect.ChangeY((targetRect.Height - currentRect.Height) / 2).ChangeX((targetRect.Width - currentRect.Width) / 2),
                ContentAlignment.MiddleRight => newRect.ChangeY((targetRect.Height - currentRect.Height) / 2).ChangeX(targetRect.Width - currentRect.Width),
                ContentAlignment.BottomLeft => newRect.ChangeY(targetRect.Height - currentRect.Height),
                ContentAlignment.BottomCenter => newRect.ChangeY(targetRect.Height - currentRect.Height).ChangeX((targetRect.Width - currentRect.Width) / 2),
                ContentAlignment.BottomRight => newRect.ChangeY(targetRect.Height - currentRect.Height).ChangeX(targetRect.Width - currentRect.Width),
                _ => newRect
            };
        }

        /// <summary>
        /// Calculates target size of a given rectangle scaled by dragging one of its handles (corners)
        /// </summary>
        /// <param name="originalRectangle">NativeRectFloat bounds of the current rectangle</param>
        /// <param name="resizeHandlePosition">Positions with the position of the handle/gripper being used for resized, see constants in Gripper.cs, e.g. Gripper.POSITION_TOP_LEFT</param>
        /// <param name="resizeHandleCoords">NativePointFloat coordinates of the used handle/gripper</param>
        /// <param name="options">ScaleOptions to use when scaling</param>
        /// <returns>NativeRectFloat scaled originalRectangle</returns>
        public static NativeRectFloat Scale(NativeRectFloat originalRectangle, Positions resizeHandlePosition, NativePointFloat resizeHandleCoords, ScaleOptions? options)
        {
            options ??= GetScaleOptions();

            if ((options & ScaleOptions.Rational) == ScaleOptions.Rational)
            {
                resizeHandleCoords = AdjustCoordsForRationalScale(originalRectangle, resizeHandlePosition, resizeHandleCoords);
            }

            if ((options & ScaleOptions.Centered) == ScaleOptions.Centered)
            {
                // store center coordinates of rectangle
                float rectCenterX = originalRectangle.Left + originalRectangle.Width / 2;
                float rectCenterY = originalRectangle.Top + originalRectangle.Height / 2;
                // scale rectangle using handle coordinates
                originalRectangle = Scale(originalRectangle, resizeHandlePosition, resizeHandleCoords);
                // mirror handle coordinates via rectangle center coordinates
                resizeHandleCoords = resizeHandleCoords.Offset(-2 * (resizeHandleCoords.X - rectCenterX), -2 * (resizeHandleCoords.Y - rectCenterY));
                // scale again with opposing handle and mirrored coordinates
                resizeHandlePosition = (Positions) ((((int) resizeHandlePosition) + 4) % 8);
                originalRectangle = Scale(originalRectangle, resizeHandlePosition, resizeHandleCoords);
            }
            else
            {
                originalRectangle = Scale(originalRectangle, resizeHandlePosition, resizeHandleCoords);
            }

            return originalRectangle;
        }

        /// <summary>
        /// Calculates target size of a given rectangle scaled by dragging one of its handles (corners)
        /// </summary>
        /// <param name="originalRectangle">NativeRectFloat bounds of the current rectangle</param>
        /// <param name="resizeHandlePosition">Positions with the position of the handle/gripper being used for resized, see constants in Gripper.cs, e.g. Gripper.POSITION_TOP_LEFT</param>
        /// <param name="resizeHandleCoords">NativePointFloat with coordinates of the used handle/gripper</param>
        /// <returns>NativeRectFloat with the scaled originalRectangle</returns>
        private static NativeRectFloat Scale(NativeRectFloat originalRectangle, Positions resizeHandlePosition, NativePointFloat resizeHandleCoords)
        {
            return resizeHandlePosition switch
            {
                Positions.TopLeft => new NativeRectFloat(resizeHandleCoords.X, resizeHandleCoords.Y, originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X, originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y),
                Positions.TopCenter => new NativeRectFloat(originalRectangle.X, resizeHandleCoords.Y, originalRectangle.Width, originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y),
                Positions.TopRight => new NativeRectFloat(originalRectangle.X, resizeHandleCoords.Y, resizeHandleCoords.X - originalRectangle.Left, originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y),
                Positions.MiddleLeft => new NativeRectFloat(resizeHandleCoords.X, originalRectangle.Y, originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X, originalRectangle.Height),
                Positions.MiddleRight => new NativeRectFloat(originalRectangle.X, originalRectangle.Y, resizeHandleCoords.X - originalRectangle.Left, originalRectangle.Height), Positions.BottomLeft => new NativeRectFloat(resizeHandleCoords.X, originalRectangle.Y, originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X, resizeHandleCoords.Y - originalRectangle.Top),
                Positions.BottomCenter => new NativeRectFloat(originalRectangle.X, originalRectangle.Y, originalRectangle.Width, resizeHandleCoords.Y - originalRectangle.Top), Positions.BottomRight => new NativeRectFloat(originalRectangle.X, originalRectangle.Y, resizeHandleCoords.X - originalRectangle.Left, resizeHandleCoords.Y - originalRectangle.Top),
                _ => throw new ArgumentException("Position cannot be handled: " + resizeHandlePosition)
            };
        }

        /// <summary>
        /// Adjusts resizeHandleCoords so that aspect ratio is kept after resizing a given rectangle with provided arguments.
        /// An adjustment can always be done in two ways, e.g. *in*crease width until fit or *de*crease height until fit.
        /// To avoid objects growing near infinity unexpectedly in certain combinations, the adjustment will choose the
        /// option resulting in the smaller rectangle.
        /// </summary>
        /// <param name="originalRectangle">NativeRectFloat with the bounds of the current rectangle</param>
        /// <param name="resizeHandlePosition">Positions with the position of the handle/gripper being used for resized, see Position</param>
        /// <param name="resizeHandleCoords">NativePointFloat with coordinates of the used handle/gripper</param>
        /// <returns>NativePointFloat with the adjusted coordinates</returns>
        private static NativePointFloat AdjustCoordsForRationalScale(NativeRectFloat originalRectangle, Positions resizeHandlePosition, NativePointFloat resizeHandleCoords)
        {
            NativeSizeFloat selectedRectangle, newSize;

            switch (resizeHandlePosition)
            {
                case Positions.TopLeft:
                    selectedRectangle = new NativeSizeFloat(originalRectangle.Right - resizeHandleCoords.X, originalRectangle.Bottom - resizeHandleCoords.Y);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Right - newSize.Width, originalRectangle.Bottom - newSize.Height);
                    break;

                case Positions.TopRight:
                    selectedRectangle = new NativeSizeFloat(resizeHandleCoords.X - originalRectangle.Left, originalRectangle.Bottom - resizeHandleCoords.Y);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Left + newSize.Width, originalRectangle.Bottom - newSize.Height);
                    break;

                case Positions.BottomLeft:
                    selectedRectangle = new NativeSizeFloat(originalRectangle.Right - resizeHandleCoords.X, resizeHandleCoords.Y - originalRectangle.Top);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Right - newSize.Width, originalRectangle.Top + newSize.Height);
                    break;

                case Positions.BottomRight:
                    selectedRectangle = new NativeSizeFloat(resizeHandleCoords.X - originalRectangle.Left, resizeHandleCoords.Y - originalRectangle.Top);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Left + newSize.Width, originalRectangle.Top + newSize.Height);
                    break;
            }

            return resizeHandleCoords;
        }

        /// <summary>
        /// For an original size, and a selected size, returns the the largest possible size that
        /// * has the same aspect ratio as the original
        /// * fits into selected size
        /// </summary>
        /// <param name="originalSize">NativeSizeFloat to be considered for keeping aspect ratio</param>
        /// <param name="selectedSize">NativeSizeFloat selection size (i.e. the size we'd produce if we wouldn't keep aspect ratio)</param>
        /// <returns>NativeSizeFloat</returns>
        private static NativeSizeFloat GetNewSizeForRationalScale(NativeSizeFloat originalSize, NativeSizeFloat selectedSize)
        {
            NativeSizeFloat newSize = selectedSize;
            float originalRatio = originalSize.Width / originalSize.Height;
            float selectedRatio = selectedSize.Width / selectedSize.Height;
            // will fix orientation if the scaling causes size to be flipped in any direction
            int flippedRatioSign = Math.Sign(selectedRatio) * Math.Sign(originalRatio);
            if (Math.Abs(selectedRatio) > Math.Abs(originalRatio))
            {
                // scaled rectangle (ratio) would be wider than original
                // keep height and tweak width to maintain aspect ratio
                newSize = newSize.ChangeWidth(selectedSize.Height * originalRatio * flippedRatioSign);
            }
            else if (Math.Abs(selectedRatio) < Math.Abs(originalRatio))
            {
                // scaled rectangle (ratio) would be taller than original
                // keep width and tweak height to maintain aspect ratio
                newSize = newSize.ChangeHeight(selectedSize.Width / originalRatio * flippedRatioSign);
            }

            return newSize;
        }

        /// <summary>
        /// Scale the boundsBeforeResize with the specified position and new location, using the angle angleRoundBehavior
        /// </summary>
        /// <param name="boundsBeforeResize">NativeRect</param>
        /// <param name="cursorX">int</param>
        /// <param name="cursorY">int</param>
        /// <param name="angleRoundBehavior">IDoubleProcessor</param>
        /// <param name="scaleOptions">ScaleOptions</param>
        /// <returns>NativeRectFloat</returns>
        public static NativeRectFloat Scale(NativeRect boundsBeforeResize, int cursorX, int cursorY, IDoubleProcessor angleRoundBehavior, ScaleOptions? scaleOptions = null)
        {
            scaleOptions ??= GetScaleOptions();

            NativeRectFloat result = boundsBeforeResize;
            bool rationalScale = (scaleOptions & ScaleOptions.Rational) == ScaleOptions.Rational;
            bool centeredScale = (scaleOptions & ScaleOptions.Centered) == ScaleOptions.Centered;

            if (rationalScale)
            {
                double angle = GeometryHelper.Angle2D(boundsBeforeResize.X, boundsBeforeResize.Y, cursorX, cursorY);

                if (angleRoundBehavior != null)
                {
                    angle = angleRoundBehavior.Process(angle);
                }

                int dist = GeometryHelper.Distance2D(boundsBeforeResize.X, boundsBeforeResize.Y, cursorX, cursorY);

                result = result
                    .ChangeWidth((int)Math.Round(dist * Math.Cos(angle / 180 * Math.PI)))
                    .ChangeHeight((int) Math.Round(dist * Math.Sin(angle / 180 * Math.PI)));
            }

            if (centeredScale)
            {
                float wdiff = result.Width - result.Width;
                float hdiff = result.Height - result.Height;
                result = result.Inflate(wdiff, hdiff);
            }

            return result;
        }

        /// <returns>the current ScaleOptions depending on modifier keys held down</returns>
        public static ScaleOptions GetScaleOptions()
        {
            bool anchorAtCenter = (Control.ModifierKeys & Keys.Control) != 0;
            bool maintainAspectRatio = (Control.ModifierKeys & Keys.Shift) != 0;
            ScaleOptions opts = ScaleOptions.Default;
            if (anchorAtCenter) opts |= ScaleOptions.Centered;
            if (maintainAspectRatio) opts |= ScaleOptions.Rational;
            return opts;
        }
    }
}