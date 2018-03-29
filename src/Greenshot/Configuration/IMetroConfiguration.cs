using System.ComponentModel;
using Dapplo.CaliburnMicro.Metro;
using Dapplo.Ini;
using Dapplo.InterfaceImpl.Extensions;

namespace Greenshot.Configuration
{
    [IniSection("Metro")]
    public interface IMetroConfiguration : IIniSection, ITransactionalProperties
    {
        [DefaultValue(Themes.BaseLight)]
        Themes Theme { get; set; }

        [DefaultValue(ThemeAccents.Orange)]
        ThemeAccents ThemeAccent { get; set; }
    }
}
