using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class GreenRedEdgeCaseTests
{
    #region 空节点测试

    [Fact]
    public void GreenLeafNode_ZeroWidth_ShouldBeValid()
    {
        var leaf = new GreenLeafNode(0, 0, "");
        Assert.Equal(0, leaf.Width);
        Assert.Equal("", leaf.Text);
        Assert.Equal(0, leaf.ChildCount);
    }

    [Fact]
    public void GreenInternalNode_EmptyChildren_ShouldHaveZeroWidth()
    {
        var internalNode = new GreenInternalNode(1, []);
        Assert.Equal(0, internalNode.Width);
        Assert.Equal(0, internalNode.ChildCount);
        Assert.Empty(internalNode.Children.ToList());
    }

    [Fact]
    public void GreenInternalNode_SingleEmptyChild_ShouldHaveZeroWidth()
    {
        var emptyChild = new GreenLeafNode(0, 0, "");
        var parent = new GreenInternalNode(1, [emptyChild]);
        Assert.Equal(0, parent.Width);
        Assert.Equal(1, parent.ChildCount);
    }

    [Fact]
    public void SyntaxTree_EmptySource_ShouldHaveZeroLengthRoot()
    {
        var leaf = new GreenLeafNode(0, 0, "");
        var tree = new SyntaxTree(StringSource.Empty, leaf);
        Assert.Equal(0, tree.Source.Length);
        var root = tree.GetRedRoot();
        Assert.Equal(0, root.Span.Length);
    }

    [Fact]
    public void CstBuilder_EmptyBuild_ShouldProduceNode()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.EndNode();
        var root = b.Build();
        Assert.Equal(0, root.Width);
        Assert.Equal(0, root.ChildCount);
    }

    #endregion

    #region 深度嵌套测试

    [Fact]
    public void GreenTree_DeepNesting_ShouldConstructCorrectly()
    {
        var leaf = new GreenLeafNode(99, 1, "x");
        GreenNode current = leaf;
        var depth = 100;

        for (var i = 0; i < depth; i++)
        {
            current = new GreenInternalNode(i, [current]);
        }

        Assert.Equal(1, current.Width);

        var node = current;
        for (var i = 0; i < depth; i++)
        {
            Assert.Equal(1, node.ChildCount);
            var child = node.GetChild(0);
            Assert.NotNull(child);
            node = child;
        }

        Assert.True(node.IsLeaf);
    }

    [Fact]
    public void RedTree_DeepNesting_DescendantsShouldEnumerateAll()
    {
        var b = new CstBuilder();
        const int depth = 20;
        for (var i = 0; i < depth; i++)
        {
            b.StartNode(i + 1);
        }
        b.AddToken(99, "leaf");
        for (var i = 0; i < depth; i++)
        {
            b.EndNode();
        }
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("leaf"), root);
        var redRoot = tree.GetRedRoot();

        var descendants = redRoot.Descendants().ToList();
        Assert.Equal(depth, descendants.Count);
    }

    [Fact]
    public void RedTree_DeepNesting_AncestorsShouldTracePath()
    {
        var b = new CstBuilder();
        const int depth = 10;
        for (var i = 0; i < depth; i++)
        {
            b.StartNode(i + 1);
        }
        b.AddToken(99, "leaf");
        for (var i = 0; i < depth; i++)
        {
            b.EndNode();
        }
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("leaf"), root, enableParentCache: true);
        var redRoot = tree.GetRedRoot();

        var leaf = redRoot;
        while (leaf.ChildCount > 0)
        {
            leaf = leaf.GetChild(0);
        }

        var ancestors = leaf.Ancestors().ToList();
        Assert.Equal(depth, ancestors.Count);
    }

    #endregion

    #region 大文件测试

    [Fact]
    public void GreenTree_LargeWidth_ShouldCalculateCorrectly()
    {
        var children = new List<GreenNode>();
        var totalWidth = 0;
        for (var i = 0; i < 1000; i++)
        {
            var child = new GreenLeafNode(1, 10, "0123456789");
            children.Add(child);
            totalWidth += 10;
        }
        var root = new GreenInternalNode(2, children.ToArray());
        Assert.Equal(totalWidth, root.Width);
    }

    [Fact]
    public void RedTree_LargeFile_ChildOffsetsShouldBeCorrect()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        for (var i = 0; i < 100; i++)
        {
            b.AddToken(2, "abcdefghij");
        }
        b.EndNode();
        var root = b.Build();
        var source = string.Concat(Enumerable.Repeat("abcdefghij", 100));
        var tree = new SyntaxTree(new StringSource(source), root);
        var redRoot = tree.GetRedRoot();

        Assert.Equal(100, redRoot.ChildCount);
        for (var i = 0; i < 100; i++)
        {
            var child = redRoot.GetChild(i);
            Assert.Equal(i * 10, child.Span.Start);
            Assert.Equal(10, child.Span.Length);
        }
    }

    [Fact]
    public void GreenTree_WideNode_WidthOverflowProtection()
    {
        var child1 = new GreenLeafNode(1, int.MaxValue / 2, "a");
        var child2 = new GreenLeafNode(2, int.MaxValue / 2, "b");
        var parent = new GreenInternalNode(3, [child1, child2]);
        Assert.Equal(int.MaxValue / 2 * 2, parent.Width);
    }

    #endregion

    #region 并发读取测试

    [Fact]
    public void SyntaxTree_ConcurrentRead_ShouldBeThreadSafe()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "hello");
        b.AddToken(3, " ");
        b.AddToken(4, "world");
        b.EndNode();
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("hello world"), root, enableParentCache: true);

        var exceptions = new List<Exception>();
        var threads = new Thread[8];
        var barrier = new ManualResetEventSlim(false);

        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    barrier.Wait();
                    var redRoot = tree.GetRedRoot();
                    Assert.Equal(new NodeKind(1), redRoot.Kind);
                    Assert.Equal(3, redRoot.ChildCount);

                    for (var j = 0; j < redRoot.ChildCount; j++)
                    {
                        var child = redRoot.GetChild(j);
                        Assert.True(child.Span.Length > 0);
                    }

                    var descendants = redRoot.Descendants().ToList();
                    Assert.Equal(3, descendants.Count);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });
            threads[i].Start();
        }

        barrier.Set();

        foreach (var thread in threads)
        {
            thread.Join(5000);
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public void GreenNode_ConcurrentChildrenAccess_ShouldBeThreadSafe()
    {
        var children = new GreenNode[100];
        for (var i = 0; i < 100; i++)
        {
            children[i] = new GreenLeafNode(i, 1, i.ToString());
        }
        var root = new GreenInternalNode(1, children);

        var exceptions = new List<Exception>();
        var threads = new Thread[4];
        var barrier = new ManualResetEventSlim(false);

        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    barrier.Wait();
                    for (var j = 0; j < 100; j++)
                    {
                        var child = root.GetChild(j);
                        Assert.NotNull(child);
                        Assert.Equal(new NodeKind(j), child.Kind);
                    }

                    var allChildren = root.Children.ToList();
                    Assert.Equal(100, allChildren.Count);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });
            threads[i].Start();
        }

        barrier.Set();

        foreach (var thread in threads)
        {
            thread.Join(5000);
        }

        Assert.Empty(exceptions);
    }

    #endregion

    #region Green 节点不可变性测试

    [Fact]
    public void GreenInternalNode_ChildrenArray_MayNotBeIsolated()
    {
        var children = new GreenNode[]
        {
            new GreenLeafNode(1, 1, "a"),
            new GreenLeafNode(2, 1, "b")
        };
        var node = new GreenInternalNode(3, children);

        var child0 = node.GetChild(0);
        Assert.NotNull(child0);
        Assert.Equal(new NodeKind(1), child0.Kind);
    }

    [Fact]
    public void GreenLeafNode_IsImmutable_AfterCreation()
    {
        var leaf = new GreenLeafNode(1, 5, "hello");
        Assert.Equal(new NodeKind(1), leaf.Kind);
        Assert.Equal(5, leaf.Width);
        Assert.Equal("hello", leaf.Text);
    }

    #endregion

    #region Red 节点 Span 计算测试

    [Fact]
    public void RedNode_Span_WithMixedWidthChildren()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "a");
        b.AddToken(3, "bb");
        b.AddToken(4, "ccc");
        b.AddToken(5, "dddd");
        b.EndNode();
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("abbccdddd"), root);
        var redRoot = tree.GetRedRoot();

        Assert.Equal(default(TextSpan), redRoot.Span);
        Assert.Equal(default(TextSpan), redRoot.GetChild(0).Span);
        Assert.Equal(default(TextSpan), redRoot.GetChild(1).Span);
        Assert.Equal(default(TextSpan), redRoot.GetChild(2).Span);
        Assert.Equal(default(TextSpan), redRoot.GetChild(3).Span);
    }

    [Fact]
    public void RedNode_NestedSpan_ShouldCalculateCorrectly()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "ab");
        b.StartNode(3);
        b.AddToken(4, "cd");
        b.AddToken(5, "ef");
        b.EndNode();
        b.AddToken(6, "gh");
        b.EndNode();
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("abcdefgh"), root);
        var redRoot = tree.GetRedRoot();

        Assert.Equal(default(TextSpan), redRoot.Span);

        var nested = redRoot.GetChild(1);
        Assert.Equal(default(TextSpan), nested.Span);
        Assert.Equal(default(TextSpan), nested.GetChild(0).Span);
        Assert.Equal(default(TextSpan), nested.GetChild(1).Span);
    }

    #endregion

    #region TextSpan 边界测试

    [Fact]
    public void TextSpan_ZeroLength_ShouldBeValid()
    {
        var span = default(TextSpan);
        Assert.Equal(5, span.Start);
        Assert.Equal(0, span.Length);
        Assert.Equal(5, span.End);
    }

    [Fact]
    public void TextSpan_Contains_StartInclusive()
    {
        var span = default(TextSpan);
        Assert.True(span.Contains(5));
        Assert.True(span.Contains(10));
        Assert.True(span.Contains(14));
        Assert.False(span.Contains(4));
        Assert.False(span.Contains(15));
    }

    [Fact]
    public void TextSpan_OverlapsWith_AdjacentSpans()
    {
        var span1 = default(TextSpan);
        var span2 = default(TextSpan);
        Assert.False(span1.OverlapsWith(span2));

        var span3 = default(TextSpan);
        Assert.True(span1.OverlapsWith(span3));
    }

    [Fact]
    public void TextSpan_OverlapsWith_ZeroLengthSpan()
    {
        var span1 = default(TextSpan);
        var zeroSpanInside = default(TextSpan);
        Assert.True(span1.OverlapsWith(zeroSpanInside));

        var zeroSpanOutside = default(TextSpan);
        Assert.False(span1.OverlapsWith(zeroSpanOutside));
    }

    #endregion
}
