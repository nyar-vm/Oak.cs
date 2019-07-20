namespace Oak.Syntax;

/// <summary>
///     语法节点工厂，维护 NodeKind 到构造委托的映射，避免反射调用
/// </summary>
public static class NodeFactory
{
    private static readonly Dictionary<int, Func<GreenNode, SyntaxTree, int, SyntaxNode>> Factories = new();

    /// <summary>
    ///     注册指定节点类型的构造委托
    /// </summary>
    public static void Register(NodeKind kind, Func<GreenNode, SyntaxTree, int, SyntaxNode> factory)
    {
        Factories[kind.Value] = factory;
    }

    /// <summary>
    ///     尝试获取指定节点类型的构造委托
    /// </summary>
    public static Func<GreenNode, SyntaxTree, int, SyntaxNode>? Get(NodeKind kind)
    {
        return Factories.TryGetValue(kind.Value, out var factory) ? factory : null;
    }

    /// <summary>
    ///     使用注册的构造委托创建语法节点
    /// </summary>
    public static SyntaxNode? Create(NodeKind kind, GreenNode green, SyntaxTree tree, int offset)
    {
        var factory = Get(kind);
        return factory?.Invoke(green, tree, offset);
    }

    /// <summary>
    ///     判断指定节点类型是否已注册
    /// </summary>
    public static bool IsRegistered(NodeKind kind)
    {
        return Factories.ContainsKey(kind.Value);
    }

    /// <summary>
    ///     清空所有注册
    /// </summary>
    public static void Clear()
    {
        Factories.Clear();
    }
}