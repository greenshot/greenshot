using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace Greenshot.Editor.Measurements
{
    /// <summary>Bounded primitive record before the unchanged final calibration trailer.</summary>
    internal static class DimensionMetadata
    {
        private const string Magic = "GSPDIM01";
        internal static void Write(Stream stream, DimensionStyle style)
        {
            using var body = new MemoryStream();
            using (var writer = new BinaryWriter(body, Encoding.UTF8, true))
            {
                byte[] family = Encoding.UTF8.GetBytes(style.Family);
                writer.Write(1); writer.Write((ushort)family.Length); writer.Write(family);
                writer.Write(style.Size); writer.Write((int)style.FontStyle); writer.Write(style.Color.ToArgb());
                // Keep the version-1 base readable by old readers, which ignore the rest of this bounded body.
                writer.Write(Encoding.ASCII.GetBytes("GSPFMT01")); writer.Write(1);
                writer.Write(style.Format.Packed); writer.Write(style.BackgroundColor.ToArgb());
            }
            body.WriteTo(stream);
            using var footer = new BinaryWriter(stream, Encoding.UTF8, true);
            footer.Write((int)body.Length); footer.Write(Encoding.ASCII.GetBytes(Magic));
        }
        internal static DimensionStyle Read(Stream stream, long start, long length)
        {
            long position = stream.Position;
            try
            {
                if (start < 0 || length < MeasurementMetadata.Size + 12 || length > stream.Length - start) return DimensionStyle.Default;
                long footer = start + length - MeasurementMetadata.Size - 12;
                stream.Position = footer;
                using var reader = new BinaryReader(stream, Encoding.UTF8, true);
                int count = reader.ReadInt32();
                if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != Magic || count < 18 || count > 4096 || count > footer - start) return DimensionStyle.Default;
                stream.Position = footer - count;
                using var body = new MemoryStream(reader.ReadBytes(count), false);
                using var data = new BinaryReader(body, Encoding.UTF8, true);
                if (data.ReadInt32() != 1) return DimensionStyle.Default;
                int familyLength = data.ReadUInt16();
                if (familyLength < 1 || familyLength > 512 || familyLength > body.Length - body.Position - 12) return DimensionStyle.Default;
                string family = new UTF8Encoding(false, true).GetString(data.ReadBytes(familyLength));
                var style = new DimensionStyle(family, data.ReadSingle(), (FontStyle)data.ReadInt32(), Color.FromArgb(data.ReadInt32()));
                // A malformed extension must never discard a valid base font/colour or the independent calibration trailer.
                if (body.Length - body.Position != 20) return style;
                if (Encoding.ASCII.GetString(data.ReadBytes(8)) != "GSPFMT01" || data.ReadInt32() != 1) return style;
                var format = DimensionFormat.Unpack(data.ReadInt32());
                var background = Color.FromArgb(data.ReadInt32());
                return format == null ? style : new DimensionStyle(style.Family, style.Size, style.FontStyle, style.Color, format, background);
            }
            catch (ArgumentException) { return DimensionStyle.Default; }
            catch (IOException) { return DimensionStyle.Default; }
            finally { stream.Position = position; }
        }
    }
}
