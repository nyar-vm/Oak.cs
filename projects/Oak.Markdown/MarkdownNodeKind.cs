using Oak.Syntax;

namespace Oak.Markdown;

/// <summary>
///     Markdown 词法节点类型
/// </summary>
public static class MarkdownNodeKind
{
    public static readonly NodeKind Unknown = 0;
    public static readonly NodeKind Text = 1;
    public static readonly NodeKind NewLine = 2;
    public static readonly NodeKind Whitespace = 3;
    public static readonly NodeKind HeadingMarker = 4;
    public static readonly NodeKind StrongMarker = 5;
    public static readonly NodeKind EmphasisMarker = 6;
    public static readonly NodeKind StrikethroughMarker = 7;
    public static readonly NodeKind HighlightMarker = 8;
    public static readonly NodeKind InlineCodeMarker = 9;
    public static readonly NodeKind CodeBlockMarker = 10;
    public static readonly NodeKind CodeLanguage = 11;
    public static readonly NodeKind CodeContent = 12;
    public static readonly NodeKind BlockquoteMarker = 13;
    public static readonly NodeKind UnorderedListMarker = 14;
    public static readonly NodeKind OrderedListMarker = 15;
    public static readonly NodeKind TaskListMarker = 16;
    public static readonly NodeKind LinkOpen = 17;
    public static readonly NodeKind LinkClose = 18;
    public static readonly NodeKind UrlOpen = 19;
    public static readonly NodeKind UrlClose = 20;
    public static readonly NodeKind ImageMarker = 21;
    public static readonly NodeKind HorizontalRule = 22;
    public static readonly NodeKind TableDelimiter = 23;
    public static readonly NodeKind TableAlign = 24;
    public static readonly NodeKind Escape = 25;
    public static readonly NodeKind AutoLink = 26;
    public static readonly NodeKind HtmlTag = 27;
    public static readonly NodeKind FootnoteMarker = 28;
    public static readonly NodeKind MathMarker = 29;
    public static readonly NodeKind SetextHeadingMarker = 30;
    public static readonly NodeKind IndentedCodeMarker = 31;
    public static readonly NodeKind Colon = 32;
    public static readonly NodeKind Eof = 33;
}
