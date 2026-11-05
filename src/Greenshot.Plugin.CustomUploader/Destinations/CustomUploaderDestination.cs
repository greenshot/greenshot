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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.CustomUploader.Sxcu;
using log4net;

namespace Greenshot.Plugin.CustomUploader.Destinations;

/// <summary>
/// "Custom uploader" in the destination picker and the editor, with one sub destination per imported uploader.
/// The uploaders are read each time, an import shows up without a restart.
/// </summary>
internal sealed class CustomUploaderDestination : DestinationBase, IRequiresRecipeAuthorization
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(CustomUploaderDestination));

    public const string DesignationName = "CustomUploader";

    private readonly CustomUploaderPlugin _plugin;

    public CustomUploaderDestination(CustomUploaderPlugin plugin)
    {
        _plugin = plugin;
    }

    public override string Designation => DesignationName;

    public override DestinationDescriptor Descriptor =>
        new DestinationDescriptor(Texts.Get<ICustomUploaderLanguage>().UploadMenuItem, iconKey: CustomUploaderIconProvider.GenericKey, hasDynamicDestinations: true,
            useDynamicsOnly: true, isLinkable: true);

    /// <summary>
    /// Only shown when there is an uploader
    /// </summary>
    public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && CustomUploaderPlugin.GetUploaders().Length > 0;

    public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken) =>
        new ValueTask<IReadOnlyList<IDestination>>(CreateDestinations());

    /// <summary>
    /// A recipe with this destination may send the capture to every uploader
    /// </summary>
    public IEnumerable<RecipeGatedAction> GetGatedActions() => CustomUploaderPlugin.GetUploaders().Select(CustomUploaderPlugin.CreateGatedAction);

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        var destinations = CreateDestinations();
        if (destinations.Count == 1)
        {
            return await destinations[0].ExportAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (destinations.Count == 0 || !request.Ui.IsInteractive)
        {
            // Without the user it's unclear which uploader is meant, a recipe uses the step with the uploader's name
            Log.WarnFormat("Custom uploader: {0} uploaders, none picked", destinations.Count);
            return ExportResult.Declined;
        }

        var picked = await request.Ui.PickDestinationAsync(destinations, request.Metadata, cancellationToken).ConfigureAwait(false);
        return picked == null ? ExportResult.Declined : (await picked.ExportAsync(request, cancellationToken).ConfigureAwait(false)).WithExportedBy(picked);
    }

    private IReadOnlyList<IDestination> CreateDestinations() =>
        CustomUploaderPlugin.GetUploaders().Select(definition => (IDestination)new UploaderDestination(_plugin, definition)).ToList();
}

/// <summary>
/// One imported uploader.
/// </summary>
internal sealed class UploaderDestination : DestinationBase, IRequiresRecipeAuthorization
{
    private readonly CustomUploaderPlugin _plugin;

    public UploaderDestination(CustomUploaderPlugin plugin, UploaderDefinition definition)
    {
        _plugin = plugin;
        Definition = definition;
    }

    public UploaderDefinition Definition { get; }

    public override string Designation => CustomUploaderDestination.DesignationName + ":" + Definition.Name;

    public override DestinationDescriptor Descriptor => new DestinationDescriptor(Definition.Name, iconKey: CustomUploaderIconProvider.KeyFor(Definition.Name), isLinkable: true);

    public IEnumerable<RecipeGatedAction> GetGatedActions()
    {
        yield return CustomUploaderPlugin.CreateGatedAction(Definition);
    }

    public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken) =>
        _plugin.UploadAsync(request.Source, request.Metadata, Definition, request.Ui, cancellationToken);
}
