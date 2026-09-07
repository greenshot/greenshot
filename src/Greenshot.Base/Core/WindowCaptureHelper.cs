/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Helper methods for window selection, heuristics, and capture mode resolution.
    /// </summary>
    public static class WindowCaptureHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowCaptureHelper));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// Optional custom window capture handler (e.g. for Windows Graphics Capture when beta tester mode is enabled).
        /// </summary>
        public static Func<IntPtr, Image> CustomWindowCaptureHandler { get; set; }

        /// <summary>
        /// Select the window to capture, resolving linked windows for special applications (e.g. TOAD, Excel).
        /// </summary>
        public static WindowDetails SelectCaptureWindow(WindowDetails windowToCapture)
        {
            if (windowToCapture == null) return null;

            NativeRect windowRectangle = windowToCapture.WindowRectangle;
            if (windowRectangle.Width == 0 || windowRectangle.Height == 0)
            {
                Log.WarnFormat("Window {0} has nothing to capture, using workaround to find other window of same process.", windowToCapture.Text);
                WindowDetails linkedWindow = WindowDetails.GetLinkedWindow(windowToCapture);
                if (linkedWindow != null)
                {
                    windowToCapture = linkedWindow;
                }
                else
                {
                    return null;
                }
            }

            return windowToCapture;
        }

        private static bool IsWpf(Process process)
        {
            if (process == null) return false;
            try
            {
                foreach (ProcessModule module in process.Modules)
                {
                    if (!module.ModuleName.StartsWith("PresentationFramework"))
                    {
                        continue;
                    }
                    Log.InfoFormat("Found that Process {0} uses {1}, assuming it's using WPF", process.ProcessName, module.FileName);
                    return true;
                }
            }
            catch (Exception)
            {
                Log.WarnFormat("No access on the modules from process {0}, assuming WPF is used.", process.ProcessName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Captures a target window using the specified WindowCaptureMode and fallback heuristics.
        /// </summary>
        public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode)
        {
            if (captureForWindow == null)
            {
                captureForWindow = new Capture();
            }

            if (CustomWindowCaptureHandler != null && CoreConfig.IsBetaTester)
            {
                try
                {
                    var customImage = CustomWindowCaptureHandler(windowToCapture.Handle);
                    if (customImage != null)
                    {
                        captureForWindow.Image = customImage;
                        captureForWindow.CaptureDetails.Title = windowToCapture.Text;
                        return captureForWindow;
                    }
                    Log.DebugFormat("CustomWindowCaptureHandler returned null for window {0}, falling back to standard capture.", windowToCapture.Handle);
                }
                catch (Exception ex)
                {
                    Log.Warn($"CustomWindowCaptureHandler failed for window {windowToCapture.Handle} ('{windowToCapture.Text}'), falling back to standard capture.", ex);
                }
            }

            NativeRect windowRectangle = windowToCapture.WindowRectangle;
            bool dwmEnabled = DwmApi.IsDwmEnabled;

            using (Process process = windowToCapture.Process)
            {
                bool isAutoMode = windowCaptureMode == WindowCaptureMode.Auto;
                if (isAutoMode)
                {
                    windowCaptureMode = WindowCaptureMode.Screen;
                    if (WindowCapture.IsGdiAllowed(process))
                    {
                        if (!dwmEnabled && IsWpf(process))
                        {
                            Log.InfoFormat("Not using GDI for windows of process {0}, as the process uses WPF", process.ProcessName);
                        }
                        else
                        {
                            windowCaptureMode = WindowCaptureMode.GDI;
                        }
                    }

                    if (dwmEnabled && WindowCapture.IsDwmAllowed(process))
                    {
                        windowCaptureMode = WindowCaptureMode.Aero;
                    }
                }
                else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent)
                {
                    if (!dwmEnabled || !WindowCapture.IsDwmAllowed(process))
                    {
                        windowCaptureMode = WindowCaptureMode.Screen;
                        if (WindowCapture.IsGdiAllowed(process))
                        {
                            windowCaptureMode = WindowCaptureMode.GDI;
                        }
                    }
                }
                else if (windowCaptureMode == WindowCaptureMode.GDI && !WindowCapture.IsGdiAllowed(process))
                {
                    windowCaptureMode = WindowCaptureMode.Screen;
                }

                Log.InfoFormat("Capturing window with mode {0}", windowCaptureMode);
                bool captureTaken = false;
                windowRectangle = windowRectangle.Intersect(captureForWindow.ScreenBounds);

                int captureAttempts = 0;
                const int maxCaptureAttempts = 5;
                while (!captureTaken && captureAttempts < maxCaptureAttempts)
                {
                    captureAttempts++;
                    ICapture tmpCapture = null;
                    switch (windowCaptureMode)
                    {
                        case WindowCaptureMode.GDI:
                            if (WindowCapture.IsGdiAllowed(process))
                            {
                                if (windowToCapture.Iconic)
                                {
                                    windowToCapture.Restore();
                                }
                                else
                                {
                                    windowToCapture.ToForeground();
                                }

                                tmpCapture = windowToCapture.CaptureGdiWindow(captureForWindow);
                                if (tmpCapture != null)
                                {
                                    int blackCountGdi = ImageHelper.CountColor(tmpCapture.Image, Color.Black, false);
                                    int gdiPixels = tmpCapture.Image.Width * tmpCapture.Image.Height;
                                    int blackPercentageGdi = blackCountGdi * 100 / gdiPixels;
                                    if (blackPercentageGdi >= 1)
                                    {
                                        int screenPixels = windowRectangle.Width * windowRectangle.Height;
                                        using ICapture screenCapture = new Capture
                                        {
                                            CaptureDetails = captureForWindow.CaptureDetails
                                        };
                                        if (WindowCapture.CaptureRectangleFromDesktopScreen(screenCapture, windowRectangle) != null)
                                        {
                                            int blackCountScreen = ImageHelper.CountColor(screenCapture.Image, Color.Black, false);
                                            int blackPercentageScreen = blackCountScreen * 100 / screenPixels;
                                            if (screenPixels == gdiPixels)
                                            {
                                                if (blackPercentageGdi > blackPercentageScreen)
                                                {
                                                    Log.Debug("Using screen capture, as GDI had additional black.");
                                                    tmpCapture.Image = screenCapture.Image;
                                                    screenCapture.NullImage();
                                                }
                                            }
                                            else if (screenPixels < gdiPixels)
                                            {
                                                if (blackPercentageGdi > 50 && blackPercentageGdi > blackPercentageScreen)
                                                {
                                                    Log.Debug("Using screen capture, as GDI had additional black.");
                                                    tmpCapture.Image = screenCapture.Image;
                                                    screenCapture.NullImage();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (tmpCapture != null)
                            {
                                captureForWindow = tmpCapture;
                                captureTaken = true;
                            }
                            else
                            {
                                windowCaptureMode = WindowCaptureMode.Screen;
                            }
                            break;

                        case WindowCaptureMode.Aero:
                        case WindowCaptureMode.AeroTransparent:
                            if (WindowCapture.IsDwmAllowed(process))
                            {
                                tmpCapture = windowToCapture.CaptureDwmWindow(captureForWindow, windowCaptureMode, isAutoMode);
                            }

                            if (tmpCapture != null)
                            {
                                captureForWindow = tmpCapture;
                                captureTaken = true;
                            }
                            else
                            {
                                windowCaptureMode = WindowCaptureMode.GDI;
                            }
                            break;

                        default:
                            if (windowToCapture.Iconic)
                            {
                                windowToCapture.Restore();
                            }
                            else
                            {
                                windowToCapture.ToForeground();
                            }

                            try
                            {
                                captureForWindow = WindowCapture.CaptureRectangleFromDesktopScreen(captureForWindow, windowRectangle);
                                captureTaken = true;
                            }
                            catch (Exception e)
                            {
                                Log.Error("Problem capturing window from desktop screen", e);
                                return null;
                            }
                            break;
                    }
                }

                if (!captureTaken)
                {
                    Log.Warn("Failed to capture window after maximum attempts, all capture modes exhausted.");
                }
            }

            if (captureForWindow != null)
            {
                captureForWindow.CaptureDetails.Title = windowToCapture.Text;
            }

            return captureForWindow;
        }
    }
}
