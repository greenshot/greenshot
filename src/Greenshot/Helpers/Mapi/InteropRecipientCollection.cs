// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Helpers.Mapi
{
    /// <summary>
    ///     Struct which contains an interop representation of a colleciton of recipients.
    /// </summary>
    internal struct InteropRecipientCollection : IDisposable
    {
        private int _count;

        /// <summary>
        ///     Default constructor for creating InteropRecipientCollection.
        /// </summary>
        /// <param name="outer"></param>
        public InteropRecipientCollection(RecipientCollection outer)
        {
            _count = outer.Count;

            if (_count == 0)
            {
                Handle = IntPtr.Zero;
                return;
            }

            // allocate enough memory to hold all recipients
            var size = Marshal.SizeOf(typeof(MapiRecipDesc));
            Handle = Marshal.AllocHGlobal(_count * size);

            // place all interop recipients into the memory just allocated
            var ptr = Handle;
            foreach (Recipient native in outer)
            {
                var interop = native.GetInteropRepresentation();

                // stick it in the memory block
                Marshal.StructureToPtr(interop, ptr, false);
                ptr = new IntPtr(ptr.ToInt64() + size);
            }
        }

        public IntPtr Handle { get; private set; }

        /// <summary>
        ///     Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                var type = typeof(MapiRecipDesc);
                var size = Marshal.SizeOf(type);

                // destroy all the structures in the memory area
                var ptr = Handle;
                for (var i = 0; i < _count; i++)
                {
                    Marshal.DestroyStructure(ptr, type);
                    ptr = new IntPtr(ptr.ToInt64() + size);
                }

                // free the memory
                Marshal.FreeHGlobal(Handle);

                Handle = IntPtr.Zero;
                _count = 0;
            }
        }
    }
}