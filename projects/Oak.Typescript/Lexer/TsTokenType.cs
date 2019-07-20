namespace Oak.Typescript.Lexer;

/// <summary>
///     TypeScript 词法单元类型
/// </summary>
public enum TsTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    Eof,

    /// <summary>
    ///     标识符
    /// </summary>
    Identifier,

    /// <summary>
    ///     关键字
    /// </summary>
    Keyword,

    /// <summary>
    ///     数字字面量
    /// </summary>
    Number,

    /// <summary>
    ///     BigInt 字面量
    /// </summary>
    BigInt,

    /// <summary>
    ///     字符串字面量
    /// </summary>
    String,

    /// <summary>
    ///     模板字符串字面量
    /// </summary>
    TemplateString,

    /// <summary>
    ///     运算符
    /// </summary>
    Operator,

    /// <summary>
    ///     分隔符
    /// </summary>
    Delimiter,

    /// <summary>
    ///     标点符号
    /// </summary>
    Punctuation,

    /// <summary>
    ///     字面量（true/false/null）
    /// </summary>
    Literal,

    /// <summary>
    ///     属性装饰器
    /// </summary>
    Attribute,

    /// <summary>
    ///     注释
    /// </summary>
    Comment,

    /// <summary>
    ///     JSX 文本
    /// </summary>
    JsxText
}
