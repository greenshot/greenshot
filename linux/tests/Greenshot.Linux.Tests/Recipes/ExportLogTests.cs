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

#nullable disable

using Greenshot.Linux.Core.Destinations;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Tests.Recipes
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

        private static CaptureFlowContext CreateContext()
        {
            var context = new CaptureFlowContext(new CaptureRecipe("export_log", "Export log"))
            {
                Payload = FakeRecipeHost.Payload(16, 16)
            };
            context.Payload.EnsureSurface();
            return context;
        }

        private static StubDestination Destination(string designation, ExportResult result = null) =>
            new StubDestination(designation, () => result ?? ExportResult.Done());

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

            branch.RecordExport(Destination("A"), ExportResult.Done(), null, null);

            Assert.Single(context.GetExportLog().ToList());
        }

        [Fact]
        public void TakeUnnotified_ReturnsTheExportsSinceTheLastCall()
        {
            using var context = CreateContext();
            context.RecordExport(Destination("A"), ExportResult.Done(), null, null);
            Assert.Single(context.GetExportLog().TakeUnnotified());

            context.RecordExport(Destination("B"), ExportResult.Done(), null, null);

            var second = context.GetExportLog().TakeUnnotified();
            Assert.Equal("B", Assert.Single(second).Destination.Designation);
            Assert.Empty(context.GetExportLog().TakeUnnotified());
        }

        [Fact]
        public void RecordExport_Declined_IsNoExport()
        {
            using var context = CreateContext();

            context.RecordExport(Destination("A"), ExportResult.Aborted, null, null);

            Assert.Empty(context.GetExportLog().ToList());
        }

    }
}
