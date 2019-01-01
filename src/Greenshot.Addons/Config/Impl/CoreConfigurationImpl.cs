#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using Dapplo.Config.Ini;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Structs;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Core.Enums;

namespace Greenshot.Addons.Config.Impl
{
    public class CoreConfigurationImpl : IniSectionBase<ICoreConfiguration>, ICoreConfiguration
    {
        #region Overrides of IniSectionBase<ICoreConfiguration>

        public override void AfterLoad()
        {
            CoreConfigurationExtensions.AfterLoad(this);
        }

        #endregion

        #region Implementation of IFileConfiguration

        public string OutputFilePath { get; set; }
        public bool OutputFileAllowOverwrite { get; set; }
        public string OutputFileFilenamePattern { get; set; }
        public OutputFormats OutputFileFormat { get; set; }
        public bool OutputFileReduceColors { get; set; }
        public bool OutputFileAutoReduceColors { get; set; }
        public int OutputFileReduceColorsTo { get; set; }
        public int OutputFileJpegQuality { get; set; }
        public bool OutputFilePromptQuality { get; set; }
        public uint OutputFileIncrementingNumber { get; set; }
        public string OptimizePNGCommand { get; set; }
        public string OptimizePNGCommandArguments { get; set; }

        #endregion

        #region Implementation of ICaptureConfiguration

        public NativeSize Win10BorderCrop { get; set; }
        public bool CaptureMousepointer { get; set; }
        public bool CaptureWindowsInteractive { get; set; }
        public int CaptureDelay { get; set; }
        public ScreenCaptureMode ScreenCaptureMode { get; set; }
        public int ScreenToCapture { get; set; }
        public WindowCaptureModes WindowCaptureMode { get; set; }
        public Color DWMBackgroundColor { get; set; }
        public IList<string> NoGDICaptureForProduct { get; set; }
        public IList<string> NoDWMCaptureForProduct { get; set; }
        public bool WindowCaptureRemoveCorners { get; set; }
        public IList<int> WindowCornerCutShape { get; set; }

        #endregion

        #region Implementation of ICoreConfiguration

        public string Language { get; set; }
        public string RegionHotkey { get; set; }
        public string WindowHotkey { get; set; }
        public string FullscreenHotkey { get; set; }
        public string LastregionHotkey { get; set; }
        public string IEHotkey { get; set; }
        public bool IsFirstLaunch { get; set; }
        public IList<string> OutputDestinations { get; set; }
        public IList<string> PickerDestinations { get; set; }
        public IList<ClipboardFormats> ClipboardFormats { get; set; }
        public bool WindowCaptureAllChildLocations { get; set; }
        public bool PlayCameraSound { get; set; }
        public bool ShowTrayNotification { get; set; }
        public bool OutputFileCopyPathToClipboard { get; set; }
        public string OutputFileAsFullpath { get; set; }
        public bool OutputPrintPromptOptions { get; set; }
        public bool OutputPrintAllowRotate { get; set; }
        public bool OutputPrintAllowEnlarge { get; set; }
        public bool OutputPrintAllowShrink { get; set; }
        public bool OutputPrintCenter { get; set; }
        public bool OutputPrintInverted { get; set; }
        public bool OutputPrintGrayscale { get; set; }
        public bool OutputPrintMonochrome { get; set; }
        public byte OutputPrintMonochromeThreshold { get; set; }
        public bool OutputPrintFooter { get; set; }
        public string OutputPrintFooterPattern { get; set; }
        public string NotificationSound { get; set; }
        public bool UseProxy { get; set; }
        public bool IECapture { get; set; }
        public bool IEFieldCapture { get; set; }
        public IList<string> WindowClassesToCheckForIE { get; set; }
        public int AutoCropDifference { get; set; }
        public IList<string> IncludePlugins { get; set; }
        public IList<string> ExcludePlugins { get; set; }
        public IList<string> ExcludeDestinations { get; set; }
        public bool CheckForUpdates { get; set; }
        public int UpdateCheckInterval { get; set; }
        public DateTime LastUpdateCheck { get; set; }
        public bool DisableSettings { get; set; }
        public bool DisableQuickSettings { get; set; }
        public bool HideTrayicon { get; set; }
        public bool HideExpertSettings { get; set; }
        public bool ThumnailPreview { get; set; }
        public bool OptimizeForRDP { get; set; }
        public bool DisableRDPOptimizing { get; set; }
        public bool MinimizeWorkingSetSize { get; set; }
        public bool CheckForUnstable { get; set; }
        public IList<string> ActiveTitleFixes { get; set; }
        public IDictionary<string, string> TitleFixMatcher { get; set; }
        public IDictionary<string, string> TitleFixReplacer { get; set; }
        public IList<string> ExperimentalFeatures { get; set; }
        public bool EnableSpecialDIBClipboardReader { get; set; }
        public ClickActions LeftClickAction { get; set; }
        public ClickActions DoubleClickAction { get; set; }
        public bool ZoomerEnabled { get; set; }
        public float ZoomerOpacity { get; set; }
        public int MaxMenuItemLength { get; set; }
        public string MailApiTo { get; set; }
        public string MailApiCC { get; set; }
        public string MailApiBCC { get; set; }
        public string LastSaveWithVersion { get; set; }
        public bool ProcessEXIFOrientation { get; set; }
        public NativeRect LastCapturedRegion { get; set; }
        public NativeSize IconSize { get; set; }
        public int WebRequestTimeout { get; set; }
        public int WebRequestReadWriteTimeout { get; set; }
        public bool IsScrollingCaptureEnabled { get; set; }
        public bool IsPortable { get; set; }
        public ISet<string> Permissions { get; set; }

        #endregion

        #region Implementation of IUiConfiguration

        public WindowStartupLocation DefaultWindowStartupLocation { get; set; }
        public bool AreWindowLocationsStored { get; set; }
        public IDictionary<string, WindowPlacement> WindowLocations { get; set; }

        #endregion
    }
}
