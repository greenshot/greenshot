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
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Recipes.Views
{
    /// <summary>
    /// How the destination window (DynamicDestinationWindow) looks and behaves. A value which isn't set (null) is taken from the settings
    /// (ICoreConfiguration), e.g. a step of a recipe sets only what it chose itself.
    /// </summary>
    public sealed class DestinationWindowOptions
    {
        /// <summary>
        /// True: the window opens centered on the mouse pointer, false: in the middle of the screen (of the owner)
        /// </summary>
        public bool? AtMouse { get; set; }

        /// <summary>
        /// Show the capture as preview
        /// </summary>
        public bool? ShowPreview { get; set; }

        /// <summary>
        /// Where the preview is: above the list or left of it
        /// </summary>
        public DestinationWindowPreviewPosition? PreviewPosition { get; set; }

        /// <summary>
        /// Show what is known of the capture (window title, time, OCR, HDR) below the preview
        /// </summary>
        public bool? ShowMetadata { get; set; }

        /// <summary>
        /// Close when something outside of the window is clicked, like a context menu; the window has no title bar then
        /// </summary>
        public bool? CloseOnDeactivate { get; set; }

        /// <summary>
        /// The window is as high as all the entries of the list need (at most as high as the screen)
        /// </summary>
        public bool? AutoSize { get; set; }

        /// <summary>
        /// Show the editor recipes (the list and its button), which change the capture
        /// </summary>
        public bool? ShowRecipes { get; set; }

        /// <summary>
        /// The options with every value set: the ones which aren't set here come from the settings
        /// </summary>
        public DestinationWindowOptions Resolve(ICoreConfiguration configuration) => new DestinationWindowOptions
        {
            AtMouse = AtMouse ?? configuration?.DestinationWindowAtMouse ?? true,
            ShowPreview = ShowPreview ?? configuration?.DestinationWindowShowPreview ?? true,
            PreviewPosition = PreviewPosition ?? configuration?.DestinationWindowPreviewPosition ?? DestinationWindowPreviewPosition.Top,
            ShowMetadata = ShowMetadata ?? configuration?.DestinationWindowShowMetadata ?? false,
            CloseOnDeactivate = CloseOnDeactivate ?? configuration?.DestinationWindowCloseOnDeactivate ?? false,
            AutoSize = AutoSize ?? configuration?.DestinationWindowAutoSize ?? false,
            ShowRecipes = ShowRecipes ?? configuration?.DestinationWindowShowRecipes ?? true
        };

        /// <summary>
        /// The preview position of a step parameter: Top or Left, null for Default (the setting) or anything else
        /// </summary>
        public static DestinationWindowPreviewPosition? ParsePreviewPosition(string value) =>
            string.Equals(value, nameof(DestinationWindowPreviewPosition.Top), StringComparison.OrdinalIgnoreCase) ? DestinationWindowPreviewPosition.Top
            : string.Equals(value, nameof(DestinationWindowPreviewPosition.Left), StringComparison.OrdinalIgnoreCase) ? DestinationWindowPreviewPosition.Left
            : null;

        /// <summary>
        /// The window position of a step parameter: true for Mouse, false for Center, null for Default (the setting) or anything else
        /// </summary>
        public static bool? ParseAtMouse(string value) =>
            string.Equals(value, "Mouse", StringComparison.OrdinalIgnoreCase) ? true
            : string.Equals(value, "Center", StringComparison.OrdinalIgnoreCase) ? false
            : null;
    }
}
