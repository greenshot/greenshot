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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Editor.Destinations;
using log4net;

namespace Greenshot.Recipes.Pipeline
{
    /// <summary>
    /// The notification after an export: where the capture went (icon of the destination, the file or link), a preview, and the buttons
    /// Open, Send to (the destination picker) and Edit. How much it shows is the ExportNotificationDetail setting.
    /// The flow disposes its capture when it ends, so a copy is kept as .greenshot file for the buttons, and a preview as PNG.
    /// </summary>
    public static class ExportNotifications
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExportNotifications));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        // The size of the preview in the notification (Windows shows the inline image at most 364 pixels wide, twice that for high DPI)
        private const int PreviewMaxWidth = 728;
        private const int PreviewMaxHeight = 400;

        // Windows shows the app logo at 48 pixels, more for high DPI
        private const int IconSize = 96;

        // The copies are only needed while the notification can be clicked
        private static readonly TimeSpan KeepCopies = TimeSpan.FromDays(1);
        private static readonly TimeSpan NotificationTimeout = TimeSpan.FromHours(1);

        private static readonly ConcurrentDictionary<string, Task<string>> IconFiles = new ConcurrentDictionary<string, Task<string>>();

        /// <summary>
        /// The folder for the copies of the captures and the icons
        /// </summary>
        public static string Folder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Greenshot", "Notifications");

        /// <summary>
        /// What the notification about the export shows, null when it isn't shown
        /// </summary>
        public sealed class Content
        {
            public bool Succeeded { get; set; }

            public string Title { get; set; }

            public string Detail { get; set; }

            public bool ShowPreview { get; set; }

            public bool ShowButtons { get; set; }

            public bool CanOpen { get; set; }

            public bool CanEdit { get; set; }
        }

        /// <summary>
        /// What the notification about the export shows with the detail setting, null when there is none.
        /// A failure is always shown, with the buttons to edit the capture or send it somewhere else.
        /// </summary>
        /// <param name="detail">The setting</param>
        /// <param name="designation">The destination</param>
        /// <param name="target">Where the capture went, e.g. the name of the destination</param>
        /// <param name="result">The result of the export</param>
        public static Content Describe(NotificationDetail detail, string designation, string target, ExportResult result)
        {
            if (result == null || result.Status == ExportStatus.Declined)
            {
                return null;
            }

            // The editor shows the capture itself
            bool canEdit = !EditorDestination.DESIGNATION.Equals(designation, StringComparison.Ordinal);
            if (!result.IsSucceeded)
            {
                return new Content
                {
                    Succeeded = false,
                    Title = string.Format(Texts.Core.ExportedToError, target).TrimEnd(' ', ':'),
                    Detail = result.Error,
                    ShowPreview = detail == NotificationDetail.Full,
                    ShowButtons = true,
                    CanEdit = canEdit
                };
            }

            if (detail == NotificationDetail.ErrorsOnly)
            {
                return null;
            }

            bool canOpen = !string.IsNullOrEmpty(result.FilePath) || result.Uri != null;
            return new Content
            {
                Succeeded = true,
                Title = string.Format(Texts.Core.ExportedTo, target),
                Detail = result.Uri?.AbsoluteUri ?? result.FilePath,
                ShowPreview = detail == NotificationDetail.Full,
                ShowButtons = detail == NotificationDetail.Full,
                CanOpen = canOpen,
                CanEdit = canEdit
            };
        }

        /// <summary>
        /// Show the notification about the export, called on the UI thread while the capture still exists
        /// </summary>
        public static void Show(IDestination destination, ExportResult result, ISurface surface)
        {
            if (destination == null || surface == null)
            {
                return;
            }

            // The picker reports the export of the destination the user picked
            destination = result?.ExportedBy ?? destination;
            string target = result?.Target ?? destination.Descriptor?.DisplayName ?? destination.Designation;
            var content = Describe(CoreConfig.ExportNotificationDetail, destination.Designation, target, result);
            if (content == null)
            {
                return;
            }

            // The flow disposes the surface after the export: a copy of the capture with its elements (the mouse cursor, annotations),
            // and the image as exported for the preview. Only the copies are made here, the files are written on the pool.
            CaptureCopy copy = null;
            try
            {
                copy = new CaptureCopy
                {
                    Image = ImageHelper.Clone(surface.Image),
                    Elements = SaveElements(surface),
                    Exported = content.ShowPreview ? surface.GetImageForExport() : null
                };
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't copy the capture for the notification", ex);
                copy?.Dispose();
                copy = null;
            }

            ShowAsync(destination, result, content, copy, surface.CaptureDetails, UiDispatcher.Current).FireAndLog("Export notification", Log);
        }

        /// <summary>
        /// The copy of the capture for the notification
        /// </summary>
        private sealed class CaptureCopy : IDisposable
        {
            public Image Image;
            public byte[] Elements;
            public Image Exported;

            public void Dispose()
            {
                Image?.Dispose();
                Exported?.Dispose();
            }
        }

        private static byte[] SaveElements(ISurface surface)
        {
            using var stream = new MemoryStream();
            surface.SaveElementsToStream(stream);
            return stream.ToArray();
        }

        private static async Task ShowAsync(IDestination destination, ExportResult result, Content content, CaptureCopy copy, ICaptureDetails captureDetails, IUiDispatcher ui)
        {
            // Writing the files takes a moment for a big capture, not on the UI thread
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            string capturePath = null;
            string previewPath = null;
            using (copy)
            {
                if (copy != null)
                {
                    (capturePath, previewPath) = SaveCopies(copy);
                }
            }

            string iconPath = await GetIconPathAsync(destination.Descriptor?.IconKey).ConfigureAwait(false);
            var notification = new ExportNotification
            {
                Succeeded = content.Succeeded,
                Title = content.Title,
                Detail = content.Detail,
                IconPath = iconPath,
                PreviewPath = previewPath,
                ShowButtons = content.ShowButtons,
                Timeout = NotificationTimeout,
                Open = content.CanOpen ? CreateOpenAction(result) : null,
                SendTo = capturePath == null ? null : () => SendToAsync(capturePath, captureDetails).FireAndLog("Send the capture from the notification", Log),
                Edit = capturePath == null || !content.CanEdit ? null : () => EditAsync(capturePath, captureDetails, content.Succeeded).FireAndLog("Edit the capture from the notification", Log)
            };

            await ui.InvokeAsync(() =>
            {
                var notificationService = SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true);
                if (notificationService == null)
                {
                    Log.InfoFormat("Notification: {0} {1}", notification.Title, notification.Detail);
                    return;
                }

                notificationService.ShowExportNotification(notification);
            }, CancellationToken.None).ConfigureAwait(false);
        }

        private static Action CreateOpenAction(ExportResult result)
        {
            if (result.Uri != null)
            {
                string link = result.Uri.AbsoluteUri;
                return () => Process.Start(link);
            }

            string filePath = result.FilePath;
            return () => ExplorerHelper.OpenInExplorer(filePath);
        }

        /// <summary>
        /// Write the capture as .greenshot file (for Send to and Edit, with its elements) and a small preview as PNG (Windows shows
        /// the pictures of a notification from files), the old ones are removed
        /// </summary>
        private static (string CapturePath, string PreviewPath) SaveCopies(CaptureCopy copy)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                RemoveOldCopies();
                string name = Guid.NewGuid().ToString("N");
                string capturePath = Path.Combine(Folder, name + ".greenshot");
                using (var stream = File.Create(capturePath))
                {
                    ImageIO.WriteGreenshotFormat(copy.Image, copy.Elements, stream);
                }

                if (copy.Exported == null)
                {
                    return (capturePath, null);
                }

                string previewPath = Path.Combine(Folder, name + "-preview.png");
                using (var preview = CreatePreview(copy.Exported))
                {
                    preview.Save(previewPath, ImageFormat.Png);
                }

                return (capturePath, previewPath);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't keep a copy of the capture for the notification", ex);
                return (null, null);
            }
        }

        /// <summary>
        /// The capture made small enough for the notification
        /// </summary>
        internal static Bitmap CreatePreview(Image image)
        {
            double scale = Math.Min(1.0, Math.Min((double)PreviewMaxWidth / image.Width, (double)PreviewMaxHeight / image.Height));
            int width = Math.Max(1, (int)Math.Round(image.Width * scale));
            int height = Math.Max(1, (int)Math.Round(image.Height * scale));
            var preview = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(preview);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(image, 0, 0, width, height);
            return preview;
        }

        private static void RemoveOldCopies()
        {
            var tooOld = DateTime.Now - KeepCopies;
            // The icons stay, they are in a folder of their own
            foreach (var file in Directory.EnumerateFiles(Folder))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < tooOld)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"Couldn't remove the old copy {file}", ex);
                }
            }
        }

        /// <summary>
        /// The icon of the destination as PNG file, made once per icon
        /// </summary>
        private static async Task<string> GetIconPathAsync(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey))
            {
                return null;
            }

            try
            {
                return await IconFiles.GetOrAdd(iconKey, CreateIconFileAsync).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug($"No icon for the notification of {iconKey}", ex);
                IconFiles.TryRemove(iconKey, out _);
                return null;
            }
        }

        private static async Task<string> CreateIconFileAsync(string iconKey)
        {
            using var icon = await DestinationIcons.GetIconAsync(iconKey).ConfigureAwait(false);
            if (icon == null)
            {
                return null;
            }

            string iconFolder = Path.Combine(Folder, "Icons");
            Directory.CreateDirectory(iconFolder);
            string fileName = string.Concat(iconKey.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            string iconPath = Path.Combine(iconFolder, $"{fileName}-{iconKey.GetHashCode():X8}.png");
            using var bitmap = new Bitmap(IconSize, IconSize, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // Windows makes the logo bigger anyway, scaled here it stays sharper
                graphics.InterpolationMode = icon.Width * 2 <= IconSize ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(icon, 0, 0, IconSize, IconSize);
            }

            bitmap.Save(iconPath, ImageFormat.Png);
            return iconPath;
        }

        /// <summary>
        /// A surface with the kept copy of the capture, for another export
        /// </summary>
        private static ISurface CreateSurface(string capturePath, ICaptureDetails captureDetails, bool modified)
        {
            var surfaceFactory = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>();
            var surface = WinFormsContextGuard.CreateWithoutContext(() => surfaceFactory());
            ImageIO.LoadGreenshotSurface(capturePath, surface);
            surface.CaptureDetails = captureDetails;
            surface.Modified = modified;
            return surface;
        }

        /// <summary>
        /// Show the destination picker and export the capture there, until an export succeeded or the picker is closed.
        /// The result is shown as notification too.
        /// </summary>
        private static async Task SendToAsync(string capturePath, ICaptureDetails captureDetails)
        {
            if (!File.Exists(capturePath))
            {
                Log.WarnFormat("The capture {0} of the notification is gone", capturePath);
                return;
            }

            var choices = DestinationHelper.GetAllDestinations()
                .Where(d => !nameof(WellKnownDestinations.Picker).Equals(d.Designation, StringComparison.OrdinalIgnoreCase) && d.IsAvailableFor(captureDetails))
                .ToList();
            while (true)
            {
                var picked = await UserInteraction.Current.PickDestinationAsync(choices, captureDetails, CancellationToken.None).ConfigureAwait(false);
                if (picked == null)
                {
                    return;
                }

                var result = await ExportCopyAsync(picked, capturePath, captureDetails, false).ConfigureAwait(false);
                if (result.Status != ExportStatus.Failed)
                {
                    return;
                }
                // Failed: the notification says why, the picker comes again
            }
        }

        private static async Task EditAsync(string capturePath, ICaptureDetails captureDetails, bool exported)
        {
            if (!File.Exists(capturePath))
            {
                Log.WarnFormat("The capture {0} of the notification is gone", capturePath);
                return;
            }

            await ExportCopyAsync(DestinationHelper.GetDestination(EditorDestination.DESIGNATION), capturePath, captureDetails, !exported).ConfigureAwait(false);
        }

        private static async Task<ExportResult> ExportCopyAsync(IDestination destination, string capturePath, ICaptureDetails captureDetails, bool modified)
        {
            var ui = UiDispatcher.Current;
            var surface = await ui.InvokeAsync(() => CreateSurface(capturePath, captureDetails, modified), CancellationToken.None).ConfigureAwait(false);
            ExportResult result = null;
            try
            {
                // Where it went is shown like after the capture
                surface.SurfaceMessage += DestinationDispatcher.SurfaceMessageReceived;
                result = await DestinationExporter.ExportAsync(destination, surface, captureDetails, true, ui).ConfigureAwait(false);
                return result;
            }
            finally
            {
                if (result?.KeepsCapture != true)
                {
                    surface.Dispose();
                }
            }
        }
    }
}
