/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Formats.Nrbf;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel.Security;
using Greenshot.Editor.FileFormat.V1.Legacy;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Tests for the restricted NRBF reader used for legacy Greenshot files.
/// </summary>
[Collection("DefaultCollection")]
public class LegacyNrbfReaderTests
{
    /// <summary>
    /// Verifies that an unknown serialized type is rejected by the legacy reader.
    /// </summary>
    /// <remarks>This covers the vulnerability attack created with ysoserial. #579 </remarks>
    [Fact]
    public void Deserialize_UnmappedType_ThrowsSecurityAccessDeniedException()
    {
        // Arrange
        var unmappedObject = new UnmappedTestClass { Value = "Test Value" };
        var binaryFormatter = new BinaryFormatter();
        
        // Serialize the object without a custom binder
        using var memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, unmappedObject);
        memoryStream.Position = 0;
            
        // Act & Assert
        // The legacy reader must reject this type without creating it.
        var exception = Assert.Throws<SecurityAccessDeniedException>(() => 
            LegacyFileHelper.GetContainerListFromLegacyContainerListStream(memoryStream));
            
        // Verify the exception message contains information about the suspicious type
        Assert.Contains("Suspicious type", exception.Message);
        // ReSharper disable once AssignNullToNotNullAttribute
        Assert.Contains(typeof(UnmappedTestClass).FullName, exception.Message);
    }

    public static IEnumerable<object[]> HistoricalRootFixtureData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "Surface_with_11_different_DrawableContainer.greenshot"), "Greenshot.Drawing.DrawableContainerList", "Greenshot, Version=1.2.0.0", "Greenshot.Plugin.Drawing.IDrawableContainer[]", 11, "Greenshot01.02"];
        yield return [Path.Combine("TestData", "GreenshotTemplate", "File_Version_1.02", "Surface_with_11_different_DrawableContainer.gst"), "Greenshot.Drawing.DrawableContainerList", "Greenshot, Version=1.2.0.0", "Greenshot.Plugin.Drawing.IDrawableContainer[]", 11, null];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.greenshot"), "Greenshot.Editor.Drawing.DrawableContainerList", "Greenshot.Editor, Version=1.3.0.0", "Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]", 14, "Greenshot01.03"];
        yield return [Path.Combine("TestData", "GreenshotTemplate", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.gst"), "Greenshot.Editor.Drawing.DrawableContainerList", "Greenshot.Editor, Version=1.3.0.0", "Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]", 14, null];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.04", "Surface_with_11_different_DrawableContainer.greenshot"), "Greenshot.Editor.Drawing.DrawableContainerList", "Greenshot.Editor, Version=1.4.0.0", "Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]", 11, "Greenshot01.04"];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.04", "CursorContainer_lt_600_100_wh_64_64.greenshot"), "Greenshot.Editor.Drawing.DrawableContainerList", "Greenshot.Editor, Version=1.4.0.0", "Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]", 1, "Greenshot01.04"];
        yield return [Path.Combine("TestData", "GreenshotTemplate", "File_Version_1.04", "CursorContainer_lt_600_100_wh_64_64.gst"), "Greenshot.Editor.Drawing.DrawableContainerList", "Greenshot.Editor, Version=1.4.0.0", "Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]", 1, null];
    }

    [Theory]
    [MemberData(nameof(HistoricalRootFixtureData))]
    public void ReadFixture_MatchesHistoricalContainerListContract(string path, string rootType, string rootAssembly, string itemType, int count, string expectedMarker)
    {
        using var stream = File.OpenRead(path);
        long expectedPayloadLength;
        if (expectedMarker != null)
        {
            stream.Seek(-22, SeekOrigin.End);
            using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true);
            expectedPayloadLength = reader.ReadInt64();
            Assert.InRange(expectedPayloadLength, 1, stream.Length - 22);
            stream.Seek(-14, SeekOrigin.End);
            var marker = new byte[14];
            Assert.Equal(marker.Length, stream.Read(marker, 0, marker.Length));
            Assert.Equal(expectedMarker, System.Text.Encoding.ASCII.GetString(marker));
            stream.Seek(-(expectedPayloadLength + 22), SeekOrigin.End);
        }
        else
        {
            expectedPayloadLength = stream.Length;
        }

        var payloadStart = stream.Position;
        var root = Assert.IsAssignableFrom<ClassRecord>(NrbfDecoder.Decode(stream, leaveOpen: true));

        Assert.Equal(expectedPayloadLength, stream.Position - payloadStart);
        Assert.Equal(rootType, root.TypeName.FullName);
        Assert.Contains(", " + rootAssembly + ",", root.TypeName.AssemblyQualifiedName);
        Assert.Equal(new[] { "<ParentID>k__BackingField", "_disposedValue", "List`1+_items", "List`1+_size", "List`1+_version" }, root.MemberNames);
        var items = Assert.IsAssignableFrom<SerializationRecord>(root.GetRawValue("List`1+_items"));
        Assert.Equal(itemType, items.TypeName.FullName);
        Assert.Equal(count, Convert.ToInt32(root.GetRawValue("List`1+_size")));
    }

    /// <summary>
    /// A test class that is intentionally not included in the legacy type allowlist.
    /// </summary>
    [Serializable]
    private class UnmappedTestClass
    {
        public string Value { get; set; }
    }
}