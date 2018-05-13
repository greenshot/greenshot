using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Box.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.Box
{
    /// <inheritdoc />
    public class BoxAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<BoxDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<BoxConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
