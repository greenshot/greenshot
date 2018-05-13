using Autofac;
using Dapplo.CaliburnMicro.Configuration;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addon.LegacyEditor.Forms;
using Greenshot.Addon.LegacyEditor.ViewModels;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Addon.LegacyEditor
{
    /// <inheritdoc />
    public class EditorAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<EditorDestination>()
                .As<IDestination>()
                .SingleInstance();
            builder
                .RegisterType<EditorConfigViewModel>()
                .As<IConfigScreen>()
                .SingleInstance();
            builder
                .RegisterType<EditorFactory>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<ImageEditorForm>()
                .AsSelf();
            builder
                .RegisterType<Surface>()
                .As<ISurface>();
        }
    }
}
