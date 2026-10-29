using System;
using System.IO;
using System.Text;

namespace Greenshot.Editor.Measurements
{
    /// <summary>Optional fixed trailer inside the legacy element payload. No new serialized object types.</summary>
    internal static class MeasurementMetadata
    {
        private const string Magic = "GSPMEAS1";
        internal const int Size = 24;

        internal static void Write(Stream stream, MeasurementState state)
        {
            if (state == null || !state.IsCalibrated) return;
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write(1);
            writer.Write(state.MetresPerPixel);
            writer.Write((int)state.Unit);
        }

        internal static MeasurementState Read(Stream stream, long payloadStart, long payloadLength)
        {
            long position = stream.Position;
            try
            {
                if (payloadLength < Size || payloadStart < 0 || payloadLength > stream.Length - payloadStart)
                    return MeasurementState.Pixels;
                stream.Position = payloadStart + payloadLength - Size;
                using var reader = new BinaryReader(stream, Encoding.UTF8, true);
                if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != Magic || reader.ReadInt32() != 1)
                    return MeasurementState.Pixels;
                double scale = reader.ReadDouble();
                var unit = (MeasurementUnit)reader.ReadInt32();
                return new MeasurementState(scale, unit);
            }
            catch (ArgumentException) { return MeasurementState.Pixels; }
            catch (IOException) { return MeasurementState.Pixels; }
            finally { stream.Position = position; }
        }
    }
}
