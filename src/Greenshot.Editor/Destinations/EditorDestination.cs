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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Forms;
using log4net;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Languages;

namespace Greenshot.Editor.Destinations
{
    /// <summary>
    /// Opens the capture in the editor (the editor takes over the surface), or adds it to an open editor.
    /// </summary>
    public class EditorDestination : DestinationBase
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(EditorDestination));
        public const string DESIGNATION = "Editor";
        private readonly IImageEditor _dedicatedEditor;
        private readonly bool? _reuseAvailableEditor;
        private readonly bool? _matchSizeToCapture;
        private readonly bool _addImageContainer;
        private readonly bool _useOpenEditor;

        public EditorDestination()
        {
            // Do not remove, is needed for the framework
        }

        private EditorDestination(IImageEditor dedicatedEditor, bool addImageContainer, bool useOpenEditor)
        {
            _dedicatedEditor = dedicatedEditor;
            _addImageContainer = addImageContainer;
            _useOpenEditor = useOpenEditor;
        }

        private EditorDestination(bool? reuseAvailableEditor, bool? matchSizeToCapture)
        {
            _reuseAvailableEditor = reuseAvailableEditor;
            _matchSizeToCapture = matchSizeToCapture;
        }

        /// <summary>
        /// The capture replaces the surface of the editor (the editor takes over the capture).
        /// </summary>
        /// <param name="currentEditor">The editor whose surface is replaced</param>
        public static EditorDestination NewDestinationToReplaceSurface(IImageEditor currentEditor)
            => new EditorDestination(currentEditor, addImageContainer: false, useOpenEditor: false);

        /// <summary>
        /// Adds the capture as image container to an editor, the surface of the editor is kept.
        /// </summary>
        /// <param name="targetEditor">The editor to add to, null to use the shown editor or a new one</param>
        /// <param name="useOpenEditor">With targetEditor null: true takes the tab shown in the last active editor window (modified or not), false opens a new editor</param>
        public static EditorDestination NewDestinationToAddImageContainer(IImageEditor targetEditor, bool useOpenEditor = false)
            => new EditorDestination(targetEditor, addImageContainer: true, useOpenEditor);

        /// <summary>
        /// The capture opens in an editor: as a new tab of an open editor (reuseAvailableEditor), else in a new editor window.
        /// </summary>
        /// <param name="reuseAvailableEditor">null to use the setting</param>
        /// <param name="matchSizeToCapture">null to use the setting</param>
        public static EditorDestination NewDestinationToOpenSurface(bool? reuseAvailableEditor = null, bool? matchSizeToCapture = null)
            => new EditorDestination(reuseAvailableEditor, matchSizeToCapture);

        public override string Designation => DESIGNATION;

        public override DestinationDescriptor Descriptor
        {
            get
            {
                string name;
                if (_dedicatedEditor == null)
                {
                    name = Texts.Settings.DestinationEditor;
                }
                else
                {
                    var title = _dedicatedEditor.CaptureDetails?.Title;
                    name = title == null
                        ? Texts.Settings.DestinationEditorAdd
                        : Texts.Settings.DestinationEditorAdd + " - " + title.Substring(0, Math.Min(20, title.Length));
                }

                return new DestinationDescriptor(name, 1, DestinationIcons.Greenshot, hasDynamicDestinations: _dedicatedEditor == null);
            }
        }

        public override async ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            // The editors live on the UI thread
            return await UiDispatcher.Current.InvokeAsync<IReadOnlyList<IDestination>>(
                () => ImageEditorForm.Editors.Select(editor => (IDestination)NewDestinationToAddImageContainer(editor)).ToList(), cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            if (_addImageContainer)
            {
                // Add the capture as image to an editor, it gets its own copy
                using var lease = await request.Source.RenderAsync(new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false) { DisableReduceColors = true }, cancellationToken).ConfigureAwait(false);
                await UiDispatcher.Current.InvokeAsync(() => GetEditorForImage(_dedicatedEditor, _useOpenEditor).Surface.AddNextImageContainer(lease.Image), cancellationToken).ConfigureAwait(false);
                // The editor only shows the capture, it isn't saved
                return ExportResult.Succeeded(clearsModified: false);
            }

            // Hand the surface itself to an editor, on the UI thread: the editor keeps it
            return await request.Source.UseSurfaceAsync(surface => OpenInEditor(surface, captureDetails), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs on the UI thread: the editor which gets the image. The target; else (if allowed) the tab shown in the last active
        /// editor window, which is brought to the front; else a new editor.
        /// </summary>
        private static IImageEditor GetEditorForImage(IImageEditor targetEditor, bool useOpenEditor)
        {
            if (targetEditor != null)
            {
                return targetEditor;
            }

            if (useOpenEditor)
            {
                var shownTab = ImageEditorForm.ShowLastActiveTab();
                if (shownTab != null)
                {
                    return shownTab;
                }
            }

            return ImageEditorForm.OpenNewImage();
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

            try
            {
                // As a tab of the editor window used last or in its own window, see the settings
                var tab = ImageEditorForm.Open(surface, _reuseAvailableEditor, _matchSizeToCapture);
                if (!string.IsNullOrEmpty(captureDetails?.Filename))
                {
                    tab.SetImagePath(captureDetails.Filename);
                }

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
