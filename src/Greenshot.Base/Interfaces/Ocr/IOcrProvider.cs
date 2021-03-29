/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading.Tasks;

namespace Greenshot.Base.Interfaces.Ocr
{
    /// <summary>
    /// This interface describes something that can do OCR of a bitmap
    /// </summary>
    public interface IOcrProvider
    {
        /// <summary>
        /// Start the actual OCR
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>OcrInformation</returns>
        Task<OcrInformation> DoOcrAsync(Image image);

        /// <summary>
        /// Start the actual OCR
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <returns>OcrInformation</returns>
        Task<OcrInformation> DoOcrAsync(ISurface surface);
    }
}