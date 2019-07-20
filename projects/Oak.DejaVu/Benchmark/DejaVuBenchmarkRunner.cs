using System.Diagnostics;
using System.Text;
using Oak.DejaVu.CodeGen;
using Oak.DejaVu.Ecosystem;
using Oak.DejaVu.Optimizer;

namespace Oak.DejaVu.Benchmark;

/// <summary>
///     DejaVu 性能基准运行器——编译速度、渲染吞吐量、代码生成质量基准。
/// </summary>
public sealed class DejaVuBenchmarkRunner
{
    /// <summary>
    ///     运行全部基准测试
    /// </summary>
    /// <returns>基准报告</returns>
    public BenchmarkReport RunAll()
    {
        var report = new BenchmarkReport
        {
            RunAt = DateTimeOffset.UtcNow,
            MachineInfo = $"{Environment.MachineName} | {Environment.OSVersion} | .NET {Environment.Version}"
        };

        report.CompileBenchmarks.AddRange(RunCompileBenchmarks());
        report.RenderBenchmarks.AddRange(RunRenderBenchmarks());
        report.CodeGenBenchmarks.AddRange(RunCodeGenBenchmarks());

        return report;
    }

    /// <summary>
    ///     编译性能基准
    /// </summary>
    public List<BenchmarkEntry> RunCompileBenchmarks()
    {
        var entries = new List<BenchmarkEntry>
        {
            Benchmark("编译-简单变量", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.Compile("{{ name }}");
            }),
            Benchmark("编译-条件+循环", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.Compile("{% if show %}{% loop item in items %}{{ item }}{% end %}{% end %}");
            }),
            Benchmark("编译-管道过滤器", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.Compile("{{ name |> uppercase |> trim |> truncate:30 }}");
            }),
            Benchmark("编译-模板继承", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.Compile("{% extends \"layout.djv\" %}{% block content %}Hello{% end %}");
            }),
            Benchmark("编译-完整页面", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.Compile(GenerateFullPageTemplate());
            }),
            Benchmark("编译-带符号解析", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.Compile("{% let x = 1 %}{% loop item in items %}{{ item }}{% end %}", emitSymbolTable: true);
            }),
            Benchmark("编译-带类型检查", () =>
            {
                var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
                compiler.CheckTypes("{% let x = 1 %}{% if show %}{{ x }}{% end %}");
            })
        };

        return entries;
    }

    /// <summary>
    ///     渲染性能基准
    /// </summary>
    public List<BenchmarkEntry> RunRenderBenchmarks()
    {
        var entries = new List<BenchmarkEntry>();
        var context = new Dictionary<string, object>
        {
            ["title"] = "Benchmark",
            ["name"] = "World",
            ["items"] = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList(),
            ["show"] = true,
            ["count"] = 42
        };

        entries.Add(Benchmark("渲染-简单变量(编译+渲染)", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            var compiled = compiler.Compile("Hello {{ name }}!", emitRenderFunc: true);
            compiled.RenderFunc?.Invoke(context);
        }));

        entries.Add(Benchmark("渲染-条件+循环(编译+渲染)", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            var compiled = compiler.Compile("{% if show %}{% loop item in items %}{{ item }}{% end %}{% end %}", emitRenderFunc: true);
            compiled.RenderFunc?.Invoke(context);
        }));

        entries.Add(Benchmark("渲染-管道过滤器(编译+渲染)", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            var compiled = compiler.Compile("{{ name |> uppercase |> trim }}", emitRenderFunc: true);
            compiled.RenderFunc?.Invoke(context);
        }));

        entries.Add(Benchmark("渲染-预编译简单变量", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            var compiled = compiler.Compile("Hello {{ name }}!", emitRenderFunc: true);
            var renderFunc = compiled.RenderFunc!;
            for (var i = 0; i < 10; i++)
            {
                renderFunc(context);
            }
        }));

        return entries;
    }

    /// <summary>
    ///     代码生成基准
    /// </summary>
    public List<BenchmarkEntry> RunCodeGenBenchmarks()
    {
        var entries = new List<BenchmarkEntry>();
        var source = GenerateFullPageTemplate();

        entries.Add(Benchmark("代码生成-TypeScript", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            compiler.CompileToTypeScript(source);
        }));

        entries.Add(Benchmark("代码生成-Java", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            compiler.CompileToJava(source);
        }));

        entries.Add(Benchmark("代码生成-接口推导", () =>
        {
            var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
            compiler.InferTypeScriptInterface(source);
        }));

        return entries;
    }

    /// <summary>
    ///     生成基准报告文本
    /// </summary>
    public static string GenerateReport(BenchmarkReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== DejaVu 性能基准报告 ===");
        sb.AppendLine($"运行时间: {report.RunAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"运行环境: {report.MachineInfo}");
        sb.AppendLine();

        PrintSection(sb, "编译性能", report.CompileBenchmarks);
        PrintSection(sb, "渲染性能", report.RenderBenchmarks);
        PrintSection(sb, "代码生成性能", report.CodeGenBenchmarks);

        sb.AppendLine("--- 汇总 ---");
        var all = report.CompileBenchmarks.Concat(report.RenderBenchmarks).Concat(report.CodeGenBenchmarks).ToList();
        sb.AppendLine($"总基准数: {all.Count}");
        sb.AppendLine($"平均耗时: {all.Average(e => e.AvgMs):F3}ms");
        sb.AppendLine($"最慢基准: {all.OrderByDescending(e => e.AvgMs).First().Name} ({all.Max(e => e.AvgMs):F3}ms)");
        sb.AppendLine($"最快基准: {all.OrderBy(e => e.AvgMs).First().Name} ({all.Min(e => e.AvgMs):F3}ms)");

        return sb.ToString();
    }

    private static void PrintSection(StringBuilder sb, string title, List<BenchmarkEntry> entries)
    {
        sb.AppendLine($"--- {title} ---");
        sb.AppendLine($"  {"名称",-30} {"迭代",6} {"平均(ms)",12} {"最小(ms)",12} {"最大(ms)",12} {"P95(ms)",12}");
        foreach (var entry in entries)
        {
            sb.AppendLine($"  {entry.Name,-30} {entry.Iterations,6} {entry.AvgMs,12:F3} {entry.MinMs,12:F3} {entry.MaxMs,12:F3} {entry.P95Ms,12:F3}");
        }

        sb.AppendLine();
    }

    private static BenchmarkEntry Benchmark(string name, Action action, int iterations = 100)
    {
        var sw = new Stopwatch();
        var times = new List<double>();

        for (var i = 0; i < iterations; i++)
        {
            sw.Restart();
            action();
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        times.Sort();

        return new BenchmarkEntry
        {
            Name = name,
            Iterations = iterations,
            AvgMs = times.Average(),
            MinMs = times[0],
            MaxMs = times[^1],
            P95Ms = times[(int)(times.Count * 0.95)],
            MedianMs = times[times.Count / 2]
        };
    }

    private static string GenerateFullPageTemplate()
    {
        return """
               <html>
               <head><title>{{ title }}</title></head>
               <body>
               <header>{% block header %}Default Header{% end %}</header>
               <nav>{% block nav %}{% loop item in nav_items %}<a href="{{ item.url }}">{{ item.label }}</a>{% end %}{% end %}</nav>
               <main>{% block content %}{% if show_content %}{% loop post in posts %}<article><h2>{{ post.title }}</h2><p>{{ post.body |> truncate:200 }}</p><span>{{ post.date |> date }}</span></article>{% end %}{% else %}<p>No content</p>{% end %}{% end %}</main>
               <aside>{% block sidebar %}{% let recent = recent_posts %}{% loop item in recent %}<a href="{{ item.url }}">{{ item.title }}</a>{% end %}{% end %}</aside>
               <footer>{% block footer %}&copy; 2026{% end %}</footer>
               </body>
               </html>
               """;
    }
}

/// <summary>
///     基准报告
/// </summary>
public sealed class BenchmarkReport
{
    /// <summary>
    ///     运行时间
    /// </summary>
    public DateTimeOffset RunAt { get; init; }

    /// <summary>
    ///     运行环境
    /// </summary>
    public string MachineInfo { get; init; } = string.Empty;

    /// <summary>
    ///     编译基准
    /// </summary>
    public List<BenchmarkEntry> CompileBenchmarks { get; init; } = [];

    /// <summary>
    ///     渲染基准
    /// </summary>
    public List<BenchmarkEntry> RenderBenchmarks { get; init; } = [];

    /// <summary>
    ///     代码生成基准
    /// </summary>
    public List<BenchmarkEntry> CodeGenBenchmarks { get; init; } = [];
}

/// <summary>
///     基准条目
/// </summary>
public sealed class BenchmarkEntry
{
    /// <summary>
    ///     基准名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     迭代次数
    /// </summary>
    public int Iterations { get; init; }

    /// <summary>
    ///     平均耗时（毫秒）
    /// </summary>
    public double AvgMs { get; init; }

    /// <summary>
    ///     最小耗时（毫秒）
    /// </summary>
    public double MinMs { get; init; }

    /// <summary>
    ///     最大耗时（毫秒）
    /// </summary>
    public double MaxMs { get; init; }

    /// <summary>
    ///     P95 耗时（毫秒）
    /// </summary>
    public double P95Ms { get; init; }

    /// <summary>
    ///     中位数耗时（毫秒）
    /// </summary>
    public double MedianMs { get; init; }
}
