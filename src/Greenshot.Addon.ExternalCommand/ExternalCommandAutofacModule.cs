using Autofac;
using Dapplo.Addons;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.ExternalCommand.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.ExternalCommand
{
    /// <inheritdoc />
    public class ExternalCommandAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<ExternalCommandStartup>()
                .As<IStartup>()
                .SingleInstance();
            builder
                .RegisterType<ExternalCommandConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }

    }
}
