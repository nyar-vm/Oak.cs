using System.Collections;
using System.Globalization;

namespace Oak.DejaVu.Filters;

/// <summary>
///     过滤器注册表
/// </summary>
public sealed class FilterRegistry
{
    private readonly Dictionary<string, IFilter> _filters;

    public FilterRegistry()
    {
        _filters = new Dictionary<string, IFilter>();
        RegisterDefaultFilters();
    }

    /// <summary>
    ///     注册过滤器
    /// </summary>
    public void Register(string name, IFilter filter)
    {
        _filters[name] = filter;
    }

    /// <summary>
    ///     获取过滤器
    /// </summary>
    public IFilter? Get(string name)
    {
        return _filters.TryGetValue(name, out var filter) ? filter : null;
    }

    /// <summary>
    ///     检查过滤器是否已注册
    /// </summary>
    public bool HasFilter(string name)
    {
        return _filters.ContainsKey(name);
    }

    /// <summary>
    ///     应用过滤器
    /// </summary>
    public object? Apply(string name, object? value, object?[] arguments)
    {
        var filter = Get(name);
        return filter?.Apply(value, arguments);
    }

    /// <summary>
    ///     注册默认过滤器
    /// </summary>
    private void RegisterDefaultFilters()
    {
        // 字符串过滤器
        Register("uppercase", new DelegateFilter(value =>
            value?.ToString()?.ToUpperInvariant()));
        Register("lowercase", new DelegateFilter(value =>
            value?.ToString()?.ToLowerInvariant()));
        Register("trim", new DelegateFilter(value =>
            value?.ToString()?.Trim()));
        Register("length", new DelegateFilter(value =>
            value?.ToString()?.Length ?? 0));
        Register("reverse", new DelegateFilter(value =>
        {
            var str = value?.ToString();
            return str == null ? null : new string(str.Reverse().ToArray());
        }));

        // 数字过滤器
        Register("abs", new DelegateFilter(value =>
            value is double d ? Math.Abs(d) : value));
        Register("round", new DelegateFilter((value, args) =>
        {
            if (value is not double d) return value;
            var decimals = args.Length > 0 && args[0] is int i ? i : 0;
            return Math.Round(d, decimals);
        }));
        Register("floor", new DelegateFilter(value =>
            value is double d ? Math.Floor(d) : value));
        Register("ceil", new DelegateFilter(value =>
            value is double d ? Math.Ceiling(d) : value));

        // 集合过滤器
        Register("first", new DelegateFilter(value =>
        {
            if (value is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                return enumerator.MoveNext() ? enumerator.Current : null;
            }

            return null;
        }));
        Register("last", new DelegateFilter(value =>
        {
            if (value is IEnumerable enumerable)
            {
                object? last = null;
                foreach (var item in enumerable) last = item;
                return last;
            }

            return null;
        }));
        Register("count", new DelegateFilter(value =>
        {
            if (value is ICollection collection) return collection.Count;
            if (value is string str) return str.Length;
            return 0;
        }));
        Register("join", new DelegateFilter((value, args) =>
        {
            if (value is not IEnumerable enumerable) return value;
            var separator = args.Length > 0 ? args[0]?.ToString() ?? ", " : ", ";
            var items = new List<string>();
            foreach (var item in enumerable) items.Add(item?.ToString() ?? "");
            return string.Join(separator, items);
        }));

        // 日期过滤器
        Register("date", new DelegateFilter((value, args) =>
        {
            if (value is not DateTime dt) return value;
            var format = args.Length > 0 ? args[0]?.ToString() ?? "yyyy-MM-dd" : "yyyy-MM-dd";
            return dt.ToString(format, CultureInfo.InvariantCulture);
        }));
        Register("datetime", new DelegateFilter((value, args) =>
        {
            if (value is not DateTime dt) return value;
            var format = args.Length > 0 ? args[0]?.ToString() ?? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd HH:mm:ss";
            return dt.ToString(format, CultureInfo.InvariantCulture);
        }));

        // 默认值过滤器
        Register("default", new DelegateFilter((value, args) =>
        {
            if (value != null && !string.IsNullOrEmpty(value.ToString())) return value;
            return args.Length > 0 ? args[0] : null;
        }));

        // HTML 过滤器
        Register("escape", new DelegateFilter(value =>
        {
            var str = value?.ToString();
            if (str == null) return null;
            return str
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;");
        }));
        Register("safe", new DelegateFilter(value => value));
    }
}

/// <summary>
///     过滤器接口
/// </summary>
public interface IFilter
{
    /// <summary>
    ///     应用过滤器
    /// </summary>
    object? Apply(object? value, object?[] arguments);
}

/// <summary>
///     委托过滤器
/// </summary>
public sealed class DelegateFilter : IFilter
{
    private readonly Func<object?, object?[], object?> _func;

    public DelegateFilter(Func<object?, object?> func)
    {
        _func = (value, _) => func(value);
    }

    public DelegateFilter(Func<object?, object?[], object?> func)
    {
        _func = func;
    }

    public object? Apply(object? value, object?[] arguments)
    {
        return _func(value, arguments);
    }
}