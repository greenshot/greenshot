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
using System.Drawing.Text;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx.Legacy;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of SpeechbubbleContainer.
	/// </summary>
	[Serializable]
	public class SpeechbubbleContainer : TextContainer
	{
		private NativePoint _initialGripperPoint;

		public SpeechbubbleContainer(Surface parent, IEditorConfiguration editorConfiguration)
			: base(parent, editorConfiguration)
		{
		}

		/// <summary>
		///     The DrawingBound should be so close as possible to the shape, so we don't invalidate to much.
		/// </summary>
		public override NativeRect DrawingBounds
		{
			get
			{
				if (Status != EditStatus.Undrawn)
				{
					var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
					var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
					var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
					using (var pen = new Pen(lineColor, lineThickness))
					{
						var inflateValue = lineThickness + 2 + (shadow ? 6 : 0);
						using (var tailPath = CreateTail())
						{
                            NativeRectFloat tailBounds = tailPath.GetBounds(new Matrix(), pen);
                            var bounds = new NativeRect(Left, Top, Width, Height).Normalize();

                            return tailBounds.Round().Union(bounds).Inflate(inflateValue, inflateValue);
						}
					}
				}
				return NativeRect.Empty;
			}
		}

		/// <summary>
		///     We set our own field values
		/// </summary>
		protected override void InitializeFields()
		{
			AddField(GetType(), FieldTypes.LINE_THICKNESS, 2);
			AddField(GetType(), FieldTypes.LINE_COLOR, Color.Blue);
			AddField(GetType(), FieldTypes.SHADOW, false);
			AddField(GetType(), FieldTypes.FONT_ITALIC, false);
			AddField(GetType(), FieldTypes.FONT_BOLD, true);
			AddField(GetType(), FieldTypes.FILL_COLOR, Color.White);
			AddField(GetType(), FieldTypes.FONT_FAMILY, FontFamily.GenericSansSerif.Name);
			AddField(GetType(), FieldTypes.FONT_SIZE, 20f);
			AddField(GetType(), FieldTypes.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Center);
			AddField(GetType(), FieldTypes.TEXT_VERTICAL_ALIGNMENT, StringAlignment.Center);
		}

		/// <summary>
		///     Called from Surface (the _parent) when the drawing begins (mouse-down)
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseDown(int mouseX, int mouseY)
		{
			if (TargetAdorner == null)
			{
				_initialGripperPoint = new NativePoint(mouseX, mouseY);
				InitAdorner(Color.Green, new NativePoint(mouseX, mouseY));
			}
			return base.HandleMouseDown(mouseX, mouseY);
		}

		/// <summary>
		///     Overriding the HandleMouseMove will help us to make sure the tail is always visible.
		///     Should fix BUG-1682
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

			var newGripperLocation = _initialGripperPoint.Offset(xOffset, yOffset);

			if (TargetAdorner.Location != newGripperLocation)
			{
				Invalidate();
				TargetAdorner.Location = newGripperLocation;
				Invalidate();
			}
			return returnValue;
		}

		/// <summary>
		///     Helper method to create the bubble GraphicsPath, so we can also calculate the bounds
		/// </summary>
		/// <param name="lineThickness"></param>
		/// <returns></returns>
		private GraphicsPath CreateBubble(int lineThickness)
		{
			var bubble = new GraphicsPath();
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();

			var bubbleRect = new NativeRect(0, 0, rect.Width, rect.Height).Normalize();
			// adapt corner radius to small rectangle dimensions
			var smallerSideLength = Math.Min(bubbleRect.Width, bubbleRect.Height);
			var cornerRadius = Math.Min(30, smallerSideLength / 2 - lineThickness);
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
		///     Helper method to create the tail of the bubble, so we can also calculate the bounds
		/// </summary>
		/// <returns></returns>
		private GraphicsPath CreateTail()
		{
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();

			var tailLength = GeometryHelper.Distance2D(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2, TargetAdorner.Location.X, TargetAdorner.Location.Y);
			var tailWidth = (Math.Abs(rect.Width) + Math.Abs(rect.Height)) / 20;

			// This should fix a problem with the tail being to wide
			tailWidth = Math.Min(Math.Abs(rect.Width) / 2, tailWidth);
			tailWidth = Math.Min(Math.Abs(rect.Height) / 2, tailWidth);

			var tail = new GraphicsPath();
			tail.AddLine(-tailWidth, 0, tailWidth, 0);
			tail.AddLine(tailWidth, 0, 0, -tailLength);
			tail.CloseFigure();

			var tailAngle = 90 + (int) GeometryHelper.Angle2D(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2, TargetAdorner.Location.X, TargetAdorner.Location.Y);

			using (var tailMatrix = new Matrix())
			{
				tailMatrix.Translate(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
				tailMatrix.Rotate(tailAngle);
				tail.Transform(tailMatrix);
			}

			return tail;
		}

		/// <summary>
		///     This is to draw the actual container
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="renderMode"></param>
		public override void Draw(Graphics graphics, RenderMode renderMode)
		{
			if (TargetAdorner == null)
			{
				return;
			}
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

			var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
			var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);
			var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
			var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);

			var lineVisible = lineThickness > 0 && Colors.IsVisible(lineColor);
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();

            if (Selected && renderMode == RenderMode.Edit)
			{
				DrawSelectionBorder(graphics, rect);
			}

			var bubble = CreateBubble(lineThickness);

			var tail = CreateTail();

			//draw shadow first
			if (shadow && (lineVisible || Colors.IsVisible(fillColor)))
			{
				const int basealpha = 100;
				var alpha = basealpha;
				const int steps = 5;
				var currentStep = lineVisible ? 1 : 0;
				using (var shadowMatrix = new Matrix())
				using (var bubbleClone = (GraphicsPath) bubble.Clone())
				using (var tailClone = (GraphicsPath) tail.Clone())
				{
					shadowMatrix.Translate(1, 1);
					while (currentStep <= steps)
					{
						using (var shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100)))
						{
							shadowPen.Width = lineVisible ? lineThickness : 1;
							tailClone.Transform(shadowMatrix);
							graphics.DrawPath(shadowPen, tailClone);
							bubbleClone.Transform(shadowMatrix);
							graphics.DrawPath(shadowPen, bubbleClone);
						}
						currentStep++;
						alpha = alpha - basealpha / steps;
					}
				}
			}

			var state = graphics.Save();
			// draw the tail border where the bubble is not visible
			using (var clipRegion = new Region(bubble))
			{
				graphics.SetClip(clipRegion, CombineMode.Exclude);
				using (var pen = new Pen(lineColor, lineThickness))
				{
					graphics.DrawPath(pen, tail);
				}
			}
			graphics.Restore(state);

			if (Colors.IsVisible(fillColor))
			{
				//draw the bubbleshape
				state = graphics.Save();
				using (Brush brush = new SolidBrush(fillColor))
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
					using (var pen = new Pen(lineColor, lineThickness))
					{
						//pen.EndCap = pen.StartCap = LineCap.Round;
						graphics.DrawPath(pen, bubble);
					}
				}
				graphics.Restore(state);
			}

			if (Colors.IsVisible(fillColor))
			{
				// Draw the tail border
				state = graphics.Save();
				using (Brush brush = new SolidBrush(fillColor))
				{
					graphics.FillPath(brush, tail);
				}
				graphics.Restore(state);
			}

			// cleanup the paths
			bubble.Dispose();
			tail.Dispose();

			// Draw the text
			DrawText(graphics, rect, lineThickness, lineColor, shadow, StringFormat, Text, Font);
		}

		public override bool Contains(int x, int y)
		{
			if (base.Contains(x, y))
			{
				return true;
			}
			var clickedPoint = new NativePoint(x, y);
			if (Status != EditStatus.Undrawn)
			{
				var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
				var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
				using (var pen = new Pen(lineColor, lineThickness))
				{
					using (var bubblePath = CreateBubble(lineThickness))
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

        // Only used for serializing the TargetGripper location
		private NativePoint _storedTargetGripperLocation;

		/// <summary>
		///     Store the current location of the target gripper
		/// </summary>
		/// <param name="context"></param>
		[OnSerializing]
		private void SetValuesOnSerializing(StreamingContext context)
		{
			if (TargetAdorner != null)
			{
				_storedTargetGripperLocation = TargetAdorner.Location;
			}
		}

		/// <summary>
		///     Restore the target gripper
		/// </summary>
		/// <param name="streamingContext">StreamingContext</param>
		protected override void OnDeserialized(StreamingContext streamingContext)
		{
			base.OnDeserialized(streamingContext);
			InitAdorner(Color.Green, _storedTargetGripperLocation);
		}
    }
}