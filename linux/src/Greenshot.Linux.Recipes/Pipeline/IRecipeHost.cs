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
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Ocr;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// What the selection of a capture gave: the mode it ended in (Text when the user switched to text) and the window title
    /// </summary>
    public sealed record SelectionOutcome(CaptureMode Mode, string WindowTitle);

    /// <summary>
    /// The choice of the destination flyout: a destination, the editor, or another recipe to forward the capture to
    /// </summary>
    public sealed record FlyoutChoice(IDestination Destination, CaptureRecipe Recipe, bool OpenEditor);

    /// <summary>
    /// The app: what the steps need of the desktop (capturing, the selection, the editor, the destinations, sound, windows).
    /// On Windows the steps call these parts of Greenshot directly.
    /// </summary>
    public interface IRecipeHost
    {
        /// <summary>
        /// All screens; the payload keeps what the selection needs (forSelection)
        /// </summary>
        Task<ICapturePayload> CaptureScreenAsync(bool forSelection, bool captureMouse, CancellationToken cancellationToken);

        /// <summary>
        /// The active window, or the first window whose title matches the pattern; null when there is none
        /// </summary>
        Task<ICapturePayload> CaptureWindowAsync(string titlePattern, bool matchCase, bool captureMouse, CancellationToken cancellationToken);

        Task<ICapturePayload> CaptureLastRegionAsync(bool captureMouse, CancellationToken cancellationToken);

        /// <summary>
        /// The image of the clipboard, null when there is none
        /// </summary>
        Task<ICapturePayload> CaptureClipboardAsync(CancellationToken cancellationToken);

        /// <summary>
        /// An image or .greenshot file, null when it can't be read
        /// </summary>
        Task<ICapturePayload> OpenFileAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// The capture screen on the screen of the payload: crops it to the selection; null when canceled
        /// </summary>
        Task<SelectionOutcome> SelectAsync(ICapturePayload payload, CaptureMode mode, bool allowWindowSnapping, CancellationToken cancellationToken);

        bool IsSelecting { get; }

        void BringSelectionToFront();

        /// <summary>
        /// The destinations which are switched on, the picker and the editor included
        /// </summary>
        IReadOnlyList<IDestination> Destinations { get; }

        /// <summary>
        /// Opens the capture in the editor with its annotations
        /// </summary>
        Task<ExportResult> OpenInEditorAsync(ICapturePayload payload, IImageEditor current, CancellationToken cancellationToken);

        Task<IReadOnlyList<OcrLine>> RecognizeTextAsync(SKBitmap image, CancellationToken cancellationToken);

        void PlaySound(string soundFile);

        /// <summary>
        /// The notification of the exports, with the error of the flow; false when none was shown
        /// </summary>
        Task<bool> NotifyExportsAsync(IReadOnlyList<ExportOutcome> outcomes, string error, CancellationToken cancellationToken);

        /// <summary>
        /// The key of the chosen choice, null when the prompt was closed
        /// </summary>
        Task<string> PromptAsync(string title, string message, IReadOnlyList<PromptChoice> choices, SKBitmap preview, int timeoutSeconds,
            CancellationToken cancellationToken);

        /// <summary>
        /// The destination flyout: null when it was closed
        /// </summary>
        Task<FlyoutChoice> ShowDestinationFlyoutAsync(string title, SKBitmap preview, IReadOnlyList<IDestination> destinations,
            IReadOnlyList<CaptureRecipe> recipes, string lastError, int timeoutSeconds, CancellationToken cancellationToken);
    }
}
