/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Image = System.Drawing.Image;

namespace Greenshot.Editor.Drawing.Emoji
{
    /// <summary>
    /// Description of EmojiContainer.
    /// </summary>
    [Serializable]
    public sealed class EmojiContainer : VectorGraphicsContainer, IEmojiContainer, IHaveScaleOptions
    {
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

        public override NativeSize DefaultSize => new(64, 64);

        public EmojiContainer(Surface parent, string emoji = null, int? size = null) : base(parent)
        {
            Emoji = emoji ?? EditorConfig.RecentEmoji;
            Width = size ?? DefaultSize.Width;
            Height = size ?? DefaultSize.Height;
            Init();
        }

        public override bool HandleMouseDown(int mouseX, int mouseY)
        {
            return base.HandleMouseDown(mouseX - (Width / 2), mouseY - (Height / 2));
        }

        public override bool HandleMouseMove(int x, int y)
        {
            Invalidate();
            Left = x - (Width / 2);
            Top = y - (Height / 2);
            Invalidate();
            return true;
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
