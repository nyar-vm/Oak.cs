using Oak.Diagnostics;

namespace Oak.Ini;

public sealed class IniParseResult
{
    public Dictionary<string, string> GlobalEntries { get; init; } = [];
    public IReadOnlyList<IniSection> Sections { get; init; } = [];
    public IReadOnlyList<DiagnosticMessage> Diagnostics { get; init; } = [];
}

public sealed class IniSection
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, string> Entries { get; init; } = [];
}
