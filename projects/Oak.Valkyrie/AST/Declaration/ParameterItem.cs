using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
///     函数参数项声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro greet(name: utf8, times: i32)
/// //       └─── ParameterDecl { Name = "name", ParamType = TypeAnnotation("utf8") }
/// //                               └─── ParameterDecl { Name = "times", ParamType = TypeAnnotation("i32") }
/// </code>
public sealed record ParameterItem : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations Annotations { get; init; } = new();

    /// <summary>
    ///     参数名称
    /// </summary>
    public IdentifierNode? Name { get; init; }

    /// <summary>
    ///     参数类型注解
    /// </summary>
    public TypeNode? Typing { get; init; }

    /// <summary>
    ///     默认值表达式
    /// </summary>
    public TermNode? DefaultValue { get; init; }

}
