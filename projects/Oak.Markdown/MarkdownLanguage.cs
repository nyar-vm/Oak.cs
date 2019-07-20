using Oak.Markdown.Syntax;
using Oak.Syntax;

namespace Oak.Markdown;

/// <summary>
///     Markdown 语言配置
/// </summary>
public sealed class MarkdownLanguageConfig
{
    /// <summary>
    ///     是否启用表格扩展
    /// </summary>
    public bool EnableTables { get; init; } = true;

    /// <summary>
    ///     是否启用任务列表扩展
    /// </summary>
    public bool EnableTaskLists { get; init; } = true;

    /// <summary>
    ///     是否启用删除线扩展
    /// </summary>
    public bool EnableStrikethrough { get; init; } = true;

    /// <summary>
    ///     是否启用自动链接扩展
    /// </summary>
    public bool EnableAutoLinks { get; init; } = true;

    /// <summary>
    ///     是否启用高亮/标记扩展 ==text==
    /// </summary>
    public bool EnableHighlight { get; init; } = true;

    /// <summary>
    ///     是否启用脚注扩展 [^1] / [^1]: text
    /// </summary>
    public bool EnableFootnotes { get; init; } = true;

    /// <summary>
    ///     是否启用数学公式扩展 $...$ / $$...$$
    /// </summary>
    public bool EnableMath { get; init; } = true;

    /// <summary>
    ///     是否启用 HTML 内联标签
    /// </summary>
    public bool EnableHtmlInline { get; init; } = true;

    /// <summary>
    ///     是否启用 HTML 块标签
    /// </summary>
    public bool EnableHtmlBlocks { get; init; } = true;

    /// <summary>
    ///     是否启用 Setext 风格标题（下划线 === / ---）
    /// </summary>
    public bool EnableSetextHeadings { get; init; } = true;

    /// <summary>
    ///     是否启用缩进代码块（4 空格缩进）
    /// </summary>
    public bool EnableIndentedCodeBlocks { get; init; } = true;

    /// <summary>
    ///     是否启用引用式链接 [text][id] / [id]: url
    /// </summary>
    public bool EnableReferenceLinks { get; init; } = true;

    /// <summary>
    ///     是否将段落内单个换行符渲染为软换行（&lt;br&gt;）
    /// </summary>
    public bool SoftBreakAsLineBreak { get; init; }

    /// <summary>
    ///     默认配置实例
    /// </summary>
    public static MarkdownLanguageConfig Default { get; } = new();

    /// <summary>
    ///     严格 CommonMark 配置（不启用任何扩展）
    /// </summary>
    public static MarkdownLanguageConfig CommonMark { get; } = new()
    {
        EnableTables = false,
        EnableTaskLists = false,
        EnableStrikethrough = false,
        EnableAutoLinks = false,
        EnableHighlight = false,
        EnableFootnotes = false,
        EnableMath = false,
        EnableHtmlInline = false,
        EnableHtmlBlocks = false,
        EnableSetextHeadings = true,
        EnableIndentedCodeBlocks = true,
        EnableReferenceLinks = true,
        SoftBreakAsLineBreak = false
    };

    /// <summary>
    ///     GFM（GitHub Flavored Markdown）配置
    /// </summary>
    public static MarkdownLanguageConfig Gfm { get; } = new()
    {
        EnableTables = true,
        EnableTaskLists = true,
        EnableStrikethrough = true,
        EnableAutoLinks = true,
        EnableHighlight = false,
        EnableFootnotes = false,
        EnableMath = false,
        EnableHtmlInline = true,
        EnableHtmlBlocks = true,
        EnableSetextHeadings = true,
        EnableIndentedCodeBlocks = true,
        EnableReferenceLinks = true,
        SoftBreakAsLineBreak = false
    };
}

/// <summary>
///     Markdown 语言前端，封装词法分析、语法分析管线和配置
/// </summary>
public sealed class MarkdownLanguage : Language
{
    private readonly MarkdownLexer _lexer;
    private readonly MarkdownParser _parser;

    /// <summary>
    ///     创建 Markdown 语言实例（使用默认配置）
    /// </summary>
    public MarkdownLanguage()
        : this(MarkdownLanguageConfig.Default)
    {
    }

    /// <summary>
    ///     创建 Markdown 语言实例
    /// </summary>
    public MarkdownLanguage(MarkdownLanguageConfig config)
    {
        Config = config;
        _lexer = new MarkdownLexer(config);
        _parser = new MarkdownParser(config);
    }

    public override string Name => "Markdown";

    /// <summary>
    ///     语言配置
    /// </summary>
    public MarkdownLanguageConfig Config { get; }

    /// <summary>
    ///     将 Markdown 源码解析为 AST
    /// </summary>
    public MarkdownDocument Parse(string source)
    {
        var tokens = _lexer.Tokenize(source);
        return _parser.Parse(tokens);
    }

    /// <summary>
    ///     将 Markdown 源码解析并渲染为 HTML
    /// </summary>
    public string RenderToHtml(string source, MarkdownHtmlOptions? options = null)
    {
        var document = Parse(source);
        var renderer = new MarkdownHtmlRenderer(options);
        var result = renderer.Render(document);
        return result.Html;
    }
}