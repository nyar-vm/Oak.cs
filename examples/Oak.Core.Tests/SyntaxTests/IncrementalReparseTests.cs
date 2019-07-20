using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class IncrementalReparseTests
{
    #region 辅助方法

    private static SyntaxTree CreateSimpleTree()
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

    private static SyntaxTree CreateNestedTree()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.StartNode(2);
        b.AddToken(3, "aaa");
        b.AddToken(4, "bbb");
        b.EndNode();
        b.StartNode(5);
        b.AddToken(6, "ccc");
        b.AddToken(7, "ddd");
        b.EndNode();
        b.EndNode();
        var root = b.Build();
        return new SyntaxTree(new StringSource("aaabbbcccddd"), root);
    }

    #endregion

    #region 单字符编辑测试

    [Fact]
    public void Edit_SingleCharInsertion_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();
        var originalRoot = tree.Root;

        var edit = new Edit(default(TextSpan), "!");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.NotSame(tree, newTree);
        Assert.Equal("hello! world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    [Fact]
    public void Edit_SingleCharDeletion_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("helloworld", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    [Fact]
    public void Edit_SingleCharReplacement_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "H");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("Hello world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    #endregion

    #region 多行编辑测试

    [Fact]
    public void Edit_MultiLineInsertion_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "\nnew line\n");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("hello\nnew line\n world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    [Fact]
    public void Edit_MultiLineDeletion_ShouldProduceNewTree()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "line1\n");
        b.AddToken(3, "line2\n");
        b.AddToken(4, "line3");
        b.EndNode();
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("line1\nline2\nline3"), root);

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("line1\nline3", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    #endregion

    #region 删除整个节点测试

    [Fact]
    public void Edit_DeleteEntireToken_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal(" world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    [Fact]
    public void Edit_DeleteAllContent_ShouldProduceEmptyTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    #endregion

    #region 插入新节点测试

    [Fact]
    public void Edit_InsertAtBeginning_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "prefix ");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("prefix hello world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    [Fact]
    public void Edit_InsertAtEnd_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), " suffix");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("hello worl suffix", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    #endregion

    #region 增量解析器注册测试

    [Fact]
    public void Edit_WithIncrementalParser_ShouldUseRegisteredParser()
    {
        var tree = CreateSimpleTree();

        var wasCalled = false;
        var repo = new IncrementalParserRepo();
        repo.Register(new NodeKind(1), (source, span, context, out changed) =>
        {
            wasCalled = true;
            changed = true;
            return new GreenInternalNode(1, [
                new GreenLeafNode(2, 5, "HELLO"),
                new GreenLeafNode(3, 1, " "),
                new GreenLeafNode(4, 5, "WORLD")
            ]);
        });

        var edit = new Edit(default(TextSpan), "H");
        var newTree = tree.Edit(edit, repo);

        Assert.True(wasCalled);
    }

    [Fact]
    public void Edit_WithoutIncrementalParser_ShouldStillApplyEdit()
    {
        var tree = CreateSimpleTree();

        var repo = new IncrementalParserRepo();
        var edit = new Edit(default(TextSpan), "H");
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("Hello world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    #endregion

    #region 未变部分共享测试（引用相等性）

    [Fact]
    public void Edit_UnchangedLeaf_ShouldBeSharedByReference()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.StartNode(2);
        b.AddToken(3, "aaa");
        b.EndNode();
        b.StartNode(4);
        b.AddToken(5, "bbb");
        b.EndNode();
        b.EndNode();
        var root = b.Build();
        var tree = new SyntaxTree(new StringSource("aaabbb"), root);

        var originalSecondChild = root.GetChild(1);

        var edit = new Edit(default(TextSpan), "A");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        var newSecondChild = newTree.Root.GetChild(1);
        Assert.Same(originalSecondChild, newSecondChild);
    }

    [Fact]
    public void Edit_MultipleEdits_UnchangedPartsShouldRemainShared()
    {
        var tree = CreateSimpleTree();
        var originalRoot = tree.Root;

        var repo = new IncrementalParserRepo();

        var edit1 = new Edit(default(TextSpan), "H");
        var tree2 = tree.Edit(edit1, repo);

        var edit2 = new Edit(default(TextSpan), "W");
        var tree3 = tree2.Edit(edit2, repo);

        Assert.Equal("Hello World", tree3.Source.Substring(new Range(0, tree3.Source.Length)));
    }

    #endregion

    #region TreeChangeEvent 测试

    [Fact]
    public void Edit_ShouldProduceTreeChangeEvent_WhenRootChanges()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "H");
        var repo = new IncrementalParserRepo();
        repo.Register(new NodeKind(1), (source, span, context, out changed) =>
        {
            changed = true;
            return new GreenInternalNode(1, [
                new GreenLeafNode(2, 1, "H"),
                new GreenLeafNode(3, 1, " "),
                new GreenLeafNode(4, 5, "world")
            ]);
        });
        var newTree = tree.Edit(edit, repo);

        Assert.NotSame(tree, newTree);
        Assert.NotSame(tree.Root, newTree.Root);
    }

    [Fact]
    public void Edit_Delta_ShouldBePositiveForInsertion()
    {
        var edit = new Edit(default(TextSpan), "inserted");
        Assert.Equal(8, edit.Delta);
    }

    [Fact]
    public void Edit_Delta_ShouldBeNegativeForDeletion()
    {
        var edit = new Edit(default(TextSpan), "");
        Assert.Equal(-3, edit.Delta);
    }

    [Fact]
    public void Edit_Delta_ShouldBeZeroForReplacement()
    {
        var edit = new Edit(default(TextSpan), "abc");
        Assert.Equal(0, edit.Delta);
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void Edit_AtExactBoundary_ShouldWork()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "!");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("hello worl!", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    [Fact]
    public void Edit_EmptyEdit_ShouldProduceSameSource()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.Edit(edit, repo);

        Assert.Equal("hello world", newTree.Source.Substring(new Range(0, newTree.Source.Length)));
    }

    #endregion
}
