/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Where an icon comes from: an embedded resource, an icon key (resolved by the IIconProvider of the key) or an image the UI made itself.
    /// The <see cref="SizedIconCache"/> loads the original once and makes the sizes from it; the UI only ever gets copies.
    /// </summary>
    public sealed class IconSource
    {
        private static long _ownedImageCounter;
        private readonly Func<int, Image> _load;
        private readonly Func<int, CancellationToken, Task<Image>> _loadAsync;
        private readonly Func<int, string> _variant;
        private Image _ownedImage;

        private IconSource(string key, Func<int, Image> load, Func<int, CancellationToken, Task<Image>> loadAsync, Func<int, string> variant = null)
        {
            Key = key;
            _load = load;
            _loadAsync = loadAsync;
            _variant = variant;
        }

        /// <summary>
        /// Identifies the icon, two sources with the same key show the same icon
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// True when the original can be loaded right away (embedded resources, own images), false when it needs to be loaded async (e.g. exe icons)
        /// </summary>
        public bool IsSynchronous => _load != null || IsOwnedImage;

        /// <summary>
        /// True for an image which belongs to this source only (e.g. the icon of a window), it is disposed when the source is released
        /// </summary>
        public bool IsOwnedImage { get; private set; }

        /// <summary>
        /// The key of the original which is used for the size: some sources have a small and a large original (e.g. exe icons)
        /// </summary>
        /// <param name="pixelSize">int with the size in pixels</param>
        /// <returns>string</returns>
        public string OriginalKey(int pixelSize)
        {
            var variant = _variant?.Invoke(pixelSize);
            return string.IsNullOrEmpty(variant) ? Key : Key + "|" + variant;
        }

        /// <summary>
        /// An image which is embedded in the assembly of the type, see <see cref="EmbeddedResources"/>
        /// </summary>
        /// <param name="owner">Type to which the resource belongs</param>
        /// <param name="name">string with the name of the resource, e.g. "btnSave.Image"</param>
        /// <returns>IconSource</returns>
        public static IconSource FromResource(Type owner, string name)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            return new IconSource("embedded:" + EmbeddedResources.ManifestName(owner, name), _ => EmbeddedResources.GetImage(owner, name), null);
        }

        /// <summary>
        /// An icon key of a destination descriptor or plugin, e.g. "resource:Close.Image" or "exe:0:c:\windows\system32\cmd.exe", see <see cref="DestinationIcons"/>
        /// </summary>
        /// <param name="iconKey">string</param>
        /// <returns>IconSource, null when the key is empty</returns>
        public static IconSource FromKey(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey))
            {
                return null;
            }

            if (iconKey.StartsWith(DestinationIcons.ResourcePrefix, StringComparison.Ordinal))
            {
                string resourceName = iconKey.Substring(DestinationIcons.ResourcePrefix.Length);
                return new IconSource(iconKey, _ => GreenshotResources.GetImage(resourceName), null);
            }

            return new IconSource(iconKey, null, (pixelSize, cancellationToken) => DestinationIcons.GetIconAsync(iconKey, pixelSize, cancellationToken),
                pixelSize => DestinationIcons.GetVariant(iconKey, pixelSize));
        }

        /// <summary>
        /// An image the caller made (e.g. the icon of a window), the source takes ownership and it's disposed when the source is released
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>IconSource, null when there is no image</returns>
        public static IconSource FromImage(Image image)
        {
            if (image == null)
            {
                return null;
            }

            var source = new IconSource("image:" + Interlocked.Increment(ref _ownedImageCounter), null, null)
            {
                IsOwnedImage = true,
                _ownedImage = image
            };
            return source;
        }

        /// <summary>
        /// Load the original synchronously, the caller (the cache) owns the result
        /// </summary>
        internal Image Load(int pixelSize)
        {
            if (IsOwnedImage)
            {
                return Interlocked.Exchange(ref _ownedImage, null);
            }

            return _load?.Invoke(pixelSize);
        }

        /// <summary>
        /// Load the original, the caller (the cache) owns the result
        /// </summary>
        internal Task<Image> LoadAsync(int pixelSize, CancellationToken cancellationToken)
        {
            if (_loadAsync != null)
            {
                return _loadAsync(pixelSize, cancellationToken);
            }

            return Task.FromResult(Load(pixelSize));
        }

        /// <summary>
        /// Dispose the owned image if the cache never took it
        /// </summary>
        internal void ReleaseOwnedImage()
        {
            Interlocked.Exchange(ref _ownedImage, null)?.Dispose();
        }

        public override string ToString() => Key;
    }
}
