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
        [Flags]
        public enum ScaleOptions
        {
            /// <summary>
            /// Default scale behavior.
            /// </summary>
            Default = 0x00,

            /// <summary>
            /// Scale a rectangle in two our four directions, mirrored at it's center coordinates
            /// </summary>
            Centered = 0x01,

            /// <summary>
            /// Scale a rectangle maintaining it's aspect ratio
            /// </summary>
            Rational = 0x02
        }

        /// <summary>
        /// calculates the Size an element must be resized to, in order to fit another element, keeping aspect ratio
        /// </summary>
        /// <param name="currentSize">the size of the element to be resized</param>
        /// <param name="targetSize">the target size of the element</param>
        /// <param name="crop">in case the aspect ratio of currentSize and targetSize differs: shall the scaled size fit into targetSize (i.e. that one of its dimensions is smaller - false) or vice versa (true)</param>
        /// <returns>a new SizeF object indicating the width and height the element should be scaled to</returns>
        public static SizeF GetScaledSize(SizeF currentSize, SizeF targetSize, bool crop)
        {
            float wFactor = targetSize.Width / currentSize.Width;
            float hFactor = targetSize.Height / currentSize.Height;

            float factor = crop ? Math.Max(wFactor, hFactor) : Math.Min(wFactor, hFactor);
            return new SizeF(currentSize.Width * factor, currentSize.Height * factor);
        }

        /// <summary>
        /// calculates the position of an element depending on the desired alignment within a RectangleF
        /// </summary>
        /// <param name="currentRect">the bounds of the element to be aligned</param>
        /// <param name="targetRect">the rectangle reference for alignment of the element</param>
        /// <param name="alignment">the System.Drawing.ContentAlignment value indicating how the element is to be aligned should the width or height differ from targetSize</param>
        /// <returns>a new RectangleF object with Location aligned aligned to targetRect</returns>
        public static RectangleF GetAlignedRectangle(RectangleF currentRect, RectangleF targetRect, ContentAlignment alignment)
        {
            RectangleF newRect = new RectangleF(targetRect.Location, currentRect.Size);
            switch (alignment)
            {
                case ContentAlignment.TopCenter:
                    newRect.X = (targetRect.Width - currentRect.Width) / 2;
                    break;
                case ContentAlignment.TopRight:
                    newRect.X = targetRect.Width - currentRect.Width;
                    break;
                case ContentAlignment.MiddleLeft:
                    newRect.Y = (targetRect.Height - currentRect.Height) / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    newRect.Y = (targetRect.Height - currentRect.Height) / 2;
                    newRect.X = (targetRect.Width - currentRect.Width) / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    newRect.Y = (targetRect.Height - currentRect.Height) / 2;
                    newRect.X = targetRect.Width - currentRect.Width;
                    break;
                case ContentAlignment.BottomLeft:
                    newRect.Y = targetRect.Height - currentRect.Height;
                    break;
                case ContentAlignment.BottomCenter:
                    newRect.Y = targetRect.Height - currentRect.Height;
                    newRect.X = (targetRect.Width - currentRect.Width) / 2;
                    break;
                case ContentAlignment.BottomRight:
                    newRect.Y = targetRect.Height - currentRect.Height;
                    newRect.X = targetRect.Width - currentRect.Width;
                    break;
            }

            return newRect;
        }

        /// <summary>
        /// Calculates target size of a given rectangle scaled by dragging one of its handles (corners)
        /// </summary>
        /// <param name="originalRectangle">bounds of the current rectangle, scaled values will be written to this reference</param>
        /// <param name="resizeHandlePosition">position of the handle/gripper being used for resized, see constants in Gripper.cs, e.g. Gripper.POSITION_TOP_LEFT</param>
        /// <param name="resizeHandleCoords">coordinates of the used handle/gripper</param>
        /// <param name="options">ScaleOptions to use when scaling</param>
        public static void Scale(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, NativePointFloat resizeHandleCoords, ScaleOptions? options)
        {
            options ??= GetScaleOptions();

            if ((options & ScaleOptions.Rational) == ScaleOptions.Rational)
            {
                AdjustCoordsForRationalScale(originalRectangle, resizeHandlePosition, ref resizeHandleCoords);
            }

            if ((options & ScaleOptions.Centered) == ScaleOptions.Centered)
            {
                // store center coordinates of rectangle
                float rectCenterX = originalRectangle.Left + originalRectangle.Width / 2;
                float rectCenterY = originalRectangle.Top + originalRectangle.Height / 2;
                // scale rectangle using handle coordinates
                Scale(ref originalRectangle, resizeHandlePosition, resizeHandleCoords);
                // mirror handle coordinates via rectangle center coordinates
                resizeHandleCoords = resizeHandleCoords.Offset(-2 * (resizeHandleCoords.X - rectCenterX), -2 * (resizeHandleCoords.Y - rectCenterY));
                // scale again with opposing handle and mirrored coordinates
                resizeHandlePosition = (Positions) ((((int) resizeHandlePosition) + 4) % 8);
                Scale(ref originalRectangle, resizeHandlePosition, resizeHandleCoords);
            }
            else
            {
                Scale(ref originalRectangle, resizeHandlePosition, resizeHandleCoords);
            }
        }

        /// <summary>
        /// Calculates target size of a given rectangle scaled by dragging one of its handles (corners)
        /// </summary>
        /// <param name="originalRectangle">bounds of the current rectangle, scaled values will be written to this reference</param>
        /// <param name="resizeHandlePosition">position of the handle/gripper being used for resized, see constants in Gripper.cs, e.g. Gripper.POSITION_TOP_LEFT</param>
        /// <param name="resizeHandleCoords">coordinates of the used handle/gripper</param>
        private static void Scale(ref NativeRectFloat originalRectangle, Positions resizeHandlePosition, NativePointFloat resizeHandleCoords)
        {
            switch (resizeHandlePosition)
            {
                case Positions.TopLeft:
                    originalRectangle = new NativeRectFloat(
                        resizeHandleCoords.X,
                        resizeHandleCoords.Y,
                        originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X,
                        originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y);
                    break;

                case Positions.TopCenter:
                    originalRectangle = new NativeRectFloat(
                        originalRectangle.X,
                        resizeHandleCoords.Y,
                        originalRectangle.Width,
                        originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y);
                    break;

                case Positions.TopRight:
                    originalRectangle = new NativeRectFloat(
                        originalRectangle.X,
                        resizeHandleCoords.Y,
                        resizeHandleCoords.X - originalRectangle.Left,
                        originalRectangle.Top + originalRectangle.Height - resizeHandleCoords.Y);
                    break;

                case Positions.MiddleLeft:
                    originalRectangle = new NativeRectFloat(
                        resizeHandleCoords.X,
                        originalRectangle.Y,
                        originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X,
                        originalRectangle.Height);
                    break;

                case Positions.MiddleRight:
                    originalRectangle = new NativeRectFloat(
                        originalRectangle.X,
                        originalRectangle.Y,
                        resizeHandleCoords.X - originalRectangle.Left,
                        originalRectangle.Height);
                    break;

                case Positions.BottomLeft:
                    originalRectangle = new NativeRectFloat(
                        resizeHandleCoords.X,
                        originalRectangle.Y,
                        originalRectangle.Left + originalRectangle.Width - resizeHandleCoords.X,
                        resizeHandleCoords.Y - originalRectangle.Top);
                    break;

                case Positions.BottomCenter:
                    originalRectangle = new NativeRectFloat(
                        originalRectangle.X,
                        originalRectangle.Y, 
                        originalRectangle.Width,
                        resizeHandleCoords.Y - originalRectangle.Top);
                    break;

                case Positions.BottomRight:
                    originalRectangle = new NativeRectFloat(
                        originalRectangle.X,
                        originalRectangle.Y,
                        resizeHandleCoords.X - originalRectangle.Left,
                        resizeHandleCoords.Y - originalRectangle.Top);
                    break;

                default:
                    throw new ArgumentException("Position cannot be handled: " + resizeHandlePosition);
            }
        }

        /// <summary>
        /// Adjusts resizeHandleCoords so that aspect ratio is kept after resizing a given rectangle with provided arguments.
        /// An adjustment can always be done in two ways, e.g. *in*crease width until fit or *de*crease height until fit.
        /// To avoid objects growing near infinity unexpectedly in certain combinations, the adjustment will choose the
        /// option resulting in the smaller rectangle.
        /// </summary>
        /// <param name="originalRectangle">bounds of the current rectangle</param>
        /// <param name="resizeHandlePosition">position of the handle/gripper being used for resized, see Position</param>
        /// <param name="resizeHandleCoords">coordinates of the used handle/gripper, adjusted coordinates will be written to this reference</param>
        private static void AdjustCoordsForRationalScale(NativeRectFloat originalRectangle, Positions resizeHandlePosition, ref NativePointFloat resizeHandleCoords)
        {
            SizeF selectedRectangle, newSize;

            switch (resizeHandlePosition)
            {
                case Positions.TopLeft:
                    selectedRectangle = new SizeF(originalRectangle.Right - resizeHandleCoords.X, originalRectangle.Bottom - resizeHandleCoords.Y);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Right - newSize.Width, originalRectangle.Bottom - newSize.Height);
                    break;

                case Positions.TopRight:
                    selectedRectangle = new SizeF(resizeHandleCoords.X - originalRectangle.Left, originalRectangle.Bottom - resizeHandleCoords.Y);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Left + newSize.Width, originalRectangle.Bottom - newSize.Height);
                    break;

                case Positions.BottomLeft:
                    selectedRectangle = new SizeF(originalRectangle.Right - resizeHandleCoords.X, resizeHandleCoords.Y - originalRectangle.Top);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Right - newSize.Width, originalRectangle.Top + newSize.Height);
                    break;

                case Positions.BottomRight:
                    selectedRectangle = new SizeF(resizeHandleCoords.X - originalRectangle.Left, resizeHandleCoords.Y - originalRectangle.Top);
                    newSize = GetNewSizeForRationalScale(originalRectangle.Size, selectedRectangle);
                    resizeHandleCoords = new NativePointFloat(originalRectangle.Left + newSize.Width, originalRectangle.Top + newSize.Height);
                    break;
            }
        }

        /// <summary>
        /// For an original size, and a selected size, returns the the largest possible size that
        /// * has the same aspect ratio as the original
        /// * fits into selected size
        /// </summary>
        /// <param name="originalSize">NativeSizeFloat to be considered for keeping aspect ratio</param>
        /// <param name="selectedSize">NativeSizeFloat selection size (i.e. the size we'd produce if we wouldn't keep aspect ratio)</param>
        /// <returns></returns>
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
                newSize = newSize.ChangeWidth(selectedSize.Width / originalRatio * flippedRatioSign);
            }

            return newSize;
        }

        public static void Scale(NativeRect boundsBeforeResize, int cursorX, int cursorY, ref NativeRectFloat boundsAfterResize)
        {
            Scale(boundsBeforeResize, cursorX, cursorY, ref boundsAfterResize, null);
        }

        public static void Scale(NativeRect boundsBeforeResize, int cursorX, int cursorY, ref NativeRectFloat boundsAfterResize, IDoubleProcessor angleRoundBehavior)
        {
            Scale(boundsBeforeResize, Positions.TopLeft, cursorX, cursorY, ref boundsAfterResize, angleRoundBehavior);
        }

        public static void Scale(NativeRect boundsBeforeResize, Positions gripperPosition, int cursorX, int cursorY, ref NativeRectFloat boundsAfterResize,
            IDoubleProcessor angleRoundBehavior)
        {
            ScaleOptions opts = GetScaleOptions();

            bool rationalScale = (opts & ScaleOptions.Rational) == ScaleOptions.Rational;
            bool centeredScale = (opts & ScaleOptions.Centered) == ScaleOptions.Centered;

            if (rationalScale)
            {
                double angle = GeometryHelper.Angle2D(boundsBeforeResize.X, boundsBeforeResize.Y, cursorX, cursorY);

                if (angleRoundBehavior != null)
                {
                    angle = angleRoundBehavior.Process(angle);
                }

                int dist = GeometryHelper.Distance2D(boundsBeforeResize.X, boundsBeforeResize.Y, cursorX, cursorY);

                boundsAfterResize = boundsAfterResize
                    .ChangeWidth((int)Math.Round(dist * Math.Cos(angle / 180 * Math.PI)))
                    .ChangeHeight((int) Math.Round(dist * Math.Sin(angle / 180 * Math.PI)));
            }

            if (centeredScale)
            {
                float wdiff = boundsAfterResize.Width - boundsBeforeResize.Width;
                float hdiff = boundsAfterResize.Height - boundsBeforeResize.Height;
                boundsAfterResize = boundsAfterResize.Inflate(wdiff, hdiff);
            }
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

        public interface IDoubleProcessor
        {
            double Process(double d);
        }

        public class ShapeAngleRoundBehavior : IDoubleProcessor
        {
            public static ShapeAngleRoundBehavior Instance = new();

            private ShapeAngleRoundBehavior()
            {
            }

            public double Process(double angle)
            {
                return Math.Round((angle + 45) / 90) * 90 - 45;
            }
        }

        public class LineAngleRoundBehavior : IDoubleProcessor
        {
            public static LineAngleRoundBehavior Instance = new();

            private LineAngleRoundBehavior()
            {
            }

            public double Process(double angle)
            {
                return Math.Round(angle / 15) * 15;
            }
        }
    }
}