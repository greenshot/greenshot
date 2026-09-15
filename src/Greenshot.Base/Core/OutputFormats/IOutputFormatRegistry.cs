using System.Collections.Generic;

namespace Greenshot.Base.Core.OutputFormats;

public interface IOutputFormatRegistry
{
    IReadOnlyCollection<OutputFormatDefinition> Formats { get; }

    void Register(OutputFormatDefinition format);

    bool RegisterIfMissing(OutputFormatDefinition format);

    bool TryGet(string id, out OutputFormatDefinition format);

    OutputFormatDefinition GetByExtension(string extension);
}
