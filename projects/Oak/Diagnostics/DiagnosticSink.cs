using Oak.Syntax;

namespace Oak.Diagnostics;

/// <summary>
///     诊断消息收集器
/// </summary>
public sealed class DiagnosticSink
{
    private readonly List<DiagnosticMessage> _messages = [];

    /// <summary>
    ///     所有诊断消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Messages => _messages;

    /// <summary>
    ///     是否存在错误
    /// </summary>
    public bool HasErrors => _messages.Any(m => m.Level == DiagnosticLevel.Error);

    /// <summary>
    ///     是否存在警告
    /// </summary>
    public bool HasWarnings => _messages.Any(m => m.Level == DiagnosticLevel.Warning);

    /// <summary>
    ///     是否存在提示
    /// </summary>
    public bool HasHints => _messages.Any(m => m.Level == DiagnosticLevel.Hint);

    /// <summary>
    ///     是否存在需要注意的消息（错误或警告或提示）
    /// </summary>
    public bool HasAny => _messages.Count > 0;

    /// <summary>
    ///     仅错误消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Errors => _messages.Where(m => m.Level == DiagnosticLevel.Error).ToList();

    /// <summary>
    ///     仅警告消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Warnings => _messages.Where(m => m.Level == DiagnosticLevel.Warning).ToList();

    /// <summary>
    ///     仅提示消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Hints => _messages.Where(m => m.Level == DiagnosticLevel.Hint).ToList();

    /// <summary>
    ///     仅信息消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Infos => _messages.Where(m => m.Level == DiagnosticLevel.Info).ToList();

    /// <summary>
    ///     按指定级别过滤消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> GetMessages(DiagnosticLevel level)
    {
        return _messages.Where(m => m.Level == level).ToList();
    }

    /// <summary>
    ///     添加信息
    /// </summary>
    public void AddInfo(string filePath, TextSpan span, string code, string message,
        IReadOnlyList<CodeFix>? codeFixes = null)
    {
        _messages.Add(new DiagnosticMessage(DiagnosticLevel.Info, code, message, span, filePath,
            codeFixes: codeFixes));
    }

    /// <summary>
    ///     添加警告
    /// </summary>
    public void AddWarning(string filePath, TextSpan span, string code, string message,
        IReadOnlyList<string>? suggestions = null,
        IReadOnlyList<CodeFix>? codeFixes = null)
    {
        _messages.Add(new DiagnosticMessage(DiagnosticLevel.Warning, code, message, span, filePath,
            suggestions, codeFixes));
    }

    /// <summary>
    ///     添加错误
    /// </summary>
    public void AddError(string filePath, TextSpan span, string code, string message,
        IReadOnlyList<string>? suggestions = null,
        IReadOnlyList<CodeFix>? codeFixes = null)
    {
        _messages.Add(new DiagnosticMessage(DiagnosticLevel.Error, code, message, span, filePath,
            suggestions, codeFixes));
    }

    /// <summary>
    ///     添加提示
    /// </summary>
    public void AddHint(string filePath, TextSpan span, string code, string message,
        IReadOnlyList<string>? suggestions = null,
        IReadOnlyList<CodeFix>? codeFixes = null)
    {
        _messages.Add(new DiagnosticMessage(DiagnosticLevel.Hint, code, message, span, filePath,
            suggestions, codeFixes));
    }

    /// <summary>
    ///     格式化所有消息为字符串
    /// </summary>
    public string FormatAll()
    {
        return string.Join('\n', _messages.Select(m => m.ToString()));
    }

    /// <summary>
    ///     清空所有消息
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }
}
