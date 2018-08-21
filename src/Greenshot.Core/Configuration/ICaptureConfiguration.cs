using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Core.Enums;

namespace Greenshot.Core.Configuration
{
    public interface ICaptureConfiguration
    {
        [Description("The capture is cropped with these settings, e.g. when you don't want to color around it -1,-1")]
        [DefaultValue("0,0")]
        NativeSize Win10BorderCrop { get; set; }

        [Description("Should the mouse be captured?")]
        [DefaultValue(true)]
        bool CaptureMousepointer { get; set; }

        [Description("Use interactive window selection to capture? (false=Capture active window)")]
        [DefaultValue(false)]
        bool CaptureWindowsInteractive { get; set; }

        [Description("Capture delay in millseconds.")]
        [DefaultValue(100)]
        int CaptureDelay { get; set; }

        [Description("The capture mode used to capture a screen. (Auto, FullScreen, Fixed)")]
        [DefaultValue(ScreenCaptureMode.Auto)]
        ScreenCaptureMode ScreenCaptureMode { get; set; }

        [Description("The screen number to capture when using ScreenCaptureMode Fixed.")]
        [DefaultValue(1)]
        int ScreenToCapture { get; set; }

        [Description("The capture mode used to capture a Window (Screen, GDI, Aero, AeroTransparent, Auto).")]
        [DefaultValue(WindowCaptureModes.Auto)]
        WindowCaptureModes WindowCaptureMode { get; set; }

        [Description("The background color for a DWM window capture.")]
        Color DWMBackgroundColor { get; set; }

        [Description("List of productnames for which GDI capturing is skipped (using fallback).")]
        [DefaultValue("IntelliJ IDEA")]
        IList<string> NoGDICaptureForProduct { get; set; }

        [Description("List of productnames for which DWM capturing is skipped (using fallback).")]
        [DefaultValue("Citrix ICA Client")]
        IList<string> NoDWMCaptureForProduct { get; set; }

        [Description("Remove the corners from a window capture")]
        [DefaultValue(true)]
        bool WindowCaptureRemoveCorners { get; set; }

        [Description("The cutshape which is used to remove the window corners, is mirrorred for all corners")]
        [DefaultValue("5,3,2,1,1")]
        IList<int> WindowCornerCutShape { get; set; }
    }
}
