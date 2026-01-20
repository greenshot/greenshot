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
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Controls.Emoji;
using Greenshot.Editor.Helpers;
using Image = System.Drawing.Image;

namespace Greenshot.Editor.Drawing.Emoji
{
    /// <summary>
    /// Description of EmojiContainer.
    /// </summary>
    public sealed class EmojiContainer : VectorGraphicsContainer, IEmojiContainer, IHaveScaleOptions
    {
        private static EmojiContainer _currentContainer;
        private static ElementHost _emojiPickerHost;
        private static EmojiPicker _emojiPicker;

        private bool _justCreated = true;

        private string _emoji;

        public string Emoji
        {
            get => _emoji;
            set
            {
                _emoji = value;
                ResetCachedBitmap();
            }
        }

        public EmojiContainer(Surface parent, string emoji, int size = 64) : base(parent)
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

            var absRectangle = Bounds;
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
            // TODO: This is not ideal, as we need to controls from the surface, should replace this with a different solution.
            _emojiPickerHost = _parent.Controls.Find("EmojiPickerHost", false).OfType<ElementHost>().FirstOrDefault();
            if (_emojiPickerHost != null)
            {
                return;
            }

            _emojiPicker = new EmojiPicker();
            _emojiPicker.Picked += (_, args) =>
            {
                _currentContainer.Emoji = args.Emoji;
                _currentContainer.Invalidate();
            };

            _emojiPickerHost = new ElementHost
            {
                Dock = DockStyle.None,
                Child = _emojiPicker,
                Name = "EmojiPickerHost"
            };

            _parent.Controls.Add(_emojiPickerHost);
        }

        private void HideEmojiPicker()
        {
            _emojiPicker?.ShowPopup(false);
        }

        /// <summary>
        /// Make sure we register the property changed, to manage the state of the picker
        /// </summary>
        private void Init()
        {
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>Sets <see cref="_justCreated"/> to <c>false</c> to prevent the 
        /// emoji picker from opening automatically.</remarks>
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            _justCreated = false;
        }


        /// <summary>
        /// Handle the state of the Emoji Picker
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">PropertyChangedEventArgs</param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals(nameof(Selected)))
            {
                return;
            }

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

        protected override Image ComputeBitmap()
        {
            var iconSize = Math.Min(Bounds.Width, Bounds.Height);
            if (iconSize <= 0)
            {
                return null;
            }

            var image = EmojiRenderer.GetBitmap(Emoji, iconSize);
            if (RotationAngle != 0)
            {
                var newImage = image.Rotate(RotationAngle);
                image.Dispose();
                return newImage;
            }

            return image;
        }

        public ScaleOptions GetScaleOptions()
        {
            return ScaleOptions.Rational;
        }
    }
}