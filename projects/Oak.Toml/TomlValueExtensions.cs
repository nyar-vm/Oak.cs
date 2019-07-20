using Oak.Data;
using Oak.Toml.Ast;

namespace Oak.Toml;

public static class TomlValueExtensions
{
    public static SerdeValue ToSerdeValue(this TomlValue tomlValue)
    {
        return tomlValue.Type switch
        {
            TomlValueType.String => SerdeValue.String((string)tomlValue.RawValue!),
            TomlValueType.Integer => SerdeValue.Integer(((long)tomlValue.RawValue!).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            TomlValueType.Float => ConvertFloat((double)tomlValue.RawValue!),
            TomlValueType.Boolean => SerdeValue.Boolean((bool)tomlValue.RawValue!),
            TomlValueType.DateTime or TomlValueType.Date or TomlValueType.Time => SerdeValue.String(tomlValue.RawValue?.ToString() ?? ""),
            TomlValueType.Array => SerdeValue.Array(((TomlValue[])tomlValue.RawValue!).Select(ToSerdeValue).ToList()),
            TomlValueType.InlineTable => ConvertInlineTable((Dictionary<string, TomlValue>)tomlValue.RawValue!),
            _ => SerdeValue.Null()
        };
    }

    private static SerdeValue ConvertFloat(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return SerdeValue.Null();
        }

        return SerdeValue.Decimal(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static SerdeValue ConvertInlineTable(Dictionary<string, TomlValue> table)
    {
        return SerdeValue.Object(table.ToDictionary(kv => kv.Key, kv => kv.Value.ToSerdeValue()));
    }

    public static SerdeValue ToSerdeValue(this TomlTable table)
    {
        var fields = new Dictionary<string, SerdeValue>();

        foreach (var (key, value) in table.Entries)
        {
            fields[key] = value.ToSerdeValue();
        }

        foreach (var (key, childTable) in table.Tables)
        {
            fields[key] = childTable.ToSerdeValue();
        }

        return SerdeValue.Object(fields);
    }

    public static TomlValue ToTomlValue(this SerdeValue serdeValue)
    {
        return serdeValue.Type switch
        {
            SerdeValueType.Null => new TomlValue { Type = TomlValueType.String, RawValue = "" },
            SerdeValueType.Boolean => new TomlValue { Type = TomlValueType.Boolean, RawValue = serdeValue.GetBoolean() },
            SerdeValueType.Integer => new TomlValue { Type = TomlValueType.Integer, RawValue = long.Parse(serdeValue.GetIntegerString()!, System.Globalization.CultureInfo.InvariantCulture) },
            SerdeValueType.Decimal => new TomlValue { Type = TomlValueType.Float, RawValue = double.Parse(serdeValue.GetDecimalString()!, System.Globalization.CultureInfo.InvariantCulture) },
            SerdeValueType.String => new TomlValue { Type = TomlValueType.String, RawValue = serdeValue.GetString()! },
            SerdeValueType.Array => new TomlValue { Type = TomlValueType.Array, RawValue = serdeValue.Elements!.Select(ToTomlValue).ToArray() },
            SerdeValueType.Object => new TomlValue { Type = TomlValueType.InlineTable, RawValue = serdeValue.Fields!.ToDictionary(kv => kv.Key, kv => kv.Value.ToTomlValue()) },
            _ => new TomlValue { Type = TomlValueType.String, RawValue = "" }
        };
    }
}
