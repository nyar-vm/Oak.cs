using Oak.Diagnostics;

namespace Oak.Parsing;

/// <summary>
///     解析器基类，提供通用解析逻辑
/// </summary>
public abstract class ParserBase<TInput, TOutput> : IParser<TInput, TOutput>
{
    protected DiagnosticSink? Diagnostics;

    protected ParserBase(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     执行解析，子类应重写此方法
    /// </summary>
    public abstract TOutput Parse(TInput input);
}