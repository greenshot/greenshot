using System.Runtime.InteropServices;

namespace Greenshot.Gfx.Structs
{
    /// <summary>
    /// A struct with the BGR values for a 24bit pixel
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Bgr24
    {
        /// <summary>
        /// Blue component of the pixel
        /// </summary>
        public byte B;
        /// <summary>
        /// Green component of the pixel
        /// </summary>
        public byte G;
        /// <summary>
        /// Red component of the pixel
        /// </summary>
        public byte R;
    }
}
