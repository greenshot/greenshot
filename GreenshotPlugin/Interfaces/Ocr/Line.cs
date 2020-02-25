using System.Drawing;

namespace GreenshotPlugin.Interfaces.Ocr
{
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
        /// <returns>Rectangle</returns>
        private Rectangle CalculateBounds()
        {
            if (Words.Length == 0)
            {
                return Rectangle.Empty;
            }

            var result = Words[0].Bounds;
            for (var index = 0; index < Words.Length; index++)
            {
                result = Rectangle.Union(result, Words[index].Bounds);
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

        /// <summary>
        /// Offset the words with the specified x and y coordinates
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        public void Offset(int x, int y)
        {
            foreach (var word in Words)
            {
                var location = word.Bounds;
                location.Offset(x,y);
                word.Bounds = location;
            }

            _calculatedBounds = null;
            CalculateBounds();
        }
    }
}