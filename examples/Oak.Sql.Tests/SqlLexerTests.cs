namespace Oak.Sql.Tests;

public class SqlLexerTests : SqlTestBase
{
    [Fact]
    public void Tokenize_SelectKeywords_ShouldReturnKeywordTokens()
    {
        var tokens = TokenizeWithTimeout("SELECT FROM WHERE");

        Assert.Equal(4, tokens.Count);
        Assert.Equal(SqlTokenType.Select, tokens[0].Type);
        Assert.Equal(SqlTokenType.From, tokens[1].Type);
        Assert.Equal(SqlTokenType.Where, tokens[2].Type);
        Assert.Equal(SqlTokenType.EndOfFile, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_Identifier_ShouldReturnIdentifierToken()
    {
        var tokens = TokenizeWithTimeout("users");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.Identifier, tokens[0].Type);
        Assert.Equal("users", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Number_ShouldReturnNumberToken()
    {
        var tokens = TokenizeWithTimeout("42");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.Number, tokens[0].Type);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_String_ShouldReturnStringToken()
    {
        var tokens = TokenizeWithTimeout("'hello'");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.String, tokens[0].Type);
        Assert.Equal("hello", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Operators_ShouldReturnOperatorTokens()
    {
        var tokens = TokenizeWithTimeout("= <> < > <= >=");

        Assert.Equal(7, tokens.Count);
        Assert.Equal(SqlTokenType.Equal, tokens[0].Type);
        Assert.Equal(SqlTokenType.NotEqual, tokens[1].Type);
        Assert.Equal(SqlTokenType.LessThan, tokens[2].Type);
        Assert.Equal(SqlTokenType.GreaterThan, tokens[3].Type);
        Assert.Equal(SqlTokenType.LessEqual, tokens[4].Type);
        Assert.Equal(SqlTokenType.GreaterEqual, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_QuotedIdentifier_ShouldReturnIdentifierToken()
    {
        var tokens = TokenizeWithTimeout("\"my table\"");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.Identifier, tokens[0].Type);
        Assert.Equal("my table", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_LineComment_ShouldBeSkipped()
    {
        var tokens = TokenizeWithTimeout("-- comment\nSELECT");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.Select, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_FloatNumber_ShouldReturnNumberToken()
    {
        var tokens = TokenizeWithTimeout("3.14");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.Number, tokens[0].Type);
        Assert.Equal("3.14", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_ScientificNotation_ShouldReturnNumberToken()
    {
        var tokens = TokenizeWithTimeout("1.5e-3");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(SqlTokenType.Number, tokens[0].Type);
    }
}
