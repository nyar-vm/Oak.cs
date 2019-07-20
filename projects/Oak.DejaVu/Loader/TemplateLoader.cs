namespace Oak.DejaVu.Loader;

/// <summary>
///     模板加载器接口
/// </summary>
public interface ITemplateLoader
{
    /// <summary>
    ///     加载模板
    /// </summary>
    Task<string> LoadAsync(string path);

    /// <summary>
    ///     检查模板是否存在
    /// </summary>
    bool Exists(string path);
}

/// <summary>
///     文件系统模板加载器
/// </summary>
public sealed class FileSystemTemplateLoader : ITemplateLoader
{
    private readonly string _basePath;

    public FileSystemTemplateLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <inheritdoc />
    public async Task<string> LoadAsync(string path)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Template not found: {path}", fullPath);

        return await File.ReadAllTextAsync(fullPath);
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        var fullPath = Path.Combine(_basePath, path);
        return File.Exists(fullPath);
    }
}

/// <summary>
///     内存模板加载器
/// </summary>
public sealed class MemoryTemplateLoader : ITemplateLoader
{
    private readonly Dictionary<string, string> _templates;

    public MemoryTemplateLoader()
    {
        _templates = new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public Task<string> LoadAsync(string path)
    {
        if (!_templates.TryGetValue(path, out var content))
            throw new KeyNotFoundException($"Template not found: {path}");

        return Task.FromResult(content);
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        return _templates.ContainsKey(path);
    }

    /// <summary>
    ///     添加模板
    /// </summary>
    public void Add(string path, string content)
    {
        _templates[path] = content;
    }
}

/// <summary>
///     模板缓存
/// </summary>
public sealed class TemplateCache
{
    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly TimeSpan _defaultExpiration;

    public TemplateCache(TimeSpan? defaultExpiration = null)
    {
        _cache = new Dictionary<string, CacheEntry>();
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    ///     获取缓存的模板
    /// </summary>
    public string? Get(string path)
    {
        if (_cache.TryGetValue(path, out var entry))
        {
            if (entry.ExpirationTime > DateTime.UtcNow) return entry.Content;
            _cache.Remove(path);
        }

        return null;
    }

    /// <summary>
    ///     设置缓存
    /// </summary>
    public void Set(string path, string content, TimeSpan? expiration = null)
    {
        _cache[path] = new CacheEntry
        {
            Content = content,
            ExpirationTime = DateTime.UtcNow + (expiration ?? _defaultExpiration)
        };
    }

    /// <summary>
    ///     清除缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    private class CacheEntry
    {
        public string Content { get; init; } = string.Empty;
        public DateTime ExpirationTime { get; init; }
    }
}

/// <summary>
///     模板管理器
/// </summary>
public sealed class TemplateManager
{
    private readonly TemplateCache _cache;
    private readonly ITemplateLoader _loader;

    public TemplateManager(ITemplateLoader loader)
    {
        _loader = loader;
        _cache = new TemplateCache();
    }

    /// <summary>
    ///     加载模板
    /// </summary>
    public async Task<string> LoadAsync(string path)
    {
        // 检查缓存
        var cached = _cache.Get(path);
        if (cached != null) return cached;

        // 加载模板
        var content = await _loader.LoadAsync(path);

        // 缓存结果
        _cache.Set(path, content);

        return content;
    }

    /// <summary>
    ///     清除缓存
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}