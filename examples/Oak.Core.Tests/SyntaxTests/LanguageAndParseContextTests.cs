using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public sealed class TestLanguage : Language
{
    public override string Name => "Test";
    public bool FeatureEnabled { get; init; }
}

public sealed class TestContext : ISyntaxContext
{
}

public class LanguageAndParseContextTests
{
    private const int TimeoutMs = 5000;

    private static ParseContext<TestLanguage, TestContext> CreateContext(string text)
    {
        return new ParseContext<TestLanguage, TestContext>(
            new StringSource(text), new TestLanguage(), new TestContext(), new DiagnosticSink());
    }

    [Fact]
    public void Language_Name()
    {
        var lang = new TestLanguage();
        Assert.Equal("Test", lang.Name);
    }

    [Fact]
    public void Language_ToString()
    {
        var lang = new TestLanguage();
        Assert.Equal("Test", lang.ToString());
    }

    [Fact]
    public void ParseContext_BasicOperations()
    {
        var parseCtx = CreateContext("abc");
        Assert.Equal(0, parseCtx.Position);
        Assert.Equal('a', parseCtx.Current);
        parseCtx.Advance();
        Assert.Equal(1, parseCtx.Position);
        Assert.Equal('b', parseCtx.Current);
    }

    [Fact]
    public void ParseContext_Peek()
    {
        var parseCtx = CreateContext("abc");
        Assert.Equal('a', parseCtx.Peek(0));
        Assert.Equal('b', parseCtx.Peek(1));
        Assert.Equal('c', parseCtx.Peek(2));
        Assert.Equal('\0', parseCtx.Peek(3));
    }

    [Fact]
    public void ParseContext_GetSpanFrom()
    {
        var parseCtx = CreateContext("abc");
        parseCtx.Advance();
        parseCtx.Advance();
        var span = parseCtx.GetSpanFrom(0);
        Assert.Equal(default(TextSpan), span);
    }

    [Fact]
    public void ParseContext_GetText()
    {
        var parseCtx = CreateContext("abc");
        var text = parseCtx.GetText(default(TextSpan));
        Assert.Equal("ab", text);
    }

    [Fact]
    public void ParseContext_SkipToChar()
    {
        var parseCtx = CreateContext("abc;def");
        parseCtx.SkipTo(';');
        Assert.Equal(3, parseCtx.Position);
        Assert.Equal(';', parseCtx.Current);
    }

    [Fact]
    public void ParseContext_SkipToChar_NotFound()
    {
        var parseCtx = CreateContext("abcdef");
        parseCtx.SkipTo(';');
        Assert.Equal(6, parseCtx.Position);
    }

    [Fact]
    public void ParseContext_SkipToPredicate()
    {
        var parseCtx = CreateContext("abc1def");
        parseCtx.SkipTo(char.IsDigit);
        Assert.Equal(3, parseCtx.Position);
        Assert.Equal('1', parseCtx.Current);
    }

    [Fact]
    public void ParseContext_SkipToPredicate_NotFound()
    {
        var parseCtx = CreateContext("abcdef");
        parseCtx.SkipTo(char.IsDigit);
        Assert.Equal(6, parseCtx.Position);
    }

    [Fact]
    public void ParseContext_ExpectChar_Success()
    {
        var parseCtx = CreateContext("abc");
        var result = parseCtx.Expect('a', "TEST001", "expected 'a'");
        Assert.True(result);
        Assert.Empty(parseCtx.Diagnostics.Messages);
    }

    [Fact]
    public void ParseContext_ExpectChar_Failure()
    {
        var parseCtx = CreateContext("abc");
        var result = parseCtx.Expect('x', "TEST001", "expected 'x'");
        Assert.False(result);
        Assert.Single(parseCtx.Diagnostics.Messages);
        Assert.Equal(DiagnosticLevel.Error, parseCtx.Diagnostics.Messages[0].Level);
    }

    [Fact]
    public void ParseContext_ExpectPredicate_Success()
    {
        var parseCtx = CreateContext("abc");
        var result = parseCtx.Expect(char.IsLetter, "TEST002", "expected letter");
        Assert.True(result);
        Assert.Empty(parseCtx.Diagnostics.Messages);
    }

    [Fact]
    public void ParseContext_ExpectPredicate_Failure()
    {
        var parseCtx = CreateContext("123");
        var result = parseCtx.Expect(char.IsLetter, "TEST002", "expected letter");
        Assert.False(result);
        Assert.Single(parseCtx.Diagnostics.Messages);
    }

    [Fact]
    public void ParseContext_Recover_Success()
    {
        var parseCtx = CreateContext("abc;def");
        parseCtx.Recover((ref ParseContext<TestLanguage, TestContext> p) =>
        {
            p.Advance();
            p.Advance();
        }, "should not fail");
        Assert.Empty(parseCtx.Diagnostics.Errors);
    }

    [Fact]
    public void ParseContext_Recover_Failure()
    {
        var parseCtx = CreateContext("abc;def");
        parseCtx.Recover((ref ParseContext<TestLanguage, TestContext> p) =>
        {
            p.Diagnostics.AddError("", default(TextSpan), "TEST_ERR", "forced error");
        }, "recovery test");
        Assert.True(parseCtx.Diagnostics.Errors.Count >= 2);
    }
}
