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
using Greenshot.Base.Capturing;
using Greenshot.Capturing;
using Greenshot.Capturing.Views;
using Xunit;

namespace Greenshot.Tests.Capturing
{
    public class CapturePrewarmTests
    {
        /// <summary>
        /// The prewarm selects types by namespace, a renamed namespace silently stops the prewarm (it did after the restructuring)
        /// </summary>
        [Fact]
        public void EveryPrewarmNamespaceHasTypes()
        {
            var namespaces = typeof(CaptureWindow).Assembly.GetTypes()
                .Concat(typeof(ScreenCapture).Assembly.GetTypes())
                .Select(type => type.Namespace)
                .Distinct()
                .ToList();

            foreach (var captureNamespace in CapturePrewarm.CaptureNamespaces)
            {
                Assert.Contains(captureNamespace, namespaces);
            }
        }
    }
}
