using Autofac;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Security;
using Greenshot.Addons.Components;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Components;
using Greenshot.Forms;
using Greenshot.Ui.Configuration.ViewModels;
using Greenshot.Ui.Misc.ViewModels;

namespace Greenshot
{
    /// <inheritdoc />
    public class GreenshotAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<ConfigViewModel>()
                .AsSelf()
                .SingleInstance();
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
                .RegisterType<HotkeyHandler>()
                .As<IUiStartup>()
                .As<IUiShutdown>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<MainFormStartup>()
                .As<IUiStartup>()
                .As<IUiShutdown>()
                .SingleInstance();

            // TODO: Should be removed
            builder
                .RegisterType<SettingsForm>()
                .AsSelf()
                .SingleInstance();

            // Configuration
            builder
                .RegisterAssemblyTypes(ThisAssembly)
                .AssignableTo<IConfigScreen>()
                .As<IConfigScreen>()
                .SingleInstance();

            // Startup and Shutdown
            builder
                .RegisterAssemblyTypes(ThisAssembly)
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
                .RegisterAssemblyTypes(ThisAssembly)
                .AssignableTo<IDestination>()
                .As<IDestination>()
                .SingleInstance();
        }
    }
}
