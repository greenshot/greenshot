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

namespace Greenshot.Plugin.ExternalCommand;

public partial class ExternalCommandConfigurationImpl : IExternalCommandConfiguration
{
    public void OnAfterLoad()
    {
        Commands ??= new List<string>();
        Commandline ??= new Dictionary<string, string>();
        Argument ??= new Dictionary<string, string>();
        CommandlineTemplate ??= new Dictionary<string, string>();
        ArgumentTemplate ??= new Dictionary<string, string>();
        RunInbackground ??= new Dictionary<string, bool>();
        OutputFormat ??= new Dictionary<string, string>();
        RedirectStandardErrorCommand ??= new Dictionary<string, bool>();
        RedirectStandardOutputCommand ??= new Dictionary<string, bool>();
        ShowStandardOutputInLogCommand ??= new Dictionary<string, bool>();
        ParseOutputForUriCommand ??= new Dictionary<string, bool>();
        OutputToClipboardCommand ??= new Dictionary<string, bool>();
        UriToClipboardCommand ??= new Dictionary<string, bool>();
        DeletedBuildInCommands ??= new List<string>();
    }

    public void MarkChanged() => MarkAsDirty();
}
