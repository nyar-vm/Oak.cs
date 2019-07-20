using System.Text;
using Oak.Diagnostics;
using Oak.Lexing;
using Oak.Syntax;

namespace Oak.Markdown;

/// <summary>
/// Markdown 词法�E析器
/// </summary>
public sealed class MarkdownLexer : LexerBase
{
    private readonly MarkdownLanguageConfig _config;
    

    private static readonly HashSet<string> HtmlBlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "base", "basefont", "blockquote", "body",
        "caption", "center", "col", "colgroup", "dd", "details", "dialog", "dir",
        "div", "dl", "dt", "fieldset", "figcaption", "figure", "footer", "form",
        "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header",
        "hr", "html", "iframe", "legend", "li", "link", "main", "menu", "menuitem",
        "nav", "noframes", "ol", "optgroup", "option", "p", "param", "section",
        "source", "summary", "table", "tbody", "td", "tfoot", "th", "thead",
        "title", "tr", "track", "ul"
    };

    /// <summary>
    /// 创建 Markdown 词法�E析器
    /// </summary>
    public MarkdownLexer(MarkdownLanguageConfig? config = null)
    {
        _config = config ?? MarkdownLanguageConfig.Default;
    }

    /// <summary>
    /// 封E��代码转换为词法单允E���E
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var tokens = new List<GreenLeafNode>();

        while (!IsAtEnd())
        {
            var token = ScanToken();
            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new GreenLeafNode(MarkdownNodeKind.Eof, string.Empty.Length, string.Empty));
        return tokens;
    }

    private GreenLeafNode? ScanToken()
    {
        if (IsAtEnd())
        {
            return null;
        }

        var c = Peek();

        if (c is '\n' or '\r')
        {
            return ScanNewLine();
        }

        if (c is ' ' or '\t')
        {
            return ScanWhitespace();
        }

        if (c == '#')
        {
            return ScanHeadingOrText();
        }

        if (c == '>')
        {
            return ScanBlockquoteMarker();
        }

        if (c is '-' or '*' or '+')
        {
            return ScanListOrHrOrText();
        }

        if (char.IsDigit(c))
        {
            return ScanOrderedListOrText();
        }

        if (c == '`')
        {
            return ScanCodeMarker();
        }

        if (c == '[')
        {
            return ScanBracketOpen();
        }

        if (c == ']')
        {
            return new GreenLeafNode(MarkdownNodeKind.LinkClose, Advance().ToString().Length, Advance().ToString());
        }

        if (c == '(')
        {
            return new GreenLeafNode(MarkdownNodeKind.UrlOpen, Advance().ToString().Length, Advance().ToString());
        }

        if (c == ')')
        {
            return new GreenLeafNode(MarkdownNodeKind.UrlClose, Advance().ToString().Length, Advance().ToString());
        }

        if (c == '!')
        {
            return ScanImageOrText();
        }

        if (c == '|')
        {
            return new GreenLeafNode(MarkdownNodeKind.TableDelimiter, Advance().ToString().Length, Advance().ToString());
        }

        if (c == '\\')
        {
            return ScanEscape();
        }

        if (c == '~')
        {
            if (_config.EnableStrikethrough)
            {
                return ScanStrikethroughOrText();
            }

            return ScanText();
        }

        if (c == '=' && _config.EnableSetextHeadings)
        {
            return ScanSetextHeadingOrText();
        }

        if (c == '=' && _config.EnableHighlight)
        {
            return ScanHighlightOrText();
        }

        if (c == '$' && _config.EnableMath)
        {
            return ScanMathMarker();
        }

        if (c == ':' && _config.EnableFootnotes)
        {
            return new GreenLeafNode(MarkdownNodeKind.Colon, Advance().ToString().Length, Advance().ToString());
        }

        if (c == '<' && (_config.EnableHtmlInline || _config.EnableHtmlBlocks))
        {
            var htmlToken = TryScanHtml();
            if (htmlToken is not null)
            {
                return htmlToken;
            }
        }

        if (_config.EnableAutoLinks && c is 'h' or 'H')
        {
            var autoLink = TryScanAutoLink();
            if (autoLink is not null)
            {
                return autoLink;
            }
        }

        return ScanText();
    }

    private GreenLeafNode ScanNewLine()
    {
        if (Peek() == '\r')
        {
            Advance();
        }

        if (Peek() == '\n')
        {
            Advance();
        }

        return new GreenLeafNode(MarkdownNodeKind.NewLine, "\n".Length, "\n");
    }

    private GreenLeafNode ScanWhitespace()
    {
        var atLineStart = IsAtLineStart();
        var sb = new StringBuilder();
        var spaceCount = 0;

        while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t'))
        {
            if (Peek() == ' ')
            {
                spaceCount++;
            }
            else
            {
                spaceCount += 4;
            }

            sb.Append(Advance());
        }

        if (_config.EnableIndentedCodeBlocks && spaceCount >= 4 && atLineStart)
        {
            return new GreenLeafNode(MarkdownNodeKind.IndentedCodeMarker, sb.ToString().Length, sb.ToString());
        }

        return new GreenLeafNode(MarkdownNodeKind.Whitespace, sb.ToString().Length, sb.ToString());
    }

    private bool IsAtLineStart()
    {
        return Position == 0 || Source[Position - 1] == '\n';
    }

    private GreenLeafNode ScanHeadingOrText()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!IsAtEnd() && Peek() == '#' && count < 6)
        {
            sb.Append(Advance());
            count++;
        }

        if (count > 0 && (IsAtEnd() || char.IsWhiteSpace(Peek())))
        {
            return new GreenLeafNode(MarkdownNodeKind.HeadingMarker, sb.ToString().Length, sb.ToString());
        }

        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            sb.Append(Advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.Text, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanBlockquoteMarker()
    {
        Advance();

        if (!IsAtEnd() && Peek() == ' ')
        {
            Advance();
            return new GreenLeafNode(MarkdownNodeKind.BlockquoteMarker, "> ".Length, "> ");
        }

        return new GreenLeafNode(MarkdownNodeKind.BlockquoteMarker, ">".Length, ">");
    }

    private GreenLeafNode ScanListOrHrOrText()
    {
        var marker = Advance();
        var sb = new StringBuilder();
        sb.Append(marker);

        if (!IsAtEnd() && Peek() == marker)
        {
            var count = 1;
            while (!IsAtEnd() && Peek() == marker)
            {
                sb.Append(Advance());
                count++;
            }

            if (count >= 2)
            {
                while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
                {
                    if (!char.IsWhiteSpace(Peek()))
                    {
                        return ScanTextFrom(sb.ToString());
                    }

                    sb.Append(Advance());
                }

                return new GreenLeafNode(MarkdownNodeKind.HorizontalRule, sb.ToString().Length, sb.ToString());
            }
        }

        if (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            if (Peek() == ' ')
            {
                Advance();
                var listMarker = $"{marker} ";

                if (_config.EnableTaskLists && marker == '-' && !IsAtEnd() && Peek() == '[')
                {
                    var taskToken = TryScanTaskListMarker(listMarker);
                    if (taskToken is not null)
                    {
                        return taskToken;
                    }
                }

                return new GreenLeafNode(MarkdownNodeKind.UnorderedListMarker, listMarker.Length, listMarker);
            }

            return new GreenLeafNode(MarkdownNodeKind.UnorderedListMarker, marker.ToString().Length, marker.ToString());
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode? TryScanTaskListMarker(string listPrefix)
    {
        var tempPos = Position;

        var sb = new StringBuilder(listPrefix);
        sb.Append(Advance());

        if (IsAtEnd() || (Peek() != ' ' && Peek() != 'x' && Peek() != 'X'))
        {
            Position = tempPos;
            return null;
        }

        var checkChar = Advance();
        sb.Append(checkChar);

        if (IsAtEnd() || Peek() != ']')
        {
            Position = tempPos;
            return null;
        }

        sb.Append(Advance());

        if (!IsAtEnd() && Peek() == ' ')
        {
            sb.Append(Advance());
        }

        var isChecked = checkChar is 'x' or 'X';
        return new GreenLeafNode(MarkdownNodeKind.TaskListMarker, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanOrderedListOrText()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '.')
        {
            sb.Append(Advance());

            if (!IsAtEnd() && char.IsWhiteSpace(Peek()))
            {
                if (Peek() == ' ')
                {
                    Advance();
                    return new GreenLeafNode(MarkdownNodeKind.OrderedListMarker, $"{sb} ".Length, $"{sb} ");
                }

                return new GreenLeafNode(MarkdownNodeKind.OrderedListMarker, sb.ToString().Length, sb.ToString());
            }
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode ScanCodeMarker()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!IsAtEnd() && Peek() == '`')
        {
            sb.Append(Advance());
            count++;
        }

        if (count >= 3)
        {
            return new GreenLeafNode(MarkdownNodeKind.CodeBlockMarker, sb.ToString().Length, sb.ToString());
        }

        return new GreenLeafNode(MarkdownNodeKind.InlineCodeMarker, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanBracketOpen()
    {
        if (_config.EnableFootnotes && !IsAtEnd() && PeekNext() == '^')
        {
            Advance();
            Advance();
            return new GreenLeafNode(MarkdownNodeKind.FootnoteMarker, "[^".Length, "[^");
        }

        return new GreenLeafNode(MarkdownNodeKind.LinkOpen, Advance().ToString().Length, Advance().ToString());
    }

    private GreenLeafNode ScanImageOrText()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        if (!IsAtEnd() && Peek() == '[')
        {
            sb.Append(Advance());
            return new GreenLeafNode(MarkdownNodeKind.ImageMarker, sb.ToString().Length, sb.ToString());
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode ScanEscape()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        if (!IsAtEnd())
        {
            sb.Append(Advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.Escape, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanStrikethroughOrText()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!IsAtEnd() && Peek() == '~')
        {
            sb.Append(Advance());
            count++;
        }

        if (count >= 2)
        {
            return new GreenLeafNode(MarkdownNodeKind.StrikethroughMarker, sb.ToString().Length, sb.ToString());
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode ScanSetextHeadingOrText()
    {
        var sb = new StringBuilder();
        var ch = Peek();
        var count = 0;

        while (!IsAtEnd() && Peek() == ch)
        {
            sb.Append(Advance());
            count++;
        }

        if (count >= 1)
        {
            while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
            {
                if (!char.IsWhiteSpace(Peek()))
                {
                    return ScanTextFrom(sb.ToString());
                }

                sb.Append(Advance());
            }

            return new GreenLeafNode(MarkdownNodeKind.SetextHeadingMarker, sb.ToString().Length, sb.ToString());
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode ScanHighlightOrText()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!IsAtEnd() && Peek() == '=')
        {
            sb.Append(Advance());
            count++;
        }

        if (count >= 2)
        {
            return new GreenLeafNode(MarkdownNodeKind.HighlightMarker, sb.ToString().Length, sb.ToString());
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode ScanMathMarker()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!IsAtEnd() && Peek() == '$')
        {
            sb.Append(Advance());
            count++;
        }

        if (count >= 2)
        {
            return new GreenLeafNode(MarkdownNodeKind.MathMarker, sb.ToString().Length, sb.ToString());
        }

        if (count == 1)
        {
            return new GreenLeafNode(MarkdownNodeKind.MathMarker, sb.ToString().Length, sb.ToString());
        }

        return ScanTextFrom(sb.ToString());
    }

    private GreenLeafNode? TryScanHtml()
    {
        var tempPos = Position;

        var sb = new StringBuilder();
        sb.Append(Advance());

        if (IsAtEnd())
        {
            Position = tempPos;
            return null;
        }

        var isClosing = false;
        if (Peek() == '/')
        {
            isClosing = true;
            sb.Append(Advance());
        }

        var tagName = new StringBuilder();
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '-'))
        {
            tagName.Append(Advance());
        }

        var tag = tagName.ToString();

        if (string.IsNullOrEmpty(tag))
        {
            Position = tempPos;
            return null;
        }

        if (_config.EnableHtmlBlocks && !isClosing && HtmlBlockTags.Contains(tag))
        {
            while (!IsAtEnd() && Peek() != '>')
            {
                sb.Append(Advance());
            }

            if (!IsAtEnd())
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && Peek() != '\n')
            {
                sb.Append(Advance());
            }

            return new GreenLeafNode(MarkdownNodeKind.HtmlTag, sb.ToString().Length, sb.ToString());
        }

        if (_config.EnableHtmlInline)
        {
            while (!IsAtEnd() && Peek() != '>')
            {
                sb.Append(Advance());
            }

            if (!IsAtEnd())
            {
                sb.Append(Advance());
            }

            return new GreenLeafNode(MarkdownNodeKind.HtmlTag, sb.ToString().Length, sb.ToString());
        }

        Position = tempPos;
        return null;
    }

    private GreenLeafNode? TryScanAutoLink()
    {
        var sb = new StringBuilder();
        var tempPos = Position;

        var prefix = "http";
        foreach (var ch in prefix)
        {
            if (IsAtEnd() || char.ToLower(Peek()) != ch)
            {
                Position = tempPos;
                return null;
            }

            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == 's')
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == ':' && PeekNext() == '/')
        {
            sb.Append(Advance());
            sb.Append(Advance());
        }
        else
        {
            Position = tempPos;
            return null;
        }

        while (!IsAtEnd() && !char.IsWhiteSpace(Peek()) && Peek() != ')' && Peek() != ']')
        {
            sb.Append(Advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.AutoLink, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanText()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            if (Peek() == '$' && _config.EnableMath) break;
            sb.Append(Advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.Text, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode ScanTextFrom(string prefix)
    {
        var sb = new StringBuilder(prefix);

        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            if (Peek() == '$' && _config.EnableMath) break;
            sb.Append(Advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.Text, sb.ToString().Length, sb.ToString());
    }
}
