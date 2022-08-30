using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Enums;
using Dapplo.Windows.DesktopWindowsManager.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interop;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Code for handling with "windows"
    /// Main code is taken from vbAccelerator, location:
    /// https://www.vbaccelerator.com/home/NET/Code/Libraries/Windows/Enumerating_Windows/article.asp
    /// but a LOT of changes/enhancements were made to adapt it for Greenshot.
    ///
    /// Provides details about a Window returned by the  enumeration
    /// </summary>
    public class WindowDetails : IEquatable<WindowDetails>
    {
        private const string AppWindowClass = "Windows.UI.Core.CoreWindow"; //Used for Windows 8(.1)
        private const string AppFrameWindowClass = "ApplicationFrameWindow"; // Windows 10 uses ApplicationFrameWindow

        private static readonly IList<string> IgnoreClasses = new List<string>(new[]
        {
            "Progman", "Button", "Dwm"
        }); //"MS-SDIa"

        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowDetails));
        private static readonly CoreConfiguration Conf = IniConfig.GetIniSection<CoreConfiguration>();
        private static readonly IList<IntPtr> IgnoreHandles = new List<IntPtr>();
        private static readonly IList<string> ExcludeProcessesFromFreeze = new List<string>();
        private static readonly IAppVisibility AppVisibility;

        static WindowDetails()
        {
            try
            {
                // Only try to instantiate when Windows 8 or later.
                if (WindowsVersion.IsWindows8OrLater)
                {
                    AppVisibility = COMWrapper.CreateInstance<IAppVisibility>();
                }
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Couldn't create instance of IAppVisibility: {0}", ex.Message);
            }
        }

        public static void AddProcessToExcludeFromFreeze(string processName)
        {
            if (!ExcludeProcessesFromFreeze.Contains(processName))
            {
                ExcludeProcessesFromFreeze.Add(processName);
            }
        }

        internal static bool IsIgnoreHandle(IntPtr handle)
        {
            return IgnoreHandles.Contains(handle);
        }

        private IList<WindowDetails> _childWindows;
        private IntPtr _parentHandle = IntPtr.Zero;
        private WindowDetails _parent;
        private bool _frozen;
        
        /// <summary>
        /// This checks if the window is a Windows 10 App
        /// For Windows 10 apps are hosted inside "ApplicationFrameWindow"
        /// </summary>
        public bool IsWin10App => AppFrameWindowClass.Equals(ClassName);

        /// <summary>
        /// Check if this window belongs to a background app
        /// </summary>
        public bool IsBackgroundWin10App => WindowsVersion.IsWindows10OrLater && AppFrameWindowClass.Equals(ClassName) &&
                                            !Children.Any(window => string.Equals(window.ClassName, AppWindowClass));


        /// <summary>
        /// To allow items to be compared, the hash code
        /// is set to the Window handle, so two EnumWindowsItem
        /// objects for the same Window will be equal.
        /// </summary>
        /// <returns>The Window Handle for this window</returns>
        public override int GetHashCode()
        {
            return Handle.ToInt32();
        }

        public override bool Equals(object right)
        {
            return Equals(right as WindowDetails);
        }

        /// <summary>
        /// Compare two windows details
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WindowDetails other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            return other.Handle == Handle;
        }

        /// <summary>
        /// Check if the window has children
        /// </summary>
        public bool HasChildren => (_childWindows != null) && (_childWindows.Count > 0);

        /// <summary>
        /// Freeze information updates
        /// </summary>
        public void FreezeDetails()
        {
            _frozen = true;
        }

        /// <summary>
        /// Make the information update again.
        /// </summary>
        public void UnfreezeDetails()
        {
            _frozen = false;
        }

        /// <summary>
        /// Get the file path to the exe for the process which owns this window
        /// </summary>
        public string ProcessPath
        {
            get
            {
                if (Handle == IntPtr.Zero)
                {
                    // not a valid window handle
                    return string.Empty;
                }

                // Get the process id
                User32Api.GetWindowThreadProcessId(Handle, out var processId);
                return Kernel32Api.GetProcessPath(processId);
            }
        }


        /// <summary>
        /// Get the icon belonging to the process
        /// </summary>
        public Image DisplayIcon
        {
            get
            {
                try
                {
                    using var appIcon = GetAppIcon(Handle);
                    if (appIcon != null)
                    {
                        return appIcon.ToBitmap();
                    }
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Couldn't get icon for window {0} due to: {1}", Text, ex.Message);
                    Log.Warn(ex);
                }

                try
                {
                    return PluginUtils.GetCachedExeIcon(ProcessPath, 0);
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Couldn't get icon for window {0} due to: {1}", Text, ex.Message);
                    Log.Warn(ex);
                }

                return null;
            }
        }

        /// <summary>
        /// Get the icon for a hWnd
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        private static Icon GetAppIcon(IntPtr hWnd)
        {
            IntPtr iconSmall = IntPtr.Zero;
            IntPtr iconBig = new IntPtr(1);
            IntPtr iconSmall2 = new IntPtr(2);

            IntPtr iconHandle;
            if (Conf.UseLargeIcons)
            {
                iconHandle = User32Api.SendMessage(hWnd, WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
                if (iconHandle == IntPtr.Zero)
                {
                    iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.IconHandle);
                }
            }
            else
            {
                iconHandle = User32Api.SendMessage(hWnd, WindowsMessages.WM_GETICON, iconSmall2, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.SendMessage(hWnd, WindowsMessages.WM_GETICON, iconSmall, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.IconHandle);
            }

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.SendMessage(hWnd, WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.IconHandle);
            }

            if (iconHandle == IntPtr.Zero)
            {
                return null;
            }

            Icon icon = Icon.FromHandle(iconHandle);

            return icon;
        }

        /// <summary>
        /// Use this to make remove internal windows, like the mainform and the captureforms, invisible
        /// </summary>
        /// <param name="ignoreHandle"></param>
        public static void RegisterIgnoreHandle(IntPtr ignoreHandle)
        {
            IgnoreHandles.Add(ignoreHandle);
        }

        /// <summary>
        /// Use this to remove the with RegisterIgnoreHandle registered handle
        /// </summary>
        /// <param name="ignoreHandle"></param>
        public static void UnregisterIgnoreHandle(IntPtr ignoreHandle)
        {
            IgnoreHandles.Remove(ignoreHandle);
        }

        public IList<WindowDetails> Children
        {
            get
            {
                if (_childWindows == null)
                {
                    GetChildren();
                }

                return _childWindows;
            }
        }

        /// <summary>
        /// Retrieve the child with matching classname
        /// </summary>
        public WindowDetails GetChild(string childClassname)
        {
            foreach (var child in Children)
            {
                if (childClassname.Equals(child.ClassName))
                {
                    return child;
                }
            }

            return null;
        }

        public IntPtr ParentHandle
        {
            get
            {
                if (_parentHandle == IntPtr.Zero)
                {
                    _parentHandle = User32Api.GetParent(Handle);
                    _parent = null;
                }

                return _parentHandle;
            }
            set
            {
                if (_parentHandle != value)
                {
                    _parentHandle = value;
                    _parent = null;
                }
            }
        }

        /// <summary>
        /// Get the parent of the current window
        /// </summary>
        /// <returns>WindowDetails of the parent, or null if none</returns>
        public WindowDetails GetParent()
        {
            if (_parent == null)
            {
                if (_parentHandle == IntPtr.Zero)
                {
                    _parentHandle = User32Api.GetParent(Handle);
                }

                if (_parentHandle != IntPtr.Zero)
                {
                    _parent = new WindowDetails(_parentHandle);
                }
            }

            return _parent;
        }

        /// <summary>
        /// Retrieve all the children, this only stores the children internally.
        /// One should normally use the getter "Children"
        /// </summary>
        public IList<WindowDetails> GetChildren()
        {
            if (_childWindows == null)
            {
                return GetChildren(0);
            }

            return _childWindows;
        }

        /// <summary>
        /// Retrieve all the children, this only stores the children internally, use the "Children" property for the value
        /// </summary>
        /// <param name="levelsToGo">Specify how many levels we go in</param>
        public IList<WindowDetails> GetChildren(int levelsToGo)
        {
            if (_childWindows != null)
            {
                return _childWindows;
            }

            _childWindows = new WindowsEnumerator().GetWindows(Handle, null).Items;
            foreach (var childWindow in _childWindows)
            {
                if (levelsToGo > 0)
                {
                    childWindow.GetChildren(levelsToGo - 1);
                }
            }

            return _childWindows;
        }

        /// <summary>
        /// Gets the window's handle
        /// </summary>
        public IntPtr Handle { get; }

        private string _text;

        /// <summary>
        /// Gets the window's title (caption)
        /// </summary>
        public string Text
        {
            set => _text = value;
            get
            {
                if (_text == null)
                {
                    _text = User32Api.GetText(Handle);
                }

                return _text;
            }
        }

        private string _className;

        /// <summary>
        /// Gets the window's class name.
        /// </summary>
        public string ClassName => _className ??= User32Api.GetClassname(Handle);

        /// <summary>
        /// Gets/Sets whether the window is iconic (minimized) or not.
        /// </summary>
        public bool Iconic
        {
            get
            {
                return User32Api.IsIconic(Handle) || Location.X <= -32000;
            }
            set
            {
                if (value)
                {
                    User32Api.SendMessage(Handle, WindowsMessages.WM_SYSCOMMAND, SysCommands.SC_MINIMIZE, IntPtr.Zero);
                }
                else
                {
                    User32Api.SendMessage(Handle, WindowsMessages.WM_SYSCOMMAND, SysCommands.SC_RESTORE, IntPtr.Zero);
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether the window is maximized or not.
        /// </summary>
        public bool Maximised
        {
            get
            {
                return User32Api.IsZoomed(Handle);
            }
            set
            {
                if (value)
                {
                    User32Api.SendMessage(Handle, WindowsMessages.WM_SYSCOMMAND, SysCommands.SC_MAXIMIZE, IntPtr.Zero);
                }
                else
                {
                    User32Api.SendMessage(Handle, WindowsMessages.WM_SYSCOMMAND, SysCommands.SC_MINIMIZE, IntPtr.Zero);
                }
            }
        }

        /// <summary>
        /// Returns if this window is cloaked
        /// </summary>
        public bool IsCloaked
        {
            get => DwmApi.IsWindowCloaked(Handle);
        }

        /// <summary>
        /// Gets whether the window is visible.
        /// </summary>
        public bool Visible
        {
            get
            {
                // Tip from Raymond Chen https://devblogs.microsoft.com/oldnewthing/20200302-00/?p=103507
                if (IsCloaked)
                {
                    return false;
                }

                return User32Api.IsWindowVisible(Handle);
            }
        }

        public bool HasParent
        {
            get
            {
                GetParent();
                return _parentHandle != IntPtr.Zero;
            }
        }

        public int ProcessId
        {
            get
            {
                User32Api.GetWindowThreadProcessId(Handle, out var processId);
                return processId;
            }
        }

        public Process Process
        {
            get
            {
                try
                {
                    User32Api.GetWindowThreadProcessId(Handle, out var processId);
                    return Process.GetProcessById(processId);
                }
                catch (Exception ex)
                {
                    Log.Warn(ex);
                }

                return null;
            }
        }

        private NativeRect _previousWindowRectangle = NativeRect.Empty;
        private long _lastWindowRectangleRetrieveTime;
        private const long CacheTime = TimeSpan.TicksPerSecond * 2;

        /// <summary>
        /// Gets the bounding rectangle of the window
        /// </summary>
        public NativeRect WindowRectangle
        {
            get
            {
                // Try to return a cached value
                long now = DateTime.Now.Ticks;
                if (!_previousWindowRectangle.IsEmpty && _frozen) return _previousWindowRectangle;

                if (!_previousWindowRectangle.IsEmpty && now - _lastWindowRectangleRetrieveTime <= CacheTime)
                {
                    return _previousWindowRectangle;
                }
                NativeRect windowRect = new();
                if (DwmApi.IsDwmEnabled)
                {
                    bool gotFrameBounds = GetExtendedFrameBounds(out windowRect);
                    if (IsWin10App)
                    {
                        // Pre-Cache for maximized call, this is only on Windows 8 apps (full screen)
                        if (gotFrameBounds)
                        {
                            _previousWindowRectangle = windowRect;
                            _lastWindowRectangleRetrieveTime = now;
                        }
                    }

                    if (gotFrameBounds && WindowsVersion.IsWindows10OrLater && !Maximised)
                    {
                        // Somehow DWM doesn't calculate it correctly, there is a 1 pixel border around the capture
                        // Remove this border, currently it's fixed but TODO: Make it depend on the OS?
                        windowRect = windowRect.Inflate(Conf.Win10BorderCrop);
                        _previousWindowRectangle = windowRect;
                        _lastWindowRectangleRetrieveTime = now;
                        return windowRect;
                    }
                }

                if (windowRect.IsEmpty)
                {
                    if (!GetWindowRect(out windowRect))
                    {
                        Win32Error error = Win32.GetLastErrorCode();
                        Log.WarnFormat("Couldn't retrieve the windows rectangle: {0}", Win32.GetMessage(error));
                    }
                }

                _lastWindowRectangleRetrieveTime = now;
                // Try to return something valid, by getting returning the previous size if the window doesn't have a NativeRect anymore
                if (windowRect.IsEmpty)
                {
                    return _previousWindowRectangle;
                }

                _previousWindowRectangle = windowRect;
                return windowRect;

            }
        }

        /// <summary>
        /// Gets the location of the window relative to the screen.
        /// </summary>
        public NativePoint Location
        {
            get => WindowRectangle.Location;
        }

        /// <summary>
        /// Gets the size of the window.
        /// </summary>
        public NativeSize Size
        {
            get => WindowRectangle.Size;
        }

        /// <summary>
        /// Get the client rectangle, this is the part of the window inside the borders (drawable area)
        /// </summary>
        public NativeRect ClientRectangle
        {
            get
            {
                if (!GetClientRect(out var clientRect))
                {
                    Win32Error error = Win32.GetLastErrorCode();
                    Log.WarnFormat("Couldn't retrieve the client rectangle for {0}, error: {1}", Text, Win32.GetMessage(error));
                }

                return clientRect;
            }
        }

        /// <summary>
        /// Check if the supplied point lies in the window
        /// </summary>
        /// <param name="p">Point with the coordinates to check</param>
        /// <returns>true if the point lies within</returns>
        public bool Contains(NativePoint p)
        {
            return WindowRectangle.Contains(p);
        }

        /// <summary>
        /// Restores and Brings the window to the front,
        /// assuming it is a visible application window.
        /// </summary>
        public void Restore()
        {
            if (Iconic)
            {
                User32Api.SendMessage(Handle, WindowsMessages.WM_SYSCOMMAND, SysCommands.SC_RESTORE, IntPtr.Zero);
            }

            User32Api.BringWindowToTop(Handle);
            User32Api.SetForegroundWindow(Handle);
            // Make sure windows has time to perform the action
            // TODO: this is BAD practice!
            while (Iconic)
            {
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Get / Set the WindowStyle
        /// </summary>
        public WindowStyleFlags WindowStyle
        {
            get => unchecked(
                (WindowStyleFlags)User32Api.GetWindowLongWrapper(Handle, WindowLongIndex.GWL_STYLE).ToInt64()
            );
            set => User32Api.SetWindowLongWrapper(Handle, WindowLongIndex.GWL_STYLE, new IntPtr((long) value));
        }

        /// <summary>
        /// Get/Set the WindowPlacement
        /// </summary>
        public WindowPlacement WindowPlacement
        {
            get
            {
                var placement = WindowPlacement.Create();
                User32Api.GetWindowPlacement(Handle, ref placement);
                return placement;
            }
            set { User32Api.SetWindowPlacement(Handle, ref value); }
        }

        /// <summary>
        /// Get/Set the Extended WindowStyle
        /// </summary>
        public ExtendedWindowStyleFlags ExtendedWindowStyle
        {
            get => (ExtendedWindowStyleFlags) User32Api.GetWindowLongWrapper(Handle, WindowLongIndex.GWL_EXSTYLE);
            set => User32Api.SetWindowLongWrapper(Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint) value));
        }

        /// <summary>
        /// Capture Window with GDI+
        /// </summary>
        /// <param name="capture">The capture to fill</param>
        /// <returns>ICapture</returns>
        public ICapture CaptureGdiWindow(ICapture capture)
        {
            Image capturedImage = PrintWindow();
            if (capturedImage == null) return null;
            capture.Image = capturedImage;
            capture.Location = Location;
            return capture;

        }

        /// <summary>
        /// Capture DWM Window
        /// </summary>
        /// <param name="capture">Capture to fill</param>
        /// <param name="windowCaptureMode">Wanted WindowCaptureMode</param>
        /// <param name="autoMode">True if auto mode is used</param>
        /// <returns>ICapture with the capture</returns>
        public ICapture CaptureDwmWindow(ICapture capture, WindowCaptureMode windowCaptureMode, bool autoMode)
        {
            IntPtr thumbnailHandle = IntPtr.Zero;
            Form tempForm = null;
            bool tempFormShown = false;
            try
            {
                tempForm = new Form
                {
                    ShowInTaskbar = false,
                    FormBorderStyle = FormBorderStyle.None,
                    TopMost = true
                };

                // Register the Thumbnail
                DwmApi.DwmRegisterThumbnail(tempForm.Handle, Handle, out thumbnailHandle);

                // Get the original size
                DwmApi.DwmQueryThumbnailSourceSize(thumbnailHandle, out var sourceSize);

                if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
                {
                    return null;
                }

                // Calculate the location of the temp form
                NativeRect windowRectangle = WindowRectangle;
                NativePoint formLocation = windowRectangle.Location;
                NativeSize borderSize = new NativeSize();
                bool doesCaptureFit = false;
                if (!Maximised)
                {
                    // Assume using it's own location
                    formLocation = windowRectangle.Location;
                    // TODO: Use Rectangle.Union!
                    using Region workingArea = new Region(Screen.PrimaryScreen.Bounds);
                    // Find the screen where the window is and check if it fits
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        if (!Equals(screen, Screen.PrimaryScreen))
                        {
                            workingArea.Union(screen.Bounds);
                        }
                    }

                    // If the formLocation is not inside the visible area
                    if (!workingArea.AreRectangleCornersVisisble(windowRectangle))
                    {
                        // If none found we find the biggest screen

                        foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
                        {
                            var newWindowRectangle = new NativeRect(displayInfo.WorkingArea.Location, windowRectangle.Size);
                            if (workingArea.AreRectangleCornersVisisble(newWindowRectangle))
                            {
                                formLocation = displayInfo.Bounds.Location;
                                doesCaptureFit = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        doesCaptureFit = true;
                    }
                }
                else if (!WindowsVersion.IsWindows8OrLater)
                {
                    //GetClientRect(out windowRectangle);
                    GetBorderSize(out borderSize);
                    formLocation = new NativePoint(windowRectangle.X - borderSize.Width, windowRectangle.Y - borderSize.Height);
                }

                tempForm.Location = formLocation;
                tempForm.Size = sourceSize;

                // Prepare rectangle to capture from the screen.
                var captureRectangle = new NativeRect(formLocation.X, formLocation.Y, sourceSize.Width, sourceSize.Height);
                if (Maximised)
                {
                    // Correct capture size for maximized window by offsetting the X,Y with the border size
                    // and subtracting the border from the size (2 times, as we move right/down for the capture without resizing)
                    captureRectangle = captureRectangle.Inflate(borderSize.Width, borderSize.Height);
                }
                else
                {
                    // TODO: Also 8.x?
                    if (WindowsVersion.IsWindows10OrLater)
                    {
                        captureRectangle = captureRectangle.Inflate(Conf.Win10BorderCrop);
                    }

                    if (autoMode)
                    {
                        // check if the capture fits
                        if (!doesCaptureFit)
                        {
                            // if GDI is allowed.. (a screenshot won't be better than we comes if we continue)
                            using Process thisWindowProcess = Process;
                            if (WindowCapture.IsGdiAllowed(thisWindowProcess))
                            {
                                // we return null which causes the capturing code to try another method.
                                return null;
                            }
                        }
                    }
                }

                // Prepare the displaying of the Thumbnail
                var props = new DwmThumbnailProperties()
                {
                    Opacity = 255,
                    Visible = true,
                    Destination = new NativeRect(0, 0, sourceSize.Width, sourceSize.Height)
                };
                DwmApi.DwmUpdateThumbnailProperties(thumbnailHandle, ref props);
                tempForm.Show();
                tempFormShown = true;

                // Intersect with screen
                captureRectangle = captureRectangle.Intersect(capture.ScreenBounds);

                // Destination bitmap for the capture
                Bitmap capturedBitmap = null;
                bool frozen = false;
                try
                {
                    // Check if we make a transparent capture
                    if (windowCaptureMode == WindowCaptureMode.AeroTransparent)
                    {
                        frozen = FreezeWindow();
                        // Use white, later black to capture transparent
                        tempForm.BackColor = Color.White;
                        // Make sure everything is visible
                        tempForm.Refresh();
                        Application.DoEvents();

                        try
                        {
                            using Bitmap whiteBitmap = WindowCapture.CaptureRectangle(captureRectangle);
                            // Apply a white color
                            tempForm.BackColor = Color.Black;
                            // Make sure everything is visible
                            tempForm.Refresh();
                            // Make sure the application window is active, so the colors & buttons are right
                            ToForeground();

                            // Make sure all changes are processed and visible
                            Application.DoEvents();
                            using Bitmap blackBitmap = WindowCapture.CaptureRectangle(captureRectangle);
                            capturedBitmap = ApplyTransparency(blackBitmap, whiteBitmap);
                        }
                        catch (Exception e)
                        {
                            Log.Debug("Exception: ", e);
                            // Some problem occurred, cleanup and make a normal capture
                            if (capturedBitmap != null)
                            {
                                capturedBitmap.Dispose();
                                capturedBitmap = null;
                            }
                        }
                    }

                    // If no capture up till now, create a normal capture.
                    if (capturedBitmap == null)
                    {
                        // Remove transparency, this will break the capturing
                        if (!autoMode)
                        {
                            tempForm.BackColor = Color.FromArgb(255, Conf.DWMBackgroundColor.R, Conf.DWMBackgroundColor.G, Conf.DWMBackgroundColor.B);
                        }
                        else
                        {
                            var colorizationColor = DwmApi.ColorizationColor;
                            // Modify by losing the transparency and increasing the intensity (as if the background color is white)
                            tempForm.BackColor = Color.FromArgb(255, (colorizationColor.R + 255) >> 1, (colorizationColor.G + 255) >> 1, (colorizationColor.B + 255) >> 1);
                        }

                        // Make sure everything is visible
                        tempForm.Refresh();
                        // Make sure the application window is active, so the colors & buttons are right
                        ToForeground();

                        // Make sure all changes are processed and visible
                        Application.DoEvents();
                        // Capture from the screen
                        capturedBitmap = WindowCapture.CaptureRectangle(captureRectangle);
                    }

                    if (capturedBitmap != null)
                    {
                        // Not needed for Windows 8
                        if (!WindowsVersion.IsWindows8OrLater)
                        {
                            // Only if the Inivalue is set, not maximized and it's not a tool window.
                            if (Conf.WindowCaptureRemoveCorners && !Maximised && (ExtendedWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) == 0)
                            {
                                // Remove corners
                                if (!Image.IsAlphaPixelFormat(capturedBitmap.PixelFormat))
                                {
                                    Log.Debug("Changing pixelformat to Alpha for the RemoveCorners");
                                    Bitmap tmpBitmap = ImageHelper.Clone(capturedBitmap, PixelFormat.Format32bppArgb);
                                    capturedBitmap.Dispose();
                                    capturedBitmap = tmpBitmap;
                                }

                                RemoveCorners(capturedBitmap);
                            }
                        }
                    }
                }
                finally
                {
                    // Make sure to ALWAYS unfreeze!!
                    if (frozen)
                    {
                        UnfreezeWindow();
                    }
                }

                capture.Image = capturedBitmap;
                // Make sure the capture location is the location of the window, not the copy
                capture.Location = Location;
            }
            finally
            {
                if (thumbnailHandle != IntPtr.Zero)
                {
                    // Un-register (cleanup), as we are finished we don't need the form or the thumbnail anymore
                    DwmApi.DwmUnregisterThumbnail(thumbnailHandle);
                }

                if (tempForm != null)
                {
                    if (tempFormShown)
                    {
                        tempForm.Close();
                    }

                    tempForm.Dispose();
                }
            }

            return capture;
        }

        /// <summary>
        /// Helper method to remove the corners from a DMW capture
        /// </summary>
        /// <param name="image">The bitmap to remove the corners from.</param>
        private void RemoveCorners(Bitmap image)
        {
            using IFastBitmap fastBitmap = FastBitmap.Create(image);
            for (int y = 0; y < Conf.WindowCornerCutShape.Count; y++)
            {
                for (int x = 0; x < Conf.WindowCornerCutShape[y]; x++)
                {
                    fastBitmap.SetColorAt(x, y, Color.Transparent);
                    fastBitmap.SetColorAt(image.Width - 1 - x, y, Color.Transparent);
                    fastBitmap.SetColorAt(image.Width - 1 - x, image.Height - 1 - y, Color.Transparent);
                    fastBitmap.SetColorAt(x, image.Height - 1 - y, Color.Transparent);
                }
            }
        }

        /// <summary>
        /// Apply transparency by comparing a transparent capture with a black and white background
        /// A "Math.min" makes sure there is no overflow, but this could cause the picture to have shifted colors.
        /// The pictures should have been taken without difference, except for the colors.
        /// </summary>
        /// <param name="blackBitmap">Bitmap with the black image</param>
        /// <param name="whiteBitmap">Bitmap with the black image</param>
        /// <returns>Bitmap with transparency</returns>
        private Bitmap ApplyTransparency(Bitmap blackBitmap, Bitmap whiteBitmap)
        {
            using IFastBitmap targetBuffer = FastBitmap.CreateEmpty(blackBitmap.Size, PixelFormat.Format32bppArgb, Color.Transparent);
            targetBuffer.SetResolution(blackBitmap.HorizontalResolution, blackBitmap.VerticalResolution);
            using (IFastBitmap blackBuffer = FastBitmap.Create(blackBitmap))
            {
                using IFastBitmap whiteBuffer = FastBitmap.Create(whiteBitmap);
                for (int y = 0; y < blackBuffer.Height; y++)
                {
                    for (int x = 0; x < blackBuffer.Width; x++)
                    {
                        Color c0 = blackBuffer.GetColorAt(x, y);
                        Color c1 = whiteBuffer.GetColorAt(x, y);
                        // Calculate alpha as double in range 0-1
                        int alpha = c0.R - c1.R + 255;
                        if (alpha == 255)
                        {
                            // Alpha == 255 means no change!
                            targetBuffer.SetColorAt(x, y, c0);
                        }
                        else if (alpha == 0)
                        {
                            // Complete transparency, use transparent pixel
                            targetBuffer.SetColorAt(x, y, Color.Transparent);
                        }
                        else
                        {
                            // Calculate original color
                            byte originalAlpha = (byte) Math.Min(255, alpha);
                            var alphaFactor = alpha / 255d;
                            //LOG.DebugFormat("Alpha {0} & c0 {1} & c1 {2}", alpha, c0, c1);
                            byte originalRed = (byte) Math.Min(255, c0.R / alphaFactor);
                            byte originalGreen = (byte) Math.Min(255, c0.G / alphaFactor);
                            byte originalBlue = (byte) Math.Min(255, c0.B / alphaFactor);
                            Color originalColor = Color.FromArgb(originalAlpha, originalRed, originalGreen, originalBlue);
                            //Color originalColor = Color.FromArgb(originalAlpha, originalRed, c0.G, c0.B);
                            targetBuffer.SetColorAt(x, y, originalColor);
                        }
                    }
                }
            }

            return targetBuffer.UnlockAndReturnBitmap();
        }

        /// <summary>
        /// If a window is hidden (Iconic), it also has the specified dimensions.
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <returns>bool true if hidden</returns>
        private bool IsHidden(NativeRect rect) => rect.Width == 65535 && rect.Height == 65535 && rect.Left == 32767 && rect.Top == 32767;

        /// <summary>
        /// Helper method to get the window size for DWM Windows
        /// </summary>
        /// <param name="rectangle">out NativeRect</param>
        /// <returns>bool true if it worked</returns>
        private bool GetExtendedFrameBounds(out NativeRect rectangle)
        {
            var result = DwmApi.DwmGetWindowAttribute(Handle, DwmWindowAttributes.ExtendedFrameBounds, out NativeRect rect, Marshal.SizeOf(typeof(NativeRect)));
            if (result.Succeeded())
            {
                if (IsHidden(rect))
                {
                    rect = NativeRect.Empty;
                }
                rectangle = rect;
                return true;
            }

            rectangle = NativeRect.Empty;
            return false;
        }

        /// <summary>
        /// Helper method to get the window size for GDI Windows
        /// </summary>
        /// <param name="rectangle">out NativeRect</param>
        /// <returns>bool true if it worked</returns>
        private bool GetClientRect(out NativeRect rectangle)
        {
            var windowInfo = new WindowInfo();
            // Get the Window Info for this window
            bool result = User32Api.GetWindowInfo(Handle, ref windowInfo);
            rectangle = result ? windowInfo.ClientBounds : NativeRect.Empty;
            return result;
        }

        /// <summary>
        /// Helper method to get the window size for GDI Windows
        /// </summary>
        /// <param name="rectangle">out NativeRect</param>
        /// <returns>bool true if it worked</returns>
        private bool GetWindowRect(out NativeRect rectangle)
        {
            var windowInfo = new WindowInfo();
            // Get the Window Info for this window
            bool result = User32Api.GetWindowInfo(Handle, ref windowInfo);
            if (IsHidden(windowInfo.Bounds))
            {
                rectangle = NativeRect.Empty;
            }
            else
            {
                rectangle = result ? windowInfo.Bounds : NativeRect.Empty;
            }
            return result;
        }

        /// <summary>
        /// Helper method to get the Border size for GDI Windows
        /// </summary>
        /// <param name="size">out Size</param>
        /// <returns>bool true if it worked</returns>
        private bool GetBorderSize(out NativeSize size)
        {
            var windowInfo = new WindowInfo();
            // Get the Window Info for this window
            bool result = User32Api.GetWindowInfo(Handle, ref windowInfo);
            size = result ? windowInfo.BorderSize : NativeSize.Empty;
            return result;
        }

        /// <summary>
        /// Set the window as foreground window
        /// </summary>
        /// <param name="hWnd">hWnd of the window to bring to the foreground</param>
        public static void ToForeground(IntPtr hWnd)
        {
            var foregroundWindow = User32Api.GetForegroundWindow();
            if (hWnd == foregroundWindow)
            {
                return;
            }

            var window = new WindowDetails(hWnd);
            // Nothing we can do if it's not visible!
            if (!window.Visible)
            {
                return;
            }

            var threadId1 = User32Api.GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
            var threadId2 = User32Api.GetWindowThreadProcessId(hWnd, IntPtr.Zero);

            // Show window in foreground.
            if (threadId1 != threadId2)
            {
                User32Api.AttachThreadInput(threadId1, threadId2, 1);
                User32Api.SetForegroundWindow(hWnd);
                User32Api.AttachThreadInput(threadId1, threadId2, 0);
            }
            else
            {
                User32Api.SetForegroundWindow(hWnd);
            }

            User32Api.BringWindowToTop(hWnd);

            if (window.Iconic)
            {
                window.Iconic = false;
            }
        }

        /// <summary>
        /// Set the window as foreground window
        /// </summary>
        public void ToForeground()
        {
            ToForeground(Handle);
        }

        /// <summary>
        /// Get the region for a window
        /// </summary>
        private Region GetRegion()
        {
            using (SafeRegionHandle region = Gdi32Api.CreateRectRgn(0, 0, 0, 0))
            {
                if (!region.IsInvalid)
                {
                    var result = User32Api.GetWindowRgn(Handle, region);
                    if (result != RegionResults.Error && result != RegionResults.NullRegion)
                    {
                        return Region.FromHrgn(region.DangerousGetHandle());
                    }
                }
            }

            return null;
        }

        private bool CanFreezeOrUnfreeze(string titleOrProcessname)
        {
            if (string.IsNullOrEmpty(titleOrProcessname))
            {
                return false;
            }

            if (titleOrProcessname.ToLower().Contains("greenshot"))
            {
                return false;
            }

            foreach (string excludeProcess in ExcludeProcessesFromFreeze)
            {
                if (titleOrProcessname.ToLower().Contains(excludeProcess))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Freezes the process belonging to the window
        /// Warning: Use only if no other way!!
        /// </summary>
        private bool FreezeWindow()
        {
            bool frozen = false;
            using (Process proc = Process.GetProcessById(ProcessId))
            {
                string processName = proc.ProcessName;
                if (!CanFreezeOrUnfreeze(processName))
                {
                    Log.DebugFormat("Not freezing {0}", processName);
                    return false;
                }

                if (!CanFreezeOrUnfreeze(Text))
                {
                    Log.DebugFormat("Not freezing {0}", processName);
                    return false;
                }

                Log.DebugFormat("Freezing process: {0}", processName);


                foreach (ProcessThread pT in proc.Threads)
                {
                    IntPtr pOpenThread = Kernel32Api.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint) pT.Id);

                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                    }

                    frozen = true;
                    Kernel32Api.SuspendThread(pOpenThread);
                    pT.Dispose();
                }
            }

            return frozen;
        }

        /// <summary>
        /// Unfreeze the process belonging to the window
        /// </summary>
        public void UnfreezeWindow()
        {
            using Process proc = Process.GetProcessById(ProcessId);
            string processName = proc.ProcessName;
            if (!CanFreezeOrUnfreeze(processName))
            {
                Log.DebugFormat("Not unfreezing {0}", processName);
                return;
            }

            if (!CanFreezeOrUnfreeze(Text))
            {
                Log.DebugFormat("Not unfreezing {0}", processName);
                return;
            }

            Log.DebugFormat("Unfreezing process: {0}", processName);

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = Kernel32Api.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint) pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                Kernel32Api.ResumeThread(pOpenThread);
            }
        }

        /// <summary>
        /// Return an Image representing the Window!
        /// For Windows 7, as GDI+ draws it, it will be without Aero borders!
        /// For Windows 10+, there is an option PW_RENDERFULLCONTENT, which makes sure the capture is "as is".
        /// </summary>
        public Image PrintWindow()
        {
            NativeRect windowRect = WindowRectangle;
            // Start the capture
            Exception exceptionOccurred = null;
            Image returnImage;
            using (Region region = GetRegion())
            {
                var backgroundColor = Color.Black;
                PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
                // Only use 32 bpp ARGB when the window has a region
                if (region != null)
                {
                    pixelFormat = PixelFormat.Format32bppArgb;
                    backgroundColor = Color.Transparent;
                }
                
                returnImage = ImageHelper.CreateEmpty(windowRect.Width, windowRect.Height, pixelFormat, backgroundColor, 96,96);
                using Graphics graphics = Graphics.FromImage(returnImage);
                using (SafeGraphicsDcHandle graphicsDc = graphics.GetSafeDeviceContext())
                {
                    var pwFlags = WindowsVersion.IsWindows10OrLater ? PrintWindowFlags.PW_RENDERFULLCONTENT : PrintWindowFlags.PW_COMPLETE;
                    bool printSucceeded = User32Api.PrintWindow(Handle, graphicsDc.DangerousGetHandle(), pwFlags);
                    if (!printSucceeded)
                    {
                        // something went wrong, most likely a "0x80004005" (Access Denied) when using UAC
                        exceptionOccurred = User32Api.CreateWin32Exception("PrintWindow");
                    }
                }

                // Apply the region "transparency"
                if (region != null && !region.IsEmpty(graphics))
                {
                    graphics.ExcludeClip(region);
                    graphics.Clear(Color.Transparent);
                }

                graphics.Flush();
            }

            // Return null if error
            if (exceptionOccurred != null)
            {
                Log.ErrorFormat("Error calling print window: {0}", exceptionOccurred.Message);
                returnImage.Dispose();
                return null;
            }

            if (!HasParent && Maximised)
            {
                Log.Debug("Correcting for maximized window");
                GetBorderSize(out var borderSize);
                NativeRect borderRectangle = new NativeRect(borderSize.Width, borderSize.Height, windowRect.Width - (2 * borderSize.Width), windowRect.Height - (2 * borderSize.Height));
                ImageHelper.Crop(ref returnImage, ref borderRectangle);
            }

            return returnImage;
        }

        /// <summary>
        ///  Constructs a new instance of this class for
        ///  the specified Window Handle.
        /// </summary>
        /// <param name="hWnd">The Window Handle</param>
        public WindowDetails(IntPtr hWnd)
        {
            Handle = hWnd;
        }

        /// <summary>
        /// Gets an instance of the current active foreground window
        /// </summary>
        /// <returns>WindowDetails of the current window</returns>
        public static WindowDetails GetActiveWindow()
        {
            IntPtr hWnd = User32Api.GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                if (IgnoreHandles.Contains(hWnd))
                {
                    return GetDesktopWindow();
                }

                WindowDetails activeWindow = new WindowDetails(hWnd);
                // Invisible Windows should not be active
                if (!activeWindow.Visible)
                {
                    return GetDesktopWindow();
                }

                return activeWindow;
            }

            return null;
        }

        /// <summary>
        /// Gets the Desktop window
        /// </summary>
        /// <returns>WindowDetails for the desktop window</returns>
        public static WindowDetails GetDesktopWindow()
        {
            return new WindowDetails(User32Api.GetDesktopWindow());
        }

        /// <summary>
        /// Get all the top level windows
        /// </summary>
        /// <returns>List of WindowDetails with all the top level windows</returns>
        public static IList<WindowDetails> GetAllWindows()
        {
            return GetAllWindows(null);
        }

        /// <summary>
        /// Get all the top level windows, with matching classname
        /// </summary>
        /// <returns>List WindowDetails with all the top level windows</returns>
        public static IList<WindowDetails> GetAllWindows(string classname)
        {
            return new WindowsEnumerator().GetWindows(IntPtr.Zero, classname).Items;
        }

        /// <summary>
        /// Recursive "find children which"
        /// </summary>
        /// <param name="point">NativePoint to check for</param>
        /// <returns>WindowDetails</returns>
        public WindowDetails FindChildUnderPoint(NativePoint point)
        {
            if (!Contains(point))
            {
                return null;
            }

            var rect = WindowRectangle;
            // If the mouse it at the edge, take the whole window
            if (rect.X == point.X || rect.Y == point.Y || rect.Right == point.X || rect.Bottom == point.Y)
            {
                return this;
            }

            // Look into the child windows
            foreach (var childWindow in Children)
            {
                if (childWindow.Contains(point))
                {
                    return childWindow.FindChildUnderPoint(point);
                }
            }

            return this;
        }

        /// <summary>
        /// Helper method to decide if a top level window is visible
        /// </summary>
        /// <param name="window"></param>
        /// <param name="screenBounds"></param>
        /// <returns></returns>
        private static bool IsVisible(WindowDetails window, NativeRect screenBounds)
        {
            // Ignore invisible
            if (!window.Visible)
            {
                return false;
            }

            // Ignore minimized
            if (window.Iconic)
            {
                return false;
            }

            if (IgnoreClasses.Contains(window.ClassName))
            {
                return false;
            }

            // On windows which are visible on the screen
            var windowRect = window.WindowRectangle.Intersect(screenBounds);
            if (windowRect.IsEmpty)
            {
                return false;
            }

            // Skip everything which is not rendered "normally", trying to fix BUG-2017
            var exWindowStyle = window.ExtendedWindowStyle;
            if (!window.IsWin10App && (exWindowStyle & ExtendedWindowStyleFlags.WS_EX_NOREDIRECTIONBITMAP) != 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get all the visible top level windows
        /// </summary>
        /// <returns>List WindowDetails with all the visible top level windows</returns>
        public static IEnumerable<WindowDetails> GetVisibleWindows()
        {
            var screenBounds = DisplayInfo.ScreenBounds;

            foreach (var window in GetAllWindows())
            {
                if (IsVisible(window, screenBounds))
                {
                    yield return window;
                }
            }
        }

        /// <summary>
        /// Check if the window is a top level
        /// </summary>
        /// <param name="window">WindowDetails</param>
        /// <returns>bool</returns>
        private static bool IsTopLevel(WindowDetails window)
        {
            if (window.IsCloaked)
            {
                return false;
            }

            // Windows without size
            if (window.WindowRectangle.Size.Width * window.WindowRectangle.Size.Height == 0)
            {
                return false;
            }

            if (window.HasParent)
            {
                return false;
            }

            var exWindowStyle = window.ExtendedWindowStyle;
            if ((exWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            // Skip everything which is not rendered "normally", trying to fix BUG-2017
            if (!window.IsWin10App && (exWindowStyle & ExtendedWindowStyleFlags.WS_EX_NOREDIRECTIONBITMAP) != 0)
            {
                return false;
            }

            // Skip preview windows, like the one from Firefox
            if ((window.WindowStyle & WindowStyleFlags.WS_VISIBLE) == 0)
            {
                return false;
            }

            // Ignore windows without title
            if (window.Text.Length == 0)
            {
                return false;
            }

            if (IgnoreClasses.Contains(window.ClassName))
            {
                return false;
            }

            if (!(window.Visible || window.Iconic))
            {
                return false;
            }

            return !window.IsBackgroundWin10App;
        }

        /// <summary>
        /// Get all the top level windows
        /// </summary>
        /// <returns>List WindowDetails with all the top level windows</returns>
        public static IEnumerable<WindowDetails> GetTopLevelWindows()
        {
            foreach (var possibleTopLevel in GetAllWindows())
            {
                if (IsTopLevel(possibleTopLevel))
                {
                    yield return possibleTopLevel;
                }
            }
        }

        /// <summary>
        /// Find a window belonging to the same process as the supplied window.
        /// </summary>
        /// <param name="windowToLinkTo"></param>
        /// <returns></returns>
        public static WindowDetails GetLinkedWindow(WindowDetails windowToLinkTo)
        {
            int processIdSelectedWindow = windowToLinkTo.ProcessId;
            foreach (var window in GetAllWindows())
            {
                // Ignore windows without title
                if (window.Text.Length == 0)
                {
                    continue;
                }

                // Ignore invisible
                if (!window.Visible)
                {
                    continue;
                }

                if (window.Handle == windowToLinkTo.Handle)
                {
                    continue;
                }

                if (window.Iconic)
                {
                    continue;
                }

                // Windows without size
                Size windowSize = window.WindowRectangle.Size;
                if (windowSize.Width == 0 || windowSize.Height == 0)
                {
                    continue;
                }

                if (window.ProcessId == processIdSelectedWindow)
                {
                    Log.InfoFormat("Found window {0} belonging to same process as the window {1}", window.Text, windowToLinkTo.Text);
                    return window;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method to "active" all windows that are not in the supplied list.
        /// One should preferably call "GetVisibleWindows" for the oldWindows.
        /// </summary>
        /// <param name="oldWindows">List WindowDetails with old windows</param>
        public static void ActiveNewerWindows(IEnumerable<WindowDetails> oldWindows)
        {
            var oldWindowsList = new List<WindowDetails>(oldWindows);
            foreach (var window in GetVisibleWindows())
            {
                if (!oldWindowsList.Contains(window))
                {
                    window.ToForeground();
                }
            }
        }

        /// <summary>
        /// Return true if the metro-app-launcher is visible
        /// </summary>
        /// <returns></returns>
        public static bool IsAppLauncherVisible
        {
            get
            {
                if (AppVisibility != null)
                {
                    return AppVisibility.IsLauncherVisible;
                }

                return false;
            }
        }

        /// <summary>
        /// Make a string representation of the window details
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            var result = new StringBuilder();
            result.AppendLine($"Text: {Text}");
            result.AppendLine($"ClassName: {ClassName}");
            result.AppendLine($"ExtendedWindowStyle: {ExtendedWindowStyle}");
            result.AppendLine($"WindowStyle: {WindowStyle}");
            result.AppendLine($"Size: {WindowRectangle.Size}");
            result.AppendLine($"HasParent: {HasParent}");
            result.AppendLine($"IsWin10App: {IsWin10App}");
            result.AppendLine($"Visible: {Visible}");
            result.AppendLine($"IsWindowVisible: {User32Api.IsWindowVisible(Handle)}");
            result.AppendLine($"IsCloaked: {IsCloaked}");
            result.AppendLine($"Iconic: {Iconic}");
            result.AppendLine($"IsBackgroundWin10App: {IsBackgroundWin10App}");
            if (HasChildren)
            {
                result.AppendLine($"Children classes: {string.Join(",", Children.Select(c => c.ClassName))}");
            }

            return result.ToString();
        }
    }
}