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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Represents a textbox (extends RectangleContainer for border/background support
    /// </summary>
    [Serializable]
    public class TextContainer : RectangleContainer, ITextContainer
    {
        // If makeUndoable is true the next text-change will make the change undoable.
        // This is set to true AFTER the first change is made, as there is already a "add element" on the undo stack
        // Although the name is wrong, we can't change it due to file serialization
        // ReSharper disable once InconsistentNaming
        private bool makeUndoable;
        [NonSerialized] private Font _font;
        public Font Font => _font;

        [NonSerialized] private TextBox _textBox;

        /// <summary>
        /// The StringFormat object is not serializable!!
        /// </summary>
        [NonSerialized] private StringFormat _stringFormat = new StringFormat();

        public StringFormat StringFormat => _stringFormat;

        // Although the name is wrong, we can't change it due to file serialization
        // ReSharper disable once InconsistentNaming
        private string text;

        // there is a binding on the following property!
        public string Text
        {
            get => text;
            set => ChangeText(value, true);
        }

        internal void ChangeText(string newText, bool allowUndoable)
        {
            if ((text != null || newText == null) && string.Equals(text, newText)) return;

            if (makeUndoable && allowUndoable && IsUndoable)
            {
                makeUndoable = false;
                _parent.MakeUndoable(new TextChangeMemento(this), false);
            }

            text = newText;
            OnPropertyChanged("Text");
        }

        public TextContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 2);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
            AddField(GetType(), FieldType.SHADOW, true);
            AddField(GetType(), FieldType.FONT_ITALIC, false);
            AddField(GetType(), FieldType.FONT_BOLD, false);
            AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
            AddField(GetType(), FieldType.FONT_FAMILY, FontFamily.GenericSansSerif.Name);
            AddField(GetType(), FieldType.FONT_SIZE, 11f);
            AddField(GetType(), FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Center);
            AddField(GetType(), FieldType.TEXT_VERTICAL_ALIGNMENT, StringAlignment.Center);
        }

        /// <summary>
        /// Do some logic to make sure all field are initiated correctly
        /// </summary>
        /// <param name="streamingContext">StreamingContext</param>
        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
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
            FieldChanged += TextContainer_FieldChanged;
        }

        protected override void SwitchParent(ISurface newParent)
        {
            if (InternalParent != null)
            {
                InternalParent.SizeChanged -= Parent_SizeChanged;
            }

            base.SwitchParent(newParent);
            if (InternalParent != null)
            {
                InternalParent.SizeChanged += Parent_SizeChanged;
            }
        }

        private void Parent_SizeChanged(object sender, EventArgs e)
        {
            UpdateTextBoxPosition();
            UpdateTextBoxFont();
        }

        public override void ApplyBounds(NativeRectFloat newBounds)
        {
            base.ApplyBounds(newBounds);
            UpdateTextBoxPosition();
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
            Size textSize = TextRenderer.MeasureText(text, _font);
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            Width = textSize.Width + lineThickness;
            Height = textSize.Height + lineThickness;
        }

        private void TextContainer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_textBox == null)
            {
                return;
            }

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
                else if (InternalParent != null && Selected && Status == EditStatus.IDLE && _textBox.Visible)
                {
                    // Fix (workaround) for BUG-1698
                    InternalParent.KeysLocked = true;
                }
            }

            if (_textBox.Visible)
            {
                _textBox.Invalidate();
            }
        }

        private void TextContainer_FieldChanged(object sender, FieldChangedEventArgs e)
        {
            if (_textBox == null)
            {
                return;
            }

            if (_textBox.Visible)
            {
                _textBox.Invalidate();
            }

            // Only dispose the font, and re-create it, when a font field has changed.
            if (e.Field.FieldType.Name.StartsWith("FONT"))
            {
                if (_font != null)
                {
                    _font.Dispose();
                    _font = null;
                }

                UpdateFormat();
            }
            else
            {
                UpdateAlignment();
            }

            UpdateTextBoxFormat();

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
            _textBox = new TextBox
            {
                ImeMode = ImeMode.On,
                Multiline = true,
                AcceptsTab = true,
                AcceptsReturn = true,
                BorderStyle = BorderStyle.None,
                Visible = false,
                Font = new Font(FontFamily.GenericSansSerif, 1) // just need something non-default here
            };

            _textBox.DataBindings.Add("Text", this, "Text", false, DataSourceUpdateMode.OnPropertyChanged);
            _textBox.LostFocus += TextBox_LostFocus;
            _textBox.KeyDown += textBox_KeyDown;
        }

        private void ShowTextBox()
        {
            if (InternalParent != null)
            {
                InternalParent.KeysLocked = true;
                InternalParent.Controls.Add(_textBox);
            }

            EnsureTextBoxContrast();
            if (_textBox != null)
            {
                _textBox.Show();
                _textBox.Focus();
            }
        }

        /// <summary>
        /// Makes textbox background dark if text color is very bright
        /// </summary>
        private void EnsureTextBoxContrast()
        {
            if (_textBox == null)
            {
                return;
            }

            Color lc = GetFieldValueAsColor(FieldType.LINE_COLOR);
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
            InternalParent?.Focus();
            _textBox?.Hide();
            if (InternalParent == null)
            {
                return;
            }

            InternalParent.KeysLocked = false;
            if (_textBox != null)
            {
                InternalParent.Controls.Remove(_textBox);
            }
        }

        /// <summary>
        /// Make sure the size of the font is scaled
        /// </summary>
        /// <param name="matrix"></param>
        public override void Transform(Matrix matrix)
        {
            var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            int pixelsBefore = rect.Width * rect.Height;

            // Transform this container
            base.Transform(matrix);
            rect = new NativeRect(Left, Top, Width, Height).Normalize();

            int pixelsAfter = rect.Width * rect.Height;
            float factor = pixelsAfter / (float) pixelsBefore;

            float fontSize = GetFieldValueAsFloat(FieldType.FONT_SIZE);
            fontSize *= factor;
            SetFieldValue(FieldType.FONT_SIZE, fontSize);
            UpdateFormat();
        }

        private Font CreateFont(string fontFamilyName, bool fontBold, bool fontItalic, float fontSize)
        {
            FontStyle fontStyle = FontStyle.Regular;

            bool hasStyle = false;
            using var fontFamily = new FontFamily(fontFamilyName);
            bool boldAvailable = fontFamily.IsStyleAvailable(FontStyle.Bold);
            if (fontBold && boldAvailable)
            {
                fontStyle |= FontStyle.Bold;
                hasStyle = true;
            }

            bool italicAvailable = fontFamily.IsStyleAvailable(FontStyle.Italic);
            if (fontItalic && italicAvailable)
            {
                fontStyle |= FontStyle.Italic;
                hasStyle = true;
            }

            if (!hasStyle)
            {
                bool regularAvailable = fontFamily.IsStyleAvailable(FontStyle.Regular);
                if (regularAvailable)
                {
                    fontStyle = FontStyle.Regular;
                }
                else
                {
                    if (boldAvailable)
                    {
                        fontStyle = FontStyle.Bold;
                    }
                    else if (italicAvailable)
                    {
                        fontStyle = FontStyle.Italic;
                    }
                }
            }

            return new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// Generate the Font-Formal so we can draw correctly
        /// </summary>
        protected void UpdateFormat()
        {
            if (_textBox == null)
            {
                return;
            }

            string fontFamily = GetFieldValueAsString(FieldType.FONT_FAMILY);
            bool fontBold = GetFieldValueAsBool(FieldType.FONT_BOLD);
            bool fontItalic = GetFieldValueAsBool(FieldType.FONT_ITALIC);
            float fontSize = GetFieldValueAsFloat(FieldType.FONT_SIZE);
            try
            {
                var newFont = CreateFont(fontFamily, fontBold, fontItalic, fontSize);
                _font?.Dispose();
                _font = newFont;
            }
            catch (Exception ex)
            {
                // Problem, try again with the default
                try
                {
                    fontFamily = FontFamily.GenericSansSerif.Name;
                    SetFieldValue(FieldType.FONT_FAMILY, fontFamily);
                    var newFont = CreateFont(fontFamily, fontBold, fontItalic, fontSize);
                    _font?.Dispose();
                    _font = newFont;
                }
                catch (Exception)
                {
                    // When this happens... the PC is broken
                    ex.Data.Add("fontFamilyName", fontFamily);
                    ex.Data.Add("fontBold", fontBold);
                    ex.Data.Add("fontItalic", fontItalic);
                    ex.Data.Add("fontSize", fontSize);
                    throw ex;
                }
            }

            UpdateTextBoxFont();

            UpdateAlignment();
        }

        private void UpdateAlignment()
        {
            _stringFormat.Alignment = (StringAlignment) GetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
            _stringFormat.LineAlignment = (StringAlignment) GetFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);
        }

        /// <summary>
        /// Set TextBox font according to the TextContainer font and the parent zoom factor.
        /// </summary>
        private void UpdateTextBoxFont()
        {
            if (_textBox == null || _font == null)
            {
                return;
            }

            var textBoxFontScale = _parent?.ZoomFactor ?? Fraction.Identity;

            var newFont = new Font(
                _font.FontFamily,
                _font.Size * textBoxFontScale,
                _font.Style,
                GraphicsUnit.Pixel
            );
            _textBox.Font.Dispose();
            _textBox.Font = newFont;
        }

        /// <summary>
        /// This will align the textbox exactly to the inner size of the element
        /// is a bit of a hack, but for now it seems to work...
        /// </summary>
        private void UpdateTextBoxPosition()
        {
            if (_textBox == null || Parent == null)
            {
                return;
            }

            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);

            int lineWidth = (int) Math.Floor(lineThickness / 2d);
            int correction = (lineThickness + 1) % 2;
            if (lineThickness <= 1)
            {
                lineWidth = 1;
                correction = -1;
            }

            var absRectangle = new NativeRect(Left, Top, Width, Height).Normalize();
            var displayRectangle = Parent.ToSurfaceCoordinates(absRectangle);
            _textBox.Left = displayRectangle.X + lineWidth;
            _textBox.Top = displayRectangle.Y + lineWidth;
            if (lineThickness <= 1)
            {
                lineWidth = 0;
            }

            _textBox.Width = displayRectangle.Width - 2 * lineWidth + correction;
            _textBox.Height = displayRectangle.Height - 2 * lineWidth + correction;
        }

        /// <summary>
        /// Set TextBox text align and fore color according to field values.
        /// </summary>
        private void UpdateTextBoxFormat()
        {
            if (_textBox == null)
            {
                return;
            }

            var alignment = (StringAlignment) GetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
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

            var lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            _textBox.ForeColor = lineColor;
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            // ESC and Enter/Return (w/o Shift) hide text editor
            if (e.KeyCode == Keys.Escape || ((e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) && e.Modifiers == Keys.None))
            {
                HideTextBox();
                e.SuppressKeyPress = true;
            }

            if (e.Control && !e.Alt && e.KeyCode == Keys.A)
            {
                _textBox.SelectAll();
            }

            // Added for FEATURE-1064
            if (e.KeyCode == Keys.Back && e.Control)
            {
                e.SuppressKeyPress = true;
                int selStart = _textBox.SelectionStart;
                while (selStart > 0 && _textBox.Text.Substring(selStart - 1, 1) == " ")
                {
                    selStart--;
                }

                int prevSpacePos = -1;
                if (selStart != 0)
                {
                    prevSpacePos = _textBox.Text.LastIndexOf(' ', selStart - 1);
                }

                _textBox.Select(prevSpacePos + 1, _textBox.SelectionStart - prevSpacePos - 1);
                _textBox.SelectedText = string.Empty;
            }
        }

        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            // next change will be made undoable
            makeUndoable = true;
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

            var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            if (Selected && rm == RenderMode.EDIT)
            {
                DrawSelectionBorder(graphics, rect);
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // we only draw the shadow if there is no background
            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
            Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            bool drawShadow = shadow && (fillColor == Color.Transparent || fillColor == Color.Empty);

            DrawText(graphics, rect, lineThickness, lineColor, drawShadow, _stringFormat, text, _font);
        }

        /// <summary>
        /// This method can be used from other containers
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="drawingRectange"></param>
        /// <param name="lineThickness"></param>
        /// <param name="fontColor"></param>
        /// <param name="drawShadow"></param>
        /// <param name="stringFormat"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        public static void DrawText(Graphics graphics, NativeRect drawingRectange, int lineThickness, Color fontColor, bool drawShadow, StringFormat stringFormat, string text,
            Font font)
        {
#if DEBUG
            Debug.Assert(font != null);
#else
            if (font == null)
            {
                return;
            }
#endif
            int textOffset = lineThickness > 0 ? (int) Math.Ceiling(lineThickness / 2d) : 0;
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
                    NativeRect shadowRect = new NativeRect(drawingRectange.Left + offset, drawingRectange.Top + offset, drawingRectange.Width, drawingRectange.Height).Normalize();
                    if (lineThickness > 0)
                    {
                        shadowRect = shadowRect.Inflate(-textOffset, -textOffset);
                    }
                    using Brush fontBrush = new SolidBrush(Color.FromArgb(alpha, 100, 100, 100));
                    graphics.DrawString(text, font, fontBrush, shadowRect, stringFormat);

                    currentStep++;
                    alpha -= basealpha / steps;
                }
            }

            if (lineThickness > 0)
            {
                drawingRectange = drawingRectange.Inflate(-textOffset, -textOffset);
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
            var r = new NativeRect(Left, Top, Width, Height).Normalize().Inflate(5, 5);
            return r.Contains(x, y);
        }
    }
}