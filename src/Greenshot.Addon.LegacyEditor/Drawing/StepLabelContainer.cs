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

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     This is an enumerated label, every single StepLabelContainer shows the number of the order it was created.
	///     To make sure that deleting recalculates, we check the location before every draw.
	/// </summary>
	[Serializable]
	public sealed class StepLabelContainer : DrawableContainer
	{
		private readonly bool _drawAsRectangle = false;

		[NonSerialized] private StringFormat _stringFormat = new StringFormat();

		private float _fontSize = 16;

		public StepLabelContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			parent.AddStepLabel(this);
			InitContent();
			Init();
		}

		public override Size DefaultSize => new Size(30, 30);

		private void Init()
		{
			CreateDefaultAdorners();
		}

		/// <summary>
		///     Restore values that don't serialize
		/// </summary>
		/// <param name="context"></param>
		protected override void OnDeserialized(StreamingContext context)
		{
			Init();
			_stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
		}

		/// <summary>
		///     Add the StepLabel to the parent
		/// </summary>
		/// <param name="newParent"></param>
		protected override void SwitchParent(Surface newParent)
		{
			if (newParent == Parent)
			{
				return;
			}
			((Surface) Parent)?.RemoveStepLabel(this);
			base.SwitchParent(newParent);
			if (newParent == null)
			{
				return;
			}
			// Make sure the counter start is restored (this unfortunately happens multiple times... -> hack)
			newParent.CounterStart = _counterStart;
			newParent.AddStepLabel(this);
		}

		public override bool InitContent()
		{
			_defaultEditMode = EditStatus.Idle;
			_stringFormat.Alignment = StringAlignment.Center;
			_stringFormat.LineAlignment = StringAlignment.Center;

			// Set defaults
			Width = DefaultSize.Width;
			Height = DefaultSize.Height;

			return true;
		}

		/// <summary>
		///     This makes it possible for the label to be placed exactly in the middle of the pointer.
		/// </summary>
		public override bool HandleMouseDown(int mouseX, int mouseY)
		{
			return base.HandleMouseDown(mouseX - Width / 2, mouseY - Height / 2);
		}

		/// <summary>
		///     We set our own field values
		/// </summary>
		protected override void InitializeFields()
		{
			AddField(GetType(), FieldTypes.FILL_COLOR, Color.DarkRed);
			AddField(GetType(), FieldTypes.LINE_COLOR, Color.White);
			AddField(GetType(), FieldTypes.FLAGS, FieldFlag.Counter);
		}

		/// <summary>
		///     Make sure this element is no longer referenced from the surface
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
			{
				return;
			}
			((Surface) Parent)?.RemoveStepLabel(this);
			if (_stringFormat == null)
			{
				return;
			}
			_stringFormat.Dispose();
			_stringFormat = null;
		}

		public override bool HandleMouseMove(int x, int y)
		{
			Invalidate();
			Left = x - Width / 2;
			Top = y - Height / 2;
			Invalidate();
			return true;
		}

		/// <summary>
		///     Make sure the size of the font is scaled
		/// </summary>
		/// <param name="matrix"></param>
		public override void Transform(Matrix matrix)
		{
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            var widthBefore = rect.Width;
			var heightBefore = rect.Height;

			// Transform this container
			base.Transform(matrix);
			rect = new NativeRect(Left, Top, Width, Height).Normalize();

            var widthAfter = rect.Width;
			var heightAfter = rect.Height;
			var factor = ((float) widthAfter / widthBefore + (float) heightAfter / heightBefore) / 2;

			_fontSize *= factor;
		}

		/// <summary>
		///     Override the parent, calculate the label number, than draw
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		public override void Draw(Graphics graphics, RenderMode rm)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			var text = ((Surface) Parent).CountStepLabels(this).ToString();
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);
			var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
			if (_drawAsRectangle)
			{
				RectangleContainer.DrawRectangle(rect, graphics, rm, 0, Color.Transparent, fillColor, false);
			}
			else
			{
				EllipseContainer.DrawEllipse(rect, graphics, rm, 0, Color.Transparent, fillColor, false);
			}
			using (var fam = new FontFamily(FontFamily.GenericSansSerif.Name))
			{
				using (var font = new Font(fam, _fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
				{
					TextContainer.DrawText(graphics, rect, 0, lineColor, false, _stringFormat, text, font);
				}
			}
		}

		public override bool ClickableAt(int x, int y)
		{
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);
			if (_drawAsRectangle)
			{
				return RectangleContainer.RectangleClickableAt(rect, 0, fillColor, x, y);
			}
			return EllipseContainer.EllipseClickableAt(rect, 0, fillColor, x, y);
		}

        // Used to store the number of this label, so when deserializing it can be placed back to the StepLabels list in the right location
		private int _number;
		// Used to store the counter start of the Surface, as the surface is NOT stored.
		private int _counterStart = 1;

		public int Number
		{
			get { return _number; }
			set { _number = value; }
		}

		/// <summary>
		///     Retrieve the counter before serializing
		/// </summary>
		/// <param name="context"></param>
		[OnSerializing]
		private void SetValuesOnSerializing(StreamingContext context)
		{
			if (Parent != null)
			{
				Number = ((Surface) Parent).CountStepLabels(this);
				_counterStart = ((Surface) Parent).CounterStart;
			}
		}
    }
}