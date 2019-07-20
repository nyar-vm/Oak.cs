using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.AST;

#region 基础表达式

/// <summary>
///     标识符表达式
/// </summary>
public sealed record TsIdentifier(string Name, TextSpan Span)
    : TsAstNode(Span)
{
    public TsIdentifier(string Name)
        : this(Name, default(TextSpan))
    {
    }
}

/// <summary>
///     字面量表达式
/// </summary>
public sealed record TsLiteral(string Kind, string Value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsLiteral(string Kind, string Value)
        : this(Kind, Value, default(TextSpan))
    {
    }
}

/// <summary>
///     this 表达式
/// </summary>
public sealed record TsThisExpr(TextSpan Span)
    : TsAstNode(Span)
{
    public TsThisExpr()
        : this(default(TextSpan))
    {
    }
}

/// <summary>
///     super 表达式
/// </summary>
public sealed record TsSuperExpr(TextSpan Span)
    : TsAstNode(Span)
{
    public TsSuperExpr()
        : this(default(TextSpan))
    {
    }
}

#endregion

#region 运算表达式

/// <summary>
///     二元表达式
/// </summary>
public sealed record TsBinaryExpr(
    TsAstNode Left,
    string Operator,
    TsAstNode Right,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsBinaryExpr(TsAstNode Left, string Operator, TsAstNode Right)
        : this(Left, Operator, Right, default(TextSpan))
    {
    }
}

/// <summary>
///     一元表达式
/// </summary>
public sealed record TsUnaryExpr(
    string Operator,
    TsAstNode Operand,
    bool IsPrefix,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsUnaryExpr(string Operator, TsAstNode Operand, bool IsPrefix)
        : this(Operator, Operand, IsPrefix, default(TextSpan))
    {
    }
}

/// <summary>
///     赋值表达式
/// </summary>
public sealed record TsAssignmentExpr(
    TsAstNode Left,
    string Operator,
    TsAstNode Right,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsAssignmentExpr(TsAstNode Left, string Operator, TsAstNode Right)
        : this(Left, Operator, Right, default(TextSpan))
    {
    }
}

/// <summary>
///     条件表达式（三元运算符）
/// </summary>
public sealed record TsConditionalExpr(
    TsAstNode Condition,
    TsAstNode ThenBranch,
    TsAstNode ElseBranch,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsConditionalExpr(TsAstNode Condition, TsAstNode ThenBranch, TsAstNode ElseBranch)
        : this(Condition, ThenBranch, ElseBranch, default(TextSpan))
    {
    }
}

/// <summary>
///     typeof 表达式
/// </summary>
public sealed record TsTypeofExpr(TsAstNode Operand, TextSpan Span)
    : TsAstNode(Span)
{
    public TsTypeofExpr(TsAstNode Operand)
        : this(Operand, default(TextSpan))
    {
    }
}

/// <summary>
///     instanceof 表达式
/// </summary>
public sealed record TsInstanceofExpr(
    TsAstNode Left,
    TsAstNode Right,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsInstanceofExpr(TsAstNode Left, TsAstNode Right)
        : this(Left, Right, default(TextSpan))
    {
    }
}

/// <summary>
///     yield 表达式
/// </summary>
public sealed record TsYieldExpr(TsAstNode? Value, bool IsDelegate, TextSpan Span)
    : TsAstNode(Span)
{
    public TsYieldExpr(TsAstNode? Value, bool IsDelegate)
        : this(Value, IsDelegate, default(TextSpan))
    {
    }
}

#endregion

#region 调用与成员访问

/// <summary>
///     调用表达式
/// </summary>
public sealed record TsCallExpr(
    TsAstNode Callee,
    IReadOnlyList<TsAstNode> Arguments,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsCallExpr(TsAstNode Callee, IReadOnlyList<TsAstNode> Arguments)
        : this(Callee, Arguments, default(TextSpan))
    {
    }
}

/// <summary>
///     成员表达式
/// </summary>
public sealed record TsMemberExpr(TsAstNode Object, string Member, TextSpan Span)
    : TsAstNode(Span)
{
    public TsMemberExpr(TsAstNode Object, string Member)
        : this(Object, Member, default(TextSpan))
    {
    }
}

/// <summary>
///     属性访问表达式
/// </summary>
public sealed record TsPropertyAccess(TsAstNode Object, string Property, TextSpan Span)
    : TsAstNode(Span)
{
    public TsPropertyAccess(TsAstNode Object, string Property)
        : this(Object, Property, default(TextSpan))
    {
    }
}

/// <summary>
///     元素访问表达式
/// </summary>
public sealed record TsElementAccess(
    TsAstNode Object,
    TsAstNode Index,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsElementAccess(TsAstNode Object, TsAstNode Index)
        : this(Object, Index, default(TextSpan))
    {
    }
}

/// <summary>
///     new 表达式
/// </summary>
public sealed record TsNewExpr(
    TsAstNode Callee,
    IReadOnlyList<TsAstNode> Arguments,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsNewExpr(TsAstNode Callee, IReadOnlyList<TsAstNode> Arguments)
        : this(Callee, Arguments, default(TextSpan))
    {
    }
}

#endregion

#region 函数与集合表达式

/// <summary>
///     箭头函数表达式
/// </summary>
public sealed record TsArrowFunctionExpr(
    IReadOnlyList<TsParameter> Parameters,
    TsAstNode? ReturnType,
    TsAstNode Body,
    bool IsAsync,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsArrowFunctionExpr(
        IReadOnlyList<TsParameter> Parameters,
        TsAstNode? ReturnType,
        TsAstNode Body,
        bool IsAsync
    )
        : this(Parameters, ReturnType, Body, IsAsync, default(TextSpan))
    {
    }
}

/// <summary>
///     函数表达式
/// </summary>
public sealed record TsFunctionExpr(
    string? Name,
    IReadOnlyList<TsParameter> Parameters,
    TsAstNode? ReturnType,
    TsAstNode Body,
    bool IsAsync,
    bool IsGenerator,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsFunctionExpr(
        string? Name,
        IReadOnlyList<TsParameter> Parameters,
        TsAstNode? ReturnType,
        TsAstNode Body,
        bool IsAsync,
        bool IsGenerator
    )
        : this(Name, Parameters, ReturnType, Body, IsAsync, IsGenerator, default(TextSpan))
    {
    }
}

/// <summary>
///     类表达式
/// </summary>
public sealed record TsClassExpr(
    string? Name,
    IReadOnlyList<TsAstNode> Members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsClassExpr(string? Name, IReadOnlyList<TsAstNode> Members)
        : this(Name, Members, default(TextSpan))
    {
    }
}

/// <summary>
///     数组字面量
/// </summary>
public sealed record TsArrayLiteral(
    IReadOnlyList<TsAstNode> Elements,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsArrayLiteral(IReadOnlyList<TsAstNode> Elements)
        : this(Elements, default(TextSpan))
    {
    }
}

/// <summary>
///     对象字面量
/// </summary>
public sealed record TsObjectLiteral(
    IReadOnlyList<TsProperty> Properties,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsObjectLiteral(IReadOnlyList<TsProperty> Properties)
        : this(Properties, default(TextSpan))
    {
    }
}

/// <summary>
///     模板字面量插值
/// </summary>
public sealed record TsTemplateLiteral(
    IReadOnlyList<TsAstNode> Parts,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsTemplateLiteral(IReadOnlyList<TsAstNode> Parts)
        : this(Parts, default(TextSpan))
    {
    }
}

/// <summary>
///     展开元素
/// </summary>
public sealed record TsSpreadElement(TsAstNode Argument, TextSpan Span)
    : TsAstNode(Span)
{
    public TsSpreadElement(TsAstNode Argument)
        : this(Argument, default(TextSpan))
    {
    }
}

#endregion
