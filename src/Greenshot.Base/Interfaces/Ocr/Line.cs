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

using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Ocr
{
    /// <summary>
    /// Describes a line of words
    /// </summary>
    public class Line
    {
        private NativeRect? _calculatedBounds;

        /// <summary>
        /// Constructor will preallocate the number of words
        /// </summary>
        /// <param name="wordCount">int</param>
        public Line(int wordCount)
        {
            Words = new Word[wordCount];
            for (int i = 0; i < wordCount; i++)
            {
                Words[i] = new Word();
            }
        }

        /// <summary>
        /// The text of the line
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// An array with words
        /// </summary>
        public Word[] Words { get; }

        /// <summary>
        /// Calculate the bounds of the words
        /// </summary>
        /// <returns>NativeRect</returns>
        private NativeRect CalculateBounds()
        {
            if (Words.Length == 0)
            {
                return NativeRect.Empty;
            }

            var result = Words[0].Bounds;
            for (var index = 0; index < Words.Length; index++)
            {
                result = result.Union(Words[index].Bounds);
            }

            return result;
        }

        /// <summary>
        /// Return the calculated bounds for the whole line
        /// </summary>
        public NativeRect CalculatedBounds
        {
            get { return _calculatedBounds ??= CalculateBounds(); }
        }

        /// <summary>
        /// Offset the words with the specified x and y coordinates
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        public void Offset(int x, int y)
        {
            foreach (var word in Words)
            {
                word.Bounds = word.Bounds.Offset(x, y);
            }

            _calculatedBounds = null;
            CalculateBounds();
        }
    }
}