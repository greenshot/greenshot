using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Flickr.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.Flickr
{
    /// <inheritdoc />
    public class FlickrAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<FlickrDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<FlickrConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
