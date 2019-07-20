namespace Oak.Json;

/// <summary>
///     JSON 词法单元类型
/// </summary>
public enum JsonTokenType
{
    /// <summary>
    ///     左大括号 {
    /// </summary>
    LeftBrace,

    /// <summary>
    ///     右大括号 }
    /// </summary>
    RightBrace,

    /// <summary>
    ///     左方括号 [
    /// </summary>
    LeftBracket,

    /// <summary>
    ///     右方括号 ]
    /// </summary>
    RightBracket,

    /// <summary>
    ///     逗号
    /// </summary>
    Comma,

    /// <summary>
    ///     冒号
    /// </summary>
    Colon,

    /// <summary>
    ///     字符串值
    /// </summary>
    String,

    /// <summary>
    ///     数字值
    /// </summary>
    Number,

    /// <summary>
    ///     布尔值 true
    /// </summary>
    True,

    /// <summary>
    ///     布尔值 false
    /// </summary>
    False,

    /// <summary>
    ///     空值 null
    /// </summary>
    Null,

    /// <summary>
    ///     文件结束
    /// </summary>
    EndOfFile,

    /// <summary>
    ///     无效词法单元
    /// </summary>
    Invalid
}