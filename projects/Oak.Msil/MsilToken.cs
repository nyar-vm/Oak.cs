using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Msil;

/// <summary>
///     ILASM Token
/// </summary>
/// <param name="Type">Token 类型</param>
/// <param name="Value">Token 值</param>
/// <param name="Line">行号</param>
/// <param name="Column">列号</param>
public sealed record MsilToken(MsilTokenType Type, string Value, int Line, int Column)
{
    /// <summary>
    ///     转换为源码位置
    /// </summary>
    public TextSpan ToSourceSpan() => default;
}

/// <summary>
///     ILASM Token 类型
/// </summary>
public enum MsilTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    Eof,

    /// <summary>
    ///     指令关键字（.assembly, .class, .method 等）
    /// </summary>
    Directive,

    /// <summary>
    ///     MSIL 操作码（ldarg.0, add, call 等，点分格式）
    /// </summary>
    Opcode,

    /// <summary>
    ///     标识符
    /// </summary>
    Identifier,

    /// <summary>
    ///     类型引用（int32, string, [mscorlib]System.Console 等）
    /// </summary>
    TypeReference,

    /// <summary>
    ///     数字
    /// </summary>
    Number,

    /// <summary>
    ///     字符串字面量
    /// </summary>
    StringLiteral,

    /// <summary>
    ///     IL 偏移标签（IL_0001:）
    /// </summary>
    IlLabel,

    /// <summary>
    ///     注释（// 开头）
    /// </summary>
    Comment,

    /// <summary>
    ///     访问修饰符（public, private, static 等）
    /// </summary>
    AccessModifier,

    /// <summary>
    ///     标点符号
    /// </summary>
    Punctuation
}
