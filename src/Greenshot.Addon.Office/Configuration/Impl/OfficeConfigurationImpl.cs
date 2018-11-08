using Dapplo.Config.Ini;
using Greenshot.Addon.Office.OfficeInterop;
using Microsoft.Office.Interop.PowerPoint;

namespace Greenshot.Addon.Office.Configuration.Impl
{
    public class OfficeConfigurationImpl : IniSectionBase<IOfficeConfiguration>, IOfficeConfiguration
    {
        #region Implementation of IOfficeConfiguration

        public EmailFormat OutlookEmailFormat { get; set; }
        public string EmailSubjectPattern { get; set; }
        public string EmailTo { get; set; }
        public string EmailCC { get; set; }
        public string EmailBCC { get; set; }
        public bool OutlookAllowExportInMeetings { get; set; }
        public bool WordLockAspectRatio { get; set; }
        public bool PowerpointLockAspectRatio { get; set; }
        public PpSlideLayout PowerpointSlideLayout { get; set; }

        #endregion
    }
}
