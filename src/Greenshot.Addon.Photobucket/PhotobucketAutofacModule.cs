using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Photobucket.ViewModels;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.Photobucket
{
    /// <inheritdoc />
    public class PhotobucketAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<PhotobucketDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<PhotobucketConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
        }
    }
}
