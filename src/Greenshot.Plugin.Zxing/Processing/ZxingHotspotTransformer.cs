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
using System.Diagnostics;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.Zxing.Processing;

public class ZxingHotspotTransformer : IFeatureHotspotTransformer
{
    public bool CanTransform(IDetectedFeature feature)
    {
        return feature is IBarcodeFeature;
    }

    public CaptureFormHotspot Transform(IDetectedFeature feature)
    {
        if (feature is not IBarcodeFeature barcodeFeature)
        {
            return null;
        }

        string textContent = barcodeFeature.RawText;
        // Shorten text for preview if it's too long
        string previewText = textContent.Length > 30 ? textContent.Substring(0, 27) + "..." : textContent;

        var hotspot = new CaptureFormHotspot
        {
            Bounds = barcodeFeature.Bounds,
            Text = $"QR Code: {previewText}",
            ToolTipText = textContent
        };

        hotspot.Actions.Add(new CaptureHotspotAction("Copy to Clipboard", () =>
        {
            try
            {
                ClipboardHelper.SetClipboardData(textContent);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Failed to copy to clipboard: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }));

        bool isValidUrl = Uri.TryCreate(textContent, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        if (isValidUrl)
        {
            hotspot.Actions.Add(new CaptureHotspotAction("Open URL in Browser", () =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = textContent,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    ThemedMessageBox.Show("Failed to open URL in browser: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        return hotspot;
    }
}
