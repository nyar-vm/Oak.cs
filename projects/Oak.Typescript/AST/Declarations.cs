using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.AST;

#region 模块声明

/// <summary>
///     编译单元（根节点）
/// </summary>
public sealed record TsCompilationUnit(IReadOnlyList<TsAstNode> Declarations, TextSpan Span)
    : TsAstNode(Span)
{
    public TsCompilationUnit(IReadOnlyList<TsAstNode> Declarations)
        : this(Declarations, default(TextSpan))
    {
    }
}

/// <summary>
///     导入声明
/// </summary>
public sealed record TsImportDecl(
    string ModulePath,
    string? Alias,
    bool IsTypeOnly,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsImportDecl(string ModulePath, string? Alias, bool IsTypeOnly)
        : this(ModulePath, Alias, IsTypeOnly, default(TextSpan))
    {
    }
}

/// <summary>
///     导出声明
/// </summary>
public sealed record TsExportDecl(string Name, TsAstNode Value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsExportDecl(string Name, TsAstNode Value)
        : this(Name, Value, default(TextSpan))
    {
    }
}

/// <summary>
///     命名空间声明
/// </summary>
public sealed record TsNamespaceDecl(
    string Name,
    IReadOnlyList<TsAstNode> Members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsNamespaceDecl(string Name, IReadOnlyList<TsAstNode> Members)
        : this(Name, Members, default(TextSpan))
    {
    }
}

#endregion

#region 变量与函数声明

/// <summary>
///     变量声明
/// </summary>
public sealed record TsVariableDecl(
    string Name,
    TsAstNode? TypeAnnotation,
    TsAstNode? Initializer,
    bool IsConst,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsVariableDecl(string Name, TsAstNode? TypeAnnotation, TsAstNode? Initializer, bool IsConst)
        : this(Name, TypeAnnotation, Initializer, IsConst, default(TextSpan))
    {
    }
}

/// <summary>
///     函数声明
/// </summary>
public sealed record TsFunctionDecl(
    string Name,
    IReadOnlyList<TsParameter> Parameters,
    TsAstNode? ReturnType,
    TsAstNode Body,
    bool IsAsync,
    bool IsGenerator,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsFunctionDecl(
        string Name,
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

#endregion

#region 类型声明

/// <summary>
///     类声明
/// </summary>
public sealed record TsClassDecl(
    string Name,
    IReadOnlyList<TsAstNode> Members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsClassDecl(string Name, IReadOnlyList<TsAstNode> Members)
        : this(Name, Members, default(TextSpan))
    {
    }
}

/// <summary>
///     接口声明
/// </summary>
public sealed record TsInterfaceDecl(
    string Name,
    IReadOnlyList<TsAstNode> Members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsInterfaceDecl(string Name, IReadOnlyList<TsAstNode> Members)
        : this(Name, Members, default(TextSpan))
    {
    }
}

/// <summary>
///     类型别名声明
/// </summary>
public sealed record TsTypeAliasDecl(string Name, TsAstNode Type, TextSpan Span)
    : TsAstNode(Span)
{
    public TsTypeAliasDecl(string Name, TsAstNode Type)
        : this(Name, Type, default(TextSpan))
    {
    }
}

/// <summary>
///     枚举声明
/// </summary>
public sealed record TsEnumDecl(
    string Name,
    IReadOnlyList<TsEnumMember> Members,
    bool IsConst,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsEnumDecl(string Name, IReadOnlyList<TsEnumMember> Members, bool IsConst)
        : this(Name, Members, IsConst, default(TextSpan))
    {
    }
}

/// <summary>
///     枚举成员
/// </summary>
public sealed record TsEnumMember(string Name, TsAstNode? Initializer, TextSpan Span)
    : TsAstNode(Span)
{
    public TsEnumMember(string Name, TsAstNode? Initializer)
        : this(Name, Initializer, default(TextSpan))
    {
    }
}

#endregion
