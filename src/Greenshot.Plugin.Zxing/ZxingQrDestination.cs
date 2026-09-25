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
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Zxing
{
    public class ZxingQrDestination : AbstractDestination
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ZxingQrDestination));

        public override string Designation => "ZxingQrDestination";
        public override string Description => "QR Code Actions";
        public override int Priority => 4;

        public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\imageres.dll"), 97);

        public override bool IsDynamic => true;
        public override bool UseDynamicsOnly => false;

        public override bool IsActiveFor(ICaptureDetails captureDetails)
        {
            if (!base.IsActiveFor(captureDetails))
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

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var exportInformation = new ExportInformation(Designation, Description);
            try
            {
                List<IBarcodeFeature> qrFeatures;
                lock (captureDetails.Features)
                {
                    qrFeatures = captureDetails.Features.OfType<IBarcodeFeature>().ToList();
                }

                // If not pre-scanned, scan the surface on-the-fly
                if (!qrFeatures.Any() && surface != null)
                {
                    try
                    {
                        using var image = surface.GetImageForExport();
                        if (image != null)
                        {
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
                            if (results != null && results.Length > 0)
                            {
                                lock (captureDetails.Features)
                                {
                                    foreach (var res in results)
                                    {
                                        if (!string.IsNullOrEmpty(res?.Text))
                                        {
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
                            }
                        }
                    }
                    catch (Exception scanEx)
                    {
                        Log.Warn("ZxingQrDestination: Error scanning surface for barcodes", scanEx);
                    }
                }

                if (qrFeatures.Any())
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var feature in qrFeatures)
                    {
                        sb.AppendLine(feature.RawText);
                    }
                    var fullText = sb.ToString().TrimEnd();
                    if (!string.IsNullOrWhiteSpace(fullText))
                    {
                        ClipboardHelper.SetClipboardData(fullText);
                    }
                    exportInformation.ExportMade = true;
                }
                else
                {
                    // No QR codes detected on image: notify user without crashing
                    exportInformation.ExportMade = true;
                    surface?.SendMessageEvent(this, SurfaceMessageTyp.Info, "No QR codes or barcodes detected in capture.");
                }
            }
            catch (Exception ex)
            {
                exportInformation.ExportMade = false;
                exportInformation.ErrorMessage = ex.Message;
            }
            ProcessExport(exportInformation, surface);
            return exportInformation;
        }

        public override IEnumerable<IDestination> DynamicDestinations(ICaptureDetails captureDetails)
        {
            if (captureDetails == null)
            {
                yield break;
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
                yield return new QrActionDestination(
                    Designation + "_copy_" + text.GetHashCode(),
                    $"Copy: \"{truncatedText}\"",
                    () => ClipboardHelper.SetClipboardData(text)
                );

                // Open URL action
                bool isValidUrl = Uri.TryCreate(text, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isValidUrl)
                {
                    yield return new QrActionDestination(
                        Designation + "_open_" + text.GetHashCode(),
                        $"Open: \"{truncatedText}\"",
                        () =>
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = text,
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Failed to open URL in browser", ex);
                                MessageBox.Show("Failed to open URL in browser: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    );
                }
            }
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }

        private class QrActionDestination : AbstractDestination
        {
            private readonly string _designation;
            private readonly string _description;
            private readonly Action _action;

            public QrActionDestination(string designation, string description, Action action)
            {
                _designation = designation;
                _description = description;
                _action = action;
            }

            public override string Designation => _designation;
            public override string Description => _description;

            public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
            {
                var exportInformation = new ExportInformation(Designation, Description);
                try
                {
                    _action();
                    exportInformation.ExportMade = true;
                }
                catch (Exception ex)
                {
                    exportInformation.ExportMade = false;
                    exportInformation.ErrorMessage = ex.Message;
                }
                ProcessExport(exportInformation, surface);
                return exportInformation;
            }
        }
    }
}
