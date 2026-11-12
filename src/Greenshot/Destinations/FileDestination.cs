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
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Expressions;
using Greenshot.Base.Hdr;
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
        /// What happens when the file exists; default: Overwrite for the capture's own file, the setting for a new name and for FilePath.
        /// </summary>
        public FileExistsAction? IfFileExists { get; set; }

        /// <summary>
        /// The file the flow chose (folder, name and format of the step): saved to, and suggested by the save dialog.
        /// </summary>
        public string FilePath { get; set; }

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
            FileExistsAction ifExists;
            string fullPath;
            // A name made from the template: its counters go up once the file is written
            RenderedFileName generated = null;
            if (_options?.FilePath != null)
            {
                // The file of the flow is a new name: the settings decide, unless the flow does
                fullPath = _options.FilePath;
                ifExists = _options.IfFileExists ?? CoreConfig.OutputFileIfExists;
                outputSettings.Format = ImageIO.FormatForFilename(fullPath);
            }
            else if (captureDetails?.Filename != null)
            {
                // As we save a pre-selected file, allow to overwrite.
                Log.InfoFormat("Using previous filename");
                fullPath = captureDetails.Filename;
                ifExists = _options?.IfFileExists ?? FileExistsAction.Overwrite;
                outputSettings.Format = ImageIO.FormatForFilename(fullPath);
            }
            else
            {
                generated = await CreateNewFilenameAsync(captureDetails, request.Ui, cancellationToken).ConfigureAwait(false);
                fullPath = generated?.FullPath;
                // As we generate a file, the configuration tells us if we allow to overwrite
                ifExists = _options?.IfFileExists ?? CoreConfig.OutputFileIfExists;
            }

            if (fullPath == null)
            {
                // The user didn't fix the invalid filename pattern
                return ExportResult.Declined;
            }

            if (ifExists == FileExistsAction.Ask && !request.Ui.IsInteractive)
            {
                // Nobody can answer the save dialog: a free name instead of failing
                Log.Info("The file may exist and nobody can be asked, a number is added to the name when it does.");
                ifExists = FileExistsAction.AddNumber;
            }
            bool overwrite = ifExists == FileExistsAction.Overwrite;

            if (HdrOutput.IsHdrFormat(_options?.HdrFormat))
            {
                var hdrResult = await TrySaveHdrAsync(request, fullPath, ifExists, cancellationToken).ConfigureAwait(false);
                if (hdrResult != null)
                {
                    generated?.Commit();
                    return hdrResult;
                }
                // Without it the normal file is saved (the log tells why; a recipe can ask before with payload.hdr)
            }

            if (!await ConfirmHdrLossAsync(request, fullPath, cancellationToken).ConfigureAwait(false))
            {
                return ExportResult.Declined;
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
            if (ifExists == FileExistsAction.AddNumber)
            {
                fullPath = NumberedFileNames.FirstFree(fullPath);
            }
            try
            {
                // A file on this computer gets every detail of ExportMetadata, also the private ones
                outputSettings.IncludePrivateMetadata = true;
                fullPath = await ExportFiles.SaveAsync(request.Source, fullPath, overwrite, outputSettings, cancellationToken).ConfigureAwait(false);
                generated?.Commit();
            }
            catch (FileAlreadyExistsException ex)
            {
                // The file exists (Ask, or a numbered name was taken meanwhile): display 'save-as' with the next free name
                Log.InfoFormat("Not overwriting: {0}", ex.Message);
                if (!request.Ui.IsInteractive)
                {
                    return ExportResult.Failed(ex.Message, ex);
                }

                fullPath = await SaveWithDialogAsync(request, copyPath, cancellationToken, NumberedFileNames.FirstFree(fullPath), _options?.OutputSettings).ConfigureAwait(false);
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
                fullPath = await SaveWithDialogAsync(request, copyPath, cancellationToken, _options?.FilePath, _options?.OutputSettings).ConfigureAwait(false);
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
        /// Null when the normal file is to be saved: no HDR content (not on an HDR display, the Source step doesn't capture HDR), the capture's image was changed, or the encoder failed.
        /// Only a failure of the encoder is notified; that there is no HDR content is logged, a recipe which wants to ask checks payload.hdr before.
        /// </summary>
        private async Task<ExportResult> TrySaveHdrAsync(ExportRequest request, string fullPath, FileExistsAction ifExists, CancellationToken cancellationToken)
        {
            string normalFormat = Path.GetExtension(fullPath)?.TrimStart('.').ToUpperInvariant();
            var (formatId, reasonText) = await GetHdrToSaveAsync(request, cancellationToken).ConfigureAwait(false);
            if (formatId == null)
            {
                Log.Info($"Saving {fullPath} instead of an HDR file.");
                NotifyHdrFallback(request, reasonText, normalFormat);
                return null;
            }

            string hdrFile = Path.ChangeExtension(fullPath, "." + formatId);
            if (ifExists == FileExistsAction.AddNumber)
            {
                hdrFile = NumberedFileNames.FirstFree(hdrFile);
            }
            var (hdrPath, failure) = await WriteHdrFileAsync(request, formatId, hdrFile, ifExists == FileExistsAction.Overwrite, cancellationToken).ConfigureAwait(false);
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
        /// Save with the save dialog (AskForFilename). With an HDR file possible, the dialog suggests the HDR format and the HDR file is written when the user keeps it;
        /// another format saves the normal file. Without HDR image the normal file is saved.
        /// </summary>
        private async Task<ExportResult> SaveAsAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var captureDetails = request.Metadata;
            bool copyPath = _options?.CopyPathToClipboard ?? CoreConfig.OutputFileCopyPathToClipboard;
            string hdrFormatId = null;
            string reasonText = null;
            if (HdrOutput.IsHdrFormat(_options?.HdrFormat))
            {
                (hdrFormatId, reasonText) = await GetHdrToSaveAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var (hdrAvailable, hdrRequested) = await request.Source.UseSurfaceAsync(surface =>
                (HdrExport.CanWriteHdr(surface), HdrExport.CanWriteHdr(surface) && surface.IsHdrRequested), cancellationToken).ConfigureAwait(false);
            string suggestedFormat = hdrFormatId ?? (hdrRequested ? WellKnownFileFormats.Jxr : null);
            string fileName = await request.Ui.PickSaveFileAsync(new SaveFileRequest(captureDetails, _options?.FilePath, suggestedFormat, hdrFormatId != null || hdrAvailable), cancellationToken).ConfigureAwait(false);
            if (fileName == null)
            {
                return ExportResult.Declined;
            }

            if (hdrFormatId != null && string.Equals(ImageIO.FormatForFilename(fileName), hdrFormatId, StringComparison.OrdinalIgnoreCase))
            {
                // The user picked the file: overwrite
                var (hdrPath, failure) = await WriteHdrFileAsync(request, hdrFormatId, fileName, true, cancellationToken).ConfigureAwait(false);
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
            if (!await ConfirmHdrLossAsync(request, fileName, cancellationToken).ConfigureAwait(false))
            {
                return ExportResult.Declined;
            }
            string savedPath = await SaveToPickedFileAsync(request, fileName, copyPath, _options?.OutputSettings, cancellationToken).ConfigureAwait(false);
            if (savedPath == null)
            {
                return ExportResult.Declined;
            }
            NotifyHdrFallback(request, reasonText, ImageIO.FormatForFilename(savedPath).ToUpperInvariant());
            return Saved(captureDetails, savedPath);
        }

        /// <summary>
        /// The HDR format to write when the capture can be saved as an HDR file (it has HDR content and its image is unchanged; what is drawn on it
        /// goes into the HDR file at the SDR white level). No format when the normal file is to be saved; the text (with {0} for the normal format) tells why.
        /// </summary>
        private async Task<(string FormatId, string ReasonText)> GetHdrToSaveAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            // IsModified can't tell: a new capture counts as modified until it is saved
            var (hasHdr, canWrite) = await request.Source.UseSurfaceAsync(surface => (surface.HdrState != HdrState.None, HdrExport.CanWriteHdr(surface)),
                cancellationToken).ConfigureAwait(false);
            if (!canWrite)
            {
                // Quietly the normal file: a recipe which wants to ask the user checks payload.hdr before (e.g. with a UserPrompt step)
                Log.Info(hasHdr ? "Not saving an HDR file, the capture's image was changed." : "Not saving an HDR file, the capture has no HDR content (not on an HDR display, or the Source step doesn't capture HDR).");
                return (null, null);
            }

            var encoder = SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true);
            string formatId = HdrOutput.ResolveFormatId(_options?.HdrFormat);
            if (encoder == null || formatId == null || !encoder.CanEncode(formatId))
            {
                Log.Warn($"No encoder can write the HDR format {_options?.HdrFormat} on this system.");
                return (null, Texts.Core.HdrNotSavedFailed);
            }
            return (formatId, null);
        }

        /// <summary>
        /// Write the HDR file. No path when it wasn't written: the text (with {0} for the normal format) tells why,
        /// or it's null when the file exists and may not be overwritten (then the normal file is saved, as before).
        /// </summary>
        private static async Task<(string Path, string ReasonText)> WriteHdrFileAsync(ExportRequest request, string formatId, string path, bool overwrite,
            CancellationToken cancellationToken)
        {
            // Like the normal file: with the details which are only for the user
            var (encoded, reasonText) = await HdrExport.EncodeAsync(request.Source, formatId, true, cancellationToken).ConfigureAwait(false);
            if (encoded == null)
            {
                return (null, reasonText);
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
            catch (Exception ex) when (ex is ExternalException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // The normal file is saved then, with its own handling of the problem (e.g. the save as dialog)
                Log.Warn($"Couldn't write the HDR file {path}, saving the normal file.", ex);
                return (null, Texts.Core.HdrNotSavedFailed);
            }
        }

        /// <summary>
        /// A JPEG XR of a capture whose HDR content was lost (its image was changed, e.g. by an effect) is an 8-bit file, e.g. when an opened HDR file
        /// is saved over: the user who saves it (the editor, the destination picker) is asked first. A flow saves it without asking, the recipe can
        /// check payload.hdr before. False when the user doesn't want the file.
        /// </summary>
        internal static async Task<bool> ConfirmHdrLossAsync(ExportRequest request, string fullPath, CancellationToken cancellationToken)
        {
            if (!string.Equals(ImageIO.FormatForFilename(fullPath), WellKnownFileFormats.Jxr, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            bool lost = await request.Source.UseSurfaceAsync(surface => surface.HdrState == HdrState.Lost, cancellationToken).ConfigureAwait(false);
            if (!lost)
            {
                return true;
            }
            Log.Info($"Saving {fullPath} as an 8-bit JPEG XR, the capture's HDR content was lost.");
            if (!request.ManuallyInitiated || !request.Ui.IsInteractive)
            {
                return true;
            }
            bool? confirmed = await request.Ui.ConfirmAsync(Texts.Core.HdrLostTitle, string.Format(Texts.Core.HdrLostQuestion, Path.GetFileName(fullPath)), false, cancellationToken)
                .ConfigureAwait(false);
            return confirmed != false;
        }

        /// <summary>
        /// Tell the user that the HDR file couldn't be written and the normal file was saved instead
        /// </summary>
        /// <param name="request">The export</param>
        /// <param name="reasonText">The text which tells why, with {0} for the format of the normal file</param>
        /// <param name="normalFormat">The format of the normal file, e.g. PNG</param>
        private static void NotifyHdrFallback(ExportRequest request, string reasonText, string normalFormat)
        {
            if (reasonText == null)
            {
                return;
            }
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
        /// <param name="request">The export</param>
        /// <param name="copyPathToClipboard">Copy the path of the saved file</param>
        /// <param name="cancellationToken">Cancels the export</param>
        /// <param name="suggestedPath">The file the dialog suggests, null for the file of the settings</param>
        /// <param name="outputSettings">The quality settings of the flow (the quality was asked already), null for the settings</param>
        internal static async Task<string> SaveWithDialogAsync(ExportRequest request, bool copyPathToClipboard, CancellationToken cancellationToken,
            string suggestedPath = null, SurfaceOutputSettings outputSettings = null)
        {
            // JPEG XR of a capture with HDR content is the HDR file (see ExportFiles), the dialog says so; when HDR was asked for
            // (a Source step with Hdr, an opened HDR file) it is suggested, only the setting keeps the configured format
            var (hdrAvailable, hdrRequested) = await request.Source.UseSurfaceAsync(surface =>
                (HdrExport.CanWriteHdr(surface), HdrExport.CanWriteHdr(surface) && surface.IsHdrRequested), cancellationToken).ConfigureAwait(false);
            string fileNameWithExtension = await request.Ui.PickSaveFileAsync(
                new SaveFileRequest(request.Metadata, suggestedPath, hdrRequested ? WellKnownFileFormats.Jxr : null, hdrAvailable), cancellationToken).ConfigureAwait(false);
            if (fileNameWithExtension == null)
            {
                return null;
            }
            if (!await ConfirmHdrLossAsync(request, fileNameWithExtension, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }
            return await SaveToPickedFileAsync(request, fileNameWithExtension, copyPathToClipboard, outputSettings, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Save the capture to the file the user picked (with the quality of the flow, else asking for it if configured), null when the user declined or the file couldn't be written.
        /// </summary>
        private static async Task<string> SaveToPickedFileAsync(ExportRequest request, string fileNameWithExtension, bool copyPathToClipboard, SurfaceOutputSettings flowSettings,
            CancellationToken cancellationToken)
        {
            string format = ImageIO.FormatForFilename(fileNameWithExtension);
            var outputSettings = flowSettings != null
                ? new SurfaceOutputSettings(format, flowSettings.JPGQuality, flowSettings.ReduceColors)
                : new SurfaceOutputSettings(format);
            if (flowSettings == null && CoreConfig.OutputFilePromptQuality)
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
        /// Create the file name for a new capture from the configured folder and file name templates, when the name is invalid the user can
        /// fix it in the settings. Commit the result when the file was written, so its counters go up.
        /// </summary>
        internal static async Task<RenderedFileName> CreateNewFilenameAsync(ICaptureDetails captureDetails, IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            Log.InfoFormat("Creating new filename");
            var scope = VariableScope.ForCapture(captureDetails);
            string filepath = CoreConfig.GetOutputDirectory(scope, out string folderProblem);
            if (folderProblem != null)
            {
                // Saved to the default folder this time: say so, instead of letting the screenshot "disappear"
                string message = string.Format(Texts.Core.ErrorOutputfolderFallback, CoreConfig.OutputDirectory, folderProblem, filepath);
                userInteraction.NotifyAsync(new Notification(NotificationKind.Warning, message)).FireAndLog("Screenshot folder fallback notification", Log);
            }
            try
            {
                scope.TargetFolder = filepath;
                return OutputTemplates.RenderFileName(null, FileFormatRegistry.GetPreferredExtensionWithDot(CoreConfig.OutputFileFormat), scope);
            }
            catch (ArgumentException)
            {
                // configured filename or path not valid, show error message...
                Log.InfoFormat("Generated path or filename not valid: {0}, {1}", filepath, OutputTemplates.FileNameTemplate);
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
