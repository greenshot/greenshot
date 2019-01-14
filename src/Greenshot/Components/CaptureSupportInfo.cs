using System.Collections.Generic;
using Greenshot.Addon.InternetExplorer;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Components
{
    /// <summary>
    /// This is the bundled information which is needed for making captures possible.
    /// </summary>
    public class CaptureSupportInfo
    {
        /// <summary>
        /// Constructor for DI
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="internetExplorerCaptureHelper">InternetExplorerCaptureHelper</param>
        /// <param name="formEnhancers">IEnumerable with IFormEnhancer</param>
        public CaptureSupportInfo(
            ICoreConfiguration coreConfiguration, 
            InternetExplorerCaptureHelper internetExplorerCaptureHelper,
            IEnumerable<IFormEnhancer> formEnhancers = null
            )
        {
            CoreConfiguration = coreConfiguration;
            InternetExplorerCaptureHelper = internetExplorerCaptureHelper;
            FormEnhancers = formEnhancers;
        }

        public ICoreConfiguration CoreConfiguration { get; }
        public InternetExplorerCaptureHelper InternetExplorerCaptureHelper { get; }

        public IEnumerable<IFormEnhancer> FormEnhancers { get; }
    }
}
