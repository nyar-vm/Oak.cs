using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.Protobuf;

public sealed class ProtoParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<ProtoToken> _tokens = [];

    private ProtoToken Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    public ParseResult<ProtoFile> Parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new ProtoLexer();
        _tokens = lexer.Tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        string? syntax = null;
        string? package = null;
        var imports = new List<string>();
        var options = new List<ProtoOption>();
        var messages = new List<ProtoMessage>();
        var enums = new List<ProtoEnum>();
        var services = new List<ProtoService>();

        while (!IsAtEnd())
        {
            if (Check(ProtoTokenType.Syntax)) syntax = ParseSyntax();
            else if (Check(ProtoTokenType.Package)) package = ParsePackage();
            else if (Check(ProtoTokenType.Import)) imports.Add(ParseImport());
            else if (Check(ProtoTokenType.Option)) options.Add(ParseOption());
            else if (Check(ProtoTokenType.Message)) messages.Add(ParseMessage());
            else if (Check(ProtoTokenType.Enum)) enums.Add(ParseEnum());
            else if (Check(ProtoTokenType.Service)) services.Add(ParseService());
            else Advance();
        }

        var file = new ProtoFile(syntax, package, imports, options, messages, enums, services);

        if (_diagnostics is not null && _diagnostics.HasErrors)
            return ParseResult<ProtoFile>.Fail(_diagnostics.Errors);

        return ParseResult<ProtoFile>.Ok(file, _diagnostics?.Messages);
    }

    private string ParseSyntax()
    {
        Consume(ProtoTokenType.Syntax, "PROTO101", "期望 'syntax'");
        Consume(ProtoTokenType.Equals, "PROTO102", "期望 '='");
        var value = Consume(ProtoTokenType.StringLiteral, "PROTO103", "期望字符串").Text;
        Consume(ProtoTokenType.Semicolon, "PROTO104", "期望 ';'");
        return value;
    }

    private string ParsePackage()
    {
        Consume(ProtoTokenType.Package, "PROTO105", "期望 'package'");
        var name = ParseFullName();
        Consume(ProtoTokenType.Semicolon, "PROTO106", "期望 ';'");
        return name;
    }

    private string ParseImport()
    {
        Consume(ProtoTokenType.Import, "PROTO107", "期望 'import'");
        if (Check(ProtoTokenType.Identifier) && Current.Text is "public" or "weak") Advance();
        var path = Consume(ProtoTokenType.StringLiteral, "PROTO108", "期望字符串").Text;
        Consume(ProtoTokenType.Semicolon, "PROTO109", "期望 ';'");
        return path;
    }

    private ProtoOption ParseOption()
    {
        Consume(ProtoTokenType.Option, "PROTO110", "期望 'option'");
        var name = ParseFullName();
        Consume(ProtoTokenType.Equals, "PROTO111", "期望 '='");
        var value = ParseConstantValue();
        Consume(ProtoTokenType.Semicolon, "PROTO112", "期望 ';'");
        return new ProtoOption(name, value);
    }

    private ProtoMessage ParseMessage()
    {
        Consume(ProtoTokenType.Message, "PROTO113", "期望 'message'");
        var name = Consume(ProtoTokenType.Identifier, "PROTO114", "期望消息名称").Text;
        Consume(ProtoTokenType.LeftBrace, "PROTO115", "期望 '{'");

        var fields = new List<ProtoField>();
        var nestedMessages = new List<ProtoMessage>();
        var nestedEnums = new List<ProtoEnum>();
        var oneofs = new List<ProtoOneof>();
        var mapFields = new List<ProtoMapField>();
        var reserved = new List<ProtoReserved>();
        var options = new List<ProtoOption>();

        while (!Check(ProtoTokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(ProtoTokenType.Message)) nestedMessages.Add(ParseMessage());
            else if (Check(ProtoTokenType.Enum)) nestedEnums.Add(ParseEnum());
            else if (Check(ProtoTokenType.Oneof)) oneofs.Add(ParseOneof());
            else if (Check(ProtoTokenType.Map)) mapFields.Add(ParseMapField());
            else if (Check(ProtoTokenType.Reserved)) reserved.Add(ParseReserved());
            else if (Check(ProtoTokenType.Option)) options.Add(ParseOption());
            else if (Check(ProtoTokenType.Repeated) || Check(ProtoTokenType.Optional) || Check(ProtoTokenType.Required))
                fields.Add(ParseField());
            else if (Check(ProtoTokenType.Identifier) || Check(ProtoTokenType.Dot))
                fields.Add(ParseFieldNoLabel());
            else Advance();
        }

        Consume(ProtoTokenType.RightBrace, "PROTO116", "期望 '}'");

        return new ProtoMessage(name, fields, nestedMessages, nestedEnums, oneofs, mapFields, reserved, options);
    }

    private ProtoField ParseField()
    {
        var label = Advance().Text;
        var type = ParseTypeName();
        var name = Consume(ProtoTokenType.Identifier, "PROTO117", "期望字段名称").Text;
        Consume(ProtoTokenType.Equals, "PROTO118", "期望 '='");
        var number = ParseFieldNumber();
        var options = ParseFieldOptions();
        Consume(ProtoTokenType.Semicolon, "PROTO119", "期望 ';'");
        return new ProtoField(label, type, name, number, options);
    }

    private ProtoField ParseFieldNoLabel()
    {
        var type = ParseTypeName();
        var name = Consume(ProtoTokenType.Identifier, "PROTO120", "期望字段名称").Text;
        Consume(ProtoTokenType.Equals, "PROTO121", "期望 '='");
        var number = ParseFieldNumber();
        var options = ParseFieldOptions();
        Consume(ProtoTokenType.Semicolon, "PROTO122", "期望 ';'");
        return new ProtoField("", type, name, number, options);
    }

    private ProtoMapField ParseMapField()
    {
        Consume(ProtoTokenType.Map, "PROTO123", "期望 'map'");
        Consume(ProtoTokenType.Lt, "PROTO124", "期望 '<'");
        var keyType = ParseTypeName();
        Consume(ProtoTokenType.Comma, "PROTO125", "期望 ','");
        var valueType = ParseTypeName();
        Consume(ProtoTokenType.Gt, "PROTO126", "期望 '>'");
        var name = Consume(ProtoTokenType.Identifier, "PROTO127", "期望字段名称").Text;
        Consume(ProtoTokenType.Equals, "PROTO128", "期望 '='");
        var number = ParseFieldNumber();
        Consume(ProtoTokenType.Semicolon, "PROTO129", "期望 ';'");
        return new ProtoMapField(keyType, valueType, name, number);
    }

    private ProtoOneof ParseOneof()
    {
        Consume(ProtoTokenType.Oneof, "PROTO130", "期望 'oneof'");
        var name = Consume(ProtoTokenType.Identifier, "PROTO131", "期望名称").Text;
        Consume(ProtoTokenType.LeftBrace, "PROTO132", "期望 '{'");

        var fields = new List<ProtoField>();
        while (!Check(ProtoTokenType.RightBrace) && !IsAtEnd())
        {
            var type = ParseTypeName();
            var fieldName = Consume(ProtoTokenType.Identifier, "PROTO133", "期望字段名称").Text;
            Consume(ProtoTokenType.Equals, "PROTO134", "期望 '='");
            var number = ParseFieldNumber();
            Consume(ProtoTokenType.Semicolon, "PROTO135", "期望 ';'");
            fields.Add(new ProtoField("", type, fieldName, number, []));
        }

        Consume(ProtoTokenType.RightBrace, "PROTO136", "期望 '}'");
        return new ProtoOneof(name, fields);
    }

    private ProtoReserved ParseReserved()
    {
        Consume(ProtoTokenType.Reserved, "PROTO137", "期望 'reserved'");

        var names = new List<string>();
        var ranges = new List<(int Start, int End)>();

        if (Check(ProtoTokenType.StringLiteral))
        {
            names.Add(Advance().Text);
            while (Match(ProtoTokenType.Comma))
            {
                if (Check(ProtoTokenType.StringLiteral)) names.Add(Advance().Text);
                else break;
            }
        }
        else
        {
            ranges.Add(ParseRange());
            while (Match(ProtoTokenType.Comma)) ranges.Add(ParseRange());
        }

        Consume(ProtoTokenType.Semicolon, "PROTO138", "期望 ';'");
        return new ProtoReserved(names, ranges);
    }

    private (int Start, int End) ParseRange()
    {
        var start = ParseFieldNumber();
        if (Match(ProtoTokenType.Identifier) && Current.Text == "to")
        {
            Advance();
            var end = ParseFieldNumber();
            return (start, end);
        }

        return (start, start);
    }

    private ProtoEnum ParseEnum()
    {
        Consume(ProtoTokenType.Enum, "PROTO139", "期望 'enum'");
        var name = Consume(ProtoTokenType.Identifier, "PROTO140", "期望枚举名称").Text;
        Consume(ProtoTokenType.LeftBrace, "PROTO141", "期望 '{'");

        var values = new List<ProtoEnumValue>();
        while (!Check(ProtoTokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(ProtoTokenType.Option)) { ParseOption(); continue; }
            if (Check(ProtoTokenType.Reserved)) { ParseReserved(); continue; }

            var valueName = Consume(ProtoTokenType.Identifier, "PROTO142", "期望枚举值名称").Text;
            Consume(ProtoTokenType.Equals, "PROTO143", "期望 '='");
            var number = ParseFieldNumber();
            Consume(ProtoTokenType.Semicolon, "PROTO144", "期望 ';'");
            values.Add(new ProtoEnumValue(valueName, number));
        }

        Consume(ProtoTokenType.RightBrace, "PROTO145", "期望 '}'");
        return new ProtoEnum(name, values);
    }

    private ProtoService ParseService()
    {
        Consume(ProtoTokenType.Service, "PROTO146", "期望 'service'");
        var name = Consume(ProtoTokenType.Identifier, "PROTO147", "期望服务名称").Text;
        Consume(ProtoTokenType.LeftBrace, "PROTO148", "期望 '{'");

        var methods = new List<ProtoRpc>();
        while (!Check(ProtoTokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(ProtoTokenType.Option)) { ParseOption(); continue; }
            if (Check(ProtoTokenType.Rpc)) methods.Add(ParseRpc());
            else Advance();
        }

        Consume(ProtoTokenType.RightBrace, "PROTO149", "期望 '}'");
        return new ProtoService(name, methods);
    }

    private ProtoRpc ParseRpc()
    {
        Consume(ProtoTokenType.Rpc, "PROTO150", "期望 'rpc'");
        var name = Consume(ProtoTokenType.Identifier, "PROTO151", "期望方法名称").Text;
        Consume(ProtoTokenType.LeftParen, "PROTO152", "期望 '('");

        var inputStream = Match(ProtoTokenType.Stream);
        var inputType = ParseTypeName();
        Consume(ProtoTokenType.RightParen, "PROTO153", "期望 ')'");

        Consume(ProtoTokenType.Returns, "PROTO154", "期望 'returns'");
        Consume(ProtoTokenType.LeftParen, "PROTO155", "期望 '('");

        var outputStream = Match(ProtoTokenType.Stream);
        var outputType = ParseTypeName();
        Consume(ProtoTokenType.RightParen, "PROTO156", "期望 ')'");

        if (Match(ProtoTokenType.Semicolon)) { }
        else if (Match(ProtoTokenType.LeftBrace))
        {
            while (!Check(ProtoTokenType.RightBrace) && !IsAtEnd()) Advance();
            Consume(ProtoTokenType.RightBrace, "PROTO157", "期望 '}'");
        }

        return new ProtoRpc(name, inputType, inputStream, outputType, outputStream);
    }

    private string ParseTypeName()
    {
        return ParseFullName();
    }

    private string ParseFullName()
    {
        var sb = new System.Text.StringBuilder();

        while (Match(ProtoTokenType.Dot)) sb.Append('.');

        sb.Append(Consume(ProtoTokenType.Identifier, "PROTO158", "期望标识符").Text);

        while (Check(ProtoTokenType.Dot))
        {
            sb.Append(Advance().Text);
            sb.Append(Consume(ProtoTokenType.Identifier, "PROTO159", "期望标识符").Text);
        }

        return sb.ToString();
    }

    private int ParseFieldNumber()
    {
        var token = Consume(ProtoTokenType.IntLiteral, "PROTO160", "期望字段编号");
        return int.TryParse(token.Text, out var value) ? value : 0;
    }

    private IReadOnlyList<ProtoOption> ParseFieldOptions()
    {
        var options = new List<ProtoOption>();

        if (!Match(ProtoTokenType.LeftBracket)) return options;

        options.Add(ParseFieldOption());
        while (Match(ProtoTokenType.Comma)) options.Add(ParseFieldOption());

        Consume(ProtoTokenType.RightBracket, "PROTO161", "期望 ']'");
        return options;
    }

    private ProtoOption ParseFieldOption()
    {
        var name = ParseFullName();
        Consume(ProtoTokenType.Equals, "PROTO162", "期望 '='");
        var value = ParseConstantValue();
        return new ProtoOption(name, value);
    }

    private string ParseConstantValue()
    {
        if (Check(ProtoTokenType.StringLiteral)) return $"\"{Advance().Text}\"";
        if (Check(ProtoTokenType.IntLiteral)) return Advance().Text;
        if (Check(ProtoTokenType.FloatLiteral)) return Advance().Text;
        if (Check(ProtoTokenType.BoolLiteral)) return Advance().Text;
        if (Check(ProtoTokenType.Identifier)) return ParseFullName();

        _diagnostics?.AddError(string.Empty, default,
            "PROTO163", $"期望常量值，实际遇到 {Current.Type}");
        return Advance().Text;
    }

    private bool IsAtEnd()
    {
        return Current.Type == ProtoTokenType.EndOfFile;
    }

    private bool Check(ProtoTokenType type)
    {
        return Current.Type == type;
    }

    private bool Match(ProtoTokenType type)
    {
        if (Current.Type != type) return false;
        Advance();
        return true;
    }

    private ProtoToken Advance()
    {
        var token = Current;
        if (_position < _tokens.Count - 1) _position++;
        return token;
    }

    private ProtoToken Consume(ProtoTokenType type, string errorCode, string message)
    {
        if (Current.Type == type) return Advance();

        _diagnostics?.AddError(string.Empty, default,
            errorCode, $"{message}，实际遇到 {Current.Type}");
        return Current;
    }
}
