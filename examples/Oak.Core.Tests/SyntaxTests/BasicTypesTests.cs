using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

/// <summary>
/// TextSpan、Edit、NodeKind 基础类型测试
/// </summary>
public class BasicTypesTests
{
    [Fact]
    public void TextSpan_Properties()
    {
        var span = default(TextSpan);
        Assert.Equal(10, span.Start);
        Assert.Equal(5, span.Length);
        Assert.Equal(15, span.End);
    }

    [Fact]
    public void TextSpan_Contains()
    {
        var span = default(TextSpan);
        Assert.True(span.Contains(10));
        Assert.True(span.Contains(12));
        Assert.True(span.Contains(14));
        Assert.False(span.Contains(9));
        Assert.False(span.Contains(15));
    }

    [Fact]
    public void TextSpan_OverlapsWith()
    {
        var span1 = default(TextSpan);
        var span2 = default(TextSpan);
        var span3 = default(TextSpan);
        Assert.True(span1.OverlapsWith(span2));
        Assert.False(span1.OverlapsWith(span3));
    }

    [Fact]
    public void TextSpan_Equality()
    {
        var span1 = default(TextSpan);
        var span2 = default(TextSpan);
        var span3 = default(TextSpan);
        Assert.Equal(span1, span2);
        Assert.NotEqual(span1, span3);
    }

    [Fact]
    public void Edit_Properties()
    {
        var edit = new Edit(default(TextSpan), "abc");
        Assert.Equal(default(TextSpan), edit.OldSpan);
        Assert.Equal("abc", edit.NewText);
        Assert.Equal(0, edit.Delta);
    }

    [Fact]
    public void Edit_Delta_Positive()
    {
        var edit = new Edit(default(TextSpan), "abcd");
        Assert.Equal(2, edit.Delta);
    }

    [Fact]
    public void Edit_Delta_Negative()
    {
        var edit = new Edit(default(TextSpan), "ab");
        Assert.Equal(-3, edit.Delta);
    }

    [Fact]
    public void NodeKind_ImplicitConversion()
    {
        NodeKind kind = 42;
        Assert.Equal(42, kind.Value);
        int value = kind;
        Assert.Equal(42, value);
    }

    [Fact]
    public void NodeKind_Equality()
    {
        var kind1 = new NodeKind(1);
        var kind2 = new NodeKind(1);
        var kind3 = new NodeKind(2);
        Assert.Equal(kind1, kind2);
        Assert.NotEqual(kind1, kind3);
    }
}
