using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GreenshotPlugin.Interfaces.Ocr
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
                line.Offset(x,y);
            }
        }
    }
}