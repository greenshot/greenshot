using Autofac;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.Win10
{
    /// <inheritdoc />
    public class Win10AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // TODO: Check Winows version
            builder
                .RegisterType<Win10OcrDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<Win10ShareDestination>()
                .As<IDestination>()
                .SingleInstance();
        }
    }
}
