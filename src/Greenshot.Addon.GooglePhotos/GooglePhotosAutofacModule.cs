using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.GooglePhotos.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.GooglePhotos
{
    /// <inheritdoc />
    public class GooglePhotosAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<GooglePhotosDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<GooglePhotosConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
