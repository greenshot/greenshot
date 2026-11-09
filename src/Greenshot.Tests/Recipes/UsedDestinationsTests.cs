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
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.ExternalCommand.Recipes;
using Greenshot.Plugin.Office.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// A destination which is switched off is not used in a recipe, also not by a step of its own
    /// </summary>
    public class UsedDestinationsTests
    {
        public UsedDestinationsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CoreConfigurationImpl Configuration(params string[] switchedOff)
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.ExcludePluginNames = new List<string>();
            config.AllowedPluginNames = new List<string>();
            config.BlockedPluginNames = new List<string>();
            config.ExcludePlugins = new List<string>();
            config.IncludePlugins = new List<string>();
            config.AllowedDestinations = new List<string>();
            config.BlockedDestinations = new List<string>();
            config.ExcludeDestinations = new List<string>(switchedOff);
            return config;
        }

        private static RecipeNodeConfig Node(string stepType, string parameter, string value) =>
            new RecipeNodeConfig { Id = "node", StepType = stepType, Parameters = new Dictionary<string, object> { [parameter] = value } };

        [Fact]
        public void ExternalCommandStep_CommandOfTheSettingsSwitchedOff_IsNotAllowed()
        {
            var step = new ExternalCommandStep(Node("ExternalCommand", "Command", "MS Paint"));

            Assert.Equal("External MS Paint", UsedDestinations.FirstNotAllowed(step, Configuration("External MS Paint")));
            Assert.Null(UsedDestinations.FirstNotAllowed(step, Configuration("External Other")));
        }

        [Fact]
        public void ExternalCommandStep_CommandOfTheRecipe_UsesNoDestination()
        {
            var step = new ExternalCommandStep(Node("ExternalCommand", "Program", "tool.exe"));

            Assert.Empty(step.UsedDestinations);
            Assert.Null(UsedDestinations.FirstNotAllowed(step, Configuration("External tool.exe")));
        }

        [Fact]
        public void OfficeStep_ApplicationSwitchedOff_IsNotAllowed_IgnoringCase()
        {
            var step = new OfficeStep(Node("Office", "Application", "PowerPoint"));

            Assert.Equal("PowerPoint", UsedDestinations.FirstNotAllowed(step, Configuration("Powerpoint")));
            Assert.Null(UsedDestinations.FirstNotAllowed(step, Configuration("Word")));
        }

        [Fact]
        public void StepWithoutDestinations_IsAllowed()
        {
            Assert.Null(UsedDestinations.FirstNotAllowed(new object(), Configuration("Box")));
        }
    }
}
