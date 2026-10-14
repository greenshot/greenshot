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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Editor.Drawing;
using Greenshot.Recipes;
using Greenshot.Recipes.Pipeline;
using log4net;

namespace Greenshot.Notifications
{
    /// <summary>
    /// The captures which notifications (toasts) show and offer in the destination flyout (a recipe), also after a restart: what went to the destination,
    /// kept as .greenshot file with a thumbnail. Both are named by an id (UUID v7), which is the tag of the toast. They are
    /// removed when the toast is clicked, and once their toast is no longer in the notification center. Not in Greenshot Light.
    /// </summary>
    internal static class NotificationCaptures
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationCaptures));

        /// <summary>
        /// The largest thumbnail Windows shows in a toast without scaling it down
        /// </summary>
        private const int ThumbnailWidth = 364;
        private const int ThumbnailHeight = 180;

        private static string StoreDirectory => Path.Combine(GreenshotEnvironment.LocalDataDirectory, "Notifications");

        public static string GetThumbnailPath(string id) => Path.Combine(StoreDirectory, id + ".png");

        private static string GetCapturePath(string id) => Path.Combine(StoreDirectory, id + ".greenshot");

        /// <summary>
        /// Keeps the capture for a notification
        /// </summary>
        /// <returns>The id, null when it couldn't be kept</returns>
        public static string Store(ISurface surface)
        {
            // Only a notification which shows the capture needs it (a toast, not the balloon of the tray icon)
            var notificationService = SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true);
            if (surface?.Image == null || notificationService?.ShowsCaptures != true)
            {
                return null;
            }

            RemoveUnused(notificationService);
            string id = CreateId();
            try
            {
                Directory.CreateDirectory(StoreDirectory);
                using (var stream = File.Create(GetCapturePath(id)))
                {
                    ImageIO.SaveToStream(surface, stream, new SurfaceOutputSettings(WellKnownFileFormats.Greenshot));
                }
                using (var image = surface.GetImageForExport())
                using (var thumbnail = ImageHelper.CreateThumbnail(image, Math.Min(image.Width, ThumbnailWidth), -1, ThumbnailWidth, ThumbnailHeight))
                {
                    thumbnail.Save(GetThumbnailPath(id), ImageFormat.Png);
                }
                return id;
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't keep the capture for the notification.", ex);
                Delete(id);
                return null;
            }
        }

        /// <summary>
        /// Runs the notification recipe (the destination flyout) for the capture, on the UI thread. The capture file is removed,
        /// its toast is gone with the click.
        /// </summary>
        /// <returns>false when there is no capture with the id</returns>
        public static bool OfferCapture(string id)
        {
            // The id comes from the toast arguments, only an id may name a file
            if (!Guid.TryParseExact(id, "D", out _) || !File.Exists(GetCapturePath(id)))
            {
                return false;
            }

            ISurface surface;
            try
            {
                surface = ImageIO.LoadGreenshotSurface(GetCapturePath(id), new Surface());
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't load the capture of the notification.", ex);
                return false;
            }
            finally
            {
                Delete(id);
            }

            var recipe = RecipeManager.Instance.GetRecipeById(NotificationRecipes.RecipeIdNotification);
            if (recipe?.IsEnabled != true)
            {
                surface.Dispose();
                return false;
            }

            surface.CaptureDetails ??= new CaptureDetails();
            var payload = new CapturePayload
            {
                Surface = surface,
                RawCapture = new Capture(surface.GetImageForExport())
                {
                    CaptureDetails = surface.CaptureDetails
                }
            };
            CaptureFlowRunner.Current.Start(recipe, configure: context =>
            {
                context.Payload = payload;
                context.IsPayloadPreSupplied = true;
            });
            return true;
        }

        /// <summary>
        /// Removes the captures of the notifications the user can no longer click
        /// </summary>
        public static void RemoveUnused(INotificationService notificationService)
        {
            if (!Directory.Exists(StoreDirectory))
            {
                return;
            }

            try
            {
                var shownIds = notificationService.GetShownCaptureIds();
                // A clicked toast is gone from the notification center before Greenshot handles the click (also when Windows starts
                // Greenshot for it), so a capture is kept as long as its toast lives (an hour, see DestinationDispatcher.Notify)
                var keptBefore = DateTime.UtcNow - TimeSpan.FromHours(1);
                foreach (var file in Directory.GetFiles(StoreDirectory))
                {
                    string id = Path.GetFileNameWithoutExtension(file);
                    if (!shownIds.Contains(id, StringComparer.OrdinalIgnoreCase) && File.GetLastWriteTimeUtc(file) < keptBefore)
                    {
                        Delete(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't remove the captures of the notifications which are gone.", ex);
            }
        }

        private static void Delete(string id)
        {
            foreach (var path in new[] { GetCapturePath(id), GetThumbnailPath(id) })
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Couldn't remove {0}: {1}", path, ex.Message);
                }
            }
        }

        /// <summary>
        /// A UUID version 7 (RFC 9562): the time in milliseconds followed by random bits, so the files sort by their creation
        /// </summary>
        private static string CreateId()
        {
            var bytes = new byte[16];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }
            long milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (int i = 0; i < 6; i++)
            {
                bytes[i] = unchecked((byte)(milliseconds >> (40 - i * 8)));
            }
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            string hex = string.Concat(bytes.Select(b => b.ToString("x2")));
            return $"{hex.Substring(0, 8)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}-{hex.Substring(16, 4)}-{hex.Substring(20)}";
        }
    }
}
