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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Definitions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Plugin.CustomUploader.Sxcu;

namespace Greenshot.Plugin.CustomUploader.Destinations;

/// <summary>
/// "Custom uploader" in the destination picker and the editor, with one sub destination per imported uploader.
/// </summary>
internal sealed class CustomUploaderDestination : DefinitionDestination<UploaderDefinition>
{
    public const string DesignationName = "CustomUploader";

    private readonly CustomUploaderPlugin _plugin;

    public CustomUploaderDestination(CustomUploaderPlugin plugin) : base(UploaderKind.Instance)
    {
        _plugin = plugin;
    }

    public override DestinationDescriptor Descriptor =>
        new DestinationDescriptor(Texts.Get<ICustomUploaderLanguage>().UploadMenuItem, iconKey: CustomUploaderIconProvider.GenericKey, hasDynamicDestinations: true,
            useDynamicsOnly: true, isLinkable: true);

    protected override IReadOnlyList<UploaderDefinition> GetDefinitions() => CustomUploaderPlugin.GetUploaders();

    protected override DefinitionItemDestination<UploaderDefinition> CreateDestination(UploaderDefinition definition) => new UploaderDestination(_plugin, definition);
}

/// <summary>
/// One imported uploader.
/// </summary>
internal sealed class UploaderDestination : DefinitionItemDestination<UploaderDefinition>
{
    private readonly CustomUploaderPlugin _plugin;

    public UploaderDestination(CustomUploaderPlugin plugin, UploaderDefinition definition) : base(UploaderKind.Instance, definition)
    {
        _plugin = plugin;
    }

    public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken) =>
        _plugin.UploadAsync(request.Source, request.Metadata, Definition, request.Ui, cancellationToken);
}
