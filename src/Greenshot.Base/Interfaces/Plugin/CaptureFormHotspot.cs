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
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// An area of the capture the user can click while selecting, e.g. a QR code. A click shows the actions in a menu,
/// choosing one runs it and ends the selection without a capture.
/// </summary>
public class CaptureFormHotspot
{
    /// <summary>
    /// The area, in coordinates of the capture
    /// </summary>
    public NativeRect Bounds { get; set; }

    /// <summary>
    /// The title of the menu
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Shown when the cursor is over the hotspot
    /// </summary>
    public string ToolTipText { get; set; }

    /// <summary>
    /// What the user can do with the hotspot
    /// </summary>
    public IList<CaptureHotspotAction> Actions { get; } = new List<CaptureHotspotAction>();
}

/// <summary>
/// An action of a hotspot, runs on the UI thread after the selection was closed
/// </summary>
public class CaptureHotspotAction
{
    public CaptureHotspotAction(string text, Action execute)
    {
        Text = text;
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// The text of the menu item
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Runs the action
    /// </summary>
    public Action Execute { get; }
}
