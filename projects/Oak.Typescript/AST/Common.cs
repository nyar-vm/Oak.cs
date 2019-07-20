using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Typescript.AST;

#region 参数与属性

/// <summary>
///     函数参数
/// </summary>
public sealed record TsParameter(
    string Name,
    TsAstNode? TypeAnnotation,
    TsAstNode? DefaultValue,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsParameter(string Name, TsAstNode? TypeAnnotation, TsAstNode? DefaultValue)
        : this(Name, TypeAnnotation, DefaultValue, default(TextSpan))
    {
    }
}

/// <summary>
///     对象属性
/// </summary>
public sealed record TsProperty(string Key, TsAstNode Value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsProperty(string Key, TsAstNode Value)
        : this(Key, Value, default(TextSpan))
    {
    }
}

#endregion