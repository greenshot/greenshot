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

using System.Linq;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Covers the one-time migration of the legacy RegionHotkey/WindowHotkey/FullscreenHotkey/LastregionHotkey/
    /// ClipboardHotkey settings into the new, freely configurable HotkeyShortcutsJson list
    /// (see CoreConfigurationImpl.OnAfterLoad / MigrateLegacyHotkeysToShortcuts).
    /// </summary>
    public class HotkeyShortcutsMigrationTests
    {
        [Fact]
        public void OnAfterLoad_FreshDefaults_MigratesTheFourNonEmptyLegacyHotkeys()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            // Sanity: migration hasn't happened yet
            Assert.False(config.HotkeyShortcutsMigrated);

            config.OnAfterLoad();

            Assert.True(config.HotkeyShortcutsMigrated);

            // The legacy fields are cleared so they are never read (or migrated) again
            Assert.True(string.IsNullOrEmpty(config.RegionHotkey));
            Assert.True(string.IsNullOrEmpty(config.WindowHotkey));
            Assert.True(string.IsNullOrEmpty(config.FullscreenHotkey));
            Assert.True(string.IsNullOrEmpty(config.LastregionHotkey));
            Assert.True(string.IsNullOrEmpty(config.ClipboardHotkey));

            var shortcuts = HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);

            // ClipboardHotkey has no [DefaultValue], so only the other 4 defaults are migrated
            Assert.Equal(4, shortcuts.Count);
            Assert.Contains(shortcuts, s => s.Hotkey == "PrintScreen" && s.CaptureFunctionId == HotkeyCaptureFunctions.Region);
            Assert.Contains(shortcuts, s => s.Hotkey == "Alt + PrintScreen" && s.CaptureFunctionId == HotkeyCaptureFunctions.Window);
            Assert.Contains(shortcuts, s => s.Hotkey == "Ctrl + PrintScreen" && s.CaptureFunctionId == HotkeyCaptureFunctions.Fullscreen);
            Assert.Contains(shortcuts, s => s.Hotkey == "Shift + PrintScreen" && s.CaptureFunctionId == HotkeyCaptureFunctions.LastRegion);
            // The default OutputDestinations ("Picker") becomes each migrated entry's destinations
            Assert.All(shortcuts, s => Assert.Equal(new System.Collections.Generic.List<string> { "Picker" }, s.Destinations));
            // Stable, non-empty, unique technical ids
            Assert.All(shortcuts, s => Assert.False(string.IsNullOrEmpty(s.Id)));
            Assert.Equal(shortcuts.Select(s => s.Id).Distinct().Count(), shortcuts.Count);
        }

        [Fact]
        public void OnAfterLoad_CalledTwice_IsIdempotent_DoesNotDuplicateEntries()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            config.OnAfterLoad();
            var firstRun = HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);

            // Simulate the application loading the (now already migrated) ini a second time
            config.OnAfterLoad();
            var secondRun = HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);

            Assert.Equal(firstRun.Count, secondRun.Count);
            Assert.Equal(firstRun.Select(s => s.Id).OrderBy(id => id), secondRun.Select(s => s.Id).OrderBy(id => id));
        }

        [Fact]
        public void OnAfterLoad_NoLegacyHotkeysSet_MigratesNothingButStillSetsTheFlag()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            // Simulate an ini file that already explicitly cleared every legacy hotkey (e.g. hand-edited)
            config.RegionHotkey = null;
            config.WindowHotkey = null;
            config.FullscreenHotkey = null;
            config.LastregionHotkey = null;
            config.ClipboardHotkey = null;

            config.OnAfterLoad();

            Assert.True(config.HotkeyShortcutsMigrated);
            var shortcuts = HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);
            Assert.Empty(shortcuts);
        }

        [Fact]
        public void OnAfterLoad_PreExistingCustomShortcuts_AreKeptAlongsideMigratedOnes()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            // Simulate a user who (somehow) already has one custom shortcut configured before the migration runs
            var preExisting = HotkeyShortcutConfig.Create("Ctrl + Alt + Q", HotkeyCaptureFunctions.Clipboard, new[] { "Editor" });
            config.SetRawValue(nameof(config.HotkeyShortcutsJson), HotkeyShortcutConfig.ToJson(new System.Collections.Generic.List<HotkeyShortcutConfig> { preExisting }));

            config.OnAfterLoad();

            var shortcuts = HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);
            // 1 pre-existing + 4 migrated from the still-default legacy fields
            Assert.Equal(5, shortcuts.Count);
            Assert.Contains(shortcuts, s => s.Id == preExisting.Id && s.Hotkey == "Ctrl + Alt + Q");
        }

        [Fact]
        public void OnAfterLoad_CorruptHotkeyShortcutsJson_DoesNotPreventStartup()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.SetRawValue(nameof(config.HotkeyShortcutsJson), "{this is not json");

            // Must not throw: a broken configuration value may never block the application from starting
            config.OnAfterLoad();

            Assert.True(config.HotkeyShortcutsMigrated);
            var shortcuts = HotkeyShortcutConfig.ParseList(config.HotkeyShortcutsJson);
            // The corrupt pre-existing value is discarded, but the legacy defaults still migrate cleanly
            Assert.Equal(4, shortcuts.Count);
        }
    }
}
