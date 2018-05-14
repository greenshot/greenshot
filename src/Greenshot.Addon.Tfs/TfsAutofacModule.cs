using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Tfs.ViewModels;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.Tfs
{
    /// <inheritdoc />
    public class TfsAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<TfsDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<TfsConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
            builder
                .RegisterType<TfsClient>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
