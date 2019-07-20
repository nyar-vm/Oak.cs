namespace Oak.Valkyrie.AST.Template;

/// <summary>
///     元模板纯文本节点，表示 meta 代码中不被插值或指令解释的静态文本
/// </summary>
/// <para>示例：</para>
/// <code>
/// fn hello() { return text; }
/// </code>
public sealed record MetaTemplateText : ValkyrieNode
{
    /// <summary>
    ///     带文本的构造函数
    /// </summary>
    /// <param name="text">静态文本内容</param>
    public MetaTemplateText(string text)
    {
        Text = text;
    }

    /// <summary>
    ///     静态文本内容
    /// </summary>
    public string Text { get; }
}
