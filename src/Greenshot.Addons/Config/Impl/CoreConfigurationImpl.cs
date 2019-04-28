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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using Dapplo.Config.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Structs;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Core.Enums;

namespace Greenshot.Addons.Config.Impl
{
    /// <summary>
    /// Implementation of the ICoreConfiguration
    /// </summary>
    public class CoreConfigurationImpl : IniSectionBase<ICoreConfiguration>, ICoreConfiguration
    {
        /// <inheritdoc />
        public override void AfterLoad()
        {
            CoreConfigurationExtensions.AfterLoad(this);
        }

        /// <inheritdoc />
        public string OutputFilePath { get; set; }

        /// <inheritdoc />
        public bool OutputFileAllowOverwrite { get; set; }

        /// <inheritdoc />
        public string OutputFileFilenamePattern { get; set; }

        /// <inheritdoc />
        public OutputFormats OutputFileFormat { get; set; }

        /// <inheritdoc />
        public bool OutputFileReduceColors { get; set; }

        /// <inheritdoc />
        public bool OutputFileAutoReduceColors { get; set; }

        /// <inheritdoc />
        public int OutputFileReduceColorsTo { get; set; }

        /// <inheritdoc />
        public int OutputFileJpegQuality { get; set; }

        /// <inheritdoc />
        public bool OutputFilePromptQuality { get; set; }

        /// <inheritdoc />
        public uint OutputFileIncrementingNumber { get; set; }

        /// <inheritdoc />
        public string OptimizePNGCommand { get; set; }

        /// <inheritdoc />
        public string OptimizePNGCommandArguments { get; set; }

        /// <inheritdoc />
        public NativeSize Win10BorderCrop { get; set; }

        /// <inheritdoc />
        public bool CaptureMousepointer { get; set; }

        /// <inheritdoc />
        public bool CaptureWindowsInteractive { get; set; }

        /// <inheritdoc />
        public int CaptureDelay { get; set; }

        /// <inheritdoc />
        public ScreenCaptureMode ScreenCaptureMode { get; set; }

        /// <inheritdoc />
        public int ScreenToCapture { get; set; }

        /// <inheritdoc />
        public WindowCaptureModes WindowCaptureMode { get; set; }

        /// <inheritdoc />
        public Color DWMBackgroundColor { get; set; }

        /// <inheritdoc />
        public IList<string> NoGDICaptureForProduct { get; set; }

        /// <inheritdoc />
        public IList<string> NoDWMCaptureForProduct { get; set; }

        /// <inheritdoc />
        public bool WindowCaptureRemoveCorners { get; set; }

        /// <inheritdoc />
        public IList<int> WindowCornerCutShape { get; set; }

        /// <inheritdoc />
        public string Language { get; set; }

        /// <inheritdoc />
        public string RegionHotkey { get; set; }

        /// <inheritdoc />
        public string WindowHotkey { get; set; }

        /// <inheritdoc />
        public string FullscreenHotkey { get; set; }

        /// <inheritdoc />
        public string LastregionHotkey { get; set; }

        /// <inheritdoc />
        public string IEHotkey { get; set; }

        /// <inheritdoc />
        public bool IsFirstLaunch { get; set; }

        /// <inheritdoc />
        public IList<string> OutputDestinations { get; set; }

        /// <inheritdoc />
        public IList<string> PickerDestinations { get; set; }

        /// <inheritdoc />
        public IList<ClipboardFormats> ClipboardFormats { get; set; }

        /// <inheritdoc />
        public bool WindowCaptureAllChildLocations { get; set; }

        /// <inheritdoc />
        public bool PlayCameraSound { get; set; }

        /// <inheritdoc />
        public bool ShowTrayNotification { get; set; }

        /// <inheritdoc />
        public bool OutputFileCopyPathToClipboard { get; set; }

        /// <inheritdoc />
        public string OutputFileAsFullpath { get; set; }

        /// <inheritdoc />
        public bool OutputPrintPromptOptions { get; set; }

        /// <inheritdoc />
        public bool OutputPrintAllowRotate { get; set; }

        /// <inheritdoc />
        public bool OutputPrintAllowEnlarge { get; set; }

        /// <inheritdoc />
        public bool OutputPrintAllowShrink { get; set; }

        /// <inheritdoc />
        public bool OutputPrintCenter { get; set; }

        /// <inheritdoc />
        public bool OutputPrintInverted { get; set; }

        /// <inheritdoc />
        public bool OutputPrintGrayscale { get; set; }

        /// <inheritdoc />
        public bool OutputPrintMonochrome { get; set; }

        /// <inheritdoc />
        public byte OutputPrintMonochromeThreshold { get; set; }

        /// <inheritdoc />
        public bool OutputPrintFooter { get; set; }

        /// <inheritdoc />
        public string OutputPrintFooterPattern { get; set; }

        /// <inheritdoc />
        public string NotificationSound { get; set; }

        /// <inheritdoc />
        public bool UseProxy { get; set; }

        /// <inheritdoc />
        public bool IECapture { get; set; }

        /// <inheritdoc />
        public bool IEFieldCapture { get; set; }

        /// <inheritdoc />
        public IList<string> WindowClassesToCheckForIE { get; set; }

        /// <inheritdoc />
        public int AutoCropDifference { get; set; }

        /// <inheritdoc />
        public IList<string> IncludePlugins { get; set; }

        /// <inheritdoc />
        public IList<string> ExcludePlugins { get; set; }

        /// <inheritdoc />
        public IList<string> ExcludeDestinations { get; set; }

        /// <inheritdoc />
        public bool CheckForUpdates { get; set; }

        /// <inheritdoc />
        public int UpdateCheckInterval { get; set; }

        /// <inheritdoc />
        public DateTime LastUpdateCheck { get; set; }

        /// <inheritdoc />
        public bool DisableSettings { get; set; }

        /// <inheritdoc />
        public bool DisableQuickSettings { get; set; }

        /// <inheritdoc />
        public bool HideTrayicon { get; set; }

        /// <inheritdoc />
        public bool HideExpertSettings { get; set; }

        /// <inheritdoc />
        public bool ThumnailPreview { get; set; }

        /// <inheritdoc />
        public bool OptimizeForRDP { get; set; }

        /// <inheritdoc />
        public bool DisableRDPOptimizing { get; set; }

        /// <inheritdoc />
        public bool MinimizeWorkingSetSize { get; set; }

        /// <inheritdoc />
        public bool CheckForUnstable { get; set; }

        /// <inheritdoc />
        public IList<string> ActiveTitleFixes { get; set; }

        /// <inheritdoc />
        public IDictionary<string, string> TitleFixMatcher { get; set; }

        /// <inheritdoc />
        public IDictionary<string, string> TitleFixReplacer { get; set; }

        /// <inheritdoc />
        public IList<string> ExperimentalFeatures { get; set; }

        /// <inheritdoc />
        public bool EnableSpecialDIBClipboardReader { get; set; }

        /// <inheritdoc />
        public ClickActions LeftClickAction { get; set; }

        /// <inheritdoc />
        public ClickActions DoubleClickAction { get; set; }

        /// <inheritdoc />
        public bool ZoomerEnabled { get; set; }

        /// <inheritdoc />
        public float ZoomerOpacity { get; set; }

        /// <inheritdoc />
        public int MaxMenuItemLength { get; set; }

        /// <inheritdoc />
        public string MailApiTo { get; set; }

        /// <inheritdoc />
        public string MailApiCC { get; set; }

        /// <inheritdoc />
        public string MailApiBCC { get; set; }

        /// <inheritdoc />
        public string LastSaveWithVersion { get; set; }

        /// <inheritdoc />
        public bool ProcessEXIFOrientation { get; set; }

        /// <inheritdoc />
        public NativeRect LastCapturedRegion { get; set; }

        /// <inheritdoc />
        public NativeSize IconSize { get; set; }

        /// <inheritdoc />
        public int WebRequestTimeout { get; set; }

        /// <inheritdoc />
        public int WebRequestReadWriteTimeout { get; set; }

        /// <inheritdoc />
        public bool IsScrollingCaptureEnabled { get; set; }

        /// <inheritdoc />
        public bool IsPortable { get; set; }

        /// <inheritdoc />
        public ISet<string> Permissions { get; set; }

        /// <inheritdoc />
        public WindowStartupLocation DefaultWindowStartupLocation { get; set; }

        /// <inheritdoc />
        public bool AreWindowLocationsStored { get; set; }

        /// <inheritdoc />
        public IDictionary<string, WindowPlacement> WindowLocations { get; set; }
    }
}
