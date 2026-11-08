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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Base.Definitions;

/// <summary>
/// The destination of a kind of definitions (e.g. "Custom uploader") in the destination picker and the editor, with one sub destination
/// per definition. The definitions are read each time, an import shows up without a restart.
/// </summary>
/// <typeparam name="T">the definition</typeparam>
public abstract class DefinitionDestination<T> : DestinationBase, IRequiresRecipeAuthorization where T : class
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(DefinitionDestination<T>));

    protected DefinitionDestination(IDefinitionKind<T> kind)
    {
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
    }

    protected IDefinitionKind<T> Kind { get; }

    public override string Designation => Kind.Designation;

    /// <summary>
    /// The definitions which can be used, sorted by name
    /// </summary>
    protected abstract IReadOnlyList<T> GetDefinitions();

    /// <summary>
    /// The destination of one definition
    /// </summary>
    protected abstract DefinitionItemDestination<T> CreateDestination(T definition);

    /// <summary>
    /// Only shown when there is a definition
    /// </summary>
    public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && GetDefinitions().Count > 0;

    public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken) =>
        new ValueTask<IReadOnlyList<IDestination>>(CreateDestinations());

    /// <summary>
    /// A recipe with this destination may use every definition
    /// </summary>
    public IEnumerable<RecipeGatedAction> GetGatedActions() => GetDefinitions().Select(Kind.CreateGatedAction);

    /// <summary>
    /// With one definition that one, with more the user picks one. Without the user it's unclear which one is meant:
    /// declined, a recipe names the definition in its step.
    /// </summary>
    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var destinations = CreateDestinations();
        if (destinations.Count == 1)
        {
            return await destinations[0].ExportAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (destinations.Count == 0 || !request.Ui.IsInteractive)
        {
            Log.WarnFormat("{0}: {1} definitions, none picked", Designation, destinations.Count);
            return ExportResult.Declined;
        }

        var picked = await request.Ui.PickDestinationAsync(destinations, request.Metadata, cancellationToken).ConfigureAwait(false);
        return picked == null ? ExportResult.Declined : (await picked.ExportAsync(request, cancellationToken).ConfigureAwait(false)).WithExportedBy(picked);
    }

    private IReadOnlyList<IDestination> CreateDestinations() => GetDefinitions().Select(definition => (IDestination)CreateDestination(definition)).ToList();
}

/// <summary>
/// The destination of one definition, "Designation:name", with the icon of the definition and its approval.
/// </summary>
/// <typeparam name="T">the definition</typeparam>
public abstract class DefinitionItemDestination<T> : DestinationBase, IRequiresRecipeAuthorization where T : class
{
    protected DefinitionItemDestination(IDefinitionKind<T> kind, T definition)
    {
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected IDefinitionKind<T> Kind { get; }

    public T Definition { get; }

    public override string Designation => DesignationOf(Kind, Kind.NameOf(Definition));

    public override DestinationDescriptor Descriptor =>
        new DestinationDescriptor(Kind.NameOf(Definition), iconKey: DefinitionIcons.KeyFor(Kind, Kind.NameOf(Definition)), isLinkable: true);

    public IEnumerable<RecipeGatedAction> GetGatedActions()
    {
        yield return Kind.CreateGatedAction(Definition);
    }

    /// <summary>
    /// The designation of the destination of a definition: "Designation:name"
    /// </summary>
    public static string DesignationOf(IDefinitionKind<T> kind, string name) => kind.Designation + ":" + name;
}
