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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Greenshot.Base.Interfaces.Ocr
{
    /// <summary>
    /// Contains all the information on the OCR result
    /// </summary>
    public class OcrInformation
    {
        /// <summary>
        /// Check if there is any content
        /// </summary>
        public bool HasContent => Lines.Any();

        /// <summary>
        /// The complete text
        /// </summary>
        public string Text
        {
            get
            {
                // Build the text from the lines, otherwise it's just everything concatenated together
                var text = new StringBuilder();
                foreach (var line in Lines)
                {
                    text.AppendLine(line.Text);
                }

                return text.ToString();
            }
        }

        /// <summary>
        /// The lines of test which the OCR engine found
        /// </summary>
        public IList<Line> Lines { get; } = new List<Line>();

        /// <summary>
        /// Change the offset of the
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        public void Offset(int x, int y)
        {
            foreach (var line in Lines)
            {
                line.Offset(x, y);
            }
        }
    }
}