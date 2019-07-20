# Oak 2.0 文档

欢迎来到 **Oak** 官方文档。

## 简介

Oak 是一个**纯语法框架**，面向现代语言前端的基础设施，只做语法层，绝不涉足语义。

> 把基础设施做到极致，把决策权完整交还给人。

## 快速导航

| 文档 | 描述 |
|:---|:---|
| [项目介绍](./introduction.md) | 了解 Oak 的核心职责与绝对边界 |
| [设计哲学](./design-philosophy.md) | 纯偏移、不可变绿树、手写而非生成 |
| [常见问题](./faq.md) | 疑难问题解答 |

## 核心概念

### 两大核心抽象

| 概念 | 说明 |
|:---|:---|
| **Green/Red 树** | 不可变 Green 节点（共享结构）+ 惰性 Red 视图（带位置和 Parent） |
| **纯偏移位置** | 所有节点只记录 `byte offset` + `byte length`，行列转换由可选工具提供 |

### 解析器辅助

| 概念 | 说明 |
|:---|:---|
| **CstBuilder** | 栈上构建器，`ref struct`，零堆分配 |
| **ParseContext** | 可注入上下文的解析状态，携带 `Language` + `ISyntaxContext` |

## 文档结构

```
documentation/
├── overview/               # 概述文档
│   ├── index.md            # 文档首页
│   ├── introduction.md     # 项目介绍
│   ├── design-philosophy.md # 设计哲学
│   └── faq.md              # 常见问题
├── core-systems/           # 核心系统
│   ├── data-model.md       # 数据模型（ISource, TextSpan, GreenNode, RedNode, SyntaxTree）
│   ├── parsing.md          # 解析器辅助（CstBuilder, ParseContext）
│   ├── incremental-reparse.md # 增量重解析
│   ├── typed-ast.md        # 强类型 AST
│   ├── language-injection.md # 语言注入
│   ├── synthetic-construction.md # 程序化构造
│   ├── utilities.md        # 可选工具层
│   ├── language-abstraction.md # Language 抽象
│   └── language-registry.md # LanguageRegistry
├── development/            # 开发指南
│   ├── getting-started.md  # 入门指南
│   └── api-reference.md    # API 参考
└── technical/              # 技术细节
    └── nyar-integration.md # Nyar 体系集成
```
