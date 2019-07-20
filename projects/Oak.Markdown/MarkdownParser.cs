using System.Text;
using Oak.Markdown.Syntax;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.Markdown;

/// <summary>
///     Markdown 语法分析器，将词法单元序列解析为 AST
/// </summary>
public sealed class MarkdownParser : IParser<IReadOnlyList<GreenLeafNode>, MarkdownDocument>
{
    private readonly MarkdownLanguageConfig _config;

    private readonly Dictionary<string, MarkdownReferenceLinkDefinition> _referenceLinks =
        new(StringComparer.OrdinalIgnoreCase);

    private int _position;
    private IReadOnlyList<GreenLeafNode> _tokens = [];

    /// <summary>
    ///     创建 Markdown 语法分析器
    /// </summary>
    public MarkdownParser(MarkdownLanguageConfig? config = null)
    {
        _config = config ?? MarkdownLanguageConfig.Default;
    }

    /// <summary>
    ///     解析词法单元序列为 AST
    /// </summary>
    public MarkdownDocument Parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        _tokens = tokens;
        _position = 0;
        _referenceLinks.Clear();

        var blocks = new List<MarkdownNode>();

        while (!IsAtEnd())
        {
            SkipEmptyLines();

            if (IsAtEnd()) break;

            var block = ParseBlock();
            if (block is not null) blocks.Add(block);
        }

        return new MarkdownDocument(blocks);
    }

    #region 行内解析

    private IReadOnlyList<MarkdownNode> ParseInline(string text)
    {
        var reader = new InlineParser(text, _config, _referenceLinks);
        return reader.Parse();
    }

    #endregion

    #region 块级解析

    private MarkdownNode? ParseBlock()
    {
        if (IsAtEnd()) return null;

        if (TryParseHorizontalRule(out var hr)) return hr;

        if (TryParseHeading(out var heading)) return heading;

        if (TryParseCodeBlock(out var codeBlock)) return codeBlock;

        if (_config.EnableIndentedCodeBlocks && TryParseIndentedCodeBlock(out var indentedCode)) return indentedCode;

        if (TryParseBlockquote(out var blockquote)) return blockquote;

        if (TryParseList(out var list)) return list;

        if (_config.EnableTables && TryParseTable(out var table)) return table;

        if (_config.EnableFootnotes && TryParseFootnoteDefinition(out var footnoteDef)) return footnoteDef;

        if (_config.EnableReferenceLinks && TryParseReferenceLinkDefinition(out var refLink)) return refLink;

        if (_config.EnableHtmlBlocks && TryParseHtmlBlock(out var htmlBlock)) return htmlBlock;

        if (_config.EnableMath && TryParseMathBlock(out var mathBlock)) return mathBlock;

        if (_config.EnableSetextHeadings && TryParseSetextHeading(out var setextHeading)) return setextHeading;

        return ParseParagraph();
    }

    private bool TryParseHorizontalRule(out MarkdownHorizontalRule? hr)
    {
        hr = null;
        var start = _position;

        if (Match(MarkdownNodeKind.HorizontalRule))
        {
            SkipToEndOfLine();
            hr = new MarkdownHorizontalRule();
            return true;
        }

        if (Match(MarkdownNodeKind.Text))
        {
            var text = Previous().Text.Trim();
            if (IsHorizontalRuleText(text))
            {
                hr = new MarkdownHorizontalRule();
                return true;
            }

            _position = start;
        }

        return false;
    }

    private static bool IsHorizontalRuleText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < 3) return false;

        var ch = trimmed[0];
        if (ch != '-' && ch != '*' && ch != '_') return false;

        foreach (var c in trimmed)
            if (c != ch && !char.IsWhiteSpace(c))
                return false;

        return true;
    }

    private bool TryParseHeading(out MarkdownHeading? heading)
    {
        heading = null;
        var start = _position;

        if (!Match(MarkdownNodeKind.HeadingMarker)) return false;

        var marker = Previous().Text;
        var level = marker.Trim().Length;

        if (level is < 1 or > 6)
        {
            _position = start;
            return false;
        }

        var text = ReadLineText();
        var children = ParseInline(text);
        heading = new MarkdownHeading(level, children);
        return true;
    }

    private bool TryParseSetextHeading(out MarkdownHeading? heading)
    {
        heading = null;
        var start = _position;

        if (!Check(MarkdownNodeKind.Text)) return false;

        var textLines = new List<string> { Advance().Text.Trim() };

        while (!IsAtEnd() && Check(MarkdownNodeKind.NewLine))
        {
            var newlinePos = _position;
            Advance();

            if (Check(MarkdownNodeKind.SetextHeadingMarker))
            {
                var marker = Advance().Text.Trim();
                var level = marker[0] == '=' ? 1 : 2;
                SkipToEndOfLine();

                var text = string.Join(" ", textLines);
                var children = ParseInline(text);
                heading = new MarkdownHeading(level, children);
                return true;
            }

            if (Check(MarkdownNodeKind.Text))
            {
                textLines.Add(Advance().Text.Trim());
            }
            else
            {
                _position = newlinePos + 1;
                break;
            }
        }

        _position = start;
        return false;
    }

    private bool TryParseCodeBlock(out MarkdownCodeBlock? codeBlock)
    {
        codeBlock = null;
        var start = _position;

        if (!Match(MarkdownNodeKind.CodeBlockMarker)) return false;

        var language = string.Empty;
        var remaining = ReadLineText().Trim();

        if (!string.IsNullOrEmpty(remaining)) language = remaining;

        var contentLines = new List<string>();

        while (!IsAtEnd())
        {
            if (Check(MarkdownNodeKind.CodeBlockMarker))
            {
                Advance();
                SkipToEndOfLine();
                break;
            }

            if (Check(MarkdownNodeKind.NewLine))
            {
                contentLines.Add(string.Empty);
                Advance();
            }
            else if (Check(MarkdownNodeKind.Text) || Check(MarkdownNodeKind.Whitespace))
            {
                contentLines.Add(ReadLineText());
            }
            else
            {
                var token = Advance();
                if (token.Kind != MarkdownNodeKind.Eof) contentLines.Add(token.Text);
            }
        }

        var content = string.Join("\n", contentLines).TrimEnd('\n');
        codeBlock = new MarkdownCodeBlock(
            string.IsNullOrEmpty(language) ? null : language,
            content);
        return true;
    }

    private bool TryParseIndentedCodeBlock(out MarkdownIndentedCodeBlock? codeBlock)
    {
        codeBlock = null;
        var start = _position;

        if (!Check(MarkdownNodeKind.IndentedCodeMarker)) return false;

        var lines = new List<string>();

        while (!IsAtEnd())
            if (Check(MarkdownNodeKind.IndentedCodeMarker))
            {
                Advance();
                var line = ReadLineText().TrimStart();
                lines.Add(line);
            }
            else if (Check(MarkdownNodeKind.NewLine))
            {
                Advance();
            }
            else
            {
                break;
            }

        if (lines.Count == 0)
        {
            _position = start;
            return false;
        }

        var content = string.Join("\n", lines).TrimEnd('\n');
        codeBlock = new MarkdownIndentedCodeBlock(content);
        return true;
    }

    private bool TryParseBlockquote(out MarkdownBlockquote? blockquote)
    {
        blockquote = null;
        var start = _position;

        if (!Check(MarkdownNodeKind.BlockquoteMarker)) return false;

        var lines = new List<string>();

        while (!IsAtEnd() && Check(MarkdownNodeKind.BlockquoteMarker))
        {
            Advance();
            lines.Add(ReadLineText());
        }

        if (lines.Count == 0)
        {
            _position = start;
            return false;
        }

        var content = string.Join("\n", lines);
        var innerParser = new MarkdownParser(_config);
        var innerLexer = new MarkdownLexer(_config);
        var tokens = innerLexer.Tokenize(content);
        var innerDoc = innerParser.Parse(tokens);

        blockquote = new MarkdownBlockquote(innerDoc.Children);
        return true;
    }

    private bool TryParseList(out MarkdownList? list)
    {
        list = null;
        var start = _position;

        var isOrdered = Check(MarkdownNodeKind.OrderedListMarker);
        var isUnordered = Check(MarkdownNodeKind.UnorderedListMarker);

        if (!isOrdered && !isUnordered) return false;

        var items = new List<MarkdownNode>();

        while (!IsAtEnd())
        {
            if (Check(MarkdownNodeKind.TaskListMarker))
            {
                var taskMarker = Advance().Text;
                var isChecked = taskMarker.Contains('x') || taskMarker.Contains('X');

                var itemLines = new List<string> { ReadLineText() };

                while (!IsAtEnd() && !IsListItemStart() && !IsBlockEnd()) itemLines.Add(ReadLineText());

                var itemContent = string.Join("\n", itemLines);
                var innerParser = new MarkdownParser(_config);
                var innerLexer = new MarkdownLexer(_config);
                var tokens = innerLexer.Tokenize(itemContent);
                var itemDoc = innerParser.Parse(tokens);

                items.Add(new MarkdownTaskListItem(isChecked, itemDoc.Children));
                continue;
            }

            if (isOrdered && !Check(MarkdownNodeKind.OrderedListMarker)) break;

            if (isUnordered && !Check(MarkdownNodeKind.UnorderedListMarker)) break;

            Advance();

            var itemLines2 = new List<string> { ReadLineText() };

            while (!IsAtEnd() && !IsListItemStart() && !IsBlockEnd()) itemLines2.Add(ReadLineText());

            var itemContent2 = string.Join("\n", itemLines2);
            var innerParser2 = new MarkdownParser(_config);
            var innerLexer2 = new MarkdownLexer(_config);
            var tokens2 = innerLexer2.Tokenize(itemContent2);
            var itemDoc2 = innerParser2.Parse(tokens2);

            items.Add(new MarkdownListItem(itemDoc2.Children));
        }

        if (items.Count == 0)
        {
            _position = start;
            return false;
        }

        list = new MarkdownList(isOrdered, items.ToList());
        return true;
    }

    private bool TryParseTable(out MarkdownTable? table)
    {
        table = null;
        var start = _position;

        var headerRow = TryParseTableRow();
        if (headerRow is null) return false;

        SkipEmptyLines();

        if (!IsAtEnd() && Check(MarkdownNodeKind.TableDelimiter))
        {
            SkipToEndOfLine();
        }
        else
        {
            _position = start;
            return false;
        }

        var rows = new List<MarkdownTableRow>();

        while (!IsAtEnd())
        {
            SkipEmptyLines();

            if (IsAtEnd() || IsBlockEnd()) break;

            var row = TryParseTableRow();
            if (row is not null)
                rows.Add(row);
            else
                break;
        }

        table = new MarkdownTable(headerRow, rows);
        return true;
    }

    private MarkdownTableRow? TryParseTableRow()
    {
        var start = _position;
        var cells = new List<MarkdownTableCell>();

        if (Check(MarkdownNodeKind.TableDelimiter)) Advance();

        while (!IsAtEnd() && !Check(MarkdownNodeKind.NewLine))
        {
            var cellText = ReadUntil(MarkdownNodeKind.TableDelimiter, MarkdownNodeKind.NewLine);

            if (!string.IsNullOrWhiteSpace(cellText))
            {
                var children = ParseInline(cellText.Trim());
                cells.Add(new MarkdownTableCell(children));
            }

            if (Check(MarkdownNodeKind.TableDelimiter))
                Advance();
            else
                break;
        }

        if (cells.Count == 0)
        {
            _position = start;
            return null;
        }

        SkipToEndOfLine();
        return new MarkdownTableRow(cells);
    }

    private bool TryParseFootnoteDefinition(out MarkdownFootnoteDefinition? footnoteDef)
    {
        footnoteDef = null;
        var start = _position;

        if (!Check(MarkdownNodeKind.FootnoteMarker)) return false;

        Advance();

        var label = new StringBuilder();
        while (!IsAtEnd() && !Check(MarkdownNodeKind.LinkClose)) label.Append(Advance().Text);

        if (!Match(MarkdownNodeKind.LinkClose))
        {
            _position = start;
            return false;
        }

        if (!Match(MarkdownNodeKind.Colon))
        {
            _position = start;
            return false;
        }

        var content = ReadLineText().Trim();
        var children = ParseInline(content);

        footnoteDef = new MarkdownFootnoteDefinition(label.ToString().Trim(), children);
        return true;
    }

    private bool TryParseReferenceLinkDefinition(out MarkdownReferenceLinkDefinition? refLink)
    {
        refLink = null;
        var start = _position;

        if (!Match(MarkdownNodeKind.LinkOpen)) return false;

        var label = new StringBuilder();
        while (!IsAtEnd() && !Check(MarkdownNodeKind.LinkClose)) label.Append(Advance().Text);

        if (!Match(MarkdownNodeKind.LinkClose))
        {
            _position = start;
            return false;
        }

        if (!Match(MarkdownNodeKind.Colon))
        {
            _position = start;
            return false;
        }

        SkipWhitespaceTokens();

        var url = new StringBuilder();
        while (!IsAtEnd() && !Check(MarkdownNodeKind.NewLine) && !Check(MarkdownNodeKind.Whitespace))
            url.Append(Advance().Text);

        string? title = null;
        if (Check(MarkdownNodeKind.Whitespace))
        {
            Advance();
            if (Check(MarkdownNodeKind.Text))
            {
                var titleText = Advance().Text.Trim();
                if (titleText.StartsWith('"') && titleText.EndsWith('"'))
                    title = titleText[1..^1];
                else
                    title = titleText;
            }
        }

        SkipToEndOfLine();

        var labelStr = label.ToString().Trim();
        var urlStr = url.ToString().Trim();

        refLink = new MarkdownReferenceLinkDefinition(labelStr, urlStr, title);
        _referenceLinks[labelStr] = refLink;
        return true;
    }

    private bool TryParseHtmlBlock(out MarkdownHtmlBlock? htmlBlock)
    {
        htmlBlock = null;
        var start = _position;

        if (!Check(MarkdownNodeKind.HtmlTag)) return false;

        var sb = new StringBuilder();

        while (!IsAtEnd() && Check(MarkdownNodeKind.HtmlTag))
        {
            sb.Append(Advance().Text);

            while (!IsAtEnd() && !Check(MarkdownNodeKind.NewLine) && !Check(MarkdownNodeKind.HtmlTag))
                sb.Append(Advance().Text);

            if (Check(MarkdownNodeKind.NewLine))
                sb.Append(Advance().Text);
            else
                break;
        }

        if (sb.Length == 0)
        {
            _position = start;
            return false;
        }

        htmlBlock = new MarkdownHtmlBlock(sb.ToString().TrimEnd('\n'));
        return true;
    }

    private bool TryParseMathBlock(out MarkdownMathBlock? mathBlock)
    {
        mathBlock = null;
        var start = _position;

        if (!Check(MarkdownNodeKind.MathMarker)) return false;

        var marker = Current().Text;
        if (marker.Length < 2) return false;

        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (Check(MarkdownNodeKind.MathMarker) && Current().Text.Length >= 2)
            {
                Advance();
                SkipToEndOfLine();
                break;
            }

            if (Check(MarkdownNodeKind.NewLine))
            {
                Advance();
                sb.Append('\n');
                continue;
            }

            sb.Append(Advance().Text);
        }

        mathBlock = new MarkdownMathBlock(sb.ToString().Trim('\n', '\r'));
        return true;
    }

    private MarkdownParagraph ParseParagraph()
    {
        var lines = new List<string>();

        while (!IsAtEnd() && !IsBlockEnd())
        {
            lines.Add(ReadLineText());

            if (IsAtEnd() || Check(MarkdownNodeKind.NewLine))
                if (PeekNextNonNewLine() is null || IsBlockEnd(true))
                    break;
        }

        var text = string.Join(" ", lines);
        var children = ParseInline(text);

        return new MarkdownParagraph(children);
    }

    #endregion

    #region 辅助方法

    private string ReadLineText()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && !Check(MarkdownNodeKind.NewLine)) sb.Append(Advance().Text);

        if (Check(MarkdownNodeKind.NewLine)) Advance();

        return sb.ToString();
    }

    private string ReadLineTextBeforeMarker()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && !Check(MarkdownNodeKind.MathMarker) && !Check(MarkdownNodeKind.NewLine))
            sb.Append(Advance().Text);

        return sb.ToString();
    }

    private string ReadUntil(params NodeKind[] stopTypes)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && !stopTypes.Contains(Current().Kind)) sb.Append(Advance().Text);

        return sb.ToString();
    }

    private void SkipEmptyLines()
    {
        while (!IsAtEnd() && Check(MarkdownNodeKind.NewLine)) Advance();
    }

    private void SkipWhitespaceTokens()
    {
        while (!IsAtEnd() && Check(MarkdownNodeKind.Whitespace)) Advance();
    }

    private void SkipToEndOfLine()
    {
        while (!IsAtEnd() && !Check(MarkdownNodeKind.NewLine)) Advance();

        if (Check(MarkdownNodeKind.NewLine)) Advance();
    }

    private bool IsListItemStart()
    {
        return Check(MarkdownNodeKind.OrderedListMarker)
               || Check(MarkdownNodeKind.UnorderedListMarker)
               || Check(MarkdownNodeKind.TaskListMarker);
    }

    private bool IsBlockEnd(bool skipNewLines = false)
    {
        if (IsAtEnd()) return true;

        if (Check(MarkdownNodeKind.Eof)) return true;

        if (Check(MarkdownNodeKind.HorizontalRule)) return true;

        if (Check(MarkdownNodeKind.HeadingMarker)) return true;

        if (Check(MarkdownNodeKind.CodeBlockMarker)) return true;

        if (Check(MarkdownNodeKind.BlockquoteMarker)) return true;

        if (IsListItemStart()) return true;

        if (Check(MarkdownNodeKind.TableDelimiter) && skipNewLines) return true;

        if (Check(MarkdownNodeKind.IndentedCodeMarker) && skipNewLines) return true;

        if (Check(MarkdownNodeKind.HtmlTag) && _config.EnableHtmlBlocks) return true;

        if (_config.EnableMath && Check(MarkdownNodeKind.MathMarker) && Current().Text.Length >= 2) return true;

        return false;
    }

    private GreenLeafNode? PeekNextNonNewLine()
    {
        var pos = _position;

        while (pos < _tokens.Count && _tokens[pos].Kind == MarkdownNodeKind.NewLine) pos++;

        if (pos < _tokens.Count) return _tokens[pos];

        return null;
    }

    private bool IsAtEnd()
    {
        return _position >= _tokens.Count || Current().Kind == MarkdownNodeKind.Eof;
    }

    private GreenLeafNode Current()
    {
        if (_position < _tokens.Count) return _tokens[_position];

        return _tokens[_tokens.Count - 1];
    }

    private GreenLeafNode Advance()
    {
        var token = Current();
        _position++;
        return token;
    }

    private bool Check(NodeKind type)
    {
        if (IsAtEnd()) return false;

        return Current().Kind == type;
    }

    private bool Match(NodeKind type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private GreenLeafNode Previous()
    {
        return _tokens[_position - 1];
    }

    #endregion
}

/// <summary>
///     Markdown 行内解析器
/// </summary>
internal sealed class InlineParser
{
    private static readonly HashSet<char> EscapableChars =
        ['\\', '`', '*', '_', '{', '}', '[', ']', '(', ')', '#', '+', '-', '.', '!', '|', '~', '>', '=', '$'];

    private readonly MarkdownLanguageConfig _config;
    private readonly Dictionary<string, MarkdownReferenceLinkDefinition> _referenceLinks;
    private readonly string _text;
    private int _position;

    public InlineParser(string text, MarkdownLanguageConfig? config = null,
        Dictionary<string, MarkdownReferenceLinkDefinition>? referenceLinks = null)
    {
        _text = text;
        _config = config ?? MarkdownLanguageConfig.Default;
        _referenceLinks = referenceLinks ??
                          new Dictionary<string, MarkdownReferenceLinkDefinition>(StringComparer.OrdinalIgnoreCase);
        _position = 0;
    }

    public IReadOnlyList<MarkdownNode> Parse()
    {
        var nodes = new List<MarkdownNode>();
        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (TryParseEscape(out var escapedText))
            {
                FlushText();
                nodes.Add(new MarkdownText(escapedText));
                continue;
            }

            if (TryParseStrong(out var strong))
            {
                FlushText();
                nodes.Add(strong!);
                continue;
            }

            if (TryParseEmphasis(out var emphasis))
            {
                FlushText();
                nodes.Add(emphasis!);
                continue;
            }

            if (_config.EnableStrikethrough && TryParseStrikethrough(out var strike))
            {
                FlushText();
                nodes.Add(strike!);
                continue;
            }

            if (_config.EnableHighlight && TryParseHighlight(out var highlight))
            {
                FlushText();
                nodes.Add(highlight!);
                continue;
            }

            if (TryParseInlineCode(out var code))
            {
                FlushText();
                nodes.Add(code!);
                continue;
            }

            if (TryParseLink(out var link))
            {
                FlushText();
                nodes.Add(link!);
                continue;
            }

            if (TryParseImage(out var image))
            {
                FlushText();
                nodes.Add(image!);
                continue;
            }

            if (_config.EnableFootnotes && TryParseFootnote(out var footnote))
            {
                FlushText();
                nodes.Add(footnote!);
                continue;
            }

            if (_config.EnableMath && TryParseMathInline(out var math))
            {
                FlushText();
                nodes.Add(math!);
                continue;
            }

            if (_config.EnableHtmlInline && TryParseHtmlInline(out var htmlInline))
            {
                FlushText();
                nodes.Add(htmlInline!);
                continue;
            }

            if (_config.EnableAutoLinks && TryParseAutoLink(out var autoLink))
            {
                FlushText();
                nodes.Add(autoLink!);
                continue;
            }

            if (TryParseLineBreak())
            {
                FlushText();
                nodes.Add(new MarkdownLineBreak());
                continue;
            }

            if (TryParseSoftBreak())
            {
                FlushText();
                nodes.Add(new MarkdownSoftBreak());
                continue;
            }

            sb.Append(Advance());
        }

        FlushText();
        return nodes;

        void FlushText()
        {
            if (sb.Length > 0)
            {
                nodes.Add(new MarkdownText(sb.ToString()));
                sb.Clear();
            }
        }
    }

    private bool TryParseEscape(out string escaped)
    {
        escaped = string.Empty;
        var start = _position;

        if (Peek() != '\\') return false;

        if (_position + 1 < _text.Length && EscapableChars.Contains(_text[_position + 1]))
        {
            _position++;
            escaped = Advance().ToString();
            return true;
        }

        return false;
    }

    private bool TryParseStrong(out MarkdownStrong? strong)
    {
        strong = null;
        var start = _position;

        if (!Match("**")) return false;

        var content = ReadUntil("**");
        if (!Match("**"))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _referenceLinks);
        strong = new MarkdownStrong(innerParser.Parse());
        return true;
    }

    private bool TryParseEmphasis(out MarkdownEmphasis? emphasis)
    {
        emphasis = null;
        var start = _position;

        if (!Match("*") || Peek() == '*')
        {
            _position = start;
            return false;
        }

        var content = ReadUntil("*");
        if (!Match("*"))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _referenceLinks);
        emphasis = new MarkdownEmphasis(innerParser.Parse());
        return true;
    }

    private bool TryParseStrikethrough(out MarkdownStrikethrough? strike)
    {
        strike = null;
        var start = _position;

        if (!Match("~~")) return false;

        var content = ReadUntil("~~");
        if (!Match("~~"))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _referenceLinks);
        strike = new MarkdownStrikethrough(innerParser.Parse());
        return true;
    }

    private bool TryParseHighlight(out MarkdownHighlight? highlight)
    {
        highlight = null;
        var start = _position;

        if (!Match("==")) return false;

        var content = ReadUntil("==");
        if (!Match("=="))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _referenceLinks);
        highlight = new MarkdownHighlight(innerParser.Parse());
        return true;
    }

    private bool TryParseInlineCode(out MarkdownInlineCode? code)
    {
        code = null;
        var start = _position;

        if (!Match("`")) return false;

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '`') sb.Append(Advance());

        if (!Match("`"))
        {
            _position = start;
            return false;
        }

        code = new MarkdownInlineCode(sb.ToString());
        return true;
    }

    private bool TryParseLink(out MarkdownLink? link)
    {
        link = null;
        var start = _position;

        if (!Match("[")) return false;

        var text = ReadUntil("]");
        if (!Match("]"))
        {
            _position = start;
            return false;
        }

        if (Match("("))
        {
            var url = ReadUrl();
            string? title = null;

            if (Peek() == ' ')
            {
                Advance();
                if (Peek() == '"')
                {
                    Advance();
                    title = ReadUntil("\"");
                    Match("\"");
                }
            }

            if (!Match(")"))
            {
                _position = start;
                return false;
            }

            var innerParser = new InlineParser(text, _config, _referenceLinks);
            link = new MarkdownLink(url, title, innerParser.Parse());
            return true;
        }

        if (_config.EnableReferenceLinks && Match("["))
        {
            var refLabel = ReadUntil("]");
            if (!Match("]"))
            {
                _position = start;
                return false;
            }

            var label = string.IsNullOrEmpty(refLabel) ? text : refLabel;
            var innerParser = new InlineParser(text, _config, _referenceLinks);

            if (_referenceLinks.TryGetValue(label, out var refDef))
            {
                link = new MarkdownLink(refDef.Url, refDef.Title, innerParser.Parse());
                return true;
            }

            link = new MarkdownLink($"#{label}", null, innerParser.Parse());
            return true;
        }

        _position = start;
        return false;
    }

    private bool TryParseImage(out MarkdownImage? image)
    {
        image = null;
        var start = _position;

        if (!Match("![")) return false;

        var alt = ReadUntil("]");
        if (!Match("]"))
        {
            _position = start;
            return false;
        }

        if (Match("("))
        {
            var url = ReadUrl();
            string? title = null;

            if (Peek() == ' ')
            {
                Advance();
                if (Peek() == '"')
                {
                    Advance();
                    title = ReadUntil("\"");
                    Match("\"");
                }
            }

            if (!Match(")"))
            {
                _position = start;
                return false;
            }

            image = new MarkdownImage(url, alt, title);
            return true;
        }

        if (_config.EnableReferenceLinks && Match("["))
        {
            var refLabel = ReadUntil("]");
            Match("]");

            var label = string.IsNullOrEmpty(refLabel) ? alt : refLabel;

            if (_referenceLinks.TryGetValue(label, out var refDef))
            {
                image = new MarkdownImage(refDef.Url, alt, refDef.Title);
                return true;
            }

            image = new MarkdownImage($"#{label}", alt);
            return true;
        }

        _position = start;
        return false;
    }

    private bool TryParseFootnote(out MarkdownFootnote? footnote)
    {
        footnote = null;
        var start = _position;

        if (!Match("[^")) return false;

        var label = new StringBuilder();
        while (!IsAtEnd() && Peek() != ']') label.Append(Advance());

        if (!Match("]"))
        {
            _position = start;
            return false;
        }

        footnote = new MarkdownFootnote(label.ToString());
        return true;
    }

    private bool TryParseMathInline(out MarkdownMathInline? math)
    {
        math = null;
        var start = _position;

        if (!Match("$") || Peek() == '$')
        {
            _position = start;
            return false;
        }

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '$') sb.Append(Advance());

        if (!Match("$"))
        {
            _position = start;
            return false;
        }

        math = new MarkdownMathInline(sb.ToString());
        return true;
    }

    private bool TryParseHtmlInline(out MarkdownHtmlInline? htmlInline)
    {
        htmlInline = null;
        var start = _position;

        if (Peek() != '<') return false;

        var sb = new StringBuilder();
        sb.Append(Advance());

        if (!IsAtEnd() && (Peek() == '/' || char.IsLetter(Peek())))
        {
            while (!IsAtEnd() && Peek() != '>') sb.Append(Advance());

            if (Match(">"))
            {
                sb.Append('>');
                htmlInline = new MarkdownHtmlInline(sb.ToString());
                return true;
            }
        }

        _position = start;
        return false;
    }

    private bool TryParseAutoLink(out MarkdownLink? link)
    {
        link = null;

        if (!(_text.AsSpan(_position).StartsWith("http://") || _text.AsSpan(_position).StartsWith("https://")))
            return false;

        var sb = new StringBuilder();

        while (!IsAtEnd() && !char.IsWhiteSpace(Peek()) && Peek() != ')' && Peek() != ']') sb.Append(Advance());

        var url = sb.ToString();
        link = new MarkdownLink(url, null, new List<MarkdownNode> { new MarkdownText(url) });
        return true;
    }

    private bool TryParseLineBreak()
    {
        if (Peek() == '\\' && PeekNext() == '\n')
        {
            Advance();
            Advance();
            return true;
        }

        if (Peek() == ' ' && PeekNext() == ' ' && PeekNextNext() == '\n')
        {
            Advance();
            Advance();
            Advance();
            return true;
        }

        return false;
    }

    private bool TryParseSoftBreak()
    {
        if (Peek() == '\n')
        {
            Advance();
            return true;
        }

        return false;
    }

    private string ReadUrl()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != ')' && Peek() != ' ') sb.Append(Advance());

        return sb.ToString();
    }

    private string ReadUntil(string terminator)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (_text.AsSpan(_position).StartsWith(terminator)) break;

            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private bool Match(string expected)
    {
        if (_text.AsSpan(_position).StartsWith(expected))
        {
            _position += expected.Length;
            return true;
        }

        return false;
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _text[_position];
    }

    private char PeekNext()
    {
        return _position + 1 >= _text.Length ? '\0' : _text[_position + 1];
    }

    private char PeekNextNext()
    {
        return _position + 2 >= _text.Length ? '\0' : _text[_position + 2];
    }

    private char Advance()
    {
        return _text[_position++];
    }

    private bool IsAtEnd()
    {
        return _position >= _text.Length;
    }
}