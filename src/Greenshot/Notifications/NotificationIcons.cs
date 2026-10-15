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
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Notifications
{
    /// <summary>
    /// The icons of the destinations as image files, a toast only shows files: one per icon key in
    /// %LocalAppData%\Greenshot\Notifications\Icons, written the first time a notification needs it. Not in Greenshot Light.
    /// </summary>
    internal static class NotificationIcons
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationIcons));

        private static string IconDirectory => Path.Combine(GreenshotEnvironment.LocalDataDirectory, "Notifications", "Icons");

        /// <summary>
        /// The icon files of the destinations, by the icon key of the destination
        /// </summary>
        /// <returns>Empty when the notifications don't show images (e.g. the balloons of the tray icon) or are turned off</returns>
        public static async Task<IReadOnlyDictionary<string, string>> GetIconPathsAsync(IEnumerable<IDestination> destinations, CancellationToken cancellationToken = default)
        {
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
            var notificationService = SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true);
            if (notificationService?.ShowsCaptures != true || !notificationService.IsEnabled)
            {
                return paths;
            }

            var iconKeys = destinations.Select(d => d?.Descriptor?.IconKey).Where(k => !string.IsNullOrEmpty(k)).Distinct(StringComparer.Ordinal);
            foreach (var iconKey in iconKeys)
            {
                var path = await GetIconPathAsync(iconKey, cancellationToken).ConfigureAwait(false);
                if (path != null)
                {
                    paths[iconKey] = path;
                }
            }
            return paths;
        }

        /// <summary>
        /// The file name of the icon of the icon key, the key can contain a path (e.g. the icon of an exe)
        /// </summary>
        internal static string GetFileName(string iconKey)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(iconKey));
            return string.Concat(hash.Take(16).Select(b => b.ToString("x2"))) + ".png";
        }

        private static async Task<string> GetIconPathAsync(string iconKey, CancellationToken cancellationToken)
        {
            var path = Path.Combine(IconDirectory, GetFileName(iconKey));
            if (File.Exists(path))
            {
                return path;
            }
            try
            {
                using var icon = await DestinationIcons.GetIconAsync(iconKey, cancellationToken).ConfigureAwait(false);
                if (icon == null)
                {
                    return null;
                }
                Directory.CreateDirectory(IconDirectory);
                // Written next to it and moved, a notification shown at the same time never reads half a file
                var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                icon.Save(temporaryPath, ImageFormat.Png);
                if (File.Exists(path))
                {
                    File.Delete(temporaryPath);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
                return path;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't write the icon '{iconKey}' for the notification.", ex);
                return null;
            }
        }
    }
}
