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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Greenshot.sln must list every configuration for every platform and project, otherwise Visual Studio fills in the missing
    /// ones itself (rewriting the file) and builds everything for them, also in "Debug Light".
    /// </summary>
    public class SolutionConfigurationTests
    {
        private static readonly string[] Configurations = { "Debug", "Debug Light", "Release", "Release Light" };
        private static readonly string[] Platforms = { "Any CPU", "x64", "x86" };
        private static readonly string[] LightProjects = { "Greenshot", "Greenshot.Base", "Greenshot.Editor", "Greenshot.BuildTasks" };

        private static string ReadSolution()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Greenshot.sln")))
            {
                directory = directory.Parent;
            }
            Assert.NotNull(directory);
            return File.ReadAllText(Path.Combine(directory.FullName, "Greenshot.sln"));
        }

        [Fact]
        public void Solution_HasEveryConfigurationAndPlatform_ForEveryProject()
        {
            string solution = ReadSolution();
            var combinations = Configurations.SelectMany(c => Platforms.Select(p => $"{c}|{p}")).ToList();

            foreach (string combination in combinations)
            {
                Assert.Contains($"\t\t{combination} = {combination}", solution);
            }

            var projects = Regex.Matches(solution, @"^Project\(""\{(?!2150E333)[^}]+\}""\) = ""([^""]+)"", ""[^""]+"", ""(\{[^}]+\})""", RegexOptions.Multiline)
                .Cast<Match>()
                .ToDictionary(m => m.Groups[2].Value, m => m.Groups[1].Value);
            Assert.NotEmpty(projects);
            foreach (var project in projects)
            {
                foreach (string combination in combinations)
                {
                    Assert.True(solution.Contains($"{project.Key}.{combination}.ActiveCfg = "), $"{project.Value} has no {combination}");
                }
            }
        }

        [Fact]
        public void Solution_LightConfigurations_OnlyBuildGreenshotLight()
        {
            string solution = ReadSolution();
            var projects = Regex.Matches(solution, @"^Project\(""\{[^}]+\}""\) = ""([^""]+)"", ""[^""]+"", ""(\{[^}]+\})""", RegexOptions.Multiline)
                .Cast<Match>()
                .ToDictionary(m => m.Groups[2].Value, m => m.Groups[1].Value);

            foreach (string configuration in new[] { "Debug Light", "Release Light" })
            {
                foreach (string platform in Platforms)
                {
                    var built = new List<string>();
                    foreach (Match build in Regex.Matches(solution, $@"(\{{[^}}]+\}})\.{Regex.Escape(configuration + "|" + platform)}\.Build\.0 = (.+)$", RegexOptions.Multiline))
                    {
                        string name = projects[build.Groups[1].Value];
                        built.Add(name);
                        if (name == "Greenshot")
                        {
                            Assert.Equal(configuration.Replace(" ", string.Empty) + "|Any CPU", build.Groups[2].Value.Trim());
                        }
                    }
                    Assert.Equal(LightProjects.OrderBy(n => n), built.OrderBy(n => n));
                }
            }
        }
    }
}
