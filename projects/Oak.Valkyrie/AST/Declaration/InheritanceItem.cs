using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

public sealed record InheritanceItem : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    public IdentifierNode? Name { get; init; } = null;
    
    /// <summary>
    ///     基类或接口的类型注解
    /// </summary>
    public TypeNode BaseType { get; init; } = new();
}
