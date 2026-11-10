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

using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.ViewModels;
using log4net;

namespace Greenshot.Plugin.SmartText.Capturing;

/// <summary>
/// A smart text item becomes a numbered hotspot of the text tool: its box with a number badge and a row in the list,
/// "Open" (web address, e-mail, phone) as the default action and "Copy". Both end the capture.
/// </summary>
internal sealed class SmartTextHotspotTransformer : IFeatureHotspotTransformer
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextHotspotTransformer));

    public bool CanTransform(IDetectedFeature feature) => feature is SmartTextItem;

    public CaptureFormHotspot Transform(IDetectedFeature feature)
    {
        if (feature is not SmartTextItem item)
        {
            return null;
        }
        // The labels and the action of the Smart text window
        var shown = new SmartTextItemViewModel(item, 0);
        var hotspot = new CaptureFormHotspot
        {
            Bounds = item.Box.Bounds,
            Outline = item.Box.IsRotated ? item.Box.Corners().Select(corner => new NativePointFloat((float)corner.X, (float)corner.Y)).ToList() : null,
            Text = shown.Label,
            ToolTipText = item.Value,
            ToolId = SmartTextCaptureOverlay.TextToolId,
            IsNumbered = true
        };
        string target = shown.ActionTarget;
        if (target != null)
        {
            hotspot.Actions.Add(new CaptureHotspotAction(shown.ActionText, () => SmartTextActions.Open(target)));
        }
        string value = item.Value;
        hotspot.Actions.Add(new CaptureHotspotAction(SmartTextPlugin.Language.Copy,
            () => ClipboardService.Current.SetTextAsync(value).FireAndLog("Copy a smart text item to the clipboard", Log)));
        return hotspot;
    }
}
