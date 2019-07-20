using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.AST;

#region 类型注解与基础类型

/// <summary>
///     类型注解
/// </summary>
public sealed record TsTypeAnnotation(TsAstNode Type, TextSpan Span)
    : TsAstNode(Span)
{
    public TsTypeAnnotation(TsAstNode Type)
        : this(Type, default(TextSpan))
    {
    }
}

/// <summary>
///     原始类型
/// </summary>
public sealed record TsPrimitiveType(string Name, TextSpan Span)
    : TsAstNode(Span)
{
    public TsPrimitiveType(string Name)
        : this(Name, default(TextSpan))
    {
    }
}

#endregion

#region 复合类型

/// <summary>
///     联合类型
/// </summary>
public sealed record TsUnionType(IReadOnlyList<TsAstNode> Types, TextSpan Span)
    : TsAstNode(Span)
{
    public TsUnionType(IReadOnlyList<TsAstNode> Types)
        : this(Types, default(TextSpan))
    {
    }
}

/// <summary>
///     数组类型
/// </summary>
public sealed record TsArrayType(TsAstNode ElementType, TextSpan Span)
    : TsAstNode(Span)
{
    public TsArrayType(TsAstNode ElementType)
        : this(ElementType, default(TextSpan))
    {
    }
}

#endregion