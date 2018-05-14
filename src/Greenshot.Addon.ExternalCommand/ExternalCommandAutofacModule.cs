using Autofac;
using Dapplo.Addons;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.ExternalCommand.ViewModels;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.ExternalCommand
{
    /// <inheritdoc />
    public class ExternalCommandAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<ExternalCommandDestinationProvider>()
                .As<IStartup>()
                .As<IDestinationProvider>()
                .SingleInstance();
            builder
                .RegisterType<ExternalCommandConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }

    }
}
