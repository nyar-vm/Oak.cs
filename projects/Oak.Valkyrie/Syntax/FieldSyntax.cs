using Oak.Syntax;

namespace Oak.Valkyrie.Syntax;

/// <summary>
///     字段声明语法节点：Name: Type (= defaultValue)?
/// </summary>
public sealed class FieldSyntax : ValkyrieDeclarationSyntax
{
    public FieldSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset) { }

    /// <summary>字段名称</summary>
    public SyntaxToken Name => ChildToken(0);

    /// <summary>冒号</summary>
    public SyntaxToken Colon => ChildToken(1);

    /// <summary>类型名称</summary>
    public SyntaxToken TypeName => ChildToken(2);

    /// <summary>是否有默认值</summary>
    public bool HasDefaultValue => ChildCount >= 5;

    /// <summary>等号（仅在 HasDefaultValue 时有效）</summary>
    public SyntaxToken? EqualsToken => HasDefaultValue ? ChildToken(3) : null;

    /// <summary>默认值（仅在 HasDefaultValue 时有效）</summary>
    public SyntaxToken? DefaultValue => HasDefaultValue ? ChildToken(4) : null;
}