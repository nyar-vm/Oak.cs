namespace Oak.Data;

/// <summary>
///     Serde 值类型，所有序列化格式的公共语义模型
/// </summary>
public enum SerdeValueType
{
    /// <summary>
    ///     空值
    /// </summary>
    Null,

    /// <summary>
    ///     布尔值
    /// </summary>
    Boolean,

    /// <summary>
    ///     整数（无范围限制，使用字符串存储）
    /// </summary>
    Integer,

    /// <summary>
    ///     小数（无范围限制，使用字符串存储）
    /// </summary>
    Decimal,

    /// <summary>
    ///     字符串
    /// </summary>
    String,

    /// <summary>
    ///     数组
    /// </summary>
    Array,

    /// <summary>
    ///     对象（键值对集合）
    /// </summary>
    Object
}
