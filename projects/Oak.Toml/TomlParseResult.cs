using Oak.Diagnostics;
using Oak.Toml.Ast;

namespace Oak.Toml;

public sealed class TomlParseResult
{
    public TomlTable Root { get; init; } = new();

    public IReadOnlyList<DiagnosticMessage> Diagnostics { get; init; } = [];
}