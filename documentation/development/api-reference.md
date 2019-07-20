# API 参考

## Oak.Syntax

### ISource — 只读源文本

```csharp
public interface ISource
{
    char this[int index] { get; }
    int Length { get; }
    string Substring(Range range);
}
```

内置实现 `StringSource`：

```csharp
public sealed class StringSource : ISource
{
    public StringSource(string text);
    public static readonly StringSource Empty;
}
```

### TextSpan — 纯偏移范围

```csharp
public readonly struct TextSpan : IEquatable<TextSpan>
{
    public int Start { get; }
    public int Length { get; }
    public int End { get; }
    public bool Contains(int position);
    public bool OverlapsWith(TextSpan other);
}
```

### Edit — 编辑描述

```csharp
public readonly struct Edit
{
    public TextSpan OldSpan { get; }
    public string NewText { get; }
    public int Delta { get; }
}
```

### GreenNode — 不可变语法节点

```csharp
public abstract class GreenNode
{
    public abstract NodeKind Kind { get; }
    public abstract int Width { get; }
    public abstract int ChildCount { get; }
    public bool IsLeaf { get; }
    public abstract GreenNode? GetChild(int index);
    public IEnumerable<GreenNode> Children { get; }
}
```

子类：

- `GreenInternalNode`：内部节点，持有子节点列表
- `GreenLeafNode`：叶子节点，持有 token 文本或仅长度

### RedNode — 带绝对位置的视图

```csharp
public readonly struct RedNode
{
    internal GreenNode Green { get; }
    internal SyntaxTree Tree { get; }
    internal int Start { get; }

    public NodeKind Kind { get; }
    public TextSpan Span { get; }
    public int ChildCount { get; }
    public bool IsLeaf { get; }
    public RedNode? Parent { get; }
    public RedNode GetChild(int index);
    public IEnumerable<RedNode> Children { get; }
    public IEnumerable<RedNode> Descendants();
    public IEnumerable<RedNode> Ancestors();
}
```

### SyntaxTree — 语法树

```csharp
public class SyntaxTree
{
    public ISource Source { get; }
    public GreenNode Root { get; }
    public bool EnableParentCache { get; }
    public IReadOnlyList<SyntaxRoot> AllRoots { get; }
    public SyntaxRoot? PrimaryRoot { get; }

    public RedNode GetRedRoot();
    public SyntaxRoot? GetRoot(string languageId);
    public SyntaxTree Edit(Edit edit, IncrementalParserRepo parsers);

    public event Action<TreeChangeEvent>? Changed;
}
```

### SyntaxNode — 强类型 AST 基类

```csharp
public abstract class SyntaxNode
{
    internal GreenNode Green { get; }
    internal SyntaxTree Tree { get; }
    internal int Offset { get; }
    public TextSpan Span { get; }

    protected T ChildNode<T>(int index) where T : SyntaxNode;
    protected SyntaxToken ChildToken(int index);
    public abstract void Accept(SyntaxVisitor visitor);
}
```

### SyntaxRoot — 语法树根基类

```csharp
public abstract class SyntaxRoot : SyntaxNode
{
    public string LanguageId { get; }
}
```

### CstBuilder — Green 树构建器

```csharp
public ref struct CstBuilder
{
    public void StartNode(NodeKind kind);
    public void EndNode();
    public void AddToken(NodeKind kind, TextSpan sourceRange);
    public void AddToken(NodeKind kind, string text);
    public void AddChild(GreenNode node);
    public GreenNode Build();
}
```

### ParseContext — 解析上下文

```csharp
public ref struct ParseContext<TLanguage, TContext>
    where TLanguage : Language
    where TContext : ISyntaxContext
{
    public ISource Source { get; }
    public int Position { get; set; }
    public TLanguage Language { get; }
    public TContext Context { get; }
    public DiagnosticSink Diagnostics { get; }

    public char Current { get; }
    public char Peek(int offset = 0);
    public void Advance();
    public void SkipTo(char target);
    public void SkipTo(Func<char, bool> predicate);
    public void SkipTo(NodeKind kind);
    public void Recover(RefParseAction<TLanguage, TContext> parseAction, string message);
    public bool Expect(char expected, string code, string message);
    public bool Expect(Func<char, bool> predicate, string code, string message);
    public TextSpan GetSpanFrom(int startPosition);
    public string GetText(TextSpan span);
}
```

### Language — 语言抽象基类

```csharp
public abstract class Language
{
    public abstract string Name { get; }
}
```

### LanguageRegistry — 语言注册表

```csharp
public static class LanguageRegistry
{
    public static IReadOnlyCollection<string> RegisteredLanguages { get; }
    public static void Register(string languageId, Language language, Func<ISource, SyntaxRoot> parser);
    public static SyntaxRoot Parse(string languageId, ISource source);
    public static bool TryParse(string languageId, ISource source, out SyntaxRoot? root);
    public static Language? GetLanguage(string languageId);
    public static bool IsRegistered(string languageId);
    public static bool Unregister(string languageId);
    public static void Clear();
}
```

### IncrementalParser — 增量重解析委托

```csharp
public delegate GreenNode? IncrementalParser(ISource source, TextSpan span, ISyntaxContext? context, out bool changed);
```

### TreeChangeEvent — 语法树变更事件

```csharp
public readonly struct TreeChangeEvent
{
    public SyntaxTree OldTree { get; }
    public SyntaxTree NewTree { get; }
    public TextSpan ChangedSpan { get; }
    public IReadOnlyList<GreenNode> ReplacedNodes { get; }
    public Edit SourceEdit { get; }
}
```

### SyntaxVisitor — 访问器空壳基类

```csharp
public abstract class SyntaxVisitor
{
    // 用户为每个节点类型手写虚方法
}
```

### ISyntaxContext — 语义上下文接口

```csharp
public interface ISyntaxContext { }
```

---

## Oak.Diagnostics

### DiagnosticSink — 诊断收集器

```csharp
public class DiagnosticSink
{
    public IReadOnlyList<DiagnosticMessage> Errors { get; }
    public void AddError(string filePath, TextSpan span, string code, string message);
}
```

### DiagnosticLevel — 诊断级别

```csharp
public enum DiagnosticLevel
{
    Info,
    Warning,
    Error
}
```

---

## Oak.Utilities

### LineIndex — 行列转换

```csharp
public sealed class LineIndex
{
    public LineIndex(ISource source);
    public int LineCount { get; }
    public (int Line, int Column) GetLineColumn(int offset);
    public int GetOffset(int line, int column);
    public int GetLineStart(int line);
    public int GetLineEnd(int line);
}
```

### IndentTracker — 缩进追踪

```csharp
public ref struct IndentTracker
{
    // 缩进栈维护，支持 INDENT/DEDENT 产生
}
```
