/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Destinations;
using Greenshot.Recipes.Pipeline;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class ExportNotificationsTests
    {
        public ExportNotificationsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(NotificationDetail.ErrorsOnly)]
        [InlineData(NotificationDetail.Short)]
        [InlineData(NotificationDetail.Full)]
        public void Failure_IsAlwaysShown_WithTheButtons(NotificationDetail detail)
        {
            var content = ExportNotifications.Describe(detail, "Imgur", "Imgur", ExportResult.Failed("No connection"));

            Assert.NotNull(content);
            Assert.False(content.Succeeded);
            Assert.Equal("No connection", content.Detail);
            Assert.True(content.ShowButtons);
            Assert.True(content.CanEdit);
            Assert.False(content.CanOpen);
            // The preview only with all details
            Assert.Equal(detail == NotificationDetail.Full, content.ShowPreview);
        }

        [Fact]
        public void Success_WithErrorsOnly_IsNotShown()
        {
            Assert.Null(ExportNotifications.Describe(NotificationDetail.ErrorsOnly, "FileNoDialog", "File", ExportResult.Succeeded(@"C:\Captures\a.png")));
        }

        [Fact]
        public void Declined_IsNotShown()
        {
            Assert.Null(ExportNotifications.Describe(NotificationDetail.Full, "FileDialog", "File", ExportResult.Declined));
        }

        [Fact]
        public void Success_Short_ShowsWhereItWent_WithoutPreviewAndButtons()
        {
            var content = ExportNotifications.Describe(NotificationDetail.Short, "FileNoDialog", "File", ExportResult.Succeeded(@"C:\Captures\a.png"));

            Assert.True(content.Succeeded);
            Assert.Equal(@"C:\Captures\a.png", content.Detail);
            Assert.False(content.ShowPreview);
            Assert.False(content.ShowButtons);
            // A click on the notification opens the file
            Assert.True(content.CanOpen);
        }

        [Fact]
        public void Success_Full_ShowsTheLink_PreviewAndButtons()
        {
            var content = ExportNotifications.Describe(NotificationDetail.Full, "Imgur", "Imgur", ExportResult.Succeeded(uri: new Uri("https://i.imgur.com/abc.png")));

            Assert.Equal("https://i.imgur.com/abc.png", content.Detail);
            Assert.True(content.ShowPreview);
            Assert.True(content.ShowButtons);
            Assert.True(content.CanOpen);
            Assert.True(content.CanEdit);
        }

        [Fact]
        public void Success_WithoutFileOrLink_CanOnlyBeEdited()
        {
            var content = ExportNotifications.Describe(NotificationDetail.Full, "Clipboard", "Clipboard", ExportResult.Succeeded());

            Assert.Null(content.Detail);
            Assert.False(content.CanOpen);
            Assert.True(content.CanEdit);
        }

        [Fact]
        public void Editor_IsNotOfferedForTheEditor()
        {
            var content = ExportNotifications.Describe(NotificationDetail.Full, EditorDestination.DESIGNATION, "Editor", ExportResult.Succeeded(clearsModified: false, keepsCapture: true));

            Assert.False(content.CanEdit);
        }

        [Fact]
        public void DefaultAction_OpensTheExport_OrEditsTheCapture()
        {
            Action open = () => { };
            Action edit = () => { };
            Assert.Same(open, new ExportNotification { Open = open, Edit = edit }.DefaultAction);
            Assert.Same(edit, new ExportNotification { Edit = edit }.DefaultAction);
        }

        [Theory]
        [InlineData(3840, 2160, 711, 400)]
        [InlineData(2000, 300, 728, 109)]
        [InlineData(200, 100, 200, 100)]
        public void Preview_FitsTheNotification_KeepingTheAspectRatio(int width, int height, int expectedWidth, int expectedHeight)
        {
            using var capture = new Bitmap(width, height);
            using var preview = ExportNotifications.CreatePreview(capture);

            Assert.Equal(expectedWidth, preview.Width);
            Assert.Equal(expectedHeight, preview.Height);
        }
    }
}
