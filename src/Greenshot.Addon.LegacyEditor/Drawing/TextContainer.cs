/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addon.LegacyEditor.Memento;
using Greenshot.Addons.Interfaces.Drawing;
#if DEBUG
using System.Diagnostics;
#endif

namespace Greenshot.Addon.LegacyEditor.Drawing
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
        [NonSerialized]
        private Font _font;
        public Font Font => _font;

        [NonSerialized]
        private TextBox _textBox;

        /// <summary>
        /// The StringFormat object is not serializable!!
        /// </summary>
        [NonSerialized]
        private StringFormat _stringFormat = new StringFormat();

        public StringFormat StringFormat => _stringFormat;

        // Although the name is wrong, we can't change it due to file serialization
        // ReSharper disable once InconsistentNaming
        private string text;
        // there is a binding on the following property!
        public string Text
        {
            get { return text; }
            set
            {
                ChangeText(value, true);
            }
        }

        internal void ChangeText(string newText, bool allowUndoable)
        {
            if ((text == null && newText != null) || !string.Equals(text, newText))
            {
                if (makeUndoable && allowUndoable)
                {
                    makeUndoable = false;
                    _parent.MakeUndoable(new TextChangeMemento(this), false);
                }
                text = newText;
                OnPropertyChanged("Text");
            }
        }

        public TextContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
        {
            Init();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldTypes.LINE_THICKNESS, 2);
            AddField(GetType(), FieldTypes.LINE_COLOR, Color.Red);
            AddField(GetType(), FieldTypes.SHADOW, true);
            AddField(GetType(), FieldTypes.FONT_ITALIC, false);
            AddField(GetType(), FieldTypes.FONT_BOLD, false);
            AddField(GetType(), FieldTypes.FILL_COLOR, Color.Transparent);
            AddField(GetType(), FieldTypes.FONT_FAMILY, FontFamily.GenericSansSerif.Name);
            AddField(GetType(), FieldTypes.FONT_SIZE, 11f);
            AddField(GetType(), FieldTypes.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Center);
            AddField(GetType(), FieldTypes.TEXT_VERTICAL_ALIGNMENT, StringAlignment.Center);
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
            var textSize = TextRenderer.MeasureText(text, _font);
            int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
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
                else if (Selected && Status == EditStatus.Drawing)
                {
                    ShowTextBox();
                }
                else if (_parent != null && Selected && Status == EditStatus.Idle && _textBox.Visible)
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
                Visible = false
            };

            _textBox.DataBindings.Add("Text", this, "Text", false, DataSourceUpdateMode.OnPropertyChanged);
            _textBox.LostFocus += textBox_LostFocus;
            _textBox.KeyDown += textBox_KeyDown;
        }

        private void ShowTextBox()
        {
            if (_parent != null)
            {
                _parent.KeysLocked = true;
                _parent.Controls.Add(_textBox);
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
            var lc = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
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
            _textBox?.Hide();
            _parent.KeysLocked = false;
            _parent.Controls.Remove(_textBox);
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
            float factor = pixelsAfter / (float)pixelsBefore;

            float fontSize = GetFieldValueAsFloat(FieldTypes.FONT_SIZE);
            fontSize *= factor;
            SetFieldValue(FieldTypes.FONT_SIZE, fontSize);
            UpdateFormat();
        }

        private Font CreateFont(string fontFamilyName, bool fontBold, bool fontItalic, float fontSize)
        {
            var fontStyle = FontStyle.Regular;

            bool hasStyle = false;
            using (var fontFamily = new FontFamily(fontFamilyName))
            {
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
            string fontFamily = GetFieldValueAsString(FieldTypes.FONT_FAMILY);
            bool fontBold = GetFieldValueAsBool(FieldTypes.FONT_BOLD);
            bool fontItalic = GetFieldValueAsBool(FieldTypes.FONT_ITALIC);
            float fontSize = GetFieldValueAsFloat(FieldTypes.FONT_SIZE);
            try
            {
                var newFont = CreateFont(fontFamily, fontBold, fontItalic, fontSize);
                _font?.Dispose();
                _font = newFont;
                _textBox.Font = _font;
            }
            catch (Exception ex)
            {
                // Problem, try again with the default
                try
                {
                    fontFamily = FontFamily.GenericSansSerif.Name;
                    SetFieldValue(FieldTypes.FONT_FAMILY, fontFamily);
                    var newFont = CreateFont(fontFamily, fontBold, fontItalic, fontSize);
                    _font?.Dispose();
                    _font = newFont;
                    _textBox.Font = _font;
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

            UpdateAlignment();
        }

        private void UpdateAlignment()
        {
            _stringFormat.Alignment = (StringAlignment)GetFieldValue(FieldTypes.TEXT_HORIZONTAL_ALIGNMENT);
            _stringFormat.LineAlignment = (StringAlignment)GetFieldValue(FieldTypes.TEXT_VERTICAL_ALIGNMENT);
        }

        /// <summary>
        /// This will create the textbox exactly to the inner size of the element
        /// is a bit of a hack, but for now it seems to work...
        /// </summary>
        private void UpdateTextBoxPosition()
        {
            if (_textBox == null)
            {
                return;
            }
            int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);

            int lineWidth = (int)Math.Floor(lineThickness / 2d);
            int correction = (lineThickness + 1) % 2;
            if (lineThickness <= 1)
            {
                lineWidth = 1;
                correction = -1;
            }
            var absRectangle = new NativeRect(Left, Top, Width, Height).Normalize();
            _textBox.Left = absRectangle.Left + lineWidth;
            _textBox.Top = absRectangle.Top + lineWidth;
            if (lineThickness <= 1)
            {
                lineWidth = 0;
            }
            _textBox.Width = absRectangle.Width - 2 * lineWidth + correction;
            _textBox.Height = absRectangle.Height - 2 * lineWidth + correction;
        }

        public override void ApplyBounds(NativeRect newBounds)
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
            var alignment = (StringAlignment)GetFieldValue(FieldTypes.TEXT_HORIZONTAL_ALIGNMENT);
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

            var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
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

            if (e.Control && e.KeyCode == Keys.A)
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
                _textBox.SelectedText = "";
            }
        }

        private void textBox_LostFocus(object sender, EventArgs e)
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
            if (Selected && rm == RenderMode.Edit)
            {
                DrawSelectionBorder(graphics, rect);
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // we only draw the shadow if there is no background
            bool shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
            var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);
            int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
            var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
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
        public static void DrawText(Graphics graphics, NativeRect drawingRectange, int lineThickness, Color fontColor, bool drawShadow, StringFormat stringFormat, string text, Font font)
        {
#if DEBUG
            Debug.Assert(font != null);
#else
            if (font == null)
            {
                return;
            }
#endif
            int textOffset = lineThickness > 0 ? (int)Math.Ceiling(lineThickness / 2d) : 0;
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
                    var shadowRect = new NativeRect(drawingRectange.Left + offset, drawingRectange.Top + offset, drawingRectange.Width, drawingRectange.Height).Normalize();
                    if (lineThickness > 0)
                    {
                        shadowRect = shadowRect.Inflate(-textOffset, -textOffset);
                    }
                    using (Brush fontBrush = new SolidBrush(Color.FromArgb(alpha, 100, 100, 100)))
                    {
                        graphics.DrawString(text, font, fontBrush, (Rectangle)shadowRect, stringFormat);
                        currentStep++;
                        alpha = alpha - basealpha / steps;
                    }
                }
            }

            if (lineThickness > 0)
            {
                drawingRectange = drawingRectange.Inflate(-textOffset, -textOffset);
            }
            using (var fontBrush = new SolidBrush(fontColor))
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
            return new NativeRect(Left, Top, Width, Height).Normalize().Inflate(5, 5).Contains(x, y);
        }
    }
}
