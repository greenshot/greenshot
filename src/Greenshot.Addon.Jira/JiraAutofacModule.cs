using Autofac;
using Dapplo.CaliburnMicro;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.Jira.ViewModels;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.Jira
{
    /// <inheritdoc />
    public class JiraAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<JiraDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<JiraConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
            builder
                .RegisterType<JiraViewModel>()
                .AsSelf();
            builder
                .RegisterType<JiraConnector>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<JiraMonitor>()
                .As<IUiStartup>()
                .As<IUiShutdown>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
