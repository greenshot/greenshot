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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Hdr;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Native;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Base.Recipes.Sources
{
    /// <summary>
    /// Acquires a capture payload by loading an image file or .greenshot file from disk.
    /// Explicitly resolves the target file path from RecipeNodeConfig parameters ("Filename", "file", etc.)
    /// with expression evaluation (e.g. "${Filename}"), falling back to flow context properties.
    /// </summary>
    public class FileCaptureSource : ICaptureSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileCaptureSource));

        private readonly RecipeNodeConfig _nodeConfig;

        public string Name => "FileCaptureSource";

        public FileCaptureSource(RecipeNodeConfig nodeConfig = null)
        {
            _nodeConfig = nodeConfig;
        }

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            string filename = null;

            // 1. Resolve from RecipeNodeConfig parameters if configured
            if (_nodeConfig?.Parameters != null)
            {
                if (_nodeConfig.Parameters.TryGetValue("Filename", out var cfgVal))
                {
                    // Already evaluated by the engine (e.g. "${Filename}"); must not be evaluated again
                    string configured = cfgVal?.ToString();
                    if (!string.IsNullOrWhiteSpace(configured))
                    {
                        filename = configured;
                    }
                }
            }

            // 2. Fall back to flow context properties bag
            if (string.IsNullOrWhiteSpace(filename) && context?.Properties != null)
            {
                if (context.Properties.TryGetValue("Filename", out var fnObj))
                {
                    filename = fnObj as string;
                }
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                context?.Abort("SourceType 'File' requires a file path. Specify 'Filename' parameter on the Source node (e.g. 'Filename': '${var.Filename}') or provide 'Filename' in the flow context properties.");
                return Task.FromResult<ICapturePayload>(null);
            }

            if (!File.Exists(filename))
            {
                context?.Abort($"SourceType 'File' could not find file: '{filename}'");
                return Task.FromResult<ICapturePayload>(null);
            }

            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

            var extension = Path.GetExtension(filename);
            extension = FileFormatHandlerExtensions.NormalizeExtension(extension);

            var loadFileFormatHandler = fileFormatHandlers
                .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadFromStream, extension))
                .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadFromStream, extension))
                .FirstOrDefault();

            try
            {
                if (WellKnownFileFormats.IsEqualFormat(WellKnownFileFormats.Greenshot, extension.TrimStart('.')))
                {
                    using FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        var surface = loadFileFormatHandler.LoadSurface(fileStream);

                        var payload = new CapturePayload
                        {
                            Surface = surface,
                            RawCapture = new Capture(surface.GetImageForExport())
                            {
                                CaptureDetails = surface.CaptureDetails
                            }
                        };
                        if (surface?.CaptureDetails != null)
                        {
                            surface.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
                            surface.CaptureDetails.Filename = filename;
                            surface.CaptureDetails.AddMetaData("file", filename);
                            surface.CaptureDetails.AddMetaData("source", "file");
                            surface.CaptureDetails.AddMetaData("dirname", Path.GetDirectoryName(filename) ?? string.Empty);
                            surface.CaptureDetails.AddMetaData("filename", Path.GetFileNameWithoutExtension(filename));
                            surface.CaptureDetails.AddMetaData("basename", Path.GetFileNameWithoutExtension(filename));
                            surface.CaptureDetails.AddMetaData("extension", Path.GetExtension(filename));
                        }
                        return Task.FromResult<ICapturePayload>(payload);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Couldn't read file contents", ex);
                    }

                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
            }



            try
            {
                // An HDR file is opened like an HDR capture: tone mapped, with its HDR image, so it can be saved as HDR file again
                var hdrImage = TryLoadHdrImage(filename);
                Image fileImage;
                try
                {
                    fileImage = hdrImage != null ? HdrImageToneMapping.ToBitmap(hdrImage) : ImageIO.LoadImage(filename);
                }
                catch
                {
                    hdrImage?.Dispose();
                    throw;
                }
                if (fileImage == null)
                {
                    hdrImage?.Dispose();
                    context.Abort($"Could not load image from '{filename}'");
                    return Task.FromResult<ICapturePayload>(null);
                }

                ICapture capture = new Capture(fileImage);
                // An HDR file without HDR content (nothing brighter than SDR white or outside sRGB) opens as a normal image: no HDR image
                if (hdrImage != null && !hdrImage.HasHdrContent)
                {
                    hdrImage.Dispose();
                    hdrImage = null;
                }
                if (hdrImage != null)
                {
                    hdrImage.BelongTo(capture.Image);
                    // An HDR file: saving it again suggests JPEG XR
                    hdrImage.IsRequested = true;
                    capture.HdrImage = hdrImage;
                }
                capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
                capture.CaptureDetails.Filename = filename;
                capture.CaptureDetails.AddMetaData("file", filename);
                capture.CaptureDetails.AddMetaData("source", "file");
                capture.CaptureDetails.AddMetaData("dirname", Path.GetDirectoryName(filename) ?? string.Empty);
                capture.CaptureDetails.AddMetaData("filename", Path.GetFileNameWithoutExtension(filename));
                capture.CaptureDetails.AddMetaData("basename", Path.GetFileNameWithoutExtension(filename));
                capture.CaptureDetails.AddMetaData("extension", Path.GetExtension(filename));
                ImageMetadataReader.ReadInto(capture.CaptureDetails, filename);

                var payload = new CapturePayload(capture);
                return Task.FromResult<ICapturePayload>(payload);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
            }
            context.Abort($"No filename specified.");
            return Task.FromResult<ICapturePayload>(null);
        }

        /// <summary>
        /// The HDR image of an HDR file (see IHdrImageDecoder), null for an 8-bit file or without a decoder
        /// </summary>
        private static HdrImage TryLoadHdrImage(string filename)
        {
            var decoder = SimpleServiceProvider.Current.GetInstance<IHdrImageDecoder>(true);
            if (decoder == null || !File.Exists(filename))
            {
                return null;
            }
            using var stream = File.OpenRead(filename);
            return decoder.TryDecode(stream, out var hdrImage) ? hdrImage : null;
        }
    }
}
