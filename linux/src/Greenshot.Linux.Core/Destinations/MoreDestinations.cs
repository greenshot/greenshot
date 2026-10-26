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

using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.DBus;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Core.Output;

namespace Greenshot.Linux.Core.Destinations;

/// <summary>
/// Files for other programs (the mail program, the printer): in a folder only the user can read, removed after a day
/// </summary>
public static class ExportFiles
{
    public static string Folder => Path.Combine(XdgDirectories.RuntimeDir, "greenshot", "export");

    public static async Task<string> WriteAsync(CoreSettings settings, Capture capture, string? extension = null, byte[]? data = null)
    {
        Directory.CreateDirectory(Folder);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(Folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        RemoveOld();
        var format = extension ?? ImageFormats.Normalize(settings.OutputFileFormat);
        // The filename pattern without counting up ${NUM}
        var pattern = new FilenamePattern(() => settings.OutputFileIncrementingNumber);
        var name = pattern.GetFilename(settings.OutputFileFilenamePattern, format, capture.Details);
        var path = FileDestination.MakeUnique(Path.Combine(Folder, name));
        data ??= ImageFormats.Encode(capture.Image, format, settings.OutputFileJpegQuality);
        await File.WriteAllBytesAsync(path, data).ConfigureAwait(false);
        return path;
    }

    private static void RemoveOld()
    {
        foreach (var file in Directory.EnumerateFiles(Folder))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-1))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // Still in use, next time
            }
        }
    }
}

/// <summary>
/// Copies the recognized text to the clipboard, the Win10OcrDestination of Windows with tesseract
/// </summary>
public sealed class OcrDestination(CoreSettings settings) : IDestination
{
    public string Designation => WellKnownDestinations.Ocr;

    public string DisplayName => Texts.Linux("ocr_destination");

    public int Priority => 3;

    public bool IsAvailable => TesseractOcr.IsAvailable;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        if (!TesseractOcr.IsAvailable)
        {
            return ExportResult.Failed(Texts.Linux("ocr_missing"));
        }

        try
        {
            var text = await RecognizeTextAsync(settings, capture, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
            {
                return ExportResult.Done(message: Texts.Linux("ocr_none"));
            }

            await SystemClipboard.SetTextAsync(text).ConfigureAwait(false);
            return ExportResult.Done(message: Texts.Linux("ocr_copied", Preview(text)));
        }
        catch (InvalidOperationException ex)
        {
            Log.Error("Text recognition failed", ex);
            return ExportResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// The text of the capture, one line per recognized line
    /// </summary>
    public static async Task<string> RecognizeTextAsync(CoreSettings settings, Capture capture, CancellationToken cancellationToken = default)
    {
        var languages = TesseractOcr.ChooseLanguages(settings.OcrLanguages, Texts.Current, await TesseractOcr.GetLanguagesAsync().ConfigureAwait(false));
        var lines = await TesseractOcr.RecognizeAsync(capture.Image, languages, cancellationToken).ConfigureAwait(false);
        return string.Join(Environment.NewLine, lines.Select(l => l.Text));
    }

    internal static string Preview(string text)
    {
        var oneLine = text.ReplaceLineEndings(" ");
        return oneLine.Length > 80 ? oneLine[..80] + "…" : oneLine;
    }
}

/// <summary>
/// Copies the text of the QR codes and barcodes in the capture, the ZxingQrDestination of the Zxing plugin
/// </summary>
public sealed class QrCodeDestination : IDestination
{
    public string Designation => WellKnownDestinations.QrCode;

    public string DisplayName => Texts.Linux("qr_destination");

    public int Priority => 4;

    public bool IsAvailable => true;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var codes = Barcodes.Barcodes.Detect(capture.Image);
        if (codes.Count == 0)
        {
            return ExportResult.Done(message: Texts.Linux("qr_none"));
        }

        var text = string.Join(Environment.NewLine, codes.Select(c => c.Text));
        await SystemClipboard.SetTextAsync(text).ConfigureAwait(false);
        return ExportResult.Done(message: Texts.Linux("qr_copied", OcrDestination.Preview(text))) with
        {
            Link = codes.FirstOrDefault(c => c.IsUrl)?.Text,
        };
    }
}

/// <summary>
/// Prints through the print dialog of the desktop (Print portal), without it with lp to the default printer.
/// The page is laid out like the PrintHelper of Windows does.
/// </summary>
public sealed class PrinterDestination(CoreSettings settings, PrintPortal? portal, Func<PrintOptions, Task<PrintOptions?>>? askOptions = null)
    : IDestination
{
    public string Designation => WellKnownDestinations.Printer;

    public string DisplayName => Texts.Settings("destination_printer");

    public int Priority => 5;

    public bool IsAvailable => portal != null || ProcessRunner.IsOnPath("lp");

    public PrintOptions OptionsFromSettings(CaptureDetails details) => new(
        settings.OutputPrintAllowShrink,
        settings.OutputPrintAllowEnlarge,
        settings.OutputPrintAllowRotate,
        settings.OutputPrintCenter,
        settings.OutputPrintInverted,
        settings.OutputPrintGrayscale,
        settings.OutputPrintMonochrome,
        settings.OutputPrintFooter
            ? new FilenamePattern(() => settings.OutputFileIncrementingNumber) { Culture = Texts.Culture }.Fill(settings.OutputPrintFooterPattern, details, filenameSafe: false)
            : null);

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var options = OptionsFromSettings(capture.Details);
        if (settings.OutputPrintPromptOptions && askOptions != null)
        {
            var chosen = await askOptions(options).ConfigureAwait(true);
            if (chosen == null)
            {
                return ExportResult.Aborted;
            }

            options = chosen;
        }

        var title = capture.Details.Title ?? "Greenshot";
        try
        {
            var page = PageSetup.A4;
            uint? token = null;
            if (portal != null)
            {
                var prepared = await portal.PrepareAsync(title, cancellationToken).ConfigureAwait(true);
                if (prepared == null)
                {
                    return ExportResult.Aborted;
                }

                (page, var printToken) = prepared.Value;
                token = printToken;
            }

            var pdf = PrintLayout.CreatePdf(capture.Image, page, options, title);
            var path = await ExportFiles.WriteAsync(settings, capture, "pdf", pdf).ConfigureAwait(true);
            bool printed = token != null
                ? await portal!.PrintAsync(title, path, token.Value, cancellationToken).ConfigureAwait(true)
                : (await ProcessRunner.RunAsync("lp", ["-t", title, path], timeout: TimeSpan.FromSeconds(30), cancellationToken: cancellationToken)
                    .ConfigureAwait(true)).ExitCode == 0;
            return printed ? ExportResult.Done(message: Texts.Linux("print_sent")) : ExportResult.Failed(Texts.Linux("print_failed", title));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Tmds.DBus.Protocol.DBusExceptionBase)
        {
            Log.Error("Printing failed", ex);
            return ExportResult.Failed(Texts.Linux("print_failed", ex.Message));
        }
    }
}

/// <summary>
/// A new e-mail with the capture attached: Email portal, without it xdg-email
/// </summary>
public sealed class EmailDestination(CoreSettings settings, EmailPortal? portal) : IDestination
{
    public string Designation => WellKnownDestinations.Email;

    public string DisplayName => Texts.Settings("destination_email");

    public int Priority => 6;

    public bool IsAvailable => portal != null || ProcessRunner.IsOnPath("xdg-email");

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var subject = capture.Details.Title ?? "Greenshot";
        try
        {
            var path = await ExportFiles.WriteAsync(settings, capture).ConfigureAwait(false);
            bool opened = portal != null
                ? await portal.ComposeAsync(subject, path, cancellationToken).ConfigureAwait(false)
                : (await ProcessRunner.RunAsync("xdg-email", ["--subject", subject, "--attach", path], timeout: TimeSpan.FromSeconds(30),
                    cancellationToken: cancellationToken, captureOutput: false).ConfigureAwait(false)).ExitCode == 0;
            return opened ? ExportResult.Done(message: Texts.Linux("email_opened")) : ExportResult.Failed(Texts.Linux("email_failed", subject));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Tmds.DBus.Protocol.DBusExceptionBase)
        {
            Log.Error("Opening the e-mail failed", ex);
            return ExportResult.Failed(Texts.Linux("email_failed", ex.Message));
        }
    }
}
