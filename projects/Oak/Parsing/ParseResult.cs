using Oak.Diagnostics;

namespace Oak.Parsing;

/// <summary>
///     解析结果
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public sealed class ParseResult<T>
{
    private ParseResult(bool success, T? value, IReadOnlyList<DiagnosticMessage> diagnostics)
    {
        Success = success;
        Value = value;
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     是否解析成功
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     解析结果值
    /// </summary>
    public T? Value { get; }

    /// <summary>
    ///     诊断消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Diagnostics { get; }

    /// <summary>
    ///     创建成功结果
    /// </summary>
    public static ParseResult<T> Ok(T value, IReadOnlyList<DiagnosticMessage>? diagnostics = null)
    {
        return new ParseResult<T>(true, value, diagnostics ?? []);
    }

    /// <summary>
    ///     创建失败结果
    /// </summary>
    public static ParseResult<T> Fail(IReadOnlyList<DiagnosticMessage> diagnostics)
    {
        return new ParseResult<T>(false, default, diagnostics);
    }
}