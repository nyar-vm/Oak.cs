using Oak.Syntax;
namespace Oak.Diagnostics;

/// <summary>
///     源代码中的行列位置范围。
///     此类型仅作为便利工具保留，Oak 核心不存储行列信息。
///     行列信息应由 LineIndex 根据 offset 按需计算。
/// </summary>
[Obsolete("Oak 核心不存储行列信息。请使用 TextSpan + LineIndex 按需计算行列。")]
public readonly record struct SourceSpan
{
    public SourceSpan(int startLine, int startColumn, int endLine, int endColumn, string? filePath = null)
    {
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        FilePath = filePath;
    }

    /// <summary>
    ///     起始行（从 1 开始）
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    ///     起始列（从 1 开始）
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    ///     结束行
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    ///     结束列
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    ///     所属文件路径
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    ///     创建单行范围
    /// </summary>
    public static SourceSpan SingleLine(int line, int column, int length = 1)
    {
        return new SourceSpan(line, column, line, column + length);
    }

    public override string ToString()
    {
        return FilePath is not null
            ? $"{FilePath}:({StartLine},{StartColumn})-({EndLine},{EndColumn})"
            : $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";
    }
}
