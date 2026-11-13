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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.DocumentModel;
using Greenshot.Helpers;
using Greenshot.Ipc;
using log4net;

[assembly: GreenshotModule(typeof(StandaloneEditorDestination))]

namespace Greenshot.DocumentModel
{
    /// <summary>
    /// "Editor (stand-alone)": opens the capture in greenshot-editor.exe (Greenshot.Editor.Standalone), the editor which is a
    /// program of its own in the folder Editor below Greenshot. The surface is mapped to a document, the document is written
    /// as a file into the handover folder and the editor is started with it; an editor which already runs takes it over.
    /// Only in Debug builds: Greenshot.csproj leaves this folder out of Release.
    /// </summary>
    public sealed class StandaloneEditorDestination : DestinationBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StandaloneEditorDestination));

        public const string DesignationName = "StandaloneEditor";

        /// <summary>
        /// The argument of greenshot-editor.exe which names a file of the handover folder
        /// </summary>
        public const string HandoverArgument = "--handover";

        /// <summary>
        /// Where Greenshot and the editor give each other documents as files: a folder of the user, both sides name it the same
        /// way. A file is removed by who reads it.
        /// </summary>
        public static string HandoverDirectory => Path.Combine(Path.GetTempPath(), "GreenshotEditor");

        /// <summary>
        /// greenshot-editor.exe of this Greenshot
        /// </summary>
        public static string EditorExecutable => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IpcClientVerifier.StandaloneEditor);

        public override string Designation => DesignationName;

        // No language key while this is Debug only: a key needs an entry in every language pack
        public override DestinationDescriptor Descriptor => new("Editor (stand-alone)", 1, DestinationIcons.Stock(StockIconId.ImageFiles));

        /// <summary>
        /// Only offered when the editor is there
        /// </summary>
        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && File.Exists(EditorExecutable);

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (!request.Ui.IsInteractive)
            {
                // Nobody to edit it
                return ExportResult.Declined;
            }

            string error = await request.Source.UseSurfaceAsync(surface => Open(surface, request.Metadata), cancellationToken).ConfigureAwait(false);
            // The editor keeps its own document, saving it there is its own business
            return error == null ? ExportResult.Succeeded(clearsModified: false) : ExportResult.Failed(error);
        }

        /// <summary>
        /// Runs on the UI thread: the document is a copy, the surface stays with the flow or the old editor.
        /// Returns null, or why the editor could not be started.
        /// </summary>
        private static string Open(ISurface surface, ICaptureDetails captureDetails)
        {
            string file = null;
            try
            {
                var document = SurfaceToDocument.ToDocument(surface);
                string title = captureDetails?.Title ?? surface.CaptureDetails?.Title;
                if (!string.IsNullOrEmpty(title))
                {
                    document = document with { Metadata = (document.Metadata ?? new DocumentMetadata()) with { Title = title } };
                }

                Directory.CreateDirectory(HandoverDirectory);
                file = Path.Combine(HandoverDirectory, Guid.NewGuid().ToString("N") + ".greenshot");
                var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>();
                using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    GreenshotPackage.Write(document, stream, mediaType => registry.GetByMimeType(mediaType)?.PreferredExtension);
                }

                // The editor removes the file when it has read it. The file name has no spaces or quotes (a GUID), the folder may.
                var start = new ProcessStartInfo(EditorExecutable, $"{HandoverArgument} \"{file}\"")
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(EditorExecutable)
                };
                using (Process.Start(start))
                {
                }

                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                Log.Error("The stand-alone editor could not be started", ex);
                TryDelete(file);
                return ex.Message;
            }
        }

        internal static void TryDelete(string file)
        {
            try
            {
                if (file != null)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"The handed over file {file} could not be removed", ex);
            }
        }
    }
}
