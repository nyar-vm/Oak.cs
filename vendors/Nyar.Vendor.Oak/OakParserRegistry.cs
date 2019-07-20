using Oak.Parsing;

namespace Oak;

/// <summary>
///     Oak 解析器注册表，集中管理所有 Oak 解析器
/// </summary>
public sealed class OakParserRegistry
{
    private readonly Dictionary<string, object> _parsers = new();

    /// <summary>
    ///     注册解析器
    /// </summary>
    public void Register<TInput, TOutput>(string name, IParser<TInput, TOutput> parser)
    {
        _parsers[name] = parser;
    }

    /// <summary>
    ///     获取解析器
    /// </summary>
    public IParser<TInput, TOutput>? Get<TInput, TOutput>(string name)
    {
        if (_parsers.TryGetValue(name, out var parser) && parser is IParser<TInput, TOutput> typed) return typed;

        return null;
    }

    /// <summary>
    ///     获取字符串解析器
    /// </summary>
    public IStringParser<TOutput>? GetStringParser<TOutput>(string name)
    {
        return Get<string, TOutput>(name) as IStringParser<TOutput>;
    }

    /// <summary>
    ///     检查是否已注册
    /// </summary>
    public bool HasParser(string name)
    {
        return _parsers.ContainsKey(name);
    }

    /// <summary>
    ///     获取所有已注册的解析器名称
    /// </summary>
    public IReadOnlyList<string> GetRegisteredNames()
    {
        return _parsers.Keys.ToList();
    }
}