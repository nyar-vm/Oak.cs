namespace Oak.Csv;

/// <summary>
///     配置表字段定义
/// </summary>
public sealed class ConfigField
{
    /// <summary>
    ///     字段名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     字段类型
    /// </summary>
    public TableFieldType FieldType { get; init; } = TableFieldType.String;

    /// <summary>
    ///     字段在行中的列索引
    /// </summary>
    public int Index { get; init; }
}