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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Settings.Views;
using log4net;

using Greenshot.Base.Languages;

namespace Greenshot.Destinations
{
    /// <summary>
    /// Options of a file export which a flow can override (the Destination.* variables of the recipe).
    /// </summary>
    public sealed class FileDestinationOptions
    {
        /// <summary>
        /// Overwrite an existing file; default: true for the capture's own file, the setting for a generated name.
        /// </summary>
        public bool? AllowOverwrite { get; set; }

        /// <summary>
        /// Copy the path of the file to the clipboard; default: the setting.
        /// </summary>
        public bool? CopyPathToClipboard { get; set; }

        /// <summary>
        /// The output settings to use (the quality was asked already); default: the settings, asking for the quality if configured.
        /// </summary>
        public SurfaceOutputSettings OutputSettings { get; set; }

        /// <summary>
        /// Save an HDR file (Auto or Jxr: JPEG XR, see HdrOutput) when the capture has HDR pixels and wasn't edited; otherwise the normal file is saved.
        /// Null or None: the normal file.
        /// </summary>
        public string HdrFormat { get; set; }
    }

    /// <summary>
    /// This is the destination which saves the capture to the default location (no dialog)
    /// </summary>
    public class FileDestination : DestinationBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileDestination));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private readonly FileDestinationOptions _options;

        public FileDestination()
        {
        }

        public FileDestination(FileDestinationOptions options)
        {
            _options = options;
        }

        public override string Designation => nameof(WellKnownDestinations.FileNoDialog);

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(
            Texts.Core.QuicksettingsDestinationFile, 0, DestinationIcons.Resource("Save.Image"), "Ctrl+S");

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            var outputSettings = _options?.OutputSettings ?? new SurfaceOutputSettings();
            bool overwrite;
            string fullPath;
            if (captureDetails?.Filename != null)
            {
                // As we save a pre-selected file, allow to overwrite.
                Log.InfoFormat("Using previous filename");
                fullPath = captureDetails.Filename;
                overwrite = _options?.AllowOverwrite ?? true;
                outputSettings.Format = ImageIO.FormatForFilename(fullPath);
            }
            else
            {
                fullPath = await CreateNewFilenameAsync(captureDetails, request.Ui, cancellationToken).ConfigureAwait(false);
                // As we generate a file, the configuration tells us if we allow to overwrite
                overwrite = _options?.AllowOverwrite ?? CoreConfig.OutputFileAllowOverwrite;
            }

            if (fullPath == null)
            {
                // The user didn't fix the invalid filename pattern
                return ExportResult.Declined;
            }

            if (HdrOutput.IsHdrFormat(_options?.HdrFormat))
            {
                var hdrResult = await TrySaveHdrAsync(request, fullPath, overwrite, cancellationToken).ConfigureAwait(false);
                if (hdrResult != null)
                {
                    return hdrResult;
                }
                // Without HDR pixels the normal file is saved, the user was told why
            }

            if (_options?.OutputSettings == null && CoreConfig.OutputFilePromptQuality && request.Ui.IsInteractive)
            {
                outputSettings = await request.Ui.PromptOutputSettingsAsync(outputSettings, cancellationToken).ConfigureAwait(false);
                if (outputSettings == null)
                {
                    // The user cancelled the quality dialog
                    return ExportResult.Declined;
                }
            }

            bool copyPath = _options?.CopyPathToClipboard ?? CoreConfig.OutputFileCopyPathToClipboard;
            try
            {
                // A file on this computer gets every detail of ExportMetadata, also the private ones
                outputSettings.IncludePrivateMetadata = true;
                fullPath = await ExportFiles.SaveAsync(request.Source, fullPath, overwrite, outputSettings, cancellationToken).ConfigureAwait(false);
            }
            catch (FileAlreadyExistsException ex)
            {
                // Our generated filename exists, display 'save-as'
                Log.InfoFormat("Not overwriting: {0}", ex.Message);
                if (!request.Ui.IsInteractive)
                {
                    return ExportResult.Failed(ex.Message, ex);
                }

                fullPath = await SaveWithDialogAsync(request, copyPath, cancellationToken).ConfigureAwait(false);
                return fullPath == null ? ExportResult.Declined : Saved(captureDetails, fullPath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Catching any exception to prevent that the user can't write in the directory.
                // This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
                Log.Error("Error saving screenshot!", ex);
                if (!request.ManuallyInitiated || !request.Ui.IsInteractive)
                {
                    // Part of a flow: the failure is reported (and the capture kept for the editor)
                    return ExportResult.Failed(ex.Message, ex);
                }

                // Show the problem, then present a save-as dialog
                await request.Ui.ConfirmAsync(Texts.Core.Error, Texts.Core.ErrorSave, true, cancellationToken).ConfigureAwait(false);
                fullPath = await SaveWithDialogAsync(request, copyPath, cancellationToken).ConfigureAwait(false);
                return fullPath == null ? ExportResult.Declined : Saved(captureDetails, fullPath);
            }

            if (copyPath)
            {
                await ClipboardService.Current.SetTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
            }

            return Saved(captureDetails, fullPath);
        }

        /// <summary>
        /// Save the HDR pixels of the capture as an HDR file (see HdrOutput), next to where the normal file would go with the extension of the HDR format.
        /// Null when the normal file is to be saved: no HDR pixels (not on an HDR display, the frozen screen), the capture was edited, or the encoder failed.
        /// Then a notification tells why.
        /// </summary>
        private async Task<ExportResult> TrySaveHdrAsync(ExportRequest request, string fullPath, bool overwrite, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            var pixels = captureDetails?.HdrPixels;
            string normalFormat = Path.GetExtension(fullPath)?.TrimStart('.').ToUpperInvariant();
            if (pixels == null || request.Source.IsModified)
            {
                string reason = pixels == null ? "the capture has no HDR pixels (not on an HDR display, or the frozen screen)" : "the capture was edited";
                Log.Info($"Not saving an HDR file, {reason}: saving {fullPath}.");
                NotifyHdrFallback(request, normalFormat);
                return null;
            }

            var encoder = SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true);
            string formatId = HdrOutput.ResolveFormatId(_options.HdrFormat);
            if (encoder == null || formatId == null || !encoder.CanEncode(formatId))
            {
                Log.Warn($"No encoder can write the HDR format {_options.HdrFormat} on this system, saving {fullPath}.");
                NotifyHdrFallback(request, normalFormat);
                return null;
            }

            byte[] bytes;
            using (var buffer = new MemoryStream())
            {
                // Like the normal file: with the details which are only for the user
                if (!encoder.TryEncode(pixels, formatId, buffer, captureDetails, true, out string error))
                {
                    Log.Warn($"Couldn't write the HDR file: {error} Saving {fullPath}.");
                    NotifyHdrFallback(request, normalFormat);
                    return null;
                }
                bytes = buffer.ToArray();
            }

            string hdrPath = Path.ChangeExtension(fullPath, "." + formatId);
            try
            {
                hdrPath = await ExportFiles.SaveEncodedAsync(new EncodedImage(bytes, formatId), hdrPath, overwrite, cancellationToken).ConfigureAwait(false);
            }
            catch (FileAlreadyExistsException ex)
            {
                Log.InfoFormat("Not overwriting the HDR file {0}, saving the normal file.", ex.Message);
                return null;
            }

            if (_options?.CopyPathToClipboard ?? CoreConfig.OutputFileCopyPathToClipboard)
            {
                await ClipboardService.Current.SetTextAsync(hdrPath, cancellationToken).ConfigureAwait(false);
            }
            return Saved(captureDetails, hdrPath);
        }

        private static void NotifyHdrFallback(ExportRequest request, string normalFormat)
        {
            string message = string.Format(Texts.Core.HdrNotSaved, normalFormat);
            request.Ui.NotifyAsync(new Notification(NotificationKind.Warning, message)).FireAndLog("HDR fallback notification", Log);
        }

        /// <summary>
        /// Remember the file: the capture's filename and the last saved file of the settings (written on the UI thread).
        /// </summary>
        internal static ExportResult Saved(ICaptureDetails captureDetails, string fullPath)
        {
            if (captureDetails != null)
            {
                captureDetails.Filename = fullPath;
            }

            UiDispatcher.Current.InvokeAsync(() => CoreConfig.OutputFileAsFullpath = fullPath).FireAndLog("Remember the last saved file", Log);
            return ExportResult.Succeeded(filePath: fullPath);
        }

        /// <summary>
        /// Ask for a file and save the capture to it, null when the user declined or the file couldn't be written.
        /// </summary>
        internal static async Task<string> SaveWithDialogAsync(ExportRequest request, bool copyPathToClipboard, CancellationToken cancellationToken)
        {
            string fileNameWithExtension = await request.Ui.PickSaveFileAsync(new SaveFileRequest(request.Metadata), cancellationToken).ConfigureAwait(false);
            if (fileNameWithExtension == null)
            {
                return null;
            }

            var outputSettings = new SurfaceOutputSettings(ImageIO.FormatForFilename(fileNameWithExtension));
            if (CoreConfig.OutputFilePromptQuality)
            {
                outputSettings = await request.Ui.PromptOutputSettingsAsync(outputSettings, cancellationToken).ConfigureAwait(false);
                if (outputSettings == null)
                {
                    // The user cancelled the quality dialog
                    return null;
                }
            }

            try
            {
                // The user picked the file: overwrite
                outputSettings.IncludePrivateMetadata = true;
                await ExportFiles.SaveAsync(request.Source, fileNameWithExtension, true, outputSettings, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is ExternalException || e is IOException || e is UnauthorizedAccessException)
            {
                string message = string.Format(Texts.Core.ErrorNowriteaccess, fileNameWithExtension).Replace(@"\\", @"\");
                await request.Ui.ConfirmAsync(Texts.Core.Error, message, true, cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (copyPathToClipboard)
            {
                await ClipboardService.Current.SetTextAsync(fileNameWithExtension, cancellationToken).ConfigureAwait(false);
            }

            return fileNameWithExtension;
        }

        /// <summary>
        /// Create the filename for a new capture from the configured pattern, when the pattern is invalid the user can fix it in the settings.
        /// </summary>
        internal static async Task<string> CreateNewFilenameAsync(ICaptureDetails captureDetails, IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            Log.InfoFormat("Creating new filename");
            string pattern = CoreConfig.OutputFileFilenamePattern;
            if (string.IsNullOrEmpty(pattern))
            {
                pattern = "greenshot ${capturetime}";
            }

            string filename = FilenameHelper.GetFilenameFromPattern(pattern, CoreConfig.OutputFileFormat, captureDetails);
            string filepath = CoreConfig.GetOutputDirectory(out string folderProblem);
            if (folderProblem != null)
            {
                // Saved to the default folder this time: say so, instead of letting the screenshot "disappear"
                string message = string.Format(Texts.Core.ErrorOutputfolderFallback, CoreConfig.OutputFilePath, folderProblem, filepath);
                userInteraction.NotifyAsync(new Notification(NotificationKind.Warning, message)).FireAndLog("Screenshot folder fallback notification", Log);
            }
            try
            {
                return Path.Combine(filepath, filename);
            }
            catch (ArgumentException)
            {
                // configured filename or path not valid, show error message...
                Log.InfoFormat("Generated path or filename not valid: {0}, {1}", filepath, filename);
                if (!userInteraction.IsInteractive)
                {
                    throw;
                }
            }

            await userInteraction.ConfirmAsync(Texts.Core.Error, Texts.Core.ErrorSaveInvalidChars, true, cancellationToken).ConfigureAwait(false);
            // ... lets get the pattern fixed....
            bool fixedPattern = await UiDispatcher.Current.InvokeAsync(() => new SettingsWindow().ShowDialog() == true, cancellationToken).ConfigureAwait(false);
            // ... OK -> then try again, cancelled -> no file
            return fixedPattern ? await CreateNewFilenameAsync(captureDetails, userInteraction, cancellationToken).ConfigureAwait(false) : null;
        }
    }
}
