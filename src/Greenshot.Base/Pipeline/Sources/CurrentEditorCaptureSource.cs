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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Forms;
using log4net;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Provides the surface supplied by an editor-triggered recipe.
    /// </summary>
    public class CurrentEditorCaptureSource : ICaptureSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CurrentEditorCaptureSource));

        public string Name => "CurrentEditorCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (!context.Properties.TryGetValue("EditorForm", out var editorObject) || !(editorObject is IImageEditor editor) || editor.Surface == null)
            {
                context.Abort("Current editor surface is not available. Property 'EditorForm' with IImageEditor is required.");

                return Task.FromResult<ICapturePayload>(null);
            }

            var sufaceClone = editor.Surface.Clone();

            return Task.FromResult<ICapturePayload>(new CapturePayload
            {
                Surface = sufaceClone, 
                RetainSurfaceForEditor = true,

                RawCapture = new Capture(sufaceClone.GetImageForExport())
                {
                    CaptureDetails = sufaceClone.CaptureDetails
                }
            });
        }

    }
}
