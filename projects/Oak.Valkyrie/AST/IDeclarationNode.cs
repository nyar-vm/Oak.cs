using Oak.Syntax;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST;

/// <summary>
///     声明节点接口，统一访问 Name、Attributes、Span
/// </summary>
public interface IDeclarationNode
{
    Annotations Annotations { get; }
    
    /// <summary>
    ///     文档注释
    /// </summary>
    string DocumentText => Annotations.DocumentText();

    /// <summary>
    ///     属性列表
    /// </summary>
    IReadOnlyList<AttributeItem> Attributes => Annotations.Attributes();

    /// <summary>
    ///     修饰符列表
    /// </summary>
    IReadOnlyList<IdentifierNode> Modifiers => Annotations.Modifiers;


    /// <summary>
    ///     声明名称
    /// </summary>
    IdentifierNode? Name { get; }


    /// <summary>
    ///     源代码位置范围
    /// </summary>
    TextSpan Span { get; }
}
