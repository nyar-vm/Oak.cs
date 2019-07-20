using Oak.Data;

namespace Oak.Yaml;

/// <summary>
///     YamlValue 扩展方法，支持转换为统一 SerdeValue 模型
/// </summary>
public static class YamlValueExtensions
{
    /// <summary>
    ///     将 YamlValue 转换为 SerdeValue
    /// </summary>
    public static SerdeValue ToSerdeValue(this YamlValue yamlValue)
    {
        return yamlValue switch
        {
            YamlNull => SerdeValue.Null(),
            YamlBoolean b => SerdeValue.Boolean(b.Value),
            YamlNumber n => ConvertYamlNumber(n),
            YamlString s => SerdeValue.String(s.Value),
            YamlSequence seq => SerdeValue.Array(seq.Items.Select(ToSerdeValue).ToList()),
            YamlMapping map => SerdeValue.Object(map.Properties.ToDictionary(p => p.Key, p => p.Value.ToSerdeValue())),
            _ => SerdeValue.Null()
        };
    }

    private static SerdeValue ConvertYamlNumber(YamlNumber number)
    {
        if (number.TryGetLong(out var longValue))
        {
            return SerdeValue.Integer(longValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var doubleStr = number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (double.IsNaN(number.Value) || double.IsInfinity(number.Value))
        {
            return SerdeValue.Null();
        }

        return SerdeValue.Decimal(doubleStr);
    }

    /// <summary>
    ///     将 SerdeValue 转换为 YamlValue
    /// </summary>
    public static YamlValue ToYamlValue(this SerdeValue serdeValue)
    {
        return serdeValue.Type switch
        {
            SerdeValueType.Null => YamlNull.Instance,
            SerdeValueType.Boolean => new YamlBoolean(serdeValue.GetBoolean()),
            SerdeValueType.Integer => new YamlNumber(double.Parse(serdeValue.GetIntegerString()!, System.Globalization.CultureInfo.InvariantCulture)),
            SerdeValueType.Decimal => new YamlNumber(double.Parse(serdeValue.GetDecimalString()!, System.Globalization.CultureInfo.InvariantCulture)),
            SerdeValueType.String => new YamlString(serdeValue.GetString()!),
            SerdeValueType.Array => new YamlSequence(serdeValue.Elements!.Select(ToYamlValue).ToArray()),
            SerdeValueType.Object => new YamlMapping(serdeValue.Fields!.Select(kv => (kv.Key, kv.Value.ToYamlValue())).ToArray()),
            _ => YamlNull.Instance
        };
    }
}
