using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Confluence.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.Confluence
{
    /// <inheritdoc />
    public class ConfluenceAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<ConfluenceDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<ConfluenceConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
