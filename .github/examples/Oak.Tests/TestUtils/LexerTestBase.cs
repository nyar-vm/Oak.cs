using Oak.Diagnostics;
using Oak.Lexing;

namespace Oak.Tests.TestUtils;

/// <summary>
///     Lexer 测试基类，提供词法分析器测试的通用工具方法
/// </summary>
/// <typeparam name="TToken">词法单元类型</typeparam>
public abstract class LexerTestBase<TToken> where TToken : IToken
{
    /// <summary>
    ///     默认超时时间（毫秒），防止词法分析器死循环
    /// </summary>
    protected virtual int TimeoutMs => 5000;

    /// <summary>
    ///     执行词法分析并应用超时保护
    /// </summary>
    protected IReadOnlyList<TToken> TokenizeWithTimeout(ILexer<TToken> lexer, string source)
    {
        var tokens = new List<TToken>();
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                var result = lexer.Tokenize(source);
                tokens.AddRange(result);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.Start();

        if (!thread.Join(TimeoutMs))
        {
            thread.Interrupt();
            thread.Join(100);
            throw new TimeoutException($"词法分析器在 {TimeoutMs}ms 内未完成，可能存在死循环");
        }

        if (exception is not null) throw exception;

        return tokens;
    }

    /// <summary>
    ///     断言词法单元数量
    /// </summary>
    protected static void AssertTokenCount(IReadOnlyList<TToken> tokens, int expected)
    {
        Assert.Equal(expected, tokens.Count);
    }

    /// <summary>
    ///     断言指定位置的词法单元类型
    /// </summary>
    protected static void AssertTokenType(IReadOnlyList<TToken> tokens, int index, string expectedType)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedType, tokens[index].TokenType);
    }

    /// <summary>
    ///     断言指定位置的词法单元值
    /// </summary>
    protected static void AssertTokenValue(IReadOnlyList<TToken> tokens, int index, string expectedValue)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedValue, tokens[index].Value);
    }

    /// <summary>
    ///     断言指定位置的词法单元位置
    /// </summary>
    protected static void AssertTokenPosition(IReadOnlyList<TToken> tokens, int index, int expectedLine,
        int expectedColumn)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedLine, tokens[index].Line);
        Assert.Equal(expectedColumn, tokens[index].Column);
    }

    /// <summary>
    ///     断言没有诊断错误
    /// </summary>
    protected static void AssertNoErrors(DiagnosticSink diagnostics)
    {
        Assert.False(diagnostics.HasErrors, $"词法分析产生了意外的错误：\n{diagnostics.FormatAll()}");
    }

    /// <summary>
    ///     断言存在指定数量的错误
    /// </summary>
    protected static void AssertErrorCount(DiagnosticSink diagnostics, int expectedCount)
    {
        Assert.Equal(expectedCount, diagnostics.Errors.Count);
    }

    /// <summary>
    ///     断言最后一个词法单元是 EOF
    /// </summary>
    protected static void AssertEndsWithEof(IReadOnlyList<TToken> tokens)
    {
        Assert.NotEmpty(tokens);
        Assert.Equal("Eof", tokens[^1].TokenType);
    }
}