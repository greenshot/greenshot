/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Specifies which fields are valid in a FileDescriptor Structure
    /// </summary>    
    [Flags]
    public enum FileDescriptorFlags : uint
    {
    }

    internal static class FileDescriptorReader
    {
        public static IEnumerable<FileDescriptor> Read(Stream fileDescriptorStream)
        {
            if (fileDescriptorStream == null)
            {
                yield break;
            }

            var reader = new BinaryReader(fileDescriptorStream);
            var count = reader.ReadUInt32();
            while (count > 0)
            {
                var descriptor = new FileDescriptor(reader);

                yield return descriptor;

                count--;
            }
        }

        public static IEnumerable<string> ReadFileNames(Stream fileDescriptorStream)
        {
            if (fileDescriptorStream == null)
            {
                yield break;
            }

            var reader = new BinaryReader(fileDescriptorStream);
            var count = reader.ReadUInt32();
            while (count > 0)
            {
                FileDescriptor descriptor = new FileDescriptor(reader);

                yield return descriptor.FileName;

                count--;
            }
        }

        internal static MemoryStream GetFileContents(System.Windows.Forms.IDataObject dataObject, int index)
        {
            //cast the default IDataObject to a com IDataObject
            var comDataObject = (IDataObject) dataObject;

            var format = System.Windows.DataFormats.GetDataFormat("FileContents");
            if (format == null)
            {
                return null;
            }

            //create STGMEDIUM to output request results into
            var medium = new STGMEDIUM();

            unchecked
            {
                var formatetc = new FORMATETC
                {
                    cfFormat = (short) format.Id,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = index,
                    tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL
                };

                //using the com IDataObject interface get the data using the defined FORMATETC
                comDataObject.GetData(ref formatetc, out medium);
            }

            return medium.tymed switch
            {
                TYMED.TYMED_ISTREAM => GetIStream(medium),
                _ => null
            };
        }

        private static MemoryStream GetIStream(STGMEDIUM medium)
        {
            //marshal the returned pointer to a IStream object
            IStream iStream = (IStream) Marshal.GetObjectForIUnknown(medium.unionmember);
            Marshal.Release(medium.unionmember);

            //get the STATSTG of the IStream to determine how many bytes are in it
            var iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
            iStream.Stat(out iStreamStat, 0);
            int iStreamSize = (int) iStreamStat.cbSize;

            //read the data from the IStream into a managed byte array
            byte[] iStreamContent = new byte[iStreamSize];
            iStream.Read(iStreamContent, iStreamContent.Length, IntPtr.Zero);

            //wrapped the managed byte array into a memory stream
            return new MemoryStream(iStreamContent);
        }
    }
}