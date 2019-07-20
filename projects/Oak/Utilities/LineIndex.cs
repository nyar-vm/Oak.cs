using Oak.Syntax;

namespace Oak.Utilities;

/// <summary>
///     行索引，用于在字节偏移量与行/列位置之间进行转换
/// </summary>
public sealed class LineIndex
{
    private readonly int[] _lineStarts;

    /// <summary>
    ///     从源文本构建行索引
    /// </summary>
    public LineIndex(ISource source)
    {
        var lineStarts = new List<int> { 0 };
        for (var i = 0; i < source.Length; i++)
            if (source[i] == '\n')
            {
                lineStarts.Add(i + 1);
            }
            else if (source[i] == '\r')
            {
                if (i + 1 < source.Length && source[i + 1] == '\n') i++;
                lineStarts.Add(i + 1);
            }

        _lineStarts = lineStarts.ToArray();
    }

    /// <summary>
    ///     总行数
    /// </summary>
    public int LineCount => _lineStarts.Length;

    /// <summary>
    ///     将字节偏移量转换为行号和列号（均从 1 开始）
    /// </summary>
    public (int Line, int Column) GetLineColumn(int offset)
    {
        var line = Array.BinarySearch(_lineStarts, offset);
        if (line < 0) line = ~line - 1;
        var column = offset - _lineStarts[line];
        return (line + 1, column + 1);
    }

    /// <summary>
    ///     将行号和列号（均从 1 开始）转换为字节偏移量，越界时返回 -1
    /// </summary>
    public int GetOffset(int line, int column)
    {
        if (line < 1 || line > _lineStarts.Length) return -1;
        return _lineStarts[line - 1] + column - 1;
    }

    /// <summary>
    ///     获取指定行的起始字节偏移量，越界时返回 -1
    /// </summary>
    public int GetLineStart(int line)
    {
        if (line < 1 || line > _lineStarts.Length) return -1;
        return _lineStarts[line - 1];
    }

    /// <summary>
    ///     获取指定行的结束字节偏移量（不含换行符），越界时返回 -1
    /// </summary>
    public int GetLineEnd(int line)
    {
        if (line < 1 || line > _lineStarts.Length) return -1;
        if (line == _lineStarts.Length) return _lineStarts[^1];
        return _lineStarts[line] - 1;
    }
}