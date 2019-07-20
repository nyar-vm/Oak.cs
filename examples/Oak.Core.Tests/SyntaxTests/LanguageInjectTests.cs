using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class LanguageInjectTests
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

    [Fact]
    public void LanguageInject_InjectUnregistered_ReturnsNull()
    {
        RunWithTimeout(() =>
        {
            LanguageRegistry.Clear();
            var result = LanguageInject.Inject("nonexistent", new StringSource("test"));
            Assert.Null(result);
            return true;
        });
    }

    [Fact]
    public void LanguageInject_InjectRegistered_ReturnsGreenNode()
    {
        RunWithTimeout(() =>
        {
            LanguageRegistry.Clear();
            LanguageRegistry.Register("test-lang", new InjectTestLang(), s =>
            {
                var b = new CstBuilder();
                b.AddToken(1, "test");
                var green = b.Build();
                return new InjectTestRoot(green, new SyntaxTree(s, green), 0, "test-lang");
            });

            var result = LanguageInject.Inject("test-lang", new StringSource("test"));
            Assert.NotNull(result);
            LanguageRegistry.Clear();
            return true;
        });
    }

    [Fact]
    public void LanguageInject_InjectWithRange()
    {
        RunWithTimeout(() =>
        {
            LanguageRegistry.Clear();
            LanguageRegistry.Register("test-lang", new InjectTestLang(), s =>
            {
                var b = new CstBuilder();
                b.AddToken(1, s.Substring(new Range(0, s.Length)));
                var green = b.Build();
                return new InjectTestRoot(green, new SyntaxTree(s, green), 0, "test-lang");
            });

            var mainSource = new StringSource("prefix test suffix");
            var result = LanguageInject.Inject("test-lang", mainSource, default(TextSpan));
            Assert.NotNull(result);
            LanguageRegistry.Clear();
            return true;
        });
    }

    private sealed class InjectTestLang : Language
    {
        public override string Name => "InjectTest";
    }

    private sealed class InjectTestRoot : SyntaxRoot
    {
        public InjectTestRoot(GreenNode green, SyntaxTree tree, int offset, string languageId)
            : base(green, tree, offset, languageId) { }
        public override VisitRecursionMode Accept(SyntaxVisitor visitor) => visitor.VisitDefault(this);
    }
}
