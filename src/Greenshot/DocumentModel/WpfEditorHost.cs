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
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.WpfEditor;

namespace Greenshot.DocumentModel
{
    /// <summary>
    /// The bridge from the new editor to the destinations of Greenshot: the document becomes a surface (DocumentToSurface),
    /// which goes to the destination like an export from the old editor. Only in Debug builds.
    /// </summary>
    public sealed class WpfEditorHost : IWpfEditorHost
    {
        public static WpfEditorHost Instance { get; } = new();

        // The editor saves, copies and prints itself; the picker and the window of destinations show menus for a surface
        private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
        {
            WpfEditorDestination.DesignationName,
            nameof(WellKnownDestinations.Picker),
            nameof(WellKnownDestinations.DestinationWindow)
        };

        public IReadOnlyList<EditorDestination> Destinations =>
            DestinationHelper.GetAllDestinations()
                .Where(destination => !Excluded.Contains(destination.Designation) && destination.IsAvailableFor(null) && !destination.Descriptor.UseDynamicsOnly)
                .OrderBy(destination => destination.Descriptor.Priority)
                .ThenBy(destination => destination.Descriptor.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Select(destination => new EditorDestination(destination.Descriptor.DisplayName, (document, title) => ExportAsync(destination, document, title)))
                .ToList();

        private static async Task ExportAsync(IDestination destination, GreenshotDocument document, string title)
        {
            var surface = DocumentToSurface.ToSurface(document);
            if (!string.IsNullOrEmpty(title) && surface.CaptureDetails != null)
            {
                surface.CaptureDetails.Title = title;
            }

            var result = await DestinationExporter.ExportAsync(destination, surface, surface.CaptureDetails, manuallyInitiated: true);
            // The old editor keeps the surface it was given, every other destination is done with it
            if (destination.Designation != Greenshot.Editor.Destinations.EditorDestination.DESIGNATION)
            {
                surface.Dispose();
            }

            if (result is { Status: ExportStatus.Failed })
            {
                throw new InvalidOperationException(result.Error ?? destination.Descriptor.DisplayName);
            }
        }
    }
}
