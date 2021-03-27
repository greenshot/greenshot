/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using GreenshotPlugin.UnmanagedHelpers.Structs;

namespace GreenshotPlugin.Interop
{

    /// <summary>
    /// Specifies which fields are valid in a FileDescriptor Structure
    /// </summary>    
    [Flags]
    internal enum FileDescriptorFlags : uint
    {
    }

    internal static class FileDescriptorReader
    {
        internal sealed class FileDescriptor
        {
            public FileDescriptorFlags Flags { get; set; }
            public Guid ClassId { get; set; }
            public SIZE Size { get; set; }
            public POINT Point { get; set; }
            public FileAttributes FileAttributes { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public DateTime LastWriteTime { get; set; }
            public Int64 FileSize { get; set; }
            public string FileName { get; set; }

            public FileDescriptor(BinaryReader reader)
            {
                //Flags
                Flags = (FileDescriptorFlags) reader.ReadUInt32();
                //ClassID
                ClassId = new Guid(reader.ReadBytes(16));
                //Size
                Size = new SIZE(reader.ReadInt32(), reader.ReadInt32());
                //Point
                Point = new POINT(reader.ReadInt32(), reader.ReadInt32());
                //FileAttributes
                FileAttributes = (FileAttributes) reader.ReadUInt32();
                //CreationTime
                CreationTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
                //LastAccessTime
                LastAccessTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
                //LastWriteTime
                LastWriteTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
                //FileSize
                FileSize = reader.ReadInt64();
                //FileName
                byte[] nameBytes = reader.ReadBytes(520);
                int i = 0;
                while (i < nameBytes.Length)
                {
                    if (nameBytes[i] == 0 && nameBytes[i + 1] == 0)
                        break;
                    i++;
                    i++;
                }

                FileName = Encoding.Unicode.GetString(nameBytes, 0, i);
            }
        }

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
            var comDataObject = (IDataObject)dataObject;

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
                    cfFormat = (short)format.Id,
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
            IStream iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
            Marshal.Release(medium.unionmember);

            //get the STATSTG of the IStream to determine how many bytes are in it
            var iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
            iStream.Stat(out iStreamStat, 0);
            int iStreamSize = (int)iStreamStat.cbSize;

            //read the data from the IStream into a managed byte array
            byte[] iStreamContent = new byte[iStreamSize];
            iStream.Read(iStreamContent, iStreamContent.Length, IntPtr.Zero);

            //wrapped the managed byte array into a memory stream
            return new MemoryStream(iStreamContent);
        }
    }
}