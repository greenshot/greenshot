using Autofac;
using Dapplo.Addons;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Security;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Components;
using Greenshot.Forms;
using Greenshot.Ui.Misc.ViewModels;

namespace Greenshot
{
    /// <inheritdoc />
    public class GreenshotAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<AuthenticationProvider>()
                .As<IAuthenticationProvider>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<MainForm>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<HotKeyHandler>()
                .AsSelf()
                .SingleInstance();
            // TODO: Should be removed
            builder
                .RegisterType<SettingsForm>()
                .AsSelf()
                .SingleInstance();

            // Configuration
            builder
                .RegisterAssemblyTypes()
                .AssignableTo<IConfigScreen>()
                .As<IConfigScreen>()
                .SingleInstance();

            // Startup and Shutdown
            builder
                .RegisterAssemblyTypes()
                .AssignableTo<IStartupMarker>()
                .As<IStartupMarker>()
                .SingleInstance();

            builder
                .RegisterType<ErrorViewModel>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<WindowHandle>()
                .AsSelf()
                .SingleInstance();

            // Destinations
            builder
                .RegisterAssemblyTypes()
                .AssignableTo<IDestination>()
                .As<IDestination>()
                .SingleInstance();
        }
    }
}
