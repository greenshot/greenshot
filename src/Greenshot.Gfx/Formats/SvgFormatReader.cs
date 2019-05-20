using System;
using System.Collections.Generic;
using System.IO;
using Dapplo.Log;

namespace Greenshot.Gfx.Formats
{
    /// <summary>
    /// This implements a IImageFormatReader which reads svg
    /// </summary>
    public class SvgFormatReader : IImageFormatReader
    {
        private static readonly LogSource Log = new LogSource();

        /// <inheritdoc/>
        public IEnumerable<string> SupportedFormats { get; } = new []{ "svg" };

        /// <inheritdoc/>
        public IBitmapWithNativeSupport Read(Stream stream, string extension = null)
        {
            stream.Position = 0;
            try
            {
                return SvgBitmap.FromStream(stream);
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Can't load SVG");
            }
            return null;
        }
    }
}
