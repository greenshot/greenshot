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
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Icon keys of destination descriptors and their resolution through the registered <see cref="IIconProvider"/>s.
    /// Built in keys: "resource:Name" (Greenshot resources), "exe:index:path" (icon of an executable), "greenshot" (the application icon).
    /// </summary>
    public static class DestinationIcons
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationIcons));
        private static readonly CoreIconProvider BuiltIn = new CoreIconProvider();

        public const string Greenshot = "greenshot";

        public static string Resource(string name) => name == null ? null : "resource:" + name;

        public static string Exe(string path, int index) => path == null ? null : string.Format(CultureInfo.InvariantCulture, "exe:{0}:{1}", index, path);

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
        /// The built in icons (resources, executables), which don't need to wait for anything.
        /// </summary>
        private sealed class CoreIconProvider : IIconProvider
        {
            public bool CanProvide(string iconKey) =>
                iconKey == Greenshot || iconKey.StartsWith("resource:", StringComparison.Ordinal) || iconKey.StartsWith("exe:", StringComparison.Ordinal);

            public Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
            {
                Image image = null;
                if (iconKey == Greenshot)
                {
                    using var icon = GreenshotResources.GetGreenshotIcon();
                    image = icon?.ToBitmap();
                }
                else if (iconKey.StartsWith("resource:", StringComparison.Ordinal))
                {
                    // The resource manager creates a new image every time
                    image = GreenshotResources.GetImage(iconKey.Substring("resource:".Length));
                }
                else
                {
                    // exe:index:path
                    var parts = iconKey.Split(new[] { ':' }, 3);
                    if (parts.Length == 3 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    {
                        var cached = PluginUtils.GetCachedExeIcon(parts[2], index);
                        image = cached == null ? null : ImageHelper.Clone(cached);
                    }
                }

                return Task.FromResult(image);
            }
        }
    }

    /// <summary>
    /// <see cref="IIconProvider"/> for the images in the resources of a (plugin) type: key "prefix:ResourceName".
    /// </summary>
    public sealed class ResourceIconProvider : IIconProvider
    {
        private readonly string _prefix;
        private readonly ComponentResourceManager _resources;

        public ResourceIconProvider(string prefix, Type resourceType)
        {
            _prefix = (prefix ?? throw new ArgumentNullException(nameof(prefix))) + ":";
            _resources = new ComponentResourceManager(resourceType ?? throw new ArgumentNullException(nameof(resourceType)));
        }

        public string KeyFor(string resourceName) => _prefix + resourceName;

        public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(_prefix, StringComparison.Ordinal);

        public Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
        {
            // The resource manager creates a new image every time, the caller owns it
            return Task.FromResult(_resources.GetObject(iconKey.Substring(_prefix.Length)) as Image);
        }
    }
}
