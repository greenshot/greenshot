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
using System.Drawing.Imaging;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Editor.Forms
{
    public delegate void ColorPickerEventHandler(object o, ColorPickerEventArgs e);

    public class ColorPickerToolStripButton : ToolStripButton
    {
        private Color _color;
        public NativePoint Offset = new NativePoint(0, 0);
        public event ColorPickerEventHandler ColorPicked;
        private readonly ColorDialog _cd;


        public ColorPickerToolStripButton()
        {
            _cd = ColorDialog.GetInstance();
            Click += ToolStripButton1Click;
        }

        public Color Color
        {
            set
            {
                _color = value;
                Invalidate();
            }
            get { return _color; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_color == null) return;
            // replace transparent color with selected color
            Graphics g = e.Graphics;
            ColorMap[] colorMap = new ColorMap[1];
            colorMap[0] = new ColorMap
            {
                OldColor = Color.Magenta, //this.ImageTransparentColor;
                NewColor = _color
            };
            ImageAttributes attr = new ImageAttributes();
            attr.SetRemapTable(colorMap);
            var rect = new NativeRect(0, 0, Image.Width, Image.Height);
            // todo find a way to retrieve transparency offset automatically
            // for now, we use the public variable Offset to define this manually
            rect = rect.Offset(Offset.X, Offset.Y);
            //ssif(color.Equals(Color.Transparent)) ((Bitmap)Image).MakeTransparent(Color.Magenta);
            g.DrawImage(Image, rect, 0, 0, rect.Width, rect.Height, GraphicsUnit.Pixel, attr);
            //this.Image.In
        }

        void ToolStripButton1Click(object sender, EventArgs e)
        {
            _cd.ShowDialog(Owner);
            Color = _cd.Color;
            ColorPicked?.Invoke(this, new ColorPickerEventArgs(Color));
        }
    }

    public class ColorPickerEventArgs : EventArgs
    {
        public Color Color;

        public ColorPickerEventArgs(Color color)
        {
            Color = color;
        }
    }
}