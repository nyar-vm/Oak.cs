using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.AST;

/// <summary>
/// TypeScript AST 节点的抽象基类
/// </summary>
public abstract record TsAstNode(TextSpan Span = default(TextSpan));
