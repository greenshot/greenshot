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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Plugin.Imgur
{
    /// <summary>
    /// Capture recipe step that uploads the current capture surface to Imgur.
    /// </summary>
    public class ImgurStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImgurStep));
        private static IImgurConfiguration Config => IniConfigRegistry.GetSection<IImgurConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public ImgurStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ImgurUploadStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("ImgurStep: No surface available to upload.");
                Log.Warn("ImgurStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            string formatStr = NodeConfig.GetParameter<string>("Format") ?? NodeConfig.GetParameter<string>("UploadFormat");
            OutputFormat uploadFormat = OutputFormat.png;
            if (!string.IsNullOrWhiteSpace(formatStr) && Enum.TryParse<OutputFormat>(formatStr, true, out var parsedFormat))
            {
                uploadFormat = parsedFormat;
            }

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

            bool copyToClipboard = NodeConfig.GetParameter<bool?>("CopyLinkToClipboard")
                ?? NodeConfig.GetParameter<bool?>("CopyToClipboard")
                ?? true;

            context.LogStep("Uploading capture to Imgur...");
            Log.Info("ImgurStep: Uploading capture to Imgur.");

            var outputSettings = new SurfaceOutputSettings(uploadFormat, jpegQuality, false);

            ImgurInfo imgurInfo = null;
            await Task.Run(() =>
            {
                imgurInfo = UploadToImgur(surface, captureDetails, outputSettings, title, description);
            }, cancellationToken).ConfigureAwait(false);

            if (imgurInfo != null && !string.IsNullOrEmpty(imgurInfo.Original))
            {
                string link = imgurInfo.Original;
                context.Properties["Imgur.UploadUrl"] = link;
                context.Properties["Imgur.Hash"] = imgurInfo.Hash;
                context.Properties["Imgur.DeleteHash"] = imgurInfo.DeleteHash;
                surface.UploadUrl = link;

                if (copyToClipboard)
                {
                    ClipboardHelper.SetClipboardData(link);
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

        public static ImgurInfo UploadToImgur(ISurface surface, ICaptureDetails captureDetails, SurfaceOutputSettings outputSettings, string title, string description)
        {
            var config = Config;
            string apiUrl = (config?.ImgurApi3Url ?? "https://api.imgur.com/3") + "/image.xml";

            try
            {
                byte[] imageBytes = null;
                using (var ms = new MemoryStream())
                {
                    ImageIO.SaveToStream(surface, ms, outputSettings);
                    imageBytes = ms.ToArray();
                }

                string base64Image = Convert.ToBase64String(imageBytes);

                HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(apiUrl, HTTPMethod.POST);
                webRequest.ServicePoint.Expect100Continue = false;
                webRequest.Headers.Add("Authorization", "Client-ID " + ImgurCredentials.CONSUMER_KEY);
                webRequest.ContentType = "application/x-www-form-urlencoded";

                var postData = new Dictionary<string, string>
                {
                    { "image", base64Image },
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

                string encodedParams = string.Join("&", postData.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(encodedParams);
                webRequest.ContentLength = requestBytes.Length;

                using (var reqStream = webRequest.GetRequestStream())
                {
                    reqStream.Write(requestBytes, 0, requestBytes.Length);
                }

                string responseString = null;
                using (var response = webRequest.GetResponse())
                using (var resStream = response.GetResponseStream())
                {
                    if (resStream != null)
                    {
                        using (var reader = new StreamReader(resStream))
                        {
                            responseString = reader.ReadToEnd();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(responseString))
                {
                    var info = ImgurInfo.ParseResponse(responseString);
                    if (info != null && config != null)
                    {
                        config.ImgurUploadHistory ??= new Dictionary<string, string>();
                        config.RuntimeImgurHistory ??= new Dictionary<string, ImgurInfo>();
                        config.ImgurUploadHistory[info.Hash] = info.DeleteHash;
                        config.RuntimeImgurHistory[info.Hash] = info;
                    }
                    return info;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error uploading image to Imgur", ex);
            }

            return null;
        }
    }
}
