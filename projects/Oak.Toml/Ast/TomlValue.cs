namespace Oak.Toml.Ast;

public sealed class TomlValue
{
    public TomlValueType Type { get; init; }
    public object? RawValue { get; init; }

    public string? AsString => RawValue as string;

    public long? AsInteger => RawValue as long?;

    public double? AsFloat => RawValue as double?;

    public bool? AsBoolean => RawValue as bool?;

    public TomlValue[]? AsArray => RawValue as TomlValue[];

    public Dictionary<string, TomlValue>? AsInlineTable => RawValue as Dictionary<string, TomlValue>;
}