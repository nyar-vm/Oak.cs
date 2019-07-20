using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.C;

#region 语句

/// <summary>
///     表达式语句
/// </summary>
public sealed record CExprStmt(CAstNode Expression, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     变量声明
/// </summary>
public sealed record CVarDecl(CAstNode Type, string Name, CAstNode? Initializer, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     If 语句
/// </summary>
public sealed record CIf(
    CAstNode Condition,
    CAstNode ThenBody,
    CAstNode? ElseBody,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     While 循环语句
/// </summary>
public sealed record CWhile(
    CAstNode Condition,
    CAstNode Body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Do-While 循环语句
/// </summary>
public sealed record CDoWhile(
    CAstNode Body,
    CAstNode Condition,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     For 循环语句
/// </summary>
public sealed record CFor(
    CAstNode? Init,
    CAstNode? Condition,
    CAstNode? Increment,
    CAstNode Body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Return 语句
/// </summary>
public sealed record CReturn(CAstNode? Value, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Break 语句
/// </summary>
public sealed record CBreak(TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Continue 语句
/// </summary>
public sealed record CContinue(TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Goto 语句
/// </summary>
public sealed record CGoto(string Label, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     标签语句
/// </summary>
public sealed record CLabel(string Name, CAstNode Statement, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Switch 语句
/// </summary>
public sealed record CSwitch(
    CAstNode Expression,
    IReadOnlyList<CAstNode> Cases,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     Case 分支
/// </summary>
public sealed record CCase(CAstNode? Value, IReadOnlyList<CAstNode> Body, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     复合语句（代码块）
/// </summary>
public sealed record CCompound(IReadOnlyList<CAstNode> Statements, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     函数定义
/// </summary>
public sealed record CFunctionDef(
    CAstNode ReturnType,
    string Name,
    IReadOnlyList<CParamDecl> Parameters,
    CAstNode Body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     参数声明
/// </summary>
public sealed record CParamDecl(CAstNode Type, string Name, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     结构体定义
/// </summary>
public sealed record CStructDef(
    string? Name,
    IReadOnlyList<CVarDecl> Fields,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     类型定义（typedef）
/// </summary>
public sealed record CTypedef(CAstNode Type, string Name, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     类型节点
/// </summary>
public sealed record CTypeNode(string Name, bool IsPointer, bool IsConst, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

/// <summary>
///     翻译单元（根节点）
/// </summary>
public sealed record CTranslationUnit(IReadOnlyList<CAstNode> Declarations, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

#endregion