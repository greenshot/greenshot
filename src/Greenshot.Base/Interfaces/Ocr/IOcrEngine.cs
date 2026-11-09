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

using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Interfaces.Ocr
{
    /// <summary>
    /// One way to recognize text, e.g. Windows OCR or one of a plugin. Greenshot uses the one of the setting OcrEngine
    /// through <see cref="IOcrProvider"/>, the others aren't called.
    /// </summary>
    public interface IOcrEngine : IOcrProvider
    {
        /// <summary>
        /// The id kept in the setting, never changes (e.g. Windows, RapidOcr)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name shown in the settings
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// True when it can recognize text now; false e.g. while files it needs aren't downloaded
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Get it ready, asking the user first for anything like a download. Called when the user picks this engine, and when it
        /// is the chosen engine but can't recognize text (e.g. its files were deleted): then the question offers Windows OCR instead.
        /// </summary>
        /// <param name="owner">the window handle the questions belong to, 0 for none</param>
        /// <param name="isChosen">true: it is already the chosen engine and isn't ready, false: the user picks it now</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>false when the user said no or it can't be used: the setting goes back to the engine before (Windows OCR when it was the chosen one)</returns>
        Task<bool> PrepareAsync(System.IntPtr owner, bool isChosen, CancellationToken cancellationToken);
    }
}
