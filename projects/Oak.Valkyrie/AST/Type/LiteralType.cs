using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Type;

/// <summary>
///     字面量类型枚举，用于标识 <see cref="TermAtomicLiteral"/> 中值的具体类型
/// </summary>
public enum LiteralType
{
    /// <summary>
    ///     数值字面量（整数或浮点）
    /// </summary>
    Number,

    /// <summary>
    ///     字符串字面量（<c>"content"</c>）
    /// </summary>
    String,

    /// <summary>
    ///     布尔字面量（<c>true</c> / <c>false</c>）
    /// </summary>
    Boolean,

    /// <summary>
    ///     空值字面量（<c>null</c>）
    /// </summary>
    Null
}
