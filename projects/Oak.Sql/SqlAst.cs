namespace Oak.Sql;

public abstract class SqlNode
{
    public override abstract string ToString();
}

/// <summary>
///     INSERT 操作类型
/// </summary>
public enum InsertKind
{
    Default,
    Insert,
    InsertOrReplace,
    InsertOrIgnore,
    Replace,
    Upsert
}

/// <summary>
///     复合查询运算符
/// </summary>
public enum CompoundOperator
{
    Union,
    UnionAll,
    Intersect,
    Except
}

/// <summary>
///     ON CONFLICT 冲突解决策略
/// </summary>
public enum ConflictAction
{
    Rollback,
    Abort,
    Fail,
    Ignore,
    Replace
}

public sealed class SelectStatement : SqlNode
{
    public IReadOnlyList<SqlColumn> Columns { get; }
    public SqlTableRef? From { get; }
    public SqlExpression? Where { get; }
    public IReadOnlyList<SqlTableRef> Joins { get; }
    public IReadOnlyList<SqlExpression> GroupBy { get; }
    public SqlExpression? Having { get; }
    public IReadOnlyList<OrderByItem> OrderBy { get; }
    public int? Limit { get; }
    public int? Offset { get; }
    public bool Distinct { get; }

    public SelectStatement(
        IReadOnlyList<SqlColumn> columns,
        SqlTableRef? from = null,
        SqlExpression? where = null,
        IReadOnlyList<SqlTableRef>? joins = null,
        IReadOnlyList<SqlExpression>? groupBy = null,
        SqlExpression? having = null,
        IReadOnlyList<OrderByItem>? orderBy = null,
        int? limit = null,
        int? offset = null,
        bool distinct = false)
    {
        Columns = columns;
        From = from;
        Where = where;
        Joins = joins ?? [];
        GroupBy = groupBy ?? [];
        Having = having;
        OrderBy = orderBy ?? [];
        Limit = limit;
        Offset = offset;
        Distinct = distinct;
    }

    public override string ToString()
    {
        var distinct = Distinct ? "DISTINCT " : "";
        var parts = new List<string> { $"SELECT {distinct}{string.Join(", ", Columns)}" };
        if (From is not null) parts.Add($"FROM {From}");
        if (Where is not null) parts.Add($"WHERE {Where}");
        return string.Join(" ", parts);
    }
}

/// <summary>
///     复合查询：SELECT ... UNION/INTERSECT/EXCEPT SELECT ...
/// </summary>
public sealed class CompoundSelectStatement : SqlNode
{
    public CompoundOperator Operator { get; }
    public SelectStatement Left { get; }
    public SelectStatement Right { get; }

    public CompoundSelectStatement(CompoundOperator op, SelectStatement left, SelectStatement right)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public override string ToString()
    {
        var op = Operator switch
        {
            CompoundOperator.Union => "UNION",
            CompoundOperator.UnionAll => "UNION ALL",
            CompoundOperator.Intersect => "INTERSECT",
            CompoundOperator.Except => "EXCEPT",
            _ => "?"
        };
        return $"{Left} {op} {Right}";
    }
}

public sealed class InsertStatement : SqlNode
{
    public InsertKind Kind { get; }
    public string Table { get; }
    public IReadOnlyList<string> Columns { get; }
    public IReadOnlyList<IReadOnlyList<SqlExpression>> ValuesRows { get; }
    public bool IsDefaultValues { get; }
    public SelectStatement? SelectSource { get; }
    public IReadOnlyList<SqlColumn>? Returning { get; }
    public OnConflictClause? OnConflict { get; }

    public InsertStatement(
        string table,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<SqlExpression>> valuesRows,
        bool isDefaultValues = false,
        InsertKind kind = InsertKind.Insert,
        SelectStatement? selectSource = null,
        IReadOnlyList<SqlColumn>? returning = null,
        OnConflictClause? onConflict = null)
    {
        Table = table;
        Columns = columns;
        ValuesRows = valuesRows;
        IsDefaultValues = isDefaultValues;
        Kind = kind;
        SelectSource = selectSource;
        Returning = returning;
        OnConflict = onConflict;
    }

    public override string ToString()
    {
        var prefix = Kind switch
        {
            InsertKind.Replace => "REPLACE",
            InsertKind.InsertOrReplace => "INSERT OR REPLACE",
            InsertKind.InsertOrIgnore => "INSERT OR IGNORE",
            InsertKind.Upsert => "UPSERT",
            _ => "INSERT"
        };

        if (IsDefaultValues)
        {
            return $"{prefix} INTO {Table} DEFAULT VALUES";
        }

        var cols = Columns.Count > 0 ? $"({string.Join(", ", Columns)})" : "";
        var source = SelectSource is not null
            ? $" {SelectSource}"
            : $" VALUES {string.Join(", ", ValuesRows.Select(r => $"({string.Join(", ", r)})"))}";

        var onConflict = OnConflict is not null ? $" {OnConflict}" : "";

        return $"{prefix} INTO {Table}{cols}{source}{onConflict}";
    }
}

/// <summary>
///     ON CONFLICT 子句
/// </summary>
public sealed class OnConflictClause : SqlNode
{
    public IReadOnlyList<string>? ConflictColumns { get; }
    public ConflictAction? Action { get; }

    /// <summary>
    ///     DO UPDATE SET 时的赋值列表
    /// </summary>
    public IReadOnlyList<(string Column, SqlExpression Value)>? UpdateAssignments { get; }

    /// <summary>
    ///     DO UPDATE 时的 WHERE 条件
    /// </summary>
    public SqlExpression? UpdateWhere { get; }

    public OnConflictClause(
        IReadOnlyList<string>? conflictColumns = null,
        ConflictAction? action = null,
        IReadOnlyList<(string Column, SqlExpression Value)>? updateAssignments = null,
        SqlExpression? updateWhere = null)
    {
        ConflictColumns = conflictColumns;
        Action = action;
        UpdateAssignments = updateAssignments;
        UpdateWhere = updateWhere;
    }

    public override string ToString()
    {
        var cols = ConflictColumns is { Count: > 0 }
            ? $"({string.Join(", ", ConflictColumns)})"
            : "";

        var action = Action switch
        {
            ConflictAction.Rollback => "ROLLBACK",
            ConflictAction.Abort => "ABORT",
            ConflictAction.Fail => "FAIL",
            ConflictAction.Ignore => "OR IGNORE",  // INSERT OR IGNORE / DO NOTHING
            ConflictAction.Replace => "OR REPLACE", // INSERT OR REPLACE / DO UPDATE
            _ => ""
        };

        var upsert = Action == ConflictAction.Replace && UpdateAssignments is { Count: > 0 }
            ? $"DO UPDATE SET {string.Join(", ", UpdateAssignments.Select(a => $"{a.Column} = {a.Value}"))}"
            : Action == ConflictAction.Ignore
                ? "DO NOTHING"
                : "";

        return $"ON CONFLICT{cols}{action} {upsert}".TrimEnd();
    }
}

public sealed class UpdateStatement : SqlNode
{
    public string Table { get; }
    public IReadOnlyList<(string Column, SqlExpression Value)> Assignments { get; }
    public SqlExpression? Where { get; }
    public IReadOnlyList<SqlColumn>? Returning { get; }

    public UpdateStatement(
        string table,
        IReadOnlyList<(string Column, SqlExpression Value)> assignments,
        SqlExpression? where = null,
        IReadOnlyList<SqlColumn>? returning = null)
    {
        Table = table;
        Assignments = assignments;
        Where = where;
        Returning = returning;
    }

    public override string ToString()
    {
        var sets = string.Join(", ", Assignments.Select(a => $"{a.Column} = {a.Value}"));
        var parts = new List<string> { $"UPDATE {Table} SET {sets}" };
        if (Where is not null) parts.Add($"WHERE {Where}");
        return string.Join(" ", parts);
    }
}

public sealed class DeleteStatement : SqlNode
{
    public string Table { get; }
    public SqlExpression? Where { get; }
    public IReadOnlyList<SqlColumn>? Returning { get; }

    public DeleteStatement(string table, SqlExpression? where = null, IReadOnlyList<SqlColumn>? returning = null)
    {
        Table = table;
        Where = where;
        Returning = returning;
    }

    public override string ToString()
    {
        var parts = new List<string> { $"DELETE FROM {Table}" };
        if (Where is not null) parts.Add($"WHERE {Where}");
        return string.Join(" ", parts);
    }
}

public sealed class DropTableStatement : SqlNode
{
    public string Table { get; }
    public bool IfExists { get; }

    public DropTableStatement(string table, bool ifExists = false)
    {
        Table = table;
        IfExists = ifExists;
    }

    public override string ToString()
    {
        var exists = IfExists ? "IF EXISTS " : "";
        return $"DROP TABLE {exists}{Table}";
    }
}

public sealed class CreateTableStatement : SqlNode
{
    public string Table { get; }
    public IReadOnlyList<ColumnDef> Columns { get; }
    public bool IfNotExists { get; }

    public CreateTableStatement(string table, IReadOnlyList<ColumnDef> columns, bool ifNotExists = false)
    {
        Table = table;
        Columns = columns;
        IfNotExists = ifNotExists;
    }

    public override string ToString()
    {
        var exists = IfNotExists ? "IF NOT EXISTS " : "";
        return $"CREATE TABLE {exists}{Table} ({string.Join(", ", Columns)})";
    }
}

/// <summary>
///     ALTER TABLE 语句
/// </summary>
public sealed class AlterTableStatement : SqlNode
{
    public string Table { get; }
    public AlterTableAction Action { get; }

    public AlterTableStatement(string table, AlterTableAction action)
    {
        Table = table;
        Action = action;
    }

    public override string ToString()
    {
        return $"ALTER TABLE {Table} {Action}";
    }
}

/// <summary>
///     ALTER TABLE 的操作类型
/// </summary>
public abstract class AlterTableAction : SqlNode;

public sealed class AddColumnAction : AlterTableAction
{
    public ColumnDef Column { get; }

    public AddColumnAction(ColumnDef column)
    {
        Column = column;
    }

    public override string ToString()
    {
        return $"ADD COLUMN {Column}";
    }
}

public sealed class RenameColumnAction : AlterTableAction
{
    public string OldName { get; }
    public string NewName { get; }

    public RenameColumnAction(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }

    public override string ToString()
    {
        return $"RENAME COLUMN {OldName} TO {NewName}";
    }
}

public sealed class ElseIfClause : SqlNode
{
    public SqlExpression Condition { get; }
    public IReadOnlyList<SqlNode> Body { get; }

    public ElseIfClause(SqlExpression condition, IReadOnlyList<SqlNode> body)
    {
        Condition = condition;
        Body = body;
    }

    public override string ToString() => $"ELSIF ({Condition}) THEN ...";
}

/// <summary>
///     IF 语句（存储过程内使用）
/// </summary>
public sealed class IfStatement : SqlNode
{
    public SqlExpression Condition { get; }
    public IReadOnlyList<SqlNode> ThenBody { get; }
    public IReadOnlyList<ElseIfClause> ElseIfClauses { get; }
    public IReadOnlyList<SqlNode>? ElseBody { get; }

    public IfStatement(
        SqlExpression condition,
        IReadOnlyList<SqlNode> thenBody,
        IReadOnlyList<ElseIfClause> elseIfClauses,
        IReadOnlyList<SqlNode>? elseBody)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseIfClauses = elseIfClauses;
        ElseBody = elseBody;
    }

    public override string ToString() => $"IF ({Condition}) THEN ... END IF";
}

/// <summary>
///     CREATE MATERIALIZED VIEW 语句
/// </summary>
public sealed class CreateMaterializedViewStatement : SqlNode
{
    public string Name { get; }
    public SelectStatement SelectStatement { get; }
    public string? RefreshMode { get; }
    public bool IfNotExists { get; }

    public CreateMaterializedViewStatement(string name, SelectStatement selectStatement, string? refreshMode, bool ifNotExists = false)
    {
        Name = name;
        SelectStatement = selectStatement;
        RefreshMode = refreshMode;
        IfNotExists = ifNotExists;
    }

    public override string ToString()
    {
        var ifne = IfNotExists ? "IF NOT EXISTS " : "";
        return $"CREATE MATERIALIZED VIEW {ifne}{Name} AS {SelectStatement}";
    }
}

/// <summary>
///     DROP MATERIALIZED VIEW 语句
/// </summary>
public sealed class DropMaterializedViewStatement : SqlNode
{
    public string Name { get; }
    public bool IfExists { get; }

    public DropMaterializedViewStatement(string name, bool ifExists = false)
    {
        Name = name;
        IfExists = ifExists;
    }

    public override string ToString()
    {
        return $"DROP MATERIALIZED VIEW {(IfExists ? "IF EXISTS " : "")}{Name}";
    }
}

/// <summary>
///     REFRESH MATERIALIZED VIEW 语句
/// </summary>
public sealed class RefreshMaterializedViewStatement : SqlNode
{
    public string Name { get; }

    public RefreshMaterializedViewStatement(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return $"REFRESH MATERIALIZED VIEW {Name}";
    }
}

public sealed class RenameTableAction : AlterTableAction
{
    public string NewName { get; }

    public RenameTableAction(string newName)
    {
        NewName = newName;
    }

    public override string ToString()
    {
        return $"RENAME TO {NewName}";
    }
}

/// <summary>
///     ALTER TABLE DROP COLUMN 操作
/// </summary>
public sealed class DropColumnAction : AlterTableAction
{
    public string ColumnName { get; }

    public DropColumnAction(string columnName)
    {
        ColumnName = columnName;
    }

    public override string ToString()
    {
        return $"DROP COLUMN {ColumnName}";
    }
}

/// <summary>
///     CREATE INDEX 语句
/// </summary>
public sealed class CreateIndexStatement : SqlNode
{
    public bool IsUnique { get; }
    public bool IfNotExists { get; }
    public string? Name { get; }
    public string Table { get; }
    public IReadOnlyList<(string Column, bool Descending)> Columns { get; }

    public CreateIndexStatement(
        bool isUnique,
        bool ifNotExists,
        string? name,
        string table,
        IReadOnlyList<(string Column, bool Descending)> columns)
    {
        IsUnique = isUnique;
        IfNotExists = ifNotExists;
        Name = name;
        Table = table;
        Columns = columns;
    }

    public override string ToString()
    {
        var unique = IsUnique ? "UNIQUE " : "";
        var ifne = IfNotExists ? "IF NOT EXISTS " : "";
        return $"CREATE {unique}INDEX {ifne}{Name} ON {Table} ({string.Join(", ", Columns.Select(c => c.Descending ? $"{c.Column} DESC" : c.Column))})";
    }
}

/// <summary>
///     DROP INDEX 语句
/// </summary>
public sealed class DropIndexStatement : SqlNode
{
    public string Name { get; }
    public bool IfExists { get; }

    public DropIndexStatement(string name, bool ifExists = false)
    {
        Name = name;
        IfExists = ifExists;
    }

    public override string ToString()
    {
        var exists = IfExists ? "IF EXISTS " : "";
        return $"DROP INDEX {exists}{Name}";
    }
}

/// <summary>
///     CREATE FUNCTION 语句
/// </summary>
public sealed class CreateFunctionStatement : SqlNode
{
    public CreateFunctionStatement(
        string name,
        IReadOnlyList<ParameterDef> parameters,
        string returnType,
        SqlExpression body)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
        Body = body;
    }

    /// <summary>
    ///     函数名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<ParameterDef> Parameters { get; }

    /// <summary>
    ///     返回值类型
    /// </summary>
    public string ReturnType { get; }

    /// <summary>
    ///     函数体（表达式）
    /// </summary>
    public SqlExpression Body { get; }

    public override string ToString()
    {
        var paramStr = string.Join(", ", Parameters.Select(p => $"{p.Name} {p.Type}"));
        return $"CREATE FUNCTION {Name}({paramStr}) RETURNS {ReturnType} AS {Body}";
    }
}

/// <summary>
///     CREATE PROCEDURE 语句
/// </summary>
public sealed class CreateProcedureStatement : SqlNode
{
    public CreateProcedureStatement(
        string name,
        IReadOnlyList<ParameterDef> parameters,
        IReadOnlyList<SqlNode> body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }

    /// <summary>
    ///     存储过程名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<ParameterDef> Parameters { get; }

    /// <summary>
    ///     过程体（多条语句）
    /// </summary>
    public IReadOnlyList<SqlNode> Body { get; }

    public override string ToString()
    {
        var paramStr = string.Join(", ", Parameters.Select(p => $"{p.Name} {p.Type}"));
        var bodyStr = string.Join("; ", Body.Select(b => b.ToString()));
        return $"CREATE PROCEDURE {Name}({paramStr}) BEGIN {bodyStr}; END";
    }
}

/// <summary>
///     SHOW TABLES 语句
/// </summary>
public sealed class ShowTablesStatement : SqlNode
{
    public override string ToString()
    {
        return "SHOW TABLES";
    }
}

/// <summary>
///     DESCRIBE table_name 语句
/// </summary>
public sealed class DescribeTableStatement : SqlNode
{
    public string TableName { get; }

    public DescribeTableStatement(string tableName)
    {
        TableName = tableName;
    }

    public override string ToString()
    {
        return $"DESCRIBE {TableName}";
    }
}

/// <summary>
///     SHOW COLUMNS FROM table_name 语句
/// </summary>
public sealed class ShowColumnsStatement : SqlNode
{
    public string TableName { get; }

    public ShowColumnsStatement(string tableName)
    {
        TableName = tableName;
    }

    public override string ToString()
    {
        return $"SHOW COLUMNS FROM {TableName}";
    }
}

/// <summary>
///     CALL 语句
/// </summary>
public sealed class CallStatement : SqlNode
{
    public CallStatement(string procedureName, IReadOnlyList<SqlExpression> arguments)
    {
        ProcedureName = procedureName;
        Arguments = arguments;
    }

    /// <summary>
    ///     存储过程名
    /// </summary>
    public string ProcedureName { get; }

    /// <summary>
    ///     实参列表
    /// </summary>
    public IReadOnlyList<SqlExpression> Arguments { get; }

    public override string ToString()
    {
        var argStr = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"CALL {ProcedureName}({argStr})";
    }
}

/// <summary>
///     PREPARE 语句 — 创建预处理语句
///     PREPARE stmt_name FROM 'query_string'
/// </summary>
public sealed class PrepareStatement : SqlNode
{
    public string Name { get; }
    public string Query { get; }

    public PrepareStatement(string name, string query)
    {
        Name = name;
        Query = query;
    }

    public override string ToString() => $"PREPARE {Name} FROM '{Query}'";
}

/// <summary>
///     EXECUTE 语句 — 执行预处理语句
///     EXECUTE stmt_name [USING @var1, @var2, ...]
/// </summary>
public sealed class ExecuteStatement : SqlNode
{
    public string Name { get; }
    public IReadOnlyList<SqlExpression> Parameters { get; }

    public ExecuteStatement(string name, IReadOnlyList<SqlExpression> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    public override string ToString() => $"EXECUTE {Name}";
}

/// <summary>
///     DEALLOCATE PREPARE 语句
/// </summary>
public sealed class DeallocatePrepareStatement : SqlNode
{
    public string Name { get; }

    public DeallocatePrepareStatement(string name)
    {
        Name = name;
    }

    public override string ToString() => $"DEALLOCATE PREPARE {Name}";
}

/// <summary>
///     函数/存储过程参数定义
/// </summary>
public sealed class ParameterDef
{
    public ParameterDef(string name, string type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    ///     参数名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     参数类型
    /// </summary>
    public string Type { get; }
}

public sealed class SqlColumn : SqlNode
{
    public SqlExpression Expression { get; }
    public string? Alias { get; }

    public SqlColumn(SqlExpression expression, string? alias = null)
    {
        Expression = expression;
        Alias = alias;
    }

    public override string ToString()
    {
        return Alias is not null ? $"{Expression} AS {Alias}" : Expression.ToString()!;
    }
}

public sealed class SqlTableRef : SqlNode
{
    public string Name { get; }
    public string? Alias { get; }
    public SqlTokenType JoinType { get; }
    public SqlExpression? OnCondition { get; }

    public SqlTableRef(string name, string? alias = null, SqlTokenType joinType = SqlTokenType.From, SqlExpression? onCondition = null)
    {
        Name = name;
        Alias = alias;
        JoinType = joinType;
        OnCondition = onCondition;
    }

    public override string ToString()
    {
        return Alias is not null ? $"{Name} AS {Alias}" : Name;
    }
}

public sealed class ColumnDef : SqlNode
{
    public string Name { get; }
    public string Type { get; }
    public bool IsPrimaryKey { get; }
    public bool IsNotNull { get; }
    public bool IsAutoincrement { get; }
    public bool IsUnique { get; }
    public SqlExpression? DefaultValue { get; }
    public SqlExpression? CheckExpression { get; }
    public string? Collate { get; }

    public ColumnDef(
        string name,
        string type,
        bool isPrimaryKey = false,
        bool isNotNull = false,
        SqlExpression? defaultValue = null,
        bool isAutoincrement = false,
        bool isUnique = false,
        SqlExpression? checkExpression = null,
        string? collate = null)
    {
        Name = name;
        Type = type;
        IsPrimaryKey = isPrimaryKey;
        IsNotNull = isNotNull;
        DefaultValue = defaultValue;
        IsAutoincrement = isAutoincrement;
        IsUnique = isUnique;
        CheckExpression = checkExpression;
        Collate = collate;
    }

    public override string ToString()
    {
        var parts = new List<string> { Name, Type };
        if (IsPrimaryKey) parts.Add("PRIMARY KEY");
        if (IsAutoincrement) parts.Add("AUTOINCREMENT");
        if (IsNotNull) parts.Add("NOT NULL");
        if (IsUnique) parts.Add("UNIQUE");
        if (DefaultValue is not null) parts.Add($"DEFAULT {DefaultValue}");
        if (CheckExpression is not null) parts.Add($"CHECK ({CheckExpression})");
        if (Collate is not null) parts.Add($"COLLATE {Collate}");
        return string.Join(" ", parts);
    }
}

public sealed class OrderByItem : SqlNode
{
    public SqlExpression Expression { get; }
    public bool Descending { get; }
    public bool NullsFirst { get; }
    public bool NullsLast { get; }

    public OrderByItem(SqlExpression expression, bool descending = false, bool nullsFirst = false, bool nullsLast = false)
    {
        Expression = expression;
        Descending = descending;
        NullsFirst = nullsFirst;
        NullsLast = nullsLast;
    }

    public override string ToString()
    {
        var result = Descending ? $"{Expression} DESC" : $"{Expression} ASC";
        if (NullsFirst) result += " NULLS FIRST";
        if (NullsLast) result += " NULLS LAST";
        return result;
    }
}

public abstract class SqlExpression : SqlNode;

public sealed class ColumnRef : SqlExpression
{
    public string? Table { get; }
    public string Name { get; }

    public ColumnRef(string name, string? table = null)
    {
        Table = table;
        Name = name;
    }

    public override string ToString()
    {
        return Table is not null ? $"{Table}.{Name}" : Name;
    }
}

public sealed class LiteralValue : SqlExpression
{
    public object? Value { get; }
    public SqlTokenType Type { get; }

    public LiteralValue(object? value, SqlTokenType type)
    {
        Value = value;
        Type = type;
    }

    public override string ToString()
    {
        return Type switch
        {
            SqlTokenType.String => $"'{Value}'",
            SqlTokenType.True => "TRUE",
            SqlTokenType.False => "FALSE",
            SqlTokenType.Null => "NULL",
            _ => Value?.ToString() ?? "NULL"
        };
    }
}

public sealed class StarExpression : SqlExpression
{
    public string? Table { get; }

    public StarExpression(string? table = null)
    {
        Table = table;
    }

    public override string ToString()
    {
        return Table is not null ? $"{Table}.*" : "*";
    }
}

/// <summary>
///     子查询表达式：(SELECT ...)
/// </summary>
public sealed class SubqueryExpr : SqlExpression
{
    public SelectStatement Query { get; }

    public SubqueryExpr(SelectStatement query)
    {
        Query = query;
    }

    public override string ToString()
    {
        return $"({Query})";
    }
}

public sealed class BinaryExpr : SqlExpression
{
    public SqlExpression Left { get; }
    public string Operator { get; }
    public SqlExpression Right { get; }

    public BinaryExpr(SqlExpression left, string op, SqlExpression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override string ToString()
    {
        return $"({Left} {Operator} {Right})";
    }
}

public sealed class UnaryExpr : SqlExpression
{
    public string Operator { get; }
    public SqlExpression Operand { get; }

    public UnaryExpr(string op, SqlExpression operand)
    {
        Operator = op;
        Operand = operand;
    }

    public override string ToString()
    {
        return $"({Operator} {Operand})";
    }
}

public sealed class FunctionCall : SqlExpression
{
    public string Name { get; }
    public IReadOnlyList<SqlExpression> Arguments { get; }
    public bool Distinct { get; }

    public FunctionCall(string name, IReadOnlyList<SqlExpression> arguments, bool distinct = false)
    {
        Name = name;
        Arguments = arguments;
        Distinct = distinct;
    }

    public override string ToString()
    {
        var args = Distinct ? $"DISTINCT {string.Join(", ", Arguments)}" : string.Join(", ", Arguments);
        return $"{Name}({args})";
    }
}

public sealed class InExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public IReadOnlyList<SqlExpression> Values { get; }
    public bool Negated { get; }

    public InExpr(SqlExpression expression, IReadOnlyList<SqlExpression> values, bool negated = false)
    {
        Expression = expression;
        Values = values;
        Negated = negated;
    }

    public override string ToString()
    {
        var not = Negated ? "NOT " : "";
        return $"{Expression} {not}IN ({string.Join(", ", Values)})";
    }
}

public sealed class BetweenExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public SqlExpression Low { get; }
    public SqlExpression High { get; }
    public bool Negated { get; }

    public BetweenExpr(SqlExpression expression, SqlExpression low, SqlExpression high, bool negated = false)
    {
        Expression = expression;
        Low = low;
        High = high;
        Negated = negated;
    }

    public override string ToString()
    {
        var not = Negated ? "NOT " : "";
        return $"{Expression} {not}BETWEEN {Low} AND {High}";
    }
}

public sealed class IsNullExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public bool Negated { get; }

    public IsNullExpr(SqlExpression expression, bool negated = false)
    {
        Expression = expression;
        Negated = negated;
    }

    public override string ToString()
    {
        var not = Negated ? "NOT " : "";
        return $"{Expression} IS {not}NULL";
    }
}

/// <summary>
///     EXISTS (子查询) 表达式
/// </summary>
public sealed class ExistsExpr : SqlExpression
{
    public SelectStatement Subquery { get; }
    public bool Negated { get; }

    public ExistsExpr(SelectStatement subquery, bool negated = false)
    {
        Subquery = subquery;
        Negated = negated;
    }

    public override string ToString()
    {
        var not = Negated ? "NOT " : "";
        return $"{not}EXISTS ({Subquery})";
    }
}

/// <summary>
///     CASE WHEN ... THEN ... ELSE ... END 表达式
/// </summary>
public sealed class CaseExpr : SqlExpression
{
    public IReadOnlyList<(SqlExpression When, SqlExpression Then)> WhenClauses { get; }
    public SqlExpression? ElseExpr { get; }

    public CaseExpr(
        IReadOnlyList<(SqlExpression When, SqlExpression Then)> whenClauses,
        SqlExpression? elseExpr = null)
    {
        WhenClauses = whenClauses;
        ElseExpr = elseExpr;
    }

    public override string ToString()
    {
        var parts = new List<string> { "CASE" };
        foreach (var (w, t) in WhenClauses)
        {
            parts.Add($"WHEN {w} THEN {t}");
        }

        if (ElseExpr is not null)
        {
            parts.Add($"ELSE {ElseExpr}");
        }

        parts.Add("END");
        return string.Join(" ", parts);
    }
}

/// <summary>
///     CAST(expr AS type) 表达式
/// </summary>
public sealed class CastExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public string TargetType { get; }

    public CastExpr(SqlExpression expression, string targetType)
    {
        Expression = expression;
        TargetType = targetType;
    }

    public override string ToString()
    {
        return $"CAST({Expression} AS {TargetType})";
    }
}

/// <summary>
///     expr COLLATE collation 表达式
/// </summary>
public sealed class CollateExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public string Collation { get; }

    public CollateExpr(SqlExpression expression, string collation)
    {
        Expression = expression;
        Collation = collation;
    }

    public override string ToString()
    {
        return $"{Expression} COLLATE {Collation}";
    }
}

/// <summary>
///     LIKE / ILIKE 表达式
/// </summary>
public sealed class LikeExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public SqlExpression Pattern { get; }
    public bool Negated { get; }
    public bool CaseInsensitive { get; }

    public LikeExpr(SqlExpression expression, SqlExpression pattern, bool negated = false, bool caseInsensitive = false)
    {
        Expression = expression;
        Pattern = pattern;
        Negated = negated;
        CaseInsensitive = caseInsensitive;
    }

    public override string ToString()
    {
        var op = CaseInsensitive ? "ILIKE" : (Negated ? "NOT LIKE" : "LIKE");
        return $"{Expression} {op} {Pattern}";
    }
}

/// <summary>
///     IN (子查询) 表达式
/// </summary>
public sealed class InSubqueryExpr : SqlExpression
{
    public SqlExpression Expression { get; }
    public SelectStatement Subquery { get; }
    public bool Negated { get; }

    public InSubqueryExpr(SqlExpression expression, SelectStatement subquery, bool negated = false)
    {
        Expression = expression;
        Subquery = subquery;
        Negated = negated;
    }

    public override string ToString()
    {
        var not = Negated ? "NOT " : "";
        return $"{Expression} {not}IN ({Subquery})";
    }
}
