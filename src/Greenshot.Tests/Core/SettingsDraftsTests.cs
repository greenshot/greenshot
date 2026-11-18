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

using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core;
using Greenshot.Settings.ViewModels;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The settings window edits drafts: nothing reaches the live sections before OK
    /// </summary>
    public class SettingsDraftsTests
    {
        public SettingsDraftsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Get_ReturnsOneDraftPerSection()
        {
            var live = new CoreConfigurationImpl();
            var drafts = new SettingsDrafts();

            var draft = drafts.Get<ICoreConfiguration>(live);

            Assert.NotSame(live, draft);
            Assert.Same(draft, drafts.Get<ICoreConfiguration>(live));
            Assert.Same(draft, drafts.Get(draft));
            Assert.Single(drafts.Drafts);
        }

        [Fact]
        public void ChangedDraft_LeavesLiveSectionAlone_UntilApply()
        {
            var live = new CoreConfigurationImpl { OutputFileJpegQuality = 80, PlayCameraSound = false };
            var drafts = new SettingsDrafts();
            var draft = drafts.Get<ICoreConfiguration>(live);

            draft.OutputFileJpegQuality = 55;
            Assert.Equal(80, live.OutputFileJpegQuality);

            // Another part of Greenshot changes a key the window didn't touch: Apply keeps it
            live.PlayCameraSound = true;
            drafts.Apply();

            Assert.Equal(55, live.OutputFileJpegQuality);
            Assert.True(live.PlayCameraSound);
        }

        [Fact]
        public void Apply_WithoutChange_ReportsNothing()
        {
            var live = new CoreConfigurationImpl { OutputFileJpegQuality = 80, OutputDestinations = new System.Collections.Generic.List<string> { "Editor" } };
            var drafts = new SettingsDrafts();
            var draft = drafts.Get<ICoreConfiguration>(live);

            // OK writes the same values again, e.g. the destinations list: that is no change
            draft.OutputFileJpegQuality = 80;
            draft.OutputDestinations = new System.Collections.Generic.List<string> { "Editor" };

            Assert.False(drafts.Apply());
        }

        [Fact]
        public void Apply_WithChange_ReportsIt()
        {
            var live = new CoreConfigurationImpl { OutputFileJpegQuality = 80 };
            var drafts = new SettingsDrafts();
            drafts.Get<ICoreConfiguration>(live).OutputFileJpegQuality = 55;

            Assert.True(drafts.Apply());
            Assert.Equal(55, live.OutputFileJpegQuality);
        }

        [Fact]
        public void DroppedDrafts_ChangeNothing()
        {
            var live = new CoreConfigurationImpl { OutputFileJpegQuality = 80 };
            var draft = new SettingsDrafts().Get<ICoreConfiguration>(live);

            draft.OutputFileJpegQuality = 55;

            Assert.Equal(80, live.OutputFileJpegQuality);
        }

        [Fact]
        public void SettingsViewModel_EditsDrafts()
        {
            var viewModel = new SettingsViewModel();
            var live = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

            Assert.True(viewModel.CoreConfiguration.IsDraft());
            Assert.Same(live, viewModel.CoreConfiguration.GetDraftSource());
        }
    }
}
