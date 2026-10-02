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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Plugin.Imgur
{
    /// <summary>
    /// Capture recipe step that uploads the current capture surface to Imgur.
    /// </summary>
    [StepInfo("Imgur", "Upload to Imgur", "Uploads the capture to Imgur.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Format", ContractDataType.Enum, Description = "Image format of the upload", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100) when uploading as JPEG")]
    [StepParameter("Title", ContractDataType.String, Description = "Title of the image (default: the capture title)")]
    [StepParameter("Description", ContractDataType.String, Description = "Description of the image")]
    [StepParameter("CopyLinkToClipboard", ContractDataType.Boolean, DefaultValue = true, Description = "Copy the link to the clipboard")]
    [StepOutputVariable("Imgur.UploadUrl", ContractDataType.String, "Link to the uploaded image", Conditional = true)]
    [StepOutputVariable("Imgur.Hash", ContractDataType.String, "Imgur hash of the image", Conditional = true)]
    [StepOutputVariable("Imgur.DeleteHash", ContractDataType.String, "Hash to delete the image", Conditional = true)]
    public class ImgurStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImgurStep));
        private static IImgurConfiguration Config => IniConfigRegistry.GetSection<IImgurConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        /// <summary>
        /// Uploads the capture: the user has to allow network access when approving a recipe with this step
        /// </summary>
        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Imgur (imgur.com)");
        }

        public ImgurStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ImgurUploadStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Payload?.EnsureSurface() == null)
            {
                context.LogStep("ImgurStep: No surface available to upload.");
                Log.Warn("ImgurStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            string formatStr = NodeConfig.GetParameter<string>("Format");
            var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            string uploadFormat = formatRegistry.ResolveFormatId(formatStr, WellKnownFileFormats.Png);

            int jpegQuality = NodeConfig.GetParameter<int?>("JpegQuality") ?? 90;
            string title = NodeConfig.GetParameter<string>("Title") ?? captureDetails.Title;
            if (!string.IsNullOrEmpty(title))
            {
                title = FilenameHelper.FillVariables(title, false);
            }

            string description = NodeConfig.GetParameter<string>("Description");
            if (!string.IsNullOrEmpty(description))
            {
                description = FilenameHelper.FillVariables(description, false);
            }

            bool copyToClipboard = NodeConfig.GetParameter<bool?>("CopyLinkToClipboard") ?? true;

            context.LogStep("Uploading capture to Imgur...");
            Log.Info("ImgurStep: Uploading capture to Imgur.");

            var outputSettings = new SurfaceOutputSettings(uploadFormat, jpegQuality, false);

            var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            var image = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
            ImgurInfo imgurInfo = await UploadToImgurAsync(image, title, description, cancellationToken).ConfigureAwait(false);

            if (imgurInfo != null && !string.IsNullOrEmpty(imgurInfo.Original))
            {
                string link = imgurInfo.Original;
                context.Properties["Imgur.UploadUrl"] = link;
                context.Properties["Imgur.Hash"] = imgurInfo.Hash;
                context.Properties["Imgur.DeleteHash"] = imgurInfo.DeleteHash;
                await source.UseSurfaceAsync(surface => surface.UploadUrl = link, cancellationToken).ConfigureAwait(false);

                if (copyToClipboard)
                {
                    await ClipboardService.For(context.Ui).SetTextAsync(link, cancellationToken).ConfigureAwait(false);
                    context.LogStep($"Copied Imgur URL '{link}' to clipboard.");
                }

                context.LogStep($"Successfully uploaded to Imgur: {link}");
                Log.InfoFormat("ImgurStep: Uploaded image to Imgur: {0}", link);
            }
            else
            {
                context.LogStep("ImgurStep: Upload to Imgur failed.");
                Log.Warn("ImgurStep: Upload to Imgur failed.");
            }
        }

        /// <summary>
        /// Upload the encoded capture to Imgur and add it to the history
        /// </summary>
        /// <returns>ImgurInfo, null when the upload failed</returns>
        public static async Task<ImgurInfo> UploadToImgurAsync(EncodedImage image, string title, string description, CancellationToken cancellationToken)
        {
            var config = Config;
            string apiUrl = (config?.ImgurApi3Url ?? "https://api.imgur.com/3") + "/image.xml";

            try
            {
                var postData = new Dictionary<string, string>
                {
                    { "image", Convert.ToBase64String(image.ToArray()) },
                    { "type", "base64" }
                };

                if (!string.IsNullOrEmpty(title))
                {
                    postData["title"] = title;
                }

                if (!string.IsNullOrEmpty(description))
                {
                    postData["description"] = description;
                }

                var request = ImgurUtils.CreateRequest(HttpMethod.Post, apiUrl);
                // FormUrlEncodedContent can't handle the length of a base64 image on .NET Framework
                string encodedParams = string.Join("&", postData.Select(kvp => $"{NetworkHelper.EscapeDataString(kvp.Key)}={NetworkHelper.EscapeDataString(kvp.Value)}"));
                request.Content = new StringContent(encodedParams, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                string responseString = await NetworkHelper.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(responseString))
                {
                    var info = ImgurInfo.ParseResponse(responseString);
                    if (info != null && config != null)
                    {
                        await UiDispatcher.Current.InvokeAsync(() =>
                        {
                            config.ImgurUploadHistory ??= new Dictionary<string, string>();
                            config.RuntimeImgurHistory ??= new Dictionary<string, ImgurInfo>();
                            config.ImgurUploadHistory[info.Hash] = info.DeleteHash;
                            config.RuntimeImgurHistory[info.Hash] = info;
                        }, CancellationToken.None).ConfigureAwait(false);
                    }

                    return info;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error("Error uploading image to Imgur", ex);
            }

            return null;
        }
    }
}
