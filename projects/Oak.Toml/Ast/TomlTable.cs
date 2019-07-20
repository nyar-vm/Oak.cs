namespace Oak.Toml.Ast;

public sealed class TomlTable
{
    public Dictionary<string, TomlValue> Entries { get; init; } = [];
    public Dictionary<string, TomlTable> Tables { get; init; } = [];
    public string Name { get; init; } = string.Empty;
    public bool IsArrayTable { get; init; }
}