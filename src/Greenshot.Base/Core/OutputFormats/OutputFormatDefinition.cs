using System;

namespace Greenshot.Base.Core.OutputFormats;

public sealed class OutputFormatDefinition
{
    public OutputFormatDefinition(string id, string extension, string displayName)
    {
        Id = id?.ToLowerInvariant();
        Extension = extension;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string Extension { get; }

    public string DisplayName { get; }
}
