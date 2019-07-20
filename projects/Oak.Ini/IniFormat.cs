using Oak.Data;
using Oak.Diagnostics;

namespace Oak.Ini;

public sealed class IniFormat : ISerdeFormat
{
    private readonly IniParser _parser = new();

    public string FormatName => "INI";

    public SerdeValue Deserialize(string source)
    {
        var diagnostics = new DiagnosticSink();
        var result = _parser.Parse(source, diagnostics);
        return result.ToSerdeValue();
    }

    public string Serialize(SerdeValue value)
    {
        return SerializeIni(value);
    }

    private static string SerializeIni(SerdeValue value)
    {
        var sb = new System.Text.StringBuilder();

        if (value.Type != SerdeValueType.Object) return "";

        foreach (var (key, fieldValue) in value.Fields ?? [])
        {
            switch (fieldValue.Type)
            {
                case SerdeValueType.Object:
                    sb.AppendLine();
                    sb.AppendLine($"[{key}]");

                    foreach (var (entryKey, entryValue) in fieldValue.Fields ?? [])
                    {
                        sb.AppendLine($"{entryKey} = {SerializeIniValue(entryValue)}");
                    }

                    break;

                default:
                    sb.AppendLine($"{key} = {SerializeIniValue(fieldValue)}");
                    break;
            }
        }

        return sb.ToString();
    }

    private static string SerializeIniValue(SerdeValue value)
    {
        return value.Type switch
        {
            SerdeValueType.Boolean => value.GetBoolean().ToString().ToLowerInvariant(),
            SerdeValueType.Integer => value.GetIntegerString() ?? "",
            SerdeValueType.Decimal => value.GetDecimalString() ?? "",
            SerdeValueType.String => value.GetString() ?? "",
            SerdeValueType.Null => "",
            _ => value.ToString() ?? ""
        };
    }
}
