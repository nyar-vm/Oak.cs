namespace Oak.Syntax;

/// <summary>
///     语法高亮类别
/// </summary>
public enum HighlightKind
{
    /// <summary>关键字</summary>
    Keyword,
    /// <summary>数字字面量</summary>
    Number,
    /// <summary>字符串字面量</summary>
    String,
    /// <summary>注释</summary>
    Comment,
    /// <summary>类型名称</summary>
    TypeName,
    /// <summary>函数名称</summary>
    FunctionName,
    /// <summary>属性/注解</summary>
    Attribute,
    /// <summary>标识符</summary>
    Identifier,
    /// <summary>操作符</summary>
    Operator,
    /// <summary>分隔符</summary>
    Delimiter,
    /// <summary>其他</summary>
    Other,
}
