namespace Oak.Csv;

/// <summary>
///     配置表，从 CSV 行数据构建并提供类型安全的字段访问
/// </summary>
public sealed class ConfigTable
{
    private ConfigTable(string name, IReadOnlyList<ConfigField> fields, IReadOnlyList<ConfigRow> rows)
    {
        Name = name;
        Fields = fields;
        Rows = rows;
    }

    /// <summary>
    ///     配置表名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     字段定义列表
    /// </summary>
    public IReadOnlyList<ConfigField> Fields { get; }

    /// <summary>
    ///     数据行列表
    /// </summary>
    public IReadOnlyList<ConfigRow> Rows { get; }

    /// <summary>
    ///     从行数据构建配置表
    /// </summary>
    public static ConfigTable FromRows(
        string name,
        IReadOnlyList<IReadOnlyList<string>> rows,
        int nameRowIndex = 0,
        int typeRowIndex = 1,
        int dataStartIndex = 2)
    {
        if (rows.Count <= typeRowIndex)
            throw new ArgumentException($"行数据不足：需要至少 {typeRowIndex + 1} 行，实际 {rows.Count} 行");

        var nameRow = rows[nameRowIndex];
        var typeRow = rows[typeRowIndex];
        var fieldCount = Math.Min(nameRow.Count, typeRow.Count);

        var fields = new ConfigField[fieldCount];
        var parser = new TableFieldTypeParser();

        for (var i = 0; i < fieldCount; i++)
            fields[i] = new ConfigField
            {
                Name = nameRow[i],
                FieldType = parser.Parse(typeRow[i]),
                Index = i
            };

        var dataRows = new List<ConfigRow>(Math.Max(0, rows.Count - dataStartIndex));

        for (var i = dataStartIndex; i < rows.Count; i++) dataRows.Add(new ConfigRow(fields, rows[i]));

        return new ConfigTable(name, fields, dataRows);
    }

    /// <summary>
    ///     根据索引获取数据行
    /// </summary>
    public ConfigRow? GetRow(int index)
    {
        if (index < 0 || index >= Rows.Count) return null;

        return Rows[index];
    }

    /// <summary>
    ///     根据行索引和字段名获取类型转换后的值
    /// </summary>
    public T GetField<T>(int rowIndex, string fieldName)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex), $"行索引超出范围：{rowIndex}");

        return Rows[rowIndex].GetField<T>(fieldName);
    }

    /// <summary>
    ///     根据条件筛选数据行
    /// </summary>
    public IEnumerable<ConfigRow> Where(Func<ConfigRow, bool> predicate)
    {
        return Rows.Where(predicate);
    }
}