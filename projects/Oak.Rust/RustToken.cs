using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Rust;

/// <summary>
///     Rust 词法单元
/// </summary>
public readonly struct RustToken
{
    public NodeKind Kind { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }

    public RustToken(NodeKind kind, string text, int line = 0, int column = 0)
    {
        Kind = kind;
        Text = text;
        Line = line;
        Column = column;
    }

    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
