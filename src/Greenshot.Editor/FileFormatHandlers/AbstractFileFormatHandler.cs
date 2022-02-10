using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormatHandlers
{
    public abstract class AbstractFileFormatHandler : IFileFormatHandler
    {
        /// <summary>
        /// Make sure we handle the input extension always the same, by "normalizing" it
        /// </summary>
        /// <param name="extension">string</param>
        /// <returns>string</returns>
        protected string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            extension = extension.ToLowerInvariant();
            return !extension.StartsWith(".") ? $".{extension}" : extension;
        }

        protected abstract string[] OurExtensions { get; }

        /// <inheritdoc />
        public virtual IEnumerable<string> SupportedExtensions(FileFormatHandlerActions fileFormatHandlerAction)
        {
            return OurExtensions;
        }

        /// <inheritdoc />
        public virtual bool Supports(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return OurExtensions.Contains(NormalizeExtension(extension));
        }

        /// <inheritdoc />
        public virtual int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return int.MaxValue;
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
