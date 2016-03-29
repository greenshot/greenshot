/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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

using System.ComponentModel;
using System.Drawing;
using Dapplo.Config.Interfaces;

namespace Greenshot.Addon.Configuration
{

	/// <summary>
	/// Specify what click actions there are, and Greenshot can respond to.
	/// Used in the double/left/right-click actions
	/// </summary>
	public enum ClickActions
	{
		DoNothing,
		OpenLastInExplorer,
		OpenLastInEditor,
		OpenSettings,
		ShowContextMenu,
		CaptureRegion,
		CaptureScreen,
		CaptureWindow,
		CaptureLastRegion
	}

	/// <summary>
	/// This interface represents all the UI settings
	/// </summary>
	public interface IUiConfiguration
	{
		[Description("The language in IETF format (e.g. en-US)")]
		string Language
		{
			get;
			set;
		}


		[Description("Is this the first time launch?"), DefaultValue(true)]
		bool IsFirstLaunch
		{
			get;
			set;
		}

		[Description("Flash the screen after taking a capture."), DefaultValue(true), Tag(ConfigTags.LanguageKey, "settings_showflashlight")]
		bool ShowFlash
		{
			get;
			set;
		}

		[Description("Play a camera sound after taking a capture."), DefaultValue(false), Tag(ConfigTags.LanguageKey, "settings_playsound")]
		bool PlayCameraSound
		{
			get;
			set;
		}

		[Description("Show a notification from the systray when a capture is taken."), DefaultValue(true), Tag(ConfigTags.LanguageKey, "settings_shownotify")]
		bool ShowTrayNotification
		{
			get;
			set;
		}

		[Description("The wav-file to play when a capture is taken, loaded only once at the Greenshot startup"), DefaultValue("default")]
		string NotificationSound
		{
			get;
			set;
		}

		[Description("Enable/disable the access to the settings, can only be changed manually in this .ini"), DefaultValue(false)]
		bool DisableSettings
		{
			get;
			set;
		}

		[Description("Enable/disable the access to the quick settings, can only be changed manually in this .ini"), DefaultValue(false)]
		bool DisableQuickSettings
		{
			get;
			set;
		}

		[Description("Disable the trayicon, can only be changed manually in this .ini"), DefaultValue(false)]
		bool HideTrayicon
		{
			get;
			set;
		}

		[Description("Hide expert tab in the settings, can only be changed manually in this .ini"), DefaultValue(false)]
		bool HideExpertSettings
		{
			get;
			set;
		}

		[Description("Enable/disable thumbnail previews"), DefaultValue(true)]
		bool ThumnailPreview
		{
			get;
			set;
		}

		[Description("Make some optimizations for usage with remote desktop"), DefaultValue(false)]
		bool OptimizeForRdp
		{
			get;
			set;
		}

		[Description("Specify what action is made if the tray icon is left clicked, if a double-click action is specified this action is initiated after a delay (configurable via the windows double-click speed)"), DefaultValue(ClickActions.ShowContextMenu)]
		ClickActions LeftClickAction
		{
			get;
			set;
		}

		[Description("Specify what action is made if the tray icon is double clicked"), DefaultValue(ClickActions.OpenLastInExplorer)]
		ClickActions DoubleClickAction
		{
			get;
			set;
		}

		[Description("Sets if the zoomer is enabled"), DefaultValue(true)]
		bool ZoomerEnabled
		{
			get;
			set;
		}

		[Description("Specify the transparency for the zoomer, from 0-1 (where 1 is no transparency and 0 is complete transparent. An usefull setting would be 0.7)"), DefaultValue(1)]
		float ZoomerOpacity
		{
			get;
			set;
		}

		[Description("Maximum length of submenu items in the context menu, making this longer might cause context menu issues on dual screen systems."), DefaultValue(25)]
		int MaxMenuItemLength
		{
			get;
			set;
		}

		[Description("Defines the size of the icons (e.g. for the buttons in the editor), default value 16,16 anything bigger will cause scaling"), DefaultValue("16,16")]
		Size IconSize
		{
			get;
			set;
		}
	}
}