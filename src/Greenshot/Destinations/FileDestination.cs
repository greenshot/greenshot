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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
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
        /// Save an HDR file (Auto or Jxr: JPEG XR, see HdrOutput) when the capture has HDR image and wasn't edited; otherwise the normal file is saved.
        /// Null or None: the normal file.
        /// </summary>
        public string HdrFormat { get; set; }

        /// <summary>
        /// Ask for the file with the save dialog instead of saving to the configured location; with HdrFormat the dialog suggests the HDR format.
        /// Only when the export is interactive, otherwise the file is saved without the dialog.
        /// </summary>
        public bool AskForFilename { get; set; }
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
            if (_options?.AskForFilename == true)
            {
                if (request.Ui.IsInteractive)
                {
                    return await SaveAsAsync(request, cancellationToken).ConfigureAwait(false);
                }
                Log.Info("AskForFilename: the export isn't interactive, saving without the dialog.");
            }

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
                // Without HDR image the normal file is saved, the user was told why
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
        /// Save the HDR image of the capture as an HDR file (see HdrOutput), next to where the normal file would go with the extension of the HDR format.
        /// Null when the normal file is to be saved: no HDR image (not on an HDR display, the frozen screen), the capture was edited, or the encoder failed.
        /// Then a notification tells why.
        /// </summary>
        private async Task<ExportResult> TrySaveHdrAsync(ExportRequest request, string fullPath, bool overwrite, CancellationToken cancellationToken)
        {
            string normalFormat = Path.GetExtension(fullPath)?.TrimStart('.').ToUpperInvariant();
            var (hdrImage, formatId, reasonText) = await GetHdrToSaveAsync(request, cancellationToken).ConfigureAwait(false);
            if (hdrImage == null)
            {
                Log.Info($"Saving {fullPath} instead of an HDR file.");
                NotifyHdrFallback(request, reasonText, normalFormat);
                return null;
            }

            var (hdrPath, failure) = await WriteHdrFileAsync(request, hdrImage, formatId, Path.ChangeExtension(fullPath, "." + formatId), overwrite, cancellationToken).ConfigureAwait(false);
            if (hdrPath == null)
            {
                if (failure != null)
                {
                    NotifyHdrFallback(request, failure, normalFormat);
                }
                return null;
            }

            if (_options?.CopyPathToClipboard ?? CoreConfig.OutputFileCopyPathToClipboard)
            {
                await ClipboardService.Current.SetTextAsync(hdrPath, cancellationToken).ConfigureAwait(false);
            }
            return Saved(request.Metadata, hdrPath);
        }

        /// <summary>
        /// Save with the save dialog (AskForFilename). With HDR image to save, the dialog suggests the HDR format and the HDR file is written when the user keeps it;
        /// another format saves the normal file. Without HDR image the normal file is saved and a notification tells why.
        /// </summary>
        private async Task<ExportResult> SaveAsAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            bool copyPath = _options?.CopyPathToClipboard ?? CoreConfig.OutputFileCopyPathToClipboard;
            HdrImage hdrImage = null;
            string hdrFormatId = null;
            string reasonText = null;
            if (HdrOutput.IsHdrFormat(_options?.HdrFormat))
            {
                (hdrImage, hdrFormatId, reasonText) = await GetHdrToSaveAsync(request, cancellationToken).ConfigureAwait(false);
            }

            string fileName = await request.Ui.PickSaveFileAsync(new SaveFileRequest(captureDetails, format: hdrFormatId), cancellationToken).ConfigureAwait(false);
            if (fileName == null)
            {
                return ExportResult.Declined;
            }

            if (hdrImage != null && string.Equals(ImageIO.FormatForFilename(fileName), hdrFormatId, StringComparison.OrdinalIgnoreCase))
            {
                // The user picked the file: overwrite
                var (hdrPath, failure) = await WriteHdrFileAsync(request, hdrImage, hdrFormatId, fileName, true, cancellationToken).ConfigureAwait(false);
                if (hdrPath != null)
                {
                    if (copyPath)
                    {
                        await ClipboardService.Current.SetTextAsync(hdrPath, cancellationToken).ConfigureAwait(false);
                    }
                    return Saved(captureDetails, hdrPath);
                }
                reasonText = failure;
            }

            // Another format picked, or no HDR file possible
            string savedPath = await SaveToPickedFileAsync(request, fileName, copyPath, cancellationToken).ConfigureAwait(false);
            if (savedPath == null)
            {
                return ExportResult.Declined;
            }
            if (reasonText != null)
            {
                NotifyHdrFallback(request, reasonText, ImageIO.FormatForFilename(savedPath).ToUpperInvariant());
            }
            return Saved(captureDetails, savedPath);
        }

        /// <summary>
        /// The HDR image of the capture (owned by the surface) which can be saved, with the HDR format to write.
        /// No image when the normal file is to be saved; the text (with {0} for the normal format) tells why.
        /// </summary>
        private async Task<(HdrImage HdrImage, string FormatId, string ReasonText)> GetHdrToSaveAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            // IsModified can't tell: a new capture counts as modified until it is saved. The HDR image still shows the capture when the surface
            // has the image it was taken with and nothing drawn on it; the mouse cursor is left out of the HDR file.
            var (hdrImage, unchanged) = await request.Source.UseSurfaceAsync(surface =>
            {
                var surfaceHdrImage = surface.HdrImage;
                return (surfaceHdrImage, surfaceHdrImage != null && surfaceHdrImage.BelongsTo(surface.Image) && surface.Elements.All(element => element is ICursorContainer));
            }, cancellationToken).ConfigureAwait(false);
            if (!unchanged)
            {
                string reason = hdrImage == null ? "the capture has no HDR image (not on an HDR display, or the frozen screen)" : "something was drawn on the capture or it was changed";
                Log.Info($"Not saving an HDR file, {reason}.");
                return (null, null, hdrImage == null ? Texts.Core.HdrNotSavedNoHdr : Texts.Core.HdrNotSavedChanged);
            }

            var encoder = SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true);
            string formatId = HdrOutput.ResolveFormatId(_options?.HdrFormat);
            if (encoder == null || formatId == null || !encoder.CanEncode(formatId))
            {
                Log.Warn($"No encoder can write the HDR format {_options?.HdrFormat} on this system.");
                return (null, null, Texts.Core.HdrNotSavedFailed);
            }
            return (hdrImage, formatId, null);
        }

        /// <summary>
        /// Write the HDR file. No path when it wasn't written: the text (with {0} for the normal format) tells why,
        /// or it's null when the file exists and may not be overwritten (then the normal file is saved, as before).
        /// </summary>
        private static async Task<(string Path, string ReasonText)> WriteHdrFileAsync(ExportRequest request, HdrImage hdrImage, string formatId, string path, bool overwrite,
            CancellationToken cancellationToken)
        {
            var encoder = SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true);
            EncodedImage encoded = null;
            string error = null;
            // Like the normal file: with the details which are only for the user
            if (encoder == null || !encoder.TryEncode(hdrImage, formatId, request.Metadata, true, out encoded, out error))
            {
                Log.Warn($"Couldn't write the HDR file {path}: {error ?? "no HDR encoder"}");
                return (null, Texts.Core.HdrNotSavedFailed);
            }

            try
            {
                string savedPath = await ExportFiles.SaveEncodedAsync(encoded, path, overwrite, cancellationToken).ConfigureAwait(false);
                return (savedPath, null);
            }
            catch (FileAlreadyExistsException ex)
            {
                Log.InfoFormat("Not overwriting the HDR file {0}, saving the normal file.", ex.Message);
                return (null, null);
            }
        }

        /// <summary>
        /// Tell the user why the normal file was saved instead of the HDR file
        /// </summary>
        /// <param name="request">The export</param>
        /// <param name="reasonText">The text which tells why, with {0} for the format of the normal file</param>
        /// <param name="normalFormat">The format of the normal file, e.g. PNG</param>
        private static void NotifyHdrFallback(ExportRequest request, string reasonText, string normalFormat)
        {
            string message = string.Format(reasonText, normalFormat);
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
            return await SaveToPickedFileAsync(request, fileNameWithExtension, copyPathToClipboard, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Save the capture to the file the user picked (asking for the quality if configured), null when the user declined or the file couldn't be written.
        /// </summary>
        private static async Task<string> SaveToPickedFileAsync(ExportRequest request, string fileNameWithExtension, bool copyPathToClipboard, CancellationToken cancellationToken)
        {
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
