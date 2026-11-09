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
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Plugin.RapidOcr;
using Greenshot.Plugin.RapidOcr.Files;
using Greenshot.Plugin.RapidOcr.ViewModels;
using Xunit;

namespace Greenshot.Tests.RapidOcr
{
    /// <summary>
    /// The download window: asking, the download, and how it ends with and without the window
    /// </summary>
    public class RapidOcrDownloadViewModelTests
    {
        public RapidOcrDownloadViewModelTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static IRapidOcrLanguage Language => Texts.Get<IRapidOcrLanguage>();

        private static RapidOcrDownloadViewModel Create(Func<IProgress<DownloadProgress>, CancellationToken, Task> download, List<(NotificationKind Kind, string Text)> notifications) =>
            new(Language, "question", Language.DownloadLater, download, (kind, text) => notifications.Add((kind, text)), CancellationToken.None);

        [Fact]
        public async Task NotNow_DownloadsNothing()
        {
            bool downloaded = false;
            var notifications = new List<(NotificationKind, string)>();
            var viewModel = Create((_, _) =>
            {
                downloaded = true;
                return Task.CompletedTask;
            }, notifications);
            bool closeRequested = false;
            viewModel.CloseRequested += (_, _) => closeRequested = true;

            viewModel.Decline();

            Assert.False(await viewModel.Completion);
            Assert.False(downloaded);
            Assert.True(closeRequested);
            Assert.Empty(notifications);
        }

        [Fact]
        public async Task ClosingTheWindowWhileAsking_IsNotNow()
        {
            var viewModel = Create((_, _) => Task.CompletedTask, new List<(NotificationKind, string)>());

            viewModel.WindowClosed();

            Assert.False(await viewModel.Completion);
            Assert.Equal(DownloadStage.Cancelled, viewModel.Stage);
        }

        [Fact]
        public async Task Download_EndsReadyInTheWindow_WithoutNotification()
        {
            var notifications = new List<(NotificationKind, string)>();
            var viewModel = Create((_, _) => Task.CompletedTask, notifications);

            await viewModel.DownloadAsync();

            Assert.True(await viewModel.Completion);
            Assert.Equal(DownloadStage.Ready, viewModel.Stage);
            Assert.True(viewModel.IsFinished);
            Assert.Equal(Language.DownloadReady, viewModel.ResultText);
            Assert.Empty(notifications);
        }

        [Fact]
        public async Task InformMe_ClosesTheWindow_AndNotifiesAtTheEnd()
        {
            var notifications = new List<(NotificationKind Kind, string Text)>();
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var viewModel = Create((_, _) => release.Task, notifications);
            bool closeRequested = false;
            viewModel.CloseRequested += (_, _) => closeRequested = true;

            var download = viewModel.DownloadAsync();
            Assert.True(viewModel.IsDownloading);
            viewModel.InformWhenDone();
            viewModel.WindowClosed();
            Assert.True(closeRequested);
            // The download goes on after the window closed
            Assert.False(viewModel.Completion.IsCompleted);

            release.SetResult(true);
            await download;

            Assert.True(await viewModel.Completion);
            var notification = Assert.Single(notifications);
            Assert.Equal(NotificationKind.Info, notification.Kind);
            Assert.Equal(Language.DownloadReady, notification.Text);
        }

        [Fact]
        public async Task FailedDownload_AfterTheWindowClosed_NotifiesTheError()
        {
            var notifications = new List<(NotificationKind Kind, string Text)>();
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var viewModel = Create(async (_, _) =>
            {
                await release.Task;
                throw new HttpRequestException("404 Not Found");
            }, notifications);

            var download = viewModel.DownloadAsync();
            viewModel.WindowClosed();
            release.SetResult(true);
            await download;

            Assert.False(await viewModel.Completion);
            Assert.Equal(DownloadStage.Failed, viewModel.Stage);
            Assert.True(viewModel.IsError);
            var notification = Assert.Single(notifications);
            Assert.Equal(NotificationKind.Error, notification.Kind);
            Assert.Contains("404 Not Found", notification.Text);
        }

        [Fact]
        public async Task Cancel_StopsTheDownload()
        {
            var notifications = new List<(NotificationKind, string)>();
            var viewModel = Create((_, token) => Task.Delay(Timeout.Infinite, token), notifications);

            var download = viewModel.DownloadAsync();
            viewModel.Cancel();
            await download;

            Assert.False(await viewModel.Completion);
            Assert.Equal(DownloadStage.Cancelled, viewModel.Stage);
            Assert.Equal(Language.DownloadCancelled, viewModel.ResultText);
            Assert.Empty(notifications);
        }

        [Fact]
        public async Task Progress_IsShown()
        {
            var reported = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var viewModel = Create(async (progress, _) =>
            {
                progress.Report(new DownloadProgress(RapidOcrFiles.DetectionFile, 1024 * 1024, 4 * 1024 * 1024));
                await release.Task;
            }, new List<(NotificationKind, string)>());
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(RapidOcrDownloadViewModel.CurrentFile)) reported.TrySetResult(true);
            };

            var download = viewModel.DownloadAsync();
            // Progress<T> reports on the pool here (no UI thread in the tests): wait for it, not for a time
            await reported.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.Equal(25, viewModel.Percentage, 3);
            Assert.Equal(RapidOcrFiles.DetectionFile, viewModel.CurrentFile);
            Assert.Equal(string.Format(Language.DownloadProgress, 1.0, 4.0), viewModel.ProgressText);

            release.SetResult(true);
            await download;
            Assert.Equal(100, viewModel.Percentage, 3);
        }
    }
}
