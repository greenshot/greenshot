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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Pipeline;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The exports of a flow, which the Notification step shows
    /// </summary>
    public class ExportLogTests
    {
        public ExportLogTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private sealed class StubDestination : DestinationBase
        {
            private readonly Func<ExportResult> _result;

            public StubDestination(string designation, Func<ExportResult> result)
            {
                Designation = designation;
                Descriptor = new DestinationDescriptor(designation);
                _result = result;
            }

            public override string Designation { get; }
            public override DestinationDescriptor Descriptor { get; }

            public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken) => Task.FromResult(_result());
        }

        private static CaptureFlowContext CreateContext()
        {
            var context = new CaptureFlowContext(new CaptureRecipe("export_log", "Export log"))
            {
                Payload = new CapturePayload(new Capture(new Bitmap(16, 16)))
            };
            context.Payload.EnsureSurface();
            return context;
        }

        private static StubDestination Destination(string designation, ExportResult result = null) =>
            new StubDestination(designation, () => result ?? ExportResult.Succeeded(clearsModified: false));

        [Fact]
        public async Task Dispatch_TwoDestinationSteps_BothAreInTheLog()
        {
            using var context = CreateContext();

            // Destination A, then Destination B: the Notification step after them shows both
            await Task.Run(() => new DestinationDispatcher().DispatchAsync(context, new IDestination[] { Destination("A") }));
            await Task.Run(() => new DestinationDispatcher().DispatchAsync(context, new IDestination[] { Destination("B") }));

            var exports = context.GetExportLog().TakeUnnotified();
            Assert.Equal(new[] { "A", "B" }, exports.Select(e => e.Destination.Designation));
            Assert.All(exports, e => Assert.NotNull(e.Source));
        }

        [Fact]
        public void RecordExport_InABranch_IsInTheLogOfTheFlow()
        {
            using var context = CreateContext();
            var branch = context.CreateBranchContext();

            branch.RecordExport(Destination("A"), ExportResult.Succeeded(), null, null);

            Assert.Single(context.GetExportLog().ToList());
        }

        [Fact]
        public void TakeUnnotified_ReturnsTheExportsSinceTheLastCall()
        {
            using var context = CreateContext();
            context.RecordExport(Destination("A"), ExportResult.Succeeded(), null, null);
            Assert.Single(context.GetExportLog().TakeUnnotified());

            context.RecordExport(Destination("B"), ExportResult.Succeeded(), null, null);

            var second = context.GetExportLog().TakeUnnotified();
            Assert.Equal("B", Assert.Single(second).Destination.Designation);
            Assert.Empty(context.GetExportLog().TakeUnnotified());
        }

        [Fact]
        public void RecordExport_Declined_IsNoExport()
        {
            using var context = CreateContext();

            context.RecordExport(Destination("A"), ExportResult.Declined, null, null);

            Assert.Empty(context.GetExportLog().ToList());
        }

        [Fact]
        public void ExportOutcome_Destination_IsTheOneWhichExported()
        {
            var picked = Destination("Clipboard");

            var outcome = new ExportOutcome(Destination("Picker"), ExportResult.Succeeded().WithExportedBy(picked), null, null);

            Assert.Same(picked, outcome.Destination);
        }

        [Fact]
        public void CreateNotification_FileAndUploadAndFailure_LinesKindAndButtonsFromTheirResults()
        {
            var file = new ExportOutcome(Destination("FileNoDialog"), ExportResult.Succeeded(filePath: @"C:\Captures\a.png", target: "a.png"), null, null);
            var upload = new ExportOutcome(Destination("Imgur"), ExportResult.Succeeded(uri: new Uri("https://example.com/a")), null, null);
            var failed = new ExportOutcome(Destination("Jira"), ExportResult.Failed("offline"), null, null);

            var shown = NotificationStep.GetShownExports(new[] { file, upload, failed });
            var notification = NotificationStep.CreateNotification(shown, null, "0192a5a4-0000-7000-8000-000000000000", null);

            Assert.Equal(3, notification.Lines.Count);
            Assert.Contains("a.png", notification.Lines[0].Text);
            Assert.Contains("offline", notification.Lines[2].Text);
            // One export failed
            Assert.Equal(NotificationKind.Error, notification.Kind);
            // The buttons act on the file and on the link of their own export
            Assert.Contains(notification.Buttons, b => b.Value == @"C:\Captures\a.png");
            Assert.Contains(notification.Buttons, b => b.Value == "https://example.com/a");
        }
    }
}
