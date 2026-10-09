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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Office.OfficeExport;
using Greenshot.Plugin.Office.OfficeInterop;
using Microsoft.Win32;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// Description of OutlookDestination.
    /// </summary>
    public class OutlookDestination : OfficeDestinationBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OutlookDestination));
        private const int IconApplication = 0;
        private const int IconMeeting = 2;

        private static readonly string MailIconKey = DestinationIcons.Resource("Email.Image");
        private static readonly IOfficeConfiguration OfficeConfig = IniConfigRegistry.GetSection<IOfficeConfiguration>();
        private static readonly string ExePath = GetComServerPath("Outlook.Application");
        private const string MapiClient = "Microsoft Outlook";
        private readonly string _outlookInspectorCaption;
        private readonly OlObjectClass _outlookInspectorType;
        private readonly OutlookEmailExporter _outlookEmailExporter = new();

        /// <summary>
        /// The new Outlook has no COM interface, when the user switched to it the classic Outlook shouldn't be used
        /// </summary>
        private static bool UsesNewOutlook()
        {
            using var preferences = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office\16.0\Outlook\Preferences", false);
            return preferences?.GetValue("UseNewOutlook") is int useNewOutlook && useNewOutlook != 0;
        }

        public OutlookDestination()
        {
        }

        public OutlookDestination(string outlookInspectorCaption, OlObjectClass outlookInspectorType)
        {
            _outlookInspectorCaption = outlookInspectorCaption;
            _outlookInspectorType = outlookInspectorType;
        }

        public override string Designation => "Outlook";

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(_outlookInspectorCaption ?? MapiClient, 3, IconKey, "Ctrl+E",
            hasDynamicDestinations: _outlookInspectorCaption == null);

        private string IconKey
        {
            get
            {
                if (_outlookInspectorCaption == null)
                {
                    return IconKeyFor(ExePath, IconApplication);
                }

                return OlObjectClass.olAppointment.Equals(_outlookInspectorType) ? IconKeyFor(ExePath, IconMeeting) : MailIconKey;
            }
        }

        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && ExePath != null && !UsesNewOutlook();

        public override async ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            if (_outlookInspectorCaption != null)
            {
                return await base.GetDynamicDestinationsAsync(metadata, cancellationToken).ConfigureAwait(false);
            }

            return await GetInspectorDestinationsAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<List<IDestination>> GetInspectorDestinationsAsync(CancellationToken cancellationToken)
        {
            var inspectorCaptions = await RunOnOfficeAsync(() => _outlookEmailExporter.RetrievePossibleTargets()?.ToList(), cancellationToken).ConfigureAwait(false);
            return inspectorCaptions?.Select(inspector => (IDestination) new OutlookDestination(inspector.Key, inspector.Value)).ToList() ?? new List<IDestination>();
        }

        /// <summary>
        /// Export the capture to outlook
        /// </summary>
        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (_outlookInspectorCaption == null && !request.ManuallyInitiated)
            {
                var inspectorDestinations = await GetInspectorDestinationsAsync(cancellationToken).ConfigureAwait(false);
                if (inspectorDestinations.Count > 0)
                {
                    inspectorDestinations.Insert(0, new OutlookDestination());
                    // A new e-mail or one of the open ones
                    return await PickAndExportAsync(request, inspectorDestinations, cancellationToken).ConfigureAwait(false);
                }
            }

            var captureDetails = request.Metadata;
            var (tmpFile, created) = await GetImageFileAsync(request, cancellationToken).ConfigureAwait(false);
            if (!created)
            {
                Log.InfoFormat("Using already available file: {0}", tmpFile);
            }

            // Create a attachment name for the image
            string attachmentName = captureDetails?.Title;
            if (!string.IsNullOrEmpty(attachmentName))
            {
                attachmentName = attachmentName.Trim();
            }

            // Set default if non is set
            if (string.IsNullOrEmpty(attachmentName))
            {
                attachmentName = "Greenshot Capture";
            }

            // Make sure it's "clean" so it doesn't corrupt the header
            attachmentName = Regex.Replace(attachmentName, @"[^\x20\d\w]", string.Empty);

            bool exported;
            if (_outlookInspectorCaption != null)
            {
                exported = await RunOnOfficeAsync(() => _outlookEmailExporter.ExportToInspector(_outlookInspectorCaption, tmpFile, attachmentName), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                string subject = FilenameHelper.FillPattern(OfficeConfig.EmailSubjectPattern, captureDetails, false, DateCultureMode.UILanguage);
                exported = await RunOnOfficeAsync(() => _outlookEmailExporter.ExportToOutlook(OfficeConfig.OutlookEmailFormat, tmpFile,
                    subject, attachmentName, OfficeConfig.EmailTo, OfficeConfig.EmailCC, OfficeConfig.EmailBCC), cancellationToken).ConfigureAwait(false);
            }

            return exported ? ExportResult.Succeeded() : ExportResult.Failed("Export to Outlook failed");
        }
    }
}
