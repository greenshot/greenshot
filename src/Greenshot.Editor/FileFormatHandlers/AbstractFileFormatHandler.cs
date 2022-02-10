using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormatHandlers
{
    public abstract class AbstractFileFormatHandler : IFileFormatHandler
    {
        /// <inheritdoc />
        public IDictionary<FileFormatHandlerActions, IList<string>> SupportedExtensions { get; } = new Dictionary<FileFormatHandlerActions, IList<string>>();

        /// <inheritdoc />
        public virtual int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return 0;
        }

        public abstract bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension);
        public abstract bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap);

        /// <summary>
        /// Default implementation taking the TryLoadFromStream image and placing it in an ImageContainer 
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="drawableContainer">IDrawableContainer out</param>
        /// <param name="parentSurface">ISurface</param>
        /// <returns>bool</returns>
        public virtual bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface parentSurface = null)
        {
            if (TryLoadFromStream(stream, extension, out var bitmap))
            {
                var imageContainer = new ImageContainer(parentSurface)
                {
                    Image = bitmap
                };
                drawableContainer = imageContainer;
                return true;
            }

            drawableContainer = null;
            return true;
        }
    }
}
