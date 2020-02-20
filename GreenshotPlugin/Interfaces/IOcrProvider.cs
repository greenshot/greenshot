/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace GreenshotPlugin.Interfaces
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

    /// <summary>
    /// Contains the information about a word
    /// </summary>
    public class Word
    {
        /// <summary>
        /// The actual text for the word
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The location of the word
        /// </summary>
        public Rectangle Location { get; set; }
    }

    /// <summary>
    /// Describes a line of words
    /// </summary>
    public class Line
    {
        private Rectangle? _calculatedBounds;

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
        /// An array with words
        /// </summary>
        public Word[] Words { get; }

        /// <summary>
        /// Calculate the bounds of the words
        /// </summary>
        /// <returns>Rectangle</returns>
        private Rectangle CalculateBounds()
        {
            if (Words.Length == 0)
            {
                return Rectangle.Empty;
            }

            var result = Words[0].Location;
            for (var index = 0; index < Words.Length; index++)
            {
                result = Rectangle.Union(result, Words[index].Location);
            }

            return result;
        }

        /// <summary>
        /// Return the calculated bounds for the whole line
        /// </summary>
        public Rectangle CalculatedBounds
        {
            get { return _calculatedBounds ??= CalculateBounds(); }
        }
    }

    /// <summary>
    /// Contains all the information on the OCR result
    /// </summary>
    public class OcrInformation
    {
        public string Text { get; set; }

        public IList<Line> Lines { get; } = new List<Line>();
    }
}
