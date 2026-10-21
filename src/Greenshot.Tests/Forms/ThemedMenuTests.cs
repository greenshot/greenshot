/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Interop;
using System.Windows.Threading;
using Greenshot.Base.Wpf;
using Xunit;

namespace Greenshot.Tests.Forms
{
    [Collection(TestCollections.WpfThemeState)]
    public class ThemedMenuTests
    {
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hwnd);

        [Fact]
        public void MenuAction_RunsWhenTheMenuWindowIsGone()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                var menu = ThemedMenu.CreateContextMenu();
                try
                {
                    IntPtr menuWindow = IntPtr.Zero;
                    bool? menuWindowExistedInAction = null;
                    // Like "Save as" in the destination picker: the action shows a dialog, which must not belong to the menu
                    var item = ThemedMenu.CreateItem("Save as", null, () => menuWindowExistedInAction = IsWindow(menuWindow));
                    menu.Items.Add(item);
                    ThemedMenu.ShowAtCursor(menu);
                    // Waits for the menu window, not a fixed time: with the other tests running in parallel it can take longer
                    PumpUntil(() => (PresentationSource.FromVisual(menu) as HwndSource)?.Handle is { } handle && handle != IntPtr.Zero, TimeSpan.FromSeconds(10));
                    menuWindow = (PresentationSource.FromVisual(menu) as HwndSource)?.Handle ?? IntPtr.Zero;
                    Assert.NotEqual(IntPtr.Zero, menuWindow);

                    // Clicked like the user does: the menu closes (and fades out), then the action runs
                    ((IInvokeProvider)new MenuItemAutomationPeer(item).GetPattern(PatternInterface.Invoke)).Invoke();
                    PumpUntil(() => menuWindowExistedInAction.HasValue, TimeSpan.FromSeconds(10));

                    Assert.False(menuWindowExistedInAction ?? true, "The action ran while the menu window still existed, or not at all");
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
                finally
                {
                    menu.IsOpen = false;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        /// <summary>
        /// Lets the dispatcher work until the condition is met, or the time is over
        /// </summary>
        private static void PumpUntil(Func<bool> condition, TimeSpan timeout)
        {
            var end = DateTime.Now + timeout;
            while (DateTime.Now < end)
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                if (condition())
                {
                    return;
                }
                Thread.Sleep(10);
            }
        }
    }
}
