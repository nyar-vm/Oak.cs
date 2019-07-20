using System.Diagnostics;
using System.Text;
using Oak.DejaVu.Expressions;
using Oak.Diagnostics;

namespace Oak.DejaVu.Debug;

/// <summary>
///     模板调试器——源码映射、渲染追踪、性能剖析。
/// </summary>
public sealed class TemplateDebugger
{
    private readonly List<TraceEntry> _traceEntries = [];
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    ///     是否启用渲染追踪
    /// </summary>
    public bool EnableTracing { get; init; } = true;

    /// <summary>
    ///     是否启用性能剖析
    /// </summary>
    public bool EnableProfiling { get; init; } = true;

    /// <summary>
    ///     追踪条目
    /// </summary>
    public IReadOnlyList<TraceEntry> TraceEntries => _traceEntries;

    /// <summary>
    ///     开始追踪
    /// </summary>
    public void StartTrace()
    {
        _traceEntries.Clear();
        _stopwatch.Restart();
    }

    /// <summary>
    ///     停止追踪
    /// </summary>
    public void StopTrace()
    {
        _stopwatch.Stop();
    }

    /// <summary>
    ///     记录节点追踪
    /// </summary>
    /// <param name="nodeType">节点类型</param>
    /// <param name="sourceLine">源码行号</param>
    /// <param name="sourceColumn">源码列号</param>
    /// <param name="detail">详细信息</param>
    /// <param name="elapsedMs">耗时（毫秒）</param>
    public void Trace(string nodeType, int sourceLine, int sourceColumn, string detail, double elapsedMs = 0)
    {
        if (!EnableTracing) return;

        _traceEntries.Add(new TraceEntry
        {
            NodeType = nodeType,
            SourceLine = sourceLine,
            SourceColumn = sourceColumn,
            Detail = detail,
            ElapsedMs = elapsedMs,
            Timestamp = _stopwatch.Elapsed.TotalMilliseconds
        });
    }

    /// <summary>
    ///     生成追踪报告
    /// </summary>
    public string GenerateTraceReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== DejaVu 模板渲染追踪报告 ===");
        sb.AppendLine($"总条目数: {_traceEntries.Count}");
        sb.AppendLine($"总耗时: {_stopwatch.Elapsed.TotalMilliseconds:F2}ms");
        sb.AppendLine();

        if (_traceEntries.Count == 0)
        {
            sb.AppendLine("（无追踪数据）");
            return sb.ToString();
        }

        sb.AppendLine("--- 节点执行明细 ---");
        foreach (var entry in _traceEntries)
        {
            var location = entry.SourceLine > 0 ? $"L{entry.SourceLine}:{entry.SourceColumn}" : "未知位置";
            var elapsed = entry.ElapsedMs > 0 ? $" [{entry.ElapsedMs:F3}ms]" : "";
            sb.AppendLine($"  [{entry.NodeType}] {location} {entry.Detail}{elapsed}");
        }

        if (EnableProfiling)
        {
            sb.AppendLine();
            sb.AppendLine("--- 性能剖析 ---");

            var profile = _traceEntries
                .Where(e => e.ElapsedMs > 0)
                .GroupBy(e => e.NodeType)
                .Select(g => new ProfileEntry
                {
                    NodeType = g.Key,
                    Count = g.Count(),
                    TotalMs = g.Sum(e => e.ElapsedMs),
                    AvgMs = g.Average(e => e.ElapsedMs),
                    MaxMs = g.Max(e => e.ElapsedMs)
                })
                .OrderByDescending(p => p.TotalMs)
                .ToList();

            if (profile.Count > 0)
            {
                sb.AppendLine($"  {"类型",-15} {"次数",6} {"总耗时(ms)",12} {"平均(ms)",12} {"最大(ms)",12}");
                foreach (var p in profile)
                {
                    sb.AppendLine($"  {p.NodeType,-15} {p.Count,6} {p.TotalMs,12:F3} {p.AvgMs,12:F3} {p.MaxMs,12:F3}");
                }
            }
            else
            {
                sb.AppendLine("（无性能数据，启用 EnableProfiling 以收集）");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成数据上下文快照
    /// </summary>
    public static string GenerateContextSnapshot(IDictionary<string, object> context, int maxDepth = 3)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 数据上下文快照 ===");

        foreach (var (key, value) in context)
        {
            sb.AppendLine($"  {key}: {FormatValue(value, maxDepth)}");
        }

        return sb.ToString();
    }

    private static string FormatValue(object? value, int depth, int indent = 0)
    {
        if (value == null) return "null";

        var prefix = new string(' ', indent * 2);

        if (depth <= 0) return $"{prefix}{value.GetType().Name}...";

        switch (value)
        {
            case string s:
                return s.Length > 50 ? $"\"{s[..50]}...\"" : $"\"{s}\"";
            case bool b:
                return b ? "true" : "false";
            case double d:
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case int i:
                return i.ToString();
            case IDictionary<string, object> dict:
                var dictSb = new StringBuilder();
                dictSb.AppendLine("{");
                foreach (var (k, v) in dict.Take(10))
                {
                    dictSb.AppendLine($"{prefix}  {k}: {FormatValue(v, depth - 1, indent + 1)}");
                }

                if (dict.Count > 10) dictSb.AppendLine($"{prefix}  ... ({dict.Count - 10} more)");
                dictSb.Append($"{prefix}}}");
                return dictSb.ToString();
            case System.Collections.ICollection collection:
                return $"[{collection.Count} items]";
            default:
                return value.ToString() ?? value.GetType().Name;
        }
    }
}

/// <summary>
///     追踪条目
/// </summary>
public sealed class TraceEntry
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>
    ///     源码行号
    /// </summary>
    public int SourceLine { get; init; }

    /// <summary>
    ///     源码列号
    /// </summary>
    public int SourceColumn { get; init; }

    /// <summary>
    ///     详细信息
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    ///     耗时（毫秒）
    /// </summary>
    public double ElapsedMs { get; init; }

    /// <summary>
    ///     时间戳（相对于追踪开始的毫秒数）
    /// </summary>
    public double Timestamp { get; init; }
}

/// <summary>
///     性能剖析条目
/// </summary>
public sealed class ProfileEntry
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>
    ///     执行次数
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    ///     总耗时（毫秒）
    /// </summary>
    public double TotalMs { get; init; }

    /// <summary>
    ///     平均耗时（毫秒）
    /// </summary>
    public double AvgMs { get; init; }

    /// <summary>
    ///     最大耗时（毫秒）
    /// </summary>
    public double MaxMs { get; init; }
}

/// <summary>
///     源码映射——编译后代码位置 → 模板源码位置
/// </summary>
public sealed class SourceMap
{
    private readonly List<SourceMapping> _mappings = [];

    /// <summary>
    ///     映射条目
    /// </summary>
    public IReadOnlyList<SourceMapping> Mappings => _mappings;

    /// <summary>
    ///     添加映射
    /// </summary>
    public void AddMapping(int generatedLine, int generatedColumn, int sourceLine, int sourceColumn, string sourceFile = "")
    {
        _mappings.Add(new SourceMapping
        {
            GeneratedLine = generatedLine,
            GeneratedColumn = generatedColumn,
            SourceLine = sourceLine,
            SourceColumn = sourceColumn,
            SourceFile = sourceFile
        });
    }

    /// <summary>
    ///     从生成代码位置查找源码位置
    /// </summary>
    public SourceMapping? FindSourcePosition(int generatedLine, int generatedColumn)
    {
        SourceMapping? best = null;

        foreach (var mapping in _mappings)
        {
            if (mapping.GeneratedLine < generatedLine ||
                (mapping.GeneratedLine == generatedLine && mapping.GeneratedColumn <= generatedColumn))
            {
                if (best == null ||
                    mapping.GeneratedLine > best.GeneratedLine ||
                    (mapping.GeneratedLine == best.GeneratedLine && mapping.GeneratedColumn > best.GeneratedColumn))
                {
                    best = mapping;
                }
            }
        }

        return best;
    }

    /// <summary>
    ///     从模板 AST 构建源码映射
    /// </summary>
    public static SourceMap BuildFromNodes(IReadOnlyList<DejaVuTemplateNode> nodes, string sourceFile = "")
    {
        var sourceMap = new SourceMap();
        var generatedLine = 1;

        foreach (var node in nodes)
        {
            BuildMappingForNode(sourceMap, node, ref generatedLine, sourceFile);
        }

        return sourceMap;
    }

    private static void BuildMappingForNode(SourceMap sourceMap, DejaVuTemplateNode node, ref int generatedLine, string sourceFile)
    {
        var sourceLine = node.SourceLine > 0 ? node.SourceLine : 0;
        var sourceColumn = node.SourceColumn > 0 ? node.SourceColumn : 0;

        sourceMap.AddMapping(generatedLine, 0, sourceLine, sourceColumn, sourceFile);

        switch (node)
        {
            case DejaVuIfNode ifNode:
                generatedLine++;
                foreach (var child in ifNode.Children)
                {
                    BuildMappingForNode(sourceMap, child, ref generatedLine, sourceFile);
                }

                generatedLine++;
                break;
            case DejaVuLoopNode loopNode:
                generatedLine++;
                foreach (var child in loopNode.Children)
                {
                    BuildMappingForNode(sourceMap, child, ref generatedLine, sourceFile);
                }

                generatedLine++;
                break;
            case DejaVuBlockNode blockNode:
                foreach (var child in blockNode.Children)
                {
                    BuildMappingForNode(sourceMap, child, ref generatedLine, sourceFile);
                }

                break;
            case DejaVuRawNode rawNode:
                foreach (var child in rawNode.Children)
                {
                    BuildMappingForNode(sourceMap, child, ref generatedLine, sourceFile);
                }

                break;
        }

        generatedLine++;
    }
}

/// <summary>
///     源码映射条目
/// </summary>
public sealed class SourceMapping
{
    /// <summary>
    ///     生成代码行号
    /// </summary>
    public int GeneratedLine { get; init; }

    /// <summary>
    ///     生成代码列号
    /// </summary>
    public int GeneratedColumn { get; init; }

    /// <summary>
    ///     源码行号
    /// </summary>
    public int SourceLine { get; init; }

    /// <summary>
    ///     源码列号
    /// </summary>
    public int SourceColumn { get; init; }

    /// <summary>
    ///     源码文件路径
    /// </summary>
    public string SourceFile { get; init; } = string.Empty;
}
