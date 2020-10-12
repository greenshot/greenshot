using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using GreenshotPlugin.UnmanagedHelpers.Enums;
using GreenshotPlugin.UnmanagedHelpers.Structs;

namespace GreenshotPlugin.Core
{
    /// <summary>
    ///     This handles DPI changes see
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266.aspx">Writing DPI-Aware Desktop and Win32 Applications</a>
    /// </summary>
    public static class DpiHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DpiHelper));

        /// <summary>
        ///     This is the default DPI for the screen
        /// </summary>
        public const uint DefaultScreenDpi = 96;

        /// <summary>
        ///     Retrieve the current DPI for the UI element which is related to this DpiHandler
        /// </summary>
        public static uint Dpi { get; private set; } = WindowsVersion.IsWindows10OrLater ? GetDpiForSystem() : DefaultScreenDpi;

        /// <summary>
        /// Calculate a DPI scale factor
        /// </summary>
        /// <param name="dpi">uint</param>
        /// <returns>double</returns>
        public static float DpiScaleFactor(uint dpi)
        {
            if (dpi == 0)
            {
                dpi = Dpi;
            }
            return (float)dpi / DefaultScreenDpi;
        }

        /// <summary>
        ///     Scale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. the width 16 for 16x16 images</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with the scaled number</returns>
        public static float ScaleWithDpi(float someNumber, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return dpiScaleFactor * someNumber;
        }

        /// <summary>
        ///     Scale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="number">int with e.g. 16 for 16x16 images</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Scaled width</returns>
        public static int ScaleWithDpi(int number, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return (int)(dpiScaleFactor * number);
        }

        /// <summary>
        ///     Scale the supplied Size according to the supplied dpi
        /// </summary>
        /// <param name="size">Size to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static Size ScaleWithDpi(Size size, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new Size((int)(dpiScaleFactor * size.Width), (int)(dpiScaleFactor * size.Height));
        }

        /// <summary>
        ///     Scale the supplied NativePoint according to the supplied dpi
        /// </summary>
        /// <param name="size">NativePoint to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint scaled</returns>
        public static Point ScaleWithDpi(Point size, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new Point((int)(dpiScaleFactor * size.X), (int)(dpiScaleFactor * size.Y));
        }

        /// <summary>
        ///     Scale the supplied NativeSizeFloat according to the supplied dpi
        /// </summary>
        /// <param name="point">PointF</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>PointF</returns>
        public static PointF ScaleWithDpi(PointF point, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new PointF(dpiScaleFactor * point.X, dpiScaleFactor * point.Y);
        }

        /// <summary>
        ///     Scale the supplied NativeSizeFloat according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static SizeF ScaleWithDpi(SizeF size, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new SizeF(dpiScaleFactor * size.Width, dpiScaleFactor * size.Height);
        }

        /// <summary>
        ///     Scale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with scaled number</returns>
        public static float ScaleWithCurrentDpi(float someNumber, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">int with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>int with scaled number</returns>
        public static int ScaleWithCurrentDpi(int someNumber, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativeSize to the current dpi
        /// </summary>
        /// <param name="size">NativeSize to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static Size ScaleWithCurrentDpi(Size size, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativeSizeFloat to the current dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSizeFloat scaled</returns>
        public static SizeF ScaleWithCurrentDpi(SizeF size, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativePoint to the current dpi
        /// </summary>
        /// <param name="point">NativePoint to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint scaled</returns>
        public static Point ScaleWithCurrentDpi(Point point, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(point, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied PointF to the current dpi
        /// </summary>
        /// <param name="point">PointF to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>PointF scaled</returns>
        public static PointF ScaleWithCurrentDpi(PointF point, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(point, Dpi, scaleModifier);
        }

        /// <summary>
        /// Calculate a DPI unscale factor
        /// </summary>
        /// <param name="dpi">uint</param>
        /// <returns>float</returns>
        public static float DpiUnscaleFactor(uint dpi)
        {
            return (float)DefaultScreenDpi / dpi;
        }

        /// <summary>
        ///     Unscale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. the scaled width</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with the unscaled number</returns>
        public static float UnscaleWithDpi(float someNumber, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return dpiUnscaleFactor * someNumber;
        }

        /// <summary>
        ///    Unscale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="number">int with a scaled width</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Unscaled width</returns>
        public static int UnscaleWithDpi(int number, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return (int)(dpiUnscaleFactor * number);
        }

        /// <summary>
        ///     Unscale the supplied NativeSize according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSize to unscale</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Size unscaled</returns>
        public static Size UnscaleWithDpi(Size size, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return new Size((int)(dpiUnscaleFactor * size.Width), (int)(dpiUnscaleFactor * size.Height));
        }

        /// <summary>
        ///     Unscale the supplied Point according to the supplied dpi
        /// </summary>
        /// <param name="size">Point to unscale</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Point unscaled</returns>
        public static Point UnscaleWithDpi(Point point, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return new Point((int)(dpiUnscaleFactor * point.X), (int)(dpiUnscaleFactor * point.Y));
        }

        /// <summary>
        ///     unscale the supplied NativeSizeFloat according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>SizeF unscaled</returns>
        public static SizeF UnscaleWithDpi(SizeF size, uint dpi, Func<float, float> scaleModifier = null)
        {
            float dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return new SizeF(dpiUnscaleFactor * size.Width, dpiUnscaleFactor * size.Height);
        }

        /// <summary>
        ///     Unscale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with unscaled number</returns>
        public static float UnscaleWithCurrentDpi(float someNumber, Func<float, float> scaleModifier = null)
        {
            return UnscaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">int with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>int with unscaled number</returns>
        public static int UnscaleWithCurrentDpi(int someNumber, Func<float, float> scaleModifier = null)
        {
            return UnscaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativeSize to the current dpi
        /// </summary>
        /// <param name="size">Size to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Size unscaled</returns>
        public static Size UnscaleWithCurrentDpi(Size size, Func<float, float> scaleModifier = null)
        {
            return UnscaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativeSizeFloat to the current dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSizeFloat unscaled</returns>
        public static SizeF UnscaleWithCurrentDpi(SizeF size, Func<float, float> scaleModifier = null)
        {
            return UnscaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativePoint to the current dpi
        /// </summary>
        /// <param name="point">NativePoint to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint unscaled</returns>
        public static Point UnscaleWithCurrentDpi(Point point, Func<float, float> scaleModifier = null)
        {
            return UnscaleWithDpi(point, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativePointFloat to the current dpi
        /// </summary>
        /// <param name="point">NativePointFloat to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePointFloat unscaled</returns>
        public static PointF UnscaleWithCurrentDpi(PointF point, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(point, Dpi, scaleModifier);
        }

        /// <summary>
        /// public wrapper for EnableNonClientDpiScaling, this also checks if the function is available.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>true if it worked</returns>
        public static bool TryEnableNonClientDpiScaling(IntPtr hWnd)
        {
            // EnableNonClientDpiScaling is only available on Windows 10 and later
            if (!WindowsVersion.IsWindows10OrLater)
            {
                return false;
            }

            var result = EnableNonClientDpiScaling(hWnd);
            if (result.Succeeded())
            {
                return true;
            }

            var error = Win32.GetLastErrorCode();
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("Error enabling non client dpi scaling : {0}", Win32.GetMessage(error));
            }

            return false;
        }

        /// <summary>
        /// Make the current process DPI Aware, this should be done via the manifest but sometimes this is not possible.
        /// </summary>
        /// <returns>bool true if it was possible to change the DPI awareness</returns>
        public static bool EnableDpiAware()
        {
            // We can only test this for Windows 8.1 or later
            if (!WindowsVersion.IsWindows81OrLater)
            {
                Log.Debug("An application can only be DPI aware starting with Window 8.1 and later.");
                return false;
            }

            if (WindowsVersion.IsWindows10BuildOrLater(15063))
            {
                if (IsValidDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2))
                {
                    SetProcessDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2);
                }
                else
                {
                    SetProcessDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2);
                }

                return true;
            }
            return SetProcessDpiAwareness(DpiAwareness.PerMonitorAware).Succeeded();
        }

        /// <summary>
        ///     Check if the process is DPI Aware, an DpiHandler doesn't make sense if not.
        /// </summary>
        public static bool IsDpiAware
        {
            get
            {
                // We can only test this for Windows 8.1 or later
                if (!WindowsVersion.IsWindows81OrLater)
                {
                    Log.Debug("An application can only be DPI aware starting with Window 8.1 and later.");
                    return false;
                }

                using var process = Process.GetCurrentProcess();
                GetProcessDpiAwareness(process.Handle, out var dpiAwareness);
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat("Process {0} has a Dpi awareness {1}", process.ProcessName, dpiAwareness);
                }

                return dpiAwareness != DpiAwareness.Unaware && dpiAwareness != DpiAwareness.Invalid;
            }
        }

        /// <summary>
        /// Return the DPI for the screen which the location is located on
        /// </summary>
        /// <param name="location">POINT</param>
        /// <returns>uint</returns>
        public static uint GetDpi(POINT location)
        {
            RECT rect = new RECT(location.X, location.Y, 1,1);
            IntPtr hMonitor = User32.MonitorFromRect(ref rect, User32.MONITOR_DEFAULTTONEAREST);
            var result = GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY);
            if (result.Succeeded())
            {
                return dpiX;
            }
            return DefaultScreenDpi;
        }


        /// <summary>
        ///     Retrieve the DPI value for the supplied window handle
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>dpi value</returns>
        public static uint GetDpi(IntPtr hWnd)
        {
            if (!User32.IsWindow(hWnd))
            {
                return DefaultScreenDpi;
            }

            // Use the easiest method, but this only works for Windows 10
            if (WindowsVersion.IsWindows10OrLater)
            {
                return GetDpiForWindow(hWnd);
            }

            // Use the second easiest method, but this only works for Windows 8.1 or later
            if (WindowsVersion.IsWindows81OrLater)
            {
                var hMonitor = User32.MonitorFromWindow(hWnd, MonitorFrom.DefaultToNearest);
                // ReSharper disable once UnusedVariable
                var result = GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY);
                if (result.Succeeded())
                {
                    return dpiX;
                }
            }

            // Fallback to the global DPI settings
            using var hdc = SafeWindowDcHandle.FromWindow(hWnd);
            if (hdc == null)
            {
                return DefaultScreenDpi;
            }
            return (uint)GDI32.GetDeviceCaps(hdc, DeviceCaps.LOGPIXELSX);
        }

        /// <summary>
        /// See details <a hef="https://msdn.microsoft.com/en-us/library/windows/desktop/dn302113(v=vs.85).aspx">GetProcessDpiAwareness function</a>
        /// Retrieves the dots per inch (dpi) awareness of the specified process.
        /// </summary>
        /// <param name="processHandle">IntPtr with handle of the process that is being queried. If this parameter is NULL, the current process is queried.</param>
        /// <param name="value">out DpiAwareness - The DPI awareness of the specified process. Possible values are from the PROCESS_DPI_AWARENESS enumeration.</param>
        /// <returns>HResult</returns>
        [DllImport("shcore")]
        private static extern HResult GetProcessDpiAwareness(IntPtr processHandle, out DpiAwareness value);

        /// <summary>
        /// Sets the current process to a specified dots per inch (dpi) awareness level. The DPI awareness levels are from the PROCESS_DPI_AWARENESS enumeration.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn302122(v=vs.85).aspx">SetProcessDpiAwareness function</a>
        /// </summary>
        /// <param name="dpiAwareness">DpiAwareness</param>
        /// <returns>HResult</returns>
        [DllImport("shcore")]
        private static extern HResult SetProcessDpiAwareness(DpiAwareness dpiAwareness);

        /// <summary>
        /// It is recommended that you set the process-default DPI awareness via application manifest. See Setting the default DPI awareness for a process for more information. Setting the process-default DPI awareness via API call can lead to unexpected application behavior.
        ///
        /// Sets the current process to a specified dots per inch (dpi) awareness context. The DPI awareness contexts are from the DPI_AWARENESS_CONTEXT value.
        /// Remarks:
        /// This API is a more advanced version of the previously existing SetProcessDpiAwareness API, allowing for the process default to be set to the finer-grained DPI_AWARENESS_CONTEXT values. Most importantly, this allows you to programmatically set Per Monitor v2 as the process default value, which is not possible with the previous API.
        ///
        /// This method sets the default DPI_AWARENESS_CONTEXT for all threads within an application. Individual threads can have their DPI awareness changed from the default with the SetThreadDpiAwarenessContext method.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt807676(v=vs.85).aspx">SetProcessDpiAwarenessContext function</a>
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext</param>
        /// <returns>bool</returns>
        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748624(v=vs.85).aspx">GetDpiForWindow function</a>
        /// Returns the dots per inch (dpi) value for the associated window.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>uint with dpi</returns>
        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx">GetDpiForMonitor function</a>
        ///     Queries the dots per inch (dpi) of a display.
        /// </summary>
        /// <param name="hMonitor">IntPtr</param>
        /// <param name="dpiType">MonitorDpiType</param>
        /// <param name="dpiX">out int for the horizontal dpi</param>
        /// <param name="dpiY">out int for the vertical dpi</param>
        /// <returns>true if all okay</returns>
        [DllImport("shcore.dll", SetLastError = true)]
        private static extern HResult GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748621(v=vs.85).aspx">EnableNonClientDpiScaling function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>bool</returns>
        [DllImport("User32.dll", SetLastError = true)]
        private static extern HResult EnableNonClientDpiScaling(IntPtr hWnd);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748623(v=vs.85).aspx">GetDpiForSystem function</a>
        /// Returns the system DPI.
        /// </summary>
        /// <returns>uint with the system DPI</returns>
        [DllImport("User32.dll")]
        private static extern uint GetDpiForSystem();

        /// <summary>
        /// Converts a point in a window from logical coordinates into physical coordinates, regardless of the dots per inch (dpi) awareness of the caller. For more information about DPI awareness levels, see PROCESS_DPI_AWARENESS.
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn384110(v=vs.85).aspx">LogicalToPhysicalPointForPerMonitorDPI function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr A handle to the window whose transform is used for the conversion.</param>
        /// <param name="point">A pointer to a POINT structure that specifies the logical coordinates to be converted. The new physical coordinates are copied into this structure if the function succeeds.</param>
        /// <returns>bool</returns>
        [DllImport("User32.dll")]
        private static extern bool LogicalToPhysicalPointForPerMonitorDPI(IntPtr hWnd, ref POINT point);

        /// <summary>
        /// Converts a point in a window from logical coordinates into physical coordinates, regardless of the dots per inch (dpi) awareness of the caller. For more information about DPI awareness levels, see PROCESS_DPI_AWARENESS.
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn384112(v=vs.85).aspx">PhysicalToLogicalPointForPerMonitorDPI function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr A handle to the window whose transform is used for the conversion.</param>
        /// <param name="point">NativePoint A pointer to a POINT structure that specifies the physical/screen coordinates to be converted. The new logical coordinates are copied into this structure if the function succeeds.</param>
        /// <returns>bool</returns>
        [DllImport("User32.dll")]
        private static extern bool PhysicalToLogicalPointForPerMonitorDPI(IntPtr hWnd, ref POINT point);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">SystemParametersInfo function</a>
        ///     Retrieves the value of one of the system-wide parameters, taking into account the provided DPI value.
        /// </summary>
        /// <param name="uiAction">
        /// SystemParametersInfoActions The system-wide parameter to be retrieved.
        /// This function is only intended for use with SPI_GETICONTITLELOGFONT, SPI_GETICONMETRICS, or SPI_GETNONCLIENTMETRICS. See SystemParametersInfo for more information on these values.
        /// </param>
        /// <param name="uiParam">
        ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
        ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
        ///     zero for this parameter.
        /// </param>
        /// <param name="pvParam">IntPtr</param>
        /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
        /// <param name="dpi">uint with dpi value</param>
        /// <returns>bool</returns>
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoForDpi(SystemParametersInfoActions uiAction, uint uiParam, IntPtr pvParam, SystemParametersInfoBehaviors fWinIni, uint dpi);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748626(v=vs.85).aspx">GetThreadDpiAwarenessContext function</a>
        /// Gets the DPI_AWARENESS_CONTEXT for the current thread.
        ///
        /// This method will return the latest DPI_AWARENESS_CONTEXT sent to SetThreadDpiAwarenessContext. If SetThreadDpiAwarenessContext was never called for this thread, then the return value will equal the default DPI_AWARENESS_CONTEXT for the process.
        /// </summary>
        /// <returns>DpiAwarenessContext</returns>
        [DllImport("User32.dll")]
        private static extern DpiAwarenessContext GetThreadDpiAwarenessContext();

        /// <summary>
        /// Set the DPI awareness for the current thread to the provided value.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext the new value for the current thread</param>
        /// <returns>DpiAwarenessContext previous value</returns>
        [DllImport("User32.dll")]
        private static extern DpiAwarenessContext SetThreadDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Retrieves the DpiAwareness value from a DpiAwarenessContext.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext</param>
        /// <returns>DpiAwareness</returns>
        [DllImport("User32.dll")]
        private static extern DpiAwareness GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Retrieves the DPI from a given DPI_AWARENESS_CONTEXT handle. This enables you to determine the DPI of a thread without needed to examine a window created within that thread.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext</param>
        /// <returns>uint with dpi value</returns>
        [DllImport("User32.dll")]
        private static extern uint GetDpiFromDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Determines if a specified DPI_AWARENESS_CONTEXT is valid and supported by the current system.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext The context that you want to determine if it is supported.</param>
        /// <returns>bool true if supported otherwise false</returns>
        [DllImport("User32.dll")]
        private static extern bool IsValidDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Returns the DPI_HOSTING_BEHAVIOR of the specified window.
        ///
        /// This API allows you to examine the hosting behavior of a window after it has been created. A window's hosting behavior is the hosting behavior of the thread in which the window was created, as set by a call to SetThreadDpiHostingBehavior. This is a permanent value and cannot be changed after the window is created, even if the thread's hosting behavior is changed.
        /// </summary>
        /// <returns>DpiHostingBehavior</returns>
        [DllImport("User32.dll")]
        private static extern DpiHostingBehavior GetWindowDpiHostingBehavior();

        /// <summary>
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt845775.aspx">SetThreadDpiHostingBehavior function</a>
        /// Sets the thread's DPI_HOSTING_BEHAVIOR. This behavior allows windows created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT.
        ///
        /// DPI_HOSTING_BEHAVIOR enables a mixed content hosting behavior, which allows parent windows created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT value. This property only effects new windows created within this thread while the mixed hosting behavior is active. A parent window with this hosting behavior is able to host child windows with different DPI_AWARENESS_CONTEXT values, regardless of whether the child windows have mixed hosting behavior enabled.
        ///
        /// This hosting behavior does not allow for windows with per-monitor DPI_AWARENESS_CONTEXT values to be hosted until windows with DPI_AWARENESS_CONTEXT values of system or unaware.
        ///
        /// To avoid unexpected outcomes, a thread's DPI_HOSTING_BEHAVIOR should be changed to support mixed hosting behaviors only when creating a new window which needs to support those behaviors. Once that window is created, the hosting behavior should be switched back to its default value.
        ///
        /// This API is used to change the thread's DPI_HOSTING_BEHAVIOR from its default value. This is only necessary if your app needs to host child windows from plugins and third-party components that do not support per-monitor-aware context. This is most likely to occur if you are updating complex applications to support per-monitor DPI_AWARENESS_CONTEXT behaviors.
        ///
        /// Enabling mixed hosting behavior will not automatically adjust the thread's DPI_AWARENESS_CONTEXT to be compatible with legacy content. The thread's awareness context must still be manually changed before new windows are created to host such content.
        /// </summary>
        /// <param name="dpiHostingBehavior">DpiHostingBehavior</param>
        /// <returns>previous DpiHostingBehavior</returns>
        [DllImport("User32.dll")]
        private static extern DpiHostingBehavior SetThreadDpiHostingBehavior(DpiHostingBehavior dpiHostingBehavior);

        /// <summary>
        ///Retrieves the DPI_HOSTING_BEHAVIOR from the current thread.
        /// </summary>
        /// <returns>DpiHostingBehavior</returns>
        [DllImport("User32.dll")]
        private static extern DpiHostingBehavior GetThreadDpiHostingBehavior();

        /// <summary>
        /// Overrides the default per-monitor DPI scaling behavior of a child window in a dialog.
        /// This function returns TRUE if the operation was successful, and FALSE otherwise. To get extended error information, call GetLastError.
        ///
        /// Possible errors are ERROR_INVALID_HANDLE if passed an invalid HWND, and ERROR_ACCESS_DENIED if the windows belongs to another process.
        ///
        /// The behaviors are specified as values from the DIALOG_CONTROL_DPI_CHANGE_BEHAVIORS enum. This function follows the typical two-parameter approach to setting flags, where a mask specifies the subset of the flags to be changed.
        ///
        /// It is valid to set these behaviors on any window. It does not matter if the window is currently a child of a dialog at the point in time that SetDialogControlDpiChangeBehavior is called. The behaviors are retained and will take effect only when the window is an immediate child of a dialog that has per-monitor DPI scaling enabled.
        ///
        /// This API influences individual controls within dialogs. The dialog-wide per-monitor DPI scaling behavior is controlled by SetDialogDpiChangeBehavior.
        /// </summary>
        /// <param name="hWnd">IntPtr A handle for the window whose behavior will be modified.</param>
        /// <param name="mask">DialogScalingBehaviors A mask specifying the subset of flags to be changed.</param>
        /// <param name="values">DialogScalingBehaviors The desired value to be set for the specified subset of flags.</param>
        /// <returns>bool</returns>
        [DllImport("User32.dll")]
        private static extern bool SetDialogControlDpiChangeBehavior(IntPtr hWnd, DialogScalingBehaviors mask, DialogScalingBehaviors values);

        /// <summary>
        /// Retrieves and per-monitor DPI scaling behavior overrides of a child window in a dialog.
        /// The flags set on the given window. If passed an invalid handle, this function will return zero, and set its last error to ERROR_INVALID_HANDLE.
        /// </summary>
        /// <param name="hWnd">IntPtr A handle for the window whose behavior will be modified.</param>
        /// <returns>DialogScalingBehaviors</returns>
        [DllImport("User32.dll")]
        private static extern DialogScalingBehaviors GetDialogControlDpiChangeBehavior(IntPtr hWnd);
    }
}
