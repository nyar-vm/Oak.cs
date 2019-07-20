namespace Oak.Yaml;

/// <summary>
///     YAML 词法单元类型
/// </summary>
public enum YamlTokenType
{
    /// <summary>
    ///     文档开始 ---
    /// </summary>
    DocumentStart,

    /// <summary>
    ///     文档结束 ...
    /// </summary>
    DocumentEnd,

    /// <summary>
    ///     缩进
    /// </summary>
    Indent,

    /// <summary>
    ///     键
    /// </summary>
    Key,

    /// <summary>
    ///     值
    /// </summary>
    Value,

    /// <summary>
    ///     序列项标记 -
    /// </summary>
    Dash,

    /// <summary>
    ///     字符串值
    /// </summary>
    String,

    /// <summary>
    ///     数字值
    /// </summary>
    Number,

    /// <summary>
    ///     布尔值
    /// </summary>
    Boolean,

    /// <summary>
    ///     null 值
    /// </summary>
    Null,

    /// <summary>
    ///     流式映射开始 {
    /// </summary>
    FlowMapStart,

    /// <summary>
    ///     流式映射结束 }
    /// </summary>
    FlowMapEnd,

    /// <summary>
    ///     流式序列开始 [
    /// </summary>
    FlowSeqStart,

    /// <summary>
    ///     流式序列结束 ]
    /// </summary>
    FlowSeqEnd,

    /// <summary>
    ///     流式逗号
    /// </summary>
    FlowComma,

    /// <summary>
    ///     冒号
    /// </summary>
    Colon,

    /// <summary>
    ///     注释
    /// </summary>
    Comment,

    /// <summary>
    ///     换行
    /// </summary>
    Newline,

    /// <summary>
    ///     多行字符串 | 或 >
    /// </summary>
    MultilineIndicator,

    /// <summary>
    ///     标签 !xxx
    /// </summary>
    Tag,

    /// <summary>
    ///     锚点 &xxx
    /// </summary>
    Anchor,

    /// <summary>
    ///     别名 *xxx
    /// </summary>
    Alias,

    /// <summary>
    ///     文件结束
    /// </summary>
    EndOfFile,

    /// <summary>
    ///     无效词法单元
    /// </summary>
    Invalid
}