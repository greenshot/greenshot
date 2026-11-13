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

using System.Collections.Generic;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Languages;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The destination window on the Destination tab: where the preview is
    /// </summary>
    public partial class SettingsViewModel
    {
        private IList<DestinationWindowPreviewPositionChoice> _destinationWindowPreviewPositionChoices;

        /// <summary>
        /// The choices of the drop-down of the place of the preview
        /// </summary>
        public IList<DestinationWindowPreviewPositionChoice> DestinationWindowPreviewPositionChoices => _destinationWindowPreviewPositionChoices ??=
            new List<DestinationWindowPreviewPositionChoice>
            {
                new DestinationWindowPreviewPositionChoice(DestinationWindowPreviewPosition.Top, Texts.Settings.DestinationWindowPreviewTop),
                new DestinationWindowPreviewPositionChoice(DestinationWindowPreviewPosition.Left, Texts.Settings.DestinationWindowPreviewLeft)
            };
    }

    /// <summary>
    /// One choice of the drop-down of the place of the preview in the destination window
    /// </summary>
    public sealed class DestinationWindowPreviewPositionChoice
    {
        public DestinationWindowPreviewPositionChoice(DestinationWindowPreviewPosition value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public DestinationWindowPreviewPosition Value { get; }

        public string DisplayName { get; }
    }
}
