using Autofac;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.OCR
{
    /// <inheritdoc />
    public class PhotobucketAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // TODO: Check for MODI
            builder
                .RegisterType<OcrDestination>()
                .As<IDestination>()
                .SingleInstance();
            
        }
    }
}
