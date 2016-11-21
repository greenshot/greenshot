//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.ComponentModel;
using Dapplo.Config.Language;
using Greenshot.CaptureCore.Configuration;
using Greenshot.Core.Configuration;

#endregion

namespace Greenshot.Addon.Configuration
{
	[Language("Core")]
	public interface IGreenshotLanguage : ILanguage, INotifyPropertyChanged, IEditorLanguage, ICaptureTranslations, ICoreTranslations, Dapplo.CaliburnMicro.Translations.IConfigTranslations, Dapplo.CaliburnMicro.Translations.ICoreTranslations
	{
		string AboutBugs { get; }

		string AboutDonations { get; }

		string AboutHost { get; }

		string AboutIcons { get; }

		string AboutLicense { get; }

		string AboutTitle { get; }

		string AboutTranslation { get; }

		string ApplicationTitle { get; }

		string BugreportCancel { get; }

		string BugreportInfo { get; }

		string BugreportTitle { get; }

		string ColorpickerAlpha { get; }

		string ColorpickerApply { get; }

		string ColorpickerBlue { get; }

		string ColorpickerGreen { get; }

		string ColorpickerHtmlcolor { get; }

		string ColorpickerRecentcolors { get; }

		string ColorpickerRed { get; }

		string ColorpickerTitle { get; }

		string ColorpickerTransparent { get; }

		string ConfigUnauthorizedaccessWrite { get; }

		string ContextmenuAbout { get; }

		string ContextmenuDonate { get; }

		string ContextmenuExit { get; }

		string ContextmenuHelp { get; }

		string ContextmenuOpenfile { get; }

		string ContextmenuOpenrecentcapture { get; }

		string ContextmenuQuicksettings { get; }

		string ContextmenuSettings { get; }

		string DestinationExportFailed { get; }

		[DefaultValue("An instance of Greenshot is already running.")]
		string ErrorMultipleinstances { get; }

		[DefaultValue("Could not open link '{0}'.")]
		string ErrorOpenlink { get; }

		string ErrorSave { get; }

		string ErrorSaveInvalidChars { get; }

		string ExportedTo { get; }

		string HelpTitle { get; }

		string Hotkeys { get; }

		string JpegqualitydialogChoosejpegquality { get; }

		string PrintError { get; }

		string PrintoptionsAllowcenter { get; }

		string PrintoptionsAllowenlarge { get; }

		string PrintoptionsAllowrotate { get; }

		string PrintoptionsAllowshrink { get; }

		string PrintoptionsColors { get; }

		string PrintoptionsDontaskagain { get; }

		string PrintoptionsInverted { get; }

		string PrintoptionsPagelayout { get; }

		string PrintoptionsPrintcolor { get; }

		string PrintoptionsPrintgrayscale { get; }

		string PrintoptionsPrintmonochrome { get; }

		string PrintoptionsTimestamp { get; }

		string PrintoptionsTitle { get; }

		string QualitydialogDontaskagain { get; }

		string QualitydialogTitle { get; }

		string QuicksettingsDestinationFile { get; }

		string SettingsAlwaysshowprintoptionsdialog { get; }

		string SettingsAlwaysshowqualitydialog { get; }

		string SettingsApplicationsettings { get; }

		string SettingsAutostartshortcut { get; }

		string SettingsCapture { get; }

		string SettingsCaptureMousepointer { get; }

		string SettingsCaptureWindowsInteractive { get; }

		string SettingsCheckperiod { get; }

		string SettingsCopypathtoclipboard { get; }

		string SettingsDestination { get; }

		string SettingsDestinationClipboard { get; }

		string SettingsDestinationEmail { get; }

		string SettingsDestinationFile { get; }

		string SettingsDestinationFileas { get; }

		string SettingsDestinationPicker { get; }

		string SettingsDestinationPrinter { get; }

		string SettingsEditor { get; }

		string SettingsFilenamepattern { get; }

		string SettingsGeneral { get; }

		string SettingsIecapture { get; }

		string SettingsJpegquality { get; }

		string SettingsLanguage { get; }

		string SettingsMessageFilenamepattern { get; }

		string SettingsNetwork { get; }

		string SettingsOutput { get; }

		string SettingsPlaysound { get; }

		string SettingsPlugins { get; }

		string SettingsPluginsCreatedby { get; }

		string SettingsPluginsDllpath { get; }

		string SettingsPluginsName { get; }

		string SettingsPluginsVersion { get; }

		string SettingsPreferredfilesettings { get; }

		string SettingsPrimaryimageformat { get; }

		string SettingsPrinter { get; }

		string SettingsPrintoptions { get; }

		string SettingsQualitysettings { get; }

		string SettingsReducecolors { get; }

		string SettingsRegisterhotkeys { get; }

		string SettingsShowflashlight { get; }

		string SettingsShownotify { get; }

		string SettingsStoragelocation { get; }

		string SettingsTitle { get; }

		string SettingsTooltipFilenamepattern { get; }

		string SettingsTooltipLanguage { get; }

		string SettingsTooltipPrimaryimageformat { get; }

		string SettingsTooltipRegisterhotkeys { get; }

		string SettingsTooltipStoragelocation { get; }

		string SettingsUsedefaultproxy { get; }

		string SettingsVisualization { get; }

		string SettingsWaittime { get; }

		string SettingsWindowCaptureMode { get; }

		string SettingsWindowscapture { get; }

		string TooltipFirststart { get; }

		string UpdateFound { get; }

		string Warning { get; }

		string WarningHotkeys { get; }
	}
}