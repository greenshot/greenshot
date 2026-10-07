using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base.Core.Enums;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interop;
using log4net;
using System.Threading.Tasks;

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
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private static readonly IList<IntPtr> IgnoreHandles = new List<IntPtr>();

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
                    var cachedIcon = PluginUtils.GetCachedExeIcon(ProcessPath, 0);
                    // Clone the cached icon to prevent issues when the cache is cleared on icon size change
                    return cachedIcon != null ? ImageHelper.Clone(cachedIcon) : null;
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
            if (Conf.IconSize.Width >= 32 || Conf.IconSize.Height >= 32)
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
        /// Use this to make remove internal windows, like the message windows and the capture window, invisible
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
        private bool _textRetrieved;

        /// <summary>
        /// Gets the window's title (caption)
        /// </summary>
        public string Text
        {
            set
            {
                _text = value;
                _textRetrieved = true;
            }
            get
            {
                if (!_textRetrieved)
                {
                    _textRetrieved = true;
                    _text = User32Api.GetText(Handle);

                    // Fallback for sandboxed applications where GetWindowText fails
                    // due to cross-process security restrictions
                    if (string.IsNullOrEmpty(_text))
                    {
                        _text = WindowTitleHelper.GetWindowTitle(Handle);
                    }
                }

                return _text ?? string.Empty;
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
        public async Task RestoreAsync(CancellationToken cancellationToken = default)
        {
            if (Iconic)
            {
                User32Api.SendMessage(Handle, WindowsMessages.WM_SYSCOMMAND, SysCommands.SC_RESTORE, IntPtr.Zero);
            }

            User32Api.BringWindowToTop(Handle);
            User32Api.SetForegroundWindow(Handle);
            // Wait for the window to restore, polling with a timeout (~2 seconds max, 100 * 20ms)
            const int maxWaitAttempts = 100;
            for (int waitAttempts = 0; Iconic && waitAttempts < maxWaitAttempts; waitAttempts++)
            {
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
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
                User32Api.AttachThreadInput(threadId1, threadId2, true);
                User32Api.SetForegroundWindow(hWnd);
                User32Api.AttachThreadInput(threadId1, threadId2, false);
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
            // Visible already checks IsCloaked, so this serves as a fail-fast for cloaked windows
            // and also verifies the window is actually visible (or iconic/minimized)
            if (!(window.Visible || window.Iconic))
            {
                return false;
            }

            // Windows without size
            if (window.WindowRectangle.Size.Width * window.WindowRectangle.Size.Height == 0)
            {
                return false;
            }

            // Child windows cannot be top level
            if (window.HasParent)
            {
                return false;
            }

            // Tool windows are mostly overlays
            if ((window.ExtendedWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            // If the title is empty, it's rarely a window the user interacts with
            if (window.Text.Length == 0)
            {
                return false;
            }

            if (IgnoreClasses.Contains(window.ClassName))
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