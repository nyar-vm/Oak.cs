using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     强类型语法标记包装器
/// </summary>
public readonly struct SyntaxToken
{
    /// <summary>
    ///     标记类型
    /// </summary>
    public NodeKind Kind { get; }

    /// <summary>
    ///     标记在源文本中的范围
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    ///     标记的原始文本
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     初始化语法标记
    /// </summary>
    public SyntaxToken(NodeKind kind, TextSpan span, string text)
    {
        Kind = kind;
        Span = span;
        Text = text;
    }

    public override string ToString()
    {
        return Text;
    }
}