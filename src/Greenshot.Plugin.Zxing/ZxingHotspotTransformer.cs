/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Zxing;

public class ZxingHotspotTransformer : IFeatureHotspotTransformer
{
    public bool CanTransform(IDetectedFeature feature)
    {
        return feature is IBarcodeFeature;
    }

    public CaptureFormHotspot Transform(IDetectedFeature feature, Form captureForm)
    {
        if (feature is not IBarcodeFeature barcodeFeature)
        {
            return null;
        }

        string textContent = barcodeFeature.RawText;

        return new CaptureFormHotspot
        {
            Bounds = (Rectangle)barcodeFeature.Bounds,
            Text = textContent,
            ToolTipText = textContent,
            ClickAction = (e) =>
            {
                var menu = new ContextMenuStrip();
                
                // Shorten text for preview if it's too long
                string previewText = textContent.Length > 30 ? textContent.Substring(0, 27) + "..." : textContent;

                var previewItem = new ToolStripMenuItem($"QR Code: {previewText}") { Enabled = false };
                menu.Items.Add(previewItem);
                menu.Items.Add(new ToolStripSeparator());

                var copyItem = new ToolStripMenuItem("Copy to Clipboard", null, (s, ev) =>
                {
                    try
                    {
                        Clipboard.SetText(textContent);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to copy to clipboard: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    captureForm.DialogResult = DialogResult.Cancel;
                    captureForm.Close();
                });
                menu.Items.Add(copyItem);

                bool isValidUrl = Uri.TryCreate(textContent, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isValidUrl)
                {
                    var openItem = new ToolStripMenuItem("Open URL in Browser", null, (s, ev) =>
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
                            MessageBox.Show("Failed to open URL in browser: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        captureForm.DialogResult = DialogResult.Cancel;
                        captureForm.Close();
                    });
                    menu.Items.Add(openItem);
                }

                menu.Show(captureForm, captureForm.PointToClient(Cursor.Position));
            }
        };
    }
}
