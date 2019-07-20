using System.Text;
using Oak.Diagnostics;
using Oak.DejaVu.Expressions;
using Oak.DejaVu.Optimizer;
using Oak.Parsing;
using OakTextReader = Oak.Text.TextReader;

namespace Oak.DejaVu;

/// <summary>
///     DejaVu 语言定义
/// </summary>
public sealed class DejaVuLanguage
{
    /// <summary>
    ///     Dora 模板语言（使用 &lt;% %&gt;）
    /// </summary>
    public static readonly DejaVuLanguage Dora = new(
        "dora",
        "<%",
        "%>",
        "<%--",
        "--%>"
    );

    /// <summary>
    ///     Doki 模板语言（使用 {% %}）
    /// </summary>
    public static readonly DejaVuLanguage Doki = new(
        "doki",
        "{%",
        "%}",
        "{%--",
        "--%}"
    );

    /// <summary>
    ///     创建 DejaVu 语言定义
    /// </summary>
    public DejaVuLanguage(string name, string openingDelimiter, string closingDelimiter, string commentStart,
        string commentEnd)
    {
        Name = name;
        OpeningDelimiter = openingDelimiter;
        ClosingDelimiter = closingDelimiter;
        CommentStart = commentStart;
        CommentEnd = commentEnd;
    }

    /// <summary>
    ///     代码块开始分隔符
    /// </summary>
    public string OpeningDelimiter { get; init; }

    /// <summary>
    ///     代码块结束分隔符
    /// </summary>
    public string ClosingDelimiter { get; init; }

    /// <summary>
    ///     注释开始分隔符
    /// </summary>
    public string CommentStart { get; init; }

    /// <summary>
    ///     注释结束分隔符
    /// </summary>
    public string CommentEnd { get; init; }

    /// <summary>
    ///     语言名称
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     根据名称获取语言定义
    /// </summary>
    public static DejaVuLanguage GetByName(string name)
    {
        return name switch
        {
            "dora" => Dora,
            "doki" => Doki,
            _ => throw new ArgumentException($"Unknown language: {name}")
        };
    }
}

/// <summary>
///     end 闭合检查结果
/// </summary>
internal enum EndCheckResult
{
    /// <summary>
    ///     不是 end 指令
    /// </summary>
    NotEnd,

    /// <summary>
    ///     end 栈匹配（裸 end）
    /// </summary>
    EndStack,

    /// <summary>
    ///     end 显式匹配（end if / end loop 等）
    /// </summary>
    EndExplicit
}

/// <summary>
///     DejaVu 模板解析器
/// </summary>
public sealed class DejaVuParser
{
    #region 字段

    private readonly DiagnosticSink _diagnostics;
    private readonly DejaVuLanguage _language;
    private readonly ExpressionParser? _expressionParser;

    private static readonly HashSet<string> BlockKeywords = new(StringComparer.Ordinal)
    {
        "if", "loop", "match", "block"
    };

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建 DejaVu 解析器
    /// </summary>
    /// <param name="language">模板语言</param>
    /// <param name="diagnostics">诊断消息收集器</param>
    /// <param name="expressionParser">表达式解析器（若提供则预解析表达式）</param>
    public DejaVuParser(DejaVuLanguage language, DiagnosticSink? diagnostics = null, ExpressionParser? expressionParser = null)
    {
        _language = language;
        _diagnostics = diagnostics ?? new DiagnosticSink();
        _expressionParser = expressionParser;
    }

    /// <summary>
    ///     创建 DejaVu 解析器
    /// </summary>
    /// <param name="languageName">模板语言名称，可选值："dora" 或 "doki"</param>
    /// <param name="diagnostics">诊断消息收集器</param>
    /// <param name="expressionParser">表达式解析器（若提供则预解析表达式）</param>
    public DejaVuParser(string languageName, DiagnosticSink? diagnostics = null, ExpressionParser? expressionParser = null)
    {
        try
        {
            _language = DejaVuLanguage.GetByName(languageName);
        }
        catch (ArgumentException ex)
        {
            _language = DejaVuLanguage.Dora;
            _diagnostics = diagnostics ?? new DiagnosticSink();
            _expressionParser = expressionParser;
            _diagnostics.AddError("", default, "InvalidTemplateType", ex.Message);
            return;
        }

        _diagnostics = diagnostics ?? new DiagnosticSink();
        _expressionParser = expressionParser;
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     解析 DejaVu 模板
    /// </summary>
    /// <returns>解析结果</returns>
    public DejaVuParseResult Parse(string source)
    {
        var nodes = new List<DejaVuTemplateNode>();
        var reader = new OakTextReader(source);

        ParseTemplate(reader, nodes);

        return new DejaVuParseResult
        {
            Nodes = nodes,
            TemplateType = _language.Name
        };
    }

    /// <summary>
    ///     编译模板——解析 + 优化，返回可缓存的编译产物
    /// </summary>
    public CompiledTemplate Compile(string source, string templatePath = "")
    {
        var parseResult = Parse(source);
        return CompiledTemplate.Compile(parseResult, templatePath, DateTimeOffset.UtcNow);
    }

    #endregion

    #region 解析核心

    /// <summary>
    ///     解析模板内容
    /// </summary>
    private void ParseTemplate(OakTextReader reader, List<DejaVuTemplateNode> nodes)
    {
        var sb = new StringBuilder();

        while (!reader.IsAtEnd)
        {
            var text = ReadUntilDelimiter(reader, out var isCode, out var isComment);

            if (!string.IsNullOrEmpty(text)) sb.Append(text);

            if (reader.IsAtEnd) break;

            if (isComment)
            {
                SkipComment(reader);
            }
            else if (isCode)
            {
                if (sb.Length > 0)
                {
                    nodes.Add(new DejaVuTextNode { Text = sb.ToString() });
                    sb.Clear();
                }

                ProcessCodeBlock(reader, nodes);
            }
        }

        if (sb.Length > 0) nodes.Add(new DejaVuTextNode { Text = sb.ToString() });
    }

    /// <summary>
    ///     读取文本直到遇到分隔符
    /// </summary>
    private string ReadUntilDelimiter(OakTextReader reader, out bool isCode, out bool isComment)
    {
        isCode = false;
        isComment = false;

        var start = reader.Position;

        while (!reader.IsAtEnd)
        {
            if (reader.Peek() == _language.CommentStart[0])
                if (CheckDelimiter(reader, _language.CommentStart))
                {
                    isComment = true;
                    break;
                }

            if (reader.Peek() == _language.OpeningDelimiter[0])
                if (CheckDelimiter(reader, _language.OpeningDelimiter))
                    if (!_language.CommentStart.StartsWith(_language.OpeningDelimiter) ||
                        !CheckDelimiter(reader, _language.CommentStart))
                    {
                        isCode = true;
                        break;
                    }

            reader.Advance();
        }

        return reader.Slice(start, reader.Position - start);
    }

    /// <summary>
    ///     检查当前位置是否匹配指定分隔符
    /// </summary>
    private bool CheckDelimiter(OakTextReader reader, string delimiter)
    {
        for (var i = 0; i < delimiter.Length; i++)
            if (reader.Peek(i) != delimiter[i])
                return false;

        return true;
    }

    /// <summary>
    ///     跳过注释
    /// </summary>
    private void SkipComment(OakTextReader reader)
    {
        for (var i = 0; i < _language.CommentStart.Length; i++) reader.Advance();

        while (!reader.IsAtEnd)
        {
            if (reader.Peek() == _language.CommentEnd[0])
                if (CheckDelimiter(reader, _language.CommentEnd))
                {
                    for (var i = 0; i < _language.CommentEnd.Length; i++) reader.Advance();

                    return;
                }

            reader.Advance();
        }

        _diagnostics.AddError("", default, "UnclosedComment", "未闭合的注释。");
    }

    #endregion

    #region 代码块处理

    /// <summary>
    ///     读取代码块内容
    /// </summary>
    private string ReadCodeContent(OakTextReader reader)
    {
        for (var i = 0; i < _language.OpeningDelimiter.Length; i++) reader.Advance();

        var codeStart = reader.Position;

        while (!reader.IsAtEnd)
        {
            if (reader.Peek() == _language.ClosingDelimiter[0])
                if (CheckDelimiter(reader, _language.ClosingDelimiter))
                    break;

            reader.Advance();
        }

        var codeContent = reader.Slice(codeStart, reader.Position - codeStart).Trim();

        if (reader.IsAtEnd)
        {
            _diagnostics.AddError("", default, "UnclosedCodeBlock", "未闭合的代码块。");
            return codeContent;
        }

        for (var i = 0; i < _language.ClosingDelimiter.Length; i++) reader.Advance();

        return codeContent;
    }

    /// <summary>
    ///     检查 end 指令类型
    /// </summary>
    /// <param name="codeContent">代码块内容</param>
    /// <param name="expectedType">期望的块类型</param>
    /// <param name="actualType">实际的块类型（仅显式 end 时有效）</param>
    /// <returns>end 检查结果</returns>
    private static EndCheckResult CheckEndDirective(string codeContent, string expectedType, out string? actualType)
    {
        actualType = null;

        if (codeContent == "end")
        {
            return EndCheckResult.EndStack;
        }

        if (codeContent.StartsWith("end "))
        {
            var type = codeContent["end ".Length..].Trim();
            actualType = type;
            return EndCheckResult.EndExplicit;
        }

        return EndCheckResult.NotEnd;
    }

    /// <summary>
    ///     处理 end 闭合，返回是否匹配成功
    /// </summary>
    private bool HandleEnd(string codeContent, string expectedType)
    {
        var result = CheckEndDirective(codeContent, expectedType, out var actualType);

        switch (result)
        {
            case EndCheckResult.EndStack:
                return true;

            case EndCheckResult.EndExplicit:
                if (actualType != expectedType)
                {
                    _diagnostics.AddError("", default, "EndTypeMismatch",
                        $"end 类型不匹配：期望 end {expectedType}，实际 end {actualType}。");
                }

                return true;

            default:
                return false;
        }
    }

    /// <summary>
    ///     处理代码块
    /// </summary>
    private void ProcessCodeBlock(OakTextReader reader, List<DejaVuTemplateNode> nodes)
    {
        var codeContent = ReadCodeContent(reader);

        if (CheckEndDirective(codeContent, "", out _) != EndCheckResult.NotEnd)
        {
            _diagnostics.AddError("", default, "UnexpectedEnd", "此处没有需要闭合的块。");
            return;
        }

        if (codeContent.StartsWith("if "))
        {
            var condition = codeContent["if ".Length..].Trim();
            var ifNode = new DejaVuIfNode { Condition = condition, ParsedCondition = TryParseExpression(condition) };
            nodes.Add(ifNode);
            ParseIfBlock(reader, ifNode);
        }
        else if (codeContent == "raw")
        {
            var rawNode = new DejaVuRawNode();
            nodes.Add(rawNode);
            ParseBlock(reader, rawNode.Children, "raw");
        }
        else if (codeContent.StartsWith("loop "))
        {
            var loopNode = ParseLoopDirective(codeContent);
            nodes.Add(loopNode);
            ParseBlock(reader, loopNode.Children, "loop");
        }
        else if (codeContent.StartsWith("match "))
        {
            var expression = codeContent["match ".Length..].Trim();
            var matchNode = new DejaVuMatchNode { Expression = expression, ParsedExpression = TryParseExpression(expression) };
            nodes.Add(matchNode);
            ParseBlock(reader, matchNode.Children, "match");
        }
        else if (codeContent.StartsWith("block "))
        {
            var blockName = codeContent["block ".Length..].Trim();
            var blockNode = new DejaVuBlockNode { Name = blockName };
            nodes.Add(blockNode);
            ParseBlock(reader, blockNode.Children, "block");
        }
        else if (codeContent.StartsWith("let "))
        {
            var letContent = codeContent["let ".Length..].Trim();
            var eqIndex = letContent.IndexOf('=');
            if (eqIndex > 0)
            {
                var varName = letContent[..eqIndex].Trim();
                var expr = letContent[(eqIndex + 1)..].Trim();
                var letNode = new DejaVuLetNode { VariableName = varName, Expression = expr, ParsedExpression = TryParseExpression(expr) };
                nodes.Add(letNode);
                ParseBlock(reader, letNode.Children, "let");
            }
            else
            {
                nodes.Add(new DejaVuCodeNode { Code = codeContent });
            }
        }
        else if (codeContent.StartsWith("with "))
        {
            var withContent = codeContent["with ".Length..].Trim();
            var eqIndex = withContent.IndexOf('=');
            if (eqIndex > 0)
            {
                var aliasName = withContent[..eqIndex].Trim();
                var expr = withContent[(eqIndex + 1)..].Trim();
                var withNode = new DejaVuWithNode { AliasName = aliasName, Expression = expr, ParsedExpression = TryParseExpression(expr) };
                nodes.Add(withNode);
                ParseBlock(reader, withNode.Children, "with");
            }
            else
            {
                nodes.Add(new DejaVuCodeNode { Code = codeContent });
            }
        }
        else if (codeContent == "super()")
        {
            nodes.Add(new DejaVuSuperNode());
        }
        else if (codeContent.StartsWith("extends "))
        {
            var parentTemplate = codeContent["extends ".Length..].Trim();
            nodes.Add(new DejaVuExtendsNode { ParentTemplate = parentTemplate });
        }
        else if (codeContent.StartsWith("include "))
        {
            var templatePath = codeContent["include ".Length..].Trim();
            nodes.Add(new DejaVuIncludeNode { TemplatePath = templatePath });
        }
        else
        {
            nodes.Add(new DejaVuCodeNode { Code = codeContent, ParsedExpression = TryParseExpression(codeContent) });
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     解析 loop 指令（支持 loop items 和 loop item in items 两种语法）
    /// </summary>
    private DejaVuLoopNode ParseLoopDirective(string codeContent)
    {
        var loopContent = codeContent["loop ".Length..].Trim();
        var inIndex = loopContent.IndexOf(" in ");
        if (inIndex > 0)
        {
            var itemName = loopContent[..inIndex].Trim();
            var expression = loopContent[(inIndex + 4)..].Trim();
            return new DejaVuLoopNode { ItemName = itemName, Expression = expression, ParsedExpression = TryParseExpression(expression) };
        }

        return new DejaVuLoopNode { Expression = loopContent, ParsedExpression = TryParseExpression(loopContent) };
    }

    /// <summary>
    ///     安全地解析表达式，失败时返回 null
    /// </summary>
    private IExpressionNode? TryParseExpression(string expression)
    {
        if (_expressionParser == null || string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        try
        {
            return _expressionParser.Parse(expression);
        }
        catch (ParseException)
        {
            return null;
        }
    }

    /// <summary>
    ///     解析 raw 块（所有内容作为原始文本，不解析标签）
    /// </summary>
    private void ParseRawBlock(OakTextReader reader, List<DejaVuTemplateNode> nodes)
    {
        var endMarker = _language.OpeningDelimiter + " end " + _language.ClosingDelimiter;
        var endMarkerAlt = _language.OpeningDelimiter + " end" + _language.ClosingDelimiter;
        var endMarkerRaw = _language.OpeningDelimiter + " end raw " + _language.ClosingDelimiter;
        var endMarkerRawAlt = _language.OpeningDelimiter + " end raw" + _language.ClosingDelimiter;

        var remaining = reader.Slice(reader.Position, reader.Remaining);
        var sb = new System.Text.StringBuilder();

        var endIndex = remaining.IndexOf(endMarker, StringComparison.Ordinal);
        var endAltIndex = remaining.IndexOf(endMarkerAlt, StringComparison.Ordinal);

        if (endIndex < 0 || (endAltIndex >= 0 && endAltIndex < endIndex))
        {
            endIndex = endAltIndex;
        }

        if (remaining.IndexOf(endMarkerRaw, StringComparison.Ordinal) == endIndex ||
            remaining.IndexOf(endMarkerRawAlt, StringComparison.Ordinal) == endIndex)
        {
            // end raw 也是有效的结束标记
        }

        if (endIndex >= 0)
        {
            if (endIndex > 0)
            {
                sb.Append(remaining[..endIndex]);
            }
        }
        else
        {
            sb.Append(remaining);
        }

        if (sb.Length > 0)
        {
            nodes.Add(new DejaVuTextNode { Text = sb.ToString() });
        }
    }

    #endregion

    #region 块解析

    /// <summary>
    ///     解析块内容
    /// </summary>
    private void ParseBlock(OakTextReader reader, List<DejaVuTemplateNode> nodes, string blockType)
    {
        if (blockType == "raw")
        {
            ParseRawBlock(reader, nodes);
            return;
        }

        while (!reader.IsAtEnd)
        {
            var text = ReadUntilDelimiter(reader, out var isCode, out var isComment);

            if (!string.IsNullOrEmpty(text)) nodes.Add(new DejaVuTextNode { Text = text });

            if (reader.IsAtEnd) break;

            if (isComment)
            {
                SkipComment(reader);
                continue;
            }

            if (!isCode) continue;

            var codeContent = ReadCodeContent(reader);

            if (HandleEnd(codeContent, blockType)) return;

            if (reader.IsAtEnd && codeContent.Length > 0)
            {
                nodes.Add(new DejaVuCodeNode { Code = codeContent, ParsedExpression = TryParseExpression(codeContent) });
                return;
            }

            if (codeContent.StartsWith("if "))
            {
                var condition = codeContent["if ".Length..].Trim();
                var ifNode = new DejaVuIfNode { Condition = condition, ParsedCondition = TryParseExpression(condition) };
                nodes.Add(ifNode);
                ParseIfBlock(reader, ifNode);
            }
            else if (codeContent.StartsWith("loop "))
            {
                var loopNode = ParseLoopDirective(codeContent);
                nodes.Add(loopNode);
                ParseBlock(reader, loopNode.Children, "loop");
            }
            else if (codeContent.StartsWith("match "))
            {
                var expression = codeContent["match ".Length..].Trim();
                var matchNode = new DejaVuMatchNode { Expression = expression, ParsedExpression = TryParseExpression(expression) };
                nodes.Add(matchNode);
                ParseBlock(reader, matchNode.Children, "match");
            }
            else if (codeContent.StartsWith("block "))
            {
                var blockName = codeContent["block ".Length..].Trim();
                var blockNode = new DejaVuBlockNode { Name = blockName };
                nodes.Add(blockNode);
                ParseBlock(reader, blockNode.Children, "block");
            }
            else
            {
                nodes.Add(new DejaVuCodeNode { Code = codeContent, ParsedExpression = TryParseExpression(codeContent) });
            }
        }
    }

    /// <summary>
    ///     解析 if 块（支持 else 和 else if）
    /// </summary>
    private void ParseIfBlock(OakTextReader reader, DejaVuIfNode ifNode)
    {
        var currentNodes = ifNode.Children;

        while (!reader.IsAtEnd)
        {
            var text = ReadUntilDelimiter(reader, out var isCode, out var isComment);

            if (!string.IsNullOrEmpty(text)) currentNodes.Add(new DejaVuTextNode { Text = text });

            if (reader.IsAtEnd) break;

            if (isComment)
            {
                SkipComment(reader);
                continue;
            }

            if (!isCode) continue;

            var codeContent = ReadCodeContent(reader);

            if (HandleEnd(codeContent, "if")) return;

            if (reader.IsAtEnd && codeContent.Length > 0)
            {
                currentNodes.Add(new DejaVuCodeNode { Code = codeContent, ParsedExpression = TryParseExpression(codeContent) });
                return;
            }

            if (codeContent.StartsWith("else if "))
            {
                var condition = codeContent["else if ".Length..].Trim();
                var elseIfNode = new DejaVuElseIfNode { Condition = condition, ParsedCondition = TryParseExpression(condition) };
                ifNode.ElseIfNodes.Add(elseIfNode);
                currentNodes = elseIfNode.Children;
            }
            else if (codeContent == "else")
            {
                currentNodes = ifNode.ElseChildren;
            }
            else if (codeContent.StartsWith("if "))
            {
                var condition = codeContent["if ".Length..].Trim();
                var nestedIfNode = new DejaVuIfNode { Condition = condition, ParsedCondition = TryParseExpression(condition) };
                currentNodes.Add(nestedIfNode);
                ParseIfBlock(reader, nestedIfNode);
            }
            else if (codeContent.StartsWith("loop "))
            {
                var loopNode = ParseLoopDirective(codeContent);
                currentNodes.Add(loopNode);
                ParseBlock(reader, loopNode.Children, "loop");
            }
            else if (codeContent.StartsWith("match "))
            {
                var expression = codeContent["match ".Length..].Trim();
                var matchNode = new DejaVuMatchNode { Expression = expression, ParsedExpression = TryParseExpression(expression) };
                currentNodes.Add(matchNode);
                ParseBlock(reader, matchNode.Children, "match");
            }
            else if (codeContent.StartsWith("block "))
            {
                var blockName = codeContent["block ".Length..].Trim();
                var blockNode = new DejaVuBlockNode { Name = blockName };
                currentNodes.Add(blockNode);
                ParseBlock(reader, blockNode.Children, "block");
            }
            else
            {
                currentNodes.Add(new DejaVuCodeNode { Code = codeContent, ParsedExpression = TryParseExpression(codeContent) });
            }
        }
    }
    #endregion

    /// <summary>
    ///     获取解析过程中的诊断信息。
    /// </summary>
    public DiagnosticSink GetDiagnostics()
    {
        return _diagnostics;
    }
}
