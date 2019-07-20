using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

/// <summary>
/// RedNode 红树节点和 SyntaxTree 语法树测试
/// </summary>
public class RedNodeAndSyntaxTreeTests
{
    private static SyntaxTree CreateTestTree()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "hello");
        b.AddToken(3, " ");
        b.AddToken(4, "world");
        b.EndNode();
        var root = b.Build();
        return new SyntaxTree(new StringSource("hello world"), root);
    }

    [Fact]
    public void SyntaxTree_GetRedRoot()
    {
        var tree = CreateTestTree();
        var redRoot = tree.GetRedRoot();
        Assert.Equal(new NodeKind(1), redRoot.Kind);
        Assert.Equal(default(TextSpan), redRoot.Span);
        Assert.Equal(3, redRoot.ChildCount);
    }

    [Fact]
    public void RedNode_GetChild()
    {
        var tree = CreateTestTree();
        var root = tree.GetRedRoot();
        var child0 = root.GetChild(0);
        Assert.Equal(new NodeKind(2), child0.Kind);
        Assert.Equal(default(TextSpan), child0.Span);
        var child1 = root.GetChild(1);
        Assert.Equal(new NodeKind(3), child1.Kind);
        Assert.Equal(default(TextSpan), child1.Span);
        var child2 = root.GetChild(2);
        Assert.Equal(new NodeKind(4), child2.Kind);
        Assert.Equal(default(TextSpan), child2.Span);
    }

    [Fact]
    public void RedNode_Descendants()
    {
        var tree = CreateTestTree();
        var root = tree.GetRedRoot();
        var descendants = root.Descendants().ToList();
        Assert.Equal(3, descendants.Count);
    }

    [Fact]
    public void SyntaxTree_Source()
    {
        var tree = CreateTestTree();
        Assert.Equal(11, tree.Source.Length);
    }
}
