using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.OneDrive.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.OneDrive
{
    /// <inheritdoc />
    public class OneDriveAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<OneDriveDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<OneDriveConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
