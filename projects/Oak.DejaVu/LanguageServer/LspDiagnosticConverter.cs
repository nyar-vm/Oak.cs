using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.DejaVu.LanguageServer;

/// <summary>
///     LSP 诊断转换器——将 Oak DiagnosticMessage 转换为 LSP 兼容的 JSON 结构。
///     输出可直接序列化为 LSP `textDocument/publishDiagnostics` 的 `Diagnostic[]`。
/// </summary>
public sealed class LspDiagnosticConverter
{
    /// <summary>
    ///     将 Oak 诊断消息转换为 LSP 诊断对象列表
    /// </summary>
    /// <param name="messages">Oak 诊断消息列表</param>
    /// <returns>LSP 诊断对象列表</returns>
    public static List<LspDiagnostic> Convert(IReadOnlyList<DiagnosticMessage> messages)
    {
        var result = new List<LspDiagnostic>(messages.Count);

        foreach (var msg in messages)
        {
            result.Add(ConvertOne(msg));
        }

        return result;
    }

    /// <summary>
    ///     将 Oak DiagnosticSink 转换为 LSP 诊断对象列表
    /// </summary>
    public static List<LspDiagnostic> Convert(DiagnosticSink sink)
    {
        return Convert(sink.Messages);
    }

    private static LspDiagnostic ConvertOne(DiagnosticMessage msg)
    {
        return new LspDiagnostic
        {
            Range = TextSpanToLspRange(msg.Span),
            Severity = LevelToSeverity(msg.Level),
            Code = msg.Code,
            Source = "dejavu",
            Message = msg.Message,
            RelatedInformation = ConvertRelatedInfo(msg)
        };
    }

    private static LspRange TextSpanToLspRange(TextSpan span)
    {
        if (span == default)
        {
            return new LspRange { Start = new LspPosition { Line = 0, Character = 0 }, End = new LspPosition { Line = 0, Character = 0 } };
        }

        return new LspRange
        {
            Start = new LspPosition { Line = span.Start, Character = 0 },
            End = new LspPosition { Line = span.End, Character = 0 }
        };
    }

    private static int LevelToSeverity(DiagnosticLevel level)
    {
        return level switch
        {
            DiagnosticLevel.Error => 1,
            DiagnosticLevel.Warning => 2,
            DiagnosticLevel.Info => 3,
            DiagnosticLevel.Hint => 4,
            _ => 2
        };
    }

    private static List<LspDiagnosticRelatedInformation>? ConvertRelatedInfo(DiagnosticMessage msg)
    {
        var related = new List<LspDiagnosticRelatedInformation>();

        foreach (var suggestion in msg.Suggestions)
        {
            related.Add(new LspDiagnosticRelatedInformation
            {
                Location = new LspLocation
                {
                    Uri = msg.FilePath,
                    Range = TextSpanToLspRange(msg.Span)
                },
                Message = $"建议: {suggestion}"
            });
        }

        foreach (var fix in msg.CodeFixes)
        {
            related.Add(new LspDiagnosticRelatedInformation
            {
                Location = new LspLocation
                {
                    Uri = msg.FilePath,
                    Range = TextSpanToLspRange(msg.Span)
                },
                Message = $"修复: {fix.Description}"
            });
        }

        return related.Count > 0 ? related : null;
    }
}

/// <summary>
///     LSP Diagnostic 对象
/// </summary>
public sealed class LspDiagnostic
{
    /// <summary>
    ///     诊断范围
    /// </summary>
    public LspRange Range { get; init; } = new();

    /// <summary>
    ///     严重级别（1=Error, 2=Warning, 3=Info, 4=Hint）
    /// </summary>
    public int Severity { get; init; }

    /// <summary>
    ///     诊断代码
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    ///     诊断来源
    /// </summary>
    public string Source { get; init; } = "dejavu";

    /// <summary>
    ///     诊断消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///     相关信息
    /// </summary>
    public List<LspDiagnosticRelatedInformation>? RelatedInformation { get; init; }
}

/// <summary>
///     LSP Range 对象
/// </summary>
public sealed class LspRange
{
    /// <summary>
    ///     起始位置
    /// </summary>
    public LspPosition Start { get; init; } = new();

    /// <summary>
    ///     结束位置
    /// </summary>
    public LspPosition End { get; init; } = new();
}

/// <summary>
///     LSP Position 对象
/// </summary>
public sealed class LspPosition
{
    /// <summary>
    ///     行号（0-based）
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    ///     列号（0-based，UTF-16 代码单元）
    /// </summary>
    public int Character { get; init; }
}

/// <summary>
///     LSP Location 对象
/// </summary>
public sealed class LspLocation
{
    /// <summary>
    ///     文档 URI
    /// </summary>
    public string Uri { get; init; } = string.Empty;

    /// <summary>
    ///     范围
    /// </summary>
    public LspRange Range { get; init; } = new();
}

/// <summary>
///     LSP DiagnosticRelatedInformation 对象
/// </summary>
public sealed class LspDiagnosticRelatedInformation
{
    /// <summary>
    ///     相关位置
    /// </summary>
    public LspLocation Location { get; init; } = new();

    /// <summary>
    ///     相关消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
///     LSP CompletionItem 对象
/// </summary>
public sealed class LspCompletionItem
{
    /// <summary>
    ///     补全标签
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    ///     补全类型（1=Text, 2=Method, 3=Function, 5=Field, 6=Variable, 9=Module, 11=File, 12=Folder, 13=Class, 14=Interface, 15=Color, 17=Keyword, 18=Snippet）
    /// </summary>
    public int Kind { get; init; }

    /// <summary>
    ///     补全详情
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    ///     补全文档
    /// </summary>
    public string Documentation { get; init; } = string.Empty;

    /// <summary>
    ///     插入文本
    /// </summary>
    public string InsertText { get; init; } = string.Empty;
}

/// <summary>
///     LSP Hover 对象
/// </summary>
public sealed class LspHover
{
    /// <summary>
    ///     悬停范围
    /// </summary>
    public LspRange Range { get; init; } = new();

    /// <summary>
    ///     悬停内容（Markdown 格式）
    /// </summary>
    public string Contents { get; init; } = string.Empty;
}
