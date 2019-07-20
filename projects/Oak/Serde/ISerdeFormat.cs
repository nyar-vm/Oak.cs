namespace Oak.Data;

/// <summary>
///     Serde 序列化器接口，所有序列化格式的统一抽象
/// </summary>
public interface ISerdeSerializer
{
    /// <summary>
    ///     将 SerdeValue 序列化为文本
    /// </summary>
    string Serialize(SerdeValue value);
}

/// <summary>
///     Serde 反序列化器接口，所有序列化格式的统一抽象
/// </summary>
public interface ISerdeDeserializer
{
    /// <summary>
    ///     将文本反序列化为 SerdeValue
    /// </summary>
    SerdeValue Deserialize(string source);
}

/// <summary>
///     Serde 格式序列化/反序列化器接口
/// </summary>
public interface ISerdeFormat : ISerdeSerializer, ISerdeDeserializer
{
    /// <summary>
    ///     格式名称
    /// </summary>
    string FormatName { get; }
}
