using System.Collections.Generic;
using Greenshot.Addon.InternetExplorer;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Components
{
    /// <summary>
    /// This is the information which is needed for making captures possible.
    /// </summary>
    public class CaptureSupportInfo
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="internetExplorerCaptureHelper">InternetExplorerCaptureHelper</param>
        /// <param name="formEnhancers">IEnumerable with IFormEnhancer</param>
        public CaptureSupportInfo(
            InternetExplorerCaptureHelper internetExplorerCaptureHelper,
            IEnumerable<IFormEnhancer> formEnhancers = null
            )
        {
            InternetExplorerCaptureHelper = internetExplorerCaptureHelper;
            FormEnhancers = formEnhancers;
        }

        public InternetExplorerCaptureHelper InternetExplorerCaptureHelper { get; }

        public IEnumerable<IFormEnhancer> FormEnhancers { get; }
    }
}
