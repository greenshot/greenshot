using System.ComponentModel;
using Dapplo.CaliburnMicro.Metro;
using Dapplo.Config.Ini;

namespace Greenshot.Configuration
{
    [IniSection("Metro")]
    public interface IMetroConfiguration : IIniSection
    {
        [DefaultValue(Themes.BaseLight)]
        Themes Theme { get; set; }

        [DefaultValue(ThemeAccents.Orange)]
        ThemeAccents ThemeAccent { get; set; }
    }
}
