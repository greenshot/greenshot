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
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Recipes.ViewModels;
using Greenshot.Recipes.Views;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The destination window lists the destinations in one fixed order and numbers the entries as shown, also when filtered
    /// </summary>
    public class DynamicDestinationWindowTests
    {
        public DynamicDestinationWindowTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Destinations_AreInTheSameOrderWhateverOrderTheyCameIn()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[]
                {
                    new TestDestination("Zeta"),
                    new TestDestination("Printer"),
                    new TestDestination("Alpha", priority: 5),
                    new TestDestination("Clipboard"),
                    new TestDestination("Beta"),
                    new TestDestination("Windows10Share")
                });

                // The well known ones first in their order, then by priority, then by designation
                Assert.Equal(new[] { "Clipboard", "Printer", "Windows10Share", "Alpha", "Beta", "Zeta" },
                    window.AllDestinationTiles.Select(tile => tile.Destination.Designation).ToArray());
                window.Close();
            });
        }

        [Fact]
        public void Numbers_AreThePlaceInTheFilteredList()
        {
            RunOnSta(() =>
            {
                var destinations = Enumerable.Range(1, 12).Select(i => (IDestination)new TestDestination("Dest" + i.ToString("00"))).ToList();
                destinations.Add(new TestDestination("Other"));
                var window = new DynamicDestinationWindow(null, destinations);

                // All: 1 to 9 are the first nine, 0 is the tenth, then no number
                Assert.Equal(13, window.FilteredDestinationTiles.Count);
                Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "", "", "" },
                    window.FilteredDestinationTiles.Select(tile => tile.NumberText).ToArray());
                Assert.Equal("Ctrl+1", window.FilteredDestinationTiles[0].ShortcutText);
                Assert.Null(window.FilteredDestinationTiles[10].ShortcutText);

                // Filtered: the numbers start again at 1 and follow the order
                window.SearchText = "dest1";
                Assert.Equal(new[] { "Dest10", "Dest11", "Dest12" }, window.FilteredDestinationTiles.Select(tile => tile.Title).ToArray());
                Assert.Equal(new[] { "1", "2", "3" }, window.FilteredDestinationTiles.Select(tile => tile.NumberText).ToArray());
                Assert.Equal(0, window.AllDestinationTiles.First(tile => tile.Title == "Dest01").Position);

                // The first entry is the one Enter takes
                Assert.Equal(0, window.DestinationList.SelectedIndex);

                window.SearchText = "nothing matches this";
                Assert.Empty(window.FilteredDestinationTiles);
                window.Close();
            });
        }

        [Theory]
        [InlineData(Key.D1, 0)]
        [InlineData(Key.D9, 8)]
        [InlineData(Key.D0, 9)]
        [InlineData(Key.NumPad1, 0)]
        [InlineData(Key.NumPad0, 9)]
        [InlineData(Key.A, -1)]
        [InlineData(Key.LeftCtrl, -1)]
        [InlineData(Key.Enter, -1)]
        public void Digits_PickThePlaceInTheList(Key key, int index)
        {
            Assert.Equal(index, DynamicDestinationWindow.IndexOfDigit(key));
        }

        [Theory]
        // In the middle of the work area: centered on the mouse
        [InlineData(1000, 500, 400, 300, 800, 350)]
        // Near the right and bottom edge: stays inside
        [InlineData(1900, 1050, 400, 300, 1520, 780)]
        // Near the left and top edge: stays inside
        [InlineData(10, 10, 400, 300, 0, 0)]
        // Wider and higher than the work area: starts at its top left
        [InlineData(500, 500, 3000, 2000, 0, 0)]
        public void TheWindowAtTheMouse_IsCenteredOnIt_AndStaysInTheWorkArea(int mouseX, int mouseY, int width, int height, int expectedX, int expectedY)
        {
            var workArea = new System.Drawing.Rectangle(0, 0, 1920, 1080);

            var (x, y) = DynamicDestinationWindow.LocationAtMouse(mouseX, mouseY, width, height, workArea);

            Assert.Equal((expectedX, expectedY), (x, y));
        }

        [Fact]
        public void TheWindowAtTheMouse_OnASecondScreen_StaysInThatScreen()
        {
            // A screen to the left of the main screen has negative coordinates
            var workArea = new System.Drawing.Rectangle(-1920, 0, 1920, 1040);

            var (x, y) = DynamicDestinationWindow.LocationAtMouse(-10, 1030, 400, 300, workArea);

            Assert.Equal((-400, 740), (x, y));
        }

        [Theory]
        // Inside: stays where it is
        [InlineData(100, 200, 400, 300, 100, 200)]
        // Grew below the bottom of the work area: moves up
        [InlineData(100, 900, 400, 300, 100, 780)]
        // Higher than the work area: starts at its top
        [InlineData(100, 500, 400, 2000, 100, 0)]
        public void AWindowWhichGrew_IsMovedBackIntoTheWorkArea(int x, int y, int width, int height, int expectedX, int expectedY)
        {
            var workArea = new System.Drawing.Rectangle(0, 0, 1920, 1080);

            Assert.Equal((expectedX, expectedY), DynamicDestinationWindow.KeepInside(x, y, width, height, workArea));
        }

        [Theory]
        [InlineData("Top", DestinationWindowPreviewPosition.Top)]
        [InlineData("left", DestinationWindowPreviewPosition.Left)]
        [InlineData("Default", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("1", null)]
        public void ThePreviewPositionOfAStep_IsTopLeftOrTheSetting(string value, DestinationWindowPreviewPosition? expected)
        {
            Assert.Equal(expected, DestinationWindowOptions.ParsePreviewPosition(value));
        }

        [Theory]
        [InlineData("Mouse", true)]
        [InlineData("center", false)]
        [InlineData("Default", null)]
        [InlineData(null, null)]
        public void TheWindowPositionOfAStep_IsMouseCenterOrTheSetting(string value, bool? expected)
        {
            Assert.Equal(expected, DestinationWindowOptions.ParseAtMouse(value));
        }

        [Fact]
        public void TheOptionsOfAStep_OverrideTheSettings_TheOthersComeFromTheSettings()
        {
            var configuration = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var position = configuration.DestinationWindowPreviewPosition;
            var autoSize = configuration.DestinationWindowAutoSize;
            try
            {
                configuration.DestinationWindowPreviewPosition = DestinationWindowPreviewPosition.Left;
                configuration.DestinationWindowAutoSize = true;

                var resolved = new DestinationWindowOptions { AutoSize = false, ShowMetadata = true }.Resolve(configuration);

                Assert.Equal(DestinationWindowPreviewPosition.Left, resolved.PreviewPosition);
                Assert.False(resolved.AutoSize);
                Assert.True(resolved.ShowMetadata);
                Assert.Equal(configuration.DestinationWindowShowPreview, resolved.ShowPreview);
                Assert.Equal(configuration.DestinationWindowCloseOnDeactivate, resolved.CloseOnDeactivate);
                Assert.Equal(configuration.DestinationWindowShowRecipes, resolved.ShowRecipes);
                Assert.False(new DestinationWindowOptions { ShowRecipes = false }.Resolve(configuration).ShowRecipes);
            }
            finally
            {
                configuration.DestinationWindowPreviewPosition = position;
                configuration.DestinationWindowAutoSize = autoSize;
            }
        }

        [Fact]
        public void ThePreviewAtTheLeft_PutsTheListBesideIt_WithoutAPreviewTheListIsBelow()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[] { new TestDestination("Clipboard") });

                window.ApplyOptions(new DestinationWindowOptions { ShowPreview = true, PreviewPosition = DestinationWindowPreviewPosition.Left });
                Assert.True(window.IsPreviewLeft);
                Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(window.ListPanel));
                Assert.Equal(0, System.Windows.Controls.Grid.GetRow(window.ListPanel));
                Assert.Equal(1, System.Windows.Controls.Grid.GetColumnSpan(window.SidePanel));
                Assert.Equal(400 + DynamicDestinationWindow.SidePanelWidth, window.Width);

                // The recipes are above the list again
                window.ApplyOptions(new DestinationWindowOptions { ShowPreview = false, PreviewPosition = DestinationWindowPreviewPosition.Left });
                Assert.False(window.IsPreviewLeft);
                Assert.Equal(0, System.Windows.Controls.Grid.GetColumn(window.ListPanel));
                Assert.Equal(1, System.Windows.Controls.Grid.GetRow(window.ListPanel));
                Assert.Equal(2, System.Windows.Controls.Grid.GetColumnSpan(window.SidePanel));
                Assert.Equal(400, window.Width);
                window.Close();
            });
        }

        [Fact]
        public void AWindowWhichClosesLikeAContextMenu_HasNoTitleBar_OneWhichGrowsHasNoListHeight()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[] { new TestDestination("Clipboard") });
                Assert.Contains(window.TitleBar, window.RootGrid.Children.Cast<System.Windows.UIElement>());
                Assert.Equal(360, window.DestinationList.MaxHeight);

                window.ApplyOptions(new DestinationWindowOptions { CloseOnDeactivate = true, AutoSize = true });

                Assert.DoesNotContain(window.TitleBar, window.RootGrid.Children.Cast<System.Windows.UIElement>());
                Assert.True(double.IsPositiveInfinity(window.DestinationList.MaxHeight));
                window.Close();
            });
        }

        [Fact]
        public void TheOcrBadge_ShowsWhetherTheCaptureHasRecognizedText()
        {
            var details = new CaptureDetails();
            // Other features (a window, a QR code) are no text
            details.Features.Add(new OtherFeature());
            Assert.False(CaptureMetadataViewModel.HasOcrLines(details));

            details.Features.Add(new DetectedOcrLine(new NativeRect(0, 0, 40, 20), "text", new List<OcrWordInfo>()));
            Assert.True(CaptureMetadataViewModel.HasOcrLines(details));
            // Only shown: the capture keeps everything
            Assert.Equal(2, details.Features.Count);
        }

        [Fact]
        public void HoldingCtrl_UnderlinesTheNumbers()
        {
            var tile = new DestinationTileViewModel { Title = "x" };
            var changed = new List<string>();
            tile.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            tile.ShortcutHintVisible = true;
            tile.ShortcutHintVisible = true;

            Assert.True(tile.ShortcutHintVisible);
            Assert.Equal(new[] { nameof(DestinationTileViewModel.ShortcutHintVisible) }, changed);
        }

        [Fact]
        public void EntriesBelowADestination_ShowWhenOpened_AndAreNotNumbered()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[]
                {
                    new TestDestination("Clipboard"),
                    new TestDestination("Printer", hasDynamicDestinations: true),
                    new TestDestination("Zeta")
                });
                var printer = window.AllDestinationTiles.Single(tile => tile.Destination.Designation == "Printer");
                window.ApplySubEntries(printer, new IDestination[] { new TestDestination("Laser"), new TestDestination("Ink") });

                // Not shown until opened, and a plus is there
                Assert.Equal(2, printer.SubEntries.Count);
                Assert.Equal(System.Windows.Visibility.Visible, printer.ToggleVisibility);
                Assert.Equal(new[] { "Clipboard", "Printer", "Zeta" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());

                window.ToggleExpanded(printer);
                Assert.Equal(new[] { "Clipboard", "Printer", "Ink", "Laser", "Zeta" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                // The numbers are the places in the list as shown, the entries count
                Assert.Equal(new[] { "1", "2", "3", "4", "5" }, window.DisplayedTiles.Select(tile => tile.NumberText).ToArray());
                Assert.Equal(System.Windows.Visibility.Collapsed, printer.SubEntries[0].ToggleVisibility);
                Assert.Same(printer, printer.SubEntries[0].Parent);

                // Filtering keeps the open entries of what matches
                window.SearchText = "print";
                Assert.Equal(new[] { "Printer", "Ink", "Laser" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                Assert.Equal(new[] { "1", "2", "3" }, window.DisplayedTiles.Select(tile => tile.NumberText).ToArray());
                window.SearchText = "";

                // Closed again, the numbers are those of the destinations
                window.ToggleExpanded(printer);
                Assert.Equal(new[] { "Clipboard", "Printer", "Zeta" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                Assert.Equal(new[] { "1", "2", "3" }, window.DisplayedTiles.Select(tile => tile.NumberText).ToArray());
                window.Close();
            });
        }

        [Fact]
        public void ADestinationOpens_WhenAnEntryBelowItMatchesTheSearch()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[]
                {
                    new TestDestination("Clipboard"),
                    new TestDestination("Printer", hasDynamicDestinations: true),
                    new TestDestination("Zeta")
                });
                var printer = window.AllDestinationTiles.Single(tile => tile.Destination.Designation == "Printer");
                window.ApplySubEntries(printer, new IDestination[] { new TestDestination("Laser"), new TestDestination("Ink") });

                // "ink" is only in the entry: the destination stays and opens, with the entry which matches
                window.SearchText = "ink";
                Assert.Equal(new[] { "Printer", "Ink" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                Assert.True(printer.IsShowingEntries);
                Assert.False(printer.IsExpanded);
                Assert.Equal("<<<", printer.ToggleGlyph);
                Assert.Equal(new[] { "1", "2" }, window.DisplayedTiles.Select(tile => tile.NumberText).ToArray());

                // Closed by the user: it stays closed while the search text stays
                window.ToggleExpanded(printer);
                Assert.Equal(new[] { "Printer" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                Assert.Equal(">>>", printer.ToggleGlyph);

                // Another search text opens it again, an empty one closes it
                window.SearchText = "las";
                Assert.Equal(new[] { "Printer", "Laser" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                window.SearchText = "";
                Assert.Equal(new[] { "Clipboard", "Printer", "Zeta" }, window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                Assert.False(printer.IsShowingEntries);
                window.Close();
            });
        }

        [Fact]
        public void ExpandAll_ShowsAndHidesTheEntriesOfAllDestinations_AndNumbersAgain()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[]
                {
                    new TestDestination("Clipboard"),
                    new TestDestination("Editor", hasDynamicDestinations: true),
                    new TestDestination("Printer", hasDynamicDestinations: true)
                });
                var editor = window.AllDestinationTiles.Single(tile => tile.Destination.Designation == "Editor");
                var printer = window.AllDestinationTiles.Single(tile => tile.Destination.Designation == "Printer");
                window.ApplySubEntries(editor, new IDestination[] { new TestDestination("Shot1") });
                window.ApplySubEntries(printer, new IDestination[] { new TestDestination("Laser"), new TestDestination("Ink") });

                Assert.True(window.ExpandAll(true));
                Assert.Equal(new[] { "Editor", "Shot1", "Clipboard", "Printer", "Ink", "Laser" },
                    window.DisplayedTiles.Select(tile => tile.Destination.Designation).ToArray());
                Assert.Equal(new[] { "1", "2", "3", "4", "5", "6" }, window.DisplayedTiles.Select(tile => tile.NumberText).ToArray());
                Assert.False(window.ExpandAll(true));

                Assert.True(window.ExpandAll(false));
                Assert.Equal(3, window.DisplayedTiles.Count);
                Assert.Equal(new[] { "1", "2", "3" }, window.DisplayedTiles.Select(tile => tile.NumberText).ToArray());
                window.Close();
            });
        }

        [Fact]
        public void ADestinationWhichNeedsAnEntry_BecomesTheOnlyEntry_AndOpensWithSeveral()
        {
            RunOnSta(() =>
            {
                var window = new DynamicDestinationWindow(null, new IDestination[]
                {
                    new TestDestination("Single", hasDynamicDestinations: true, useDynamicsOnly: true),
                    new TestDestination("Several", hasDynamicDestinations: true, useDynamicsOnly: true)
                });
                var single = window.AllDestinationTiles.Single(tile => tile.Destination.Designation == "Single");
                var several = window.AllDestinationTiles.Single(tile => tile.Destination.Designation == "Several");
                var only = new TestDestination("Only");

                window.ApplySubEntries(single, new IDestination[] { only });
                window.ApplySubEntries(several, new IDestination[] { new TestDestination("A"), new TestDestination("B") });

                // One: the row is that entry, no plus
                Assert.Same(only, single.Destination);
                Assert.Equal("Only", single.Title);
                Assert.Empty(single.SubEntries);
                Assert.Equal(System.Windows.Visibility.Collapsed, single.ToggleVisibility);

                // Several: the destination itself can't be chosen, it opens
                Assert.False(several.CanChooseItself);
                Assert.True(single.CanChooseItself);
                window.Close();
            });
        }

        private sealed class OtherFeature : IDetectedFeature
        {
            public NativeRect Bounds => new NativeRect(0, 0, 10, 10);
            public string FeatureType => "Other";
            public string Text => "";
            public string ToolTipText => "";
            public void Offset(int x, int y)
            {
            }
        }

        private sealed class TestDestination : DestinationBase
        {
            public TestDestination(string designation, int priority = 10, bool hasDynamicDestinations = false, bool useDynamicsOnly = false)
            {
                Designation = designation;
                Descriptor = new DestinationDescriptor(designation, priority, hasDynamicDestinations: hasDynamicDestinations, useDynamicsOnly: useDynamicsOnly);
            }

            public override string Designation { get; }

            public override DestinationDescriptor Descriptor { get; }

            public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken) => Task.FromResult(ExportResult.Declined);
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
