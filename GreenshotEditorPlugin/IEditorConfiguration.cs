/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Config.Ini;
using Greenshot.Core;
using GreenshotPlugin.UnmanagedHelpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;

namespace GreenshotEditorPlugin {
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Editor"), Description("Greenshot editor configuration")]
	public interface IEditorConfiguration : IIniSection<IEditorConfiguration>, INotifyPropertyChanged {
		[Description("Last used colors")]
		IList<Color> RecentColors {
			get;
			set;
		}

		[DataMember(Name = "LastFieldValue"), Description("Field values, make sure the last used settings are re-used")]
		IDictionary<string, object> LastUsedFieldValues {
			get;
			set;
		}

		[Description("Match the editor window size to the capture"), DefaultValue(true)]
		bool MatchSizeToCapture {
			get;
			set;
		}

		[Description("Placement flags")]
		WindowPlacementFlags WindowPlacementFlags {
			get;
			set;
		}

		[Description("Show command"), DefaultValue(ShowWindowCommand.Normal)]
		ShowWindowCommand ShowWindowCommand {
			get;
			set;
		}

		[Description("Position of minimized window"), DefaultValue("-1,-1")]
		Point WindowMinPosition {
			get;
			set;
		}

		[Description("Position of maximized window"), DefaultValue("-1,-1")]
		Point WindowMaxPosition {
			get;
			set;
		}

		[Description("Position of normal window"), DefaultValue("100,100,400,400")]
		Rectangle WindowNormalPosition {
			get;
			set;
		}

		[Description("Reuse already open editor"), DefaultValue(false)]
		bool ReuseEditor {
			get;
			set;
		}

		[Description("The smaller this number, the less smoothing is used. Decrease for detailed drawing, e.g. when using a pen. Increase for smoother lines. e.g. when you want to draw a smooth line."), DefaultValue(3)]
		int FreehandSensitivity {
			get;
			set;
		}

		[Description("Suppressed the 'do you want to save' dialog when closing the editor."), DefaultValue(false)]
		bool SuppressSaveDialogAtClose {
			get;
			set;
		}


		[Description("Settings for the drop shadow effect."), TypeConverter(typeof(EffectConverter))]
		DropShadowEffect DropShadowEffectSettings {
			get;
			set;
		}


		[Description("Settings for the torn edge effect."), TypeConverter(typeof(EffectConverter))]
		TornEdgeEffect TornEdgeEffectSettings {
			get;
			set;
		}

		[Description("Sets how to compare the colors for the autocrop detection, the higher the more is 'selected'. Possible values are from 0 to 255, where everything above ~150 doesn't make much sense!"), DefaultValue("10")]
		int AutoCropDifference {
			get;
			set;
		}
	}
}
