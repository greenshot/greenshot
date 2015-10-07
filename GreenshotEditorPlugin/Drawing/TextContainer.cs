/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using GreenshotEditorPlugin.Memento;
using GreenshotPlugin.Extensions;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace GreenshotEditorPlugin.Drawing
{
	/// <summary>
	/// Represents a textbox (extends RectangleContainer for border/background support
	/// </summary>
	[Serializable]
	public class TextContainer : RectangleContainer, ITextContainer
	{
		// If makeUndoable is true the next text-change will make the change undoable.
		// This is set to true AFTER the first change is made, as there is already a "add element" on the undo stack
		private bool _makeUndoable;

		[NonSerialized]
		private Font _font;

		public Font Font
		{
			get
			{
				return _font;
			}
		}

		[NonSerialized]
		private TextBox _textBox;

		/// <summary>
		/// The StringFormat object is not serializable!!
		/// </summary>
		[NonSerialized]
		private StringFormat _stringFormat = new StringFormat();

		public StringFormat StringFormat
		{
			get
			{
				return _stringFormat;
			}
		}

		private string _text;
		// there is a binding on the following property!
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				ChangeText(value, true);
			}
		}

		public void ChangeText(string newText, bool allowUndoable)
		{
			if ((_text == null && newText != null) || !_text.Equals(newText))
			{
				if (_makeUndoable && allowUndoable)
				{
					_makeUndoable = false;
					_parent.MakeUndoable(new TextChangeMemento(this), false);
				}
				_text = newText;
				OnPropertyChanged("Text");
			}
		}

		protected bool _italic = false;

		[Field(FieldTypes.FONT_ITALIC)]
		public bool Italic
		{
			get
			{
				return _italic;
			}
			set
			{
				_italic = value;
				OnFieldPropertyChanged(FieldTypes.FONT_ITALIC);
			}
		}

		protected bool _bold = false;

		[Field(FieldTypes.FONT_BOLD)]
		public bool Bold
		{
			get
			{
				return _bold;
			}
			set
			{
				_bold = value;
				OnFieldPropertyChanged(FieldTypes.FONT_BOLD);
			}
		}

		protected float _fontSize = 11f;

		[Field(FieldTypes.FONT_SIZE)]
		public float FontSize
		{
			get
			{
				return _fontSize;
			}
			set
			{
				_fontSize = value;
				OnFieldPropertyChanged(FieldTypes.FONT_SIZE);
			}
		}

		protected string _fontFamily = FontFamily.GenericSansSerif.Name;

		[Field(FieldTypes.FONT_FAMILY)]
		public string Family
		{
			get
			{
				return _fontFamily;
			}
			set
			{
				_fontFamily = value;
				OnFieldPropertyChanged(FieldTypes.FONT_FAMILY);
			}
		}

		private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;

		[Field(FieldTypes.TEXT_HORIZONTAL_ALIGNMENT)]
		public HorizontalAlignment HorizontalAlignment
		{
			get
			{
				return _horizontalAlignment;
			}
			set
			{
				_horizontalAlignment = value;
				OnFieldPropertyChanged(FieldTypes.TEXT_HORIZONTAL_ALIGNMENT);
			}
		}

		private VerticalAlignment _verticalAlignment = VerticalAlignment.CENTER;

		[Field(FieldTypes.TEXT_VERTICAL_ALIGNMENT)]
		public VerticalAlignment VerticalAlignment
		{
			get
			{
				return _verticalAlignment;
			}
			set
			{
				_verticalAlignment = value;
				OnFieldPropertyChanged(FieldTypes.TEXT_VERTICAL_ALIGNMENT);
			}
		}

		public TextContainer(Surface parent) : base(parent)
		{
			Init();
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			Init();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_font != null)
				{
					_font.Dispose();
					_font = null;
				}
				if (_stringFormat != null)
				{
					_stringFormat.Dispose();
					_stringFormat = null;
				}
				if (_textBox != null)
				{
					_textBox.Dispose();
					_textBox = null;
				}
			}
			base.Dispose(disposing);
		}

		private void Init()
		{
			_stringFormat = new StringFormat
			{
				Trimming = StringTrimming.EllipsisWord
			};
			CreateTextBox();
			UpdateFormat();
			UpdateTextBoxFormat();
			PropertyChanged += TextContainer_PropertyChanged;
		}

		public override void Invalidate()
		{
			base.Invalidate();
			if (_textBox != null && _textBox.Visible)
			{
				_textBox.Invalidate();
			}
		}

		public void FitToText()
		{
			Size textSize = TextRenderer.MeasureText(_text, _font);
			Width = textSize.Width + _lineThickness;
			Height = textSize.Height + _lineThickness;
		}

		private void TextContainer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_textBox.Visible)
			{
				_textBox.Invalidate();
			}

			UpdateTextBoxPosition();
			UpdateTextBoxFormat();
			if (e.PropertyName.Equals("Selected"))
			{
				if (!Selected && _textBox.Visible)
				{
					HideTextBox();
				}
				else if (Selected && Status == EditStatus.DRAWING)
				{
					ShowTextBox();
				}
				else if (Selected && Status == EditStatus.IDLE && _textBox.Visible)
				{
					// Fix (workaround) for BUG-1698
					_parent.KeysLocked = true;
				}
			}
			if (_textBox.Visible)
			{
				_textBox.Invalidate();
			}
		}

		public override void OnDoubleClick()
		{
			ShowTextBox();
		}

		private void CreateTextBox()
		{
			_textBox = new TextBox();

			_textBox.ImeMode = ImeMode.On;
			_textBox.Multiline = true;
			_textBox.AcceptsTab = true;
			_textBox.AcceptsReturn = true;
			_textBox.DataBindings.Add("Text", this, "Text", false, DataSourceUpdateMode.OnPropertyChanged);
			_textBox.LostFocus += textBox_LostFocus;
			_textBox.KeyDown += textBox_KeyDown;
			_textBox.BorderStyle = BorderStyle.None;
			_textBox.Visible = false;
		}

		private void ShowTextBox()
		{
			_parent.KeysLocked = true;
			_parent.Controls.Add(_textBox);
			EnsureTextBoxContrast();
			_textBox.Show();
			_textBox.Focus();
		}

		/// <summary>
		/// Makes textbox background dark if text color is very bright
		/// </summary>
		private void EnsureTextBoxContrast()
		{
			Color lc = _lineColor;
			if (lc.R > 203 && lc.G > 203 && lc.B > 203)
			{
				_textBox.BackColor = Color.FromArgb(51, 51, 51);
			}
			else
			{
				_textBox.BackColor = Color.White;
			}
		}

		private void HideTextBox()
		{
			_parent.Focus();
			_textBox.Hide();
			_parent.KeysLocked = false;
			_parent.Controls.Remove(_textBox);
		}

		/// <summary>
		/// Make sure the size of the font is scaled
		/// </summary>
		/// <param name="matrix"></param>
		public override void Transform(Matrix matrix)
		{
			Rectangle rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			int pixelsBefore = rect.Width*rect.Height;

			// Transform this container
			base.Transform(matrix);
			rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();

			int pixelsAfter = rect.Width*rect.Height;
			float factor = pixelsAfter/(float) pixelsBefore;

			_fontSize = _fontSize*factor;
			UpdateFormat();
		}

		/// <summary>
		/// Generate the Font-Formal so we can draw correctly
		/// </summary>
		protected void UpdateFormat()
		{
			try
			{
				FontStyle fs = FontStyle.Regular;

				bool hasStyle = false;
				using (FontFamily fam = new FontFamily(_fontFamily))
				{
					bool boldAvailable = fam.IsStyleAvailable(FontStyle.Bold);
					if (_bold && boldAvailable)
					{
						fs |= FontStyle.Bold;
						hasStyle = true;
					}

					bool italicAvailable = fam.IsStyleAvailable(FontStyle.Italic);
					if (_italic && italicAvailable)
					{
						fs |= FontStyle.Italic;
						hasStyle = true;
					}

					if (!hasStyle)
					{
						bool regularAvailable = fam.IsStyleAvailable(FontStyle.Regular);
						if (regularAvailable)
						{
							fs = FontStyle.Regular;
						}
						else
						{
							if (boldAvailable)
							{
								fs = FontStyle.Bold;
							}
							else if (italicAvailable)
							{
								fs = FontStyle.Italic;
							}
						}
					}
					_font = new Font(fam, _fontSize, fs, GraphicsUnit.Pixel);
					_textBox.Font = _font;
				}
			}
			catch (Exception ex)
			{
				ex.Data.Add("fontFamily", _fontFamily);
				ex.Data.Add("fontBold", _bold);
				ex.Data.Add("fontItalic", _italic);
				ex.Data.Add("fontSize", _fontSize);
				throw;
			}

			UpdateAlignment();
		}

		private void UpdateAlignment()
		{
			_stringFormat.Alignment = (StringAlignment) _horizontalAlignment;
			_stringFormat.LineAlignment = (StringAlignment) _verticalAlignment;
		}

		/// <summary>
		/// This will create the textbox exactly to the inner size of the element
		/// is a bit of a hack, but for now it seems to work...
		/// </summary>
		private void UpdateTextBoxPosition()
		{
			int lineWidth = (int) Math.Floor(_lineThickness/2d);
			int correction = (_lineThickness + 1)%2;
			if (_lineThickness <= 1)
			{
				lineWidth = 1;
				correction = -1;
			}
			Rectangle absRectangle = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			_textBox.Left = absRectangle.Left + lineWidth;
			_textBox.Top = absRectangle.Top + lineWidth;
			if (_lineThickness <= 1)
			{
				lineWidth = 0;
			}
			_textBox.Width = absRectangle.Width - (2*lineWidth) + correction;
			_textBox.Height = absRectangle.Height - (2*lineWidth) + correction;
		}

		public override void ApplyBounds(RectangleF newBounds)
		{
			base.ApplyBounds(newBounds);
			UpdateTextBoxPosition();
		}

		private void UpdateTextBoxFormat()
		{
			if (_textBox == null)
			{
				return;
			}
			StringAlignment alignment = (StringAlignment) _horizontalAlignment;
			switch (alignment)
			{
				case StringAlignment.Near:
					_textBox.TextAlign = HorizontalAlignment.Left;
					break;
				case StringAlignment.Far:
					_textBox.TextAlign = HorizontalAlignment.Right;
					break;
				case StringAlignment.Center:
					_textBox.TextAlign = HorizontalAlignment.Center;
					break;
			}

			_textBox.ForeColor = _lineColor;
		}

		private void textBox_KeyDown(object sender, KeyEventArgs e)
		{
			// ESC and Enter/Return (w/o Shift) hide text editor
			if (e.KeyCode == Keys.Escape || ((e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) && e.Modifiers == Keys.None))
			{
				HideTextBox();
				e.SuppressKeyPress = true;
			}
		}

		private void textBox_LostFocus(object sender, EventArgs e)
		{
			// next change will be made undoable
			_makeUndoable = true;
			HideTextBox();
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			base.Draw(graphics, rm);

			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			graphics.TextRenderingHint = TextRenderingHint.SystemDefault;

			Rectangle rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			if (Selected && rm == RenderMode.EDIT)
			{
				DrawSelectionBorder(graphics, rect);
			}

			if (string.IsNullOrEmpty(_text))
			{
				return;
			}

			// we only draw the _shadow if there is no background
			bool drawShadow = _shadow && (_fillColor == Color.Transparent || _fillColor == Color.Empty);

			DrawText(graphics, rect, _lineThickness, _lineColor, drawShadow, _stringFormat, _text, _font);
		}

		/// <summary>
		/// This method can be used from other containers
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="drawingRectange"></param>
		/// <param name="lineThickness"></param>
		/// <param name="fontColor"></param>
		/// <param name="stringFormat"></param>
		/// <param name="text"></param>
		/// <param name="font"></param>
		public static void DrawText(Graphics graphics, Rectangle drawingRectange, int lineThickness, Color fontColor, bool drawShadow, StringFormat stringFormat, string text, Font font)
		{
			int textOffset = (lineThickness > 0) ? (int) Math.Ceiling(lineThickness/2d) : 0;
			// draw shadow before anything else
			if (drawShadow)
			{
				int basealpha = 100;
				int alpha = basealpha;
				int steps = 5;
				int currentStep = 1;
				while (currentStep <= steps)
				{
					int offset = currentStep;
					Rectangle shadowRect = new Rectangle(drawingRectange.Left + offset, drawingRectange.Top + offset, drawingRectange.Width, drawingRectange.Height).MakeGuiRectangle();
					if (lineThickness > 0)
					{
						shadowRect.Inflate(-textOffset, -textOffset);
					}
					using (Brush fontBrush = new SolidBrush(Color.FromArgb(alpha, 100, 100, 100)))
					{
						graphics.DrawString(text, font, fontBrush, shadowRect, stringFormat);
						currentStep++;
						alpha = alpha - basealpha/steps;
					}
				}
			}

			if (lineThickness > 0)
			{
				drawingRectange.Inflate(-textOffset, -textOffset);
			}
			using (Brush fontBrush = new SolidBrush(fontColor))
			{
				if (stringFormat != null)
				{
					graphics.DrawString(text, font, fontBrush, drawingRectange, stringFormat);
				}
				else
				{
					graphics.DrawString(text, font, fontBrush, drawingRectange);
				}
			}
		}

		public override bool ClickableAt(int x, int y)
		{
			Rectangle r = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			r.Inflate(5, 5);
			return r.Contains(x, y);
		}
	}
}