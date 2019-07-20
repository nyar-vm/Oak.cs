using System.Globalization;

namespace Oak.Yaml;

/// <summary>
///     YAML 值类型
/// </summary>
public enum YamlValueType
{
    Null,
    Boolean,
    Number,
    String,
    Sequence,
    Mapping
}

/// <summary>
///     YAML 值，不可变的 YAML 数据模型
/// </summary>
public abstract class YamlValue
{
    /// <summary>
    ///     值类型
    /// </summary>
    public abstract YamlValueType ValueType { get; }

    /// <summary>
    ///     是否为 null
    /// </summary>
    public bool IsNull => ValueType == YamlValueType.Null;

    /// <summary>
    ///     是否为布尔值
    /// </summary>
    public bool IsBoolean => ValueType == YamlValueType.Boolean;

    /// <summary>
    ///     是否为数字
    /// </summary>
    public bool IsNumber => ValueType == YamlValueType.Number;

    /// <summary>
    ///     是否为字符串
    /// </summary>
    public bool IsString => ValueType == YamlValueType.String;

    /// <summary>
    ///     是否为序列
    /// </summary>
    public bool IsSequence => ValueType == YamlValueType.Sequence;

    /// <summary>
    ///     是否为映射
    /// </summary>
    public bool IsMapping => ValueType == YamlValueType.Mapping;
}

/// <summary>
///     YAML null 值
/// </summary>
public sealed class YamlNull : YamlValue
{
    public static YamlNull Instance { get; } = new();

    public override YamlValueType ValueType => YamlValueType.Null;

    public override string ToString()
    {
        return "null";
    }
}

/// <summary>
///     YAML 布尔值
/// </summary>
public sealed class YamlBoolean : YamlValue
{
    public YamlBoolean(bool value)
    {
        Value = value;
    }

    public bool Value { get; }

    public override YamlValueType ValueType => YamlValueType.Boolean;

    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}

/// <summary>
///     YAML 数字值
/// </summary>
public sealed class YamlNumber : YamlValue
{
    public YamlNumber(double value)
    {
        Value = value;
    }

    public double Value { get; }

    public override YamlValueType ValueType => YamlValueType.Number;

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
///     YAML 字符串值
/// </summary>
public sealed class YamlString : YamlValue
{
    public YamlString(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override YamlValueType ValueType => YamlValueType.String;

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
///     YAML 序列
/// </summary>
public sealed class YamlSequence : YamlValue
{
    private readonly YamlValue[] _items;

    public YamlSequence(YamlValue[] items)
    {
        _items = items;
    }

    public override YamlValueType ValueType => YamlValueType.Sequence;

    /// <summary>
    ///     元素数量
    /// </summary>
    public int Count => _items.Length;

    /// <summary>
    ///     按索引获取元素
    /// </summary>
    public YamlValue this[int index] => _items[index];

    /// <summary>
    ///     获取所有元素
    /// </summary>
    public IReadOnlyList<YamlValue> Items => _items;
}

/// <summary>
///     YAML 映射
/// </summary>
public sealed class YamlMapping : YamlValue
{
    private readonly Dictionary<string, int> _index;
    private readonly (string Key, YamlValue Value)[] _properties;

    public YamlMapping((string Key, YamlValue Value)[] properties)
    {
        _properties = properties;
        _index = new Dictionary<string, int>(properties.Length);

        for (var i = 0; i < properties.Length; i++)
            if (!_index.ContainsKey(properties[i].Key))
                _index[properties[i].Key] = i;
    }

    public override YamlValueType ValueType => YamlValueType.Mapping;

    /// <summary>
    ///     属性数量
    /// </summary>
    public int Count => _properties.Length;

    /// <summary>
    ///     按键获取值
    /// </summary>
    public YamlValue? this[string key]
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
    public IReadOnlyList<(string Key, YamlValue Value)> Properties => _properties;

    /// <summary>
    ///     是否包含指定键
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _index.ContainsKey(key);
    }

    /// <summary>
    ///     尝试获取值
    /// </summary>
    public bool TryGetValue(string key, out YamlValue? value)
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