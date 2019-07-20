using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Ini;

public sealed class IniParser
{
    private DiagnosticSink? _diagnostics;

    public IniParseResult Parse(string source, DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;

        var globalEntries = new Dictionary<string, string>();
        var sections = new List<IniSection>();
        Dictionary<string, string> currentEntries = globalEntries;

        var position = 0;
        var line = 1;

        while (position < source.Length)
        {
            var lineEnd = source.IndexOf('\n', position);

            ReadOnlySpan<char> lineSpan;
            int nextPosition;

            if (lineEnd >= 0)
            {
                lineSpan = source.AsSpan(position, lineEnd - position).Trim();
                nextPosition = lineEnd + 1;
            }
            else
            {
                lineSpan = source.AsSpan(position).Trim();
                nextPosition = source.Length;
            }

            if (!lineSpan.IsEmpty)
            {
                if (lineSpan[0] == ';' || lineSpan[0] == '#')
                {
                    // 注释行，跳过
                }
                else if (lineSpan[0] == '[')
                {
                    var endBracket = lineSpan.IndexOf(']');

                    if (endBracket > 1)
                    {
                        var sectionName = lineSpan[1..endBracket].Trim().ToString();
                        var section = new IniSection { Name = sectionName };
                        sections.Add(section);
                        currentEntries = section.Entries;
                    }
                    else
                    {
                        _diagnostics?.AddWarning(
                            string.Empty,
                            default,
                            "INI001",
                            "无效的节头格式");
                    }
                }
                else
                {
                    ParseKeyValueLine(lineSpan, currentEntries, line);
                }
            }

            position = nextPosition;
            line++;
        }

        return new IniParseResult
        {
            GlobalEntries = globalEntries,
            Sections = sections,
            Diagnostics = _diagnostics?.Messages ?? []
        };
    }

    private void ParseKeyValueLine(ReadOnlySpan<char> line, Dictionary<string, string> entries, int lineNumber)
    {
        var eqIndex = line.IndexOf('=');

        if (eqIndex < 0)
        {
            eqIndex = line.IndexOf(':');
        }

        if (eqIndex <= 0) return;

        var key = line[..eqIndex].Trim().ToString();
        var valueSpan = line[(eqIndex + 1)..].Trim();

        var value = StripInlineComment(valueSpan);

        entries[key] = value;
    }

    private static string StripInlineComment(ReadOnlySpan<char> span)
    {
        var inQuote = false;
        var quoteChar = '\0';

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];

            if (inQuote)
            {
                if (c == quoteChar) inQuote = false;
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            if (c is ';' or '#')
            {
                return span[..i].Trim().ToString();
            }
        }

        return span.Trim().ToString();
    }
}
