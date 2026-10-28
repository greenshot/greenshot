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

using System.Text.RegularExpressions;
using Avalonia.Threading;
using Greenshot.Linux.Capturing;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Destinations;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Serialization;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Ai;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;
using Capture = Greenshot.Linux.Core.Capturing.Capture;
using CaptureMode = Greenshot.Linux.Core.Capturing.CaptureMode;
using RecipeCaptureMode = Greenshot.Linux.Recipes.CaptureMode;


namespace Greenshot.Linux.RecipeUi;

/// <summary>
/// The desktop for the recipe steps: capturing with the capture service, the capture screen, the editor, the destinations,
/// OCR, the sounds, the windows of the prompts and the notification
/// </summary>
internal sealed class AppRecipeHost(GreenshotController controller) : IRecipeHost, IUserInteraction
{
    private const string SnapshotKey = "ScreenSnapshot";
    private int _selecting;

    private ScreenCaptureService Service => controller.CaptureService ?? throw new InvalidOperationException("No way to capture the screen was found, see the log.");

    #region Capturing

    /// <summary>
    /// The picture of all screens; for an AI tool with the windows of excluded applications blacked out
    /// </summary>
    private async Task<ScreenSnapshot> SnapshotAsync(bool captureMouse, CancellationToken cancellationToken)
    {
        var snapshot = await Service.SnapshotAsync(cancellationToken, captureMouse, applyDelay: false).ConfigureAwait(false);
        if (!AiToolCapture.IsActive)
        {
            return snapshot;
        }

        try
        {
            int redacted = await AiToolCapture.RedactExcludedWindowsAsync(snapshot, controller.Compositor).ConfigureAwait(false);
            if (redacted > 0)
            {
                Log.Info($"Blacked out {redacted} window(s) of applications excluded from AI tools.");
            }

            return snapshot;
        }
        catch
        {
            snapshot.Dispose();
            throw;
        }
    }

    public async Task<ICapturePayload> CaptureRegionAsync(SKRectI region, bool captureMouse, CancellationToken cancellationToken)
    {
        using var snapshot = await SnapshotAsync(captureMouse, cancellationToken).ConfigureAwait(false);
        var pixels = snapshot.ToPixels(new LayoutRect(region.Left, region.Top, region.Width, region.Height));
        if (pixels.IsEmpty)
        {
            return null!;
        }

        return new CapturePayload(await Service.CreateCaptureAsync(snapshot, pixels, CaptureMode.Region).ConfigureAwait(false));
    }

    public async Task<ICapturePayload> CaptureGivenWindowAsync(WindowInfo window, bool captureMouse, CancellationToken cancellationToken)
    {
        using var snapshot = await SnapshotAsync(captureMouse, cancellationToken).ConfigureAwait(false);
        var pixels = snapshot.ToPixels(window.Bounds);
        if (pixels.IsEmpty)
        {
            return null!;
        }

        return new CapturePayload(await Service.CreateCaptureAsync(snapshot, pixels, CaptureMode.ActiveWindow, window.Title).ConfigureAwait(false));
    }

    public async Task<ICapturePayload> CaptureScreenAsync(bool forSelection, bool captureMouse, CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(captureMouse, cancellationToken).ConfigureAwait(false);
        if (!forSelection)
        {
            using (snapshot)
            {
                return new CapturePayload(await Service.CreateFullScreenCaptureAsync(snapshot).ConfigureAwait(false));
            }
        }

        // The selection cuts the capture out of the snapshot, until then the capture is the whole screen
        var payload = new CapturePayload(new Capture(snapshot.Image.Copy(), new CaptureDetails { DateTime = snapshot.TakenAt, Mode = CaptureMode.Region }));
        payload.Metadata[SnapshotKey] = snapshot;
        return payload;
    }

    public async Task<ICapturePayload> CaptureWindowAsync(string titlePattern, bool matchCase, bool captureMouse, CancellationToken cancellationToken)
    {
        var compositor = controller.Compositor;
        WindowInfo? window = null;
        if (compositor.KnowsWindows)
        {
            if (string.IsNullOrEmpty(titlePattern))
            {
                window = await compositor.GetActiveWindowAsync().ConfigureAwait(false);
            }
            else
            {
                var regex = new Regex(titlePattern, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                window = (await compositor.GetVisibleWindowsAsync().ConfigureAwait(false)).FirstOrDefault(w => regex.IsMatch(w.Title));
                if (window == null)
                {
                    return null!;
                }
            }
        }

        if (window == null)
        {
            // The desktop doesn't tell the windows: the user picks the window, like the capture of the active window does
            var payload = await CaptureScreenAsync(true, captureMouse, cancellationToken).ConfigureAwait(false);
            if (await SelectAsync(payload, RecipeCaptureMode.Window, true, cancellationToken).ConfigureAwait(false) == null)
            {
                payload.Dispose();
                return null!;
            }

            return payload;
        }

        using var snapshot = await SnapshotAsync(captureMouse, cancellationToken).ConfigureAwait(false);
        var capture = await Service.CreateCaptureAsync(snapshot, snapshot.ToPixels(window.Bounds), CaptureMode.ActiveWindow, window.Title).ConfigureAwait(false);
        return new CapturePayload(capture);
    }

    public async Task<ICapturePayload> CaptureLastRegionAsync(bool captureMouse, CancellationToken cancellationToken)
    {
        var settings = controller.Settings;
        if (settings.LastCapturedRegion.IsEmpty)
        {
            // No region yet: the user selects one, like the capture of the last region on Windows
            var payload = await CaptureScreenAsync(true, captureMouse, cancellationToken).ConfigureAwait(false);
            if (await SelectAsync(payload, RecipeCaptureMode.Region, true, cancellationToken).ConfigureAwait(false) == null)
            {
                payload.Dispose();
                return null!;
            }

            return payload;
        }

        using var snapshot = await SnapshotAsync(captureMouse, cancellationToken).ConfigureAwait(false);
        return new CapturePayload(await Service.CreateCaptureAsync(snapshot, snapshot.ToPixels(settings.LastCapturedRegion), CaptureMode.LastRegion).ConfigureAwait(false));
    }

    public async Task<ICapturePayload> CaptureClipboardAsync(CancellationToken cancellationToken)
    {
        var data = await SystemClipboard.GetImageAsync().ConfigureAwait(false);
        var image = data != null ? SKBitmap.Decode(data) : null;
        return image == null ? null! : new CapturePayload(new Capture(image, new CaptureDetails { Title = "Clipboard", Mode = CaptureMode.Clipboard }));
    }

    public Task<ICapturePayload> OpenFileAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult<ICapturePayload>(null!);
        }

        var details = new CaptureDetails { Title = Path.GetFileNameWithoutExtension(path), DateTime = File.GetLastWriteTime(path), Mode = CaptureMode.File };
        if (Path.GetExtension(path).Equals(GreenshotFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            // A .greenshot file keeps its elements on the surface
            var content = GreenshotFile.Load(path);
            var payload = new CapturePayload(new Capture(content.Image.Copy(), details)) { Surface = new EditorSurface(content.Image) };
            if (content.ElementsJson != null)
            {
                payload.Surface.LoadElements(content.ElementsJson);
            }

            return Task.FromResult<ICapturePayload>(payload);
        }

        var image = SKBitmap.Decode(path);
        return Task.FromResult<ICapturePayload>(image == null ? null! : new CapturePayload(new Capture(image, details)));
    }

    #endregion

    #region Selection

    public bool IsSelecting => Volatile.Read(ref _selecting) > 0;

    /// <summary>
    /// The capture screen is above everything already
    /// </summary>
    public void BringSelectionToFront()
    {
    }

    public async Task<SelectionOutcome> SelectAsync(ICapturePayload payload, RecipeCaptureMode mode, bool allowWindowSnapping, CancellationToken cancellationToken)
    {
        if (!payload.Metadata.TryGetValue(SnapshotKey, out var value) || value is not ScreenSnapshot snapshot)
        {
            return new SelectionOutcome(mode, payload.Details?.Title);
        }

        Interlocked.Increment(ref _selecting);
        try
        {
            var compositor = controller.Compositor;
            var windows = allowWindowSnapping && compositor.KnowsWindows ? await compositor.GetVisibleWindowsAsync().ConfigureAwait(false) : Array.Empty<WindowInfo>();
            var coreMode = mode switch
            {
                RecipeCaptureMode.Window => CaptureMode.Window,
                RecipeCaptureMode.Text => CaptureMode.Text,
                _ => CaptureMode.Region,
            };
            var selection = await Dispatcher.UIThread.InvokeAsync(
                () => CaptureOverlay.SelectAsync(snapshot, coreMode, windows, compositor, controller.PlatformScreens, controller.Settings));
            if (selection == null)
            {
                return null!;
            }

            var capture = await Service.CreateCaptureAsync(snapshot, selection.Pixels, selection.Mode, selection.Title).ConfigureAwait(false);
            payload.RawCapture?.Dispose();
            payload.RawCapture = capture;
            payload.Surface?.Dispose();
            payload.Surface = null;
            payload.Metadata.Remove(SnapshotKey);
            snapshot.Dispose();
            return new SelectionOutcome(selection.Mode == CaptureMode.Text ? RecipeCaptureMode.Text : selection.Mode == CaptureMode.Window ? RecipeCaptureMode.Window : RecipeCaptureMode.Region,
                selection.Title);
        }
        finally
        {
            Interlocked.Decrement(ref _selecting);
        }
    }

    #endregion

    #region Destinations, editor, OCR, sound

    public IReadOnlyList<IDestination> Destinations => controller.RecipeDestinations;

    public Task<ExportResult> OpenInEditorAsync(ICapturePayload payload, IImageEditor current, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var details = payload.Details ?? new CaptureDetails();
            if (current is Editing.EditorWindow editor)
            {
                // The editor which ran the recipe gets the result
                editor.ReplaceContent(payload.EnsureSurface());
                return ExportResult.Done();
            }

            // The editor takes the surface with its elements, the flow must not dispose it
            var surface = payload.EnsureSurface();
            payload.RetainSurfaceForEditor = true;
            controller.ShowEditor(surface, details, null);
            return ExportResult.Done();
        }).GetTask();

    public async Task<IReadOnlyList<OcrLine>> RecognizeTextAsync(SKBitmap image, CancellationToken cancellationToken)
    {
        if (!TesseractOcr.IsAvailable)
        {
            return Array.Empty<OcrLine>();
        }

        var languages = TesseractOcr.ChooseLanguages(controller.Settings.OcrLanguages, Texts.Current, await TesseractOcr.GetLanguagesAsync().ConfigureAwait(false));
        return await TesseractOcr.RecognizeAsync(image, languages, cancellationToken).ConfigureAwait(false);
    }

    public void PlaySound(string soundFile)
    {
        if (string.IsNullOrWhiteSpace(soundFile))
        {
            ProcessRunner.StartDetached("canberra-gtk-play", "-i", "camera-shutter", "-d", "Greenshot");
        }
        else if (ProcessRunner.IsOnPath("pw-play"))
        {
            ProcessRunner.StartDetached("pw-play", soundFile);
        }
        else
        {
            ProcessRunner.StartDetached("paplay", soundFile);
        }
    }

    #endregion

    #region Windows

    public async Task<bool> NotifyExportsAsync(IReadOnlyList<ExportOutcome> outcomes, string error, CancellationToken cancellationToken)
    {
        await controller.ReportRecipeAsync(outcomes, error).ConfigureAwait(false);
        return true;
    }

    public Task<string> PromptAsync(string title, string message, IReadOnlyList<PromptChoice> choices, SKBitmap preview, int timeoutSeconds, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() => RecipeWindows.PromptAsync(title, message, choices, preview, timeoutSeconds, cancellationToken));

    public Task<FlyoutChoice> ShowDestinationFlyoutAsync(string title, SKBitmap preview, IReadOnlyList<IDestination> destinations, IReadOnlyList<CaptureRecipe> recipes,
        string lastError, int timeoutSeconds, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() => RecipeWindows.FlyoutAsync(title, preview, destinations, recipes, lastError, timeoutSeconds, cancellationToken));

    #endregion

    #region IUserInteraction

    public bool IsInteractive => true;

    public async Task<string> PickSaveFileAsync(string suggestedPath, string format, CancellationToken cancellationToken)
    {
        return (await new SaveDialog(controller.FileChooser).AskAsync(suggestedPath).ConfigureAwait(false))!;
    }

    public async Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, Capture capture, CancellationToken cancellationToken) =>
        (await Dispatcher.UIThread.InvokeAsync(() => PickerWindow.PickAsync(capture, choices)))!;

    public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() => RecipeWindows.ConfirmAsync(title, message));

    public Task NotifyAsync(NotificationKind kind, string message, string link = null!) =>
        controller.NotifyRecipeAsync(kind, message, link);

    #endregion
}

/// <summary>
/// The Avalonia UI thread for the recipe engine
/// </summary>
internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) => Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Normal, cancellationToken).GetTask();

    public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default) => Dispatcher.UIThread.InvokeAsync(func, DispatcherPriority.Normal, cancellationToken).GetTask();

    public Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default) => Dispatcher.UIThread.InvokeAsync(func, DispatcherPriority.Normal);
}
