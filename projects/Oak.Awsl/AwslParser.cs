using System.Text;
using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Widget.Lexer;

namespace Oak.Widget;

/// <summary>
///     AWSL 语法解析器，基于 AwslLexer 的 Token 流进行递归下降解析。
///     支持组件标签（&lt;widget&gt;, &lt;template&gt;, &lt;script&gt;, &lt;style&gt;）、
///     响应式绑定（@bind, @click）、事件处理、组件声明、模板表达式、条件/循环指令等。
/// </summary>
public sealed class AwslParser
{
    private readonly DiagnosticSink _diagnostics;
    private IReadOnlyList<GreenLeafNode> _tokens = [];
    private int _position;

    /// <summary>
    ///     当前 Token
    /// </summary>
    private GreenLeafNode Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    /// <summary>
    ///     创建 AWSL 语法解析器
    /// </summary>
    /// <param name="diagnostics">诊断接收器</param>
    public AwslParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    /// <summary>
    ///     解析 AWSL 源码，返回组件解析结果
    /// </summary>
    /// <param name="source">AWSL 源码</param>
    /// <param name="filePath">源文件路径（用于提取组件名）</param>
    /// <returns>解析结果</returns>
    public WidgetParseResult Parse(string source, string filePath = "")
    {
        var lexer = new AwslLexer(_diagnostics);
        _tokens = lexer.Tokenize(source);
        _position = 0;

        var result = new WidgetParseResult
        {
            Name = ExtractComponentName(filePath),
            Properties = new List<WidgetProperty>(),
            Methods = new List<WidgetMethod>(),
            TemplateNodes = new List<WidgetTemplateNode>(),
            Styles = new Dictionary<string, string>()
        };

        var properties = (List<WidgetProperty>)result.Properties;
        var methods = (List<WidgetMethod>)result.Methods;
        var templateNodes = (List<WidgetTemplateNode>)result.TemplateNodes;
        var styles = (Dictionary<string, string>)result.Styles;

        while (!IsAtEnd())
        {
            if (IsBlockStart("script"))
            {
                ParseScriptBlock(properties, methods);
            }
            else if (IsBlockStart("widget") || IsBlockStart("template"))
            {
                ParseWidgetBlock(templateNodes);
            }
            else if (IsBlockStart("style"))
            {
                ParseStyleBlock(styles);
            }
            else
            {
                Advance();
            }
        }

        return result;
    }

    #region Token 流操作

    /// <summary>
    ///     是否到达 Token 流末尾
    /// </summary>
    private bool IsAtEnd()
    {
        return Current.Kind == AwslNodeKind.Eof;
    }

    /// <summary>
    ///     前进到下一个 Token 并返回当前 Token
    /// </summary>
    private GreenLeafNode Advance()
    {
        if (!IsAtEnd())
        {
            _position++;
        }

        return _tokens[_position - 1];
    }

    /// <summary>
    ///     查看前方第 offset 个 Token
    /// </summary>
    private GreenLeafNode Peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _tokens.Count ? _tokens[index] : _tokens[^1];
    }

    /// <summary>
    ///     检查当前 Token 的种类
    /// </summary>
    private bool IsKind(NodeKind kind)
    {
        return Current.Kind == kind;
    }

    /// <summary>
    ///     检查当前 Token 的种类是否为给定值之一
    /// </summary>
    private bool IsKind(NodeKind a, NodeKind b)
    {
        return Current.Kind == a || Current.Kind == b;
    }

    /// <summary>
    ///     检查当前 Token 的文本
    /// </summary>
    private bool IsToken(string text)
    {
        return Current.Text == text;
    }

    /// <summary>
    ///     检查指定偏移处 Token 的种类是否为给定值之一
    /// </summary>
    private bool IsKindAt(int offset, NodeKind a, NodeKind b)
    {
        var kind = Peek(offset).Kind;
        return kind == a || kind == b;
    }

    /// <summary>
    ///     尝试匹配指定种类的 Token
    /// </summary>
    private bool Match(NodeKind kind)
    {
        if (Current.Kind == kind)
        {
            Advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     尝试匹配指定文本的 Token
    /// </summary>
    private bool Match(string text)
    {
        if (Current.Text == text)
        {
            Advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     尝试匹配指定种类和文本的 Token
    /// </summary>
    private bool Match(NodeKind kind, string text)
    {
        if (Current.Kind == kind && Current.Text == text)
        {
            Advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     期望匹配指定文本的 Token，否则报错
    /// </summary>
    private bool Expect(string text)
    {
        if (Current.Text == text)
        {
            Advance();
            return true;
        }

        _diagnostics.AddError(
            string.Empty,
            default,
            "AWSL2001",
            $"期望 '{text}'，但遇到了 '{Current.Text}'");

        return false;
    }

    /// <summary>
    ///     跳过直到遇到指定文本的 Token
    /// </summary>
    private void SkipUntil(string text)
    {
        while (!IsAtEnd() && Current.Text != text)
        {
            Advance();
        }
    }

    #endregion

    #region 块检测

    /// <summary>
    ///     检查当前位置是否是块起始标签（&lt;tagName&gt; 或 &lt;tagName ... &gt;）
    /// </summary>
    private bool IsBlockStart(string tagName)
    {
        if (Current.Kind != AwslNodeKind.Operator || Current.Text != "<")
        {
            return false;
        }

        return IsKindAt(1, AwslNodeKind.Keyword, AwslNodeKind.Identifier) && Peek(1).Text == tagName;
    }

    /// <summary>
    ///     检查是否是块结束标签（&lt;/tagName&gt;）
    /// </summary>
    private bool IsBlockEnd(string tagName)
    {
        if (Current.Kind != AwslNodeKind.Operator || Current.Text != "</")
        {
            return false;
        }

        return IsKindAt(1, AwslNodeKind.Keyword, AwslNodeKind.Identifier) && Peek(1).Text == tagName;
    }

    /// <summary>
    ///     跳过块起始标签（&lt;tagName ... &gt;）
    /// </summary>
    private void SkipBlockStart(string tagName)
    {
        Expect("<");
        Expect(tagName);

        while (!IsAtEnd() && !(Current.Kind == AwslNodeKind.Operator && Current.Text == ">"))
        {
            Advance();
        }

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == ">")
        {
            Advance();
        }
    }

    /// <summary>
    ///     跳过块结束标签（&lt;/tagName&gt;）
    /// </summary>
    private void SkipBlockEnd(string tagName)
    {
        if (Current.Kind == AwslNodeKind.Operator && Current.Text == "</")
        {
            Advance();
        }

        if (Current.Text == tagName)
        {
            Advance();
        }

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == ">")
        {
            Advance();
        }
    }

    #endregion

    #region Script 块解析

    /// <summary>
    ///     解析 &lt;script&gt; 块
    /// </summary>
    private void ParseScriptBlock(List<WidgetProperty> properties, List<WidgetMethod> methods)
    {
        SkipBlockStart("script");

        while (!IsAtEnd() && !IsBlockEnd("script"))
        {
            if (Current.Kind == AwslNodeKind.Keyword)
            {
                switch (Current.Text)
                {
                    case "import":
                        ParseImportDeclaration();
                        break;
                    case "let":
                        ParseLetDeclaration(properties);
                        break;
                    case "const":
                        ParseConstDeclaration(properties);
                        break;
                    case "micro":
                        ParseMicroDeclaration(methods);
                        break;
                    default:
                        Advance();
                        break;
                }
            }
            else
            {
                Advance();
            }
        }

        SkipBlockEnd("script");
    }

    /// <summary>
    ///     解析 import 语句：import { A, B } from "path"
    /// </summary>
    private void ParseImportDeclaration()
    {
        Advance();
    }

    /// <summary>
    ///     解析 let 声明：let name: type = value
    /// </summary>
    private void ParseLetDeclaration(List<WidgetProperty> properties)
    {
        Advance();

        if (!IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            SkipUntil("\n");

            return;
        }

        var name = Current.Text ?? string.Empty;
        Advance();

        var typeName = "auto";
        if (Current.Kind == AwslNodeKind.Punctuation && Current.Text == ":")
        {
            Advance();

            if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
            {
                typeName = MapTypeName(Current.Text ?? "auto");
                Advance();
            }
        }

        string? defaultValue = null;
        var valueKind = WidgetValueKind.None;

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == "=")
        {
            Advance();
            (defaultValue, valueKind) = ParseDefaultValue();
        }

        properties.Add(new WidgetProperty
        {
            Name = name,
            TypeName = typeName,
            IsReadonly = false,
            DefaultValue = defaultValue,
            DefaultValueKind = valueKind
        });
    }

    /// <summary>
    ///     解析 const 声明：const name: type = value
    /// </summary>
    private void ParseConstDeclaration(List<WidgetProperty> properties)
    {
        Advance();

        if (!IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            SkipUntil("\n");

            return;
        }

        var name = Current.Text ?? string.Empty;
        Advance();

        var typeName = "auto";
        if (Current.Kind == AwslNodeKind.Punctuation && Current.Text == ":")
        {
            Advance();

            if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
            {
                typeName = MapTypeName(Current.Text ?? "auto");
                Advance();
            }
        }

        string? defaultValue = null;
        var valueKind = WidgetValueKind.None;

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == "=")
        {
            Advance();
            (defaultValue, valueKind) = ParseDefaultValue();
        }

        properties.Add(new WidgetProperty
        {
            Name = name,
            TypeName = typeName,
            IsReadonly = true,
            DefaultValue = defaultValue,
            DefaultValueKind = valueKind
        });
    }

    /// <summary>
    ///     解析 micro 函数声明：micro name(params): retType { body }
    /// </summary>
    private void ParseMicroDeclaration(List<WidgetMethod> methods)
    {
        Advance();

        if (!IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            SkipUntil("}");

            return;
        }

        var funcName = Current.Text ?? string.Empty;
        Advance();

        var parameters = ParseMicroParameters();
        var returnType = string.Empty;

        if (Current.Kind == AwslNodeKind.Punctuation && Current.Text == ":")
        {
            Advance();

            if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
            {
                returnType = Current.Text ?? string.Empty;
                Advance();
            }
        }

        var body = ParseBracedBody();

        methods.Add(new WidgetMethod
        {
            Name = funcName,
            Parameters = parameters,
            Body = body,
            IsMicro = true
        });
    }

    /// <summary>
    ///     解析 micro 函数的参数列表 (a: i32, b: string)
    /// </summary>
    private string ParseMicroParameters()
    {
        if (Current.Kind != AwslNodeKind.Delimiter || Current.Text != "(")
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var depth = 1;
        Advance();
        sb.Append('(');

        while (!IsAtEnd() && depth > 0)
        {
            if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "(")
            {
                depth++;
            }
            else if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == ")")
            {
                depth--;
                if (depth == 0)
                {
                    sb.Append(')');
                    Advance();

                    break;
                }
            }

            sb.Append(Current.Text);
            Advance();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     解析花括号包围的代码体 { ... }
    /// </summary>
    private string ParseBracedBody()
    {
        if (Current.Kind != AwslNodeKind.Delimiter || Current.Text != "{")
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var depth = 1;
        Advance();

        while (!IsAtEnd() && depth > 0)
        {
            if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{")
            {
                depth++;
            }
            else if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "}")
            {
                depth--;
                if (depth == 0)
                {
                    Advance();

                    break;
                }
            }

            sb.Append(Current.Text);
            Advance();
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    ///     解析默认值表达式
    /// </summary>
    private (string? Value, WidgetValueKind Kind) ParseDefaultValue()
    {
        if (IsKind(AwslNodeKind.String))
        {
            var strVal = Current.Text ?? string.Empty;
            Advance();

            return (strVal, WidgetValueKind.String);
        }

        if (IsKind(AwslNodeKind.Number))
        {
            var numVal = Current.Text ?? "0";
            Advance();

            return (numVal, WidgetValueKind.Number);
        }

        if (IsKind(AwslNodeKind.Literal))
        {
            var litVal = Current.Text ?? string.Empty;
            Advance();

            return (litVal.ToLower(), WidgetValueKind.Boolean);
        }

        if (IsKind(AwslNodeKind.Delimiter) && IsToken("["))
        {
            var arrSb = new StringBuilder();
            var arrDepth = 1;
            arrSb.Append(Advance().Text);

            while (!IsAtEnd() && arrDepth > 0)
            {
                if (IsKind(AwslNodeKind.Delimiter))
                {
                    if (IsToken("["))
                    {
                        arrDepth++;
                    }
                    else if (IsToken("]"))
                    {
                        arrDepth--;
                    }
                }

                arrSb.Append(Current.Text);
                Advance();
            }

            return (arrSb.ToString(), WidgetValueKind.Array);
        }

        if (IsKind(AwslNodeKind.Delimiter) && IsToken("{"))
        {
            var exprSb = new StringBuilder();
            var exprDepth = 1;
            Advance();

            while (!IsAtEnd() && exprDepth > 0)
            {
                if (IsKind(AwslNodeKind.Delimiter))
                {
                    if (IsToken("{"))
                    {
                        exprDepth++;
                    }
                    else if (IsToken("}"))
                    {
                        exprDepth--;
                        if (exprDepth == 0)
                        {
                            Advance();

                            break;
                        }
                    }
                }

                exprSb.Append(Current.Text);
                Advance();
            }

            return (exprSb.ToString().Trim(), WidgetValueKind.Expression);
        }

        if (IsKind(AwslNodeKind.Identifier) || IsKind(AwslNodeKind.Keyword))
        {
            var idVal = Current.Text ?? string.Empty;
            Advance();

            return (idVal, WidgetValueKind.Identifier);
        }

        return (null, WidgetValueKind.None);
    }

    /// <summary>
    ///     映射 JS 类型名到 GG 类型名
    /// </summary>
    private static string MapTypeName(string jsType)
    {
        return jsType.Trim() switch
        {
            "number" => "f64",
            "boolean" => "bool",
            "object" => "map",
            "array" => "list",
            _ => jsType.Trim()
        };
    }

    #endregion

    #region Widget/Template 块解析

    /// <summary>
    ///     解析 &lt;widget&gt; 或 &lt;template&gt; 块
    /// </summary>
    private void ParseWidgetBlock(List<WidgetTemplateNode> templateNodes)
    {
        var blockTag = Peek(1).Text ?? "widget";
        SkipBlockStart(blockTag);

        while (!IsAtEnd() && !IsBlockEnd(blockTag))
        {
            ParseTemplateContent(templateNodes);
        }

        SkipBlockEnd(blockTag);
    }

    /// <summary>
    ///     解析模板内容（元素、文本、插值、控制流）
    /// </summary>
    private void ParseTemplateContent(List<WidgetTemplateNode> nodes)
    {
        if (Current.Kind == AwslNodeKind.Operator && Current.Text == "<")
        {
            var next = Peek(1);
            if (next.Kind == AwslNodeKind.Operator && next.Text == "/")
            {
                return;
            }

            var nextText = next.Text ?? string.Empty;
            if (next.Kind == AwslNodeKind.Keyword || next.Kind == AwslNodeKind.Identifier)
            {
                switch (nextText)
                {
                    case "if":
                        ParseIfBlock(nodes);

                        return;
                    case "loop":
                    case "for":
                        ParseLoopBlock(nodes);

                        return;
                    default:
                        ParseElement(nodes);

                        return;
                }
            }
        }

        if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{")
        {
            var interpNode = ParseInterpolation();

            if (interpNode is not null)
            {
                nodes.Add(interpNode);
            }

            return;
        }

        ParseTextNode(nodes);
    }

    /// <summary>
    ///     解析元素节点：&lt;TagName attrs...&gt; children &lt;/TagName&gt; 或 &lt;TagName attrs... /&gt;
    /// </summary>
    private void ParseElement(List<WidgetTemplateNode> nodes)
    {
        Expect("<");

        var tagName = Current.Text ?? string.Empty;
        Advance();

        var attributes = ParseElementAttributes();
        var isSelfClosing = false;

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == "/>")
        {
            isSelfClosing = true;
            Advance();
        }
        else if (Current.Kind == AwslNodeKind.Operator && Current.Text == ">")
        {
            Advance();
        }

        if (isSelfClosing)
        {
            nodes.Add(new WidgetElementNode
            {
                TagName = tagName,
                Attributes = attributes,
                IsSelfClosing = true
            });

            return;
        }

        var children = new List<WidgetTemplateNode>();

        while (!IsAtEnd() && !(IsBlockEnd(tagName)))
        {
            ParseTemplateContent(children);
        }

        SkipBlockEnd(tagName);

        nodes.Add(new WidgetElementNode
        {
            TagName = tagName,
            Attributes = attributes,
            Children = children,
            IsSelfClosing = false
        });
    }

    /// <summary>
    ///     解析元素属性列表
    /// </summary>
    private Dictionary<string, string> ParseElementAttributes()
    {
        var attributes = new Dictionary<string, string>();

        while (!IsAtEnd())
        {
            if (Current.Kind == AwslNodeKind.Operator && Current.Text is ">" or "/>")
            {
                break;
            }

            if (Current.Kind == AwslNodeKind.AtPrefix)
            {
                var atName = "@" + (Current.Text ?? string.Empty);
                Advance();

                if (Current.Kind == AwslNodeKind.Operator && Current.Text == "=")
                {
                    Advance();
                    var atValue = ParseAttributeValue();
                    attributes[atName] = atValue;
                }
                else
                {
                    attributes[atName] = "true";
                }
            }
            else if (IsKind(AwslNodeKind.Identifier) || IsKind(AwslNodeKind.Keyword))
            {
                var attrName = Current.Text ?? string.Empty;
                Advance();

                if (Current.Kind == AwslNodeKind.Operator && Current.Text == "=")
                {
                    Advance();
                    var attrValue = ParseAttributeValue();
                    attributes[attrName] = attrValue;
                }
                else
                {
                    attributes[attrName] = "true";
                }
            }
            else
            {
                break;
            }
        }

        return attributes;
    }

    /// <summary>
    ///     解析属性值（字符串、表达式或标识符）
    /// </summary>
    private string ParseAttributeValue()
    {
        if (IsKind(AwslNodeKind.String))
        {
            var strVal = Current.Text ?? string.Empty;
            Advance();

            return strVal;
        }

        if (IsKind(AwslNodeKind.Delimiter) && IsToken("{"))
        {
            return ParseBracedExpression();
        }

        if (IsKind(AwslNodeKind.Number))
        {
            var num = Current.Text ?? string.Empty;
            Advance();

            return num;
        }

        if (IsKind(AwslNodeKind.Literal))
        {
            var lit = Current.Text ?? string.Empty;
            Advance();

            return lit.ToLower();
        }

        if (IsKind(AwslNodeKind.Identifier) || IsKind(AwslNodeKind.Keyword))
        {
            var id = Current.Text ?? string.Empty;
            Advance();

            return id;
        }

        return string.Empty;
    }

    /// <summary>
    ///     解析花括号表达式 { expr }
    /// </summary>
    private string ParseBracedExpression()
    {
        if (Current.Kind != AwslNodeKind.Delimiter || Current.Text != "{")
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var depth = 1;
        Advance();

        while (!IsAtEnd() && depth > 0)
        {
            if (Current.Kind == AwslNodeKind.Delimiter)
            {
                if (Current.Text == "{")
                {
                    depth++;
                }
                else if (Current.Text == "}")
                {
                    depth--;
                    if (depth == 0)
                    {
                        Advance();

                        break;
                    }
                }
            }

            sb.Append(Current.Kind == AwslNodeKind.String ? $"\"{Current.Text}\"" : Current.Text);
            Advance();
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    ///     解析插值节点：{ expression }
    /// </summary>
    private WidgetInterpolationNode? ParseInterpolation()
    {
        if (Current.Kind != AwslNodeKind.Delimiter || Current.Text != "{")
        {
            return null;
        }

        var expr = ParseBracedExpression();
        return new WidgetInterpolationNode { Expression = expr };
    }

    /// <summary>
    ///     解析文本节点
    /// </summary>
    private void ParseTextNode(List<WidgetTemplateNode> nodes)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd()
               && !(Current.Kind == AwslNodeKind.Operator && Current.Text == "<")
               && !(Current.Kind == AwslNodeKind.Operator && Current.Text == "</")
               && !(Current.Kind == AwslNodeKind.Operator && Current.Text == "/>")
               && !(Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{"))
        {
            sb.Append(Current.Text);
            Advance();
        }

        var text = sb.ToString().Trim();

        if (!string.IsNullOrEmpty(text))
        {
            nodes.Add(new WidgetTextNode { Text = text });
        }
    }

    #endregion

    #region 控制流块解析

    /// <summary>
    ///     解析 if 块：&lt;if {condition}&gt; children [&lt;else/&gt; elseChildren] &lt;/if&gt;
    /// </summary>
    private void ParseIfBlock(List<WidgetTemplateNode> nodes)
    {
        Expect("<");
        Expect("if");

        var condition = ParseIfCondition();
        var ifChildren = new List<WidgetTemplateNode>();
        var elseChildren = new List<WidgetTemplateNode>();

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == ">")
        {
            Advance();
        }

        var inElse = false;

        while (!IsAtEnd() && !IsBlockEnd("if"))
        {
            if (!inElse && Current.Kind == AwslNodeKind.Operator && Current.Text == "<"
                && Peek(1).Text == "else" && Peek(2).Kind == AwslNodeKind.Operator && Peek(2).Text == "/>")
            {
                Advance();
                Advance();
                Advance();
                inElse = true;

                continue;
            }

            var target = inElse ? elseChildren : ifChildren;
            ParseTemplateContent(target);
        }

        SkipBlockEnd("if");

        nodes.Add(new WidgetIfNode
        {
            Condition = condition,
            Children = ifChildren,
            ElseChildren = elseChildren
        });
    }

    /// <summary>
    ///     解析 if 条件表达式。
    ///     支持三种语法：
    ///     &lt;if {condition}&gt; — 花括号表达式
    ///     &lt;if condition={expr}&gt; — 属性语法
    ///     &lt;if showMessage&gt; — 纯标识符
    /// </summary>
    private string ParseIfCondition()
    {
        if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{")
        {
            return ParseBracedExpression();
        }

        var raw = ParseRawUntil(">");

        var eqIdx = raw.IndexOf('=');
        if (eqIdx >= 0)
        {
            var valuePart = raw[(eqIdx + 1)..].Trim();

            if (valuePart.StartsWith("{") && valuePart.EndsWith("}"))
            {
                return valuePart[1..^1].Trim();
            }

            if ((valuePart.StartsWith("\"") && valuePart.EndsWith("\""))
                || (valuePart.StartsWith("'") && valuePart.EndsWith("'")))
            {
                return valuePart[1..^1];
            }

            return valuePart;
        }

        return raw.Trim();
    }

    /// <summary>
    ///     读取原始 Token 文本直到遇到指定字符串
    /// </summary>
    private string ParseRawUntil(string endText)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && !(Current.Kind == AwslNodeKind.Operator && Current.Text == endText))
        {
            sb.Append(Current.Text);
            Advance();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     解析 loop/for 块。
    ///     支持多种语法：
    ///     &lt;loop item in {items}&gt; — 标准语法
    ///     &lt;for each={item} in={items}&gt; — 属性语法
    ///     &lt;for item in items&gt; — 简写
    /// </summary>
    private void ParseLoopBlock(List<WidgetTemplateNode> nodes)
    {
        Expect("<");
        var loopKeyword = Current.Text ?? "loop";
        Advance();

        var iterator = "item";
        var iterable = "items";

        if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            var first = Current.Text ?? string.Empty;

            if (first == "each")
            {
                Advance();
                if (Current.Kind == AwslNodeKind.Operator && Current.Text == "=")
                {
                    Advance();
                    iterator = ParseLoopAttributeValue();
                }
            }
            else if (first == "in")
            {
                Advance();
                iterable = ParseLoopAttributeValue();
            }
            else
            {
                iterator = first;
                Advance();
            }
        }

        if (Current.Kind == AwslNodeKind.Keyword && Current.Text == "in")
        {
            Advance();

            if (Current.Kind == AwslNodeKind.Operator && Current.Text == "=")
            {
                Advance();
            }

            iterable = ParseIterableExpression();
        }
        else if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            var next = Current.Text ?? string.Empty;

            if (next is "in")
            {
                Advance();
                iterable = ParseIterableExpression();
            }
            else
            {
                iterable = next;
                Advance();
            }
        }

        if (Current.Kind == AwslNodeKind.Operator && Current.Text == ">")
        {
            Advance();
        }

        var children = new List<WidgetTemplateNode>();

        while (!IsAtEnd() && !IsBlockEnd(loopKeyword))
        {
            ParseTemplateContent(children);
        }

        SkipBlockEnd(loopKeyword);

        nodes.Add(new WidgetForNode
        {
            Iterator = iterator,
            Iterable = iterable,
            Children = children
        });
    }

    /// <summary>
    ///     解析 loop 的遍历表达式
    /// </summary>
    private string ParseIterableExpression()
    {
        if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{")
        {
            return ParseBracedExpression();
        }

        if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            var id = Current.Text ?? string.Empty;
            Advance();

            return id;
        }

        return "items";
    }

    /// <summary>
    ///     解析 loop 属性值（{expr}、字符串或标识符）
    /// </summary>
    private string ParseLoopAttributeValue()
    {
        if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{")
        {
            return ParseBracedExpression();
        }

        if (Current.Kind == AwslNodeKind.String)
        {
            var strVal = Current.Text ?? string.Empty;
            Advance();

            return strVal;
        }

        if (IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            var id = Current.Text ?? string.Empty;
            Advance();

            return id;
        }

        return string.Empty;
    }

    #endregion

    #region Style 块解析

    /// <summary>
    ///     解析 &lt;style&gt; 块
    /// </summary>
    private void ParseStyleBlock(Dictionary<string, string> styles)
    {
        SkipBlockStart("style");

        while (!IsAtEnd() && !IsBlockEnd("style"))
        {
            ParseCssRule(styles);
        }

        SkipBlockEnd("style");
    }

    /// <summary>
    ///     解析 CSS 规则：.className { properties }
    /// </summary>
    private void ParseCssRule(Dictionary<string, string> styles)
    {
        if (Current.Kind == AwslNodeKind.Operator && Current.Text == ".")
        {
            Advance();
        }

        if (!IsKind(AwslNodeKind.Identifier, AwslNodeKind.Keyword))
        {
            Advance();

            return;
        }

        var className = Current.Text ?? string.Empty;
        Advance();

        if (Current.Kind == AwslNodeKind.Delimiter && Current.Text == "{")
        {
            var body = ParseBracedBody();

            if (!string.IsNullOrEmpty(body))
            {
                styles[className] = body;
            }
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     从文件路径中提取组件名称
    /// </summary>
    private static string ExtractComponentName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);

        return string.IsNullOrEmpty(name) ? "AnonymousWidget" : name;
    }

    #endregion
}
