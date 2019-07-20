using System.Globalization;
using System.Numerics;
using System.Text;

namespace Oak.Data;

/// <summary>
///     Serde 统一值，所有序列化格式的公共语义模型
/// </summary>
public sealed class SerdeValue
{
    private SerdeValue(SerdeValueType type, object? rawValue = null,
        Dictionary<string, SerdeValue>? fields = null, List<SerdeValue>? elements = null)
    {
        Type = type;
        RawValue = rawValue;
        Fields = fields;
        Elements = elements;
    }

    /// <summary>
    ///     值类型
    /// </summary>
    public SerdeValueType Type { get; }

    /// <summary>
    ///     原始值
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    ///     对象字段（仅对象类型）
    /// </summary>
    public Dictionary<string, SerdeValue>? Fields { get; }

    /// <summary>
    ///     数组元素（仅数组类型）
    /// </summary>
    public List<SerdeValue>? Elements { get; }

    /// <summary>
    ///     创建空值
    /// </summary>
    public static SerdeValue Null()
    {
        return new SerdeValue(SerdeValueType.Null);
    }

    /// <summary>
    ///     创建布尔值
    /// </summary>
    public static SerdeValue Boolean(bool value)
    {
        return new SerdeValue(SerdeValueType.Boolean, value);
    }

    /// <summary>
    ///     创建整数（无范围限制，使用字符串存储）
    /// </summary>
    public static SerdeValue Integer(string value)
    {
        return new SerdeValue(SerdeValueType.Integer, value);
    }

    /// <summary>
    ///     创建小数（无范围限制，使用字符串存储）
    /// </summary>
    public static SerdeValue Decimal(string value)
    {
        return new SerdeValue(SerdeValueType.Decimal, value);
    }

    /// <summary>
    ///     创建字符串
    /// </summary>
    public static SerdeValue String(string value)
    {
        return new SerdeValue(SerdeValueType.String, value);
    }

    /// <summary>
    ///     创建对象
    /// </summary>
    public static SerdeValue Object(Dictionary<string, SerdeValue> fields)
    {
        return new SerdeValue(SerdeValueType.Object, fields: fields);
    }

    /// <summary>
    ///     创建数组
    /// </summary>
    public static SerdeValue Array(List<SerdeValue> elements)
    {
        return new SerdeValue(SerdeValueType.Array, elements: elements);
    }

    /// <summary>
    ///     获取布尔值
    /// </summary>
    public bool GetBoolean()
    {
        return Type == SerdeValueType.Boolean && (bool)(RawValue ?? false);
    }

    /// <summary>
    ///     获取整数字符串表示
    /// </summary>
    public string? GetIntegerString()
    {
        return Type == SerdeValueType.Integer ? (string?)RawValue : null;
    }

    /// <summary>
    ///     获取小数字符串表示
    /// </summary>
    public string? GetDecimalString()
    {
        return Type == SerdeValueType.Decimal ? (string?)RawValue : null;
    }

    /// <summary>
    ///     获取字符串
    /// </summary>
    public string? GetString()
    {
        return Type == SerdeValueType.String ? (string?)RawValue : null;
    }

    /// <summary>
    ///     获取字段值
    /// </summary>
    public SerdeValue? GetField(string name)
    {
        if (Fields is not null && Fields.TryGetValue(name, out var value)) return value;

        return null;
    }

    /// <summary>
    ///     获取字段值并转换为指定类型
    /// </summary>
    public T? GetFieldAs<T>(string name) where T : struct
    {
        var field = GetField(name);
        if (field is null) return null;

        return field.Type switch
        {
            SerdeValueType.Integer => ConvertBigInteger<T>(field.GetIntegerString()),
            SerdeValueType.Decimal => ConvertDecimal<T>(field.GetDecimalString()),
            SerdeValueType.Boolean => (T)(object)field.GetBoolean(),
            _ => null
        };
    }

    private static T? ConvertBigInteger<T>(string? value) where T : struct
    {
        if (value is null) return null;
        if (typeof(T) == typeof(long)) return (T)(object)long.Parse(value);
        if (typeof(T) == typeof(int)) return (T)(object)int.Parse(value);
        if (typeof(T) == typeof(BigInteger)) return (T)(object)BigInteger.Parse(value);
        return null;
    }

    private static T? ConvertDecimal<T>(string? value) where T : struct
    {
        if (value is null) return null;
        if (typeof(T) == typeof(decimal)) return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(double)) return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);
        return null;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Type switch
        {
            SerdeValueType.Null => "null",
            SerdeValueType.Boolean => GetBoolean() ? "true" : "false",
            SerdeValueType.Integer => FormatIntegerOrNull(),
            SerdeValueType.Decimal => FormatDecimalOrNull(),
            SerdeValueType.String => $"\"{GetString()}\"",
            SerdeValueType.Object => FormatObject(),
            SerdeValueType.Array => FormatArray(),
            _ => "unknown"
        };
    }

    private string FormatIntegerOrNull()
    {
        var value = GetIntegerString();
        if (string.IsNullOrEmpty(value)) return "null";
        if (value == "-") return "null";
        return value;
    }

    private string FormatDecimalOrNull()
    {
        var value = GetDecimalString();
        if (string.IsNullOrEmpty(value)) return "null";
        if (value is "-" or "." or "-.") return "null";
        return value;
    }

    private string FormatObject()
    {
        var sb = new StringBuilder();
        sb.Append("{ ");

        if (Fields is not null)
        {
            var first = true;
            foreach (var (key, value) in Fields)
            {
                if (!first) sb.Append(", ");
                sb.Append(key);
                sb.Append(": ");
                sb.Append(value);
                first = false;
            }
        }

        sb.Append(" }");
        return sb.ToString();
    }

    private string FormatArray()
    {
        var sb = new StringBuilder();
        sb.Append("[ ");

        if (Elements is not null)
        {
            var first = true;
            foreach (var element in Elements)
            {
                if (!first) sb.Append(", ");
                sb.Append(element);
                first = false;
            }
        }

        sb.Append(" ]");
        return sb.ToString();
    }
}
