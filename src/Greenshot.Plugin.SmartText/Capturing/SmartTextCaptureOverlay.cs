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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.SmartText.Detection;
using log4net;

namespace Greenshot.Plugin.SmartText.Capturing;

/// <summary>
/// While the text tool (T) of the capture window is active, the text found so far (application text and OCR, which arrive one after
/// the other) is scanned for smart text: the items are added to the features of the capture, the SmartTextHotspotTransformer
/// shows them as numbered hotspots of the text tool. The overlay draws nothing itself.
/// </summary>
internal sealed class SmartTextCaptureOverlay : CaptureOverlay
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextCaptureOverlay));

    /// <summary>The id of the text tool of the capture window (CaptureMode.Text)</summary>
    internal const string TextToolId = "Text";

    // More text often comes in a burst (one window after the other): scanned once it is quiet for a moment
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(100);

    private DispatcherTimer _timer;
    private IReadOnlyList<SmartTextDetector> _detectors;
    private string _scanned;

    public override void Attach(ICaptureToolHost host)
    {
        base.Attach(host);
        _timer = new DispatcherTimer { Interval = Quiet };
        _timer.Tick += (sender, args) => Scan();
    }

    public override void OnToolChanged() => ScanSoon();

    /// <summary>Called when the features changed (more text was found), the overlay itself draws nothing</summary>
    public override void Draw(DrawingContext drawingContext) => ScanSoon();

    private bool IsTextToolActive => Host?.ActiveTool?.Id == TextToolId;

    private void ScanSoon()
    {
        if (_timer == null || !IsTextToolActive)
        {
            return;
        }
        _timer.Stop();
        _timer.Start();
    }

    private void Scan()
    {
        _timer.Stop();
        var details = Host.Capture?.CaptureDetails;
        if (details == null || !IsTextToolActive)
        {
            return;
        }
        try
        {
            List<IOcrLineFeature> lines;
            lock (details.Features)
            {
                lines = details.Features.OfType<IOcrLineFeature>().ToList();
            }
            var bounds = new NativeRect(0, 0, Host.ScreenBounds.Width, Host.ScreenBounds.Height);
            _detectors ??= SmartTextDefinitions.Detectors(null, null, out _);
            var items = SmartTextScanner.Scan(TextLayout.From(lines, bounds), _detectors);

            // Adding the items changes the features again: only when something else was found
            string scanned = string.Join("\n", items.Select(item => $"{item.DetectorId}\t{item.Value}\t{item.Box.Bounds}"));
            if (scanned == _scanned)
            {
                return;
            }
            _scanned = scanned;
            lock (details.Features)
            {
                details.Features.RemoveAll(feature => feature is SmartTextItem);
                details.Features.AddRange(items);
            }
            (details as CaptureDetails)?.NotifyFeaturesChanged();
        }
        catch (Exception ex)
        {
            Log.Warn("Smart text couldn't scan the text of the capture window", ex);
        }
    }
}

/// <summary>
/// Adds the smart text overlay to every capture window
/// </summary>
internal sealed class SmartTextCaptureOverlayProvider : ICaptureOverlayProvider
{
    public IEnumerable<ICaptureOverlay> CreateOverlays()
    {
        yield return new SmartTextCaptureOverlay();
    }
}
