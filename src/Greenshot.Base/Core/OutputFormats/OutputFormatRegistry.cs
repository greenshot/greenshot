using System;
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Base.Core.OutputFormats;

public sealed class OutputFormatRegistry : IOutputFormatRegistry
{
    private readonly Dictionary<string, OutputFormatDefinition> _formatsById =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OutputFormatDefinition> _formatsByExtension =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<OutputFormatDefinition> Formats =>
        _formatsById.Values.ToArray();

    public void Register(OutputFormatDefinition format)
    {
        Validate(format);
        string normalizedExtension = Normalize(format.Extension);

        if (_formatsById.ContainsKey(format.Id))
        {
            throw new InvalidOperationException(
                $"The output format '{format.Id}' is already registered.");
        }

        if (_formatsByExtension.ContainsKey(normalizedExtension))
        {
            throw new InvalidOperationException(
                $"The output extension '{normalizedExtension}' is already registered.");
        }

        _formatsById.Add(format.Id, format);
        _formatsByExtension.Add(normalizedExtension, format);
    }

    public bool RegisterIfMissing(OutputFormatDefinition format)
    {
        Validate(format);
        string normalizedExtension = Normalize(format.Extension);

        if (_formatsById.ContainsKey(format.Id) ||
            _formatsByExtension.ContainsKey(normalizedExtension))
        {
            return false;
        }

        _formatsById.Add(format.Id, format);
        _formatsByExtension.Add(normalizedExtension, format);
        return true;
    }

    public bool TryGet(string id, out OutputFormatDefinition format)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            format = null;
            return false;
        }

        return _formatsById.TryGetValue(id, out format);
    }

    public OutputFormatDefinition GetByExtension(string extension)
    {
        string normalizedExtension = Normalize(extension);
        if (normalizedExtension == null)
        {
            return null;
        }

        _formatsByExtension.TryGetValue(normalizedExtension, out OutputFormatDefinition format);
        return format;
    }

    private static void Validate(OutputFormatDefinition format)
    {
        if (format == null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        if (string.IsNullOrWhiteSpace(format.Id))
        {
            throw new ArgumentException("The output format ID must not be empty.", nameof(format));
        }

        if (Normalize(format.Extension) == null)
        {
            throw new ArgumentException("The output format extension must not be empty.", nameof(format));
        }
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().TrimStart('.').ToLowerInvariant();
    }
}
