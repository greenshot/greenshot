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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Icon keys of destination descriptors and their resolution through the registered <see cref="IIconProvider"/>s.
    /// Built in keys: "resource:Name" (Greenshot resources), "exe:index:path" (icon of an executable), "stock:Id" (an icon of Windows, e.g.
    /// the one for an error, see <see cref="StockIconId"/>), "greenshot" (the application icon).
    /// </summary>
    public static class DestinationIcons
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationIcons));
        private static readonly CoreIconProvider BuiltIn = new CoreIconProvider();

        public const string Greenshot = "greenshot";

        private const string StockPrefix = "stock:";

        /// <summary>
        /// The size of the icons of Windows ("stock:"), large enough for the big image of a toast
        /// </summary>
        public const int StockIconSize = 96;

        public static string Resource(string name) => name == null ? null : "resource:" + name;

        public static string Exe(string path, int index) => path == null ? null : string.Format(CultureInfo.InvariantCulture, "exe:{0}:{1}", index, path);

        /// <summary>
        /// The key of an icon of Windows, e.g. <see cref="StockIconId.Error"/>
        /// </summary>
        public static string Stock(StockIconId id) => StockPrefix + id;

        /// <summary>
        /// The icon of Windows in <see cref="StockIconSize"/>, synchronously (it needs nothing to wait for); the caller owns (disposes) it,
        /// null when Windows doesn't have it
        /// </summary>
        public static Image GetStockIcon(StockIconId id)
        {
            try
            {
                return StockIcons.GetIcon<Bitmap>(id, StockIconSize);
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't get the icon {id} of Windows", ex);
                return null;
            }
        }

        /// <summary>
        /// Resolve the icon, the caller owns (disposes) the image; null when unknown.
        /// </summary>
        public static async Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(iconKey))
            {
                return null;
            }

            try
            {
                if (BuiltIn.CanProvide(iconKey))
                {
                    return await BuiltIn.GetIconAsync(iconKey, cancellationToken).ConfigureAwait(false);
                }

                var providers = SimpleServiceProvider.Current?.GetAllInstances<IIconProvider>() ?? Enumerable.Empty<IIconProvider>();
                foreach (var provider in providers)
                {
                    if (provider.CanProvide(iconKey))
                    {
                        return await provider.GetIconAsync(iconKey, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't resolve icon '{iconKey}'", ex);
            }

            return null;
        }

        /// <summary>
        /// Resolve the icon as a frozen WPF image source (usable on any thread), null when unknown.
        /// </summary>
        public static async Task<System.Windows.Media.ImageSource> GetImageSourceAsync(string iconKey, CancellationToken cancellationToken = default)
        {
            using var image = await GetIconAsync(iconKey, cancellationToken).ConfigureAwait(false);
            if (image == null)
            {
                return null;
            }

            var imageSource = image.ToBitmapSource();
            imageSource?.Freeze();
            return imageSource;
        }

        /// <summary>
        /// The built in icons (resources, executables, the icons of Windows), which don't need to wait for anything.
        /// </summary>
        private sealed class CoreIconProvider : IIconProvider
        {
            public bool CanProvide(string iconKey) =>
                iconKey == Greenshot || iconKey.StartsWith("resource:", StringComparison.Ordinal) || iconKey.StartsWith("exe:", StringComparison.Ordinal)
                || iconKey.StartsWith(StockPrefix, StringComparison.Ordinal);

            public async Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
            {
                Image image = null;
                if (iconKey == Greenshot)
                {
                    using var icon = GreenshotResources.GetGreenshotIcon();
                    image = icon?.ToBitmap();
                }
                else if (iconKey.StartsWith(StockPrefix, StringComparison.Ordinal))
                {
                    if (Enum.TryParse<StockIconId>(iconKey.Substring(StockPrefix.Length), out var id))
                    {
                        image = GetStockIcon(id);
                    }
                }
                else if (iconKey.StartsWith("resource:", StringComparison.Ordinal))
                {
                    // A new image is created every time
                    image = GreenshotResources.GetImage(iconKey.Substring("resource:".Length));
                }
                else
                {
                    // exe:index:path
                    var parts = iconKey.Split(new[] { ':' }, 3);
                    if (parts.Length == 3 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    {
                        var cached = await PluginUtils.GetCachedExeIconAsync(parts[2], index, cancellationToken).ConfigureAwait(false);
                        if (cached != null)
                        {
                            // The cache owns the icon, the caller gets a copy
                            lock (cached)
                            {
                                image = ImageHelper.Clone(cached);
                            }
                        }
                    }
                }

                return image;
            }
        }
    }

    /// <summary>
    /// <see cref="IIconProvider"/> for the images in the embedded resources of a (plugin) type: key "prefix:ResourceName".
    /// See <see cref="EmbeddedResources"/> for the naming of the resources.
    /// </summary>
    public sealed class ResourceIconProvider : IIconProvider
    {
        private readonly string _prefix;
        private readonly Type _resourceType;

        public ResourceIconProvider(string prefix, Type resourceType)
        {
            _prefix = (prefix ?? throw new ArgumentNullException(nameof(prefix))) + ":";
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        }

        public string KeyFor(string resourceName) => _prefix + resourceName;

        public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(_prefix, StringComparison.Ordinal);

        public Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
        {
            // A new image is created every time, the caller owns it
            return Task.FromResult(EmbeddedResources.GetImage(_resourceType, iconKey.Substring(_prefix.Length)));
        }
    }
}
