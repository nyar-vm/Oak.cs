namespace Oak.Markdown.Syntax;

/// <summary>
///     Markdown 节点类型
/// </summary>
public enum MarkdownNodeType
{
    Document,
    Heading,
    Paragraph,
    CodeBlock,
    InlineCode,
    Blockquote,
    List,
    ListItem,
    TaskListItem,
    HorizontalRule,
    Link,
    Image,
    Emphasis,
    Strong,
    Strikethrough,
    Highlight,
    LineBreak,
    SoftBreak,
    Text,
    Table,
    TableRow,
    TableCell,
    HtmlBlock,
    HtmlInline,
    Footnote,
    FootnoteDefinition,
    MathInline,
    MathBlock,
    ReferenceLinkDefinition,
    IndentedCodeBlock
}