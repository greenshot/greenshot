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
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Languages;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Greenshot.Editor.Tabs;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The tab row, the keys while a tool bar field has the focus and the color buttons work for keyboard and screen reader users
    /// </summary>
    public class EditorTabStripAccessibilityTests
    {
        public EditorTabStripAccessibilityTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void TabStrip_IsATabListWithNamedTabsAndButtons()
        {
            RunOnSta(() =>
            {
                using var strip = new EditorTabStrip { Width = 600, Height = 30, NewTabText = "New" };
                var first = CreateTab("First");
                var second = CreateTab("Second");
                strip.Insert(0, first);
                strip.Insert(1, second);
                strip.SelectedTab = first;
                first.Surface.Modified = false;
                second.Surface.Modified = true;
                EditorTab selectedByScreenReader = null;
                strip.TabSelected += (s, e) => selectedByScreenReader = e.Tab;

                var list = strip.AccessibilityObject;
                Assert.Equal(AccessibleRole.PageTabList, list.Role);
                Assert.Equal(3, list.GetChildCount());

                var firstTab = list.GetChild(0);
                Assert.Equal(AccessibleRole.PageTab, firstTab.Role);
                Assert.Equal("First", firstTab.Name);
                Assert.True((firstTab.State & AccessibleStates.Selected) != 0);
                Assert.Equal(AccessibleRole.PushButton, firstTab.GetChild(0).Role);
                Assert.Equal(Texts.Editor.CloseTab, firstTab.GetChild(0).Name);

                var secondTab = list.GetChild(1);
                Assert.Equal(string.Format(Texts.Editor.TabModified, "Second"), secondTab.Name);
                Assert.True((secondTab.State & AccessibleStates.Selected) == 0);
                Assert.Equal(secondTab.Name, firstTab.Navigate(AccessibleNavigation.Next).Name);

                var newTab = list.GetChild(2);
                Assert.Equal(AccessibleRole.PushButton, newTab.Role);
                Assert.Equal("New", newTab.Name);

                secondTab.DoDefaultAction();
                Assert.Same(second, selectedByScreenReader);

                first.Dispose();
                second.Dispose();
            });
        }

        [Theory]
        [InlineData(Keys.Control | Keys.Tab, true)]
        [InlineData(Keys.Control | Keys.W, true)]
        [InlineData(Keys.Control | Keys.Shift | Keys.C, true)]
        [InlineData(Keys.Alt | Keys.F, true)]
        [InlineData(Keys.F6, true)]
        [InlineData(Keys.F6 | Keys.Shift, true)]
        [InlineData(Keys.R, false)]
        [InlineData(Keys.Delete, false)]
        [InlineData(Keys.Escape, false)]
        [InlineData(Keys.Left, false)]
        [InlineData(Keys.Control | Keys.C, false)]
        [InlineData(Keys.Control | Keys.V, false)]
        [InlineData(Keys.Control | Keys.Z, false)]
        [InlineData(Keys.Control | Keys.A, false)]
        [InlineData(Keys.Control | Keys.Shift | Keys.Left, false)]
        [InlineData(Keys.Control | Keys.Back, false)]
        // AltGr types characters like @ or €
        [InlineData(Keys.Control | Keys.Alt | Keys.Q, false)]
        public void WorksWhileTyping_KeepsShortcutsButNotTheKeysOfTheField(Keys keys, bool works)
        {
            Assert.Equal(works, ImageEditorForm.WorksWhileTyping(keys));
        }

        [Fact]
        public void ColorDescription_SaysTheColor()
        {
            Assert.Equal(string.Format(Texts.Editor.ColorValue, "#FF8000"), ColorDescription.Describe(Color.FromArgb(255, 128, 0)));
            Assert.Equal(string.Format(Texts.Editor.ColorValue, Texts.Editor.ColorTransparent), ColorDescription.Describe(Color.Transparent));
            Assert.Equal(string.Format(Texts.Editor.ColorValueOpacity, "#FF0000", 50), ColorDescription.Describe(Color.FromArgb(128, 255, 0, 0)));
        }

        private static EditorTab CreateTab(string title)
        {
            var surface = new Surface(new Bitmap(20, 10));
            surface.CaptureDetails.Title = title;
            return new EditorTab(surface);
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromMinutes(1)), "Timed out");
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
