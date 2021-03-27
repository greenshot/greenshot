using System.Drawing;

namespace GreenshotPlugin.Interfaces.Ocr
{
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
        /// The bounds of the word
        /// </summary>
        public Rectangle Bounds { get; set; }
    }
}