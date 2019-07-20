using Oak.Syntax;
namespace Oak.Syntax;

/// <summary>
///     增量重新解析的委托类型
/// </summary>
public delegate GreenNode? IncrementalParser(ISource source, TextSpan span, ISyntaxContext? context, out bool changed);