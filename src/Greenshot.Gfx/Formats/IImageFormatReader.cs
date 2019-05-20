using System.Collections.Generic;
using System.IO;

namespace Greenshot.Gfx.Formats
{
    /// <summary>
    /// Implement this interface to add reading a format to Greenshot
    /// </summary>
    public interface IImageFormatReader
    {
        /// <summary>
        /// This returns all the formats the reader supports
        /// </summary>
        IEnumerable<string> SupportedFormats { get; }
        /// <summary>
        /// This reads a IBitmapWithNativeSupport from a stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <returns>IBitmapWithNativeSupport</returns>
        IBitmapWithNativeSupport Read(Stream stream, string extension = null);
    }
}
