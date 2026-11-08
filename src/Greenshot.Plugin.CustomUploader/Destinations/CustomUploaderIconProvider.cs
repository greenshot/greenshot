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

using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Definitions;
using Greenshot.Plugin.CustomUploader.Sxcu;

namespace Greenshot.Plugin.CustomUploader.Destinations;

/// <summary>
/// The icons of the destinations: the icon of the service, fetched at the import, else the generic upload icon.
/// Key "customuploader:" ("Custom uploader": the icon of the only uploader, else the generic one) or "customuploader:name".
/// </summary>
internal sealed class CustomUploaderIconProvider : DefinitionIconProvider<UploaderDefinition>
{
    public CustomUploaderIconProvider() : base(UploaderKind.Instance)
    {
    }

    /// <summary>The generic upload icon</summary>
    public static string GenericKey => DefinitionIcons.GenericKeyFor(UploaderKind.Instance);

    protected override DefinitionStore<UploaderDefinition> CreateStore() => CustomUploaderPlugin.CreateStore();

    protected override Image CreateGenericIcon() => EmbeddedResources.GetImage(typeof(CustomUploaderPlugin), "Upload");

    /// <summary>
    /// Fetched in the background, shown later: the menus ask again the next time
    /// </summary>
    protected override void OnIconMissing(DefinitionStore<UploaderDefinition> store, StoredDefinition<UploaderDefinition> stored) =>
        ServiceIcon.FetchInBackground(store, stored.Id, stored.Definition);
}
