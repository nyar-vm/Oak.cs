using System.Text;
using Oak.Diagnostics;
using Oak.Lexing;
using Oak.Syntax;

namespace Oak.Valkyrie.Lexer;

/// <summary>
///     Valkyrie 词法分析器，产出 GreenLeafNode 序列
/// </summary>
public sealed class ValkyrieLexer : LexerBase
{
    private readonly ValkyrieLanguage _language;

    /// <summary>
    ///     使用默认语言配置创建词法分析器
    /// </summary>
    public ValkyrieLexer()
    {
        _language = ValkyrieLanguage.Standard;
    }

    /// <summary>
    ///     使用诊断接收器创建词法分析器
    /// </summary>
    /// <param name="diagnostics">诊断接收器</param>
    public ValkyrieLexer(DiagnosticSink? diagnostics)
    {
        _language = ValkyrieLanguage.Standard;
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     使用指定语言配置创建词法分析器
    /// </summary>
    /// <param name="language">语言配置</param>
    public ValkyrieLexer(ValkyrieLanguage language)
    {
        _language = language;
    }

    /// <summary>
    ///     使用指定语言配置和诊断接收器创建词法分析器
    /// </summary>
    /// <param name="language">语言配置</param>
    /// <param name="diagnostics">诊断接收器</param>
    public ValkyrieLexer(ValkyrieLanguage language, DiagnosticSink? diagnostics)
    {
        _language = language;
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列（GreenLeafNode）
    /// </summary>
    /// <param name="source">源代码文本</param>
    /// <returns>词法单元列表</returns>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var tokens = new List<GreenLeafNode>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd()) break;

            var c = Peek();

            if (c is '\n' or '\r')
            {
                AdvanceNewLine();
                continue;
            }

            if (c == '<' && PeekNext() == '#')
            {
                var start = Position;
                Advance(); // <
                Advance(); // #
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.CommentL.ToNodeKind(), 2, "<#"));

                var contentStart = Position;
                while (!IsAtEnd())
                {
                    if (Peek() == '#' && PeekNext() == '>')
                    {
                        break;
                    }
                    Advance();
                }

                if (Position > contentStart)
                {
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.CommentContent.ToNodeKind(), Position - contentStart, Source.Substring(new Range(contentStart, Position))));
                }

                if (!IsAtEnd())
                {
                    Advance(); // #
                    Advance(); // >
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.CommentR.ToNodeKind(), 2, "#>"));
                }
                continue;
            }

            if (c == '#')
            {
                Advance(); // #
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.CommentStart.ToNodeKind(), 1, "#"));

                var contentStart = Position;
                while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
                {
                    Advance();
                }

                if (Position > contentStart)
                {
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.CommentContent.ToNodeKind(), Position - contentStart, Source.Substring(new Range(contentStart, Position))));
                }
                continue;
            }

            if (c == '"')
            {
                var stringText = ScanStringContent();
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.String.ToNodeKind(), stringText.Length + 2, stringText));
                continue;
            }

            if (char.IsDigit(c) || (c == '0' && (PeekNext() == 'x' || PeekNext() == 'X')))
            {
                var numberText = ScanNumberText();
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.Number.ToNodeKind(), numberText.Length, numberText));
                continue;
            }

            if (c == '_' || char.IsLetter(c))
            {
                var idText = ScanIdentifierText();
                var kind = ClassifyIdentifier(idText);

                tokens.Add(new GreenLeafNode(kind, idText.Length, idText));
                continue;
            }

            if (IsOperatorStart(c))
            {
                var opText = ScanOperatorText();
                var kind = opText switch
                {
                    "+" => ValkyrieTokenKind.Plus,
                    "-" => ValkyrieTokenKind.Minus,
                    "*" => ValkyrieTokenKind.Star,
                    "/" => ValkyrieTokenKind.Slash,
                    "%" => ValkyrieTokenKind.Percent,
                    "^" => ValkyrieTokenKind.Power,
                    "=" => ValkyrieTokenKind.Equal,
                    "+=" => ValkyrieTokenKind.PlusEqual,
                    "-=" => ValkyrieTokenKind.MinusEqual,
                    "*=" => ValkyrieTokenKind.StarEqual,
                    "/=" => ValkyrieTokenKind.SlashEqual,
                    "%=" => ValkyrieTokenKind.PercentEqual,
                    "==" => ValkyrieTokenKind.EqualEqual,
                    "!=" => ValkyrieTokenKind.BangEqual,
                    "<" => ValkyrieTokenKind.Less,
                    ">" => ValkyrieTokenKind.Greater,
                    "<=" => ValkyrieTokenKind.LessEqual,
                    ">=" => ValkyrieTokenKind.GreaterEqual,
                    "&&" => ValkyrieTokenKind.AmpAmp,
                    "||" => ValkyrieTokenKind.PipePipe,
                    "!" => ValkyrieTokenKind.Bang,
                    "&" => ValkyrieTokenKind.Amp,
                    "|" => ValkyrieTokenKind.Pipe,
                    "~" => ValkyrieTokenKind.Tilde,
                    "&=" => ValkyrieTokenKind.AmpEqual,
                    "|=" => ValkyrieTokenKind.PipeEqual,
                    "^=" => ValkyrieTokenKind.CaretEqual,
                    "<<" => ValkyrieTokenKind.LessLess,
                    ">>" => ValkyrieTokenKind.GreaterGreater,
                    "<<=" => ValkyrieTokenKind.LessLessEqual,
                    ">>=" => ValkyrieTokenKind.GreaterGreaterEqual,
                    "->" => ValkyrieTokenKind.Arrow,
                    "=>" => ValkyrieTokenKind.FatArrow,
                    "??" => ValkyrieTokenKind.QuestionQuestion,
                    "++" => ValkyrieTokenKind.PlusPlus,
                    "--" => ValkyrieTokenKind.MinusMinus,
                    "?" => ValkyrieTokenKind.Question,
                    "." => ValkyrieTokenKind.Dot,
                    ":" => ValkyrieTokenKind.Colon,
                    "::" => ValkyrieTokenKind.DoubleColon,
                    "," => ValkyrieTokenKind.Comma,
                    "⁅" => ValkyrieTokenKind.OffsetL,
                    "⁆" => ValkyrieTokenKind.OffsetR,
                    "⟨" => ValkyrieTokenKind.GenericL,
                    "⟩" => ValkyrieTokenKind.GenericR,
                    "<%" => ValkyrieTokenKind.TemplateL,
                    "%>" => ValkyrieTokenKind.TemplateR,
                    _ => ValkyrieTokenKind.Error
                };
                tokens.Add(new GreenLeafNode(kind.ToNodeKind(), opText.Length, opText));
                continue;
            }

            if (IsDelimiter(c))
            {
                var kind = c switch
                {
                    '(' => ValkyrieTokenKind.ParenthesisL,
                    ')' => ValkyrieTokenKind.ParenthesisR,
                    '[' => ValkyrieTokenKind.BracketL,
                    ']' => ValkyrieTokenKind.BracketR,
                    '{' => ValkyrieTokenKind.BraceL,
                    '}' => ValkyrieTokenKind.BraceR,
                    ';' => ValkyrieTokenKind.Semicolon,
                    _ => ValkyrieTokenKind.Error
                };
                Advance();
                tokens.Add(new GreenLeafNode(kind.ToNodeKind(), 1, c.ToString()));
                continue;
            }

            Advance();
            Diagnostics?.AddError(
                string.Empty,
                default,
                "VK1001",
                $"意外的字符 '{c}'");
        }

        tokens.Add(new GreenLeafNode(ValkyrieTokenKind.Eos.ToNodeKind(), 0, ""));
        return tokens;
    }

    /// <summary>
    ///     分类标识符为关键词、字面量或标识符
    /// </summary>
    private NodeKind ClassifyIdentifier(string text)
    {
        var kind = text switch
        {
            "namespace" => ValkyrieTokenKind.Namespace,
            "using" => ValkyrieTokenKind.Using,
            "let" => ValkyrieTokenKind.Let,
            "micro" => ValkyrieTokenKind.Micro,
            "mezzo" => ValkyrieTokenKind.Mezzo,
            "macro" => ValkyrieTokenKind.Macro,
            "if" => ValkyrieTokenKind.If,
            "else" => ValkyrieTokenKind.Else,
            "loop" => ValkyrieTokenKind.Loop,
            "while" => ValkyrieTokenKind.While,
            "until" => ValkyrieTokenKind.Until,
            "structure" => ValkyrieTokenKind.Structure,
            "class" => ValkyrieTokenKind.Class,
            "enums" => ValkyrieTokenKind.Enums,
            "flags" => ValkyrieTokenKind.Flags,
            "union" => ValkyrieTokenKind.Union,
            "unite" => ValkyrieTokenKind.Unite,
            "type" => ValkyrieTokenKind.Type,
            "where" => ValkyrieTokenKind.Where,
            "trait" => ValkyrieTokenKind.Trait,
            "match" => ValkyrieTokenKind.Match,
            "case" => ValkyrieTokenKind.Case,
            "end" => ValkyrieTokenKind.End,
            "break" => ValkyrieTokenKind.Break,
            "continue" => ValkyrieTokenKind.Continue,
            "return" => ValkyrieTokenKind.Return,
            "resume" => ValkyrieTokenKind.Resume,
            "catch" => ValkyrieTokenKind.Catch,
            "in" => ValkyrieTokenKind.In,
            "is" => ValkyrieTokenKind.Is,
            "as" => ValkyrieTokenKind.As,

            "true" => ValkyrieTokenKind.True,
            "false" => ValkyrieTokenKind.False,
            "null" => ValkyrieTokenKind.Null,

            "component" when _language.SupportEcsExtension => ValkyrieTokenKind.Component,
            "system" when _language.SupportEcsExtension => ValkyrieTokenKind.System,
            "widget" when _language.SupportUiExtension => ValkyrieTokenKind.Widget,
            "model" when _language.SupportSchemaExtension => ValkyrieTokenKind.Model,
            "service" when _language.SupportSchemaExtension => ValkyrieTokenKind.Service,
            "message" when _language.SupportSchemaExtension => ValkyrieTokenKind.Message,

            "shader" when _language.SupportShaderExtension => ValkyrieTokenKind.Shader,
            "vertex" when _language.SupportShaderExtension => ValkyrieTokenKind.Vertex,
            "fragment" when _language.SupportShaderExtension => ValkyrieTokenKind.Fragment,
            "compute" when _language.SupportShaderExtension => ValkyrieTokenKind.Compute,
            "uniform" when _language.SupportShaderExtension => ValkyrieTokenKind.Uniform,
            "varying" when _language.SupportShaderExtension => ValkyrieTokenKind.Varying,
            "cbuffer" when _language.SupportShaderExtension => ValkyrieTokenKind.CBuffer,
            "texture" when _language.SupportShaderExtension => ValkyrieTokenKind.Texture,
            "sampler" when _language.SupportShaderExtension => ValkyrieTokenKind.Sampler,
            "discard" when _language.SupportShaderExtension => ValkyrieTokenKind.Discard,
            "raygen" when _language.SupportShaderExtension => ValkyrieTokenKind.Raygen,
            "closesthit" when _language.SupportShaderExtension => ValkyrieTokenKind.Closesthit,
            "anyhit" when _language.SupportShaderExtension => ValkyrieTokenKind.Anyhit,
            "miss" when _language.SupportShaderExtension => ValkyrieTokenKind.Miss,
            "constant" when _language.SupportShaderExtension => ValkyrieTokenKind.Constant,
            "binding" when _language.SupportShaderExtension => ValkyrieTokenKind.Binding,

            "neural" => ValkyrieTokenKind.Neural,

            _ => ValkyrieTokenKind.Identifier
        };

        return kind.ToNodeKind();
    }

    /// <summary>
    ///     扫描标识符文本
    /// </summary>
    private string ScanIdentifierText()
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        return sb.ToString();
    }

    /// <summary>
    ///     扫描数字文本
    /// </summary>
    private string ScanNumberText()
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;

        if (Peek() == '0')
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == 'x' || Peek() == 'X'))
            {
                sb.Append(Advance());
                isHex = true;
            }
            else if (!IsAtEnd() && (Peek() == 'b' || Peek() == 'B'))
            {
                sb.Append(Advance());
                isBinary = true;
            }
        }

        if (isHex)
        {
            while (!IsAtEnd() && (char.IsDigit(Peek()) || (Peek() >= 'a' && Peek() <= 'f') || (Peek() >= 'A' && Peek() <= 'F')))
                sb.Append(Advance());
        }
        else if (isBinary)
        {
            while (!IsAtEnd() && (Peek() == '0' || Peek() == '1')) sb.Append(Advance());
        }
        else
        {
            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

            if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
            {
                sb.Append(Advance());
                while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
            }

            if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
            {
                sb.Append(Advance());
                if (!IsAtEnd() && (Peek() == '+' || Peek() == '-')) sb.Append(Advance());
                while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
            }
        }

        while (!IsAtEnd() && (Peek() == 'u' || Peek() == 'U' || Peek() == 'l' || Peek() == 'L' || Peek() == 'f' || Peek() == 'F'))
            sb.Append(Advance());

        return sb.ToString();
    }

    /// <summary>
    ///     扫描字符串内容（不含引号）
    /// </summary>
    private string ScanStringContent()
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;
                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '"' => '"', '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd()) Advance();
        else Diagnostics?.AddError(string.Empty, default, "VK1002", "未闭合的字符串");

        return sb.ToString();
    }

    /// <summary>
    ///     扫描运算符文本
    /// </summary>
    private string ScanOperatorText()
    {
        var start = Position;
        var c = Advance();

        switch (c)
        {
            case '+':
                if (Peek() == '+' || Peek() == '=') Advance();
                break;
            case '-':
                if (Peek() == '-' || Peek() == '=' || Peek() == '>') Advance();
                break;
            case '*':
            case '/':
            case '%':
            case '&':
            case '|':
            case '^':
            case '=':
            case '!':
                if (Peek() == '=') Advance();
                else if (c == '&' && Peek() == '&') Advance();
                else if (c == '|' && Peek() == '|') Advance();
                else if (c == '=' && Peek() == '>') Advance();
                else if (c == '%' && Peek() == '>') Advance();
                break;
            case '<':
                if (Peek() == '<')
                {
                    Advance();
                    if (Peek() == '=') Advance();
                }
                else if (Peek() == '=') Advance();
                else if (Peek() == '%') Advance();
                break;
            case '>':
                if (Peek() == '>')
                {
                    Advance();
                    if (Peek() == '=') Advance();
                }
                else if (Peek() == '=') Advance();
                break;
            case '?':
                if (Peek() == '?') Advance();
                break;
            case ':':
                if (Peek() == ':') Advance();
                break;
        }

        return Source.Substring(new Range(start, Position));
    }

    /// <summary>
    ///     判断字符是否为运算符起始
    /// </summary>
    private static bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' or '?' or ':' or ',' or '.'
                or '⁅' or '⁆' or '⟨' or '⟩' => true,
            _ => false
        };
    }

    /// <summary>
    ///     判断字符是否为分隔符
    /// </summary>
    private static bool IsDelimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ';';
    }

    /// <summary>
    ///     跳过换行符
    /// </summary>
    private void AdvanceNewLine()
    {
        if (Peek() == '\r') Advance();
        if (Peek() == '\n') Advance();
    }
}
