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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Pipeline;
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

            var shown = ExportNotification.GetShownExports(new[] { file, upload, failed });
            var notification = ExportNotification.CreateNotification(shown, null, null, null, "0192a5a4-0000-7000-8000-000000000000", null);

            Assert.Equal(3, notification.Lines.Count);
            Assert.Contains("a.png", notification.Lines[0].Text);
            Assert.Contains("offline", notification.Lines[2].Text);
            // One export failed
            Assert.Equal(NotificationKind.Error, notification.Kind);
            // Edit and Show in folder act on the capture, Copy link on the one link, nothing opens anything
            Assert.Equal(new[] { "edit", "show-in-folder", "copy-link" }, notification.Buttons.Select(b => b.Action));
            Assert.Equal(@"C:\Captures\a.png", notification.Buttons[1].Value);
            Assert.Equal("https://example.com/a", notification.Buttons[2].Value);
            Assert.Empty(notification.Buttons[2].Choices);
        }

        [Fact]
        public void CreateNotification_SeveralLinks_ADropDownChoosesWhichIsCopied()
        {
            var box = new ExportOutcome(Destination("Box"), ExportResult.Succeeded(uri: new Uri("https://box.example/a")), null, null);
            var dropbox = new ExportOutcome(Destination("Dropbox"), ExportResult.Succeeded(uri: new Uri("https://dropbox.example/b")), null, null);

            var notification = ExportNotification.CreateNotification(new[] { box, dropbox }, null, null, null, null, null);

            var copy = Assert.Single(notification.Buttons);
            Assert.Equal("copy-link", copy.Action);
            Assert.Null(copy.Value);
            Assert.Equal(new[] { "https://box.example/a", "https://dropbox.example/b" }, copy.Choices.Select(c => c.Value));
            Assert.StartsWith("Box: ", copy.Choices[0].Text);
        }

        [Fact]
        public void CreateNotification_Barcodes_ShowTheFullLinkWithThePunycodeHost_AndAreOnlyCopied()
        {
            var clipboard = new ExportOutcome(Destination("Clipboard"), ExportResult.Succeeded(), null, null);
            // The first a is the Cyrillic а
            var barcodes = new Greenshot.Base.Interfaces.Plugin.IBarcodeFeature[]
            {
                new TestBarcode("https://\u0430pple.com/login?x=1"), new TestBarcode("WIFI:S:home;T:WPA;P:secret;;", "QR_CODE"), new TestBarcode("javascript:alert(1)", "CODE_128")
            };

            var notification = ExportNotification.CreateNotification(new[] { clipboard }, barcodes, null, null, null, null);

            Assert.Equal(4, notification.Lines.Count);
            Assert.Contains("https://xn--pple-43d.com/login?x=1", notification.Lines[1].Text);
            // A QR code says so, another barcode is a barcode
            Assert.StartsWith("QR code: ", notification.Lines[2].Text);
            Assert.StartsWith("Barcode: ", notification.Lines[3].Text);
            // Only the http(s) link can be copied, nothing else becomes a link
            var copy = Assert.Single(notification.Buttons);
            Assert.Equal("copy-link", copy.Action);
            Assert.Contains("xn--pple-43d.com", copy.Value);
        }

        [Fact]
        public void CreateNotification_FailedFlow_TheExportsBeforeAndTheFailure()
        {
            // The flow failed after its exports, not in one of them (that would be the row of the export)
            var file = new ExportOutcome(Destination("FileNoDialog"), ExportResult.Succeeded(filePath: @"C:\Captures\a.png", target: "a.png"), null, null);

            var notification = ExportNotification.CreateNotification(new[] { file }, null, null, null, null, null, "The OCR failed");

            Assert.Equal(NotificationKind.Error, notification.Kind);
            Assert.Equal(2, notification.Lines.Count);
            Assert.Equal("The OCR failed", notification.Lines[1].Text);
        }

        [Fact]
        public void GetBarcodes_FromTheFeaturesOfTheCapture()
        {
            var details = new CaptureDetails();
            details.Features.Add(new TestBarcode("https://example.com"));
            details.Features.Add(new TestBarcode("https://example.com"));

            Assert.Equal(new[] { "https://example.com" }, ExportNotification.GetBarcodes(details).Select(b => b.RawText));
        }

        private sealed class TestBarcode : Greenshot.Base.Interfaces.Plugin.IBarcodeFeature
        {
            public TestBarcode(string rawText, string format = "QR_CODE")
            {
                RawText = rawText;
                Format = format;
            }

            public string RawText { get; }

            public string Format { get; }

            public Dapplo.Windows.Common.Structs.NativeRect Bounds => Dapplo.Windows.Common.Structs.NativeRect.Empty;

            public string FeatureType => "Barcode";

            public string Text => RawText;

            public string ToolTipText => RawText;

            public void Offset(int x, int y)
            {
            }
        }
    }
}
