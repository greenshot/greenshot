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
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Helpers;
using log4net;
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
        private static readonly ILog Log = LogManager.GetLogger(typeof(IconContainer));

        [NonSerialized] private static EmojiContainer _currentContainer;
        [NonSerialized] private static ElementHost _emojiPickerHost;
        [NonSerialized] private static Emoji.Wpf.Picker _emojiPicker;

        [NonSerialized] private System.Windows.Controls.Image _image;
        [NonSerialized] private bool _firstSelection = true;

        private string _emoji;

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

            CreatePickerControl();

            var absRectangle = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
            var displayRectangle = Parent.ToSurfaceCoordinates(absRectangle);
            _emojiPickerHost.Width = 0; // Trick to hide the picker's button
            _emojiPickerHost.Height = displayRectangle.Height;
            _emojiPickerHost.Left = displayRectangle.Left;
            _emojiPickerHost.Top = displayRectangle.Top;

            _emojiPicker.Selection = Emoji;
            _emojiPicker.ShowPopup = true;
        }

        private void CreatePickerControl()
        {
            if (_emojiPickerHost == null)
            {
                _emojiPicker = new Emoji.Wpf.Picker();
                _emojiPicker.Picked += (_, args) =>
                {
                    _currentContainer.Emoji = args.Emoji;
                    _currentContainer.Invalidate();
                };

                _emojiPickerHost = new ElementHost();
                _emojiPickerHost.Dock = DockStyle.None;
                _emojiPickerHost.Child = _emojiPicker;

                _parent.Controls.Add(_emojiPickerHost);
            }
        }

        private void HideEmojiPicker()
        {
            if (_emojiPicker != null)
            {
                _emojiPicker.ShowPopup = false;
            }
        }


        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();

            // Create WPF control
            _image = new System.Windows.Controls.Image();
            global::Emoji.Wpf.Image.SetSource(_image, Emoji);

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Selected)))
            {
                if (!Selected)
                {
                    HideEmojiPicker();
                }
                else if (Status == EditStatus.IDLE && _firstSelection)
                {
                    ShowEmojiPicker();
                    _firstSelection = false;
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
                _image.Measure(new Size(iconSize, iconSize));
                _image.Arrange(new Rect(0, 0, iconSize, iconSize));

                var renderTargetBitmap = new RenderTargetBitmap(iconSize, iconSize, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(_image);

                using var bitmap = BitmapFromSource(renderTargetBitmap);

                var rotationAngle = GetRotationAngle();
                if (rotationAngle != 0)
                {
                    graphics.DrawImage(RotateImage(bitmap, rotationAngle), Bounds);
                    return;
                }

                graphics.DrawImage(bitmap, Bounds);
            }
        }

        private int GetRotationAngle()
        {
            int rotationAngle = 0;
            if (Width < 0)
            {
                rotationAngle = Height > 0 ? 90 : 180;
            }
            else if (Height < 0)
            {
                rotationAngle = 270;
            }

            return rotationAngle;
        }

        private Bitmap BitmapFromSource(BitmapSource bitmapSource)
        {
            var src = new FormatConvertedBitmap();
            src.BeginInit();
            src.Source = bitmapSource;
            src.DestinationFormat = PixelFormats.Bgra32;
            src.EndInit();

            var bitmap = new Bitmap(src.PixelWidth, src.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            src.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bitmap.UnlockBits(data);

            return bitmap;
        }

        private static Image RotateImage(Image img, float rotationAngle)
        {
            var bitmap = new Bitmap(img.Width, img.Height);

            using var gfx = Graphics.FromImage(bitmap);
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

            gfx.TranslateTransform((float)bitmap.Width / 2, (float)bitmap.Height / 2);
            gfx.RotateTransform(rotationAngle);
            gfx.TranslateTransform(-(float)bitmap.Width / 2, -(float)bitmap.Height / 2);

            gfx.DrawImage(img, new Point(0, 0));

            return bitmap;
        }
    }
}