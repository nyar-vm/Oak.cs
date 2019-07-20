using System.Globalization;

namespace Oak.Json;

/// <summary>
///     JSON 值类型
/// </summary>
public enum JsonValueType
{
    Null,
    Boolean,
    Number,
    String,
    Array,
    Object
}

/// <summary>
///     JSON 值，不可变的 JSON 数据模型
/// </summary>
public abstract class JsonValue
{
    /// <summary>
    ///     值类型
    /// </summary>
    public abstract JsonValueType ValueType { get; }

    /// <summary>
    ///     是否为 null
    /// </summary>
    public bool IsNull => ValueType == JsonValueType.Null;

    /// <summary>
    ///     是否为布尔值
    /// </summary>
    public bool IsBoolean => ValueType == JsonValueType.Boolean;

    /// <summary>
    ///     是否为数字
    /// </summary>
    public bool IsNumber => ValueType == JsonValueType.Number;

    /// <summary>
    ///     是否为字符串
    /// </summary>
    public bool IsString => ValueType == JsonValueType.String;

    /// <summary>
    ///     是否为数组
    /// </summary>
    public bool IsArray => ValueType == JsonValueType.Array;

    /// <summary>
    ///     是否为对象
    /// </summary>
    public bool IsObject => ValueType == JsonValueType.Object;
}

/// <summary>
///     JSON null 值
/// </summary>
public sealed class JsonNull : JsonValue
{
    public static JsonNull Instance { get; } = new();

    public override JsonValueType ValueType => JsonValueType.Null;

    public override string ToString()
    {
        return "null";
    }
}

/// <summary>
///     JSON 布尔值
/// </summary>
public sealed class JsonBoolean : JsonValue
{
    public JsonBoolean(bool value)
    {
        Value = value;
    }

    public bool Value { get; }

    public override JsonValueType ValueType => JsonValueType.Boolean;

    public static JsonBoolean True { get; } = new(true);
    public static JsonBoolean False { get; } = new(false);

    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}

/// <summary>
///     JSON 数字值
/// </summary>
public sealed class JsonNumber : JsonValue
{
    public JsonNumber(double value)
    {
        Value = value;
    }

    public double Value { get; }

    public override JsonValueType ValueType => JsonValueType.Number;

    /// <summary>
    ///     尝试获取整数值
    /// </summary>
    public bool TryGetInt(out int value)
    {
        if (Value is >= int.MinValue and <= int.MaxValue && Value == (int)Value)
        {
            value = (int)Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     尝试获取长整数值
    /// </summary>
    public bool TryGetLong(out long value)
    {
        if (Value is >= long.MinValue and <= long.MaxValue && Value == (long)Value)
        {
            value = (long)Value;
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>
///     JSON 字符串值
/// </summary>
public sealed class JsonString : JsonValue
{
    public JsonString(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override JsonValueType ValueType => JsonValueType.String;

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}

/// <summary>
///     JSON 数组
/// </summary>
public sealed class JsonArray : JsonValue
{
    private readonly JsonValue[] _items;

    public JsonArray(JsonValue[] items)
    {
        _items = items;
    }

    public override JsonValueType ValueType => JsonValueType.Array;

    /// <summary>
    ///     元素数量
    /// </summary>
    public int Count => _items.Length;

    /// <summary>
    ///     按索引获取元素
    /// </summary>
    public JsonValue this[int index] => _items[index];

    /// <summary>
    ///     获取所有元素
    /// </summary>
    public IReadOnlyList<JsonValue> Items => _items;
}

/// <summary>
///     JSON 对象
/// </summary>
public sealed class JsonObject : JsonValue
{
    private readonly Dictionary<string, int> _index;
    private readonly (string Key, JsonValue Value)[] _properties;

    public JsonObject((string Key, JsonValue Value)[] properties)
    {
        _properties = properties;
        _index = new Dictionary<string, int>(properties.Length);

        for (var i = 0; i < properties.Length; i++) _index[properties[i].Key] = i;
    }

    public override JsonValueType ValueType => JsonValueType.Object;

    /// <summary>
    ///     属性数量
    /// </summary>
    public int Count => _properties.Length;

    /// <summary>
    ///     按属性名获取值
    /// </summary>
    public JsonValue? this[string key]
    {
        get
        {
            if (!_index.TryGetValue(key, out var i)) return null;
            return _properties[i].Value;
        }
    }

    /// <summary>
    ///     获取所有属性
    /// </summary>
    public IReadOnlyList<(string Key, JsonValue Value)> Properties => _properties;

    /// <summary>
    ///     是否包含指定属性
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _index.ContainsKey(key);
    }

    /// <summary>
    ///     尝试获取属性值
    /// </summary>
    public bool TryGetValue(string key, out JsonValue? value)
    {
        if (!_index.TryGetValue(key, out var i))
        {
            value = null;
            return false;
        }

        value = _properties[i].Value;
        return true;
    }
}