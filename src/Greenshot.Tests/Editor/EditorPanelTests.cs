/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Drawing;
using System.Linq;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls.Emoji;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Panels;
using Greenshot.Editor.Panels.Sections;
using Greenshot.Editor.Panels.ViewModels;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The panel area of the editor: when it opens by itself, which sections the Selection panel shows, and what the emoji and the
    /// size sections change. Only the logic, no WPF.
    /// </summary>
    public class EditorPanelTests
    {
        private readonly IEditorConfiguration _configuration;

        public EditorPanelTests()
        {
            TestEnvironment.EnsureInitialized();
            _configuration = IniConfigRegistry.GetSection<IEditorConfiguration>();
        }

        private sealed class OtherPanel : IEditorPanel
        {
            public string Id => "Test.Other";
            public string Title => "Other";
            public int Order => 10;
            public bool IsAvailable(IEditorPanelContext context) => true;
            public object CreateViewModel(IEditorPanelContext context) => new object();
            public object CreateView(object viewModel) => null;
        }

        private (EditorPanelContext Context, EditorPanelHost Host, List<bool> Changes) CreateHost(Surface surface, string selectedPanelId = null)
        {
            var sections = new List<IEditorSection> { new SizeAndPositionSection(), new EmojiSection(_configuration) };
            var panels = new List<IEditorPanel> { new SelectionPanel(sections, _configuration), new OtherPanel() };
            EditorPanelHost host = null;
            var context = new EditorPanelContext(null, (id, activate) => host.Show(id, activate), null);
            context.SetSurface(surface);
            host = new EditorPanelHost(context, panels, sections, selectedPanelId);
            var changes = new List<bool>();
            host.Changed += (_, activate) => changes.Add(activate);
            return (context, host, changes);
        }

        private static EmojiContainer AddEmoji(Surface surface, string emoji, int left = 10, int top = 10)
        {
            var container = new EmojiContainer(surface, emoji) { Left = left, Top = top };
            surface.AddElement(container, false, false);
            return container;
        }

        [Fact]
        public void EmojiTool_OpensTheSelectionPanelWithoutTheKeyboard_AndClosesItAgain()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var (_, host, changes) = CreateHost(surface);
            Assert.False(host.IsOpen);

            surface.DrawingMode = DrawingModes.Emoji;
            host.UpdateAutomatic();
            Assert.True(host.IsOpen);
            Assert.True(host.OpenedAutomatically);
            Assert.Equal(SelectionPanel.PanelId, host.ActivePanel.Id);
            Assert.Equal(new[] { false }, changes);

            surface.DrawingMode = DrawingModes.Rect;
            host.UpdateAutomatic();
            Assert.False(host.IsOpen);
        }

        [Fact]
        public void PanelOpenedByTheUser_StaysOpen()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var (_, host, changes) = CreateHost(surface);

            Assert.True(host.Show(SelectionPanel.PanelId, activate: true));
            Assert.Equal(new[] { true }, changes);
            surface.DrawingMode = DrawingModes.Emoji;
            host.UpdateAutomatic();
            surface.DrawingMode = DrawingModes.None;
            host.UpdateAutomatic();

            Assert.True(host.IsOpen);
            Assert.False(host.OpenedAutomatically);
        }

        [Fact]
        public void PanelClosedByTheUser_StaysClosedUntilAnEmojiIsNeededAgain()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var (_, host, _) = CreateHost(surface);
            surface.DrawingMode = DrawingModes.Emoji;
            host.UpdateAutomatic();
            host.Close();

            // Still the Emoji tool, e.g. after a selection change
            host.UpdateAutomatic();
            Assert.False(host.IsOpen);

            surface.DrawingMode = DrawingModes.Rect;
            host.UpdateAutomatic();
            surface.DrawingMode = DrawingModes.Emoji;
            host.UpdateAutomatic();
            Assert.True(host.IsOpen);
        }

        [Fact]
        public void AnotherPanelInUse_IsNotReplacedBySelection()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var (_, host, _) = CreateHost(surface);
            host.Show("Test.Other", activate: false);

            surface.DrawingMode = DrawingModes.Emoji;
            host.UpdateAutomatic();

            Assert.Equal("Test.Other", host.ActivePanel.Id);
        }

        [Fact]
        public void StoredPanel_IsShownFirst_UnknownFallsBackToSelection()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            Assert.Equal("Test.Other", CreateHost(surface, "Test.Other").Host.ActivePanel.Id);
            Assert.Equal(SelectionPanel.PanelId, CreateHost(surface, "Removed.Plugin").Host.ActivePanel.Id);
        }

        [Fact]
        public void SelectionPanel_ShowsTheSectionsOfTheSelection_SizeOnTop()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var (context, _, _) = CreateHost(surface);
            var sections = new List<IEditorSection> { new EmojiSection(_configuration), new SizeAndPositionSection() };
            using var viewModel = new SelectionPanelViewModel(context, sections, _configuration);
            Assert.True(viewModel.IsEmpty);

            var emoji = AddEmoji(surface, "😀");
            surface.SelectElement(emoji);
            context.OnSelectionChanged();
            Assert.Equal(new[] { SizeAndPositionSection.SectionId, EmojiSection.SectionId }, viewModel.Sections.Select(item => item.Section.Id));

            var rectangle = new RectangleContainer(surface) { Width = 20, Height = 20 };
            surface.AddElement(rectangle, false, false);
            surface.DeselectAllElements();
            surface.SelectElement(rectangle);
            context.OnSelectionChanged();
            Assert.Equal(new[] { SizeAndPositionSection.SectionId }, viewModel.Sections.Select(item => item.Section.Id));
        }

        [Fact]
        public void EmojiPick_ChangesTheSelectedEmoji_AsOneUndoStep()
        {
            var recent = _configuration.RecentEmoji;
            var history = _configuration.RecentEmojis.ToList();
            try
            {
                using var surface = new Surface(new Bitmap(200, 100));
                var (context, _, _) = CreateHost(surface);
                var first = AddEmoji(surface, "😀");
                var second = AddEmoji(surface, "😎", 60);
                surface.SelectElement(first);
                surface.SelectElement(second);
                using var viewModel = new EmojiSectionViewModel(context, _configuration, new List<Emojis.Group>());

                viewModel.Picker.Pick("🎉", fromKeyboard: true);

                Assert.Equal("🎉", first.Emoji);
                Assert.Equal("🎉", second.Emoji);
                Assert.Equal("🎉", _configuration.RecentEmoji);
                Assert.Equal("🎉", _configuration.RecentEmojis.First());
                Assert.Equal("🎉", viewModel.Picker.CurrentEmoji);

                surface.Undo();
                Assert.Equal("😀", first.Emoji);
                Assert.Equal("😎", second.Emoji);
                surface.Redo();
                Assert.Equal("🎉", second.Emoji);
            }
            finally
            {
                _configuration.RecentEmoji = recent;
                _configuration.RecentEmojis = history;
            }
        }

        [Fact]
        public void EmojiSearch_FindsEveryWordInTheNameOrCategory()
        {
            var smileys = new Emojis.Group { Name = "Smileys & Emotion" };
            smileys.SubGroups.Add(new Emojis.Group
            {
                Name = "face-smiling",
                Emojis =
                {
                    new Emojis.Emoji { Text = "😀", Name = "grinning face" },
                    new Emojis.Emoji { Text = "😂", Name = "face with tears of joy" }
                }
            });
            var animals = new Emojis.Group { Name = "Animals & Nature" };
            animals.SubGroups.Add(new Emojis.Group { Name = "animal-mammal", Emojis = { new Emojis.Emoji { Text = "🐶", Name = "dog face" } } });
            var categories = new[] { smileys, animals };

            Assert.Equal(new[] { "😀", "😂", "🐶" }, EmojiPickerViewModel.Find(categories, "face").Select(e => e.Text));
            Assert.Equal(new[] { "😂" }, EmojiPickerViewModel.Find(categories, "JOY face").Select(e => e.Text));
            Assert.Equal(new[] { "🐶" }, EmojiPickerViewModel.Find(categories, "mammal").Select(e => e.Text));
            Assert.Equal(new[] { "🐶" }, EmojiPickerViewModel.Find(categories, "🐶").Select(e => e.Text));
            Assert.Empty(EmojiPickerViewModel.Find(categories, "   "));
        }

        [Fact]
        public void SizeAndPosition_KeepsTheAspectRatioOfAnEmoji_AndCanBeUndone()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var (context, _, _) = CreateHost(surface);
            var emoji = AddEmoji(surface, "😀", 10, 20);
            surface.SelectElement(emoji);
            using var viewModel = new SizeAndPositionViewModel(context);
            Assert.Equal(64, viewModel.Width);

            viewModel.Width = 128;
            Assert.Equal(128, emoji.Width);
            Assert.Equal(128, emoji.Height);

            viewModel.Left = 50;
            Assert.Equal(50, emoji.Left);

            surface.Undo();
            Assert.Equal(10, emoji.Left);
            surface.Undo();
            Assert.Equal(64, emoji.Width);
            Assert.Equal(64, viewModel.Height);
        }

        [Fact]
        public void Context_FollowsTheShownCapture()
        {
            using var first = new Surface(new Bitmap(20, 20));
            using var second = new Surface(new Bitmap(20, 20));
            var context = new EditorPanelContext(null, null, null);
            int changes = 0;
            context.CurrentSurfaceChanged += (_, _) => changes++;

            context.SetSurface(first);
            context.SetSurface(first);
            context.SetSurface(second);

            Assert.Same(second, context.CurrentSurface);
            Assert.Equal(2, changes);
        }
    }
}
