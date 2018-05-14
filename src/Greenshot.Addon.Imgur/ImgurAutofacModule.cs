using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Imgur.ViewModels;
using Greenshot.Addons.Addons;

namespace Greenshot.Addon.Imgur
{
    /// <inheritdoc />
    public class ImgurAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<ImgurDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<ImgurConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
            builder
                .RegisterType<ImgurHistoryViewModel>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<ImgurApi>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
