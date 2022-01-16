﻿/*
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
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emoji.Wpf;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Helpers;
using Image = System.Drawing.Image;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of EmojiContainer.
    /// </summary>
    [Serializable]
    public class EmojiContainer : DrawableContainer, IEmojiContainer
    {
        [NonSerialized] private static EmojiContainer _currentContainer;
        [NonSerialized] private static ElementHost _emojiPickerHost;
        [NonSerialized] private static Picker _emojiPicker;

        [NonSerialized] private System.Windows.Controls.Image _image;
        [NonSerialized] private bool _justCreated = true;

        private string _emoji;
        private int _rotationAngle;

        public string Emoji
        {
            get => _emoji;
            set
            {
                _emoji = value;
                if (_image != null)
                {
                    global::Emoji.Wpf.Image.SetSource(_image, Emoji);
                }
            }
        }

        public EmojiContainer(Surface parent) : this(parent, "🙂", size: 64)
        {
        }

        public EmojiContainer(Surface parent, string emoji, int size) : base(parent)
        {
            Emoji = emoji;
            Width = size;
            Height = size;
            Init();
        }

        public override void OnDoubleClick()
        {
            ShowEmojiPicker();
        }

        private void ShowEmojiPicker()
        {
            _currentContainer = this;

            GetOrCreatePickerControl();

            var absRectangle = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
            var displayRectangle = Parent.ToSurfaceCoordinates(absRectangle);
            _emojiPickerHost.Width = 0; // Trick to hide the picker's button
            _emojiPickerHost.Height = displayRectangle.Height;
            _emojiPickerHost.Left = displayRectangle.Left;
            _emojiPickerHost.Top = displayRectangle.Top;

            _emojiPicker.Selection = Emoji;
            _emojiPicker.ShowPopup(true);
        }

        private void GetOrCreatePickerControl()
        {
            // Create one picker control by surface
            _emojiPickerHost = _parent.Controls.Find("EmojiPickerHost", false).OfType<ElementHost>().FirstOrDefault();
            if (_emojiPickerHost == null)
            {
                _emojiPicker = new Picker();
                _emojiPicker.Picked += (_, args) =>
                {
                    _currentContainer.Emoji = args.Emoji;
                    _currentContainer.Invalidate();
                };

                _emojiPickerHost = new ElementHost();
                _emojiPickerHost.Dock = DockStyle.None;
                _emojiPickerHost.Child = _emojiPicker;
                _emojiPickerHost.Name = "EmojiPickerHost";

                _parent.Controls.Add(_emojiPickerHost);
            }
        }

        private void HideEmojiPicker()
        {
            _emojiPicker?.ShowPopup(false);
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();

            // Create WPF control that will be used to render the emoji
            _image = new System.Windows.Controls.Image();
            global::Emoji.Wpf.Image.SetSource(_image, Emoji);

            PropertyChanged += OnPropertyChanged;
        }

        public override void Transform(Matrix matrix)
        {
            _rotationAngle += CalculateAngle(matrix);
            _rotationAngle %= 360;

            base.Transform(matrix);
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Selected)))
            {
                if (!Selected)
                {
                    HideEmojiPicker();
                }
                else if (Status == EditStatus.IDLE && _justCreated)
                {
                    // Show picker just after creation
                    ShowEmojiPicker();
                    _justCreated = false;
                }
            }
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);

            var iconSize = Math.Min(rect.Width, rect.Height);
            if (iconSize > 0)
            {
                using var bitmap = GetBitmap(iconSize);
                graphics.DrawImage(bitmap, Bounds);
            }
        }

        private Image GetBitmap(int iconSize)
        {
            _image.Measure(new Size(iconSize, iconSize));
            _image.Arrange(new Rect(0, 0, iconSize, iconSize));

            var renderTargetBitmap = new RenderTargetBitmap(iconSize, iconSize, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(_image);

            var bitmap = renderTargetBitmap.ToBitmap();

            if (_rotationAngle != 0)
            {
                var newBitmap = bitmap.Rotate( _rotationAngle);
                bitmap.Dispose();

                return newBitmap;
            }

            return bitmap;
        }

        
    }

    internal static class PickerExtensions
    {
        public static void ShowPopup(this Picker picker, bool show)
        {
            foreach (var child in picker.Children)
            {
                if (child is ToggleButton button)
                {
                    button.IsChecked = show;
                }
            }
        }
    }
}