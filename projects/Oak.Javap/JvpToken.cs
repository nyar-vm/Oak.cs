using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Javap;

/// <summary>
///     Javap Token
/// </summary>
/// <param name="Type">Token 类型</param>
/// <param name="Value">Token 值</param>
/// <param name="Line">行号</param>
/// <param name="Column">列号</param>
public sealed record JvpToken(JvpTokenType Type, string Value, int Line, int Column)
{
    /// <summary>
    ///     转换为源码位置
    /// </summary>
    public TextSpan ToSourceSpan() => default;
}

/// <summary>
///     Javap Token 类型
/// </summary>
public enum JvpTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    Eof,

    /// <summary>
    ///     访问修饰符（public, private, static 等）
    /// </summary>
    AccessModifier,

    /// <summary>
    ///     类型关键字（class, interface, enum 等）
    /// </summary>
    TypeKeyword,

    /// <summary>
    ///     JVM 操作码（小写下划线格式：aload_0, invokevirtual 等）
    /// </summary>
    Opcode,

    /// <summary>
    ///     标识符（类名、方法名、字段名）
    /// </summary>
    Identifier,

    /// <summary>
    ///     类型名（int, void, java.lang.String 等）
    /// </summary>
    TypeName,

    /// <summary>
    ///     数字（偏移量、操作数）
    /// </summary>
    Number,

    /// <summary>
    ///     常量池引用（#1, #42 等）
    /// </summary>
    ConstantPoolRef,

    /// <summary>
    ///     注释（// 开头）
    /// </summary>
    Comment,

    /// <summary>
    ///     标点符号（{, }, ( ), ;, :, ., ,）
    /// </summary>
    Punctuation,

    /// <summary>
    ///     "Compiled" 头部关键字
    /// </summary>
    HeaderKeyword,

    /// <summary>
    ///     "Code:" 段标记
    /// </summary>
    SectionMarker
}
