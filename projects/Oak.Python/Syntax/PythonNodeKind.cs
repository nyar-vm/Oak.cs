using Oak.Syntax;

namespace Oak.Python.Syntax;

/// <summary>
///     Python 节点类型
/// </summary>
public static class PythonNodeKind
{
    /// <summary>
    ///     未知
    /// </summary>
    public static readonly NodeKind Unknown = 0;

    /// <summary>
    ///     标识符
    /// </summary>
    public static readonly NodeKind Identifier = 1;

    /// <summary>
    ///     关键字
    /// </summary>
    public static readonly NodeKind Keyword = 2;

    /// <summary>
    ///     数字字面量
    /// </summary>
    public static readonly NodeKind Number = 3;

    /// <summary>
    ///     字符串字面量
    /// </summary>
    public static readonly NodeKind String = 4;

    /// <summary>
    ///     运算符
    /// </summary>
    public static readonly NodeKind Operator = 5;

    /// <summary>
    ///     分隔符（括号、逗号、冒号等）
    /// </summary>
    public static readonly NodeKind Delimiter = 6;

    /// <summary>
    ///     换行
    /// </summary>
    public static readonly NodeKind NewLine = 7;

    /// <summary>
    ///     缩进
    /// </summary>
    public static readonly NodeKind Indent = 8;

    /// <summary>
    ///     反缩进
    /// </summary>
    public static readonly NodeKind Dedent = 9;

    /// <summary>
    ///     文件结束
    /// </summary>
    public static readonly NodeKind Eof = 10;
}
