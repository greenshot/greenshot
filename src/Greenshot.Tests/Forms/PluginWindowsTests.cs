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
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using Greenshot.Plugin.Atlassian.Jira.Views;
using Xunit;

namespace Greenshot.Tests.Forms
{
    /// <summary>
    /// The WPF windows of the Jira plugin
    /// </summary>
    [Collection(TestCollections.WpfThemeState)]
    public class PluginWindowsTests
    {
        public PluginWindowsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static void RunOnSta(Action action)
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (threadEx != null)
            {
                throw new Exception("Failed on the STA thread", threadEx);
            }
        }

        [Fact]
        public void JiraUploadWindow_WithoutConnection_CannotUpload()
        {
            RunOnSta(() =>
            {
                var window = new JiraUploadWindow(null);
                var uploadButton = (Button)window.FindName("UploadButton");
                Assert.False(uploadButton.IsEnabled);
                Assert.False(((ComboBox)window.FindName("FilterBox")).IsEnabled);
                Assert.False(((ListView)window.FindName("IssueList")).IsEnabled);

                // A typed key is only an issue after it was looked up
                ((TextBox)window.FindName("KeyBox")).Text = "GREENSHOT-1";
                Assert.False(uploadButton.IsEnabled);
                window.Close();
            });
        }
    }
}
