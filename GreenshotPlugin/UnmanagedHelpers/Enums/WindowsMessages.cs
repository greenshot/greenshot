using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    /// <summary>
    ///     All possible windows messages
    /// See also <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined">here</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowsMessages : uint
    {
        WM_NULL = 0x0000,
        /// <summary>
        /// Sent when an application requests that a window be created by calling the CreateWindowEx or CreateWindow function. (The message is sent before the function returns.) The window procedure of the new window receives this message after the window is created, but before the window becomes visible.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632619.aspx">WM_CREATE message</a>
        /// </summary>
        WM_CREATE = 0x0001,

        /// <summary>
        /// Sent when a window is being destroyed. It is sent to the window procedure of the window being destroyed after the window is removed from the screen.
        /// This message is sent first to the window being destroyed and then to the child windows (if any) as they are destroyed. During the processing of the message, it can be assumed that all child windows still exist.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632620(v=vs.85).aspx">WM_DESTROY message</a>
        /// </summary>
        WM_DESTROY = 0x0002,
        WM_MOVE = 0x0003,
        WM_SIZE = 0x0005,
        WM_ACTIVATE = 0x0006,
        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,

        /// <summary>
        /// Sent when an application changes the enabled state of a window. It is sent to the window whose enabled state is changing. This message is sent before the EnableWindow function returns, but after the enabled state (WS_DISABLED style bit) of the window has changed.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632621(v=vs.85).aspx">WM_ENABLE message</a>
        /// </summary>
        WM_ENABLE = 0x000A,
        WM_SETREDRAW = 0x000B,
        WM_SETTEXT = 0x000C,
        WM_GETTEXT = 0x000D,
        WM_GETTEXTLENGTH = 0x000E,

        /// <summary>
        /// The WM_PAINT message is sent when the system or another application makes a request to paint a portion of an application's window. The message is sent when the UpdateWindow or RedrawWindow function is called, or by the DispatchMessage function when the application obtains a WM_PAINT message by using the GetMessage or PeekMessage function.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145213(v=vs.85).aspx">WM_PAINT message</a>
        /// </summary>
        WM_PAINT = 0x000F,

        WM_CLOSE = 0x0010,
        WM_QUERYENDSESSION = 0x0011,
        WM_QUIT = 0x0012,
        WM_QUERYOPEN = 0x0013,
        WM_ERASEBKGND = 0x0014,
        WM_SYSCOLORCHANGE = 0x0015,
        WM_ENDSESSION = 0x0016,
        WM_SHOWWINDOW = 0x0018,

        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note: The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms725499(v=vs.85).aspx">M_WININICHANGE message</a>
        /// </summary>
        WM_WININICHANGE = 0x001A,

        /// <summary>
        /// A message that is sent to all top-level windows when the SystemParametersInfo function changes a system-wide setting or when policy settings have changed.
        /// Applications should send WM_SETTINGCHANGE to all top-level windows when they make changes to system parameters. (This message cannot be sent directly to a window.) To send the WM_SETTINGCHANGE message to all top-level windows, use the SendMessageTimeout function with the hWnd parameter set to HWND_BROADCAST.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms725497(v=vs.85).aspx">WM_SETTINGCHANGE message</a>
        /// </summary>
        WM_SETTINGCHANGE = WM_WININICHANGE,
        WM_DEVMODECHANGE = 0x001B,
        WM_ACTIVATEAPP = 0x001C,
        WM_FONTCHANGE = 0x001D,
        WM_TIMECHANGE = 0x001E,
        WM_CANCELMODE = 0x001F,
        WM_SETCURSOR = 0x0020,
        WM_MOUSEACTIVATE = 0x0021,
        WM_CHILDACTIVATE = 0x0022,
        WM_QUEUESYNC = 0x0023,
        WM_GETMINMAXINFO = 0x0024,
        WM_PAINTICON = 0x0026,
        WM_ICONERASEBKGND = 0x0027,
        WM_NEXTDLGCTL = 0x0028,
        WM_SPOOLERSTATUS = 0x002A,
        WM_DRAWITEM = 0x002B,
        WM_MEASUREITEM = 0x002C,
        WM_DELETEITEM = 0x002D,
        WM_VKEYTOITEM = 0x002E,
        WM_CHARTOITEM = 0x002F,
        WM_SETFONT = 0x0030,
        WM_GETFONT = 0x0031,
        WM_SETHOTKEY = 0x0032,
        WM_GETHOTKEY = 0x0033,
        WM_QUERYDRAGICON = 0x0037,
        WM_COMPAREITEM = 0x0039,
        WM_GETOBJECT = 0x003D,
        WM_COMPACTING = 0x0041,
        WM_COMMNOTIFY = 0x0044,
        WM_WINDOWPOSCHANGING = 0x0046,
        WM_WINDOWPOSCHANGED = 0x0047,
        WM_POWER = 0x0048,
        WM_COPYDATA = 0x004A,
        WM_CANCELJOURNAL = 0x004B,
        WM_NOTIFY = 0x004E,
        WM_INPUTLANGCHANGEREQUEST = 0x0050,
        WM_INPUTLANGCHANGE = 0x0051,
        WM_TCARD = 0x0052,
        WM_HELP = 0x0053,
        WM_USERCHANGED = 0x0054,
        WM_NOTIFYFORMAT = 0x0055,

        /// <summary>
        /// Notifies a window that the user clicked the right mouse button (right-clicked) in the window.
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/menurc/wm-contextmenu">WM_CONTEXTMENU message</a>
        /// 
        /// In case of a notify icon: 
        /// If a user selects a notify icon's shortcut menu with the keyboard, the Shell now sends the associated application a WM_CONTEXTMENU message. Earlier versions send WM_RBUTTONDOWN and WM_RBUTTONUP messages.
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// </summary>
        WM_CONTEXTMENU = 0x007B,

        WM_STYLECHANGING = 0x007C,
        WM_STYLECHANGED = 0x007D,

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145210.aspx">WM_DISPLAYCHANGE message</a>
        /// </summary>
        WM_DISPLAYCHANGE = 0x007E,

        /// <summary>
        /// Sent to a window to retrieve a handle to the large or small icon associated with a window. The system displays the large icon in the ALT+TAB dialog, and the small icon in the window caption.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632625.aspx">WM_GETICON message</a>
        /// </summary>
        WM_GETICON = 0x007F,
        WM_SETICON = 0x0080,

        /// <summary>
        /// Sent prior to the WM_CREATE message when a window is first created.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632635.aspx">WM_NCCREATE message</a>
        /// </summary>
        WM_NCCREATE = 0x0081,

        WM_NCDESTROY = 0x0082,
        WM_NCCALCSIZE = 0x0083,
        WM_NCHITTEST = 0x0084,
        WM_NCPAINT = 0x0085,
        WM_NCACTIVATE = 0x0086,
        WM_GETDLGCODE = 0x0087,
        WM_SYNCPAINT = 0x0088,
        WM_SYNCTASK = 0x0089,
        WM_KLUDGEMINRECT = 0x008b,
        WM_LPKDRAWSWITCHWND = 0x008c,

        WM_UAHDESTROYWINDOW = 0x0090,
        WM_UAHDRAWMENU = 0x0091,
        WM_UAHDRAWMENUITEM = 0x0092,
        WM_UAHINITMENU = 0x0093,
        WM_UAHMEASUREMENUITEM = 0x0094,
        WM_UAHNCPAINTMENUPOPUP = 0x0095,
        WM_UAHUPDATE = 0x0096,

        /// <summary>
        /// Posted to a window when the cursor is moved within the nonclient area of the window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCMOUSEMOVE = 0x00A0,
        /// <summary>
        /// Posted when the user presses the left mouse button while the cursor is within the nonclient area of a window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCLBUTTONDOWN = 0x00A1,
        /// <summary>
        /// Posted when the user releases the left mouse button while the cursor is within the nonclient area of a window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        /// <summary>
        /// Posted when the user presses the right mouse button while the cursor is within the nonclient area of a window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCRBUTTONDOWN = 0x00A4,
        /// <summary>
        /// Posted when the user releases the right mouse button while the cursor is within the nonclient area of a window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        /// <summary>
        /// Posted when the user presses the middle mouse button while the cursor is within the nonclient area of a window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCMBUTTONDOWN = 0x00A7,
        /// <summary>
        /// Posted when the user releases the middle mouse button while the cursor is within the nonclient area of a window.
        /// This message is posted to the window that contains the cursor.
        /// If a window has captured the mouse, this message is not posted.
        /// </summary>
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9,
        WM_NCXBUTTONDOWN = 0x00AB,
        WM_NCXBUTTONUP = 0x00AC,
        WM_NCXBUTTONDBLCLK = 0x00AD,
        WM_NCUAHDRAWCAPTION = 0x00ae,
        WM_NCUAHDRAWFRAME = 0x00af,

        // Edit controls
        EM_GETSEL = 0x00b0,
        EM_SETSEL = 0x00b1,
        EM_GETRECT = 0x00b2,
        EM_SETRECT = 0x00b3,
        EM_SETRECTNP = 0x00b4,
        EM_SCROLL = 0x00b5,
        EM_LINESCROLL = 0x00b6,
        EM_SCROLLCARET = 0x00b7,
        EM_GETMODIFY = 0x00b8,
        EM_SETMODIFY = 0x00b9,
        EM_GETLINECOUNT = 0x00ba,
        EM_LINEINDEX = 0x00bb,
        EM_SETHANDLE = 0x00bc,
        EM_GETHANDLE = 0x00bd,
        EM_GETTHUMB = 0x00be,
        EM_LINELENGTH = 0x00c1,
        EM_REPLACESEL = 0x00c2,
        EM_SETFONT = 0x00c3,
        EM_GETLINE = 0x00c4,
        EM_LIMITTEXT = 0x00c5,
        EM_CANUNDO = 0x00c6,
        EM_UNDO = 0x00c7,
        EM_FMTLINES = 0x00c8,
        EM_LINEFROMCHAR = 0x00c9,
        EM_SETWORDBREAK = 0x00ca,
        EM_SETTABSTOPS = 0x00cb,
        EM_SETPASSWORDCHAR = 0x00cc,
        EM_EMPTYUNDOBUFFER = 0x00cd,
        EM_GETFIRSTVISIBLELINE = 0x00ce,
        EM_SETREADONLY = 0x00cf,
        EM_SETWORDBREAKPROC = 0x00d0,
        EM_GETWORDBREAKPROC = 0x00d1,
        EM_GETPASSWORDCHAR = 0x00d2,
        EM_SETMARGINS = 0x00d3,
        EM_GETMARGINS = 0x00d4,
        EM_GETLIMITTEXT = 0x00d5,
        EM_POSFROMCHAR = 0x00d6,
        EM_CHARFROMPOS = 0x00d7,
        EM_SETIMESTATUS = 0x00d8,
        EM_GETIMESTATUS = 0x00d9,
        EM_MSGMAX = 0x00da,

        /// <summary>
        /// Sent to the window that registered to receive raw input.
        /// A window receives this message through its WindowProc function.
        /// wParam - This parameter can be one of the following values:
        /// GIDC_ARRIVAL (1) : A new device has been added to the system.
        /// GIDC_REMOVAL (2) : A device has been removed from the system.
        /// lParam - The handle to the raw input device. Call GetRawInputDeviceInfo to get more information regarding the device.
        /// Return value
        /// If an application processes this message, it should return zero.
        /// </summary>
        WM_INPUT_DEVICE_CHANGE = 0x00FE,

        /// <summary>
        /// Sent to the window that is getting raw input.
        /// Raw input is available only when the application calls RegisterRawInputDevices with valid device specifications
        /// A window receives this message through its WindowProc function.
        /// wParam - The input code. This parameter can be one of the following values:
        /// RIM_INPUT (0) - Input occurred while the application was in the foreground. The application must call DefWindowProc so the system can perform cleanup.
        /// RIM_INPUTSINK (1) - Input occurred while the application was not in the foreground. The application must call DefWindowProc so the system can perform the cleanup.
        /// lParam - A handle to the RAWINPUT structure that contains the raw input from the device.
        /// Return value
        /// If an application processes this message, it should return zero.
        /// </summary>
        WM_INPUT = 0x00FF,

        WM_KEYFIRST = 0x0100,
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102,
        WM_DEADCHAR = 0x0103,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
        WM_SYSCHAR = 0x0106,
        WM_SYSDEADCHAR = 0x0107,
        WM_UNICHAR = 0x0109,
        WM_KEYLAST = 0x0109,

        WM_IME_STARTCOMPOSITION = 0x010D,
        WM_IME_ENDCOMPOSITION = 0x010E,
        WM_IME_COMPOSITION = 0x010F,
        WM_IME_KEYLAST = 0x010F,

        WM_INITDIALOG = 0x0110,
        WM_COMMAND = 0x0111,
        WM_SYSCOMMAND = 0x0112,
        WM_TIMER = 0x0113,
        WM_HSCROLL = 0x0114,
        WM_VSCROLL = 0x0115,
        WM_INITMENU = 0x0116,
        WM_INITMENUPOPUP = 0x0117,
        WM_MENUSELECT = 0x011F,
        WM_MENUCHAR = 0x0120,
        WM_ENTERIDLE = 0x0121,
        WM_MENURBUTTONUP = 0x0122,
        WM_MENUDRAG = 0x0123,
        WM_MENUGETOBJECT = 0x0124,
        WM_UNINITMENUPOPUP = 0x0125,
        WM_MENUCOMMAND = 0x0126,

        WM_CHANGEUISTATE = 0x0127,
        WM_UPDATEUISTATE = 0x0128,
        WM_QUERYUISTATE = 0x0129,

        WM_CTLCOLORMSGBOX = 0x0132,
        WM_CTLCOLOREDIT = 0x0133,
        WM_CTLCOLORLISTBOX = 0x0134,
        WM_CTLCOLORBTN = 0x0135,
        WM_CTLCOLORDLG = 0x0136,
        WM_CTLCOLORSCROLLBAR = 0x0137,
        WM_CTLCOLORSTATIC = 0x0138,

        // Combo Box
        CB_GETEDITSEL = 0x00140,
        CB_LIMITTEXT = 0x00141,
        CB_SETEDITSEL = 0x00142,
        CB_ADDSTRING = 0x00143,
        CB_DELETESTRING = 0x00144,
        CB_DIR = 0x00145,
        CB_GETCOUNT = 0x00146,
        CB_GETCURSEL = 0x00147,
        CB_GETLBTEXT = 0x00148,
        CB_GETLBTEXTLEN = 0x00149,
        CB_INSERTSTRING = 0x0014a,
        CB_RESETCONTENT = 0x0014b,
        CB_FINDSTRING = 0x0014c,
        CB_SELECTSTRING = 0x0014d,
        CB_SETCURSEL = 0x0014e,
        CB_SHOWDROPDOWN = 0x0014f,
        CB_GETITEMDATA = 0x00150,
        CB_SETITEMDATA = 0x00151,
        CB_GETDROPPEDCONTROLRECT = 0x00152,
        CB_SETITEMHEIGHT = 0x00153,
        CB_GETITEMHEIGHT = 0x00154,
        CB_SETEXTENDEDUI = 0x00155,
        CB_GETEXTENDEDUI = 0x00156,
        CB_GETDROPPEDSTATE = 0x00157,
        CB_FINDSTRINGEXACT = 0x00158,
        CB_SETLOCALE = 0x00159,
        CB_GETLOCALE = 0x0015a,
        CB_GETTOPINDEX = 0x0015b,
        CB_SETTOPINDEX = 0x0015c,
        CB_GETHORIZONTALEXTENT = 0x0015d,
        CB_SETHORIZONTALEXTENT = 0x0015e,
        CB_GETDROPPEDWIDTH = 0x0015f,
        CB_SETDROPPEDWIDTH = 0x00160,
        CB_INITSTORAGE = 0x00161,
        CB_MSGMAX_OLD = 0x00162,
        CB_MULTIPLEADDSTRING = 0x00163,
        CB_GETCOMBOBOXINFO = 0x00164,
        CB_MSGMAX = 0x00165,


        LB_ADDSTRING = 0x00180,
        LB_INSERTSTRING = 0x00181,
        LB_DELETESTRING = 0x00182,
        LB_SELITEMRANGEEX = 0x00183,
        LB_RESETCONTENT = 0x00184,
        LB_SETSEL = 0x00185,
        LB_SETCURSEL = 0x00186,
        LB_GETSEL = 0x00187,
        LB_GETCURSEL = 0x00188,
        LB_GETTEXT = 0x00189,
        LB_GETTEXTLEN = 0x0018a,
        LB_GETCOUNT = 0x0018b,
        LB_SELECTSTRING = 0x0018c,
        LB_DIR = 0x0018d,
        LB_GETTOPINDEX = 0x0018e,
        LB_FINDSTRING = 0x0018f,
        LB_GETSELCOUNT = 0x00190,
        LB_GETSELITEMS = 0x00191,
        LB_SETTABSTOPS = 0x00192,
        LB_GETHORIZONTALEXTENT = 0x00193,
        LB_SETHORIZONTALEXTENT = 0x00194,
        LB_SETCOLUMNWIDTH = 0x00195,
        LB_ADDFILE = 0x00196,
        LB_SETTOPINDEX = 0x00197,
        LB_GETITEMRECT = 0x00198,
        LB_GETITEMDATA = 0x00199,
        LB_SETITEMDATA = 0x0019a,
        LB_SELITEMRANGE = 0x0019b,
        LB_SETANCHORINDEX = 0x0019c,
        LB_GETANCHORINDEX = 0x0019d,
        LB_SETCARETINDEX = 0x0019e,
        LB_GETCARETINDEX = 0x0019f,
        LB_SETITEMHEIGHT = 0x001a0,
        LB_GETITEMHEIGHT = 0x001a1,
        LB_FINDSTRINGEXACT = 0x001a2,
        LBCB_CARETON = 0x001a3,
        LBCB_CARETOFF = 0x001a4,
        LB_SETLOCALE = 0x001a5,
        LB_GETLOCALE = 0x001a6,
        LB_SETCOUNT = 0x001a7,
        LB_INITSTORAGE = 0x001a8,
        LB_ITEMFROMPOINT = 0x001a9,
        LB_INSERTSTRINGUPPER = 0x001aa,
        LB_INSERTSTRINGLOWER = 0x001ab,
        LB_ADDSTRINGUPPER = 0x001ac,
        LB_ADDSTRINGLOWER = 0x001ad,
        LBCB_STARTTRACK = 0x001ae,
        LBCB_ENDTRACK = 0x001af,
        LB_MSGMAX_OLD = 0x001b0,
        LB_MULTIPLEADDSTRING = 0x001b1,
        LB_GETLISTBOXINFO = 0x001b2,
        LB_MSGMAX = 0x001b3,

        MN_FIRST = 0x01e0,
        WM_GETHMENU = 0x01E1,

        /// <summary>
        /// Defines the start of the mouse related messages
        /// </summary>
        WM_MOUSEFIRST = 0x0200,

        /// <summary>
        /// Posted to a window when the cursor moves.
        /// If the mouse is not captured, the message is posted to the window that contains the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousemove">WM_MOUSEMOVE message</a>
        /// </summary>
        WM_MOUSEMOVE = 0x0200,

        /// <summary>
        /// Posted when the user presses the left mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        /// 
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-lbuttondown">WM_LBUTTONDOWN message</a>
        /// </summary>
        WM_LBUTTONDOWN = 0x0201,

        /// <summary>
        /// Posted when the user releases the left mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-lbuttonup">WM_LBUTTONUP message</a>
        /// </summary>
        WM_LBUTTONUP = 0x0202,

        /// <summary>
        /// Posted when the user double-clicks the left mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-lbuttondblclk">WM_LBUTTONDBLCLK message</a>
        /// </summary>
        WM_LBUTTONDBLCLK = 0x0203,

        /// <summary>
        /// Posted when the user presses the right mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-rbuttondown">WM_RBUTTONDOWN message</a>
        /// </summary>
        WM_RBUTTONDOWN = 0x0204,

        /// <summary>
        /// Posted when the user releases the right mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-rbuttonup">WM_RBUTTONUP message</a>
        /// </summary>
        WM_RBUTTONUP = 0x0205,

        /// <summary>
        /// Posted when the user double-clicks the right mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-rbuttondblclk">WM_RBUTTONDBLCLK message</a>
        /// </summary>
        WM_RBUTTONDBLCLK = 0x0206,

        /// <summary>
        /// Posted when the user presses the middle mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mbuttondown">WM_MBUTTONDOWN message</a>
        /// </summary>
        WM_MBUTTONDOWN = 0x0207,

        /// <summary>
        /// Posted when the user releases the middle mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mbuttonup">WM_MBUTTONUP message</a>
        /// </summary>
        WM_MBUTTONUP = 0x0208,

        /// <summary>
        /// Posted when the user double-clicks the middle mouse button while the cursor is in the client area of a window.
        /// If the mouse is not captured, the message is posted to the window beneath the cursor.
        /// Otherwise, the message is posted to the window that has captured the mouse.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mbuttondblclk">WM_MBUTTONDBLCLK message</a>
        /// </summary>
        WM_MBUTTONDBLCLK = 0x0209,

        WM_MOUSEWHEEL = 0x020A,
        WM_XBUTTONDOWN = 0x020B,
        WM_XBUTTONUP = 0x020C,
        WM_XBUTTONDBLCLK = 0x020D,
        WM_MOUSEHWHEEL = 0x020E,

        WM_PARENTNOTIFY = 0x0210,
        WM_ENTERMENULOOP = 0x0211,
        WM_EXITMENULOOP = 0x0212,

        WM_NEXTMENU = 0x0213,
        WM_SIZING = 0x0214,
        WM_CAPTURECHANGED = 0x0215,
        WM_MOVING = 0x0216,

        WM_POWERBROADCAST = 0x0218,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/devio/wm-devicechange">WM_DEVICECHANGE message</a>
        /// Notifies an application of a change to the hardware configuration of a device or the computer.
        /// wParam - The event that has occurred (DeviceChangeEvent)
        /// lParam - A pointer to a structure that contains event-specific data. Its format depends on the value of the wParam parameter. For more information, refer to the documentation for each event
        /// 
        /// Return TRUE to grant the request, return BROADCAST_QUERY_DENY to deny the request.
        /// </summary>
        WM_DEVICECHANGE = 0x0219,

        WM_MDICREATE = 0x0220,
        WM_MDIDESTROY = 0x0221,
        WM_MDIACTIVATE = 0x0222,
        WM_MDIRESTORE = 0x0223,
        WM_MDINEXT = 0x0224,
        WM_MDIMAXIMIZE = 0x0225,
        WM_MDITILE = 0x0226,
        WM_MDICASCADE = 0x0227,
        WM_MDIICONARRANGE = 0x0228,
        WM_MDIGETACTIVE = 0x0229,


        WM_MDISETMENU = 0x0230,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        WM_DROPFILES = 0x0233,
        WM_MDIREFRESHMENU = 0x0234,

        WM_IME_REPORT = 0x0280,
        WM_IME_SETCONTEXT = 0x0281,
        WM_IME_NOTIFY = 0x0282,
        WM_IME_CONTROL = 0x0283,
        WM_IME_COMPOSITIONFULL = 0x0284,
        WM_IME_SELECT = 0x0285,
        WM_IME_CHAR = 0x0286,
        WM_IME_REQUEST = 0x0288,
        WM_IME_KEYDOWN = 0x0290,
        WM_IME_KEYUP = 0x0291,

        WM_NCMOUSEHOVER = 0x02A0,
        WM_MOUSEHOVER = 0x02A1,
        WM_NCMOUSELEAVE = 0x02A2,
        WM_MOUSELEAVE = 0x02A3,

        WM_WTSSESSION_CHANGE = 0x02B1,

        WM_TABLET_FIRST = 0x02C0,
        WM_POINTERDEVICEADDED = 0x02c8,
        WM_POINTERDEVICEDELETED = 0x02c9,
        WM_FLICK = 0x02cb,
        WM_FLICKINTERNAL = 0x02cd,
        WM_BRIGHTNESSCHANGED = 0x02ce,
        WM_TABLET_LAST = 0x02DF,

        /// <summary>
        /// Sent when the effective dots per inch (dpi) for a window has changed. The DPI is the scale factor for a window. There are multiple events that can cause the DPI to change. The following list indicates the possible causes for the change in DPI.
        /// * The window is moved to a new monitor that has a different DPI.
        /// * The DPI of the monitor hosting the window changes.
        /// The current DPI for a window always equals the last DPI sent by WM_DPICHANGED.
        /// This is the scale factor that the window should be scaling to for threads that are aware of DPI changes.
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083.aspx">WM_DPICHANGED message</a>
        /// </summary>
        WM_DPICHANGED = 0x02E0,

        /// <summary>
        /// For Per Monitor v2 top-level windows, this message is sent to all HWNDs in the child HWDN tree of the window that is undergoing a DPI change.
        /// This message occurs before the top-level window receives WM_DPICHANGED, and traverses the child tree from the bottom up.
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt807678.aspx">WM_DPICHANGED_BEFOREPARENT message</a>
        /// </summary>
        WM_DPICHANGED_BEFOREPARENT = 0x02E2,

        /// <summary>
        ///     For Per Monitor v2 top-level windows, this message is sent to all HWNDs in the child HWDN tree of the window that is undergoing a DPI change.
        ///     This message occurs after the top-level window receives WM_DPICHANGED, and traverses the child tree from the top down.
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt807677.aspx">WM_DPICHANGED_AFTERPARENT message</a>
        /// </summary>
        WM_DPICHANGED_AFTERPARENT = 0x02E3,

        /// <summary>
        ///     This message tells the operating system that the window will be sized to dimensions other than the default.
        ///     This message is sent to top-level windows with a DPI_AWARENESS_CONTEXT of Per Monitor v2 before a WM_DPICHANGED message is sent, and allows the window to compute its desired size for the pending DPI change.
        ///     As linear DPI scaling is the default behavior, this is only useful in scenarios where the window wants to scale non-linearly.
        ///     If the application responds to this message, the resulting size will be the candidate rectangle send to WM_DPICHANGED.
        ///     Use this message to alter the size of the rect that is provided with WM_DPICHANGED.
        ///
        ///     wParam
        ///     The WPARAM contains a DPI value. The scaled window size that the application would set needs to be computed as if the window were to switch to this DPI.
        /// 
        ///     lParam
        ///     The LPARAM is an in/out pointer to a SIZE struct. The _In_ value in the LPARAM is the pending size of the window after a user-initiated move or a call to SetWindowPos. If the window is being resized, this size is not necessarily the same as the window's current size at the time this message is received.
        /// 
        ///     The _Out_ value in the LPARAM should be written to by the application to specify the desired scaled window size corresponding to the provided DPI value in the WPARAM.
        ///
        ///     The function returns a BOOL. Returning TRUE indicates that a new size has been computed. Returning FALSE indicates that the message will not be handled, and the default linear DPI scaling will apply to the window.
        ///
        ///     Remarks:
        ///     This message is only sent to top-level windows which have a DPI awareness context of Per Monitor v2.
        ///     This event is necessary to facilitate graceful non-linear scaling, and ensures that the windows's position remains constant in relationship to the cursor and when moving back and forth across monitors.
        ///      There is no specific default handling of this message in DefWindowProc. As for all messages it does not explicitly handle, DefWindowProc will return zero for this message. As noted above, this return tells the system to use the default linear behavior.
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt807679.aspx">WM_GETDPISCALEDSIZE message</a>
        /// </summary>
        WM_GETDPISCALEDSIZE = 0x02E4,

        /// <summary>
        /// An application sends a WM_CUT message to an edit control or combo box to delete (cut) the current selection, if any, in the edit control and copy the deleted text to the clipboard in CF_TEXT format.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649023.aspx">WM_CUT message</a>
        /// </summary>
        WM_CUT = 0x0300,

        /// <summary>
        /// An application sends the WM_COPY message to an edit control or combo box to copy the current selection to the clipboard in CF_TEXT format.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649022.aspx">WM_COPY message</a>
        /// </summary>
        WM_COPY = 0x0301,

        /// <summary>
        /// An application sends a WM_PASTE message to an edit control or combo box to copy the current content of the clipboard to the edit control at the current caret position. Data is inserted only if the clipboard contains data in CF_TEXT format.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649028.aspx">WM_PASTE message</a>
        /// </summary>
        WM_PASTE = 0x0302,

        /// <summary>
        /// An application sends a WM_CLEAR message to an edit control or combo box to delete (clear) the current selection, if any, from the edit control.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649020.aspx">WM_CLEAR message</a>
        /// </summary>
        WM_CLEAR = 0x0303,

        /// <summary>
        /// An application sends a WM_UNDO message to an edit control to undo the last operation. When this message is sent to an edit control, the previously deleted text is restored or the previously added text is deleted.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb761693.aspx">WM_UNDO message</a>
        /// </summary>
        WM_UNDO = 0x0304,

        /// <summary>
        /// Sent to the clipboard owner if it has delayed rendering a specific clipboard format and if an application has requested data in that format. The clipboard owner must render data in the specified format and place it on the clipboard by calling the SetClipboardData function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649030.aspx">WM_RENDERFORMAT message</a>
        /// </summary>
        WM_RENDERFORMAT = 0x0305,

        /// <summary>
        /// Sent to the clipboard owner before it is destroyed, if the clipboard owner has delayed rendering one or more clipboard formats. For the content of the clipboard to remain available to other applications, the clipboard owner must render data in all the formats it is capable of generating, and place the data on the clipboard by calling the SetClipboardData function.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649029.aspx">WM_RENDERALLFORMATS message</a>
        /// </summary>
        WM_RENDERALLFORMATS = 0x0306,

        /// <summary>
        /// Sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard.
        ///A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649024.aspx">WM_DESTROYCLIPBOARD message</a>
        /// </summary>
        WM_DESTROYCLIPBOARD = 0x0307,

        /// <summary>
        /// Sent to the first window in the clipboard viewer chain when the content of the clipboard changes. This enables a clipboard viewer window to display the new content of the clipboard.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649025.aspx">WM_DRAWCLIPBOARD message</a>
        /// </summary>
        WM_DRAWCLIPBOARD = 0x0308,

        /// <summary>
        /// Sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area needs repainting.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649027.aspx">WM_PAINTCLIPBOARD message</a>
        /// </summary>
        WM_PAINTCLIPBOARD = 0x0309,

        /// <summary>
        /// Sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's vertical scroll bar. The owner should scroll the clipboard image and update the scroll bar values.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649032.aspx">WM_VSCROLLCLIPBOARD message</a>
        /// </summary>
        WM_VSCROLLCLIPBOARD = 0x030A,

        /// <summary>
        /// Sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area has changed size.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649031.aspx">WM_SIZECLIPBOARD message</a>
        /// </summary>
        WM_SIZECLIPBOARD = 0x030B,

        /// <summary>
        /// Sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.
        /// A window receives this message through its WindowProc function.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649018.aspx">WM_ASKCBFORMATNAME message</a>
        /// </summary>
        WM_ASKCBFORMATNAME = 0x030C,

        /// <summary>
        /// Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.
        /// A window receives this message through its WindowProc function.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649019.aspx">WM_CHANGECBCHAIN message</a>
        /// </summary>
        WM_CHANGECBCHAIN = 0x030D,

        /// <summary>
        /// Sent to the clipboard owner by a clipboard viewer window. This occurs when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's horizontal scroll bar. The owner should scroll the clipboard image and update the scroll bar values.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649026.aspx">WM_HSCROLLCLIPBOARD message</a>
        /// </summary>
        WM_HSCROLLCLIPBOARD = 0x030E,

        WM_QUERYNEWPALETTE = 0x030F,
        WM_PALETTEISCHANGING = 0x0310,


        WM_PALETTECHANGED = 0x0311,
        WM_HOTKEY = 0x0312,

        WM_SYSMENU = 0x313,
        WM_HOOKMSG = 0x314,
        WM_EXITPROCESS = 0x315,
        WM_WAKETHREAD = 0x316,

        /// <summary>
        /// The WM_PRINT message is sent to a window to request that it draw itself in the specified device context, most commonly in a printer device context.
        ///A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145216.aspx">WM_PRINT message</a>
        /// </summary>
        WM_PRINT = 0x0317,

        /// <summary>
        /// The WM_PRINTCLIENT message is sent to a window to request that it draw its client area in the specified device context, most commonly in a printer device context.
        /// Unlike WM_PRINT, WM_PRINTCLIENT is not processed by DefWindowProc. A window should process the WM_PRINTCLIENT message through an application-defined WindowProc function for it to be used properly.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145217.aspx">WM_PRINTCLIENT message</a>
        /// </summary>
        WM_PRINTCLIENT = 0x0318,

        /// <summary>
        /// Notifies a window that the user generated an application command event, for example, by clicking an application command button using the mouse or typing an application command key on the keyboard.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646275.aspx">WM_APPCOMMAND message</a>
        /// </summary>
        WM_APPCOMMAND = 0x0319,

        WM_THEMECHANGED = 0x031A,
        WM_UAHINIT = 0x031b,
        WM_DESKTOPNOTIFY = 0x031c,

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649021.aspx">WM_CLIPBOARDUPDATE message</a>
        /// </summary>
        WM_CLIPBOARDUPDATE = 0x031D,

        WM_DWMCOMPOSITIONCHANGED = 0x031E,
        WM_DWMNCRENDERINGCHANGED = 0x031F,
        WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0321,

        WM_DWMEXILEFRAME = 0x0322,
        WM_DWMSENDICONICTHUMBNAIL = 0x0323,
        WM_MAGNIFICATION_STARTED = 0x0324,
        WM_MAGNIFICATION_ENDED = 0x0325,
        WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
        WM_DWMTHUMBNAILSIZECHANGED = 0x0327,
        WM_MAGNIFICATION_OUTPUT = 0x0328,
        WM_BSDRDATA = 0x0329,
        WM_DWMTRANSITIONSTATECHANGED = 0x032a,
        WM_KEYBOARDCORRECTIONCALLOUT = 0x032c,
        WM_KEYBOARDCORRECTIONACTION = 0x032d,
        WM_UIACTION = 0x032e,
        WM_ROUTED_UI_EVENT = 0x032f,
        WM_MEASURECONTROL = 0x0330,
        WM_GETACTIONTEXT = 0x0331,
        WM_FORWARDKEYDOWN = 0x0333,
        WM_FORWARDKEYUP = 0x0334,


        WM_GETTITLEBARINFOEX = 0x033F,
        WM_NOTIFYWOW = 0x0340,
        WM_HANDHELDFIRST = 0x0358,
        WM_HANDHELDLAST = 0x035F,

        WM_AFXFIRST = 0x0360,
        WM_AFXLAST = 0x037F,

        WM_PENWINFIRST = 0x0380,
        WM_PENWINLAST = 0x038F,

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd757352.aspx">MM_JOY1MOVE message</a>
        /// </summary>
        MM_JOY1MOVE = 0x03A0,
        MM_JOY2MOVE = 0x03A1,
        MM_JOY1ZMOVE = 0x03A2,
        MM_JOY2ZMOVE = 0x03A3,
        MM_JOY1BUTTONDOWN = 0x03B5,
        MM_JOY2BUTTONDOWN = 0x03B6,
        MM_JOY1BUTTONUP = 0x03B7,
        MM_JOY2BUTTONUP = 0x03B8,
        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd757358.aspx">MM_MCINOTIFY message</a>
        /// </summary>
        MM_MCINOTIFY = 0x3B9,
        MM_WOM_OPEN = 0x03BB,
        MM_WOM_CLOSE = 0x03BC,
        MM_WOM_DONE = 0x03BD,
        MM_WIM_OPEN = 0x03BE,
        MM_WIM_CLOSE = 0x03BF,
        MM_WIM_DATA = 0x03C0,
        MM_MIM_OPEN = 0x03C1,
        MM_MIM_CLOSE = 0x03C2,
        MM_MIM_DATA = 0x03C3,
        MM_MIM_LONGDATA = 0x03C4,
        MM_MIM_ERROR = 0x03C5,
        MM_MIM_LONGERROR = 0x03C6,
        MM_MOM_OPEN = 0x03C7,
        MM_MOM_CLOSE = 0x03C8,
        MM_MOM_DONE = 0x03C9,
        MM_DRVM_OPEN = 0x03D0,
        MM_DRVM_CLOSE = 0x03D1,
        MM_DRVM_DATA = 0x03D2,
        MM_DRVM_ERROR = 0x03D3,
        MM_STREAM_OPEN = 0x3D4,
        MM_STREAM_CLOSE = 0x3D5,
        MM_STREAM_DONE = 0x3D6,
        MM_STREAM_ERROR = 0x3D7,
        MM_MOM_POSITIONCB = 0x03CA,
        MM_MCISIGNAL = 0x03CB,
        MM_MIM_MOREDATA = 0x03CC,
        MM_MIXM_LINE_CHANGE = 0x03D0,
        MM_MIXM_CONTROL_CHANGE = 0x03D1,

        /// <summary>
        /// Used to define private messages for use by private window classes, usually of the form WM_USER+x, where x is an integer value.
        /// </summary>
        WM_USER = 0x0400,

        /// <summary>
        /// This message is only send when using NOTIFYICON_VERSION_4, the Shell now sends the associated application an NIN_SELECT notification.
        /// Send when a notify icon is activated with mouse or ENTER key.
        /// Earlier versions send WM_RBUTTONDOWN and WM_RBUTTONUP messages.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// </summary>
        NIN_SELECT = WM_USER,

        /// <summary>
        /// This message is only send when using NOTIFYICON_VERSION_4, the Shell now sends the associated application an NIN_SELECT notification.
        /// Send when a notify icon is activated with SPACEBAR or ENTER key.
        /// Earlier versions send WM_RBUTTONDOWN and WM_RBUTTONUP messages.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// </summary>
        NIN_KEYSELECT = WM_USER + 1,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// 
        /// Sent when the balloon is shown (balloons are queued).
        /// </summary>
        NIN_BALLOONSHOW = WM_USER + 2,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// 
        /// Sent when the balloon disappears. For example, when the icon is deleted.
        /// This message is not sent if the balloon is dismissed because of a timeout or if the user clicks the mouse.
        ///
        /// As of Windows 7, NIN_BALLOONHIDE is also sent when a notification with the NIIF_RESPECT_QUIET_TIME flag set attempts to display during quiet time (a user's first hour on a new computer).
        /// In that case, the balloon is never displayed at all.
        /// </summary>
        NIN_BALLOONHIDE = WM_USER + 3,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// 
        /// Sent when the balloon is dismissed because of a timeout.
        /// </summary>
        NIN_BALLOONTIMEOUT = WM_USER + 4,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// 
        /// Sent when the balloon is dismissed because the user clicked the mouse.
        /// </summary>
        NIN_BALLOONUSERCLICK = WM_USER + 5,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// 
        /// Sent when the user hovers the cursor over an icon to indicate that the richer pop-up UI should be used in place of a standard textual tooltip.
        /// </summary>
        NIN_POPUPOPEN = WM_USER + 6,

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw">Shell_NotifyIcon function</a>
        /// 
        /// Sent when a cursor no longer hovers over an icon to indicate that the rich pop-up UI should be closed.
        /// </summary>
        NIN_POPUPCLOSE = WM_USER + 7,

        /// <summary>
        /// The WM_APP constant is used to distinguish between message values that are reserved for use by the system and values that can be used by an application to send messages within a private window class. 
        /// </summary>
        WM_APP = 0x8000,

        OCM__BASE = WM_USER + 0x1C00,
        WM_REFLECT = WM_USER + 0x1C00,
        /// <summary>
        /// From this value to 
        /// </summary>
        WM_APPLICATION_STRING = 0xc000,
        WM_RASDIALEVENT = 0xCCCD
    }
}