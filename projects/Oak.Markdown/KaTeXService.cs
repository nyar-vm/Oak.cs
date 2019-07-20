using System.Reflection;
using System.Text;
using Jint;

namespace Oak.Markdown;

/// <summary>
///     KaTeX 服务端渲染服务，使用 Jint 执行嵌入的 katex.min.js 将 LaTeX 渲染为 HTML
/// </summary>
public sealed class KaTeXService
{
    private Engine? _engine;
    private bool _initialized;
    private readonly object _lock = new();
    private string? _cssContent;

    /// <summary>
    ///     将 LaTeX 公式渲染为 HTML 字符串
    /// </summary>
    /// <param name="latex">LaTeX 公式内容</param>
    /// <param name="displayMode">是否为块级显示模式</param>
    /// <returns>渲染后的 HTML 字符串</returns>
    public string RenderToString(string latex, bool displayMode)
    {
        EnsureInitialized();

        if (_engine == null)
        {
            return displayMode ? $"$${latex}$$" : $"${latex}$";
        }

        try
        {
            var js = new StringBuilder();
            js.Append("katex.renderToString(");
            js.Append(EscapeJsString(latex));
            js.Append(", {displayMode: ");
            js.Append(displayMode ? "true" : "false");
            js.Append(", throwOnError: false})");

            var result = _engine.Evaluate(js.ToString());
            return result.AsString();
        }
        catch
        {
            return displayMode ? $"$${EscapeHtml(latex)}$$" : $"${EscapeHtml(latex)}$";
        }
    }

    /// <summary>
    ///     获取 KaTeX CSS 内容，用于在页面中内联或外链引用
    /// </summary>
    public string GetCssContent()
    {
        if (_cssContent != null) return _cssContent;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Oak.Markdown.Resources.katex.min.css";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            _cssContent = reader.ReadToEnd();
        }

        return _cssContent ?? "";
    }

    /// <summary>
    ///     获取 KaTeX CSS 的 CDN 链接
    /// </summary>
    public static string GetCssCdnUrl()
    {
        return "https://cdn.jsdelivr.net/npm/katex@0.16.11/dist/katex.min.css";
    }

    /// <summary>
    ///     获取 KaTeX CSS 的 link 标签
    /// </summary>
    public static string GetCssLinkTag()
    {
        return $"<link rel=\"stylesheet\" href=\"{GetCssCdnUrl()}\">";
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Oak.Markdown.Resources.katex.min.js";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _engine = null;
                    return;
                }

                using var reader = new StreamReader(stream);
                var katexJs = reader.ReadToEnd();

                _engine = new Engine(options => options.Strict());
                _engine.Execute(katexJs);
            }
            catch
            {
                _engine = null;
            }
            finally
            {
                _initialized = true;
            }
        }
    }

    private static string EscapeJsString(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        sb.Append('"');

        foreach (var c in s)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
