using Oak.Syntax;

namespace Oak.Widget.Lexer;

/// <summary>
///     AWSL 词法节点类型定义
/// </summary>
public static class AwslNodeKind
{
    public static readonly NodeKind Unknown = 0;

    /// <summary>
    ///     关键字（let, const, micro, if, else, for 等）
    /// </summary>
    public static readonly NodeKind Keyword = 1;

    /// <summary>
    ///     标识符（变量名、标签名、函数名等）
    /// </summary>
    public static readonly NodeKind Identifier = 2;

    /// <summary>
    ///     数字字面量
    /// </summary>
    public static readonly NodeKind Number = 3;

    /// <summary>
    ///     字符串字面量
    /// </summary>
    public static readonly NodeKind String = 4;

    /// <summary>
    ///     布尔/null 字面量（true, false, null）
    /// </summary>
    public static readonly NodeKind Literal = 5;

    /// <summary>
    ///     运算符（+, -, *, /, ==, !=, =>, -> 等）
    /// </summary>
    public static readonly NodeKind Operator = 6;

    /// <summary>
    ///     标点符号（ :, ::, ;, ., , 等）
    /// </summary>
    public static readonly NodeKind Punctuation = 7;

    /// <summary>
    ///     分隔符（(, ), [, ], {, } 等）
    /// </summary>
    public static readonly NodeKind Delimiter = 8;

    /// <summary>
    ///     注释
    /// </summary>
    public static readonly NodeKind Comment = 9;

    /// <summary>
    ///     事件/绑定前缀（@click, @bind 等）
    /// </summary>
    public static readonly NodeKind AtPrefix = 10;

    /// <summary>
    ///     文件结束
    /// </summary>
    public static readonly NodeKind Eof = 11;
}
