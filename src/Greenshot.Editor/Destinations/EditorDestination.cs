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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Forms;
using log4net;
using Greenshot.Base.Core.FileFormat;

namespace Greenshot.Editor.Destinations
{
    /// <summary>
    /// Opens the capture in the editor (the editor takes over the surface), or adds it to an open editor.
    /// </summary>
    public class EditorDestination : DestinationBase
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(EditorDestination));
        private static readonly IEditorConfiguration EditorConfiguration = IniConfigRegistry.GetSection<IEditorConfiguration>();
        public const string DESIGNATION = "Editor";
        private readonly IImageEditor _dedicatedEditor;
        private readonly bool _replaceSurfaceInDedicatedEditor;
        private readonly bool? _reuseAvailableEditor;
        private readonly bool? _matchSizeToCapture;

        public EditorDestination()
        {
            // Do not remove, is needed for the framework
        }

        public EditorDestination(IImageEditor dedicatedEditor, bool replaceSurfaceInDedicatedEditor = false)
        {
            _dedicatedEditor = dedicatedEditor;
            _replaceSurfaceInDedicatedEditor = replaceSurfaceInDedicatedEditor;
        }

        public EditorDestination(bool? reuseAvailableEditor = null, bool? matchSizeToCapture = null)
        {
            _reuseAvailableEditor = reuseAvailableEditor;
            _matchSizeToCapture = matchSizeToCapture;
        }

        public override string Designation => DESIGNATION;

        public override DestinationDescriptor Descriptor
        {
            get
            {
                string name;
                if (_dedicatedEditor == null)
                {
                    name = Language.GetString(LangKey.settings_destination_editor);
                }
                else
                {
                    var title = _dedicatedEditor.CaptureDetails?.Title;
                    name = title == null
                        ? Language.GetString(LangKey.settings_destination_editor_add)
                        : Language.GetString(LangKey.settings_destination_editor_add) + " - " + title.Substring(0, Math.Min(20, title.Length));
                }

                return new DestinationDescriptor(name, 1, DestinationIcons.Greenshot, hasDynamicDestinations: _dedicatedEditor == null);
            }
        }

        public override async ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            // The editors live on the UI thread
            return await UiDispatcher.Current.InvokeAsync<IReadOnlyList<IDestination>>(
                () => ImageEditorForm.Editors.Select(editor => (IDestination)new EditorDestination(editor)).ToList(), cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            if (_dedicatedEditor != null && !_replaceSurfaceInDedicatedEditor)
            {
                // Add the capture as image to the open editor, it gets its own copy
                using var lease = await request.Source.RenderAsync(new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false) { DisableReduceColors = true }, cancellationToken).ConfigureAwait(false);
                await UiDispatcher.Current.InvokeAsync(() => _dedicatedEditor.Surface.AddImageContainer(lease.Image, 10, 10), cancellationToken).ConfigureAwait(false);
                // The editor only shows the capture, it isn't saved
                return ExportResult.Succeeded(clearsModified: false);
            }

            // Hand the surface itself to an editor, on the UI thread: the editor keeps it
            return await request.Source.UseSurfaceAsync(surface => OpenInEditor(surface, captureDetails), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs on the UI thread: show the surface in an editor
        /// </summary>
        private ExportResult OpenInEditor(ISurface surface, ICaptureDetails captureDetails)
        {
            if (_dedicatedEditor != null)
            {
                // we explicitly replace the surface, so we can reset the modified flag
                _dedicatedEditor.Surface.Modified = false;
                _dedicatedEditor.Surface = surface;
                return ExportResult.Succeeded(clearsModified: false, keepsCapture: true);
            }

            bool reuse = _reuseAvailableEditor ?? EditorConfiguration.ReuseEditor;
            if (reuse)
            {
                foreach (IImageEditor openedEditor in ImageEditorForm.Editors)
                {
                    if (openedEditor.Surface.Modified) continue;

                    openedEditor.Surface = surface;
                    if (openedEditor is Form editorForm)
                    {
                        if (editorForm.WindowState == FormWindowState.Minimized)
                        {
                            editorForm.WindowState = FormWindowState.Normal;
                        }

                        editorForm.BringToFront();
                        editorForm.Activate();
                    }

                    return ExportResult.Succeeded(clearsModified: false, keepsCapture: true);
                }
            }

            try
            {
                var editorForm = new ImageEditorForm(surface, !surface.Modified, _matchSizeToCapture); // Output made??

                if (!string.IsNullOrEmpty(captureDetails?.Filename))
                {
                    editorForm.SetImagePath(captureDetails.Filename);
                }

                editorForm.Show();
                editorForm.Activate();
                LOG.Debug("Finished opening Editor");
                return ExportResult.Succeeded(clearsModified: false, keepsCapture: true);
            }
            catch (Exception e)
            {
                LOG.Error(e);
                return ExportResult.Failed(e.Message, e);
            }
        }
    }
}
