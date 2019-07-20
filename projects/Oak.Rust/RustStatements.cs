using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Rust;

#region 语句

/// <summary>
///     表达式语句
/// </summary>
public sealed record RustExprStmt(RustAstNode Expression, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     let 绑定语句
/// </summary>
public sealed record RustLetStmt(
    RustAstNode Pattern,
    RustAstNode? Type,
    RustAstNode? Initializer,
    bool IsMutable,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     return 语句
/// </summary>
public sealed record RustReturnStmt(RustAstNode? Value, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     break 语句
/// </summary>
public sealed record RustBreakStmt(RustAstNode? Value, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     continue 语句
/// </summary>
public sealed record RustContinueStmt(TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     while 循环
/// </summary>
public sealed record RustWhileStmt(RustAstNode Condition, RustAstNode Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     loop 循环
/// </summary>
public sealed record RustLoopStmt(RustAstNode Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     for 循环
/// </summary>
public sealed record RustForStmt(RustAstNode Pattern, RustAstNode Iterator, RustAstNode Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     函数定义
/// </summary>
public sealed record RustFunctionDef(
    string Name,
    IReadOnlyList<RustParam> Parameters,
    RustAstNode? ReturnType,
    RustAstNode Body,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     函数参数
/// </summary>
public sealed record RustParam(string Name, RustAstNode? Type, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     结构体定义
/// </summary>
public sealed record RustStructDef(
    string Name,
    IReadOnlyList<RustFieldDef> Fields,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     结构体字段
/// </summary>
public sealed record RustFieldDef(string Name, RustAstNode Type, bool IsPublic, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     枚举定义
/// </summary>
public sealed record RustEnumDef(
    string Name,
    IReadOnlyList<RustVariantDef> Variants,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     枚举变体
/// </summary>
public sealed record RustVariantDef(string Name, IReadOnlyList<RustAstNode>? Fields, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     impl 块
/// </summary>
public sealed record RustImplDef(
    RustAstNode? Trait,
    RustAstNode Type,
    IReadOnlyList<RustAstNode> Members,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     trait 定义
/// </summary>
public sealed record RustTraitDef(
    string Name,
    IReadOnlyList<RustAstNode> Members,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     类型别名 (type)
/// </summary>
public sealed record RustTypeAlias(string Name, RustAstNode Type, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     use 声明
/// </summary>
public sealed record RustUseDecl(string Path, string? Alias, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     mod 声明
/// </summary>
public sealed record RustModDecl(string Name, RustAstNode? Body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     类型节点
/// </summary>
public sealed record RustTypeNode(string Name, bool IsReference, bool IsMutable, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

/// <summary>
///     翻译单元（Crate 根节点）
/// </summary>
public sealed record RustCrate(IReadOnlyList<RustAstNode> Items, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

#endregion