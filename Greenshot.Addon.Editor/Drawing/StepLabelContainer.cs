//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Serialization;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces.Drawing;

#endregion

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	///     This is an enumerated label, every single StepLabelContainer shows the number of the order it was created.
	///     To make sure that deleting recalculates, we check the location before every draw.
	/// </summary>
	[Serializable]
	[Flag(ElementFlag.COUNTER)]
	public class StepLabelContainer : DrawableContainer
	{
		private readonly bool _drawAsRectangle = false;

		private Color _fillColor = Color.DarkRed;

		private Color _lineColor = Color.White;

		// Used to store the number of this label, so when deserializing it can be placed back to the StepLabels list in the right location
		private int _number;

		[NonSerialized] private StringFormat _stringFormat = new StringFormat();

		private float fontSize = 16;

		public StepLabelContainer(Surface parent) : base(parent)
		{
			parent.AddStepLabel(this);
			InitContent();
			Init();
		}

		[Field(FieldTypes.COUNTER_START)]
		public int CounterStart
		{
			get { return Parent.CounterStart; }
			set { Parent.CounterStart = value; }
		}

		public override Size DefaultSize
		{
			get { return new Size(30, 30); }
		}

		[Field(FieldTypes.FILL_COLOR)]
		public Color FillColor
		{
			get { return _fillColor; }
			set
			{
				_fillColor = value;
				OnFieldPropertyChanged(FieldTypes.FILL_COLOR);
			}
		}

		[Field(FieldTypes.LINE_COLOR)]
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				_lineColor = value;
				OnFieldPropertyChanged(FieldTypes.LINE_COLOR);
			}
		}

		public int Number
		{
			get { return _number; }
			set { _number = value; }
		}

		public override bool ClickableAt(int x, int y)
		{
			var rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			if (_drawAsRectangle)
			{
				return RectangleContainer.RectangleClickableAt(rect, 0, _fillColor, x, y);
			}
			return EllipseContainer.EllipseClickableAt(rect, 0, _fillColor, x, y);
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
			((Surface) Parent).RemoveStepLabel(this);
			if (_stringFormat != null)
			{
				_stringFormat.Dispose();
				_stringFormat = null;
			}
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
			string text = ((Surface) Parent).CountStepLabels(this).ToString();
			Rectangle rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			if (_drawAsRectangle)
			{
				RectangleContainer.DrawRectangle(rect, graphics, rm, 0, Color.Transparent, _fillColor, false);
			}
			else
			{
				EllipseContainer.DrawEllipse(rect, graphics, rm, 0, Color.Transparent, _fillColor, false);
			}
			using (var fam = new FontFamily(FontFamily.GenericSansSerif.Name))
			{
				using (var font = new Font(fam, fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
				{
					TextContainer.DrawText(graphics, rect, 0, _lineColor, false, _stringFormat, text, font);
				}
			}
		}

		/// <summary>
		///     This makes it possible for the label to be placed exactly in the middle of the pointer.
		/// </summary>
		public override bool HandleMouseDown(int mouseX, int mouseY)
		{
			return base.HandleMouseDown(mouseX - Width/2, mouseY - Height/2);
		}

		public override bool HandleMouseMove(int x, int y)
		{
			Invalidate();
			Left = x - Width/2;
			Top = y - Height/2;
			Invalidate();
			return true;
		}

		private void Init()
		{
			CreateDefaultAdorners();
		}

		public override bool InitContent()
		{
			DefaultEditMode = EditStatus.IDLE;
			_stringFormat.Alignment = StringAlignment.Center;
			_stringFormat.LineAlignment = StringAlignment.Center;

			// Set defaults
			Width = DefaultSize.Width;
			Height = DefaultSize.Height;

			return true;
		}

		/// <summary>
		///     Restore values that don't serialize
		/// </summary>
		/// <param name="context"></param>
		protected override void OnDeserialized(StreamingContext context)
		{
			Init();
			_stringFormat = new StringFormat();
			_stringFormat.Alignment = StringAlignment.Center;
			_stringFormat.LineAlignment = StringAlignment.Center;
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
			}
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
			if (Parent != null)
			{
				((Surface) Parent).RemoveStepLabel(this);
			}
			base.SwitchParent(newParent);
			if (newParent != null)
			{
				((Surface) Parent).AddStepLabel(this);
			}
		}

		/// <summary>
		///     Make sure the size of the font is scaled
		/// </summary>
		/// <param name="matrix"></param>
		public override void Transform(Matrix matrix)
		{
			var rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			int widthBefore = rect.Width;
			int heightBefore = rect.Height;

			// Transform this container
			base.Transform(matrix);
			rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();

			int widthAfter = rect.Width;
			int heightAfter = rect.Height;
			float factor = ((float) widthAfter/widthBefore + (float) heightAfter/heightBefore)/2;

			fontSize *= factor;
		}
	}
}