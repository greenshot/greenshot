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
using System.IO;
using System.Threading.Tasks;
using Greenshot.Editor.Standalone.Ipc;
using Greenshot.Editor.Standalone.Settings;
using Xunit;

namespace Greenshot.Editor.Standalone.Tests.Ipc
{
    public class SingleInstanceTests
    {
        [Fact]
        public void TheArguments_NameFilesAndHandedOverCaptures()
        {
            var request = StartRequest.Parse(new[] { "one.png", "--handover", "capture.greenshot", "--unknown", "two.greenshot", "--handover" });

            Assert.Equal(new[] { Path.GetFullPath("one.png"), Path.GetFullPath("two.greenshot") }, request.Files);
            Assert.Equal(new[] { Path.GetFullPath("capture.greenshot") }, request.Handover);
        }

        [Fact]
        public async Task ASecondEditor_HandsItsFilesToTheFirst()
        {
            SingleInstance.PipeName = "GreenshotEditorTest_" + Guid.NewGuid().ToString("N");
            var request = new StartRequest { Files = { "/tmp/a.png" }, Handover = { "/tmp/b.greenshot" } };
            Assert.False(SingleInstance.TrySend(request), "Nobody listens yet: this editor is the first");

            var received = new TaskCompletionSource<StartRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnReceived(StartRequest got) => received.TrySetResult(got);
            SingleInstance.Received += OnReceived;
            try
            {
                SingleInstance.Listen();
                var timeout = DateTime.UtcNow.AddSeconds(10);
                while (!SingleInstance.TrySend(request))
                {
                    Assert.True(DateTime.UtcNow < timeout, "The first editor listens");
                    await Task.Delay(20, TestContext.Current.CancellationToken);
                }

                var got = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal(request.Files, got.Files);
                Assert.Equal(request.Handover, got.Handover);
            }
            finally
            {
                SingleInstance.Received -= OnReceived;
                SingleInstance.Stop();
            }
        }

        [Fact]
        public void OnlyDocumentsInTheHandoverFolder_AreHandedOverFiles()
        {
            string folder = EditorPaths.HandoverDirectory;
            Assert.True(EditorPaths.IsHandoverFile(Path.Combine(folder, "abc.greenshot")));
            Assert.False(EditorPaths.IsHandoverFile(Path.Combine(folder, "abc.png")));
            Assert.False(EditorPaths.IsHandoverFile(Path.Combine(folder, "sub", "abc.greenshot")));
            Assert.False(EditorPaths.IsHandoverFile(Path.Combine(folder, "..", "abc.greenshot")));
            Assert.False(EditorPaths.IsHandoverFile(Path.Combine(Path.GetTempPath(), "GreenshotEditorOther", "abc.greenshot")));
            Assert.False(EditorPaths.IsHandoverFile(null));
            Assert.False(EditorPaths.IsHandoverFile(string.Empty));
        }
    }
}
