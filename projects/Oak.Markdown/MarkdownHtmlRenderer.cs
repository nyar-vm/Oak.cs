using System.Text;
using Oak.Markdown.Syntax;

namespace Oak.Markdown;

/// <summary>
///     Markdown HTML 渲染选项
/// </summary>
public sealed class MarkdownHtmlOptions
{
    /// <summary>
    ///     是否为标题生成 ID 锚点
    /// </summary>
    public bool GenerateHeadingIds { get; init; } = true;

    /// <summary>
    ///     是否为代码块生成语法高亮 class
    /// </summary>
    public bool HighlightCode { get; init; } = true;

    /// <summary>
    ///     代码块 class 前缀
    /// </summary>
    public string CodeBlockClassPrefix { get; init; } = "language-";

    /// <summary>
    ///     是否在新标签页打开外部链接
    /// </summary>
    public bool ExternalLinkNewTab { get; init; } = true;

    /// <summary>
    ///     外部链接判断前缀列表
    /// </summary>
    public IReadOnlyList<string> ExternalLinkPrefixes { get; init; } = new List<string> { "http://", "https://" };

    /// <summary>
    ///     是否生成 TOC 数据
    /// </summary>
    public bool GenerateToc { get; init; } = true;

    /// <summary>
    ///     数学公式渲染方式（默认 KaTeX）
    /// </summary>
    public MathRenderMode MathMode { get; init; } = MathRenderMode.KaTeX;

    /// <summary>
    ///     是否将软换行渲染为 &lt;br&gt;
    /// </summary>
    public bool SoftBreakAsLineBreak { get; init; } = false;
}

/// <summary>
///     数学公式渲染模式
/// </summary>
public enum MathRenderMode
{
    /// <summary>
    ///     KaTeX 渲染
    /// </summary>
    KaTeX,

    /// <summary>
    ///     MathJax 渲染
    /// </summary>
    MathJax,

    /// <summary>
    ///     原始 LaTeX 输出
    /// </summary>
    Raw
}

/// <summary>
///     Markdown HTML 渲染结果
/// </summary>
public sealed class MarkdownHtmlResult
{
    /// <summary>
    ///     渲染后的 HTML
    /// </summary>
    public string Html { get; init; } = string.Empty;

    /// <summary>
    ///     目录项列表
    /// </summary>
    public IReadOnlyList<TocItem> TocItems { get; init; } = [];

    /// <summary>
    ///     脚注定义列表
    /// </summary>
    public IReadOnlyList<MarkdownFootnoteDefinition> Footnotes { get; init; } =
        [];

    /// <summary>
    ///     是否包含 KaTeX 渲染的数学公式（需要引入 KaTeX CSS）
    /// </summary>
    public bool HasKaTeXMath { get; init; }
}

/// <summary>
///     目录项
/// </summary>
public sealed record TocItem
{
    /// <summary>
    ///     标题级别
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    ///     标题文本
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    ///     锚点 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;
}

/// <summary>
///     Markdown 转 HTML 渲染器
/// </summary>
public sealed class MarkdownHtmlRenderer
{
    private readonly List<MarkdownFootnoteDefinition> _footnotes;
    private readonly MarkdownHtmlOptions _options;
    private readonly List<TocItem> _tocItems;
    private KaTeXService? _katexService;
    private bool _hasKaTeXMath;
    private int _footnoteCounter;
    private int _headingCounter;

    public MarkdownHtmlRenderer(MarkdownHtmlOptions? options = null)
    {
        _options = options ?? new MarkdownHtmlOptions();
        _tocItems = [];
        _footnotes = [];
        _headingCounter = 0;
        _footnoteCounter = 0;
    }

    /// <summary>
    ///     获取或设置 KaTeX 服务端渲染服务实例
    ///     当 MathMode 为 KaTeX 时，若此属性为 null 则自动创建默认实例
    /// </summary>
    public KaTeXService? KaTeXService
    {
        get => _katexService;
        set => _katexService = value;
    }

    /// <summary>
    ///     渲染 Markdown 文档为 HTML
    /// </summary>
    public MarkdownHtmlResult Render(MarkdownDocument document)
    {
        _tocItems.Clear();
        _footnotes.Clear();
        _headingCounter = 0;
        _footnoteCounter = 0;
        _hasKaTeXMath = false;

        var html = RenderNodes(document.Children);
        var footnotesHtml = RenderFootnotes();

        var finalHtml = html + footnotesHtml;

        return new MarkdownHtmlResult
        {
            Html = finalHtml,
            TocItems = _tocItems.ToList(),
            Footnotes = _footnotes.ToList(),
            HasKaTeXMath = _hasKaTeXMath
        };
    }

    /// <summary>
    ///     渲染 Markdown 文本为 HTML
    /// </summary>
    public MarkdownHtmlResult Render(string markdown)
    {
        var lexer = new MarkdownLexer();
        var parser = new MarkdownParser();
        var tokens = lexer.Tokenize(markdown);
        var document = parser.Parse(tokens);
        return Render(document);
    }

    private string RenderNodes(IReadOnlyList<MarkdownNode> nodes)
    {
        var sb = new StringBuilder();

        foreach (var node in nodes) sb.Append(RenderNode(node));

        return sb.ToString();
    }

    private string RenderNode(MarkdownNode node)
    {
        return node switch
        {
            MarkdownHeading heading => RenderHeading(heading),
            MarkdownParagraph paragraph => RenderParagraph(paragraph),
            MarkdownCodeBlock codeBlock => RenderCodeBlock(codeBlock),
            MarkdownIndentedCodeBlock indentedCode => RenderIndentedCodeBlock(indentedCode),
            MarkdownInlineCode inlineCode => RenderInlineCode(inlineCode),
            MarkdownBlockquote blockquote => RenderBlockquote(blockquote),
            MarkdownList list => RenderList(list),
            MarkdownHorizontalRule => "<hr />\n",
            MarkdownLink link => RenderLink(link),
            MarkdownImage image => RenderImage(image),
            MarkdownStrong strong => $"<strong>{RenderNodes(strong.Children)}</strong>",
            MarkdownEmphasis emphasis => $"<em>{RenderNodes(emphasis.Children)}</em>",
            MarkdownStrikethrough strikethrough => $"<del>{RenderNodes(strikethrough.Children)}</del>",
            MarkdownHighlight highlight => $"<mark>{RenderNodes(highlight.Children)}</mark>",
            MarkdownText text => EscapeHtml(text.Content),
            MarkdownLineBreak => "<br />\n",
            MarkdownSoftBreak softBreak => RenderSoftBreak(softBreak),
            MarkdownTable table => RenderTable(table),
            MarkdownTaskListItem taskItem => RenderTaskListItem(taskItem),
            MarkdownFootnote footnote => RenderFootnote(footnote),
            MarkdownFootnoteDefinition footnoteDef => RenderFootnoteDefinition(footnoteDef),
            MarkdownMathInline mathInline => RenderMathInline(mathInline),
            MarkdownMathBlock mathBlock => RenderMathBlock(mathBlock),
            MarkdownHtmlBlock htmlBlock => htmlBlock.Content + "\n",
            MarkdownHtmlInline htmlInline => htmlInline.Content,
            MarkdownReferenceLinkDefinition => string.Empty,
            _ => string.Empty
        };
    }

    private string RenderHeading(MarkdownHeading heading)
    {
        var content = RenderNodes(heading.Children);
        var id = "";

        if (_options.GenerateHeadingIds)
        {
            _headingCounter++;
            id = GenerateHeadingId(content, heading.Level);
            _tocItems.Add(new TocItem { Level = heading.Level, Text = content, Id = id });
        }

        var idAttr = string.IsNullOrEmpty(id) ? "" : $" id=\"{id}\"";
        return $"<h{heading.Level}{idAttr}>{content}</h{heading.Level}>\n";
    }

    private string GenerateHeadingId(string text, int level)
    {
        var id = new StringBuilder();
        var lastWasDash = false;

        foreach (var c in text)
            if (char.IsLetterOrDigit(c))
            {
                id.Append(char.ToLowerInvariant(c));
                lastWasDash = false;
            }
            else if (c is ' ' or '-' or '_')
            {
                if (!lastWasDash && id.Length > 0)
                {
                    id.Append('-');
                    lastWasDash = true;
                }
            }

        if (id.Length == 0) id.Append($"heading-{_headingCounter}");

        return id.ToString();
    }

    private string RenderParagraph(MarkdownParagraph paragraph)
    {
        return $"<p>{RenderNodes(paragraph.Children)}</p>\n";
    }

    private string RenderCodeBlock(MarkdownCodeBlock codeBlock)
    {
        var escapedContent = EscapeHtml(codeBlock.Content);
        var languageClass = "";

        if (_options.HighlightCode && !string.IsNullOrEmpty(codeBlock.Language))
            languageClass = $" class=\"{_options.CodeBlockClassPrefix}{EscapeHtml(codeBlock.Language)}\"";

        return $"<pre><code{languageClass}>{escapedContent}</code></pre>\n";
    }

    private string RenderIndentedCodeBlock(MarkdownIndentedCodeBlock codeBlock)
    {
        var escapedContent = EscapeHtml(codeBlock.Content);
        return $"<pre><code>{escapedContent}</code></pre>\n";
    }

    private string RenderInlineCode(MarkdownInlineCode inlineCode)
    {
        return $"<code>{EscapeHtml(inlineCode.Content)}</code>";
    }

    private string RenderBlockquote(MarkdownBlockquote blockquote)
    {
        return $"<blockquote>\n{RenderNodes(blockquote.Children)}</blockquote>\n";
    }

    private string RenderList(MarkdownList list)
    {
        var tag = list.IsOrdered ? "ol" : "ul";
        var sb = new StringBuilder();
        sb.Append($"<{tag}>\n");

        foreach (var item in list.Items) sb.Append(RenderListItem(item));

        sb.Append($"</{tag}>\n");
        return sb.ToString();
    }

    private string RenderListItem(MarkdownNode item)
    {
        if (item is MarkdownTaskListItem taskItem) return RenderTaskListItem(taskItem);

        if (item is MarkdownListItem listItem) return $"<li>{RenderNodes(listItem.Children)}</li>\n";

        return $"<li>{RenderNode(item)}</li>\n";
    }

    private string RenderTaskListItem(MarkdownTaskListItem taskItem)
    {
        var checkedAttr = taskItem.IsChecked ? " checked" : "";
        var disabledAttr = " disabled";
        var checkbox = $"<input type=\"checkbox\"{checkedAttr}{disabledAttr} />";
        return $"<li>{checkbox}{RenderNodes(taskItem.Children)}</li>\n";
    }

    private string RenderLink(MarkdownLink link)
    {
        var content = RenderNodes(link.Children);
        var titleAttr = string.IsNullOrEmpty(link.Title) ? "" : $" title=\"{EscapeHtml(link.Title)}\"";
        var externalAttr = "";

        if (_options.ExternalLinkNewTab && IsExternalLink(link.Url))
            externalAttr = " target=\"_blank\" rel=\"noopener noreferrer\"";

        return $"<a href=\"{EscapeHtml(link.Url)}\"{titleAttr}{externalAttr}>{content}</a>";
    }

    private string RenderImage(MarkdownImage image)
    {
        var titleAttr = string.IsNullOrEmpty(image.Title) ? "" : $" title=\"{EscapeHtml(image.Title)}\"";
        return $"<img src=\"{EscapeHtml(image.Url)}\" alt=\"{EscapeHtml(image.Alt)}\"{titleAttr} />";
    }

    private string RenderFootnote(MarkdownFootnote footnote)
    {
        _footnoteCounter++;
        var id = _footnoteCounter;
        return
            $"<sup class=\"footnote-ref\" id=\"fnref-{EscapeHtml(footnote.Label)}\"><a href=\"#fn-{EscapeHtml(footnote.Label)}\">{id}</a></sup>";
    }

    private string RenderFootnoteDefinition(MarkdownFootnoteDefinition footnoteDef)
    {
        _footnotes.Add(footnoteDef);
        return string.Empty;
    }

    private string RenderFootnotes()
    {
        if (_footnotes.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("<section class=\"footnotes\">\n<ol>\n");

        foreach (var footnote in _footnotes)
        {
            var content = RenderNodes(footnote.Children);
            sb.Append($"<li id=\"fn-{EscapeHtml(footnote.Label)}\">{content}</li>\n");
        }

        sb.Append("</ol>\n</section>\n");
        return sb.ToString();
    }

    private string RenderMathInline(MarkdownMathInline math)
    {
        return _options.MathMode switch
        {
            MathRenderMode.KaTeX => RenderKaTeXInline(math.Content),
            MathRenderMode.MathJax => $"\\({EscapeHtml(math.Content)}\\)",
            MathRenderMode.Raw => $"${EscapeHtml(math.Content)}$",
            _ => $"${EscapeHtml(math.Content)}$"
        };
    }

    private string RenderMathBlock(MarkdownMathBlock math)
    {
        return _options.MathMode switch
        {
            MathRenderMode.KaTeX => RenderKaTeXBlock(math.Content),
            MathRenderMode.MathJax => $"$${EscapeHtml(math.Content)}$$\n",
            MathRenderMode.Raw => $"$${EscapeHtml(math.Content)}$$\n",
            _ => $"$${EscapeHtml(math.Content)}$$\n"
        };
    }

    private string RenderKaTeXInline(string latex)
    {
        _katexService ??= new KaTeXService();
        _hasKaTeXMath = true;
        return _katexService.RenderToString(latex, false);
    }

    private string RenderKaTeXBlock(string latex)
    {
        _katexService ??= new KaTeXService();
        _hasKaTeXMath = true;
        return _katexService.RenderToString(latex, true) + "\n";
    }

    private string RenderSoftBreak(MarkdownSoftBreak _)
    {
        return _options.SoftBreakAsLineBreak ? "<br />\n" : "\n";
    }

    private string RenderTable(MarkdownTable table)
    {
        var sb = new StringBuilder();
        sb.Append("<table>\n<thead>\n<tr>\n");

        foreach (var cell in table.Header.Cells) sb.Append($"<th>{RenderNodes(cell.Children)}</th>\n");

        sb.Append("</tr>\n</thead>\n<tbody>\n");

        foreach (var row in table.Rows)
        {
            sb.Append("<tr>\n");

            foreach (var cell in row.Cells) sb.Append($"<td>{RenderNodes(cell.Children)}</td>\n");

            sb.Append("</tr>\n");
        }

        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    private bool IsExternalLink(string url)
    {
        foreach (var prefix in _options.ExternalLinkPrefixes)
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}