using Oak.Syntax;

namespace Oak.Diagnostics;

/// <summary>
///     单条诊断消息
/// </summary>
public sealed class DiagnosticMessage
{
    public DiagnosticMessage(
        DiagnosticLevel level,
        string code,
        string message,
        TextSpan span = default(TextSpan),
        string filePath = "",
        IReadOnlyList<string>? suggestions = null,
        IReadOnlyList<CodeFix>? codeFixes = null)
    {
        Level = level;
        Code = code;
        Message = message;
        Span = span;
        FilePath = filePath;
        Suggestions = suggestions ?? [];
        CodeFixes = codeFixes ?? [];
    }

    /// <summary>
    ///     严重级别
    /// </summary>
    public DiagnosticLevel Level { get; }

    /// <summary>
    ///     诊断代码
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     诊断消息
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     源码范围
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    ///     文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     修正建议文本列表
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; }

    /// <summary>
    ///     代码修复建议列表
    /// </summary>
    public IReadOnlyList<CodeFix> CodeFixes { get; }

    /// <summary>
    ///     返回格式化诊断消息字符串
    /// </summary>
    public override string ToString()
    {
        var levelStr = Level.ToString().ToLowerInvariant();
        var baseMsg = Span.Start > 0
            ? $"{FilePath}[{Span.Start}..{Span.End}): {levelStr} {Code}: {Message}"
            : $"{FilePath}: {levelStr} {Code}: {Message}";

        if (Suggestions.Count > 0)
        {
            baseMsg += $" (建议: {string.Join(", ", Suggestions)})";
        }

        if (CodeFixes.Count > 0)
        {
            baseMsg += $" (修复: {string.Join("; ", CodeFixes.Select(f => f.ToString()))})";
        }

        return baseMsg;
    }
}
