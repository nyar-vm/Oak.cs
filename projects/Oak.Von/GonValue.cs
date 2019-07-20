using Oak.Data;

namespace Oak.Von;

/// <summary>
///     Gon 配置格式的值类型（已弃用，请使用 Oak.Data.SerdeValue）
/// </summary>
[Obsolete("请使用 Oak.Data.SerdeValue")]
public sealed class GonValue
{
    private GonValue(SerdeValue inner, string? typeName = null, string? variantName = null)
    {
        Inner = inner;
        TypeName = typeName;
        VariantName = variantName;
    }

    /// <summary>
    ///     内部 SerdeValue
    /// </summary>
    public SerdeValue Inner { get; }

    /// <summary>
    ///     值类型
    /// </summary>
    public GonValueType Type => (GonValueType)(int)Inner.Type;

    /// <summary>
    ///     原始值
    /// </summary>
    public object? RawValue => Inner.RawValue;

    /// <summary>
    ///     类型名（VON 特有，仅对象类型）
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    ///     变体名（VON 特有，仅对象类型）
    /// </summary>
    public string? VariantName { get; }

    /// <summary>
    ///     对象字段（仅对象类型）
    /// </summary>
    public Dictionary<string, GonValue>? Fields =>
        Inner.Fields?.ToDictionary(kv => kv.Key, kv => new GonValue(kv.Value));

    /// <summary>
    ///     数组元素（仅数组类型）
    /// </summary>
    public List<GonValue>? Elements =>
        Inner.Elements?.Select(e => new GonValue(e)).ToList();

    /// <summary>
    ///     创建空值
    /// </summary>
    public static GonValue Null() => new(SerdeValue.Null());

    /// <summary>
    ///     创建布尔值
    /// </summary>
    public static GonValue Boolean(bool value) => new(SerdeValue.Boolean(value));

    /// <summary>
    ///     创建整数（无范围限制，使用字符串存储）
    /// </summary>
    public static GonValue Integer(string value) => new(SerdeValue.Integer(value));

    /// <summary>
    ///     创建小数（无范围限制，使用字符串存储）
    /// </summary>
    public static GonValue Decimal(string value) => new(SerdeValue.Decimal(value));

    /// <summary>
    ///     创建字符串
    /// </summary>
    public static GonValue String(string value) => new(SerdeValue.String(value));

    /// <summary>
    ///     创建对象
    /// </summary>
    public static GonValue Object(string? typeName, string? variantName, Dictionary<string, GonValue> fields)
    {
        var dataFields = fields.ToDictionary(kv => kv.Key, kv => kv.Value.Inner);
        return new GonValue(SerdeValue.Object(dataFields), typeName, variantName);
    }

    /// <summary>
    ///     创建数组
    /// </summary>
    public static GonValue Array(List<GonValue> elements)
    {
        var dataElements = elements.Select(e => e.Inner).ToList();
        return new GonValue(SerdeValue.Array(dataElements));
    }

    /// <summary>
    ///     获取布尔值
    /// </summary>
    public bool GetBoolean() => Inner.GetBoolean();

    /// <summary>
    ///     获取整数字符串表示
    /// </summary>
    public string? GetIntegerString() => Inner.GetIntegerString();

    /// <summary>
    ///     获取小数字符串表示
    /// </summary>
    public string? GetDecimalString() => Inner.GetDecimalString();

    /// <summary>
    ///     获取字符串
    /// </summary>
    public string? GetString() => Inner.GetString();

    /// <summary>
    ///     获取字段值
    /// </summary>
    public GonValue? GetField(string name)
    {
        var field = Inner.GetField(name);
        return field is null ? null : new GonValue(field);
    }

    /// <summary>
    ///     获取字段值并转换为指定类型
    /// </summary>
    public T? GetFieldAs<T>(string name) where T : struct => Inner.GetFieldAs<T>(name);

    /// <inheritdoc />
    public override string ToString()
    {
        if (Type == GonValueType.Object && (TypeName is not null || VariantName is not null))
        {
            var sb = new System.Text.StringBuilder();
            if (TypeName is not null)
            {
                sb.Append(TypeName);
                sb.Append(' ');
            }
            if (VariantName is not null)
            {
                sb.Append(VariantName);
                sb.Append(' ');
            }
            sb.Append(Inner.ToString());
            return sb.ToString();
        }
        return Inner.ToString();
    }

    /// <summary>
    ///     隐式转换为 SerdeValue
    /// </summary>
    public static implicit operator SerdeValue(GonValue value) => value.Inner;

    /// <summary>
    ///     显式从 SerdeValue 转换
    /// </summary>
    public static explicit operator GonValue(SerdeValue value) => new(value);
}
