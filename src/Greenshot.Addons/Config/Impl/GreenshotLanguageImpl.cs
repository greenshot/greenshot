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

using System.Diagnostics.CodeAnalysis;
using Dapplo.Config.Language;

namespace Greenshot.Addons.Config.Impl
{
    /// <summary>
    /// This implements IGreenshotLanguage and takes care of storing, all setters are replaced via AutoProperties.Fody
    /// </summary>
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
#pragma warning disable CS1591
    internal class GreenshotLanguageImpl : LanguageBase<IGreenshotLanguage>, IGreenshotLanguage
    {
        public string Cancel { get; }
        public string Ok { get; }

        public string None { get; }
        public string AboutBugs { get; }
        public string AboutDonations { get; }
        public string AboutHost { get; }
        public string AboutIcons { get; }
        public string AboutLicense { get; }
        public string AboutTitle { get; }
        public string AboutTranslation { get; }
        public string ApplicationTitle { get; }
        public string BugreportCancel { get; }
        public string BugreportInfo { get; }
        public string BugreportTitle { get; }
        public string ClipboardError { get; }
        public string ClipboardInuse { get; }
        public string Close { get; }
        public string ColorpickerAlpha { get; }
        public string ColorpickerApply { get; }
        public string ColorpickerBlue { get; }
        public string ColorpickerGreen { get; }
        public string ColorpickerHtmlcolor { get; }
        public string ColorpickerRecentcolors { get; }
        public string ColorpickerRed { get; }
        public string ColorpickerTitle { get; }
        public string ColorpickerTransparent { get; }
        public string ConfigUnauthorizedaccessWrite { get; }
        public string ContextmenuAbout { get; }
        public string ContextmenuCapturearea { get; }
        public string ContextmenuCaptureclipboard { get; }
        public string ContextmenuCapturefullscreen { get; }
        public string ContextmenuCapturefullscreenAll { get; }
        public string ContextmenuCapturefullscreenLeft { get; }
        public string ContextmenuCapturefullscreenTop { get; }
        public string ContextmenuCapturefullscreenRight { get; }
        public string ContextmenuCapturefullscreenBottom { get; }
        public string ContextmenuCapturelastregion { get; }
        public string ContextmenuCapturewindow { get; }
        public string ContextmenuDonate { get; }
        public string ContextmenuExit { get; }
        public string ContextmenuHelp { get; }
        public string ContextmenuOpenfile { get; }
        public string ContextmenuQuicksettings { get; }
        public string ContextmenuSettings { get; }
        public string ContextmenuCaptureie { get; }
        public string ContextmenuOpenrecentcapture { get; }
        public string Error { get; }
        public string ErrorMultipleinstances { get; }
        public string ErrorNowriteaccess { get; }
        public string ErrorOpenfile { get; }
        public string ErrorOpenlink { get; }
        public string ErrorSave { get; }
        public string ErrorSaveInvalidChars { get; }
        public string HelpTitle { get; }
        public string JpegqualitydialogChoosejpegquality { get; }
        public string QualitydialogDontaskagain { get; }
        public string QualitydialogTitle { get; }
        public string SettingsReducecolors { get; }
        public string PrintError { get; }
        public string PrintoptionsAllowcenter { get; }
        public string PrintoptionsAllowenlarge { get; }
        public string PrintoptionsAllowrotate { get; }
        public string PrintoptionsAllowshrink { get; }
        public string PrintoptionsColors { get; }
        public string PrintoptionsDontaskagain { get; }
        public string PrintoptionsPagelayout { get; }
        public string PrintoptionsPrintcolor { get; }
        public string PrintoptionsPrintgrayscale { get; }
        public string PrintoptionsPrintmonochrome { get; }
        public string PrintoptionsTimestamp { get; }
        public string PrintoptionsInverted { get; }
        public string PrintoptionsTitle { get; }
        public string QuicksettingsDestinationFile { get; }
        public string SettingsAlwaysshowqualitydialog { get; }
        public string SettingsAlwaysshowprintoptionsdialog { get; }
        public string SettingsApplicationsettings { get; }
        public string SettingsAutostartshortcut { get; }
        public string SettingsCapture { get; }
        public string SettingsCaptureMousepointer { get; }
        public string SettingsCaptureWindowsInteractive { get; }
        public string SettingsCopypathtoclipboard { get; }
        public string SettingsDestination { get; }
        public string SettingsDestinationClipboard { get; }
        public string SettingsDestinationEditor { get; }
        public string SettingsDestinationEmail { get; }
        public string SettingsDestinationFile { get; }
        public string SettingsDestinationFileas { get; }
        public string SettingsDestinationPrinter { get; }
        public string SettingsDestinationPicker { get; }
        public string SettingsEditor { get; }
        public string SettingsFilenamepattern { get; }
        public string SettingsGeneral { get; }
        public string SettingsIecapture { get; }
        public string SettingsJpegquality { get; }
        public string SettingsQualitysettings { get; }
        public string SettingsLanguage { get; }
        public string SettingsMessageFilenamepattern { get; }
        public string SettingsOutput { get; }
        public string SettingsPlaysound { get; }
        public string SettingsPlugins { get; }
        public string SettingsPluginsName { get; }
        public string SettingsPluginsVersion { get; }
        public string SettingsPluginsCreatedby { get; }
        public string SettingsPluginsDllpath { get; }
        public string SettingsPreferredfilesettings { get; }
        public string SettingsPrimaryimageformat { get; }
        public string SettingsPrinter { get; }
        public string SettingsPrintoptions { get; }
        public string SettingsRegisterhotkeys { get; }
        public string SettingsShowflashlight { get; }
        public string SettingsStoragelocation { get; }
        public string SettingsTitle { get; }
        public string SettingsTooltipFilenamepattern { get; }
        public string SettingsTooltipLanguage { get; }
        public string SettingsTooltipPrimaryimageformat { get; }
        public string SettingsTooltipRegisterhotkeys { get; }
        public string SettingsTooltipStoragelocation { get; }
        public string SettingsVisualization { get; }
        public string SettingsShownotify { get; }
        public string SettingsWaittime { get; }
        public string SettingsZoom { get; }
        public string SettingsWindowscapture { get; }
        public string SettingsWindowCaptureMode { get; }
        public string SettingsScreenCaptureMode { get; }
        public string SettingsNetwork { get; }
        public string SettingsCheckperiod { get; }
        public string SettingsUsedefaultproxy { get; }
        public string TooltipFirststart { get; }
        public string Warning { get; }
        public string WarningHotkeys { get; }
        public string Hotkeys { get; }
        public string WaitIeCapture { get; }
        public string UpdateFound { get; }
        public string ExportedTo { get; }
        public string LatestVersion { get; }
        public string CurrentVersion { get; }
        public string Expert { get; }
        public string DestinationExportFailed { get; }
    }
}
