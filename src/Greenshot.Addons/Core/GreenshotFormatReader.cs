using System;
using System.Collections.Generic;
using System.IO;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;
using Greenshot.Gfx.Formats;

namespace Greenshot.Addons.Core
{
    public class GreenshotFormatReader : IImageFormatReader
    {
        private readonly Func<ISurface> _surfaceFactory;

        public GreenshotFormatReader(Func<ISurface> surfaceFactory)
        {
            _surfaceFactory = surfaceFactory;
            ImageOutput.SurfaceFactory = surfaceFactory;
        }

        public IEnumerable<string> SupportedFormats { get; } = new[] { "greenshot" };

        public IBitmapWithNativeSupport Read(Stream stream, string extension = null)
        {
            // TODO: Create surface from stream
            var surface = _surfaceFactory();
            surface.LoadElementsFromStream(stream);
            return surface.GetBitmapForExport();
        }
    }
}
