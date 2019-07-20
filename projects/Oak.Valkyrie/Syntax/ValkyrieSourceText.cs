using Oak.Syntax;
using Oak.Text;
using Oak.Utilities;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 源文本，封装 ISource 并集成 LineIndex 提供行列导航
/// </summary>
public sealed class ValkyrieSourceText : ISource
{
    private readonly ISource _source;
    private readonly LineIndex _lineIndex;

    /// <summary>
    ///     从字符串创建源文本
    /// </summary>
    public ValkyrieSourceText(string text)
    {
        _source = new StringSource(text);
        _lineIndex = new LineIndex(_source);
    }

    /// <summary>
    ///     从 ISource 创建源文本
    /// </summary>
    public ValkyrieSourceText(ISource source)
    {
        _source = source;
        _lineIndex = new LineIndex(_source);
    }

    /// <inheritdoc />
    public char this[int index] => _source[index];

    /// <inheritdoc />
    public int Length => _source.Length;

    /// <inheritdoc />
    public string Substring(Range range)
    {
        return _source.Substring(range);
    }

    /// <summary>
    ///     行索引
    /// </summary>
    public LineIndex LineIndex => _lineIndex;

    /// <summary>
    ///     总行数
    /// </summary>
    public int LineCount => _lineIndex.LineCount;

    /// <summary>
    ///     根据偏移量获取行号和列号
    /// </summary>
    public (int Line, int Column) GetLineAndColumn(int offset)
    {
        return _lineIndex.GetLineColumn(offset);
    }

    /// <summary>
    ///     根据行号和列号获取偏移量
    /// </summary>
    public int GetOffset(int line, int column)
    {
        return _lineIndex.GetOffset(line, column);
    }

    /// <summary>
    ///     获取指定行的起始偏移量
    /// </summary>
    public int GetLineStart(int line)
    {
        return _lineIndex.GetLineStart(line);
    }

    /// <summary>
    ///     获取指定行的结束偏移量（使用源文本长度修正 LineIndex 对最后一行的错误）
    /// </summary>
    public int GetLineEnd(int line)
    {
        var rawEnd = _lineIndex.GetLineEnd(line);
        // LineIndex 对最后一行返回的是行首位置，修正为源文本长度
        if (rawEnd >= 0 && rawEnd < _source.Length && line == _lineIndex.LineCount)
        {
            return _source.Length;
        }

        return rawEnd;
    }

    /// <summary>
    ///     获取指定行的文本内容
    /// </summary>
    public string GetLineText(int line)
    {
        var start = GetLineStart(line);
        var end = GetLineEnd(line);
        return _source.Substring(new Range(start, end));
    }

    /// <summary>
    ///     根据 TextSpan 获取对应的源文本
    /// </summary>
    public string GetText(TextSpan span)
    {
        return _source.Substring(new Range(span.Start, span.End));
    }

    /// <summary>
    ///     根据偏移量获取 TextPosition
    /// </summary>
    public TextPosition GetPosition(int offset)
    {
        var (line, column) = GetLineAndColumn(offset);
        return new TextPosition(offset, line, column);
    }

    /// <summary>
    ///     根据 TextSpan 获取起始和结束的 TextPosition
    /// </summary>
    public (TextPosition Start, TextPosition End) GetSpanPosition(TextSpan span)
    {
        return (GetPosition(span.Start), GetPosition(span.End));
    }

    public override string ToString()
    {
        return _source.ToString() ?? string.Empty;
    }
}
