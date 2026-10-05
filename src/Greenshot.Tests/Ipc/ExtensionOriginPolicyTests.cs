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
using System.IO;
using Greenshot.Ipc.BrowserExtension;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    public class ExtensionOriginPolicyTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "greenshot_origin_" + Guid.NewGuid().ToString("N"));

        public ExtensionOriginPolicyTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch (IOException)
            {
            }
        }

        [Theory]
        [InlineData("chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik/")]
        [InlineData("chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik")]
        [InlineData("CHROME-EXTENSION://KNLDJMFMOPNPOLAHPMMGBAGDOHDNHKIK/")]
        [InlineData("greenshot@getgreenshot.org")]
        public void OfficialExtensions_AreAllowedWithoutManifests(string origin)
        {
            Assert.True(new ExtensionOriginPolicy(_directory).IsAllowed(origin));
            Assert.True(new ExtensionOriginPolicy(null).IsAllowed(origin));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/")]
        [InlineData("moz-extension://0b1c2d3e-0000-0000-0000-000000000000/")]
        [InlineData("other@example.com")]
        [InlineData("chrome-extension://*/")]
        public void UnknownOrigins_AreRejected(string origin)
        {
            Assert.False(new ExtensionOriginPolicy(_directory).IsAllowed(origin));
        }

        [Fact]
        public void Manifests_AddDevelopmentExtensions_AndChangesArePickedUp()
        {
            var policy = new ExtensionOriginPolicy(_directory);
            const string devOrigin = "chrome-extension://bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb/";
            Assert.False(policy.IsAllowed(devOrigin));

            File.WriteAllText(Path.Combine(_directory, ExtensionOriginPolicy.ChromeManifestFileName),
                "{ \"name\": \"org.greenshot.proxy\", \"allowed_origins\": [ \"" + devOrigin + "\" ] }");
            File.WriteAllText(Path.Combine(_directory, ExtensionOriginPolicy.FirefoxManifestFileName),
                "{ \"name\": \"org.greenshot.proxy\", \"allowed_extensions\": [ \"dev@example.org\" ] }");

            Assert.True(policy.IsAllowed(devOrigin));
            Assert.True(policy.IsAllowed("dev@example.org"));
            Assert.False(policy.IsAllowed("DEV@example.org"));
        }

        [Fact]
        public void BrokenManifest_OnlyAllowsOfficialExtensions()
        {
            File.WriteAllText(Path.Combine(_directory, ExtensionOriginPolicy.ChromeManifestFileName), "{ not json");
            var policy = new ExtensionOriginPolicy(_directory);

            Assert.True(policy.IsAllowed("chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik/"));
            Assert.False(policy.IsAllowed("chrome-extension://bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb/"));
        }
    }
}
