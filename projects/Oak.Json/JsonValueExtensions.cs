using Oak.Data;

namespace Oak.Json;

/// <summary>
///     JsonValue 扩展方法，支持转换为统一 SerdeValue 模型
/// </summary>
public static class JsonValueExtensions
{
    /// <summary>
    ///     将 JsonValue 转换为 SerdeValue
    /// </summary>
    public static SerdeValue ToSerdeValue(this JsonValue jsonValue)
    {
        return jsonValue switch
        {
            JsonNull => SerdeValue.Null(),
            JsonBoolean b => SerdeValue.Boolean(b.Value),
            JsonNumber n => ConvertJsonNumber(n),
            JsonString s => SerdeValue.String(s.Value),
            JsonArray a => SerdeValue.Array(a.Items.Select(ToSerdeValue).ToList()),
            JsonObject o => SerdeValue.Object(o.Properties.ToDictionary(p => p.Key, p => p.Value.ToSerdeValue())),
            _ => SerdeValue.Null()
        };
    }

    private static SerdeValue ConvertJsonNumber(JsonNumber number)
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
    ///     将 SerdeValue 转换为 JsonValue
    /// </summary>
    public static JsonValue ToJsonValue(this SerdeValue serdeValue)
    {
        return serdeValue.Type switch
        {
            SerdeValueType.Null => JsonNull.Instance,
            SerdeValueType.Boolean => serdeValue.GetBoolean() ? JsonBoolean.True : JsonBoolean.False,
            SerdeValueType.Integer => new JsonNumber(double.Parse(serdeValue.GetIntegerString()!, System.Globalization.CultureInfo.InvariantCulture)),
            SerdeValueType.Decimal => new JsonNumber(double.Parse(serdeValue.GetDecimalString()!, System.Globalization.CultureInfo.InvariantCulture)),
            SerdeValueType.String => new JsonString(serdeValue.GetString()!),
            SerdeValueType.Array => new JsonArray(serdeValue.Elements!.Select(ToJsonValue).ToArray()),
            SerdeValueType.Object => new JsonObject(serdeValue.Fields!.Select(kv => (kv.Key, kv.Value.ToJsonValue())).ToArray()),
            _ => JsonNull.Instance
        };
    }
}
