using BenchmarkDotNet.Attributes;
using Oak.Syntax;

namespace Oak.Core.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class GreenTreeBenchmarks
{
    [Params(100, 1000, 10000)]
    public int NodeCount { get; set; }

    private string _source = "";

    [GlobalSetup]
    public void Setup()
    {
        _source = string.Join(" ", Enumerable.Range(0, NodeCount).Select(i => $"token{i}"));
    }

    [Benchmark(Description = "CstBuilder 构建")]
    public GreenNode CstBuilder_Build()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        for (var i = 0; i < NodeCount; i++)
        {
            b.AddToken(2, $"token{i}");
        }
        b.EndNode();
        return b.Build();
    }

    [Benchmark(Description = "GreenInternalNode 宽度计算")]
    public int GreenInternalNode_WidthCalculation()
    {
        var children = new GreenNode[NodeCount];
        for (var i = 0; i < NodeCount; i++)
        {
            children[i] = new GreenLeafNode(2, $"token{i}".Length, $"token{i}");
        }
        var root = new GreenInternalNode(1, children);
        return root.Width;
    }

    [Benchmark(Description = "GreenLeafNode 创建")]
    public GreenLeafNode GreenLeafNode_Creation()
    {
        return new GreenLeafNode(1, 5, "hello");
    }

    [Benchmark(Description = "SyntaxTree 创建")]
    public SyntaxTree SyntaxTree_Creation()
    {
        var b = new CstBuilder();
        b.StartNode(1);
        for (var i = 0; i < NodeCount; i++)
        {
            b.AddToken(2, $"token{i}");
        }
        b.EndNode();
        var root = b.Build();
        return new SyntaxTree(new StringSource(_source), root);
    }
}
