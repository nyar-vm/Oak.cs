namespace Oak.Csv;

/// <summary>
///     TSV 格式解析器
/// </summary>
public sealed class TsvParser
{
    /// <summary>
    ///     解析 TSV 文本为行数据
    /// </summary>
    public static List<IReadOnlyList<string>> ParseRows(string content)
    {
        var rows = new List<IReadOnlyList<string>>();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim('\r', '\n');

            if (string.IsNullOrEmpty(trimmedLine)) continue;

            rows.Add(trimmedLine.Split('\t'));
        }

        return rows;
    }
}