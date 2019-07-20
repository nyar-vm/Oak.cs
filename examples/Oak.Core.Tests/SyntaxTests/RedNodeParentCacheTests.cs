using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class RedNodeParentCacheTests
{
    private const int TimeoutMs = 5000;

    private static T RunWithTimeout<T>(Func<T> action, int timeoutMs = TimeoutMs)
    {
        T result = default!;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.Start();
        if (!thread.Join(timeoutMs))
        {
            thread.Interrupt();
            thread.Join(100);
            throw new TimeoutException($"操作在 {timeoutMs}ms 内未完成，可能存在死循环");
        }
        if (exception is not null) throw exception;
        return result;
    }

    private static SyntaxTree CreateTestTree(bool enableCache = false)
    {
        var b = new CstBuilder();
        b.StartNode(1);
        b.AddToken(2, "hello");
        b.AddToken(3, " ");
        b.AddToken(4, "world");
        b.EndNode();
        var root = b.Build();
        return new SyntaxTree(new StringSource("hello world"), root, enableCache);
    }

    [Fact]
    public void SyntaxTree_EnableParentCache_DefaultFalse()
    {
        var tree = CreateTestTree();
        Assert.False(tree.EnableParentCache);
    }

    [Fact]
    public void SyntaxTree_EnableParentCache_True()
    {
        var tree = CreateTestTree(enableCache: true);
        Assert.True(tree.EnableParentCache);
    }

    [Fact]
    public void RedNode_Parent_WithoutCache()
    {
        RunWithTimeout(() =>
        {
            var tree = CreateTestTree(enableCache: false);
            var root = tree.GetRedRoot();
            var child = root.GetChild(0);
            var parent = child.Parent;
            Assert.True(parent.HasValue);
            Assert.Equal(root.Kind, parent.Value.Kind);
            return true;
        });
    }

    [Fact]
    public void RedNode_Parent_WithCache()
    {
        RunWithTimeout(() =>
        {
            var tree = CreateTestTree(enableCache: true);
            var root = tree.GetRedRoot();
            var child = root.GetChild(0);
            var parent = child.Parent;
            Assert.True(parent.HasValue);
            Assert.Equal(root.Kind, parent.Value.Kind);
            return true;
        });
    }

    [Fact]
    public void RedNode_RootParent_IsNull()
    {
        RunWithTimeout(() =>
        {
            var tree = CreateTestTree(enableCache: true);
            var root = tree.GetRedRoot();
            Assert.Null(root.Parent);
            return true;
        });
    }
}
