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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.DocumentModel;
using Greenshot.Helpers;
using Greenshot.Ipc;
using log4net;

[assembly: GreenshotModule(typeof(StandaloneEditorIpcExtension), 20)]

namespace Greenshot.DocumentModel
{
    /// <summary>
    /// The pipe commands of the stand-alone editor (source "editor", greenshot-editor.exe in the folder Editor):
    /// EDITOR_DESTINATIONS lists the destinations, EDITOR_EXPORT sends a document to one of them. The document doesn't go
    /// through the pipe: the editor writes it as a file into the handover folder and names the file. Greenshot reads only
    /// files of that folder, and removes the file when it has read it. Only in Debug builds.
    /// </summary>
    internal sealed class StandaloneEditorIpcExtension : IIpcCommandExtension
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StandaloneEditorIpcExtension));

        public const string DestinationsCommand = "EDITOR_DESTINATIONS";
        public const string ExportCommand = "EDITOR_EXPORT";

        // The editor saves and copies itself; the picker and the window of destinations show menus for a surface
        private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
        {
            StandaloneEditorDestination.DesignationName,
            WpfEditorDestination.DesignationName,
            nameof(WellKnownDestinations.Picker),
            nameof(WellKnownDestinations.DestinationWindow)
        };

        public IEnumerable<string> Commands { get; } = new[] { DestinationsCommand, ExportCommand };

        /// <summary>
        /// All the editor may ask for: it can't run recipes, capture or change settings
        /// </summary>
        public IReadOnlyDictionary<string, IEnumerable<string>> SourceCommands { get; } = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [IpcSources.Editor] = new[] { DestinationsCommand, ExportCommand, "VERSION" }
        };

        /// <summary>
        /// The commands of the editor are only for the editor
        /// </summary>
        public bool IsAllowedForSource(string command, string source) =>
            !Commands.Contains(command, StringComparer.OrdinalIgnoreCase) || string.Equals(source, IpcSources.Editor, StringComparison.OrdinalIgnoreCase);

        public Task<string> CheckAccessAsync(string command, IpcRequestContext context) => Task.FromResult<string>(null);

        public async Task HandleAsync(string command, IpcRequestContext context, IGreenshotShell shell)
        {
            try
            {
                if (string.Equals(command, DestinationsCommand, StringComparison.OrdinalIgnoreCase))
                {
                    var destinations = await UiDispatcher.Current.InvokeAsync(() => Destinations().Select(destination => new { designation = destination.Designation, name = destination.Descriptor.DisplayName }).ToList())
                        .ConfigureAwait(false);
                    await context.ReplyAsync(new { status = "ok", exit_code = 0, destinations, language = IniConfigRegistry.GetSection<ICoreConfiguration>().Language }).ConfigureAwait(false);
                    return;
                }

                string error = await ExportAsync(context).ConfigureAwait(false);
                if (error == null)
                {
                    await context.ReplyAsync(new { status = "ok", exit_code = 0 }).ConfigureAwait(false);
                }
                else
                {
                    Log.Warn($"{ExportCommand} failed: {error}");
                    await context.ReplyAsync(new { status = "error", exit_code = 1, stderr = error }).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"The request {command} of the stand-alone editor failed", ex);
                await context.ReplyAsync(new { status = "error", exit_code = 1, stderr = ex.Message }).ConfigureAwait(false);
            }
        }

        private static IEnumerable<IDestination> Destinations() =>
            DestinationHelper.GetAllDestinations()
                .Where(destination => !Excluded.Contains(destination.Designation) && destination.IsAvailableFor(null) && !destination.Descriptor.UseDynamicsOnly)
                .OrderBy(destination => destination.Descriptor.Priority)
                .ThenBy(destination => destination.Descriptor.DisplayName, StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Null when the document went to the destination, else why not
        /// </summary>
        private static async Task<string> ExportAsync(IpcRequestContext context)
        {
            string file = context.Envelope.Files?.FirstOrDefault();
            if (!IsHandoverFile(file))
            {
                // Never read (or remove) a file from anywhere else, whatever the request names
                return "The document must be a file in the handover folder.";
            }

            try
            {
                string designation = null, title = null;
                context.Envelope.Parameters?.TryGetValue("destination", out designation);
                context.Envelope.Parameters?.TryGetValue("title", out title);

                GreenshotDocument document;
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    document = GreenshotPackage.Read(stream);
                }

                return await UiDispatcher.Current.InvokeAsync(async () =>
                {
                    var destination = Destinations().FirstOrDefault(candidate => string.Equals(candidate.Designation, designation, StringComparison.OrdinalIgnoreCase));
                    if (destination == null)
                    {
                        return $"There is no destination '{designation}'.";
                    }

                    var surface = DocumentToSurface.ToSurface(document);
                    if (!string.IsNullOrEmpty(title) && surface.CaptureDetails != null)
                    {
                        surface.CaptureDetails.Title = title;
                    }

                    var result = await DestinationExporter.ExportAsync(destination, surface, surface.CaptureDetails, manuallyInitiated: true);
                    // The old editor keeps the surface it was given, every other destination is done with it
                    if (destination.Designation != Greenshot.Editor.Destinations.EditorDestination.DESIGNATION)
                    {
                        surface.Dispose();
                    }

                    return result is { Status: ExportStatus.Failed } ? result.Error ?? destination.Descriptor.DisplayName : null;
                }).ConfigureAwait(false);
            }
            finally
            {
                StandaloneEditorDestination.TryDelete(file);
            }
        }

        /// <summary>
        /// True for a .greenshot file directly in the handover folder
        /// </summary>
        internal static bool IsHandoverFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string full = Path.GetFullPath(path);
                string folder = Path.GetFullPath(StandaloneEditorDestination.HandoverDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(Path.GetDirectoryName(full), folder, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(Path.GetExtension(full), ".greenshot", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }
    }
}
