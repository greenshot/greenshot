using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Base.Core.FileFormatHandlers
{
    public abstract class AbstractFileFormatHandler
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
    }
}
