using System;
using System.Text.Json;

namespace Greenshot.Editor.FileFormat.V2;

/// <summary>
/// Extension methods for <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Attempts to get a property value from a <see cref="JsonElement"/> using case-insensitive property name comparison.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to find (case-insensitive).</param>
    /// <param name="value">When this method returns, contains the property value if found; otherwise, the default value.</param>
    /// <returns><c>true</c> if a property with the specified name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetPropertyIgnoreCase(
        this JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
