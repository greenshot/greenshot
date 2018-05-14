using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Dropbox.ViewModels;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.Dropbox
{
    /// <inheritdoc />
    public class DropboxAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<DropboxDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<DropboxConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
