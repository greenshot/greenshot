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

using System.Windows.Forms;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// Service interface for converting abstract detected features into UI-specific interactive hotspots.
/// This allows presentation layer forms (e.g. CaptureForm) to render features without knowing
/// the underlying click actions, context menus, or business logic of specific plugins.
/// </summary>
public interface IFeatureHotspotTransformer
{
    /// <summary>
    /// Checks if this transformer is capable of converting the specified feature.
    /// </summary>
    /// <param name="feature">The detected feature to test.</param>
    /// <returns>True if the feature can be transformed; otherwise, false.</returns>
    bool CanTransform(IDetectedFeature feature);

    /// <summary>
    /// Transforms the abstract feature into a presentation-layer interactive UI hotspot.
    /// </summary>
    /// <param name="feature">The detected feature to transform.</param>
    /// <param name="captureForm">The active screenshot capture form acting as the parent UI context.</param>
    /// <returns>An interactive hotspot with boundaries and click action handlers, or null if transformation is not possible.</returns>
    CaptureFormHotspot Transform(IDetectedFeature feature, Form captureForm);
}
