using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

/// <summary>
/// CstBuilder 具体语法树构建器测试
/// </summary>
public class CstBuilderTests
{
    [Fact]
    public void CstBuilder_SingleLeaf()
    {
        var b = new CstBuilder();
        b.AddToken(1, "hello");
        var node = b.Build();
        Assert.Equal(new NodeKind(1), node.Kind);
        Assert.Equal(5, node.Width);
        Assert.True(node.IsLeaf);
    }

    [Fact]
    public void CstBuilder_SingleInternalNode()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "abc");
        b.AddToken(3, "de");
        b.EndNode();
        var node = b.Build();
        Assert.Equal(new NodeKind(1), node.Kind);
        Assert.Equal(5, node.Width);
        Assert.Equal(2, node.ChildCount);
    }

    [Fact]
    public void CstBuilder_NestedNodes()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.StartNode(2);
        b.AddToken(3, "x");
        b.EndNode();
        b.AddToken(4, "y");
        b.EndNode();
        var node = b.Build();
        Assert.Equal(new NodeKind(1), node.Kind);
        Assert.Equal(2, node.Width);
        Assert.Equal(2, node.ChildCount);
        var inner = node.GetChild(0)!;
        Assert.Equal(new NodeKind(2), inner.Kind);
        Assert.Equal(1, inner.Width);
    }

    [Fact]
    public void CstBuilder_AddChild()
    {
        var childLeaf = new GreenLeafNode(10, 3, "abc");
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddChild(childLeaf);
        b.EndNode();
        var node = b.Build();
        Assert.Equal(3, node.Width);
        Assert.Same(childLeaf, node.GetChild(0));
    }

    [Fact]
    public void CstBuilder_AddTokenWithTextSpan()
    {
        var b = new CstBuilder();
        b.AddToken(1, default(TextSpan));
        var node = b.Build();
        Assert.Equal(5, node.Width);
    }
}
