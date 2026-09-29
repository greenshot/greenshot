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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Zxing
{
    public class ZxingQrDestination : DestinationBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ZxingQrDestination));

        /// <summary>
        /// The icon of the destination: the QR icon of imageres.dll
        /// </summary>
        public static string IconKey { get; } = DestinationIcons.Exe(FilenameHelper.FillCmdVariables(@"%windir%\system32\imageres.dll"), 97);

        public override string Designation => "ZxingQrDestination";

        public override DestinationDescriptor Descriptor { get; } = new DestinationDescriptor("QR Code Actions", 4, IconKey, hasDynamicDestinations: true);

        public override bool IsAvailableFor(ICaptureDetails captureDetails)
        {
            if (!base.IsAvailableFor(captureDetails))
            {
                return false;
            }

            if (captureDetails == null)
            {
                // Active capture is not provided (e.g. tray quick settings or settings form),
                // so we want it to be generally active/available.
                return true;
            }

            lock (captureDetails.Features)
            {
                return captureDetails.Features.OfType<IBarcodeFeature>().Any();
            }
        }

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            // The metadata is optional (e.g. an export from the editor without capture details)
            var captureDetails = request.Metadata ?? new CaptureDetails();
            List<IBarcodeFeature> qrFeatures;
            lock (captureDetails.Features)
            {
                qrFeatures = captureDetails.Features.OfType<IBarcodeFeature>().ToList();
            }

            // If not pre-scanned, scan the capture on-the-fly
            if (!qrFeatures.Any())
            {
                try
                {
                    using var lease = await request.Source.RenderAsync(new SurfaceOutputSettings(), cancellationToken).ConfigureAwait(false);
                    ScanForBarcodes(lease.Image, captureDetails, qrFeatures);
                }
                catch (Exception scanEx) when (scanEx is not OperationCanceledException)
                {
                    Log.Warn("ZxingQrDestination: Error scanning surface for barcodes", scanEx);
                }
            }

            if (!qrFeatures.Any())
            {
                // No QR codes detected on image: notify user without crashing
                await request.Ui.NotifyAsync(new Notification(NotificationKind.Info, "No QR codes or barcodes detected in capture.")).ConfigureAwait(false);
                return ExportResult.Succeeded(clearsModified: false);
            }

            var sb = new System.Text.StringBuilder();
            foreach (var feature in qrFeatures)
            {
                sb.AppendLine(feature.RawText);
            }

            var fullText = sb.ToString().TrimEnd();
            if (!string.IsNullOrWhiteSpace(fullText))
            {
                await ClipboardService.Current.SetTextAsync(fullText, cancellationToken).ConfigureAwait(false);
            }

            return ExportResult.Succeeded(clearsModified: false);
        }

        private static void ScanForBarcodes(Image image, ICaptureDetails captureDetails, List<IBarcodeFeature> qrFeatures)
        {
            if (image == null)
            {
                return;
            }

            // The rendered image is borrowed from the lease, the reader gets its own copy
            using var bmp = image is Bitmap b ? (Bitmap)b.Clone() : new Bitmap(image);
            var reader = new ZXing.BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true
                }
            };
            var results = reader.DecodeMultiple(bmp);
            if (results == null || results.Length == 0)
            {
                return;
            }

            lock (captureDetails.Features)
            {
                foreach (var res in results)
                {
                    if (string.IsNullOrEmpty(res?.Text))
                    {
                        continue;
                    }

                    var bounds = Dapplo.Windows.Common.Structs.NativeRect.Empty;
                    if (res.ResultPoints != null && res.ResultPoints.Length > 0)
                    {
                        float minX = res.ResultPoints.Min(p => p.X);
                        float minY = res.ResultPoints.Min(p => p.Y);
                        float maxX = res.ResultPoints.Max(p => p.X);
                        float maxY = res.ResultPoints.Max(p => p.Y);
                        bounds = new Dapplo.Windows.Common.Structs.NativeRect((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    }

                    var detected = new DetectedBarcode(bounds, res.BarcodeFormat.ToString(), res.Text);
                    captureDetails.Features.Add(detected);
                    qrFeatures.Add(detected);
                }
            }
        }

        public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails captureDetails, CancellationToken cancellationToken)
        {
            var destinations = new List<IDestination>();
            if (captureDetails == null)
            {
                return new ValueTask<IReadOnlyList<IDestination>>(destinations);
            }

            List<IBarcodeFeature> qrFeatures;
            lock (captureDetails.Features)
            {
                qrFeatures = captureDetails.Features.OfType<IBarcodeFeature>().ToList();
            }

            foreach (var feature in qrFeatures)
            {
                var text = feature.RawText;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var truncatedText = Truncate(text, 30);

                // Copy QR Code action
                destinations.Add(new QrActionDestination(
                    Designation + "_copy_" + text.GetHashCode(),
                    $"Copy: \"{truncatedText}\"",
                    (request, token) => ClipboardService.Current.SetTextAsync(text, token)
                ));

                // Open URL action
                bool isValidUrl = Uri.TryCreate(text, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isValidUrl)
                {
                    destinations.Add(new QrActionDestination(
                        Designation + "_open_" + text.GetHashCode(),
                        $"Open: \"{truncatedText}\"",
                        (request, token) => OpenUrlAsync(request, text)
                    ));
                }
            }

            return new ValueTask<IReadOnlyList<IDestination>>(destinations);
        }

        private static Task OpenUrlAsync(ExportRequest request, string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                })?.Dispose();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open URL in browser", ex);
                return request.Ui.NotifyAsync(new Notification(NotificationKind.Error, "Failed to open URL in browser: " + ex.Message));
            }
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }

        private class QrActionDestination : DestinationBase
        {
            private readonly Func<ExportRequest, CancellationToken, Task> _action;

            public QrActionDestination(string designation, string description, Func<ExportRequest, CancellationToken, Task> action)
            {
                Designation = designation;
                Descriptor = new DestinationDescriptor(description, iconKey: IconKey);
                _action = action;
            }

            public override string Designation { get; }

            public override DestinationDescriptor Descriptor { get; }

            public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
            {
                await _action(request, cancellationToken).ConfigureAwait(false);
                return ExportResult.Succeeded(clearsModified: false);
            }
        }
    }
}
