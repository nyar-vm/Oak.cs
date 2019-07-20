using Oak.DejaVu.Expressions;
using Oak.DejaVu.Filters;
using Oak.DejaVu.Optimizer;

namespace Oak.DejaVu.CodeGen;

/// <summary>
///     模板标准库——跨语言一致的辅助函数定义。
///     每个辅助函数在 C#/TypeScript/Java 三种语言中行为一致。
/// </summary>
public sealed class TemplateStandardLibrary
{
    private readonly Dictionary<string, StandardHelper> _helpers = new();

    /// <summary>
    ///     创建模板标准库
    /// </summary>
    public TemplateStandardLibrary()
    {
        RegisterDefaultHelpers();
    }

    /// <summary>
    ///     获取所有标准辅助函数
    /// </summary>
    public IReadOnlyDictionary<string, StandardHelper> Helpers => _helpers;

    /// <summary>
    ///     获取标准辅助函数
    /// </summary>
    public StandardHelper? GetHelper(string name)
    {
        return _helpers.TryGetValue(name, out var helper) ? helper : null;
    }

    /// <summary>
    ///     注册标准辅助函数
    /// </summary>
    public void Register(string name, StandardHelper helper)
    {
        _helpers[name] = helper;
    }

    /// <summary>
    ///     检查辅助函数是否已注册
    /// </summary>
    public bool HasHelper(string name)
    {
        return _helpers.ContainsKey(name);
    }

    private void RegisterDefaultHelpers()
    {
        Register("formatDate", new StandardHelper(
            "formatDate",
            "格式化日期为指定格式",
            new HelperParameter("value", TemplateType.Any, "日期值"),
            new HelperParameter("format", TemplateType.String, "格式字符串，默认 yyyy-MM-dd"))
        {
            InputType = TemplateType.Any,
            OutputType = TemplateType.String,
            TsImplementation = "(v, fmt) => v instanceof Date ? formatDate(v, fmt ?? 'yyyy-MM-dd') : String(v)",
            JavaImplementation = "(v, fmt) -> v instanceof java.time.temporal.Temporal ? v.toString() : String.valueOf(v)"
        });

        Register("formatNumber", new StandardHelper(
            "formatNumber",
            "格式化数字为指定精度",
            new HelperParameter("value", TemplateType.Number, "数字值"),
            new HelperParameter("decimals", TemplateType.Number, "小数位数，默认 2"))
        {
            InputType = TemplateType.Number,
            OutputType = TemplateType.String,
            TsImplementation = "(v, d) => Number(v).toFixed(d ?? 2)",
            JavaImplementation = "(v, d) -> String.format(\"%.\" + (d != null ? ((Number)d).intValue() : 2) + \"f\", ((Number)v).doubleValue())"
        });

        Register("pluralize", new StandardHelper(
            "pluralize",
            "根据数量选择单数/复数形式",
            new HelperParameter("count", TemplateType.Number, "数量"),
            new HelperParameter("singular", TemplateType.String, "单数形式"),
            new HelperParameter("plural", TemplateType.String, "复数形式"))
        {
            InputType = TemplateType.Number,
            OutputType = TemplateType.String,
            TsImplementation = "(n, s, p) => n === 1 ? s : p",
            JavaImplementation = "(n, s, p) -> ((Number)n).intValue() == 1 ? s : p"
        });

        Register("i18n", new StandardHelper(
            "i18n",
            "国际化翻译——根据键名查找翻译文本",
            new HelperParameter("key", TemplateType.String, "翻译键名"))
        {
            InputType = TemplateType.String,
            OutputType = TemplateType.String,
            TsImplementation = "(key) => __i18n[key] ?? key",
            JavaImplementation = "(key) -> __i18n.getOrDefault(key, key)"
        });

        Register("truncateWords", new StandardHelper(
            "truncateWords",
            "按词数截断文本",
            new HelperParameter("text", TemplateType.String, "文本"),
            new HelperParameter("wordCount", TemplateType.Number, "保留词数，默认 30"))
        {
            InputType = TemplateType.String,
            OutputType = TemplateType.String,
            TsImplementation = "(t, n) => t.split(' ').slice(0, n ?? 30).join(' ')",
            JavaImplementation = "(t, n) -> { int limit = n != null ? ((Number)n).intValue() : 30; var words = t.toString().split(\" \"); return Arrays.stream(words).limit(limit).collect(Collectors.joining(\" \")); }"
        });

        Register("currency", new StandardHelper(
            "currency",
            "格式化为货币字符串",
            new HelperParameter("value", TemplateType.Number, "金额"),
            new HelperParameter("symbol", TemplateType.String, "货币符号，默认 ¥"))
        {
            InputType = TemplateType.Number,
            OutputType = TemplateType.String,
            TsImplementation = "(v, sym) => (sym ?? '¥') + Number(v).toFixed(2)",
            JavaImplementation = "(v, sym) -> (sym != null ? sym : \"¥\") + String.format(\"%.2f\", ((Number)v).doubleValue())"
        });

        Register("percentage", new StandardHelper(
            "percentage",
            "格式化为百分比字符串",
            new HelperParameter("value", TemplateType.Number, "数值（0-1）"),
            new HelperParameter("decimals", TemplateType.Number, "小数位数，默认 1"))
        {
            InputType = TemplateType.Number,
            OutputType = TemplateType.String,
            TsImplementation = "(v, d) => (Number(v) * 100).toFixed(d ?? 1) + '%'",
            JavaImplementation = "(v, d) -> String.format(\"%.\" + (d != null ? ((Number)d).intValue() : 1) + \"f%%\", ((Number)v).doubleValue() * 100)"
        });

        Register("stripTags", new StandardHelper(
            "stripTags",
            "移除 HTML 标签",
            new HelperParameter("html", TemplateType.String, "HTML 文本"))
        {
            InputType = TemplateType.String,
            OutputType = TemplateType.String,
            TsImplementation = "(html) => String(html).replace(/<[^>]*>/g, '')",
            JavaImplementation = "(html) -> html.toString().replaceAll(\"<[^>]*>\", \"\")"
        });

        Register("urlEncode", new StandardHelper(
            "urlEncode",
            "URL 编码",
            new HelperParameter("value", TemplateType.String, "待编码字符串"))
        {
            InputType = TemplateType.String,
            OutputType = TemplateType.String,
            TsImplementation = "(v) => encodeURIComponent(String(v))",
            JavaImplementation = "(v) -> java.net.URLEncoder.encode(v.toString(), java.nio.charset.StandardCharsets.UTF_8)"
        });

        Register("jsonEncode", new StandardHelper(
            "jsonEncode",
            "JSON 编码——将值序列化为 JSON 字符串",
            new HelperParameter("value", TemplateType.Any, "待编码值"))
        {
            InputType = TemplateType.Any,
            OutputType = TemplateType.String,
            TsImplementation = "(v) => JSON.stringify(v)",
            JavaImplementation = "(v) -> com.fasterxml.jackson.databind.ObjectMapper().writeValueAsString(v)"
        });

        Register("defaultIfEmpty", new StandardHelper(
            "defaultIfEmpty",
            "值为空时返回默认值",
            new HelperParameter("value", TemplateType.Any, "值"),
            new HelperParameter("defaultValue", TemplateType.Any, "默认值"))
        {
            InputType = TemplateType.Any,
            OutputType = TemplateType.Any,
            TsImplementation = "(v, d) => (v === null || v === undefined || v === '') ? d : v",
            JavaImplementation = "(v, d) -> (v == null || \"\".equals(v)) ? d : v"
        });

        Register("sortBy", new StandardHelper(
            "sortBy",
            "按属性排序集合",
            new HelperParameter("collection", TemplateType.Array, "集合"),
            new HelperParameter("property", TemplateType.String, "排序属性名"))
        {
            InputType = TemplateType.Array,
            OutputType = TemplateType.Array,
            TsImplementation = "(arr, prop) => [...arr].sort((a, b) => a[prop] < b[prop] ? -1 : 1)",
            JavaImplementation = "(arr, prop) -> ((List<?>) arr).stream().sorted(Comparator.comparing(o -> String.valueOf(((Map<?, ?>) o).get(prop)))).collect(Collectors.toList())"
        });
    }
}

/// <summary>
///     标准辅助函数定义
/// </summary>
public sealed class StandardHelper
{
    /// <summary>
    ///     函数名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     函数描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     参数列表
    /// </summary>
    public HelperParameter[] Parameters { get; }

    /// <summary>
    ///     输入类型
    /// </summary>
    public TemplateType InputType { get; init; } = TemplateType.Any;

    /// <summary>
    ///     输出类型
    /// </summary>
    public TemplateType OutputType { get; init; } = TemplateType.Any;

    /// <summary>
    ///     TypeScript 实现
    /// </summary>
    public string TsImplementation { get; init; } = string.Empty;

    /// <summary>
    ///     Java 实现
    /// </summary>
    public string JavaImplementation { get; init; } = string.Empty;

    /// <summary>
    ///     创建标准辅助函数定义
    /// </summary>
    public StandardHelper(string name, string description, params HelperParameter[] parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}

/// <summary>
///     辅助函数参数定义
/// </summary>
public sealed class HelperParameter
{
    /// <summary>
    ///     参数名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     参数类型
    /// </summary>
    public TemplateType Type { get; }

    /// <summary>
    ///     参数描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     创建辅助函数参数定义
    /// </summary>
    public HelperParameter(string name, TemplateType type, string description)
    {
        Name = name;
        Type = type;
        Description = description;
    }
}
