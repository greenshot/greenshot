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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Imgur
{
    public class ImgurDestination : DestinationBase, IRequiresRecipeAuthorization
    {
        /// <summary>
        /// The icons in the resources of the plugin
        /// </summary>
        public static ResourceIconProvider Icons { get; } = new ResourceIconProvider("imgur", typeof(ImgurPlugin));

        public override string Designation => "Imgur";

        /// <summary>
        /// Uploads the capture: the user has to allow network access when approving a recipe with this destination
        /// </summary>
        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Imgur (imgur.com)");
        }

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(Texts.Get<IImgurLanguage>().UploadMenuItem ?? "Upload to Imgur", iconKey: Icons.KeyFor("Imgur"));

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var outputSettings = new SurfaceOutputSettings(WellKnownFileFormats.Png, 90, false);
            var image = await request.Source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
            var info = await request.Ui.RunWithProgressAsync(Texts.Get<IImgurLanguage>().CommunicationWait,
                (progress, token) => ImgurStep.UploadToImgurAsync(image, request.Metadata?.Title, null, token), cancellationToken).ConfigureAwait(false);
            if (info == null || string.IsNullOrEmpty(info.Original))
            {
                return ExportResult.Failed("Imgur upload failed");
            }

            return ExportResult.Succeeded(uri: new Uri(info.Original));
        }
    }
}
