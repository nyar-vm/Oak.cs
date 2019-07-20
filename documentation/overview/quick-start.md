# 快速开始

## 环境要求

### 必需条件

| 工具 | 版本要求 | 说明 |
|:---|:---|:---|
| .NET SDK | 8.0+ | 核心运行时 |
| IDE | Rider / VS 2022 | 推荐使用 Rider |

## 项目结构

```
Oak.cs/
├── projects/
│   ├── Oak/                # 核心框架
│   │   ├── Syntax/         # 语法树核心（GreenNode, RedNode, SyntaxTree, CstBuilder...）
│   │   ├── Diagnostics/    # 诊断收集
│   │   ├── Lexing/         # 词法器基类
│   │   ├── Parsing/        # 解析器基类
│   │   ├── Text/           # 文本处理
│   │   ├── Utilities/      # 可选工具（LineIndex, IndentTracker...）
│   │   └── Data/           # 数据序列化
│   ├── Oak.Valkyrie/       # Valkyrie 语言（gg-script）解析器
│   ├── Oak.Typescript/     # TypeScript 解析器
│   ├── Oak.C/              # C 语言解析器
│   ├── Oak.Python/         # Python 解析器
│   ├── Oak.Rust/           # Rust 解析器
│   ├── Oak.JavaScript/     # JavaScript 解析器
│   ├── Oak.Erlang/         # Erlang 解析器
│   ├── Oak.Haskell/        # Haskell 解析器
│   ├── Oak.Julia/          # Julia 解析器
│   ├── Oak.OCaml/          # OCaml 解析器
│   ├── Oak.Prolog/         # Prolog 解析器
│   ├── Oak.Verse/          # Verse 解析器
│   ├── Oak.Markdown/       # Markdown 解析器
│   ├── Oak.Json/           # JSON 解析器
│   ├── Oak.Yaml/           # YAML 解析器
│   ├── Oak.Toml/           # TOML 解析器
│   ├── Oak.Csv/            # CSV 解析器
│   ├── Oak.Ini/            # INI 解析器
│   ├── Oak.Xml/            # XML 解析器
│   ├── Oak.Svg/            # SVG 解析器
│   ├── Oak.Scss/           # SCSS 解析器
│   ├── Oak.Vue/            # Vue SFC 解析器
│   ├── Oak.Awsl/           # AWSL Widget 解析器
│   ├── Oak.DejaVu/         # DejaVu 模板解析器
│   ├── Oak.Glsl/           # GLSL 词法器
│   ├── Oak.Hlsl/           # HLSL 词法器
│   ├── Oak.Lua/            # Lua 词法器
│   ├── Oak.Ktx/            # KTX 纹理解析器
│   ├── Oak.Obj/            # OBJ 模型解析器
│   ├── Oak.SpineAtlas/     # Spine Atlas 解析器
│   └── Oak.Von/            # GON（gg-object）解析器
└── documentation/          # 文档
```

## 基本概念

### Green/Red 树

Oak 采用 Green/Red 双树模型：

- **GreenNode**：不可变语法节点，只存 `Kind` + `Width`，可安全共享
- **RedNode**：轻量值类型视图，持有绝对偏移，提供 `Parent`、`Descendants` 等查询

### 纯偏移位置

所有节点只记录 `byte offset` + `byte length`，不存行列。行列转换由 `LineIndex` 工具提供。

### 手写解析器

Oak 不提供解析器实现。用户通过 `CstBuilder` 构建 Green 树，通过 `ParseContext` 携带解析状态。

## 简单示例

### 1. 定义语言

```csharp
using Oak.Syntax;

public class MyLanguage : Language
{
    public override string Name => "MyLang";
    public bool FeatureXEnabled { get; init; }
}
```

### 2. 构建语法树

```csharp
var b = new CstBuilder();
b.StartNode(MyNodeKind.Expr);
b.AddToken(MyNodeKind.Identifier, new TextSpan(0, 5));
b.AddToken(MyNodeKind.Plus, new TextSpan(5, 1));
b.AddToken(MyNodeKind.Identifier, new TextSpan(6, 3));
b.EndNode();
GreenNode green = b.Build();
```

### 3. 创建语法树

```csharp
var source = new StringSource("var x = 42");
var tree = new SyntaxTree(source, green);
var root = tree.GetRedRoot();

foreach (var desc in root.Descendants())
{
    Console.WriteLine($"{desc.Kind} @ {desc.Span}");
}
```

### 4. 增量编辑

```csharp
var edit = new Edit(new TextSpan(4, 1), "y");
var newTree = tree.Edit(edit, incrementalParsers);
```

## 下一步

1. 阅读 [项目介绍](./introduction.md) 了解核心职责与边界
2. 阅读 [设计哲学](./design-philosophy.md) 理解设计原则
3. 探索 [数据模型](../core-systems/data-model.md) 深入了解底层抽象
4. 查看 [API 参考](../development/api-reference.md) 了解核心接口
