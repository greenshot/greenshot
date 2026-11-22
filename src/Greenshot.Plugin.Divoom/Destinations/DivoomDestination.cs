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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.Divoom.Device;

namespace Greenshot.Plugin.Divoom.Destinations;

/// <summary>
/// Shows the capture on the Divoom device of the settings: it is made as large as fits on its screen (the capture of
/// "Capture for Divoom" has that size already)
/// </summary>
public sealed class DivoomDestination : DestinationBase, IRequiresRecipeAuthorization
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DivoomDestination));

    public const string DesignationName = "Divoom";

    public override string Designation => DesignationName;

    public override DestinationDescriptor Descriptor => new DestinationDescriptor(Texts.Get<IDivoomLanguage>().Destination, iconKey: DestinationIcons.Stock(StockIconId.DesktopPc));

    /// <summary>
    /// Sends the capture into the network: the user allows it when approving a recipe with this destination
    /// </summary>
    public IEnumerable<RecipeGatedAction> GetGatedActions()
    {
        yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Divoom device");
    }

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        var language = Texts.Get<IDivoomLanguage>();
        var config = DivoomPlugin.Configuration;
        string address = config?.DeviceAddress?.Trim();
        var commandUri = DivoomClient.GetCommandUri(address);
        if (commandUri == null)
        {
            string message = string.IsNullOrEmpty(address) ? language.NoAddress : string.Format(language.InvalidAddress, address);
            Log.Warn(string.IsNullOrEmpty(address) ? "No Divoom device is set in the settings." : "The address of the Divoom device in the settings isn't one of a device.");
            return ExportResult.Failed(message);
        }

        int size = DivoomImage.NormalizeSize(config.PanelSize);
        byte[] rgb;
        using (var lease = await request.Source.RenderAsync(new SurfaceOutputSettings(), cancellationToken).ConfigureAwait(false))
        {
            rgb = DivoomImage.ToRgb(lease.Image, size);
        }

        try
        {
            // A moment in the network, no progress window
            await DivoomClient.SendPictureAsync(commandUri, size, rgb, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is DivoomException or HttpRequestException or TimeoutException)
        {
            Log.Warn($"Sending the capture to the Divoom device at {commandUri.Host} failed", ex);
            return ExportResult.Failed(string.Format(language.SendFailed, commandUri.Host, DivoomClient.Describe(ex)), ex);
        }

        string target = string.IsNullOrWhiteSpace(config.DeviceName) ? commandUri.Host : config.DeviceName.Trim();
        return ExportResult.Succeeded(target: target, clearsModified: false);
    }
}
