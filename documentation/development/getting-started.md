# 入门指南

## 环境搭建

### 必需条件

| 工具 | 版本要求 | 说明 |
|:---|:---|:---|
| .NET SDK | 8.0+ | 核心运行时 |
| IDE | Rider / VS 2022 | 推荐使用 Rider |

### 可选条件

| 工具 | 用途 |
|:---|:---|
| Git | 版本控制 |

## 构建项目

```bash
# 克隆仓库
git clone https://github.com/your-org/Oak.cs.git
cd Oak.cs

# 还原依赖
dotnet restore

# 构建解决方案
dotnet build

# 运行测试
dotnet test
```

## 第一个 Oak 解析器

### 1. 定义节点类型

```csharp
using Oak.Syntax;

public static class MyNodeKind
{
    public const int Module = 1;
    public const int Function = 2;
    public const int Statement = 3;
    public const int Identifier = 4;
    public const int Number = 5;
    public const int Plus = 6;
}
```

### 2. 定义语言

```csharp
public class MyLanguage : Language
{
    public override string Name => "MyLang";
}
```

### 3. 手写解析器

```csharp
public static GreenNode ParseModule(ref ParseContext<MyLanguage, MyContext> ctx)
{
    var b = new CstBuilder();
    b.StartNode(MyNodeKind.Module);

    while (ctx.Position < ctx.Source.Length)
    {
        ParseStatement(ref b, ref ctx);
    }

    b.EndNode();
    return b.Build();
}
```

### 4. 注册到 LanguageRegistry

```csharp
LanguageRegistry.Register("my-lang", new MyLanguage(), source =>
{
    var ctx = new ParseContext<MyLanguage, MyContext>(source, new MyLanguage(), new MyContext(), new DiagnosticSink());
    return ParseModule(ref ctx);
});
```

### 5. 使用

```csharp
var source = new StringSource("x + 42");
var root = LanguageRegistry.Parse("my-lang", source);
var tree = root.Tree;

foreach (var node in tree.GetRedRoot().Descendants())
{
    Console.WriteLine($"{node.Kind} @ {node.Span}");
}
```

## 下一步

- 阅读 [API 参考](./api-reference.md) 了解核心接口
- 探索 [数据模型](../core-systems/data-model.md) 理解 Green/Red 树
- 查看 [增量重解析](../core-systems/incremental-reparse.md) 学习编辑机制
