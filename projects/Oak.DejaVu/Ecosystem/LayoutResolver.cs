namespace Oak.DejaVu.Ecosystem;

/// <summary>
///     布局解析器——解析模板继承链、嵌套布局、Content Placeholder。
///     支持多层 extends 嵌套和 block 覆盖传播。
/// </summary>
public sealed class LayoutResolver
{
    /// <summary>
    ///     解析布局继承链——返回从根布局到子模板的完整继承路径
    /// </summary>
    /// <param name="templatePath">起始模板路径</param>
    /// <param name="templateLoader">模板加载器</param>
    /// <returns>布局继承链（索引 0 为根布局，最后一个为当前模板）</returns>
    public LayoutChain ResolveChain(string templatePath, ITemplateLoader templateLoader)
    {
        var chain = new List<LayoutNode>();
        var visited = new HashSet<string>();
        var currentPath = templatePath;

        while (!string.IsNullOrEmpty(currentPath))
        {
            if (visited.Contains(currentPath))
            {
                return new LayoutChain(chain, LayoutResolveStatus.CircularInheritance, $"检测到循环继承: {currentPath}");
            }

            visited.Add(currentPath);

            var source = templateLoader.Load(currentPath);
            if (source == null)
            {
                return new LayoutChain(chain, LayoutResolveStatus.TemplateNotFound, $"模板未找到: {currentPath}");
            }

            var parser = new DejaVuParser("doki");
            var parseResult = parser.Parse(source);

            var extendsNode = parseResult.Nodes.OfType<DejaVuExtendsNode>().FirstOrDefault();
            var blocks = CollectBlocks(parseResult.Nodes);
            var contentPlaceholders = CollectContentPlaceholders(parseResult.Nodes);

            chain.Add(new LayoutNode
            {
                TemplatePath = currentPath,
                Source = source,
                Blocks = blocks,
                ContentPlaceholders = contentPlaceholders,
                ParentTemplatePath = extendsNode?.ParentTemplate.Trim('\'', '"')
            });

            currentPath = extendsNode?.ParentTemplate.Trim('\'', '"') ?? string.Empty;
            if (!string.IsNullOrEmpty(currentPath) && !currentPath.Contains('.') && !currentPath.Contains('/') && !currentPath.Contains('\\'))
            {
                currentPath = templateLoader.ResolvePath(currentPath);
            }
        }

        chain.Reverse();

        return new LayoutChain(chain, LayoutResolveStatus.Success, null);
    }

    /// <summary>
    ///     合并布局链——将子模板的 block 覆盖传播到根布局
    /// </summary>
    /// <param name="chain">布局继承链</param>
    /// <returns>合并后的 block 映射（block 名 → 最终内容节点列表）</returns>
    public Dictionary<string, MergedBlock> MergeBlocks(LayoutChain chain)
    {
        var merged = new Dictionary<string, MergedBlock>();

        foreach (var node in chain.Nodes)
        {
            foreach (var (name, blockInfo) in node.Blocks)
            {
                if (!merged.TryGetValue(name, out var existing))
                {
                    merged[name] = new MergedBlock
                    {
                        Name = name,
                        DefaultContent = blockInfo.Children,
                        OverrideContent = blockInfo.Children,
                        DefinedIn = node.TemplatePath,
                        OverrideFrom = node.TemplatePath
                    };
                }
                else
                {
                    var hasSuper = blockInfo.Children.Any(c => c is DejaVuSuperNode);
                    merged[name] = new MergedBlock
                    {
                        Name = name,
                        DefaultContent = existing.DefaultContent,
                        OverrideContent = hasSuper
                            ? MergeWithSuper(existing.OverrideContent, blockInfo.Children)
                            : blockInfo.Children,
                        DefinedIn = existing.DefinedIn,
                        OverrideFrom = node.TemplatePath
                    };
                }
            }
        }

        return merged;
    }

    /// <summary>
    ///     渲染布局——从根布局开始，逐层应用 block 覆盖
    /// </summary>
    /// <param name="chain">布局继承链</param>
    /// <param name="mergedBlocks">合并后的 block 映射</param>
    /// <returns>渲染后的完整模板节点列表</returns>
    public List<DejaVuTemplateNode> RenderLayout(LayoutChain chain, Dictionary<string, MergedBlock> mergedBlocks)
    {
        if (chain.Nodes.Count == 0) return [];

        var rootNode = chain.Nodes[0];
        return ReplaceBlocks(rootNode.Source, rootNode, mergedBlocks);
    }

    private List<DejaVuTemplateNode> ReplaceBlocks(string source, LayoutNode layoutNode, Dictionary<string, MergedBlock> mergedBlocks)
    {
        var parser = new DejaVuParser("doki");
        var parseResult = parser.Parse(source);
        var result = new List<DejaVuTemplateNode>();

        foreach (var node in parseResult.Nodes)
        {
            if (node is DejaVuBlockNode blockNode)
            {
                if (mergedBlocks.TryGetValue(blockNode.Name, out var merged))
                {
                    result.AddRange(merged.OverrideContent);
                }
                else
                {
                    result.AddRange(blockNode.Children);
                }
            }
            else if (node is DejaVuExtendsNode)
            {
                continue;
            }
            else
            {
                result.Add(node);
            }
        }

        return result;
    }

    private static List<DejaVuTemplateNode> MergeWithSuper(List<DejaVuTemplateNode> defaultContent, List<DejaVuTemplateNode> overrideContent)
    {
        var result = new List<DejaVuTemplateNode>();

        foreach (var node in overrideContent)
        {
            if (node is DejaVuSuperNode)
            {
                result.AddRange(defaultContent);
            }
            else
            {
                result.Add(node);
            }
        }

        return result;
    }

    private static Dictionary<string, BlockInfo> CollectBlocks(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var blocks = new Dictionary<string, BlockInfo>();

        foreach (var node in nodes)
        {
            if (node is DejaVuBlockNode blockNode)
            {
                blocks[blockNode.Name] = new BlockInfo
                {
                    Name = blockNode.Name,
                    Children = blockNode.Children.ToList()
                };
            }
        }

        return blocks;
    }

    private static List<ContentPlaceholder> CollectContentPlaceholders(IReadOnlyList<DejaVuTemplateNode> nodes)
    {
        var placeholders = new List<ContentPlaceholder>();

        foreach (var node in nodes)
        {
            if (node is DejaVuBlockNode blockNode)
            {
                placeholders.Add(new ContentPlaceholder
                {
                    Name = blockNode.Name,
                    HasDefaultContent = blockNode.Children.Count > 0
                });
            }
        }

        return placeholders;
    }
}

/// <summary>
///     布局继承链
/// </summary>
public sealed class LayoutChain
{
    /// <summary>
    ///     继承链节点（索引 0 为根布局，最后一个为当前模板）
    /// </summary>
    public IReadOnlyList<LayoutNode> Nodes { get; }

    /// <summary>
    ///     解析状态
    /// </summary>
    public LayoutResolveStatus Status { get; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     创建布局继承链
    /// </summary>
    public LayoutChain(IReadOnlyList<LayoutNode> nodes, LayoutResolveStatus status, string? errorMessage)
    {
        Nodes = nodes;
        Status = status;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     继承深度
    /// </summary>
    public int Depth => Nodes.Count;
}

/// <summary>
///     布局节点
/// </summary>
public sealed class LayoutNode
{
    /// <summary>
    ///     模板路径
    /// </summary>
    public string TemplatePath { get; init; } = string.Empty;

    /// <summary>
    ///     模板源码
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    ///     定义的 block
    /// </summary>
    public Dictionary<string, BlockInfo> Blocks { get; init; } = new();

    /// <summary>
    ///     Content Placeholder 列表
    /// </summary>
    public List<ContentPlaceholder> ContentPlaceholders { get; init; } = [];

    /// <summary>
    ///     父模板路径
    /// </summary>
    public string? ParentTemplatePath { get; init; }
}

/// <summary>
///     Block 信息
/// </summary>
public sealed class BlockInfo
{
    /// <summary>
    ///     Block 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     Block 子节点
    /// </summary>
    public List<DejaVuTemplateNode> Children { get; init; } = [];
}

/// <summary>
///     合并后的 Block
/// </summary>
public sealed class MergedBlock
{
    /// <summary>
    ///     Block 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     默认内容（来自最初定义）
    /// </summary>
    public List<DejaVuTemplateNode> DefaultContent { get; init; } = [];

    /// <summary>
    ///     覆盖内容（来自子模板）
    /// </summary>
    public List<DejaVuTemplateNode> OverrideContent { get; init; } = [];

    /// <summary>
    ///     定义位置
    /// </summary>
    public string DefinedIn { get; init; } = string.Empty;

    /// <summary>
    ///     覆盖来源
    /// </summary>
    public string OverrideFrom { get; init; } = string.Empty;
}

/// <summary>
///     Content Placeholder
/// </summary>
public sealed class ContentPlaceholder
{
    /// <summary>
    ///     Placeholder 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     是否有默认内容
    /// </summary>
    public bool HasDefaultContent { get; init; }
}

/// <summary>
///     布局解析状态
/// </summary>
public enum LayoutResolveStatus
{
    /// <summary>
    ///     成功
    /// </summary>
    Success,

    /// <summary>
    ///     循环继承
    /// </summary>
    CircularInheritance,

    /// <summary>
    ///     模板未找到
    /// </summary>
    TemplateNotFound
}

/// <summary>
///     模板加载器接口
/// </summary>
public interface ITemplateLoader
{
    /// <summary>
    ///     加载模板源码
    /// </summary>
    string? Load(string templatePath);

    /// <summary>
    ///     解析模板路径
    /// </summary>
    string ResolvePath(string templateName);
}
