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

#if !GREENSHOT_LIGHT
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;

namespace Greenshot.Destinations.ImageSearch
{
    /// <summary>
    /// Searches the web for the capture: the capture goes to a search engine and the results open in the default browser.
    /// Beta: the engines have no official API for this, see <see cref="ImageSearchEngine"/>.
    /// The capture leaves the computer, only when the user picks this destination; not in the Light edition (no network).
    /// </summary>
    public class ImageSearchDestination : DestinationBase
    {
        private readonly ImageSearchEngine _engine;

        /// <summary>
        /// The destination in the menus, it uses the first engine and offers all engines as sub destinations
        /// </summary>
        public ImageSearchDestination()
        {
        }

        private ImageSearchDestination(ImageSearchEngine engine)
        {
            _engine = engine;
        }

        public override string Designation => "ImageSearch";

        public override DestinationDescriptor Descriptor => _engine == null
            ? new DestinationDescriptor(Texts.Settings.DestinationImageSearch, 5, DestinationIcons.Stock(StockIconId.Find), hasDynamicDestinations: true)
            : new DestinationDescriptor(_engine.Name, 5, DestinationIcons.Stock(StockIconId.Find));

        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && CoreConfiguration?.IsBetaTester == true;

        public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            if (_engine != null)
            {
                return base.GetDynamicDestinationsAsync(metadata, cancellationToken);
            }

            IReadOnlyList<IDestination> engines = ImageSearchEngine.All.Select(engine => (IDestination)new ImageSearchDestination(engine)).ToList();
            return new ValueTask<IReadOnlyList<IDestination>>(engines);
        }

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (!request.Ui.IsInteractive)
            {
                // The results are a page in the browser, nobody would look at it
                return ExportResult.Declined;
            }

            var engine = _engine ?? ImageSearchEngine.All[0];
            return await engine.SearchAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
#endif
