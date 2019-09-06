using System.Collections.Generic;
using Greenshot.Addons.Interfaces;
using Microsoft.AppCenter.Analytics;

namespace Greenshot.Helpers
{
    public static class AppCenterExtensions
    {
        public static void HandleAppCenterEvent(this ExportInformation exportInformation)
        {
            if (exportInformation == null)
            {
                return;
            }

            if (exportInformation.IsError)
            {
                Analytics.TrackEvent("ExportError", new Dictionary<string, string> {
                    { "DestinationDescription", exportInformation.DestinationDescription },
                    { "Destination", exportInformation.DestinationDesignation},
                    { "ErrorMessage", exportInformation.ErrorMessage}
                });
                return;
            }
            
            Analytics.TrackEvent("Export", new Dictionary<string, string> {
                { "DestinationDescription", exportInformation.DestinationDescription },
                { "Destination", exportInformation.DestinationDesignation}
            });
        }
    }
}
