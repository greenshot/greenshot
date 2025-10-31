/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel.Security;
using Greenshot.Editor.FileFormat.V1.Legacy;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Tests for the LegacySerializationBinder class which handles the mapping 
/// of types during deserialization of legacy Greenshot files.
/// </summary>
[Collection("DefaultCollection")]
public class LegacySerializationBinderTests
{
    /// <summary>
    /// Test that verifies that a SecurityAccessDeniedException is thrown when 
    /// attempting to deserialize an object with a type that is not mapped in the binder.
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
        // This should throw a SecurityAccessDeniedException when LegacyFileHelper tries to deserialize
        // our unmapped type through the LegacySerializationBinder
        var exception = Assert.Throws<SecurityAccessDeniedException>(() => 
            LegacyFileHelper.GetContainerListFromLegacyContainerListStream(memoryStream));
            
        // Verify the exception message contains information about the suspicious type
        Assert.Contains("Suspicious type", exception.Message);
        // ReSharper disable once AssignNullToNotNullAttribute
        Assert.Contains(typeof(UnmappedTestClass).FullName, exception.Message);
    }

    /// <summary>
    /// A test class that is intentionally not mapped in the LegacySerializationBinder.
    /// </summary>
    [Serializable]
    private class UnmappedTestClass
    {
        public string Value { get; set; }
    }
}