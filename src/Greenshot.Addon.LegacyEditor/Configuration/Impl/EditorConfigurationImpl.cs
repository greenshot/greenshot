using System.Collections.Generic;
using System.Drawing;
using Dapplo.Config.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Greenshot.Gfx.Effects;

namespace Greenshot.Addon.LegacyEditor.Configuration.Impl
{
    internal class EditorConfigurationImpl : IniSectionBase<IEditorConfiguration>, IEditorConfiguration
    {
        public IList<Color> RecentColors { get; set; }
        public IDictionary<string, object> LastUsedFieldValues { get; set; }
        public bool MatchSizeToCapture { get; set; }
        public WindowPlacementFlags WindowPlacementFlags { get; set; }
        public ShowWindowCommands ShowWindowCommand { get; set; }
        public NativePoint WindowMinPosition { get; set; }
        public NativePoint WindowMaxPosition { get; set; }
        public NativeRect WindowNormalPosition { get; set; }
        public bool ReuseEditor { get; set; }
        public int FreehandSensitivity { get; set; }
        public bool SuppressSaveDialogAtClose { get; set; }
        public DropShadowEffect DropShadowEffectSettings { get; set; }
        public TornEdgeEffect TornEdgeEffectSettings { get; set; }
    }
}
