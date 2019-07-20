using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Julia.AST;

#region 基础表达式

public sealed record JlIdentifier(string Name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlLiteral(string Kind, string Value, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTuple(IReadOnlyList<JlAstNode> Elements, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlArray(IReadOnlyList<JlAstNode> Elements, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlDict(IReadOnlyList<(JlAstNode Key, JlAstNode Value)> Pairs, TextSpan Span = default(TextSpan))
    : JlAstNode(Span);

public sealed record JlSet(IReadOnlyList<JlAstNode> Elements, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlUnit(TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlSymbol(string Name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlRange(
    JlAstNode Start,
    JlAstNode? Step,
    JlAstNode End,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlComprehension(
    JlAstNode Expression,
    IReadOnlyList<(JlAstNode Iterator, JlAstNode Iterable)> Iterators,
    bool IsGenerator,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlMacroCall(
    string Name,
    IReadOnlyList<JlAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlCommand(string Value, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 运算表达式

public sealed record JlBinaryOp(
    JlAstNode Left,
    string Operator,
    JlAstNode Right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlUnaryOp(
    string Operator,
    JlAstNode Operand,
    bool IsPrefix,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlBroadcastOp(
    JlAstNode Left,
    string Operator,
    JlAstNode Right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlPipe(
    JlAstNode Left,
    JlAstNode Right,
    bool IsReverse,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTernary(
    JlAstNode Condition,
    JlAstNode ThenBranch,
    JlAstNode ElseBranch,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 调用与索引

public sealed record JlCall(
    JlAstNode Function,
    IReadOnlyList<JlAstNode> Arguments,
    IReadOnlyList<JlAstNode> KeywordArguments,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlIndex(
    JlAstNode Object,
    IReadOnlyList<JlAstNode> Indices,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlFieldAccess(
    JlAstNode Object,
    string Field,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlPropertyAccess(
    JlAstNode Object,
    string Property,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlQualifiedAccess(
    string Module,
    string Name,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlLambda(
    IReadOnlyList<JlAstNode> Parameters,
    JlAstNode Body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 类型表达式

public sealed record JlTypeVar(string Name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTypeCon(string Name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTypeApp(
    JlAstNode Constructor,
    IReadOnlyList<JlAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlUnionType(
    IReadOnlyList<JlAstNode> Types,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTupleType(
    IReadOnlyList<JlAstNode> Elements,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlWhereType(
    JlAstNode Type,
    IReadOnlyList<JlAstNode> Constraints,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlFunctionType(
    IReadOnlyList<JlAstNode> Parameters,
    JlAstNode ReturnType,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlNamedTupleType(
    IReadOnlyList<(string Name, JlAstNode Type)> Fields,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion
