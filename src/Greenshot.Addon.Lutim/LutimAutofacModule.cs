using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Lutim.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.Lutim
{
    /// <inheritdoc />
    public class LutimAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<LutimDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<LutimConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
