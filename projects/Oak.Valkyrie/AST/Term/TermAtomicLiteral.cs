using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Term;

/// <summary>
///     字面量表达式，表示源代码中的常量值
/// </summary>
/// <para>根据 <see cref="LiteralKind"/> 不同，<see cref="Value"/> 类型也不同</para>
/// <para>示例：</para>
/// <code>
/// 42               // LiteralKind = Number,   Value = 42
/// true             // LiteralKind = Boolean,  Value = true
/// "hello"          // LiteralKind = String,   Value = "hello"
/// null             // LiteralKind = Null,     Value = null
/// </code>
public sealed record TermAtomicLiteral : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermAtomicLiteral() { }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermAtomicLiteral(LiteralType literalKind, string? value)
    {
        LiteralKind = literalKind;
        Value = value;
    }

    /// <summary>
    ///     字面量类型
    /// </summary>
    public LiteralType LiteralKind { get; init; }

    /// <summary>
    ///     字面量值
    /// </summary>
    public object? Value { get; init; }
}
