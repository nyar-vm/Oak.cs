using Oak.Data;
using Oak.Diagnostics;

namespace Oak.Yaml;

/// <summary>
///     YAML 格式序列化/反序列化器，实现统一 ISerdeFormat 接口
/// </summary>
public sealed class YamlFormat : ISerdeFormat
{
    private readonly YamlParser _parser = new();

    /// <inheritdoc />
    public string FormatName => "YAML";

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
        return value.ToYamlValue().ToString();
    }
}
