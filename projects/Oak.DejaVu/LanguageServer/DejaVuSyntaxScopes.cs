namespace Oak.DejaVu.LanguageServer;

/// <summary>
///     DejaVu 语法高亮元数据——TextMate 兼容的 scope 分类。
///     用于 VSCode 扩展的 language configuration 和 TextMate 语法定义。
/// </summary>
public sealed class DejaVuSyntaxScopes
{
    /// <summary>
    ///     文件扩展名
    /// </summary>
    public static readonly string[] FileExtensions = [".djv", ".dejavu"];

    /// <summary>
    ///     语言 ID
    /// </summary>
    public const string LanguageId = "dejavu";

    /// <summary>
    ///     语言显示名称
    /// </summary>
    public const string LanguageName = "DejaVu Template";

    /// <summary>
    ///     获取所有 TextMate scope 定义
    /// </summary>
    public static List<ScopeDefinition> GetAllScopes()
    {
        return
        [
            new ScopeDefinition("comment.block.dejavu", "{%-- --%}", "模板注释"),
            new ScopeDefinition("keyword.control.dejavu", "if/else/loop/let/with/block/extends/include/raw/end", "控制关键字"),
            new ScopeDefinition("keyword.operator.pipe.dejavu", "|>", "管道运算符"),
            new ScopeDefinition("keyword.operator.colon.dejavu", ":", "参数分隔符"),
            new ScopeDefinition("variable.other.dejavu", "变量引用", "模板变量"),
            new ScopeDefinition("variable.other.loop-item.dejavu", "loop item 变量", "循环迭代变量"),
            new ScopeDefinition("variable.other.loop-index.dejavu", "index", "循环索引变量"),
            new ScopeDefinition("entity.name.function.filter.dejavu", "过滤器名称", "管道过滤器"),
            new ScopeDefinition("entity.name.function.helper.dejavu", "辅助函数名称", "标准库辅助函数"),
            new ScopeDefinition("entity.name.type.block.dejavu", "block 名称", "块定义名称"),
            new ScopeDefinition("string.quoted.double.dejavu", "\"...\"", "双引号字符串"),
            new ScopeDefinition("string.quoted.single.dejavu", "'...'", "单引号字符串"),
            new ScopeDefinition("constant.numeric.dejavu", "数字字面量", "数字"),
            new ScopeDefinition("constant.language.boolean.dejavu", "true/false", "布尔值"),
            new ScopeDefinition("constant.language.null.dejavu", "null", "空值"),
            new ScopeDefinition("punctuation.definition.tag.begin.doki.dejavu", "{%", "Doki 标签开始"),
            new ScopeDefinition("punctuation.definition.tag.end.doki.dejavu", "%}", "Doki 标签结束"),
            new ScopeDefinition("punctuation.definition.tag.begin.dora.dejavu", "<%", "Dora 标签开始"),
            new ScopeDefinition("punctuation.definition.tag.end.dora.dejavu", "%>", "Dora 标签结束"),
            new ScopeDefinition("punctuation.definition.output.begin.dejavu", "{{", "输出标签开始"),
            new ScopeDefinition("punctuation.definition.output.end.dejavu", "}}", "输出标签结束"),
            new ScopeDefinition("meta.tag.template.dejavu", "{% ... %}", "模板标签"),
            new ScopeDefinition("meta.output.template.dejavu", "{{ ... }}", "输出标签"),
            new ScopeDefinition("support.function.filter.dejavu", "内置过滤器", "过滤器函数"),
            new ScopeDefinition("support.function.helper.dejavu", "标准库辅助函数", "Helper 函数")
        ];
    }

    /// <summary>
    ///     获取语言配置（括号匹配、注释切换等）
    /// </summary>
    public static LanguageConfiguration GetLanguageConfiguration()
    {
        return new LanguageConfiguration
        {
            Comments = new CommentConfiguration
            {
                LineComment = null,
                BlockComment = new CommentPair { Open = "{%--", Close = "--%}" }
            },
            Brackets =
            [
                new BracketPair { Open = "{", Close = "}" },
                new BracketPair { Open = "[", Close = "]" },
                new BracketPair { Open = "(", Close = ")" },
                new BracketPair { Open = "{%", Close = "%}" },
                new BracketPair { Open = "{{", Close = "}}" }
            ],
            AutoClosingPairs =
            [
                new AutoClosingPair { Open = "{", Close = "}" },
                new AutoClosingPair { Open = "[", Close = "]" },
                new AutoClosingPair { Open = "(", Close = ")" },
                new AutoClosingPair { Open = "\"", Close = "\"" },
                new AutoClosingPair { Open = "'", Close = "'" }
            ],
            SurroundingPairs =
            [
                new SurroundingPair { Open = "{", Close = "}" },
                new SurroundingPair { Open = "[", Close = "]" },
                new SurroundingPair { Open = "(", Close = ")" },
                new SurroundingPair { Open = "\"", Close = "\"" },
                new SurroundingPair { Open = "'", Close = "'" }
            ],
            WordPattern = @"[a-zA-Z_]\w*",
            IndentationRules = new IndentationRules
            {
                IncreaseIndentPattern = @"\{%\s*(if|loop|let|with|block|raw|match)",
                DecreaseIndentPattern = @"\{%\s*(end|else|else\s+if)"
            },
            Folding = new FoldingConfiguration
            {
                Markers = new FoldingMarkers
                {
                    Start = @"\{%\s*(if|loop|let|with|block|raw|match)",
                    End = @"\{%\s*end\s*%\}"
                }
            }
        };
    }
}

/// <summary>
///     Scope 定义
/// </summary>
public sealed class ScopeDefinition
{
    /// <summary>
    ///     TextMate scope 名称
    /// </summary>
    public string Scope { get; }

    /// <summary>
    ///     匹配模式描述
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    ///     中文描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     创建 Scope 定义
    /// </summary>
    public ScopeDefinition(string scope, string pattern, string description)
    {
        Scope = scope;
        Pattern = pattern;
        Description = description;
    }
}

/// <summary>
///     语言配置
/// </summary>
public sealed class LanguageConfiguration
{
    /// <summary>
    ///     注释配置
    /// </summary>
    public CommentConfiguration Comments { get; init; } = new();

    /// <summary>
    ///     括号对
    /// </summary>
    public List<BracketPair> Brackets { get; init; } = [];

    /// <summary>
    ///     自动关闭对
    /// </summary>
    public List<AutoClosingPair> AutoClosingPairs { get; init; } = [];

    /// <summary>
    ///     环绕对
    /// </summary>
    public List<SurroundingPair> SurroundingPairs { get; init; } = [];

    /// <summary>
    ///     单词模式
    /// </summary>
    public string WordPattern { get; init; } = string.Empty;

    /// <summary>
    ///     缩进规则
    /// </summary>
    public IndentationRules IndentationRules { get; init; } = new();

    /// <summary>
    ///     折叠配置
    /// </summary>
    public FoldingConfiguration Folding { get; init; } = new();
}

/// <summary>
///     注释配置
/// </summary>
public sealed class CommentConfiguration
{
    /// <summary>
    ///     行注释（DejaVu 无行注释）
    /// </summary>
    public string? LineComment { get; init; }

    /// <summary>
    ///     块注释
    /// </summary>
    public CommentPair BlockComment { get; init; } = new();
}

/// <summary>
///     注释对
/// </summary>
public sealed class CommentPair
{
    /// <summary>
    ///     开始标记
    /// </summary>
    public string Open { get; init; } = string.Empty;

    /// <summary>
    ///     结束标记
    /// </summary>
    public string Close { get; init; } = string.Empty;
}

/// <summary>
///     括号对
/// </summary>
public sealed class BracketPair
{
    /// <summary>
    ///     开括号
    /// </summary>
    public string Open { get; init; } = string.Empty;

    /// <summary>
    ///     闭括号
    /// </summary>
    public string Close { get; init; } = string.Empty;
}

/// <summary>
///     自动关闭对
/// </summary>
public sealed class AutoClosingPair
{
    /// <summary>
    ///     开字符
    /// </summary>
    public string Open { get; init; } = string.Empty;

    /// <summary>
    ///     闭字符
    /// </summary>
    public string Close { get; init; } = string.Empty;
}

/// <summary>
///     环绕对
/// </summary>
public sealed class SurroundingPair
{
    /// <summary>
    ///     开字符
    /// </summary>
    public string Open { get; init; } = string.Empty;

    /// <summary>
    ///     闭字符
    /// </summary>
    public string Close { get; init; } = string.Empty;
}

/// <summary>
///     缩进规则
/// </summary>
public sealed class IndentationRules
{
    /// <summary>
    ///     增加缩进模式
    /// </summary>
    public string IncreaseIndentPattern { get; init; } = string.Empty;

    /// <summary>
    ///     减少缩进模式
    /// </summary>
    public string DecreaseIndentPattern { get; init; } = string.Empty;
}

/// <summary>
///     折叠配置
/// </summary>
public sealed class FoldingConfiguration
{
    /// <summary>
    ///     折叠标记
    /// </summary>
    public FoldingMarkers Markers { get; init; } = new();
}

/// <summary>
///     折叠标记
/// </summary>
public sealed class FoldingMarkers
{
    /// <summary>
    ///     折叠开始模式
    /// </summary>
    public string Start { get; init; } = string.Empty;

    /// <summary>
    ///     折叠结束模式
    /// </summary>
    public string End { get; init; } = string.Empty;
}
