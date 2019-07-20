using Oak.Data;
using Oak.Diagnostics;

namespace Oak.Toml;

public sealed class TomlFormat : ISerdeFormat
{
    private readonly TomlParser _parser = new();

    public string FormatName => "TOML";

    public SerdeValue Deserialize(string source)
    {
        var diagnostics = new DiagnosticSink();
        var result = _parser.Parse(source, diagnostics);
        return result.Root.ToSerdeValue();
    }

    public string Serialize(SerdeValue value)
    {
        return SerializeTomlValue(value);
    }

    private static string SerializeTomlValue(SerdeValue value, string? key = null, int indent = 0)
    {
        var prefix = new string(' ', indent * 2);

        switch (value.Type)
        {
            case SerdeValueType.Null:
                return key is not null ? $"{prefix}{key} = \"\"" : "";

            case SerdeValueType.Boolean:
                return key is not null ? $"{prefix}{key} = {value.GetBoolean().ToString().ToLowerInvariant()}" : value.GetBoolean().ToString().ToLowerInvariant();

            case SerdeValueType.Integer:
                return key is not null ? $"{prefix}{key} = {value.GetIntegerString()}" : value.GetIntegerString()!;

            case SerdeValueType.Decimal:
                return key is not null ? $"{prefix}{key} = {value.GetDecimalString()}" : value.GetDecimalString()!;

            case SerdeValueType.String:
                return key is not null ? $"{prefix}{key} = \"{value.GetString()}\"" : $"\"{value.GetString()}\"";

            case SerdeValueType.Array:
                {
                    var sb = new System.Text.StringBuilder();

                    if (key is not null)
                    {
                        sb.AppendLine($"{prefix}[[{key}]]");
                    }

                    foreach (var item in value.Elements ?? [])
                    {
                        sb.AppendLine(SerializeTomlValue(item, null, indent));
                    }

                    return sb.ToString();
                }

            case SerdeValueType.Object:
                {
                    var sb = new System.Text.StringBuilder();

                    if (key is not null)
                    {
                        sb.AppendLine($"{prefix}[{key}]");
                    }

                    foreach (var (fieldKey, fieldValue) in value.Fields ?? [])
                    {
                        if (fieldValue.Type == SerdeValueType.Object)
                        {
                            var nestedKey = key is not null ? $"{key}.{fieldKey}" : fieldKey;
                            sb.AppendLine();
                            sb.Append(SerializeTomlValue(fieldValue, nestedKey, indent));
                        }
                        else if (fieldValue.Type == SerdeValueType.Array && fieldValue.Elements?.Any(e => e.Type == SerdeValueType.Object) == true)
                        {
                            var nestedKey = key is not null ? $"{key}.{fieldKey}" : fieldKey;

                            foreach (var item in fieldValue.Elements)
                            {
                                sb.AppendLine();
                                sb.Append(SerializeTomlValue(item, nestedKey, indent));
                            }
                        }
                        else
                        {
                            sb.AppendLine(SerializeTomlValue(fieldValue, fieldKey, indent + 1));
                        }
                    }

                    return sb.ToString();
                }

            default:
                return "";
        }
    }
}
