using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

/// <summary>
/// IncrementalParserRepo、TreeChangeEvent、LanguageRegistry 增量解析与注册表测试
/// </summary>
public class IncrementalAndRegistryTests
{
    [Fact]
    public void IncrementalParserRepo_RegisterAndGet()
    {
        var repo = new IncrementalParserRepo();
        IncrementalParser parser = (ISource source, TextSpan span, ISyntaxContext? ctx, out bool changed) =>
        {
            changed = false;
            return null;
        };
        repo.Register(new NodeKind(1), parser);
        var result = repo.Get(new NodeKind(1));
        Assert.NotNull(result);
    }

    [Fact]
    public void IncrementalParserRepo_GetUnregistered_ReturnsNull()
    {
        var repo = new IncrementalParserRepo();
        var result = repo.Get(new NodeKind(99));
        Assert.Null(result);
    }

    [Fact]
    public void TreeChangeEvent_Properties()
    {
        var source = new StringSource("hello");
        var b = new CstBuilder();
        b.AddToken(1, "hello");
        var root = b.Build();
        var oldTree = new SyntaxTree(source, root);
        var newTree = new SyntaxTree(source, root);
        var changedSpan = default(TextSpan);
        var replaced = new List<GreenNode> { root }.AsReadOnly();

        var change = new TreeChangeEvent(oldTree, newTree, changedSpan, replaced, new Edit(changedSpan, "hello"));
        Assert.Same(oldTree, change.OldTree);
        Assert.Same(newTree, change.NewTree);
        Assert.Equal(changedSpan, change.ChangedSpan);
        Assert.Single(change.ReplacedNodes);
    }

    [Fact]
    public void LanguageRegistry_RegisterAndParse()
    {
        LanguageRegistry.Clear();
        var source = new StringSource("test");
        var lang = new TestLang();
        LanguageRegistry.Register("test-lang", lang, s =>
        {
            var builder = new CstBuilder();
            builder.AddToken(1, "test");
            var green = builder.Build();
            var tree = new SyntaxTree(s, green);
            return new TestSyntaxRoot(green, tree, 0, "test-lang");
        });
        Assert.True(LanguageRegistry.IsRegistered("test-lang"));
        var result = LanguageRegistry.Parse("test-lang", source);
        Assert.NotNull(result);
        LanguageRegistry.Clear();
    }

    [Fact]
    public void LanguageRegistry_Unregistered_Throws()
    {
        LanguageRegistry.Clear();
        Assert.Throws<InvalidOperationException>(() => LanguageRegistry.Parse("nonexistent", new StringSource("")));
    }

    private sealed class TestLang : Language
    {
        public override string Name => "TestLang";
    }

    private sealed class TestSyntaxRoot : SyntaxRoot
    {
        public TestSyntaxRoot(GreenNode green, SyntaxTree tree, int offset, string languageId)
            : base(green, tree, offset, languageId) { }
        public override VisitRecursionMode Accept(SyntaxVisitor visitor) => visitor.VisitDefault(this);
    }
}
