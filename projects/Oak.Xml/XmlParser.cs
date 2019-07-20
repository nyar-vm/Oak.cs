using System.Text;
using System.Xml;

namespace Oak.Xml;

/// <summary>
///     XML 文本解析器
/// </summary>
public static class XmlParser
{
    /// <summary>
    ///     解析 XML 文本为文档对象
    /// </summary>
    public static XmlDocument Parse(string content)
    {
        XmlDeclaration? declaration = null;
        XmlElement? root = null;

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using var stringReader = new StringReader(content);
        using var reader = XmlReader.Create(stringReader, settings);

        var builderStack = new Stack<ElementBuilder>();

        try
        {
            while (reader.Read())
                switch (reader.NodeType)
                {
                    case XmlNodeType.XmlDeclaration:
                        declaration = ParseDeclaration(reader);
                        break;

                    case XmlNodeType.Element:
                        var builder = CreateBuilder(reader);

                        if (builderStack.Count > 0) builderStack.Peek().Children.Add(builder.ToElement());

                        if (!reader.IsEmptyElement)
                            builderStack.Push(builder);
                        else if (builderStack.Count == 0) root = builder.ToElement();

                        break;

                    case XmlNodeType.EndElement:
                        var closedBuilder = builderStack.Pop();
                        var closedElement = closedBuilder.ToElement();

                        if (builderStack.Count > 0)
                        {
                            var parent = builderStack.Peek();
                            var index = parent.Children.Count - 1;
                            parent.Children[index] = closedElement;
                        }
                        else
                        {
                            root = closedElement;
                        }

                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        if (builderStack.Count > 0) builderStack.Peek().TextContent.Append(reader.Value);

                        break;
                }
        }
        catch (XmlException ex)
        {
            throw new FormatException($"XML 解析失败：{ex.Message}", ex);
        }

        if (root is null) throw new FormatException("XML 解析失败：未找到根元素");

        return new XmlDocument
        {
            Declaration = declaration,
            Root = root
        };
    }

    private static XmlDeclaration ParseDeclaration(XmlReader reader)
    {
        var version = reader.GetAttribute("version") ?? "1.0";
        var encoding = reader.GetAttribute("encoding") ?? "UTF-8";

        return new XmlDeclaration
        {
            Version = version,
            Encoding = encoding
        };
    }

    private static ElementBuilder CreateBuilder(XmlReader reader)
    {
        var builder = new ElementBuilder
        {
            Name = reader.Name
        };

        if (reader.HasAttributes)
        {
            for (var i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);
                builder.Attributes.Add(new XmlAttribute
                {
                    Name = reader.Name,
                    Value = reader.Value
                });
            }

            reader.MoveToElement();
        }

        return builder;
    }

    private sealed class ElementBuilder
    {
        public string Name { get; set; } = string.Empty;

        public List<XmlAttribute> Attributes { get; } = [];

        public List<XmlElement> Children { get; } = [];

        public StringBuilder TextContent { get; } = new();

        public XmlElement ToElement()
        {
            return new XmlElement
            {
                Name = Name,
                Attributes = Attributes,
                Children = Children,
                TextContent = TextContent.Length > 0 ? TextContent.ToString() : null
            };
        }
    }
}