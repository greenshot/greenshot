/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Text;
using System.Runtime.Serialization;
using Greenshot.Addon.Editor.Helpers;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces.Drawing;

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	/// Description of SpeechbubbleContainer.
	/// </summary>
	[Serializable]
	public class SpeechbubbleContainer : TextContainer
	{
		private Point _initialGripperPoint;

		#region TargetGripper serializing code

		// Only used for serializing the TargetGripper location
		private Point _storedTargetGripperLocation;

		/// <summary>
		/// Store the current location of the target gripper
		/// </summary>
		/// <param name="context"></param>
		[OnSerializing]
		private void SetValuesOnSerializing(StreamingContext context)
		{
			if (TargetGripper != null)
			{
				_storedTargetGripperLocation = TargetGripper.Location;
			}
		}

		/// <summary>
		/// Restore the target gripper
		/// </summary>
		/// <param name="context"></param>
		[OnDeserialized]
		private void SetValuesOnDeserialized(StreamingContext context)
		{
			InitTargetGripper(Color.Green, _storedTargetGripperLocation);
		}

		#endregion

		public SpeechbubbleContainer(Surface parent) : base(parent)
		{
		}

		protected override void TargetGripperMove(int absX, int absY)
		{
			base.TargetGripperMove(absX, absY);
			Invalidate();
		}

		/// <summary>
		/// Called from Surface (the _parent) when the drawing begins (mouse-down)
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseDown(int mouseX, int mouseY)
		{
			if (TargetGripper == null)
			{
				_initialGripperPoint = new Point(mouseX, mouseY);
				InitTargetGripper(Color.Green, _initialGripperPoint);
			}
			return base.HandleMouseDown(mouseX, mouseY);
		}

		/// <summary>
		/// Overriding the HandleMouseMove will help us to make sure the tail is always visible.
		/// Should fix BUG-1682
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>base.HandleMouseMove</returns>
		public override bool HandleMouseMove(int x, int y)
		{
			var returnValue = base.HandleMouseMove(x, y);

			var leftAligned = _boundsAfterResize.Right - _boundsAfterResize.Left >= 0;
			var topAligned = _boundsAfterResize.Bottom - _boundsAfterResize.Top >= 0;

			var xOffset = leftAligned ? -20 : 20;
			var yOffset = topAligned ? -20 : 20;

			var newGripperLocation = _initialGripperPoint;
			newGripperLocation.Offset(xOffset, yOffset);

			if (TargetGripper.Location != newGripperLocation)
			{
				Invalidate();
				TargetGripperMove(newGripperLocation.X, newGripperLocation.Y);
				Invalidate();
			}
			return returnValue;
		}

		/// <summary>
		/// The DrawingBound should be so close as possible to the shape, so we don't invalidate to much.
		/// </summary>
		public override Rectangle DrawingBounds
		{
			get
			{
				if (Status != EditStatus.UNDRAWN)
				{
					using (var pen = new Pen(_lineColor, _lineThickness))
					{
						var inflateValue = _lineThickness + 2 + (_shadow ? 6 : 0);
						using (var tailPath = CreateTail())
						{
							return Rectangle.Inflate(Rectangle.Union(Rectangle.Round(tailPath.GetBounds(new Matrix(), pen)), new Rectangle(Left, Top, Width, Height).MakeGuiRectangle()), inflateValue, inflateValue);
						}
					}
				}
				return Rectangle.Empty;
			}
		}

		/// <summary>
		/// Helper method to create the bubble GraphicsPath, so we can also calculate the bounds
		/// </summary>
		/// <param name="lineThickness"></param>
		/// <returns></returns>
		private GraphicsPath CreateBubble(int lineThickness)
		{
			var bubble = new GraphicsPath();
			var rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();

			var bubbleRect = new Rectangle(0, 0, rect.Width, rect.Height).MakeGuiRectangle();
			// adapt corner radius to small rectangle dimensions
			var smallerSideLength = Math.Min(bubbleRect.Width, bubbleRect.Height);
			var cornerRadius = Math.Min(30, smallerSideLength/2 - lineThickness);
			if (cornerRadius > 0)
			{
				bubble.AddArc(bubbleRect.X, bubbleRect.Y, cornerRadius, cornerRadius, 180, 90);
				bubble.AddArc(bubbleRect.X + bubbleRect.Width - cornerRadius, bubbleRect.Y, cornerRadius, cornerRadius, 270, 90);
				bubble.AddArc(bubbleRect.X + bubbleRect.Width - cornerRadius, bubbleRect.Y + bubbleRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
				bubble.AddArc(bubbleRect.X, bubbleRect.Y + bubbleRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
			}
			else
			{
				bubble.AddRectangle(bubbleRect);
			}
			bubble.CloseAllFigures();
			using (var bubbleMatrix = new Matrix())
			{
				bubbleMatrix.Translate(rect.Left, rect.Top);
				bubble.Transform(bubbleMatrix);
			}
			return bubble;
		}

		/// <summary>
		/// Helper method to create the tail of the bubble, so we can also calculate the bounds
		/// </summary>
		/// <returns></returns>
		private GraphicsPath CreateTail()
		{
			var rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();

			var tailLength = GeometryHelper.Distance2D(rect.Left + (rect.Width/2), rect.Top + (rect.Height/2), TargetGripper.Left, TargetGripper.Top);
			var tailWidth = (Math.Abs(rect.Width) + Math.Abs(rect.Height))/20;

			// This should fix a problem with the tail being to wide
			tailWidth = Math.Min(Math.Abs(rect.Width)/2, tailWidth);
			tailWidth = Math.Min(Math.Abs(rect.Height)/2, tailWidth);

			var tail = new GraphicsPath();
			tail.AddLine(-tailWidth, 0, tailWidth, 0);
			tail.AddLine(tailWidth, 0, 0, -tailLength);
			tail.CloseFigure();

			var tailAngle = 90 + (int) GeometryHelper.Angle2D(rect.Left + (rect.Width/2), rect.Top + (rect.Height/2), TargetGripper.Left, TargetGripper.Top);

			using (var tailMatrix = new Matrix())
			{
				tailMatrix.Translate(rect.Left + (rect.Width/2), rect.Top + (rect.Height/2));
				tailMatrix.Rotate(tailAngle);
				tail.Transform(tailMatrix);
			}

			return tail;
		}

		/// <summary>
		/// This is to draw the actual container
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="renderMode"></param>
		public override void Draw(Graphics graphics, RenderMode renderMode)
		{
			if (TargetGripper == null)
			{
				return;
			}
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

			var lineVisible = (_lineThickness > 0 && ColorHelper.IsVisible(_lineColor));
			var rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();

			if (Selected && renderMode == RenderMode.EDIT)
			{
				DrawSelectionBorder(graphics, rect);
			}

			var bubble = CreateBubble(_lineThickness);

			var tail = CreateTail();

			//draw shadow first
			if (_shadow && (lineVisible || ColorHelper.IsVisible(_fillColor)))
			{
				const int basealpha = 100;
				var alpha = basealpha;
				const int steps = 5;
				var currentStep = lineVisible ? 1 : 0;
				using (var shadowMatrix = new Matrix())
				{
					using (var bubbleClone = (GraphicsPath) bubble.Clone())
					using (var bubbleClipRegion = new Region(bubbleClone))
					{
						using (var tailClone = (GraphicsPath) tail.Clone())
						using (var tailClipRegion = new Region(tailClone))
						{
							shadowMatrix.Translate(1, 1);
							while (currentStep <= steps)
							{
								using (var shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100)))
								{
									shadowPen.Width = lineVisible ? _lineThickness : 1;
									// make sure we can restore to a state before the exclude clip
									var stateBeforeClip = graphics.Save();
									// Set the bubble as the exclude clip region, so we can draw the tail shadow
									graphics.ExcludeClip(bubbleClipRegion);

									tailClone.Transform(shadowMatrix);
									graphics.DrawPath(shadowPen, tailClone);

									// Restore, so the clipping is gone
									graphics.Restore(stateBeforeClip);

									// make sure we can restore to a state before the exclude clip
									stateBeforeClip = graphics.Save();

									// Set the bubble as the exclude clip region, so we can draw the tail shadow
									graphics.ExcludeClip(tailClipRegion);
									bubbleClone.Transform(shadowMatrix);
									graphics.DrawPath(shadowPen, bubbleClone);

									// Restore, so the clipping is gone
									graphics.Restore(stateBeforeClip);
								}
								currentStep++;
								alpha = alpha - (basealpha/steps);
							}
						}
					}
				}
			}

			var state = graphics.Save();
			// draw the tail border where the bubble is not visible
			using (var clipRegion = new Region(bubble))
			{
				graphics.SetClip(clipRegion, CombineMode.Exclude);
				using (var pen = new Pen(_lineColor, _lineThickness))
				{
					graphics.DrawPath(pen, tail);
				}
			}
			graphics.Restore(state);

			if (ColorHelper.IsVisible(_fillColor))
			{
				//draw the bubbleshape
				state = graphics.Save();
				using (Brush brush = new SolidBrush(_fillColor))
				{
					graphics.FillPath(brush, bubble);
				}
				graphics.Restore(state);
			}

			if (lineVisible)
			{
				//draw the bubble border
				state = graphics.Save();
				// Draw bubble where the Tail is not visible.
				using (var clipRegion = new Region(tail))
				{
					graphics.SetClip(clipRegion, CombineMode.Exclude);
					using (var pen = new Pen(_lineColor, _lineThickness))
					{
						//pen.EndCap = pen.StartCap = LineCap.Round;
						graphics.DrawPath(pen, bubble);
					}
				}
				graphics.Restore(state);
			}

			if (ColorHelper.IsVisible(_fillColor))
			{
				// Draw the tail border
				state = graphics.Save();
				using (Brush brush = new SolidBrush(_fillColor))
				{
					graphics.FillPath(brush, tail);
				}
				graphics.Restore(state);
			}

			// cleanup the paths
			bubble.Dispose();
			tail.Dispose();

			// Draw the text
			UpdateFormat();
			DrawText(graphics, rect, _lineThickness, _lineColor, _shadow, StringFormat, Text, Font);
		}

		public override bool Contains(int x, int y)
		{
			if (base.Contains(x, y))
			{
				return true;
			}
			var clickedPoint = new Point(x, y);
			if (Status != EditStatus.UNDRAWN)
			{
				using (var pen = new Pen(_lineColor, _lineThickness))
				{
					using (var bubblePath = CreateBubble(_lineThickness))
					{
						bubblePath.Widen(pen);
						if (bubblePath.IsVisible(clickedPoint))
						{
							return true;
						}
					}
					using (var tailPath = CreateTail())
					{
						tailPath.Widen(pen);
						if (tailPath.IsVisible(clickedPoint))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public override bool ClickableAt(int x, int y)
		{
			return Contains(x, y);
		}
	}
}