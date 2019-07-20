using BenchmarkDotNet.Attributes;
using Oak.Syntax;

namespace Oak.Core.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class RedTreeTraversalBenchmarks
{
    [Params(100, 1000)]
    public int NodeCount { get; set; }

    private SyntaxTree _treeNoCache = null!;
    private SyntaxTree _treeWithCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        for (var i = 0; i < NodeCount; i++)
        {
            b.StartNode(2);
            b.AddToken(3, $"token{i}");
            b.EndNode();
        }
        b.EndNode();
        var root = b.Build();
        var source = string.Join(" ", Enumerable.Range(0, NodeCount).Select(i => $"token{i}"));
        _treeNoCache = new SyntaxTree(new StringSource(source), root, enableParentCache: false);
        _treeWithCache = new SyntaxTree(new StringSource(source), root, enableParentCache: true);
    }

    [Benchmark(Description = "Descendants 遍历（无缓存）")]
    public int RedNode_Descendants_NoCache()
    {
        var root = _treeNoCache.GetRedRoot();
        var count = 0;
        foreach (var _ in root.Descendants()) count++;
        return count;
    }

    [Benchmark(Description = "Descendants 遍历（有缓存）")]
    public int RedNode_Descendants_WithCache()
    {
        var root = _treeWithCache.GetRedRoot();
        var count = 0;
        foreach (var _ in root.Descendants()) count++;
        return count;
    }

    [Benchmark(Description = "GetChild 顺序访问")]
    public RedNode RedNode_GetChild_Sequential()
    {
        var root = _treeWithCache.GetRedRoot();
        RedNode last = root;
        for (var i = 0; i < root.ChildCount; i++)
        {
            last = root.GetChild(i);
        }
        return last;
    }

    [Benchmark(Description = "Parent 查找（无缓存）")]
    public RedNode? RedNode_Parent_NoCache()
    {
        var root = _treeNoCache.GetRedRoot();
        var leaf = root;
        while (leaf.ChildCount > 0) leaf = leaf.GetChild(0);
        return leaf.Parent;
    }

    [Benchmark(Description = "Parent 查找（有缓存）")]
    public RedNode? RedNode_Parent_WithCache()
    {
        var root = _treeWithCache.GetRedRoot();
        var leaf = root;
        while (leaf.ChildCount > 0) leaf = leaf.GetChild(0);
        return leaf.Parent;
    }
}
