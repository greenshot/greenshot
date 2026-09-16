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
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Acquires a capture payload by loading an image file or .greenshot file from disk.
    /// </summary>
    public class FileCaptureSource : ICaptureSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileCaptureSource));

        public string Name => "FileCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            string filename = null;
            if (context.Properties.TryGetValue("Filename", out var fnObj))
            {
                filename = fnObj as string;
            }

            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
            {
                context.Abort($"File not found or not specified: '{filename}'");
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
                if (filename.ToLower().EndsWith("." + OutputFormat.greenshot))
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
