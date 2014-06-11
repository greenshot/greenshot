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

using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Serialization;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of StepLabelContainer.
	/// This is an enumerated label, every single StepLabelContainer shows the number of the order it was created.
	/// To make sure that deleting recalculates, we check the location before every draw.
	/// </summary>
	[Serializable]
	public class StepLabelContainer : DrawableContainer {
		[NonSerialized]
		private StringFormat _stringFormat = new StringFormat();

		private readonly bool _drawAsRectangle = false;

		public StepLabelContainer(Surface parent) : base(parent) {
			parent.AddStepLabel(this);
			InitContent();
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context) {
			_stringFormat = new StringFormat();
			_stringFormat.Alignment = StringAlignment.Center;
			_stringFormat.LineAlignment = StringAlignment.Center;
		}

		/// <summary>
		/// Make sure the StepLabel is addded to the parent after deserializing
		/// by removing it from the current parent and added it to the new
		/// </summary>
		/// <param name="newParent"></param>
		protected override void SwitchParent(Surface newParent) {
			if (Parent != null) {
				Parent.RemoveStepLabel(this);
			}
			base.SwitchParent(newParent);
			if (Parent != null) {
				Parent.AddStepLabel(this);
			}
		}

		public override Size DefaultSize {
			get {
				return new Size(30, 30);
			}
		}

		public override bool InitContent() {
			_defaultEditMode = EditStatus.IDLE;
			_stringFormat.Alignment = StringAlignment.Center;
			_stringFormat.LineAlignment = StringAlignment.Center;

			// Set defaults
			Width = DefaultSize.Width;
			Height = DefaultSize.Height;

			return true;
		}

		/// <summary>
		/// This makes it possible for the label to be placed exactly in the middle of the pointer.
		/// </summary>
		public override bool HandleMouseDown(int mouseX, int mouseY) {
			return base.HandleMouseDown(mouseX - (Width / 2), mouseY - (Height / 2));
		}

		/// <summary>
		/// We set our own field values
		/// </summary>
		protected override void InitializeFields() {
			AddField(GetType(), FieldType.FILL_COLOR, Color.DarkRed);
			AddField(GetType(), FieldType.LINE_COLOR, Color.White);
		}

		/// <summary>
		/// Make sure this element is no longer referenced from the surface
		/// </summary>
		public override void Dispose() {
			Parent.RemoveStepLabel(this);
			base.Dispose();
		}

		public override bool HandleMouseMove(int x, int y) {
			Invalidate();
			Left = x - (Width / 2);
			Top = y - (Height / 2);
			Invalidate();
			return true;
		}

		/// <summary>
		/// Override the parent, calculate the label number, than draw
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		public override void Draw(Graphics graphics, RenderMode rm) {
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			string text = Parent.CountStepLabels(this).ToString();
			Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
			Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
			Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
			if (_drawAsRectangle) {
				RectangleContainer.DrawRectangle(rect, graphics, rm, 0, Color.Transparent, fillColor, false);
			} else {
				EllipseContainer.DrawEllipse(rect, graphics, rm, 0, Color.Transparent, fillColor, false);
			}
			using (FontFamily fam = new FontFamily(FontFamily.GenericSansSerif.Name)) {
				float factor = (((float)rect.Width / DefaultSize.Width) + ((float)rect.Height / DefaultSize.Height)) / 2;
				using (Font _font = new Font(fam, 16 * factor, FontStyle.Bold, GraphicsUnit.Pixel)) {
					TextContainer.DrawText(graphics, rect, 0, lineColor, false, _stringFormat, text, _font);
				}
			}
		}

		public override bool ClickableAt(int x, int y) {
			Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
			Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
			if (_drawAsRectangle) {
				return RectangleContainer.RectangleClickableAt(rect, 0, fillColor, x, y);
			} else {
				return EllipseContainer.EllipseClickableAt(rect, 0, fillColor, x, y);
			}
		}
	}
}
