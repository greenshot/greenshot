/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Drawing
{
    /// <summary>
    /// This is an enumerated label, every single StepLabelContainer shows the number of the order it was created.
    /// To make sure that deleting recalculates, we check the location before every draw.
    /// </summary>
    [Serializable]
    public sealed class StepLabelContainer : DrawableContainer
    {
        [NonSerialized] private StringFormat _stringFormat = new StringFormat();

        private readonly bool _drawAsRectangle = false;

        public StepLabelContainer(Surface parent) : base(parent)
        {
            parent.AddStepLabel(this);
            InitContent();
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
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
        /// Retrieve the counter before serializing
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

        /// <summary>
        /// Restore values that don't serialize
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
        /// Add the StepLabel to the parent
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

        public override Size DefaultSize => new Size(30, 30);

        public override bool InitContent()
        {
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
        public override bool HandleMouseDown(int mouseX, int mouseY)
        {
            return base.HandleMouseDown(mouseX - Width / 2, mouseY - Height / 2);
        }

        /// <summary>
        /// We set our own field values
        /// </summary>
        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.FILL_COLOR, Color.DarkRed);
            AddField(GetType(), FieldType.LINE_COLOR, Color.White);
            AddField(GetType(), FieldType.FLAGS, FieldFlag.COUNTER);
        }

        /// <summary>
        /// Make sure this element is no longer referenced from the surface
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
        /// Override the parent, calculate the label number, than draw
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
            Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
            Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            if (_drawAsRectangle)
            {
                RectangleContainer.DrawRectangle(rect, graphics, rm, 0, Color.Transparent, fillColor, false);
            }
            else
            {
                EllipseContainer.DrawEllipse(rect, graphics, rm, 0, Color.Transparent, fillColor, false);
            }

            float fontSize = Math.Min(Width, Height) / 1.4f;
            using FontFamily fam = new FontFamily(FontFamily.GenericSansSerif.Name);
            using Font font = new Font(fam, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            TextContainer.DrawText(graphics, rect, 0, lineColor, false, _stringFormat, text, font);
        }

        public override bool ClickableAt(int x, int y)
        {
            Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
            Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
            if (_drawAsRectangle)
            {
                return RectangleContainer.RectangleClickableAt(rect, 0, fillColor, x, y);
            }

            return EllipseContainer.EllipseClickableAt(rect, 0, fillColor, x, y);
        }
    }
}