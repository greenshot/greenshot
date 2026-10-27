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

#nullable disable

using Greenshot.Linux.Core.Capturing;
using CaptureMode = Greenshot.Linux.Recipes.CaptureMode;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Recipes
{
    /// <summary>
    /// The app for the recipe tests: records what the steps ask for, the selection crops to 10x10
    /// </summary>
    public sealed class FakeRecipeHost : IRecipeHost
    {
        public int Selections { get; private set; }

        public CaptureMode? LastSelectionMode { get; private set; }

        public List<string> Calls { get; } = new();

        public List<IDestination> DestinationList { get; } = new();

        public List<(IReadOnlyList<ExportOutcome> Outcomes, string Error)> Notifications { get; } = new();

        public IReadOnlyList<OcrLine> OcrLines { get; set; } = Array.Empty<OcrLine>();

        public string PromptAnswer { get; set; }

        public FlyoutChoice FlyoutAnswer { get; set; }

        public SelectionOutcome SelectionAnswer { get; set; } = new SelectionOutcome(CaptureMode.Region, null);

        public static ICapturePayload Payload(int width = 100, int height = 50, string title = null) =>
            new CapturePayload(new Capture(new SKBitmap(width, height), new CaptureDetails { Title = title }));

        public Task<ICapturePayload> CaptureScreenAsync(bool forSelection, bool captureMouse, CancellationToken cancellationToken)
        {
            Calls.Add($"screen:{forSelection}:{captureMouse}");
            return Task.FromResult(Payload(200, 100));
        }

        public Task<ICapturePayload> CaptureWindowAsync(string titlePattern, bool matchCase, bool captureMouse, CancellationToken cancellationToken)
        {
            Calls.Add($"window:{titlePattern}");
            return Task.FromResult(Payload(80, 60, "Window"));
        }

        public Task<ICapturePayload> CaptureLastRegionAsync(bool captureMouse, CancellationToken cancellationToken)
        {
            Calls.Add("lastregion");
            return Task.FromResult(Payload(30, 30));
        }

        public Task<ICapturePayload> CaptureClipboardAsync(CancellationToken cancellationToken)
        {
            Calls.Add("clipboard");
            return Task.FromResult(Payload());
        }

        public Task<ICapturePayload> OpenFileAsync(string path, CancellationToken cancellationToken)
        {
            Calls.Add($"file:{path}");
            return Task.FromResult(File.Exists(path) ? Payload() : null);
        }

        public Task<SelectionOutcome> SelectAsync(ICapturePayload payload, CaptureMode mode, bool allowWindowSnapping, CancellationToken cancellationToken)
        {
            Selections++;
            LastSelectionMode = mode;
            if (SelectionAnswer != null)
            {
                payload.RawCapture = new Capture(new SKBitmap(10, 10), payload.RawCapture.Details);
            }

            return Task.FromResult(SelectionAnswer);
        }

        public bool IsSelecting => false;

        public void BringSelectionToFront()
        {
        }

        public IReadOnlyList<IDestination> Destinations => DestinationList;

        public Task<ExportResult> OpenInEditorAsync(ICapturePayload payload, IImageEditor current, CancellationToken cancellationToken)
        {
            Calls.Add($"editor:{payload.Surface?.Elements.Count ?? 0}");
            payload.RetainSurfaceForEditor = true;
            return Task.FromResult(ExportResult.Done());
        }

        public Task<IReadOnlyList<OcrLine>> RecognizeTextAsync(SKBitmap image, CancellationToken cancellationToken) => Task.FromResult(OcrLines);

        public void PlaySound(string soundFile) => Calls.Add($"sound:{soundFile}");

        public Task<bool> NotifyExportsAsync(IReadOnlyList<ExportOutcome> outcomes, string error, CancellationToken cancellationToken)
        {
            Notifications.Add((outcomes, error));
            return Task.FromResult(true);
        }

        public Task<string> PromptAsync(string title, string message, IReadOnlyList<PromptChoice> choices, SKBitmap preview, int timeoutSeconds, CancellationToken cancellationToken) =>
            Task.FromResult(PromptAnswer);

        public Task<FlyoutChoice> ShowDestinationFlyoutAsync(string title, SKBitmap preview, IReadOnlyList<IDestination> destinations, IReadOnlyList<CaptureRecipe> recipes,
            string lastError, int timeoutSeconds, CancellationToken cancellationToken) => Task.FromResult(FlyoutAnswer);
    }

    /// <summary>
    /// A destination which records its exports
    /// </summary>
    public sealed class StubDestination(string designation, Func<ExportResult> result = null) : IDestination
    {
        public List<Capture> Exports { get; } = new();

        public string Designation { get; } = designation;

        public string DisplayName => Designation;

        public int Priority => 10;

        public bool IsAvailable => true;

        public Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
        {
            Exports.Add(capture);
            return Task.FromResult(result?.Invoke() ?? ExportResult.Done());
        }
    }

    /// <summary>
    /// Sets the fake host for a test and takes it away after it
    /// </summary>
    public sealed class HostScope : IDisposable
    {
        private readonly IRecipeHost _previous = RecipeServices.Host;
        private readonly IUserInteraction _previousInteraction = RecipeServices.UserInteraction;

        public HostScope(FakeRecipeHost host = null)
        {
            Host = host ?? new FakeRecipeHost();
            RecipeServices.Host = Host;
        }

        public FakeRecipeHost Host { get; }

        public void Dispose()
        {
            RecipeServices.Host = _previous;
            RecipeServices.UserInteraction = _previousInteraction;
        }
    }
}
