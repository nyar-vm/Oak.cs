# Oak 2.0：纯语法框架完整设计

**版本**：2.0\
**定位**：面向现代语言前端的基础设施，只做语法层，绝不涉足语义。\
**哲学**：给最顶尖的工程团队提供精准、无黑盒的语法工具。框架负责机械化的基础设施，所有智慧决策留在工程师手中。

***

## 一、框架边界 —— Oak 做什么，不做什么

### 1.1 Oak 核心职责

- **不可变源抽象**：定义只读 `ISource`，不参与编辑逻辑。
- **手写解析器辅助**：提供栈上构建器 `CstBuilder` 和可注入上下文的 `ParseContext`。解析器完全由用户手写（递归下降/Pratt/组合子，任选）。
- **Green/Red 树**：不可变的 Green 节点（共享结构），惰性 Red 视图（带位置和 Parent）。完全无损，保留所有空白与注释。
- **增量重解析**：编辑发生后，自动定位受影响子树，调用用户提供的重解析函数，将新 Green 子树插回原位，未变部分共享。
- **强类型 AST（手写基座）**：提供 `SyntaxNode` 抽象基类，用户手写继承体系，不用代码生成器。
- **纯偏移位置**：所有节点只记录 `byte offset` + `byte length`，不做行列计算（提供可选工具类，不属核心）。
- **语言注入**：内置嵌套子树机制，支持多语言交织，偏移自动对接。
- **程序化树构造**：`CstBuilder` 可脱离解析环境使用，允许重构/合成新节点。
- **查询与遍历**：基于 `RedNode` 的 `Descendants`、`Ancestors` 等惰性迭代器，以及手写 AST Visitor 的分派基础设施。
- **诊断收集接口**：`ParseContext` 可注入用户定义的诊断收集器，报告语法错误和恢复信息。
- **错误恢复基元**：提供可选的模式化恢复函数 (`SkipTo`, `Expect`, `Recover`)，但完全基于公开 API，用户可自由替换。

### 1.2 Oak 绝对不做的事

- **不做语义分析**：没有符号表、没有类型检查、没有作用域、没有语义缓存。
- **不做索引与持久化**：无论磁盘索引还是内存全局符号索引，均不涉足。
- **不做项目/文件依赖图**：没有 crate graph、没有 module map、不管理多文件关系。
- **不做代码生成**：不提供 AST 生成器、不输出中间表示、不做编译后端。
- **不定义文法 DSL**：没有任何声明式语法配置，解析器必须手写。
- **不做格式化算法**：仅提供缩进辅助和空白遍历能力，具体格式规则由上层实现。
- **不做 LSP 协议实现**：只提供语法数据，LSP 消息处理是上层的事。
- **不假设文本编码**：`ISource` 的索引单元由用户定义（字节/代码单元），框架不依赖具体编码。
- **不强制缓存策略**：Red 节点惰性构造是可选的，框架不维护智能缓存，由上层决定。
- **不生成 Visitor 代码**：只提供空壳基类 `SyntaxVisitor`，所有 `VisitXXX` 方法由用户手写。
- **不提供“万能解析器”**：Oak 不懂你的语言，不能自动解析任何东西。

***

## 二、底层数据模型

### 2.1 `ISource` — 只读源文本

```csharp
public interface ISource {
    char this[int index] { get; }
    int Length { get; }
    string Substring(Range range);
}
```

- 不可变，不提供 `Replace`。编辑通过 `Edit` 描述对象传递。
- 索引单元：用户定义。可以是 `byte`、`char`（UTF-16 代码单元）等。所有位置信息均以此为单位。
- 实现：可以是简单 `string` 包装、rope、编辑器快照等。

**`Edit`** **描述编辑操作**：

```csharp
public readonly struct Edit {
    public TextSpan OldSpan { get; }
    public string NewText { get; }
}
```

### 2.2 `TextSpan` — 纯偏移范围

```csharp
public readonly struct TextSpan {
    public int Start { get; }   // 字节/单元偏移
    public int Length { get; }  // 字节/单元长度
    public int End => Start + Length;
}
```

- 不包含行/列。行列转换由 `Oak.Utilities.LineIndex` 等可选工具提供。

### 2.3 `GreenNode` — 不可变语法节点

```csharp
public abstract class GreenNode {
    public abstract NodeKind Kind { get; }
    public abstract int Width { get; }          // 占用的源长度（字节/单元）
    public abstract GreenNode? GetChild(int index);
    public abstract int ChildCount { get; }
    // 内部实现：叶节点存储 token 文本或仅长度；内部节点存储子节点列表
}
```

- 不可变，因此可安全地在多处共享（如重复的表达式）。
- 子树重用：同一个 `GreenNode` 实例可被多棵树引用，完全无副作用。
- 构建方式：通过 `CstBuilder`。

### 2.4 `RedNode` — 带绝对位置的视图

```csharp
public readonly struct RedNode {
    internal GreenNode Green { get; }
    internal SyntaxTree Tree { get; }
    internal int Start { get; }          // 绝对偏移

    public NodeKind Kind => Green.Kind;
    public TextSpan Span => new TextSpan(Start, Green.Width);
    public RedNode? Parent { get; }      // 惰性计算，从 Tree 中查找
    public RedNode GetChild(int index) { /* ... */ }
    // 便利查询：Descendants, Ancestors, FindToken 等
}
```

- `RedNode` 是轻量值类型（`readonly struct`），栈上友好。
- 每次查询生成新的 RedNode（复制成本极低），不更改内部状态。
- Parent 通过树遍历或缓存计算（若启用缓存），默认惰性向上查找。

### 2.5 `SyntaxTree` — 树的根源

```csharp
public class SyntaxTree {
    public ISource Source { get; }
    public GreenNode Root { get; private set; }
    
    public RedNode GetRedRoot() => new RedNode(Root, this, 0);
    
    // 增量编辑入口
    public SyntaxTree Edit(Edit edit, IncrementalParserRepo parsers);
}
```

- `Edit` 返回一棵**全新的** `SyntaxTree`，旧树不变，满足不可变性。
- 内置节点缓存（可选），加速 RedNode 构造和父节点查找。

***

## 三、手写解析器的伙伴：`CstBuilder` 与 `ParseContext`

### 3.1 `CstBuilder` — 构建 Green 树

```csharp
public ref struct CstBuilder {
    public void StartNode(NodeKind kind);
    public void EndNode();
    public void AddToken(TokenKind kind, TextSpan sourceRange);  // 叶子 token
    public void AddChild(GreenNode node);                        // 子树
    public GreenNode Build();                                    // 出栈根节点
}
```

- `ref struct` 保证只存在于栈上，零堆分配。
- 用法示例（由用户解析器调用，不写在框架内）：

```csharp
GreenNode ParseExpr(ref CstBuilder b, ref ParseContext ctx) {
    b.StartNode(MyKind.Expr);
    ParseTerm(ref b, ref ctx);
    while (ctx.Current.Kind == TokenKind.Plus) {
        ctx.Advance();
        ParseTerm(ref b, ref ctx);
    }
    b.EndNode();
    return b.Build();
}
```

（此示例仅为说明，Oak 框架本身不包含此逻辑）

### 3.2 `ParseContext` — 携带状态与穿透信息

```csharp
public ref struct ParseContext<TContext> where TContext : ISyntaxContext {
    public ISource Source { get; }
    public int Position { get; set; }
    public Token Current { get; }
    public TContext Context { get; }          // 用户定义的上下文（如符号表接口）
    public DiagnosticCollector Diagnostics { get; } // 错误收集
    
    public void Advance();
    public void SkipTo(TokenKind kind);
    // 可选错误恢复基元（基于公开API实现，可替换）
    public bool Recover(Action<ParseContext<TContext>> parse, string message);
}
```

- 词法器由用户完全手写，只需提供 `Current` 和 `Advance`。
- `TContext` 实现 `ISyntaxContext`（空接口，由用户定义），可传递解析时需要的外部信息（类型名列表、运算符优先级表等）。
- Oak **不实现** `ISyntaxContext`，只负责传递。

***

## 四、增量重解析机制

当发生编辑时：

1. 计算编辑影响的最小 `GreenNode`（最深层节点，其 `Width` 覆盖编辑范围）。
2. 从该节点对应的旧源文本与新源文本，调用用户为 `NodeKind` 注册的**重解析函数**。
3. 若成功（返回非 null），用新 `GreenNode` 替换旧子树，向上更新父节点宽度；若失败（返回 null），扩大一层节点范围，再次尝试，直到根节点。
4. 未受影响的子树完全重用（引用相同 `GreenNode`），避免重新解析。

**用户注册**：

```csharp
public delegate GreenNode? IncrementalParser(ISource source, TextSpan span, ISyntaxContext context, out bool changed);
```

- `changed` 指示输出是否与输入结构不同（若相同，可保守返回原节点以共享）。
- 重解析函数由用户手写，应设计为纯函数，依赖 `source` 和 `context`。

***

## 五、强类型 AST：手写而非生成

### 5.1 基类 `SyntaxNode`

```csharp
public abstract class SyntaxNode {
    internal GreenNode Green { get; }
    internal SyntaxTree Tree { get; }
    internal int Offset { get; }
    public TextSpan Span => new TextSpan(Offset, Green.Width);
    
    protected SyntaxNode ChildNode<T>(int index) where T : SyntaxNode { /* 从树中构造 */ }
    protected SyntaxToken ChildToken(int index) { /* ... */ }
    
    public abstract void Accept(SyntaxVisitor visitor);
}
```

- 不包含任何代码生成逻辑。
- 用户手工派生，如：

```csharp
public sealed class BinaryExpr : ExpressionSyntax {
    public ExpressionSyntax Left => ChildNode<ExpressionSyntax>(0);
    public SyntaxToken OperatorToken => ChildToken(1);
    public ExpressionSyntax Right => ChildNode<ExpressionSyntax>(2);
    public override void Accept(SyntaxVisitor v) => v.VisitBinaryExpr(this);
}
```

### 5.2 手写 Visitor

```csharp
public abstract class SyntaxVisitor {
    public virtual void VisitDefault(SyntaxNode node) { }
    // 用户为每个节点类型手写虚方法
    public virtual void VisitBinaryExpr(BinaryExpr node) => VisitDefault(node);
}
```

- Oak 只提供 `SyntaxVisitor` 空壳，所有分派方法全由用户手写。
- 确保完全控制，零生成污染。

***

## 六、语言注入（多语言交织）

Oak 允许一个 `SyntaxTree` 包含子树，其叶子节点可能由另一个语言的解析器产生。

**机制**：

- 主解析器在遇到可注入 token 时，调用 `InjectLanguage(languageId, sourceRange)`。
- Oak 根据语言 ID 获取注册的解析器，为该范围创建子 `ISource`，调用子解析器产出子 `GreenNode`。
- 将子 `GreenNode` 作为当前节点的子节点挂入 CST。子树的全局偏移会自动通过注入点的基偏移调整。
- 转义处理（如字符串中的 `\n`）由词法器负责，子解析器看到的已是处理后的 token 流，偏移映射保持一致。

**使用**（用户编写，非框架代码）：

```csharp
// 在解析器中，当遇到模板字符串 ${...} 时
ctx.Advance(); // 跳过 ${
var childGreen = Oak.Inject(ctx, "javascript-expression", innerRange);
b.AddChild(childGreen);
```

Oak 提供 `Inject` 辅助方法（调用已注册的语言解析器），不干预具体语言解析逻辑。

***

## 七、程序化构造与合成

应用场景：重构、代码修复、错误恢复插入虚拟节点。

### 7.1 使用 `CstBuilder` 脱离解析环境

```csharp
var b = new CstBuilder();
b.StartNode(MyKind.BinaryExpr);
b.AddToken(MyKind.Identifier, new TextSpan(0, 5)); // 实际偏移可能为虚拟
b.AddToken(MyKind.Plus, new TextSpan(5, 1));
b.AddToken(MyKind.Identifier, new TextSpan(6, 3));
b.EndNode();
GreenNode synthetic = b.Build();
```

- 虚拟 token 可指定任意 `TextSpan`，源文本可选（若偏移超出范围，获取文本时返回空）。
- 合成的 `GreenNode` 可插入增量编辑流，替换旧子树。

### 7.2 强类型 AST 的工厂方法（用户手写）

用户在自定义的 `BinaryExpr` 类中添加：

```csharp
public static BinaryExpr Create(ExpressionSyntax left, SyntaxToken op, ExpressionSyntax right) {
    var b = new CstBuilder();
    b.StartNode(MyKind.BinaryExpr);
    b.AddChild(left.Green);
    b.AddChild(op.Green);
    b.AddChild(right.Green);
    b.EndNode();
    return new BinaryExpr(b.Build(), tree: null, offset: 0);
}
```

后续挂接到树时提供正确的 `SyntaxTree` 和偏移即可。

***

## 八、可选工具层（不在核心，随 Oak 分发）

- **`LineIndex`**：从 `ISource` 构建行起始偏移数组，提供 `GetLineColumn(int offset)` 和 `GetOffset(int line, int col)`。
- **`DiagnosticCollector`**：简单错误收集器，收集语法错误。
- **`IndentSensitiveLexer`** **辅助**：提供缩进栈维护和 `INDENT`/`DEDENT` 产生逻辑，便于解析缩进语言。
- **`TextWriter`** **缩进帮助**：管理当前缩进深度，辅助格式化输出。
- **`SourceMapBuilder`**：构建源映射（`original span` -> `generated span`），用于编译器输出调试信息。
- **错误恢复组合器**：`SkipTo`、`Expect`、`Recover` 等，为不自行编写恢复逻辑的用户提供便利。这些方法是纯语法糖，基于公开 API 实现，不访问内部状态。

***

## 九、疑难问题解答

**Q1：既然 AST 完全手写，语法改动时如何保证一致性？**\
A：Oak 不负责保证。团队需通过严格代码评审和测试来维护解析器、AST、Visitor、格式化器等组件间的一致。这是顶尖团队用自觉换来的极致灵活。Oak 确保底层数据流（树结构、位置）可靠，减少一部分故障点。

**Q2：不提供行列信息，IDE 如何跳转？**\
A：IDE 通过 `LineIndex` 工具将偏移转为行列。LSP 协议中，`Position` 是零基准行列，转换仅在传输层进行一次。保存在节点中的只有稳定偏移，不受换行影响。

**Q3：语言注入时，内嵌语言的 token 偏移如何映射回主文件？**\
A：内嵌解析器产生的 `GreenNode` 子树的本地偏移（从0开始）会自动加上注入点的基偏移，`RedNode.Span` 呈现为全局绝对偏移。用户无需手动计算。

**Q4：错误恢复组合子如果不用，我还能自己写恢复吗？**\
A：完全可以。Oak 提供的 `SkipTo` 等基元是基于 `ParseContext` 公开方法实现的。你可以直接使用原始位置操作和 `CstBuilder` 编写自定义恢复，无需碰组合子。

**Q5：增量重解析对于缩进语言（如 Python）会退化成全文件解析吗？**\
A：不一定。若编辑没有改变后续行的缩进级别，只影响块内语句，重解析可限制在函数或代码块级别。Oak 的策略是自底向上尝试，无法成功则扩大范围，由用户重解析函数决定。即使退化为整个文件重解析，由于手写解析器高效，性能仍可接受。

**Q6：框架需要支持并发编辑吗（多个编辑器视图同时读写）？**\
A：`SyntaxTree` 是不可变的，每次编辑产生新树。不同的并发任务可持有各自的版本，互不干扰。但全局状态的修改（如源文件关联）需上层同步。

**Q7：Oak 如何处理文件编码（UTF-8/UTF-16）？**\
A：`ISource` 索引单元是抽象的。如果使用字节偏移，`ISource` 可实现为 `byte[]` 并提供 `char` 访问（解码）。`TextSpan` 存储的值必须与索引单元一致。Oak 不假设编码，由用户选择。

**Q8：遇到 Raku 或可扩展语法怎么办？**\
A：Oak 的固定解析器模型无法直接处理运行时改变的语法。可将 Oak 作为底层 CST 构造引擎，由上层解释器动态解析用户 grammar 并调用 `CstBuilder`。这是 Oak 的退化使用，但仍可行。

**Q9：如果我想缓存语义分析结果，Oak 提供什么？**\
A：只提供 `GreenNode` 的不可变身份和 `RedNode` 的稳定 `TextSpan`。语义层可基于 `(SyntaxTree, Offset)` 去重，利用 `GreenNode` 引用相等判断结构未变。任何高级缓存逻辑全在 Oak 之外。

**Q10：怎么确保格式化后代码能无损重新解析？**\
A：Oak 的 CST 保留所有原始空白和注释。格式器应以这些信息为输入，小心地重新调整空白，但不删除任何有语义意义的元素（如字符串内容）。这是格式化器的职责，而非 Oak 的保证。

***

## 十、结语

Oak 2.0 是一块致密、锋利的语法基石。它不试图变聪明，不预设你的语言，不替你决定好坏。它只保证：只要你肯手写，它会用最精确的偏移、最轻量的树、最干净的重解析机制回报你。剩下的语义之美，留给 Nyar.Semantic；编译之器，留给你。

你的直觉很准，`Language` 抽象是手写解析框架里一个关键的“配置中心”，它让同一套解析器内核可以灵活适配方言、版本特性开关、严格模式等。Oak 2.0 需要补上这一环，同时要死守边界——`Language` 只提供配置，不夹带任何语义逻辑。

***

## 补充：`Language` 抽象 —— 语法特性开关与方言控制

### 定位

`Language` 是用户定义的类，代表一种具体的语言方言（如 `TypeScriptLanguage`、`JavaScriptLanguage`、`JSXLanguage`），负责声明该方言下**哪些语法特性开启**、**词法行为差异**（如是否支持装饰器、管道符）、**解析时可用的选项**。它是解析器的“配置对象”，通过 `ParseContext` 注入，让手写解析器在不修改控制流的条件下自适应不同方言。

Oak 不提供 `Language` 的默认实现，只给出一个极薄的基础类/接口，所有具体逻辑由用户手写。

***

### 设计

```csharp
// Oak 提供的抽象基类
public abstract class Language {
    public abstract string Name { get; }
    // 可选：解析器工厂，用于增量/注入
    // 实际解析器仍由用户手写，Language 只是挂载点
}
```

用户可以自由扩展：

```csharp
public class TypeScriptLanguage : Language {
    public override string Name => "TypeScript";
    
    // 特性开关
    public bool DecoratorsEnabled { get; init; }
    public bool ConstEnumsEnabled { get; init; }
    public bool ImportTypeSyntaxEnabled { get; init; }
    // ... 其他语法选项
    
    // 约定：Language 不包含解析方法，只提供选项
}
```

在 `ParseContext` 中使用时，通过已有的 `TContext` 泛型参数传入，或更直接地将 `Language` 作为上下文的一部分：

```csharp
public ref struct ParseContext<TLanguage> where TLanguage : Language {
    public TLanguage Language { get; }
    // ... 其余成员
}
```

然后手写解析器里可以直接查询开关：

```csharp
if (ctx.Language is TypeScriptLanguage ts && ts.DecoratorsEnabled) {
    TryParseDecorator(ref b, ref ctx);
}
```

Oak 只在 `ParseContext` 中搬运 `Language`，不负责解析特性开关如何影响解析，那是你手写的范畴。

***

### 与 `ISyntaxContext` 的关系

`Language` 解决的是**静态配置**（同一语言的不同版本/方言），它不变化或仅在文件加载时确定。而 `ISyntaxContext` 更偏向**动态语义信息**（如作用域内类型名、运算符表），可能在解析同一文件时改变。两者分层清晰：`Language` 是解析器的配置级上下文，`ISyntaxContext` 是语义级的回答器。Oak 允许通过 `ParseContext<TContext>` 的 `TContext` 同时包含 `Language` 引用和语义上下文，用户可根据需要组合。

***

### Oak 不做什么

- 不定义 `Language` 的具体子类，不提供 `JavaLanguage`、`RustLanguage` 等。
- 不解析语言特性开关，不生成任何 if/else 逻辑。
- 不限制 `Language` 的属性类型（全是用户定义）。
- 不强制 `Language` 必须用于增量重解析：重解析时接收相同的 `Language` 实例即可。

***

### 在框架全景图中的位置

`Language` 属于“用户手写解析器”层的配置胶水。它让解析器的可组合性和复用性大幅提升，同时完全维持了 Oak “无黑盒”承诺。你现在可以写一个核心解析引擎，通过注入不同的 `Language` 对象，解析出包含不同语法特性的 CST，仍由同一个手写代码库完成。

***

这样一来，Oak 2.0 的拼图就真正齐了：最底层的不可变源、树、构建器；中间的手写 AST + Visitor；上层的配置 `Language` 抽象；以及增量重解析、语言注入、纯偏移位置、程序化构造。每一个组件都清晰刻着 Oak 的信仰：**把基础设施做到极致，把决策权完整交还给人。**

以下为 Oak 2.0 相对此前向团队展示版本的**修订部分**，仅列出新增、变更或删减的内容。未提及的部分视为保持原样。

***

## 一、`ISource` 修正：彻底只读

- **旧设计**：`ISource` 包含 `Replace` 方法，暗示可变性。
- **修订**：去除 `Replace`，`ISource` 变为纯只读接口。
  ```csharp
  public interface ISource {
      char this[int index] { get; }
      int Length { get; }
      string Substring(Range range);
  }
  ```
- **编辑表示**：新增 `Edit` 描述结构，由上层传递编辑意图。
  ```csharp
  public readonly struct Edit {
      public TextSpan OldSpan { get; }
      public string NewText { get; }
  }
  ```
- **理由**：语法层不应参与源数据修改，保证不可变性，适配编辑器只读快照或版本化源。

***

## 二、新增 `Language` 抽象（语法特性开关）

- **新增**：`abstract class Language`，作为方言/版本配置的载体。
  ```csharp
  public abstract class Language {
      public abstract string Name { get; }
  }
  ```
- **用户手写具体语言类**，例如：
  ```csharp
  public class TypeScriptLanguage : Language {
      public override string Name => "TypeScript";
      public bool DecoratorsEnabled { get; init; }
      // …其他特性开关
  }
  ```
- **解析器注入**：`ParseContext` 可携带 `Language` 实例（通过泛型 `TContext` 或直接字段）。Oak 只搬运，不解析开关含义。
- **边界**：`Language` 负责静态配置，不属于语义信息（语义信息请走 `ISyntaxContext`）。

***

## 三、引入 `SyntaxRoot` 基类，取代“文件根”概念

- **旧设计**：提及 `SourceFile` 或模糊的树根。
- **修订**：提供纯语法的 `SyntaxRoot`，不包含任何文件/路径/IO 信息。
  ```csharp
  public abstract class SyntaxRoot : SyntaxNode {
      // 语言顶层结构的基类，无文件语义
  }
  ```
- **用户派生**：`TypeScriptSyntaxRoot`、`JavaScriptSyntaxRoot` 等。
- **从属**：`SyntaxTree` 持有 `SyntaxRoot`，而非文件对象。

***

## 四、新增 `LanguageRegistry`：语言 ID → 解析器

- **新增**：全局注册机制，将语言标识符映射到解析能力。
  ```csharp
  public static class LanguageRegistry {
      public static void Register(string languageId, Func<ISource, SyntaxRoot> parser);
      public static SyntaxRoot Parse(string languageId, ISource source);
  }
  ```
- **与** **`Language`** **的关系**：用户自行关联 `languageId` 与具体 `Language` 实例（通常在解析器工厂内）。
- **不做什么**：不管文件扩展名映射（`.ts` → `"typescript"` 由上层定义）。

***

## 五、`SyntaxTree` 支持多 `SyntaxRoot`（多语言注入视图）

- **旧设计**：语言注入仅作为主树中的内嵌子树。
- **修订**：`SyntaxTree` 现在可暴露多个独立 `SyntaxRoot`，对应同一源的不同语言根（主要根 + 注入的子语言根）。
  ```csharp
  public class SyntaxTree {
      public ISource Source { get; }
      public SyntaxRoot PrimaryRoot { get; }
      public IReadOnlyList<SyntaxRoot> AllRoots { get; }
      public SyntaxRoot GetRoot(string languageId);
  }
  ```
- **实现**：注入的子解析器产出的 `GreenNode` 子树，除了挂载到主树外，也可包装为独立 `SyntaxRoot`，通过 `AllRoots` 查询。
- **不创建文件**：完全在语法树内部完成，不依赖文件抽象。

***

## 六、新增 `TreeChangeEvent`：语法树变更通知

- **新增**：不可变树修改后，Oak 可产生变更事件。
  ```csharp
  public readonly struct TreeChangeEvent {
      public SyntaxTree OldTree { get; }
      public SyntaxTree NewTree { get; }
      public TextSpan ChangedSpan { get; }
      public IReadOnlyList<GreenNode> ReplacedNodes { get; }
  }
  ```
- **用途**：面向订阅者（如 LSP 诊断、语义缓存层）精确告知受影响范围。
- **非强制**：可由 `SyntaxTree` 的实现选择是否触发。

***

## 七、明确 Oak 不涉足的具体条目

在已有“不做语义”基础上，进一步列出此次修订中明确排除的内容：

- 文件路径、文件名、文件系统 (`VirtualFile`、`FileSystem` 等) → **不属于 Oak**。
- 文件扩展名映射（`.ts` → `languageId`） → **不属于 Oak**。
- 项目/模块依赖图 → **不属于 Oak**。
- `SyntaxAnnotator` (语法标注器) → **属于 Nyar.Syntax 层**，移出 Oak 职责。
- 任何持久化索引、磁盘缓存 → **不属于 Oak**。

***

## 八、其他细微修正

- **`ParseContext`** **泛型调整**：明确除了 `TContext` 外，可直接携带 `Language` 字段，或由 `TContext` 包含 `Language`，Oak 不做强制，但文档推荐模式为：
  ```csharp
  public ref struct ParseContext<TLanguage, TContext>
      where TLanguage : Language
      where TContext : ISyntaxContext { … }
  ```
  实际 API 留待实现时选定一种重载策略。
- **`CstBuilder`** **不绑定任何** **`Language`**，保持无状态构建器角色。

***

以上即为 Oak 2.0 对已展示版本的全部修订。可对照原设计文档逐条应用。

**直言不讳**：您提出的三点完全正确。之前的划分确实存在冗余与归属错误，对高亮的分层也缺乏深入设计。以下是严格基于 Oak 2.0 纯语法定位，并借鉴 PSI 精髓后的 Nyar 体系**修订版**。

***

## 一、关键修正点

### 1. SyntaxFactory 下放至 Oak

- **原错误**：将构造强类型 AST 的工厂方法放在 Nyar.Syntax。
- **修正**：程序化构造语法树是**纯语法行为**，不依赖任何语义或文件系统。Oak 必须提供：
  - 底层 `CstBuilder`（已有）。
  - **强类型 AST 工厂基类**：用户手写 `BinaryExpr.Create(...)` 时继承的 `SyntaxFactory` 泛型基类，以及从 `ISource` 文本直接解析出 `SyntaxTree` 的 `SyntaxTreeFactory`。
  - `LanguageRegistry.Parse()` 本身就是一个工厂入口。
- **结论**：`SyntaxFactory` 概念完全被 Oak 的 `CstBuilder` + `LanguageRegistry` + `ISource` 覆盖。**Nyar.Syntax 不再包含任何构造能力**。

### 2. SyntaxTreeViewProvider 无存在意义

- **原因**：Oak 2.0 的 `SyntaxTree` 已经原生支持多 `SyntaxRoot`（`PrimaryRoot`, `AllRoots`, `GetRoot(languageId)`）。它本身就是一个多语言语法树视图。
- **修正**：删除 `SyntaxTreeViewProvider`，功能由 `SyntaxTree` 直接提供。

### 3. 高亮彻底分家：SyntaxHighlight 与 SemanticHighlight

- **语法高亮**：基于词法 token 类型和纯语法结构（关键字、字符串、注释、括号匹配）。**不应离开语法层**。
- **语义高亮**：基于符号种类（类、函数、可变变量、不可变变量）、类型信息。**必须依赖语义模型**。
- **修正**：
  - **语法高亮**划入 **Nyar.Syntax**，由 `SyntaxHighlighter` 实现，输入 `SyntaxTree`。
  - **语义高亮**划入 **Nyar.Semantic**，由 `SemanticHighlighter` 实现，输入 `SemanticModel`。
  - 原 `SyntaxAnnotator` 概念简化为高亮的内部辅助，不再独立成核心类。

***

## 二、Nyar 体系修订版（严格分层）

```
┌──────────────────────────────────────────────┐
│     Nyar.Tooling (LSP, CLI, 测试工具)         │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Nyar.Workspace (文件/项目/调度)           │
│     VirtualFile, Project, Workspace          │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Nyar.Semantic (符号/类型/索引/语义高亮)   │
│     实现 ISyntaxContext, SemanticModel        │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Nyar.Syntax (纯语法高亮、语法便利查询)    │
│     SyntaxHighlighter, SyntaxQuery            │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Oak 2.0 (纯语法: ISource, CST, AST,       │
│     Language, SyntaxTree 多根, 工厂, 增量)    │
└──────────────────────────────────────────────┘
```

***

## 三、各层精确定义（只写此版本新增或修改的）

### 1. Nyar.Syntax 层 —— 语法层的增强与着色

| 组件                       | 职责                                                                | 不在 Oak 的原因              |
| ------------------------ | ----------------------------------------------------------------- | ----------------------- |
| `SyntaxHighlighter`      | 遍历 `SyntaxRoot`，根据 `NodeKind` / `TokenKind` 输出高亮 token 流。完全不接触符号。 | Oak 不知道 IDE 概念，不定义高亮格式。 |
| `SyntaxQuery`            | 提供 `DescendantsOfKind` 等便利扩展。                                     | 无状态工具类。                 |
| `SyntaxAnnotator` (可选内部) | 简化为 `SyntaxHighlighter` 内部的模式识别器，不暴露。                             | <br />                  |

**不包含**：

- `SyntaxFactory` (已下放 Oak)
- `SyntaxTreeViewProvider` (已删除，功能在 Oak)
- 任何语义标注、跨文件引用

### 2. Nyar.Semantic 层 —— 语义信息与语义高亮

| 组件                                    | 职责                                                                |
| ------------------------------------- | ----------------------------------------------------------------- |
| `SemanticModel`                       | 绑定单个 `SyntaxTree` 的符号表、类型信息、语义诊断。                                 |
| `ISymbolTable` / `IScope` / `ISymbol` | 符号体系。                                                             |
| `TypeChecker` / `IType`               | 类型检查器基类。                                                          |
| `SemanticIndex` / `IStubBuilder`      | 全局索引与 Stub 提取。                                                    |
| `SemanticHighlighter`                 | 基于 `SemanticModel` 输出语义着色 token 流（如 `type`、`mutable variable` 等）。 |
| `SemanticSyntaxContext`               | 实现 `ISyntaxContext`，为解析器提供解析时需的名称分类、优先级表。                         |

**不包含**：任何文件操作、IO、项目组织。

### 3. Nyar.Workspace 层 —— 文件、项目、调度

**不变**（维持前版正确部分）：

- `VirtualFile` 封装文件路径、内容、`LanguageId`。
- `Project` 管理文件集与依赖。
- `Workspace` 作为中央枢纽，负责 `VirtualFile` → `SyntaxTree` → `SemanticModel` 的级联与增量更新。
- 负责编辑事务（`ApplyChange`），触发 Oak 增量重解析，并协调语义索引更新。

**明确职责边界**：Workspace **不实现**任何语法解析或类型检查算法；它只**编排** Oak 和 Nyar.Semantic 的服务。

### 4. Nyar.Tooling 层

- LSP 服务：消费 `Workspace` 提供的数据，生成 LSP 响应，包括语法/语义高亮结果。
- 命令行编译器：串联 Oak 解析 → Nyar.Semantic 分析 → 后端代码生成。
- 测试工具。

***

## 四、与 JetBrains PSI 的模型对应（修正后）

| PSI 概念                              | 对应 Nyar 体系                                        |
| ----------------------------------- | ------------------------------------------------- |
| `ASTNode` (底层树)                     | Oak `GreenNode` / `RedNode`                       |
| `PsiFile` (树根)                      | Oak `SyntaxTree` + `SyntaxRoot`                   |
| `FileViewProvider` (多视图)            | Oak `SyntaxTree.AllRoots` / `GetRoot(languageId)` |
| `PsiBuilder` / `PsiParser`          | Oak `CstBuilder` + 手写解析器                          |
| `PsiElementFactory` (构造)            | Oak `CstBuilder` + 强类型 AST 工厂                     |
| Syntax Highlighting                 | Nyar.Syntax `SyntaxHighlighter`                   |
| Semantic Highlighting               | Nyar.Semantic `SemanticHighlighter`               |
| `PsiReference` / 引用解析               | Nyar.Semantic `ISymbolTable` + 索引                 |
| `PsiStubs` / 索引                     | Nyar.Semantic `StubTree` + `SemanticIndex`        |
| `VirtualFile` / `Module`            | Nyar.Workspace `VirtualFile` / `Project`          |
| `PsiManager` / `PsiDocumentManager` | Nyar.Workspace `Workspace` (编辑调度)                 |

***

## 五、最终检查清单：谁不做什么（重中之重）

| 层级                 | 绝对禁止的行为              |
| ------------------ | -------------------- |
| **Oak 2.0**        | 语义分析、文件路径、高亮格式、项目模型  |
| **Nyar.Syntax**    | 符号表、类型、跨文件引用、文件路径    |
| **Nyar.Semantic**  | 文件系统操作、UI 渲染、LSP 协议  |
| **Nyar.Workspace** | 语法解析算法、类型检查细节、磁盘索引格式 |
| **Nyar.Tooling**   | 定义任何语法/语义数据结构        |

***

此修订解决了您指出的三个核心问题，并且职责边界如刀切一般分明。若还有细节待剖，请随时指出。
