# 🌳 Nyar.Vendor.Oak

[![NuGet](https://img.shields.io/nuget/v/Nyar.Vendor.Oak.svg)](https://www.nuget.org/packages/Nyar.Vendor.Oak)
[![License](https://img.shields.io/github/license/nyar-vm/Oak.cs.svg)](https://github.com/nyar-vm/Oak.cs)

**完整的 Oak 语法解析框架包** —— 所有 Oak 解析器的统一入口，提供一站式语法解析解决方案。

## ✨ 特性

- 🚀 **高性能** —— 零分配设计，栈上友好
- 🌳 **纯语法层** —— 只做语法，绝不涉足语义
- 🔧 **统一 API** —— 一致的解析器接口设计
- 🛡️ **类型安全** —— 完整的 .NET 类型系统支持
- 📝 **手写解析器** —— 提供基础设施，解析器由用户手写

## 📦 包含的解析器

### 🔧 核心基础

| 包名             | 说明                                                     |
|----------------|--------------------------------------------------------|
| Oak            | 核心 —— 不可变源抽象、Green/Red 树、增量重解析、CstBuilder、ParseContext |
| Oak.Attributes | 语法树注解属性                                                |

### 🎨 图形与媒体语言

| 包名           | 格式          | 说明            |
|--------------|-------------|---------------|
| Oak.GGScript | `.gg`       | Gnosis 游戏脚本语言 |
| Oak.GGShader | `.ggshader` | Gnosis 着色器语言  |

### ⚙️ 标记语言

| 包名       | 格式             | 说明           |
|----------|----------------|--------------|
| Oak.Json | `.json`        | JSON 数据交换格式  |
| Oak.Yaml | `.yaml`/`.yml` | YAML 数据序列化格式 |
| Oak.Toml | `.toml`        | TOML 配置文件格式  |
| Oak.Xml  | `.xml`         | XML 可扩展标记语言  |

### 🔨 编程语言

| 包名             | 格式    | 说明               |
|----------------|-------|------------------|
| Oak.CSharp     | `.cs` | C# 语言解析器         |
| Oak.Rust       | `.rs` | Rust 语言解析器       |
| Oak.TypeScript | `.ts` | TypeScript 语言解析器 |
| Oak.JavaScript | `.js` | JavaScript 语言解析器 |

### 🤖 数据与配置

| 包名           | 格式       | 说明                      |
|--------------|----------|-------------------------|
| Oak.Protobuf | `.proto` | Protocol Buffers 接口定义语言 |
| Oak.WasmText | `.wat`   | WebAssembly 文本格式        |
| Oak.LlvmIr   | `.ll`    | LLVM 中间表示               |

### 🗄️ 数据库与查询

| 包名          | 格式         | 说明           |
|-------------|------------|--------------|
| Oak.Sql     | `.sql`     | SQL 结构化查询语言  |
| Oak.GraphQL | `.graphql` | GraphQL 查询语言 |

### 📨 模板与文档

| 包名           | 格式       | 说明            |
|--------------|----------|---------------|
| Oak.Markdown | `.md`    | Markdown 标记语言 |
| Oak.Regex    | `.regex` | 正则表达式语法       |
| Oak.Csv      | `.csv`   | CSV 逗号分隔值格式   |

## 📥 安装

### 通过 NuGet 安装

```bash
dotnet add package Nyar.Vendor.Oak
```

### 通过包管理器安装

```powershell
Install-Package Nyar.Vendor.Oak
```

## 🚀 快速开始

### 解析源文件

```csharp
using Oak;
using Oak.GGScript;

// 解析一个 GGScript 文件
var source = new StringSource(File.ReadAllText("script.gg"));
var parser = new GGScriptParser();
var tree = parser.Parse(source);

Console.WriteLine($"根节点类型: {tree.Root.Kind}");
Console.WriteLine($"子节点数量: {tree.Root.ChildCount}");
```

### 使用增量重解析

```csharp
using Oak;

// 创建语法树
var tree = parser.Parse(source);

// 应用编辑
var edit = new Edit(new TextSpan(10, 5), "newText");
var newTree = tree.Edit(edit, parserRepo);

// 未受影响的子树完全共享
Console.WriteLine($"树已更新，根节点: {newTree.Root.Kind}");
```

### 使用 CstBuilder 构建语法树

```csharp
using Oak;

// 创建构建器
var builder = new CstBuilder();
builder.StartNode(MyKind.Expression);
builder.AddToken(MyKind.Identifier, new TextSpan(0, 5));
builder.AddToken(MyKind.Plus, new TextSpan(5, 1));
builder.AddToken(MyKind.Number, new TextSpan(6, 3));
builder.EndNode();
var node = builder.Build();
```

### 使用 RedNode 遍历树

```csharp
using Oak;

// 获取 Red 根节点
var root = tree.GetRedRoot();

// 遍历所有后代节点
foreach (var descendant in root.Descendants)
{
    Console.WriteLine($"节点: {descendant.Kind}, 偏移: {descendant.Start}");
}
```

### 手写解析器示例

```csharp
using Oak;

public class MyParser
{
    public SyntaxTree Parse(ISource source)
    {
        var builder = new CstBuilder();
        var context = new ParseContext<MyContext>(source, new MyContext());
        
        // 手写解析逻辑
        ParseExpression(ref builder, ref context);
        
        return new SyntaxTree(source, builder.Build());
    }
    
    private void ParseExpression(ref CstBuilder builder, ref ParseContext<MyContext> ctx)
    {
        builder.StartNode(MyKind.Expression);
        // ... 解析逻辑
        builder.EndNode();
    }
}
```
