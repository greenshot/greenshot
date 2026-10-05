# Prompt: improvements for hardcodet/wpf-notifyicon

A prompt for a coding session on https://github.com/hardcodet/wpf-notifyicon. It lists the changes Greenshot would like
upstream, so Greenshot can drop its workarounds. Each item says why, what Greenshot does today and what is expected.

---

You are working on the open source library **wpf-notifyicon** (`Hardcodet.NotifyIcon.Wpf`, repository
https://github.com/hardcodet/wpf-notifyicon, branch `develop`). The library is used by Greenshot (a screenshot tool,
https://github.com/greenshot/greenshot) for its notification area icon. Greenshot is a per-monitor-v2 DPI aware
.NET Framework 4.8 WPF application, often used on setups with several monitors at different scaling (e.g. 100 % and 150 %).

Prepare the changes below as **separate, small pull requests** (one topic each), in the style of the existing code.
Keep the public API backwards compatible: existing applications must behave the same unless they opt in, except where
an item says the current behaviour is a bug. Add or extend a page of the sample application (`NotifyIconWpf.Sample.*`)
for each change, so a maintainer can see it, and add unit tests where the logic can be tested without a taskbar.
Before starting, check the open issues and pull requests for each topic and link them; if a topic is already being
handled, contribute there instead of opening a duplicate. Don't change the target frameworks or the package metadata.

## 1. Per-monitor DPI for the context menu and the popups (bug)

**Today:** `Interop/SystemInfo.cs` computes one `DpiFactorX/Y` for the whole system (from a temporary `HwndSource`),
`ScaleWithDpi` divides the physical cursor position by it, and `TaskbarIcon.ShowContextMenu` places the `ContextMenu`
at `PlacementMode.AbsolutePoint` with that result. `WM_DPICHANGED` only refreshes the single global factor. The same
factor is used for `ShowTrayPopup` / `GetPopupTrayPosition` and the custom balloons.

**Problem:** in a per-monitor DPI aware application, when the taskbar (or the cursor) is on a monitor whose scaling
differs from the system DPI, the menu and the popups open at the wrong position (offset, or on the wrong monitor)
and in the wrong size.

**Greenshot's workaround:** it handles `PreviewTrayContextMenuOpen` (sets `Handled`), and opens its own `ContextMenu`
with `PlacementMode.MousePoint`, which lets WPF place and scale it for the monitor under the cursor.

**Expected:** the context menu, the tray popup and the custom balloons open at the right position and scale on every
monitor, for per-monitor-v2, per-monitor, system aware and DPI unaware applications. Use the DPI of the monitor at
the target point (`MonitorFromPoint` + `GetDpiForMonitor`, with a fallback for Windows versions without it) instead of
the system factor, or let WPF place the element (`MousePoint` for mouse activation, a `PlacementRectangle` around the
icon for keyboard activation). Verify on a mixed setup: taskbar on a 150 % monitor next to a 100 % primary monitor,
taskbar on the secondary monitor, and taskbar at the top, left and right.

## 2. Keyboard selection of the icon

**Today:** `WindowMessageSink.ProcessWindowMessage` maps `NIN_KEYSELECT` (Enter or Space on the focused icon, e.g.
after Win+B) to `ContextMenuReceived`, the same as `WM_CONTEXTMENU` (Shift+F10, the menu key), so Enter opens the
context menu. The context menu opened by the keyboard is placed like a mouse menu.

**Expected (opt-in, current behaviour stays the default):**
- A way to give `NIN_KEYSELECT` the primary action, like a left click (`NIN_SELECT`): e.g. a property
  `KeySelectAction` (`ContextMenu` = today, `LeftClick`) or a separate `TrayKeySelect` routed event plus command.
  This follows the Windows guideline that Enter/Space does the default action and the menu key opens the menu.
- A context menu which is opened by the keyboard is placed at the icon (the anchor coordinates of `NIN_KEYSELECT` /
  `WM_CONTEXTMENU` in version 4 are the icon's position, not the mouse), and its first item gets the keyboard focus.

## 3. No left click delay when nothing listens for a double click

**Today:** a left click waits the system double click time (`GetDoubleClickTime`, often 500 ms) before
`LeftClickCommand` runs and the popup opens, unless `NoLeftClickDelay` is set, in which case a double click also raises
the single click.

**Greenshot's workaround:** `NoLeftClickDelay = true` plus its own `DispatcherTimer` on `TrayLeftMouseUp`: when the
user configured no double click action the single click runs at once, otherwise it waits the double click time.

**Expected (opt-in):** a mode where the delay only happens when a double click can do something: a
`DoubleClickCommand` is set, `TrayMouseDoubleClick` has subscribers, or `MenuActivation` / `PopupActivation` include
`DoubleClick`. E.g. `LeftClickDelayMode` with `Always` (today), `Never` (= `NoLeftClickDelay`), `WhenDoubleClickIsUsed`.
`NoLeftClickDelay` keeps working and maps to `Never`.

## 4. Safe tooltip text

**Today:** `ToolTipText` is copied into `NOTIFYICONDATA.szTip` (128 characters including the terminating null);
a longer text is cut silently by the marshaller, which can split a surrogate pair (an emoji) and leaves no hint that
the text was cut.

**Greenshot's workaround:** `NotifyIconTextHelper` collapses whitespace and shortens the text at a word boundary with
an ellipsis (to 63 characters, the old WinForms `NotifyIcon` limit).

**Expected:** when the text is longer than 127 characters, the library shortens it without splitting a surrogate pair
(and without leaving a lone high surrogate), ending with "…", and the limit is documented on the property. Unit tests for
the shortening.

## 5. The right icon size for the DPI of the taskbar

**Today:** the handle of `Icon` (or the icon created from `IconSource`) is passed to `Shell_NotifyIcon` as it is,
so the shell scales whatever size the application happened to load; on a 150 % or 200 % taskbar a 16 px icon is
blurry, a 32 px icon scaled down to 24 px can be soft too.

**Expected:** when the icon has several sizes (a multi-size `.ico` from `IconSource`, or an `Icon` loaded from one),
the library picks the size which matches the small icon size for the DPI of the taskbar monitor
(`GetSystemMetricsForDpi(SM_CXSMICON, dpi)`, falling back to `SM_CXSMICON`), and picks again when the DPI changes
(`WM_DPICHANGED` of the message window, or a taskbar restart). An `Icon` with a single size is used as it is.
Verify that the icon stays sharp after moving the taskbar to a monitor with a different scaling.

## What Greenshot will do after these are released

- Remove the `PreviewTrayContextMenuOpen` bypass and use the library's context menu placement (item 1), keeping its
  own menu building in the preview event.
- Use `KeySelectAction = LeftClick` (item 2).
- Remove the own double click timer (item 3) and `NotifyIconTextHelper` (item 4).
- Pass the multi-size Greenshot icon and let the library pick the size (item 5).

The Greenshot code which uses the library is `src/Greenshot/Shell/TrayIcon.cs` and
`src/Greenshot/Helpers/NotifyIconTextHelper.cs` in https://github.com/greenshot/greenshot.
