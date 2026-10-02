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
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Editor.Forms;
using Greenshot.Forms;
using Greenshot.Plugin.Box;
using Greenshot.Plugin.Dropbox;
using Greenshot.Plugin.Imgur;
using Greenshot.Plugin.Jira;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The images and icons embedded as plain files (see EmbeddedResources), for every type which has them
    /// </summary>
    public class EmbeddedResourcesTests
    {
        public static IEnumerable<object[]> Owners => new[]
        {
            new object[] { typeof(ImageEditorForm) },
            new object[] { typeof(MainForm) },
            new object[] { typeof(GreenshotResources) },
            new object[] { typeof(BoxPlugin) },
            new object[] { typeof(DropboxPlugin) },
            new object[] { typeof(ImgurPlugin) },
            new object[] { typeof(JiraPlugin) },
        };

        [Theory]
        [MemberData(nameof(Owners))]
        public void GetNames_OnlyReturnsTheEmbeddedFiles_AndEveryOneLoads(Type owner)
        {
            var names = EmbeddedResources.GetNames(owner).ToList();

            Assert.NotEmpty(names);
            // Not the compiled .resx of the type, e.g. Greenshot.Editor.Forms.ImageEditorForm.resources, which is no image
            Assert.DoesNotContain(names, name => name.EndsWith("resources", StringComparison.Ordinal));
            foreach (string name in names)
            {
                var bytes = EmbeddedResources.GetBytes(owner, name);
                Assert.NotNull(bytes);
                bool isIcon = bytes.Length > 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0;
                if (isIcon)
                {
                    using var icon = EmbeddedResources.GetIcon(owner, name);
                    Assert.NotNull(icon);
                }
                else
                {
                    using var image = EmbeddedResources.GetImage(owner, name);
                    Assert.NotNull(image);
                    Assert.True(image.Width > 0 && image.Height > 0, $"{owner.Name}.{name} is empty");
                }
            }
        }
    }
}
