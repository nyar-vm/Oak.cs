using BenchmarkDotNet.Attributes;
using Oak.Syntax;

namespace Oak.Core.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class IncrementalReparseBenchmarks
{
    [Params(100, 1000)]
    public int NodeCount { get; set; }

    private SyntaxTree _tree = null!;
    private IncrementalParserRepo _repo = null!;

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
        _tree = new SyntaxTree(new StringSource(source), root);
        _repo = new IncrementalParserRepo();
    }

    [Benchmark(Description = "单字符编辑")]
    public SyntaxTree Edit_SingleChar()
    {
        var edit = new Edit(default(TextSpan), "X");
        return _tree.Edit(edit, _repo);
    }

    [Benchmark(Description = "多行编辑")]
    public SyntaxTree Edit_MultiLine()
    {
        var edit = new Edit(default(TextSpan), "\nnew line\n");
        return _tree.Edit(edit, _repo);
    }

    [Benchmark(Description = "删除整个节点")]
    public SyntaxTree Edit_DeleteNode()
    {
        var edit = new Edit(default(TextSpan), "");
        return _tree.Edit(edit, _repo);
    }

    [Benchmark(Description = "带增量解析器的编辑")]
    public SyntaxTree Edit_WithIncrementalParser()
    {
        var repo = new IncrementalParserRepo();
        repo.Register(new NodeKind(2), (source, span, context, out changed) =>
        {
            changed = true;
            return new GreenInternalNode(2, [
                new GreenLeafNode(3, 6, "Xoken")
            ]);
        });
        var edit = new Edit(default(TextSpan), "X");
        return _tree.Edit(edit, repo);
    }
}
