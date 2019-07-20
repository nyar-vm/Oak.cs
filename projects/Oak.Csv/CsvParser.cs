using System.Text;

namespace Oak.Csv;

/// <summary>
///     CSV 格式解析器
/// </summary>
public sealed class CsvParser
{
    /// <summary>
    ///     解析 CSV 文本为行数据
    /// </summary>
    public static List<IReadOnlyList<string>> ParseRows(string content)
    {
        var rows = new List<IReadOnlyList<string>>();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim('\r', '\n');

            if (string.IsNullOrEmpty(trimmedLine)) continue;

            rows.Add(ParseLine(trimmedLine));
        }

        return rows;
    }

    /// <summary>
    ///     解析单行 CSV
    /// </summary>
    public static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString());

        return fields;
    }
}