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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Helpers;
using Microsoft.Win32;
using Greenshot.Base.Languages;

namespace Greenshot.Destinations
{
    /// <summary>
    /// This is the EmailDestination, used for MAPI clients. The MAPI call runs on the "MAPI" STA worker.
    /// </summary>
    public class EmailDestination : DestinationBase
    {
        private static readonly bool HasMapiClient;
        private static readonly string MapiClient;

        static EmailDestination()
        {
            // Logic to decide what email implementation we use, Windows prioritizes HKCU over HKLM
            string mapiClientHkcu = RegistryHive.CurrentUser.ReadKey(@"Clients\Mail");
            string mapiClientHklm = RegistryHive.LocalMachine.ReadKey64Or32(@"Clients\Mail");
            MapiClient = !string.IsNullOrEmpty(mapiClientHkcu) ? mapiClientHkcu : mapiClientHklm;
            HasMapiClient = !string.IsNullOrEmpty(MapiClient);
        }

        public override string Designation => nameof(WellKnownDestinations.EMail);

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(
            MapiClient ?? Texts.Editor.Email, 3, DestinationIcons.Resource("Email.Image"), "Ctrl+E");

        /// <summary>
        /// Only with a MAPI client, and not when the Outlook destination of the Office plugin handles Outlook
        /// </summary>
        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && HasMapiClient && !OutlookDestinationHandlesMail(metadata);

        private static bool OutlookDestinationHandlesMail(ICaptureDetails metadata) =>
            MapiClient.IndexOf("outlook", StringComparison.OrdinalIgnoreCase) >= 0
            && DestinationHelper.GetDestination("Outlook")?.IsAvailableFor(metadata) == true;

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            string tmpFile = await ExportFiles.SaveNamedTmpFileAsync(request.Source, request.Metadata, new SurfaceOutputSettings(), cancellationToken).ConfigureAwait(false);
            await MapiMailMessage.SendImageAsync(tmpFile, request.Metadata?.Title, cancellationToken).ConfigureAwait(false);
            return ExportResult.Succeeded();
        }
    }
}
