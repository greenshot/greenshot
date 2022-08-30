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
using System.IO;
using System.Text;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Core
{
    public sealed class FileDescriptor
    {
        public FileDescriptorFlags Flags { get; set; }
        public Guid ClassId { get; set; }
        public NativeSize Size { get; set; }
        public NativePoint Point { get; set; }
        public FileAttributes FileAttributes { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public Int64 FileSize { get; set; }
        public string FileName { get; set; }

        public FileDescriptor(BinaryReader reader)
        {
            //Flags
            Flags = (FileDescriptorFlags)reader.ReadUInt32();
            //ClassID
            ClassId = new Guid(reader.ReadBytes(16));
            //Size
            Size = new NativeSize(reader.ReadInt32(), reader.ReadInt32());
            //Point
            Point = new NativePoint(reader.ReadInt32(), reader.ReadInt32());
            //FileAttributes
            FileAttributes = (FileAttributes)reader.ReadUInt32();
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
}
