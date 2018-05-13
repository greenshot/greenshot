using Autofac;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addons
{
    /// <inheritdoc />
    public class AddonsAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<FileConfigPartViewModel>()
                .AsSelf();
            
        }
    }
}
