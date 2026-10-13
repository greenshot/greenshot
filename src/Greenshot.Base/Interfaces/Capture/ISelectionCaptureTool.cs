/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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


using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// A capture tool which takes its own image of the selection after the CaptureWindow closed, instead of the frozen capture of the screen
    /// (e.g. a tool which changes the selected window while it captures it).
    /// </summary>
    public interface ISelectionCaptureTool : ICaptureTool
    {
        /// <summary>
        /// Capture the selection, called on a pool thread after the CaptureWindow closed
        /// </summary>
        /// <param name="selection">SelectionResult with the selected window</param>
        /// <param name="screenArea">NativeRect with the selection in screen coordinates</param>
        /// <param name="ui">IUiDispatcher to show UI</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Bitmap which replaces the capture, null when the user cancelled</returns>
        Task<Bitmap> CaptureSelectionAsync(SelectionResult selection, NativeRect screenArea, IUiDispatcher ui, CancellationToken cancellationToken);
    }
}
