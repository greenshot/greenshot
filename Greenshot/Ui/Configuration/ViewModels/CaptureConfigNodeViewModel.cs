using System.ComponentModel.Composition;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Configuration;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    /// This represents a node in the config
    /// </summary>
    [Export(typeof(IConfigScreen))]
    public sealed class CaptureConfigNodeViewModel : ConfigNode
    {
        public IGreenshotLanguage GreenshotLanguage { get; }

        [ImportingConstructor]
        public CaptureConfigNodeViewModel(IGreenshotLanguage greenshotLanguage)
        {
            GreenshotLanguage = greenshotLanguage;

            // automatically update the DisplayName
            GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsCapture));

            // automatically update the DisplayName
            CanActivate = false;
            Id = nameof(ConfigIds.Capture);
        }
    }
}
