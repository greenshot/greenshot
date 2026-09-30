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

using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;

namespace Greenshot.Destinations
{
    /// <summary>
    /// This is the destination which allows the user to select the location via a file dialog.
    /// </summary>
    public class FileWithDialogDestination : DestinationBase
    {
        public override string Designation => nameof(WellKnownDestinations.FileDialog);

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(
            Language.GetString(LangKey.settings_destination_fileas), 0, DestinationIcons.Resource("Save.Image"), "Ctrl+Shift+S");

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            // Bug #2918756 don't overwrite path if SaveWithDialog returns null!
            var savedTo = await FileDestination.SaveWithDialogAsync(request, CoreConfiguration.OutputFileCopyPathToClipboard, cancellationToken).ConfigureAwait(false);
            return savedTo == null ? ExportResult.Declined : FileDestination.Saved(request.Metadata, savedTo);
        }
    }
}
