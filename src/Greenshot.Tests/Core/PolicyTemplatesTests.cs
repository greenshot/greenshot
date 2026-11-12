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
using System.IO;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core;
using Greenshot.Editor.Configuration;
using Greenshot.Policies;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The Windows policy templates and the .reg export, see docs/vNext/policies.md
    /// </summary>
    public class PolicyTemplatesTests
    {
        public PolicyTemplatesTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static string WriteTemplates()
        {
            var directory = Path.Combine(Path.GetTempPath(), "greenshot-policy-templates-" + Guid.NewGuid().ToString("N"));
            var sections = new IIniSection[] { new CoreConfigurationImpl(), new EditorConfigurationImpl() };
            int count = PolicyTemplates.Write(directory, sections);
            Assert.True(count > 0);
            return directory;
        }

        [Fact]
        public void Templates_HaveTheSettings_AndTheCommonCategory()
        {
            var directory = WriteTemplates();
            try
            {
                var admx = File.ReadAllText(Path.Combine(directory, "greenshot.admx"));
                var adml = File.ReadAllText(Path.Combine(directory, "en-US", "greenshot.adml"));

                Assert.Contains(@"SOFTWARE\Policies\Greenshot\Greenshot\Core", admx);
                Assert.Contains(@"SOFTWARE\Policies\Greenshot\Greenshot\Recommended\Core", admx);
                Assert.Contains("\"OutputFileFormat\"", admx);
                Assert.Contains("\"BlockedDestinations\"", admx);
                Assert.Contains(PolicyCategories.Common, adml);
                Assert.Contains("Greenshot 1.4 or later", adml);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Theory]
        [InlineData("LastSaveWithVersion")]
        [InlineData("OutputFileIncrementingNumber")]
        [InlineData("PluginVerifiedHashes")]
        [InlineData("RecentColors")]
        [InlineData("WindowNormalPosition")]
        public void Templates_LeaveOut_WhatGreenshotKeepsForItself(string key)
        {
            var directory = WriteTemplates();
            try
            {
                var admx = File.ReadAllText(Path.Combine(directory, "greenshot.admx"));
                Assert.DoesNotContain("\"" + key + "\"", admx);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Greenshot.exe --policy-templates: the sections are found in the assemblies next to it, the plugins included
        /// (in the test output the plugin dlls are next to Greenshot.exe)
        /// </summary>
        [Fact]
        public void WriteForInstallation_FindsTheSectionsOfGreenshotAndThePlugins()
        {
            var directory = Path.Combine(Path.GetTempPath(), "greenshot-policy-templates-" + Guid.NewGuid().ToString("N"));
            try
            {
                Assert.True(PolicyTemplates.WriteForInstallation(directory));
                var admx = File.ReadAllText(Path.Combine(directory, "greenshot.admx"));
                Assert.Contains(@"Greenshot\Greenshot\Core", admx);
                Assert.Contains(@"Greenshot\Greenshot\Editor", admx);
                Assert.Contains(@"Greenshot\Greenshot\Jira", admx);
                Assert.Contains(@"Greenshot\Greenshot\Box", admx);
                // Sign-in tokens are never a policy
                Assert.DoesNotContain("\"RefreshToken\"", admx);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact]
        public void RegFile_HasTheFixedIniLines_AsMandatoryMachinePolicies()
        {
            var reg = PolicyTemplates.CreateRegFile("[Core]\r\nBlockedDestinations=Printer,Box\r\nAskForUnsignedPlugins=False\r\n");

            Assert.StartsWith("Windows Registry Editor Version 5.00", reg.TrimStart('﻿'));
            Assert.Contains(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Greenshot\Greenshot\Core]", reg);
            Assert.Contains("\"AskForUnsignedPlugins\"=dword:00000000", reg);
            Assert.Contains(@"Core\BlockedDestinations]", reg);
            Assert.Contains("\"1\"=\"Printer\"", reg);
            Assert.Contains("\"2\"=\"Box\"", reg);
            // Only the given keys
            Assert.DoesNotContain("OutputFileFormat", reg);
        }
    }
}
