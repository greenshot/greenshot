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

using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.Recipes;
using Greenshot.Plugin.SmartText.Views;

namespace Greenshot.Plugin.SmartText.Destinations;

/// <summary>
/// The Smart text window as a destination, next to the OCR destination: the found items of the capture to copy or open
/// </summary>
public sealed class SmartTextDestination : DestinationBase
{
    public const string DesignationName = "SmartText";

    public override string Designation => DesignationName;

    public override DestinationDescriptor Descriptor => new DestinationDescriptor(SmartTextPlugin.Language.WindowTitle, 3, DestinationIcons.Stock(StockIconId.Find));

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        if (request.Ui?.IsInteractive != true)
        {
            // A window needs somebody to look at it
            return ExportResult.Declined;
        }

        // The capture as exported (with the annotations), the found boxes are in its pixels
        using var lease = await request.Source.RenderAsync(new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false) { DisableReduceColors = true }, cancellationToken)
            .ConfigureAwait(false);
        var image = lease?.Image;
        if (image == null) return ExportResult.Failed(SmartTextPlugin.Language.NoText);

        var lines = await OcrLines.GetAsync(request.Metadata, image, null, request.Ui, cancellationToken).ConfigureAwait(false);
        var layout = TextLayout.From(lines, new NativeRect(0, 0, image.Width, image.Height));
        var items = SmartTextScanner.Scan(layout, SmartTextDefinitions.Detectors(null, null, out _));
        SmartTextStep.Remember(request.Metadata, items);

        var ui = SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
        // The window makes its own copy of the image before the lease is disposed
        await ui.InvokeAsync(() => SmartTextWindow.ShowToCopy(image, layout, items), cancellationToken).ConfigureAwait(false);
        return ExportResult.Succeeded(clearsModified: false);
    }
}
