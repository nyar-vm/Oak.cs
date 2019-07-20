using Oak.Data;
using Oak.Diagnostics;

namespace Oak.Json;

/// <summary>
///     JSON 格式序列化/反序列化器，实现统一 ISerdeFormat 接口
/// </summary>
public sealed class JsonFormat : ISerdeFormat
{
    private readonly JsonParser _parser = new();

    /// <inheritdoc />
    public string FormatName => "JSON";

    /// <inheritdoc />
    public SerdeValue Deserialize(string source)
    {
        var result = _parser.Parse(source);

        if (result is { Success: true, Value: not null })
        {
            return result.Value.ToSerdeValue();
        }

        return SerdeValue.Null();
    }

    /// <inheritdoc />
    public string Serialize(SerdeValue value)
    {
        return value.ToJsonValue().ToString();
    }
}
