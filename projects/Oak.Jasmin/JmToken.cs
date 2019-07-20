using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Jasmin;

/// <summary>
///     Jasmin Token
/// </summary>
/// <param name="Type">Token 类型</param>
/// <param name="Value">Token 值</param>
/// <param name="Line">行号</param>
/// <param name="Column">列号</param>
public sealed record JmToken(JmTokenType Type, string Value, int Line, int Column)
{
    /// <summary>
    ///     转换为源码位置
    /// </summary>
    public TextSpan ToSourceSpan() => default;
}

/// <summary>
///     Jasmin Token 类型
/// </summary>
public enum JmTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    Eof,

    /// <summary>
    ///     指令关键字（.class, .super, .method 等）
    /// </summary>
    Directive,

    /// <summary>
    ///     JVM 操作码（aload, invokevirtual 等）
    /// </summary>
    Opcode,

    /// <summary>
    ///     标识符
    /// </summary>
    Identifier,

    /// <summary>
    ///     类型描述符（Ljava/lang/String;、I、[I 等）
    /// </summary>
    Descriptor,

    /// <summary>
    ///     数字字面量
    /// </summary>
    Number,

    /// <summary>
    ///     字符串字面量
    /// </summary>
    StringLiteral,

    /// <summary>
    ///     标签（如 Label:）
    /// </summary>
    Label,

    /// <summary>
    ///     注释（; 开头）
    /// </summary>
    Comment,

    /// <summary>
    ///     访问修饰符（public, private, static, final 等）
    /// </summary>
    AccessModifier,

    /// <summary>
    ///     冒号
    /// </summary>
    Colon,

    /// <summary>
    ///     等号
    /// </summary>
    Equals
}
