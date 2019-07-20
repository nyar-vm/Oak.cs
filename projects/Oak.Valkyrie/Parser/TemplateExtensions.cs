using System.Text;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;
using Oak.Valkyrie.Lexer;

namespace Oak.Valkyrie.Parser;

/// <summary>
/// 元语言扩展解析：遇到 `<% ... %>` 指令时接管 TokenSource，
/// 在模板体内复用 Statement/Declaration/Expression 解析链。
/// </summary>
internal static class TemplateExtensions
{
    /// <summary>
    ///     模板指令嵌套深度。
    ///     ParseMatch / ParseIf / ParseLoop 共享此计数器，
    ///     确保嵌套的 &lt;% match|if|loop %&gt; 不会破坏外层扫描。
    /// </summary>
    private static int s_templateNestingDepth;

    internal static ValkyrieNode? ParseMetaTemplateNode(this TokenStream tokens)
    {
        if (!TryReadDirectiveTokens(tokens, out var directiveTokens) || directiveTokens.Count == 0)
        {
            return null;
        }

        var head = KindOf(directiveTokens[0]);
        if (head == ValkyrieTokenKind.Match)
        {
            return ParseMatch(tokens, directiveTokens);
        }

        if (head == ValkyrieTokenKind.If)
        {
            return ParseIf(tokens, directiveTokens);
        }

        if (head == ValkyrieTokenKind.Loop)
        {
            return ParseLoop(tokens, directiveTokens);
        }

        if (head == ValkyrieTokenKind.Equal)
        {
            var expression = ParseExpressionNode(directiveTokens.Skip(1));
            return new Splice(expression);
        }

        return null;
    }

    private static MatchTemplate ParseMatch(TokenStream tokens, IReadOnlyList<GreenLeafNode> openDirective)
    {
        var value = ParseExpressionNode(openDirective.Skip(1));
        var arms = new List<ArmTypeFragment>();
        IReadOnlyList<ValkyrieNode>? defaultBody = null;
        string? currentPattern = null;
        var isDefaultArm = false;
        var bodyTokens = new List<GreenLeafNode>();
        s_templateNestingDepth = 0;

        while (!tokens.IsAtEnd())
        {
            if (!tokens.Check(ValkyrieTokenKind.TemplateL))
            {
                bodyTokens.Add(tokens.Advance());
                continue;
            }

            if (!TryReadDirectiveTokens(tokens, out var directiveTokens) || directiveTokens.Count == 0)
            {
                break;
            }

            var firstKind = KindOf(directiveTokens[0]);

            if (IsOpeningDirective(firstKind))
            {
                s_templateNestingDepth++;
                AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.End)
            {
                if (s_templateNestingDepth > 0)
                {
                    s_templateNestingDepth--;
                    AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                    continue;
                }

                if (IsEndDirective(directiveTokens, ValkyrieTokenKind.Match))
                {
                    CommitMatchArm(arms, ref defaultBody, currentPattern, isDefaultArm, bodyTokens);
                    return new MatchTemplate(value, arms, defaultBody);
                }

                AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                continue;
            }

            if (s_templateNestingDepth > 0)
            {
                AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.Case)
            {
                CommitMatchArm(arms, ref defaultBody, currentPattern, isDefaultArm, bodyTokens);
                currentPattern = BuildText(directiveTokens.Skip(1)).Trim();
                isDefaultArm = string.Equals(currentPattern, "_", StringComparison.Ordinal)
                               || string.Equals(currentPattern, "default", StringComparison.OrdinalIgnoreCase);
                bodyTokens.Clear();
                continue;
            }

            if (firstKind == ValkyrieTokenKind.Else)
            {
                CommitMatchArm(arms, ref defaultBody, currentPattern, isDefaultArm, bodyTokens);
                currentPattern = null;
                isDefaultArm = true;
                bodyTokens.Clear();
                continue;
            }

            AppendDirectiveAsTokens(bodyTokens, directiveTokens);
        }

        CommitMatchArm(arms, ref defaultBody, currentPattern, isDefaultArm, bodyTokens);
        return new MatchTemplate(value, arms, defaultBody);
    }

    private static IfTemplate ParseIf(TokenStream tokens, IReadOnlyList<GreenLeafNode> openDirective)
    {
        var condition = ParseExpressionNode(openDirective.Skip(1));
        var thenTokens = new List<GreenLeafNode>();
        var elseTokens = new List<GreenLeafNode>();
        var inElse = false;
        s_templateNestingDepth = 0;

        while (!tokens.IsAtEnd())
        {
            if (!tokens.Check(ValkyrieTokenKind.TemplateL))
            {
                (inElse ? elseTokens : thenTokens).Add(tokens.Advance());
                continue;
            }

            if (!TryReadDirectiveTokens(tokens, out var directiveTokens) || directiveTokens.Count == 0)
            {
                break;
            }

            var firstKind = KindOf(directiveTokens[0]);

            if (IsOpeningDirective(firstKind))
            {
                s_templateNestingDepth++;
                AppendDirectiveAsTokens(inElse ? elseTokens : thenTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.End)
            {
                if (s_templateNestingDepth > 0)
                {
                    s_templateNestingDepth--;
                    AppendDirectiveAsTokens(inElse ? elseTokens : thenTokens, directiveTokens);
                    continue;
                }

                if (IsEndDirective(directiveTokens, ValkyrieTokenKind.If))
                {
                    break;
                }

                AppendDirectiveAsTokens(inElse ? elseTokens : thenTokens, directiveTokens);
                continue;
            }

            if (s_templateNestingDepth > 0)
            {
                AppendDirectiveAsTokens(inElse ? elseTokens : thenTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.Else)
            {
                inElse = true;
                continue;
            }

            AppendDirectiveAsTokens(inElse ? elseTokens : thenTokens, directiveTokens);
        }

        var thenBody = ParseBodyNodes(thenTokens);
        var elseBody = ParseBodyNodes(elseTokens);
        return new IfTemplate(condition, thenBody, elseBody.Count == 0 ? null : elseBody);
    }

    private static ValkyrieNode ParseLoop(TokenStream tokens, IReadOnlyList<GreenLeafNode> openDirective)
    {
        var bodyTokens = new List<GreenLeafNode>();
        s_templateNestingDepth = 0;

        while (!tokens.IsAtEnd())
        {
            if (!tokens.Check(ValkyrieTokenKind.TemplateL))
            {
                bodyTokens.Add(tokens.Advance());
                continue;
            }

            if (!TryReadDirectiveTokens(tokens, out var directiveTokens) || directiveTokens.Count == 0)
            {
                break;
            }

            var firstKind = KindOf(directiveTokens[0]);

            if (IsOpeningDirective(firstKind))
            {
                s_templateNestingDepth++;
                AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.End)
            {
                if (s_templateNestingDepth > 0)
                {
                    s_templateNestingDepth--;
                    AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                    continue;
                }

                if (IsEndDirective(directiveTokens, ValkyrieTokenKind.Loop))
                {
                    break;
                }

                AppendDirectiveAsTokens(bodyTokens, directiveTokens);
                continue;
            }

            AppendDirectiveAsTokens(bodyTokens, directiveTokens);
        }

        var bodyNodes = ParseBodyNodes(bodyTokens);
        var descriptor = openDirective.Skip(1).ToList();
        if (descriptor.Count > 0 && KindOf(descriptor[0]) == ValkyrieTokenKind.ParenthesisL)
        {
            var inIndex = descriptor.FindIndex(t => KindOf(t) == ValkyrieTokenKind.In);
            var vars = BuildText(descriptor.Skip(1).Take(Math.Max(0, inIndex - 2)))
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var collection = inIndex >= 0 ? BuildText(descriptor.Skip(inIndex + 1)).Trim() : string.Empty;
            return new LoopInTemplate(vars, collection, bodyNodes);
        }

        var variableName = descriptor.Count > 0 ? descriptor[0].Text?.Trim() ?? string.Empty : string.Empty;
        var rangeStart = string.Empty;
        var rangeEnd = string.Empty;
        var inTokenIndex = descriptor.FindIndex(t => KindOf(t) == ValkyrieTokenKind.In);
        if (inTokenIndex >= 0 && inTokenIndex + 3 < descriptor.Count)
        {
            rangeStart = descriptor[inTokenIndex + 3].Text ?? string.Empty;
            if (inTokenIndex + 5 < descriptor.Count)
            {
                rangeEnd = descriptor[inTokenIndex + 5].Text ?? string.Empty;
            }
        }

        return new LoopTemplate(variableName, rangeStart, rangeEnd, bodyNodes);
    }

    private static void CommitMatchArm(
        List<ArmTypeFragment> arms,
        ref IReadOnlyList<ValkyrieNode>? defaultBody,
        string? currentPattern,
        bool isDefaultArm,
        List<GreenLeafNode> bodyTokens)
    {
        if (string.IsNullOrWhiteSpace(currentPattern) && !isDefaultArm)
        {
            return;
        }

        var bodyNodes = ParseBodyNodes(bodyTokens);

        if (isDefaultArm || string.Equals(currentPattern, "_", StringComparison.Ordinal))
        {
            defaultBody = bodyNodes;
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentPattern))
        {
            var normalizedPattern = NormalizePattern(currentPattern);
            arms.Add(new ArmTypeFragment(new TypeNode(normalizedPattern, []), bodyNodes));
        }
    }

    private static IReadOnlyList<ValkyrieNode> ParseBodyNodes(IReadOnlyList<GreenLeafNode> bodyTokens)
    {
        if (bodyTokens.Count == 0)
        {
            return [];
        }

        var parserTokens = new List<GreenLeafNode>(bodyTokens.Count + 1);
        parserTokens.AddRange(bodyTokens);
        parserTokens.Add(new GreenLeafNode(ValkyrieTokenKind.Eos.ToNodeKind(), 0, string.Empty));
        var source = new TokenStream(parserTokens);
        return source.ParseTopLevelNodes(ValkyrieLanguage.Standard);
    }

    private static ValkyrieNode ParseExpressionNode(IEnumerable<GreenLeafNode> expressionTokens)
    {
        var tokenList = expressionTokens.ToList();
        if (tokenList.Count == 0)
        {
            return new IdentifierNode("_");
        }

        var parserTokens = new List<GreenLeafNode>(tokenList.Count + 1);
        parserTokens.AddRange(tokenList);
        parserTokens.Add(new GreenLeafNode(ValkyrieTokenKind.Eos.ToNodeKind(), 0, string.Empty));

        var source = new TokenStream(parserTokens);
        return source.ParseExpressionNode();
    }

    private static void AppendDirectiveAsTokens(List<GreenLeafNode> target, IReadOnlyList<GreenLeafNode> directiveTokens)
    {
        target.Add(new GreenLeafNode(ValkyrieTokenKind.TemplateL.ToNodeKind(), 2, "<%"));
        foreach (var token in directiveTokens)
        {
            target.Add(token);
        }

        target.Add(new GreenLeafNode(ValkyrieTokenKind.TemplateR.ToNodeKind(), 2, "%>"));
    }

    private static bool TryReadDirectiveTokens(TokenStream tokens, out List<GreenLeafNode> directiveTokens)
    {
        directiveTokens = [];
        if (!tokens.Check(ValkyrieTokenKind.TemplateL))
        {
            return false;
        }

        tokens.Advance();
        while (!tokens.IsAtEnd() && !tokens.Check(ValkyrieTokenKind.TemplateR))
        {
            directiveTokens.Add(tokens.Advance());
        }

        if (tokens.Check(ValkyrieTokenKind.TemplateR))
        {
            tokens.Advance();
        }

        return true;
    }

    private static bool IsOpeningDirective(ValkyrieTokenKind kind)
    {
        return kind is ValkyrieTokenKind.Match or ValkyrieTokenKind.If or ValkyrieTokenKind.Loop;
    }

    private static bool IsEndDirective(IReadOnlyList<GreenLeafNode> directiveTokens, ValkyrieTokenKind endKind)
    {
        return directiveTokens.Count >= 2 &&
               KindOf(directiveTokens[0]) == ValkyrieTokenKind.End &&
               KindOf(directiveTokens[1]) == endKind;
    }

    private static string BuildText(IEnumerable<GreenLeafNode> tokens)
    {
        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            var text = token.Text ?? string.Empty;
            if (text.Length == 0)
            {
                continue;
            }

            if (sb.Length > 0 && NeedSpace(sb[^1], text[0]))
            {
                sb.Append(' ');
            }

            sb.Append(text);
        }

        return sb.ToString();
    }

    private static bool NeedSpace(char prev, char current)
    {
        var prevWord = char.IsLetterOrDigit(prev) || prev == '_';
        var currWord = char.IsLetterOrDigit(current) || current == '_';
        return prevWord && currWord;
    }

    private static string NormalizePattern(string pattern)
    {
        var text = pattern.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
        {
            return text[1..^1];
        }

        return text;
    }

    private static ValkyrieTokenKind KindOf(GreenLeafNode token)
    {
        return (ValkyrieTokenKind)token.Kind.Value;
    }
}
