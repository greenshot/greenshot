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
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using log4net;

namespace Greenshot.Editor.Tabs
{
    /// <summary>
    /// One capture in the editor, shown as a tab of an editor window. The tab owns the surface and what belongs to it
    /// (title, status message, scroll position), the window shows the selected tab. A tab can move to another window.
    /// </summary>
    public sealed class EditorTab : IImageEditor, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EditorTab));

        internal EditorTab(Surface surface)
        {
            Attach(surface);
        }

        /// <summary>
        /// The surface of the capture
        /// </summary>
        internal Surface Surface { get; private set; }

        /// <summary>
        /// The window which shows this tab
        /// </summary>
        internal ImageEditorForm Window { get; set; }

        /// <summary>
        /// The scroll position of the canvas, kept while another tab is shown
        /// </summary>
        internal Point ScrollPosition { get; set; }

        /// <summary>
        /// The text of the tab: the file name when the capture was saved, else the title of the capture
        /// </summary>
        public string Title
        {
            get
            {
                if (Surface?.LastSaveFullPath != null)
                {
                    return Path.GetFileName(Surface.LastSaveFullPath);
                }

                return string.IsNullOrEmpty(Surface?.CaptureDetails?.Title) ? Texts.Editor.Title : Surface.CaptureDetails.Title;
            }
        }

        /// <summary>
        /// The description of the capture for the window title and the tooltip: the full path when it was saved, else the title
        /// </summary>
        public string Description => Surface?.LastSaveFullPath ?? Title;

        /// <summary>
        /// The last message of the surface (export result), shown in the status bar while the tab is selected
        /// </summary>
        public string StatusText { get; private set; }

        public bool StatusIsError { get; private set; }

        /// <summary>
        /// True when the status message is about the saved file, the status bar then offers to copy the path or open the folder
        /// </summary>
        public bool StatusIsFileSaved { get; private set; }

        /// <summary>
        /// The title or the status changed
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// The file the capture was loaded from or saved to, which is shown in the status bar
        /// </summary>
        public void SetImagePath(string fullPath)
        {
            // The editor can't save these formats
            if (fullPath != null && (fullPath.EndsWith(".ico") || fullPath.EndsWith(".wmf")))
            {
                fullPath = null;
            }

            Surface.LastSaveFullPath = fullPath;
            if (fullPath == null)
            {
                return;
            }

            SetStatus(string.Format(Texts.Editor.Imagesaved, fullPath), isError: false, isFileSaved: true);
        }

        /// <summary>
        /// Replace the surface by another one, the current surface is disposed
        /// </summary>
        internal void ReplaceSurface(Surface surface)
        {
            var previous = Surface;
            Detach();
            previous.Dispose();
            Attach(surface);
            StatusText = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void Attach(Surface surface)
        {
            Surface = surface ?? throw new ArgumentNullException(nameof(surface));
            Surface.SurfaceMessage += SurfaceMessageReceived;
        }

        private void Detach()
        {
            Surface.SurfaceMessage -= SurfaceMessageReceived;
        }

        /// <summary>
        /// Export results are reported by the surface on the UI thread, the tab keeps the message for when it is shown
        /// </summary>
        private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            string message = DateTime.Now.ToLongTimeString() + " - ";
            switch (eventArgs.MessageType)
            {
                case SurfaceMessageTyp.Error:
                    SetStatus(message + "⚠ " + eventArgs.Message, isError: true, isFileSaved: false);
                    break;
                case SurfaceMessageTyp.FileSaved:
                    SetStatus(message + eventArgs.Message, isError: false, isFileSaved: true);
                    break;
                default:
                    SetStatus(message + eventArgs.Message, isError: false, isFileSaved: false);
                    break;
            }
        }

        private void SetStatus(string text, bool isError, bool isFileSaved)
        {
            StatusText = text;
            StatusIsError = isError;
            StatusIsFileSaved = isFileSaved;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        ISurface IImageEditor.Surface
        {
            get => Surface;
            set => (Window ?? throw new InvalidOperationException("The tab was closed.")).ReplaceSurface(this, (Surface)value);
        }

        public ICaptureDetails CaptureDetails => Surface.CaptureDetails;

        public Image GetImageForExport() => Surface.GetImageForExport();

        /// <summary>
        /// Save the capture to the file (when it was changed) and close the tab, used when Windows restarts
        /// </summary>
        public bool TrySaveState(string filePath)
        {
            if (!Surface.Modified)
            {
                Window.CloseTab(this);
                return false;
            }

            try
            {
                ImageIO.Save(Surface, filePath, true, new SurfaceOutputSettings(WellKnownFileFormats.Greenshot), false);
                // Make sure the user isn't asked to save
                Surface.Modified = false;
                Window.CloseTab(this);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving surface state to {filePath}", ex);
            }

            return false;
        }

        public void Dispose()
        {
            Detach();
            Surface.Dispose();
        }

        public override string ToString() => Title;
    }
}
