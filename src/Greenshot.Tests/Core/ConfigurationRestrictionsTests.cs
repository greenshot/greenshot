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


using System.Collections.Generic;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class ConfigurationRestrictionsTests
    {
        public ConfigurationRestrictionsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CoreConfigurationImpl NewConfiguration()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.ExcludePlugins = new List<string>();
            config.IncludePlugins = new List<string>();
            config.ExcludePluginNames = new List<string>();
            config.AllowedPluginNames = new List<string>();
            config.BlockedPluginNames = new List<string>();
            config.ExcludeDestinations = new List<string>();
            config.AllowedDestinations = new List<string>();
            config.BlockedDestinations = new List<string>();
            return config;
        }

        [Theory]
        [InlineData(@"C:\Program Files\Greenshot\Plugins\Greenshot.Plugin.Box\Greenshot.Plugin.Box.dll", "Box")]
        [InlineData("Greenshot.Plugin.ExternalCommand.dll", "ExternalCommand")]
        public void PluginNameFromFile_IsThePartAfterThePrefix(string file, string expected)
        {
            Assert.Equal(expected, ConfigurationRestrictions.PluginNameFromFile(file));
        }

        [Theory]
        [InlineData("Box Plugin", "Box")]
        [InlineData("External command Plugin", "ExternalCommand")]
        [InlineData("PDF Plugin", "Pdf")]
        [InlineData("Zxing", "Zxing")]
        public void PluginNameFromLegacyIdentifier_MapsTheShippedIdentifiers(string identifier, string expected)
        {
            Assert.Equal(expected, ConfigurationRestrictions.PluginNameFromLegacyIdentifier(identifier));
        }

        [Fact]
        public void IsPluginAllowed_WithoutLists_AllowsEverything()
        {
            var config = NewConfiguration();

            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Box"));
        }

        [Fact]
        public void IsPluginAllowed_Excluded_IsBlocked()
        {
            var config = NewConfiguration();
            config.ExcludePluginNames = new List<string> { "box" };

            Assert.False(ConfigurationRestrictions.IsPluginAllowed(config, "Box"));
            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Jira"));
        }

        [Fact]
        public void IsPluginAllowed_UserExclusionAddsToTheAdministrator()
        {
            var config = NewConfiguration();
            config.BlockedPluginNames = new List<string> { "Dropbox" };
            config.ExcludePluginNames = new List<string> { "Jira" };

            Assert.False(ConfigurationRestrictions.IsPluginAllowed(config, "Dropbox"));
            Assert.False(ConfigurationRestrictions.IsPluginAllowed(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsPluginBlockedByAdministrator(config, "Dropbox"));
            Assert.False(ConfigurationRestrictions.IsPluginBlockedByAdministrator(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Box"));
        }

        [Fact]
        public void IsPluginAllowed_Included_BlocksAllOthers()
        {
            var config = NewConfiguration();
            config.AllowedPluginNames = new List<string> { "Office" };

            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Office"));
            Assert.False(ConfigurationRestrictions.IsPluginAllowed(config, "SomePluginInstalledLater"));
        }

        [Fact]
        public void IsPluginAllowed_LegacyIdentifiers_StillApply()
        {
            var config = NewConfiguration();
            config.ExcludePlugins = new List<string> { "Jira Plugin" };

            Assert.False(ConfigurationRestrictions.IsPluginAllowed(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsPluginBlockedByAdministrator(config, "Jira"));
        }

        [Fact]
        public void IsPluginAllowed_EmptyEntry_IsNoIncludeList()
        {
            var config = NewConfiguration();
            config.AllowedPluginNames = new List<string> { " " };

            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Box"));
        }

        [Fact]
        public void IsDestinationAllowed_ExcludeAndInclude()
        {
            var config = NewConfiguration();
            config.ExcludeDestinations = new List<string> { "Jira" };
            Assert.False(ConfigurationRestrictions.IsDestinationAllowed(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsDestinationAllowed(config, "Clipboard"));

            Assert.False(ConfigurationRestrictions.IsDestinationBlockedByAdministrator(config, "Jira"));

            config.BlockedDestinations = new List<string> { "Box" };
            Assert.False(ConfigurationRestrictions.IsDestinationAllowed(config, "Box"));
            Assert.True(ConfigurationRestrictions.IsDestinationBlockedByAdministrator(config, "Box"));

            config.AllowedDestinations = new List<string> { "Clipboard" };
            Assert.True(ConfigurationRestrictions.IsDestinationAllowed(config, "clipboard"));
            Assert.False(ConfigurationRestrictions.IsDestinationAllowed(config, "FileNoDialog"));
            Assert.True(ConfigurationRestrictions.IsDestinationBlockedByAdministrator(config, "FileNoDialog"));
        }

        [Fact]
        public void ReplaceListed_KeepsEntriesWhichAreNotListed()
        {
            var current = new List<string> { "Dropbox", "Box" };

            var result = ConfigurationRestrictions.ReplaceListed(current, new[] { "Box", "Jira" }, new[] { "Jira" });

            Assert.Equal(new[] { "Dropbox", "Jira" }, result);
        }

        [Fact]
        public void OnAfterLoad_MovesLegacyPluginIdentifiersToNames()
        {
            var config = NewConfiguration();
            config.ExcludePlugins = new List<string> { "Box Plugin", "Zxing" };
            config.IncludePlugins = new List<string> { "Office Plugin" };

            config.OnAfterLoad();

            Assert.Equal(new[] { "Box", "Zxing" }, config.ExcludePluginNames);
            Assert.Equal(new[] { "Office" }, config.AllowedPluginNames);
            Assert.Empty(config.ExcludePlugins);
            Assert.Empty(config.IncludePlugins);
        }
        [Fact]
        public void MergedPlugin_BlockedOldName_BlocksOnlyItsDestination()
        {
            var config = NewConfiguration();
            config.BlockedPluginNames = new List<string> { "Jira" };

            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Atlassian"));
            Assert.False(ConfigurationRestrictions.IsDestinationAllowed(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsDestinationAllowed(config, "Confluence"));
        }

        [Fact]
        public void MergedPlugin_BothOldNamesBlocked_BlocksThePlugin()
        {
            var config = NewConfiguration();
            config.ExcludePlugins = new List<string> { "Jira Plugin", "Confluence Plugin" };

            Assert.False(ConfigurationRestrictions.IsPluginAllowed(config, "Atlassian"));
        }

        [Fact]
        public void MergedPlugin_AllowListWithOneOldName_LoadsThePlugin_AllowsOnlyThatDestination()
        {
            var config = NewConfiguration();
            config.AllowedPluginNames = new List<string> { "Box", "Confluence" };

            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Atlassian"));
            Assert.True(ConfigurationRestrictions.IsDestinationAllowed(config, "Confluence"));
            Assert.False(ConfigurationRestrictions.IsDestinationAllowed(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsDestinationAllowed(config, "Box"));
        }

        [Fact]
        public void MergedPlugin_UserExcludedOldName_ExcludesItsDestination()
        {
            var config = NewConfiguration();
            config.ExcludePluginNames = new List<string> { "Jira" };

            Assert.True(ConfigurationRestrictions.IsPluginAllowed(config, "Atlassian"));
            Assert.False(ConfigurationRestrictions.IsDestinationAllowed(config, "Jira"));
            Assert.True(ConfigurationRestrictions.IsDestinationAllowed(config, "Confluence"));
        }
    }
}
