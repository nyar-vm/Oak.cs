using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Julia.AST;

#region 模块与导入

public sealed record JlModule(
    string Name,
    IReadOnlyList<JlAstNode> Exports,
    IReadOnlyList<JlAstNode> Imports,
    IReadOnlyList<JlAstNode> Statements,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlImport(
    bool IsUsing,
    string Module,
    IReadOnlyList<string> Names,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlExport(IReadOnlyList<string> Names, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 函数与宏

public sealed record JlFunctionDef(
    string Name,
    IReadOnlyList<JlAstNode> Parameters,
    IReadOnlyList<string> WhereParams,
    JlAstNode? ReturnType,
    JlAstNode Body,
    bool IsShort,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlMacroDef(
    string Name,
    IReadOnlyList<JlAstNode> Parameters,
    JlAstNode Body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlParameter(
    string Name,
    JlAstNode? TypeAnnotation,
    JlAstNode? DefaultValue,
    bool IsVarargs,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlKeywordParameter(
    string Name,
    JlAstNode? TypeAnnotation,
    JlAstNode? DefaultValue,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 类型声明

public sealed record JlStructDef(
    string Name,
    IReadOnlyList<string> TypeParams,
    IReadOnlyList<JlAstNode> Fields,
    bool IsMutable,
    bool IsAbstract,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlField(
    string Name,
    JlAstNode? TypeAnnotation,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTypeDef(
    string Name,
    IReadOnlyList<string> TypeParams,
    JlAstNode? SuperType,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion
