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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using ZXing;

namespace Greenshot.Plugin.Zxing;

public class ZxingCaptureProcessor : AbstractProcessor
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ZxingCaptureProcessor));
    private readonly IZxingConfiguration _config;

    public override string Designation => "ZxingCaptureProcessor";
    public override string Description => "ZXing Barcode Scanner";
    public override bool isActive => _config.ScanOnCapture;

    public ZxingCaptureProcessor(IZxingConfiguration config)
    {
        _config = config;
    }

    public override bool ProcessCapture(ICapture capture)
    {
        if (capture == null || capture.Image == null || capture.CaptureDetails == null)
        {
            return false;
        }

        try
        {
            // Optimization: Skip scanning if we have already scanned and stored features for this capture.
            if (capture.CaptureDetails.Features.Any())
            {
                return false;
            }

            using (var bitmap = new Bitmap(capture.Image))
            {
                var reader = new BarcodeReader
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = true,
                        TryInverted = true,
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                    }
                };

                var results = reader.DecodeMultiple(bitmap);
                if (results != null)
                {
                    var detectedFeatures = new List<IDetectedFeature>();
                    foreach (var result in results)
                    {
                        var points = result.ResultPoints;
                        if (points == null || points.Length == 0)
                        {
                            continue;
                        }

                        // Compute bounding box
                        float minX = float.MaxValue, minY = float.MaxValue;
                        float maxX = float.MinValue, maxY = float.MinValue;
                        foreach (var pt in points)
                        {
                            if (pt.X < minX) minX = pt.X;
                            if (pt.X > maxX) maxX = pt.X;
                            if (pt.Y < minY) minY = pt.Y;
                            if (pt.Y > maxY) maxY = pt.Y;
                        }

                        var rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                        rect.Inflate(5, 5); // Add a small padding

                        detectedFeatures.Add(new DetectedBarcode(
                            rect,
                            result.BarcodeFormat.ToString(),
                            result.Text
                        ));
                    }

                    if (detectedFeatures.Count > 0)
                    {
                        capture.CaptureDetails.Features.AddRange(detectedFeatures);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error scanning for QR codes during capture processing", ex);
        }
        
        return false;
    }
}
