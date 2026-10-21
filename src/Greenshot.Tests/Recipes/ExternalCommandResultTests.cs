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
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.ExternalCommand;
using Greenshot.Plugin.ExternalCommand.Destinations;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The result of an external command which runs in the background: what fails before the command runs is the result of the export
    /// </summary>
    public class ExternalCommandResultTests
    {
        public ExternalCommandResultTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task Export_InTheBackground_FileNameItDoesntAccept_Fails()
        {
            var config = IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
            config.Commands ??= new List<string>();
            config.Commandline ??= new Dictionary<string, string>();
            config.Argument ??= new Dictionary<string, string>();
            config.RunInbackground ??= new Dictionary<string, bool>();
            string command = $"Test_{Guid.NewGuid():N}";
            config.Commands.Add(command);
            // Never started: the file name is refused before
            config.Commandline[command] = "greenshot-test-command-which-does-not-exist.exe";
            config.Argument[command] = "\"{0}\"";
            config.RunInbackground[command] = true;
            try
            {
                var capture = new Capture(new Bitmap(8, 8));
                // A shell metacharacter, which the external command doesn't accept
                capture.CaptureDetails.Filename = @"C:\Temp\Greenshot & co.png";
                var payload = new CapturePayload(capture);
                payload.EnsureSurface();
                var source = await payload.GetExportSourceAsync(InlineUiDispatcher.Instance, CancellationToken.None);

                var result = await DestinationExporter.ExportAsync(new ExternalCommandDestination(command), source, capture.CaptureDetails, false, UserInteraction.Current, CancellationToken.None);

                Assert.Equal(ExportStatus.Failed, result.Status);
                Assert.Contains("dangerous", result.Error);
            }
            finally
            {
                config.Commands.Remove(command);
                config.Commandline.Remove(command);
                config.Argument.Remove(command);
                config.RunInbackground.Remove(command);
            }
        }
    }
}
