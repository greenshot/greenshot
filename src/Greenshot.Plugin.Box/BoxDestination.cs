/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.Box;

public class BoxDestination : DestinationBase
{
    /// <summary>
    /// The icons in the resources of the plugin
    /// </summary>
    public static ResourceIconProvider Icons { get; } = new ResourceIconProvider("box", typeof(BoxPlugin));

    private readonly BoxPlugin _plugin;

    public BoxDestination(BoxPlugin plugin)
    {
        _plugin = plugin;
    }

    public override string Designation => "Box";

    public override DestinationDescriptor Descriptor => new DestinationDescriptor(Language.GetString("box", LangKey.upload_menu_item), iconKey: Icons.KeyFor("Box"));

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        string uploadUrl = await _plugin.UploadAsync(request.Source, request.Metadata, request.Ui, cancellationToken).ConfigureAwait(false);
        return uploadUrl == null ? ExportResult.Declined : ExportResult.Succeeded(uri: new Uri(uploadUrl));
    }
}
