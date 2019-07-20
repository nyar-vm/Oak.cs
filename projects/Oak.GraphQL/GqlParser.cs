using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.GraphQL;

public sealed class GqlParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<GqlToken> _tokens = [];

    private GqlToken Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    public ParseResult<GqlSchema> Parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new GqlLexer();
        _tokens = lexer.Tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var typeDefs = new List<GqlTypeDefinition>();
        var directiveDefs = new List<GqlDirectiveDefinition>();

        while (!IsAtEnd())
        {
            if (Check(GqlTokenType.Type) || Check(GqlTokenType.Input) ||
                Check(GqlTokenType.Interface) || Check(GqlTokenType.Enum) ||
                Check(GqlTokenType.Union) || Check(GqlTokenType.Scalar))
            {
                var typeDef = ParseTypeDefinition();
                if (typeDef is not null) typeDefs.Add(typeDef);
            }
            else if (Check(GqlTokenType.Directive))
            {
                var dirDef = ParseDirectiveDefinition();
                if (dirDef is not null) directiveDefs.Add(dirDef);
            }
            else if (Check(GqlTokenType.Schema))
            {
                Advance();
                Consume(GqlTokenType.LeftBrace, "GQL101", "期望 '{'");
                while (!Check(GqlTokenType.RightBrace) && !IsAtEnd()) Advance();
                Consume(GqlTokenType.RightBrace, "GQL102", "期望 '}'");
            }
            else if (Check(GqlTokenType.Extend))
            {
                Advance();
                if (!IsAtEnd()) Advance();
            }
            else
            {
                Advance();
            }
        }

        var schema = new GqlSchema(typeDefs, directiveDefs);

        if (_diagnostics is not null && _diagnostics.HasErrors)
            return ParseResult<GqlSchema>.Fail(_diagnostics.Errors);

        return ParseResult<GqlSchema>.Ok(schema, _diagnostics?.Messages);
    }

    private GqlTypeDefinition? ParseTypeDefinition()
    {
        var kind = Advance().Text;
        var name = ConsumeName("GQL103", "期望类型名称");

        var implements = new List<string>();
        if (Match(GqlTokenType.Implements))
        {
            implements.Add(ConsumeName("GQL104", "期望接口名称"));
            while (Match(GqlTokenType.Ampersand)) implements.Add(ConsumeName("GQL105", "期望接口名称"));
        }

        var directives = ParseDirectives();

        if (!Check(GqlTokenType.LeftBrace)) return new GqlTypeDefinition(name, kind, [], implements, directives);

        Consume(GqlTokenType.LeftBrace, "GQL106", "期望 '{'");

        var fields = new List<GqlFieldDefinition>();
        while (!Check(GqlTokenType.RightBrace) && !IsAtEnd()) fields.Add(ParseFieldDefinition());

        Consume(GqlTokenType.RightBrace, "GQL107", "期望 '}'");

        return new GqlTypeDefinition(name, kind, fields, implements, directives);
    }

    private GqlFieldDefinition ParseFieldDefinition()
    {
        var name = ConsumeName("GQL108", "期望字段名称");

        var arguments = new List<GqlInputValueDefinition>();
        if (Match(GqlTokenType.LeftParen))
        {
            arguments.Add(ParseInputValueDefinition());
            while (Match(GqlTokenType.Comma)) arguments.Add(ParseInputValueDefinition());
            Consume(GqlTokenType.RightParen, "GQL109", "期望 ')'");
        }

        Consume(GqlTokenType.Colon, "GQL110", "期望 ':'");
        var type = ParseTypeRef();
        var directives = ParseDirectives();

        return new GqlFieldDefinition(name, arguments, type, directives);
    }

    private GqlInputValueDefinition ParseInputValueDefinition()
    {
        var name = ConsumeName("GQL111", "期望参数名称");
        Consume(GqlTokenType.Colon, "GQL112", "期望 ':'");
        var type = ParseTypeRef();

        GqlValue? defaultValue = null;
        if (Match(GqlTokenType.Equals)) defaultValue = ParseValue();

        var directives = ParseDirectives();

        return new GqlInputValueDefinition(name, type, defaultValue, directives);
    }

    private GqlDirectiveDefinition? ParseDirectiveDefinition()
    {
        Consume(GqlTokenType.Directive, "GQL113", "期望 'directive'");
        Consume(GqlTokenType.At, "GQL114", "期望 '@'");
        var name = ConsumeName("GQL115", "期望指令名称");

        var arguments = new List<GqlInputValueDefinition>();
        if (Match(GqlTokenType.LeftParen))
        {
            arguments.Add(ParseInputValueDefinition());
            while (Match(GqlTokenType.Comma)) arguments.Add(ParseInputValueDefinition());
            Consume(GqlTokenType.RightParen, "GQL116", "期望 ')'");
        }

        var repeatable = Match(GqlTokenType.Repeatable);

        Consume(GqlTokenType.On, "GQL117", "期望 'on'");

        var locations = new List<GqlDirectiveLocation> { ParseDirectiveLocation() };
        while (Match(GqlTokenType.Pipe)) locations.Add(ParseDirectiveLocation());

        return new GqlDirectiveDefinition(name, arguments, locations, repeatable);
    }

    private GqlDirectiveLocation ParseDirectiveLocation()
    {
        var name = Advance().Text;
        return name.ToUpperInvariant() switch
        {
            "QUERY" => GqlDirectiveLocation.Query,
            "MUTATION" => GqlDirectiveLocation.Mutation,
            "SUBSCRIPTION" => GqlDirectiveLocation.Subscription,
            "FIELD" => GqlDirectiveLocation.Field,
            "FRAGMENT_DEFINITION" => GqlDirectiveLocation.FragmentDefinition,
            "FRAGMENT_SPREAD" => GqlDirectiveLocation.FragmentSpread,
            "INLINE_FRAGMENT" => GqlDirectiveLocation.InlineFragment,
            "VARIABLE_DEFINITION" => GqlDirectiveLocation.VariableDefinition,
            "SCHEMA" => GqlDirectiveLocation.Schema,
            "SCALAR" => GqlDirectiveLocation.Scalar,
            "OBJECT" => GqlDirectiveLocation.Object,
            "FIELD_DEFINITION" => GqlDirectiveLocation.FieldDefinition,
            "ARGUMENT_DEFINITION" => GqlDirectiveLocation.ArgumentDefinition,
            "INTERFACE" => GqlDirectiveLocation.Interface,
            "UNION" => GqlDirectiveLocation.Union,
            "ENUM" => GqlDirectiveLocation.Enum,
            "ENUM_VALUE" => GqlDirectiveLocation.EnumValue,
            "INPUT_OBJECT" => GqlDirectiveLocation.InputObject,
            "INPUT_FIELD_DEFINITION" => GqlDirectiveLocation.InputFieldDefinition,
            _ => GqlDirectiveLocation.Field
        };
    }

    private GqlTypeRef ParseTypeRef()
    {
        GqlTypeRef inner;

        if (Match(GqlTokenType.LeftBracket))
        {
            var elementType = ParseTypeRef();
            Consume(GqlTokenType.RightBracket, "GQL118", "期望 ']'");
            inner = new GqlListType(elementType);
        }
        else
        {
            var name = ConsumeName("GQL119", "期望类型名称");
            inner = new GqlNamedType(name);
        }

        if (Match(GqlTokenType.Exclamation)) return new GqlNonNullType(inner);

        return inner;
    }

    private IReadOnlyList<GqlDirective> ParseDirectives()
    {
        var directives = new List<GqlDirective>();

        while (Check(GqlTokenType.At))
        {
            Advance();
            var name = ConsumeName("GQL120", "期望指令名称");

            var args = new List<(string Name, GqlValue Value)>();
            if (Match(GqlTokenType.LeftParen))
            {
                args.Add(ParseArgument());
                while (Match(GqlTokenType.Comma)) args.Add(ParseArgument());
                Consume(GqlTokenType.RightParen, "GQL121", "期望 ')'");
            }

            directives.Add(new GqlDirective(name, args));
        }

        return directives;
    }

    private (string Name, GqlValue Value) ParseArgument()
    {
        var name = ConsumeName("GQL122", "期望参数名称");
        Consume(GqlTokenType.Colon, "GQL123", "期望 ':'");
        var value = ParseValue();
        return (name, value);
    }

    private GqlValue ParseValue()
    {
        if (Check(GqlTokenType.IntValue)) return new GqlIntValue(Advance().Text);
        if (Check(GqlTokenType.FloatValue)) return new GqlFloatValue(Advance().Text);
        if (Check(GqlTokenType.StringValue)) return new GqlStringValue(Advance().Text);
        if (Check(GqlTokenType.True)) { Advance(); return new GqlBooleanValue(true); }
        if (Check(GqlTokenType.False)) { Advance(); return new GqlBooleanValue(false); }
        if (Check(GqlTokenType.Null)) { Advance(); return GqlNullValue.Instance; }

        if (Check(GqlTokenType.Name)) return new GqlEnumValue(Advance().Text);

        if (Check(GqlTokenType.LeftBracket))
        {
            Advance();
            var values = new List<GqlValue>();
            if (!Check(GqlTokenType.RightBracket))
            {
                values.Add(ParseValue());
                while (Match(GqlTokenType.Comma)) values.Add(ParseValue());
            }

            Consume(GqlTokenType.RightBracket, "GQL124", "期望 ']'");
            return new GqlListValue(values);
        }

        if (Check(GqlTokenType.LeftBrace))
        {
            Advance();
            var fields = new List<(string Name, GqlValue Value)>();
            if (!Check(GqlTokenType.RightBrace))
            {
                fields.Add(ParseObjectField());
                while (Match(GqlTokenType.Comma)) fields.Add(ParseObjectField());
            }

            Consume(GqlTokenType.RightBrace, "GQL125", "期望 '}'");
            return new GqlObjectValue(fields);
        }

        _diagnostics?.AddError(string.Empty, default,
            "GQL126", $"意外的词法单元：{Current.Type}");
        Advance();
        return GqlNullValue.Instance;
    }

    private (string Name, GqlValue Value) ParseObjectField()
    {
        var name = ConsumeName("GQL127", "期望字段名称");
        Consume(GqlTokenType.Colon, "GQL128", "期望 ':'");
        var value = ParseValue();
        return (name, value);
    }

    private bool IsAtEnd()
    {
        return Current.Type == GqlTokenType.EndOfFile;
    }

    private bool Check(GqlTokenType type)
    {
        return Current.Type == type;
    }

    private bool Match(GqlTokenType type)
    {
        if (Current.Type != type) return false;
        Advance();
        return true;
    }

    private GqlToken Advance()
    {
        var token = Current;
        if (_position < _tokens.Count - 1) _position++;
        return token;
    }

    private GqlToken Consume(GqlTokenType type, string errorCode, string message)
    {
        if (Current.Type == type) return Advance();

        _diagnostics?.AddError(string.Empty, default,
            errorCode, $"{message}，实际遇到 {Current.Type}");
        return Current;
    }

    private string ConsumeName(string errorCode, string message)
    {
        if (Current.Type == GqlTokenType.Name) return Advance().Text;

        _diagnostics?.AddError(string.Empty, default,
            errorCode, $"{message}，实际遇到 {Current.Type}");
        return Advance().Text;
    }
}
