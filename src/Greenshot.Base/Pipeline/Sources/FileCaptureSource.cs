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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Base.Pipeline.Sources
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
                context?.Abort("SourceType 'File' requires a file path. Specify 'Filename' parameter on the Source node (e.g. 'Filename': '${Filename}') or provide 'Filename' in the flow context properties.");
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
                var fileImage = ImageIO.LoadImage(filename);
                if (fileImage == null)
                {
                    context.Abort($"Could not load image from '{filename}'");
                    return Task.FromResult<ICapturePayload>(null);
                }

                ICapture capture = new Capture(fileImage);
                capture.CaptureDetails.Title = Path.GetFileNameWithoutExtension(filename);
                capture.CaptureDetails.Filename = filename;
                capture.CaptureDetails.AddMetaData("file", filename);
                capture.CaptureDetails.AddMetaData("source", "file");
                capture.CaptureDetails.AddMetaData("dirname", Path.GetDirectoryName(filename) ?? string.Empty);
                capture.CaptureDetails.AddMetaData("filename", Path.GetFileNameWithoutExtension(filename));
                capture.CaptureDetails.AddMetaData("basename", Path.GetFileNameWithoutExtension(filename));
                capture.CaptureDetails.AddMetaData("extension", Path.GetExtension(filename));

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
    }
}
