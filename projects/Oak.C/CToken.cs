using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.C;

/// <summary>
///     C 语言词法单元
/// </summary>
public readonly struct CToken
{
    public NodeKind Kind { get; }
    public string Text { get; }

    public CToken(NodeKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    public TextSpan ToSourceSpan()
    {
        return default;
    }

    public override string ToString()
    {
        return $"[{Kind}] '{Text}' ";
    }
}
