// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Dapplo.Config.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Greenshot.Gfx.Effects;

namespace Greenshot.Addon.LegacyEditor
{
    /// <summary>
    ///     Editor configuration
    /// </summary>
    [IniSection("Editor")]
	[Description("Greenshot editor configuration")]
	public interface IEditorConfiguration : IIniSection
	{
		[Description("Last used colors")]
		IList<Color> RecentColors { get; set; }

		[Description("Field values, make sure the last used settings are re-used")]
		IDictionary<string, object> LastUsedFieldValues { get; set; }

		[Description("Match the editor window size to the capture")]
		[DefaultValue(true)]
		bool MatchSizeToCapture { get; set; }

		[Description("Placement flags")]
		[DefaultValue(WindowPlacementFlags.None)]
		WindowPlacementFlags WindowPlacementFlags { get; set; }

		[Description("Show command")]
		[DefaultValue(ShowWindowCommands.Normal)]
		ShowWindowCommands ShowWindowCommand { get; set; }

		[Description("Position of minimized window")]
		[DefaultValue("-1,-1")]
		NativePoint WindowMinPosition { get; set; }

		[Description("Position of maximized window")]
		[DefaultValue("-1,-1")]
		NativePoint WindowMaxPosition { get; set; }

		[Description("Position of normal window")]
		[DefaultValue("100,100,400,400")]
		NativeRect WindowNormalPosition { get; set; }

		[Description("Reuse already open editor")]
		[DefaultValue(false)]
		bool ReuseEditor { get; set; }

		[Description("The smaller this number, the less smoothing is used. Decrease for detailed drawing, e.g. when using a pen. Increase for smoother lines. e.g. when you want to draw a smooth line.")]
		[DefaultValue(3)]
		int FreehandSensitivity { get; set; }

		[Description("Suppressed the 'do you want to save' dialog when closing the editor.")]
		[DefaultValue(false)]
		bool SuppressSaveDialogAtClose { get; set; }

		[Description("Settings for the drop shadow effect.")]
		DropShadowEffect DropShadowEffectSettings { get; set; }

		[Description("Settings for the torn edge effect.")]
		TornEdgeEffect TornEdgeEffectSettings { get; set; }
	}
}