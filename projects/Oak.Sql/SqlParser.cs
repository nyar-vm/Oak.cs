using System.Text;
using Oak.Diagnostics;
using Oak.Parsing;
using Oak.Syntax;

namespace Oak.Sql;

public sealed class SqlParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<SqlToken> _tokens = [];

    private SqlToken Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    private SqlTokenType Peek(int offset = 0)
    {
        var idx = _position + offset;
        return idx < _tokens.Count ? _tokens[idx].Type : SqlTokenType.EndOfFile;
    }

    public ParseResult<SqlNode> Parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new SqlLexer();
        _tokens = lexer.Tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var statements = new List<SqlNode>();

        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null) statements.Add(stmt);

            while (Match(SqlTokenType.Semicolon)) { }
        }

        if (statements.Count == 0)
        {
            _diagnostics?.AddError(string.Empty, default, "SQL001", "未解析到有效 SQL 语句");
            return ParseResult<SqlNode>.Fail(_diagnostics?.Messages ?? []);
        }

        if (statements.Count == 1) return ParseResult<SqlNode>.Ok(statements[0], _diagnostics?.Messages);

        return ParseResult<SqlNode>.Ok(statements[0], _diagnostics?.Messages);
    }

    private SqlNode? ParseStatement()
    {
        if (Check(SqlTokenType.Select)) return ParseSelectOrCompound();
        if (Check(SqlTokenType.Insert)) return ParseInsert();
        if (Check(SqlTokenType.Replace)) return ParseInsert(InsertKind.Replace);
        if (Check(SqlTokenType.Upsert)) return ParseInsert(InsertKind.Upsert);
        if (Check(SqlTokenType.Update)) return ParseUpdate();
        if (Check(SqlTokenType.Delete)) return ParseDelete();
        if (Check(SqlTokenType.Alter)) return ParseAlterTable();
        if (Check(SqlTokenType.Drop))
        {
            if (Peek(1) == SqlTokenType.Table) return ParseDropTable();
            if (Peek(1) == SqlTokenType.Index) return ParseDropIndex();
            if (Peek(1) == SqlTokenType.Materialized) return ParseDropMaterializedView();
            if (Peek(1) == SqlTokenType.Prepare) return ParseDeallocatePrepare();
        }

        if (Check(SqlTokenType.Deallocate)) return ParseDeallocatePrepare();

        if (Check(SqlTokenType.Create))
        {
            var next = Peek(1);
            if (next == SqlTokenType.Table) return ParseCreateTable();
            if (next == SqlTokenType.Function) return ParseCreateFunction();
            if (next == SqlTokenType.Procedure) return ParseCreateProcedure();
            if (next == SqlTokenType.Materialized) return ParseCreateMaterializedView();
            if (next is SqlTokenType.Unique or SqlTokenType.Index)
            {
                return ParseCreateIndex();
            }
        }

        if (Check(SqlTokenType.Refresh))
        {
            return ParseRefreshMaterializedView();
        }

        if (Check(SqlTokenType.Call)) return ParseCallStmt();
        if (Check(SqlTokenType.Show)) return ParseShowTables();
        if (Check(SqlTokenType.Describe)) return ParseDescribeTable();
        if (Check(SqlTokenType.Prepare)) return ParsePrepare();
        if (Check(SqlTokenType.Execute)) return ParseExecute();

        _diagnostics?.AddError(string.Empty, default,
            "SQL100", $"意外的词法单元：{Current.Type}");

        Advance();
        return null;
    }

    private SqlNode ParseSelectOrCompound()
    {
        var select = ParseSelect();

        while (!IsAtEnd())
        {
            if (Match(SqlTokenType.Union))
            {
                var all = Match(SqlTokenType.All);
                var right = ParseSelect();
                return new CompoundSelectStatement(
                    all ? CompoundOperator.UnionAll : CompoundOperator.Union, select, right);
            }

            if (Match(SqlTokenType.Intersect))
            {
                return new CompoundSelectStatement(CompoundOperator.Intersect, select, ParseSelect());
            }

            if (Match(SqlTokenType.Except))
            {
                return new CompoundSelectStatement(CompoundOperator.Except, select, ParseSelect());
            }

            break;
        }

        return select;
    }

    #region SELECT

    private SelectStatement ParseSelect()
    {
        Consume(SqlTokenType.Select, "SQL101", "期望 'SELECT'");

        var distinct = Match(SqlTokenType.Distinct);
        var columns = ParseColumnList();

        SqlTableRef? from = null;
        if (Match(SqlTokenType.From)) from = ParseTableRef();

        var joins = new List<SqlTableRef>();
        while (IsJoinKeyword()) joins.Add(ParseJoin());

        SqlExpression? where = null;
        if (Match(SqlTokenType.Where)) where = ParseExpression();

        var groupBy = new List<SqlExpression>();
        if (Match(SqlTokenType.Group))
        {
            Consume(SqlTokenType.By, "SQL102", "期望 'BY'");
            groupBy = ParseExpressionList().ToList();
        }

        SqlExpression? having = null;
        if (Match(SqlTokenType.Having)) having = ParseExpression();

        var orderBy = new List<OrderByItem>();
        if (Match(SqlTokenType.Order))
        {
            Consume(SqlTokenType.By, "SQL103", "期望 'BY'");
            orderBy = ParseOrderByList().ToList();
        }

        int? limit = null;
        if (Match(SqlTokenType.Limit)) limit = ParseIntegerValue();

        int? offset = null;
        if (Match(SqlTokenType.Offset)) offset = ParseIntegerValue();

        return new SelectStatement(columns, from, where, joins, groupBy, having, orderBy, limit, offset, distinct);
    }

    private IReadOnlyList<SqlColumn> ParseColumnList()
    {
        var columns = new List<SqlColumn> { ParseColumn() };

        while (Match(SqlTokenType.Comma)) columns.Add(ParseColumn());

        return columns;
    }

    private SqlColumn ParseColumn()
    {
        var expr = ParseExpression();
        string? alias = null;

        if (Match(SqlTokenType.As)) alias = ConsumeIdentifier("SQL104", "期望别名");
        else if (Check(SqlTokenType.Identifier) && !IsKeyword(Current.Type)) alias = Advance().Text;

        return new SqlColumn(expr, alias);
    }

    private SqlTableRef ParseTableRef()
    {
        var name = ConsumeIdentifier("SQL105", "期望表名");
        string? alias = null;

        if (Match(SqlTokenType.As)) alias = ConsumeIdentifier("SQL106", "期望别名");
        else if (Check(SqlTokenType.Identifier) && !IsKeyword(Current.Type)) alias = Advance().Text;

        return new SqlTableRef(name, alias);
    }

    private bool IsJoinKeyword()
    {
        return Check(SqlTokenType.Join) || Check(SqlTokenType.Inner) ||
               Check(SqlTokenType.Left) || Check(SqlTokenType.Right) ||
               Check(SqlTokenType.Full) || Check(SqlTokenType.Cross) ||
               Check(SqlTokenType.Natural);
    }

    private SqlTableRef ParseJoin()
    {
        SqlTokenType joinType = SqlTokenType.Join;

        if (Match(SqlTokenType.Natural)) joinType = SqlTokenType.Natural;
        else if (Match(SqlTokenType.Cross)) joinType = SqlTokenType.Cross;
        else if (Match(SqlTokenType.Inner)) joinType = SqlTokenType.Inner;
        else if (Match(SqlTokenType.Full)) { joinType = SqlTokenType.Full; Match(SqlTokenType.Outer); }
        else if (Match(SqlTokenType.Left)) { joinType = SqlTokenType.Left; Match(SqlTokenType.Outer); }
        else if (Match(SqlTokenType.Right)) { joinType = SqlTokenType.Right; Match(SqlTokenType.Outer); }

        Consume(SqlTokenType.Join, "SQL107", "期望 'JOIN'");

        var table = ParseTableRef();

        SqlExpression? onCondition = null;
        if (Match(SqlTokenType.On)) onCondition = ParseExpression();

        return new SqlTableRef(table.Name, table.Alias, joinType, onCondition);
    }

    private IReadOnlyList<OrderByItem> ParseOrderByList()
    {
        var items = new List<OrderByItem> { ParseOrderByItem() };

        while (Match(SqlTokenType.Comma)) items.Add(ParseOrderByItem());

        return items;
    }

    private OrderByItem ParseOrderByItem()
    {
        var expr = ParseExpression();
        var desc = Match(SqlTokenType.Desc);
        if (!desc) Match(SqlTokenType.Asc);

        var nullsFirst = false;
        var nullsLast = false;
        if (Match(SqlTokenType.Null))
        {
            if (Current.Text.Equals("FIRST", StringComparison.OrdinalIgnoreCase))
            {
                nullsFirst = true;
                Advance();
            }
            else if (Current.Text.Equals("LAST", StringComparison.OrdinalIgnoreCase))
            {
                nullsLast = true;
                Advance();
            }
        }

        return new OrderByItem(expr, desc, nullsFirst, nullsLast);
    }

    #endregion

    #region ALTER TABLE

    private AlterTableStatement ParseAlterTable()
    {
        Consume(SqlTokenType.Alter, "SQL155", "期望 'ALTER'");
        Consume(SqlTokenType.Table, "SQL156", "期望 'TABLE'");

        var table = ConsumeIdentifier("SQL157", "期望表名");

        AlterTableAction action;
        if (Match(SqlTokenType.Add))
        {
            Match(SqlTokenType.Column);
            action = new AddColumnAction(ParseColumnDef());
        }
        else if (Match(SqlTokenType.Drop))
        {
            Consume(SqlTokenType.Column, "SQL204", "期望 'COLUMN'");
            var colName = ConsumeIdentifier("SQL205", "期望列名");
            action = new DropColumnAction(colName);
        }
        else if (Match(SqlTokenType.Rename))
        {
            if (Match(SqlTokenType.Column))
            {
                var oldName = ConsumeIdentifier("SQL158", "期望旧列名");
                Consume(SqlTokenType.To, "SQL159", "期望 'TO'");
                var newName = ConsumeIdentifier("SQL160", "期望新列名");
                action = new RenameColumnAction(oldName, newName);
            }
            else
            {
                Consume(SqlTokenType.To, "SQL159", "期望 'TO'");
                var newName = ConsumeIdentifier("SQL161", "期望新表名");
                action = new RenameTableAction(newName);
            }
        }
        else
        {
            _diagnostics?.AddError(string.Empty, default,
                "SQL162", "ALTER TABLE 后期望 ADD、DROP 或 RENAME");
            action = new RenameTableAction(string.Empty);
        }

        return new AlterTableStatement(table, action);
    }

    #endregion

    #region CREATE INDEX / DROP INDEX

    private CreateIndexStatement ParseCreateIndex()
    {
        Consume(SqlTokenType.Create, "SQL163", "期望 'CREATE'");

        var isUnique = Match(SqlTokenType.Unique);

        Consume(SqlTokenType.Index, "SQL164", "期望 'INDEX'");

        var ifNotExists = false;
        if (Match(SqlTokenType.If))
        {
            Consume(SqlTokenType.Not, "SQL165", "期望 'NOT'");
            Consume(SqlTokenType.Exists, "SQL166", "期望 'EXISTS'");
            ifNotExists = true;
        }

        var name = ConsumeIdentifier("SQL167", "期望索引名");

        Consume(SqlTokenType.On, "SQL168", "期望 'ON'");

        var table = ConsumeIdentifier("SQL169", "期望表名");

        Consume(SqlTokenType.LeftParen, "SQL170", "期望 '('");

        var columns = new List<(string Column, bool Descending)>
        {
            (ConsumeIdentifier("SQL171", "期望列名"), Match(SqlTokenType.Desc))
        };
        while (Match(SqlTokenType.Comma))
        {
            var col = ConsumeIdentifier("SQL171", "期望列名");
            var desc = Match(SqlTokenType.Desc);
            columns.Add((col, desc));
        }

        Consume(SqlTokenType.RightParen, "SQL172", "期望 ')'");

        return new CreateIndexStatement(isUnique, ifNotExists, name, table, columns);
    }

    private DropIndexStatement ParseDropIndex()
    {
        Consume(SqlTokenType.Drop, "SQL173", "期望 'DROP'");
        Consume(SqlTokenType.Index, "SQL174", "期望 'INDEX'");

        var ifExists = false;
        if (Match(SqlTokenType.If))
        {
            Consume(SqlTokenType.Exists, "SQL175", "期望 'EXISTS'");
            ifExists = true;
        }

        var name = ConsumeIdentifier("SQL176", "期望索引名");

        return new DropIndexStatement(name, ifExists);
    }

    /// <summary>
    ///     解析 RETURNING 子句（INSERT/UPDATE/DELETE 后可选）
    /// </summary>
    private IReadOnlyList<SqlColumn>? ParseReturningClause()
    {
        if (!Match(SqlTokenType.Returning)) return null;

        return ParseColumnList();
    }

    #endregion

    #region INSERT

    private InsertStatement ParseInsert(InsertKind kind = InsertKind.Insert)
    {
        if (kind == InsertKind.Replace)
        {
            Consume(SqlTokenType.Replace, "SQL108", "期望 'REPLACE'");
        }
        else if (kind == InsertKind.Upsert)
        {
            Consume(SqlTokenType.Upsert, "SQL108", "期望 'UPSERT'");
        }
        else
        {
            Consume(SqlTokenType.Insert, "SQL108", "期望 'INSERT'");
            if (Match(SqlTokenType.Or))
            {
                if (Match(SqlTokenType.Replace)) kind = InsertKind.InsertOrReplace;
                else if (Match(SqlTokenType.Ignore)) kind = InsertKind.InsertOrIgnore;
            }
        }

        Consume(SqlTokenType.Into, "SQL109", "期望 'INTO'");

        var table = ConsumeIdentifier("SQL110", "期望表名");

        var columns = new List<string>();
        if (Match(SqlTokenType.LeftParen))
        {
            columns.Add(ConsumeIdentifier("SQL111", "期望列名"));
            while (Match(SqlTokenType.Comma)) columns.Add(ConsumeIdentifier("SQL112", "期望列名"));
            Consume(SqlTokenType.RightParen, "SQL113", "期望 ')'");
        }

        SelectStatement? selectSource = null;
        IReadOnlyList<IReadOnlyList<SqlExpression>> valueRows = [];
        var isDefault = false;

        if (Match(SqlTokenType.Default))
        {
            Consume(SqlTokenType.Values, "SQL114", "期望 'VALUES'");
            isDefault = true;
        }
        else if (Check(SqlTokenType.Select))
        {
            selectSource = ParseSelectRaw();
        }
        else if (Match(SqlTokenType.Values))
        {
            var rows = new List<IReadOnlyList<SqlExpression>> { ParseValueRow() };
            while (Match(SqlTokenType.Comma)) rows.Add(ParseValueRow());
            valueRows = rows;
        }

        OnConflictClause? onConflict = null;
        if (Match(SqlTokenType.On))
        {
            if (Check(SqlTokenType.Conflict))
            {
                Advance();
                onConflict = ParseOnConflictClause();
            }
        }

        var returning = ParseReturningClause();

        return new InsertStatement(table, columns, valueRows, isDefault, kind, selectSource, returning, onConflict);
    }

    private OnConflictClause ParseOnConflictClause()
    {
        IReadOnlyList<string>? conflictColumns = null;

        if (Match(SqlTokenType.LeftParen))
        {
            var cols = new List<string> { ConsumeIdentifier("SQL206", "期望冲突列名") };
            while (Match(SqlTokenType.Comma)) cols.Add(ConsumeIdentifier("SQL206", "期望冲突列名"));
            Consume(SqlTokenType.RightParen, "SQL207", "期望 ')'");
            conflictColumns = cols;
        }

        ConflictAction? action = null;
        IReadOnlyList<(string, SqlExpression)>? updateAssignments = null;
        SqlExpression? updateWhere = null;

        if (Match(SqlTokenType.Do))
        {
            if (Match(SqlTokenType.Nothing))
            {
                action = ConflictAction.Ignore;
            }
            else if (Match(SqlTokenType.Update))
            {
                Consume(SqlTokenType.Set, "SQL208", "期望 'SET'");
                action = ConflictAction.Replace;

                var assignments = new List<(string, SqlExpression)> { ParseAssignment() };
                while (Match(SqlTokenType.Comma)) assignments.Add(ParseAssignment());
                updateAssignments = assignments;

                if (Match(SqlTokenType.Where)) updateWhere = ParseExpression();
            }
        }

        return new OnConflictClause(conflictColumns, action, updateAssignments, updateWhere);
    }

    private IReadOnlyList<SqlExpression> ParseValueRow()
    {
        Consume(SqlTokenType.LeftParen, "SQL115", "期望 '('");
        var values = new List<SqlExpression> { ParseExpression() };
        while (Match(SqlTokenType.Comma)) values.Add(ParseExpression());
        Consume(SqlTokenType.RightParen, "SQL116", "期望 ')'");
        return values;
    }

    #endregion

    #region UPDATE

    private UpdateStatement ParseUpdate()
    {
        Consume(SqlTokenType.Update, "SQL117", "期望 'UPDATE'");

        var table = ConsumeIdentifier("SQL118", "期望表名");
        Consume(SqlTokenType.Set, "SQL119", "期望 'SET'");

        var assignments = new List<(string Column, SqlExpression Value)> { ParseAssignment() };
        while (Match(SqlTokenType.Comma)) assignments.Add(ParseAssignment());

        SqlExpression? where = null;
        if (Match(SqlTokenType.Where)) where = ParseExpression();

        var returning = ParseReturningClause();

        return new UpdateStatement(table, assignments, where, returning);
    }

    private (string Column, SqlExpression Value) ParseAssignment()
    {
        var column = ConsumeIdentifier("SQL120", "期望列名");
        Consume(SqlTokenType.Equal, "SQL121", "期望 '='");
        var value = ParseExpression();
        return (column, value);
    }

    #endregion

    #region DELETE

    private DeleteStatement ParseDelete()
    {
        Consume(SqlTokenType.Delete, "SQL122", "期望 'DELETE'");
        Consume(SqlTokenType.From, "SQL123", "期望 'FROM'");

        var table = ConsumeIdentifier("SQL124", "期望表名");

        SqlExpression? where = null;
        if (Match(SqlTokenType.Where)) where = ParseExpression();

        var returning = ParseReturningClause();

        return new DeleteStatement(table, where, returning);
    }

    #endregion

    #region DROP TABLE

    private DropTableStatement ParseDropTable()
    {
        Consume(SqlTokenType.Drop, "SQL132", "期望 'DROP'");
        Consume(SqlTokenType.Table, "SQL133", "期望 'TABLE'");

        var ifExists = false;
        if (Match(SqlTokenType.If))
        {
            Consume(SqlTokenType.Exists, "SQL134", "期望 'EXISTS'");
            ifExists = true;
        }

        var table = ConsumeIdentifier("SQL135", "期望表名");

        return new DropTableStatement(table, ifExists);
    }

    #endregion

    #region CREATE TABLE

    private CreateTableStatement ParseCreateTable()
    {
        Consume(SqlTokenType.Create, "SQL125", "期望 'CREATE'");
        Consume(SqlTokenType.Table, "SQL126", "期望 'TABLE'");

        var ifNotExists = false;
        if (Match(SqlTokenType.If))
        {
            Consume(SqlTokenType.Not, "SQL127", "期望 'NOT'");
            Consume(SqlTokenType.Exists, "SQL128", "期望 'EXISTS'");
            ifNotExists = true;
        }

        var table = ConsumeIdentifier("SQL129", "期望表名");
        Consume(SqlTokenType.LeftParen, "SQL130", "期望 '('");

        var columns = new List<ColumnDef> { ParseColumnDef() };
        while (Match(SqlTokenType.Comma))
        {
            if (IsTableConstraintStart())
            {
                ParseTableConstraint(columns);
                continue;
            }

            columns.Add(ParseColumnDef());
        }

        Consume(SqlTokenType.RightParen, "SQL131", "期望 ')'");

        return new CreateTableStatement(table, columns, ifNotExists);
    }

    private ColumnDef ParseColumnDef()
    {
        var name = ConsumeIdentifier("SQL132", "期望列名");
        var type = ParseTypeName();

        var isPrimaryKey = false;
        var isNotNull = false;
        var isAutoincrement = false;
        var isUnique = false;
        SqlExpression? defaultValue = null;
        SqlExpression? checkExpression = null;
        string? collate = null;

        while (true)
        {
            if (Match(SqlTokenType.Primary))
            {
                Consume(SqlTokenType.Key, "SQL133", "期望 'KEY'");
                isPrimaryKey = true;
                Match(SqlTokenType.Autoincrement);
                if (Match(SqlTokenType.Autoincrement))
                {
                    isAutoincrement = true;
                }
            }
            else if (Check(SqlTokenType.Not))
            {
                Advance();
                Consume(SqlTokenType.Null, "SQL134", "期望 'NULL'");
                isNotNull = true;
            }
            else if (Match(SqlTokenType.Default))
            {
                defaultValue = ParseExpression();
            }
            else if (Match(SqlTokenType.Unique))
            {
                isUnique = true;
            }
            else if (Match(SqlTokenType.Check))
            {
                Consume(SqlTokenType.LeftParen, "SQL144", "期望 '('");
                checkExpression = ParseExpression();
                Consume(SqlTokenType.RightParen, "SQL145", "期望 ')'");
            }
            else if (Match(SqlTokenType.Collate))
            {
                collate = ConsumeIdentifier("SQL146", "期望排序规则名");
            }
            else
            {
                break;
            }
        }

        return new ColumnDef(name, type, isPrimaryKey, isNotNull, defaultValue,
            isAutoincrement, isUnique, checkExpression, collate);
    }

    /// <summary>
    ///     检查当前 token 是否是表级约束关键字
    /// </summary>
    private bool IsTableConstraintStart()
    {
        return Check(SqlTokenType.Primary)
               || Check(SqlTokenType.Unique)
               || Check(SqlTokenType.Foreign)
               || Check(SqlTokenType.Constraint)
               || Check(SqlTokenType.Check);
    }

    /// <summary>
    ///     解析表级约束（PRIMARY KEY、UNIQUE、FOREIGN KEY、CHECK）
    /// </summary>
    private void ParseTableConstraint(List<ColumnDef> columns)
    {
        if (Match(SqlTokenType.Constraint))
        {
            ConsumeIdentifier("SQL136", "期望约束名");
        }

        if (Match(SqlTokenType.Primary))
        {
            Consume(SqlTokenType.Key, "SQL137", "期望 'KEY'");
            Consume(SqlTokenType.LeftParen, "SQL138", "期望 '('");

            var pkCols = new List<string> { ConsumeIdentifier("SQL139", "期望主键列名") };
            while (Match(SqlTokenType.Comma))
            {
                pkCols.Add(ConsumeIdentifier("SQL139", "期望主键列名"));
            }

            Consume(SqlTokenType.RightParen, "SQL140", "期望 ')'");

            for (var i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                if (pkCols.Any(pk => string.Equals(pk, col.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    columns[i] = new ColumnDef(col.Name, col.Type, true, col.IsNotNull, col.DefaultValue);
                }
            }
        }
        else if (Match(SqlTokenType.Unique))
        {
            Consume(SqlTokenType.LeftParen, "SQL138", "期望 '('");
            var cols = new List<string> { ConsumeIdentifier("SQL141", "期望唯一约束列名") };
            while (Match(SqlTokenType.Comma))
            {
                cols.Add(ConsumeIdentifier("SQL141", "期望唯一约束列名"));
            }

            Consume(SqlTokenType.RightParen, "SQL140", "期望 ')'");
        }
        else if (Match(SqlTokenType.Foreign))
        {
            Consume(SqlTokenType.Key, "SQL137", "期望 'KEY'");
            Consume(SqlTokenType.LeftParen, "SQL138", "期望 '('");
            ConsumeIdentifier("SQL142", "期望外键列名");
            while (Match(SqlTokenType.Comma))
            {
                ConsumeIdentifier("SQL142", "期望外键列名");
            }

            Consume(SqlTokenType.RightParen, "SQL140", "期望 ')'");

            if (Match(SqlTokenType.References))
            {
                ConsumeIdentifier("SQL143", "期望引用表名");
                Consume(SqlTokenType.LeftParen, "SQL138", "期望 '('");
                ConsumeIdentifier("SQL143", "期望引用列名");
                while (Match(SqlTokenType.Comma))
                {
                    ConsumeIdentifier("SQL143", "期望引用列名");
                }

                Consume(SqlTokenType.RightParen, "SQL140", "期望 ')'");
            }
        }
        else if (Match(SqlTokenType.Check))
        {
            Consume(SqlTokenType.LeftParen, "SQL138", "期望 '('");
            var depth = 1;
            while (depth > 0 && !IsAtEnd())
            {
                if (Check(SqlTokenType.LeftParen))
                {
                    depth++;
                }
                else if (Check(SqlTokenType.RightParen))
                {
                    depth--;
                }

                Advance();
            }
        }
    }

    #endregion

    #region SHOW / DESCRIBE

    private SqlNode ParseShowTables()
    {
        Consume(SqlTokenType.Show, "SQL601", "期望 'SHOW'");

        if (Match(SqlTokenType.Tables))
        {
            return new ShowTablesStatement();
        }

        if (Match(SqlTokenType.Columns))
        {
            Consume(SqlTokenType.From, "SQL603", "期望 'FROM'");
            var tableName = ConsumeIdentifier("SQL604", "期望表名");
            return new ShowColumnsStatement(tableName);
        }

        _diagnostics?.AddError(string.Empty, default, "SQL605", "期望 'TABLES' 或 'COLUMNS'");
        return null;
    }

    private SqlNode ParseDescribeTable()
    {
        Consume(SqlTokenType.Describe, "SQL610", "期望 'DESCRIBE'");
        var tableName = ConsumeIdentifier("SQL611", "期望表名");
        return new DescribeTableStatement(tableName);
    }

    #endregion

    #region MATERIALIZED VIEW

    private SqlNode ParseCreateMaterializedView()
    {
        Consume(SqlTokenType.Create, "SQL620", "期望 'CREATE'");
        Consume(SqlTokenType.Materialized, "SQL621", "期望 'MATERIALIZED'");
        Consume(SqlTokenType.View, "SQL622", "期望 'VIEW'");

        var ifNotExists = false;
        if (Match(SqlTokenType.If))
        {
            Consume(SqlTokenType.Not, "SQL623", "期望 'NOT'");
            Consume(SqlTokenType.Exists, "SQL624", "期望 'EXISTS'");
            ifNotExists = true;
        }

        var name = ConsumeIdentifier("SQL625", "期望视图名称");

        string? refreshMode = null;
        if (Match(SqlTokenType.Refresh))
        {
            if (Match(SqlTokenType.Complete))
            {
                refreshMode = "COMPLETE";
            }
            else if (Match(SqlTokenType.Fast))
            {
                refreshMode = "FAST";
            }
        }

        Consume(SqlTokenType.As, "SQL626", "期望 'AS'");
        var selectStmt = (SelectStatement)ParseSelectOrCompound();

        return new CreateMaterializedViewStatement(name, selectStmt, refreshMode, ifNotExists);
    }

    private SqlNode ParseDropMaterializedView()
    {
        Consume(SqlTokenType.Drop, "SQL630", "期望 'DROP'");
        Consume(SqlTokenType.Materialized, "SQL631", "期望 'MATERIALIZED'");
        Consume(SqlTokenType.View, "SQL632", "期望 'VIEW'");

        var ifExists = false;
        if (Match(SqlTokenType.If))
        {
            Consume(SqlTokenType.Exists, "SQL633", "期望 'EXISTS'");
            ifExists = true;
        }

        var name = ConsumeIdentifier("SQL634", "期望视图名称");

        return new DropMaterializedViewStatement(name, ifExists);
    }

    private SqlNode ParseRefreshMaterializedView()
    {
        Consume(SqlTokenType.Refresh, "SQL640", "期望 'REFRESH'");
        Consume(SqlTokenType.Materialized, "SQL641", "期望 'MATERIALIZED'");
        Consume(SqlTokenType.View, "SQL642", "期望 'VIEW'");

        var name = ConsumeIdentifier("SQL643", "期望视图名称");

        return new RefreshMaterializedViewStatement(name);
    }

    #endregion

    #region 预处理语句

    private SqlNode ParsePrepare()
    {
        Consume(SqlTokenType.Prepare, "SQL650", "期望 'PREPARE'");
        var name = ConsumeIdentifier("SQL651", "期望语句名称");
        Consume(SqlTokenType.From, "SQL652", "期望 'FROM'");
        var rawText = Current.Text;
        Consume(SqlTokenType.String, "SQL653", "期望查询字符串");

        var query = rawText;

        return new PrepareStatement(name, query);
    }

    private SqlNode ParseExecute()
    {
        Consume(SqlTokenType.Execute, "SQL654", "期望 'EXECUTE'");
        var name = ConsumeIdentifier("SQL655", "期望语句名称");

        var parameters = new List<SqlExpression>();
        if (Match(SqlTokenType.Using))
        {
            parameters.Add(ParseExpression());
            while (Match(SqlTokenType.Comma))
            {
                parameters.Add(ParseExpression());
            }
        }

        return new ExecuteStatement(name, parameters);
    }

    private SqlNode ParseDeallocatePrepare()
    {
        if (Current.Type == SqlTokenType.Drop)
        {
            Consume(SqlTokenType.Drop, "SQL656", "期望 'DROP'");
        }
        else
        {
            Consume(SqlTokenType.Deallocate, "SQL656", "期望 'DEALLOCATE'");
        }

        Consume(SqlTokenType.Prepare, "SQL657", "期望 'PREPARE'");
        var name = ConsumeIdentifier("SQL658", "期望语句名称");

        return new DeallocatePrepareStatement(name);
    }

    #endregion

    #region 函数与存储过程

    private SqlNode ParseCreateFunction()
    {
        Consume(SqlTokenType.Create, "SQL201", "期望 'CREATE'");
        Consume(SqlTokenType.Function, "SQL202", "期望 'FUNCTION'");
        var name = ConsumeIdentifier("SQL203", "期望函数名");
        Consume(SqlTokenType.LeftParen, "SQL204", "期望 '('");
        var parameters = new List<ParameterDef>();

        if (!Check(SqlTokenType.RightParen))
        {
            parameters.Add(ParseParameterDef());
            while (Match(SqlTokenType.Comma))
            {
                parameters.Add(ParseParameterDef());
            }
        }

        Consume(SqlTokenType.RightParen, "SQL205", "期望 ')'");
        Consume(SqlTokenType.Returns, "SQL206", "期望 'RETURNS'");
        var returnType = ConsumeIdentifier("SQL207", "期望返回值类型").ToUpperInvariant();
        Consume(SqlTokenType.As, "SQL208", "期望 'AS'");
        var body = ParseExpression();

        return new CreateFunctionStatement(name, parameters, returnType, body);
    }

    private SqlNode ParseCreateProcedure()
    {
        Consume(SqlTokenType.Create, "SQL301", "期望 'CREATE'");
        Consume(SqlTokenType.Procedure, "SQL302", "期望 'PROCEDURE'");
        var name = ConsumeIdentifier("SQL303", "期望存储过程名");
        Consume(SqlTokenType.LeftParen, "SQL304", "期望 '('");
        var parameters = new List<ParameterDef>();

        if (!Check(SqlTokenType.RightParen))
        {
            parameters.Add(ParseParameterDef());
            while (Match(SqlTokenType.Comma))
            {
                parameters.Add(ParseParameterDef());
            }
        }

        Consume(SqlTokenType.RightParen, "SQL305", "期望 ')'");

        var body = new List<SqlNode>();
        Consume(SqlTokenType.Begin, "SQL306", "期望 'BEGIN'");
        while (!Check(SqlTokenType.End) && !IsAtEnd())
        {
            var stmt = ParseProcedureStatement();
            if (stmt is not null)
            {
                body.Add(stmt);
            }

            Match(SqlTokenType.Semicolon);
        }

        Consume(SqlTokenType.End, "SQL307", "期望 'END'");

        return new CreateProcedureStatement(name, parameters, body);
    }

    private SqlNode? ParseProcedureStatement()
    {
        if (Check(SqlTokenType.If))
        {
            return ParseIfStatement();
        }

        return ParseStatement();
    }

    private SqlNode? ParseIfStatement()
    {
        Consume(SqlTokenType.If, "SQL500", "期望 'IF'");
        var condition = ParseExpression();
        Consume(SqlTokenType.Then, "SQL501", "期望 'THEN'");

        var thenBody = new List<SqlNode>();
        while (!Check(SqlTokenType.Elsif) && !Check(SqlTokenType.Else) && !Check(SqlTokenType.EndIf) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt is not null)
            {
                thenBody.Add(stmt);
            }
            Match(SqlTokenType.Semicolon);
        }

        var elseIfClauses = new List<ElseIfClause>();
        IReadOnlyList<SqlNode>? elseBody = null;

        while (Check(SqlTokenType.Elsif))
        {
            Advance();
            var elsifCondition = ParseExpression();
            Consume(SqlTokenType.Then, "SQL502", "期望 'THEN'");

            var elsifBody = new List<SqlNode>();
            while (!Check(SqlTokenType.Elsif) && !Check(SqlTokenType.Else) && !Check(SqlTokenType.EndIf) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt is not null)
                {
                    elsifBody.Add(stmt);
                }
                Match(SqlTokenType.Semicolon);
            }

            elseIfClauses.Add(new ElseIfClause(elsifCondition, elsifBody));
        }

        if (Match(SqlTokenType.Else))
        {
            var elseList = new List<SqlNode>();
            while (!Check(SqlTokenType.EndIf) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt is not null)
                {
                    elseList.Add(stmt);
                }
                Match(SqlTokenType.Semicolon);
            }
            elseBody = elseList;
        }

        Consume(SqlTokenType.EndIf, "SQL503", "期望 'END IF'");

        return new IfStatement(condition, thenBody, elseIfClauses, elseBody);
    }

    private SqlNode ParseCallStmt()
    {
        Consume(SqlTokenType.Call, "SQL401", "期望 'CALL'");
        var name = ConsumeIdentifier("SQL402", "期望存储过程名");
        Consume(SqlTokenType.LeftParen, "SQL403", "期望 '('");
        var args = new List<SqlExpression>();

        if (!Check(SqlTokenType.RightParen))
        {
            args.Add(ParseExpression());
            while (Match(SqlTokenType.Comma))
            {
                args.Add(ParseExpression());
            }
        }

        Consume(SqlTokenType.RightParen, "SQL404", "期望 ')'");

        return new CallStatement(name, args);
    }

    private ParameterDef ParseParameterDef()
    {
        var paramName = ConsumeIdentifier("SQL501", "期望参数名");
        var paramType = ConsumeIdentifier("SQL502", "期望参数类型").ToUpperInvariant();
        return new ParameterDef(paramName, paramType);
    }

    #endregion

    #region 表达式解析

    private IReadOnlyList<SqlExpression> ParseExpressionList()
    {
        var expressions = new List<SqlExpression> { ParseExpression() };
        while (Match(SqlTokenType.Comma)) expressions.Add(ParseExpression());
        return expressions;
    }

    private SqlExpression ParseExpression()
    {
        return ParseOr();
    }

    private SqlExpression ParseOr()
    {
        var left = ParseAnd();

        while (Match(SqlTokenType.Or)) left = new BinaryExpr(left, "OR", ParseAnd());

        return left;
    }

    private SqlExpression ParseAnd()
    {
        var left = ParseNot();

        while (Match(SqlTokenType.And)) left = new BinaryExpr(left, "AND", ParseNot());

        return left;
    }

    private SqlExpression ParseNot()
    {
        if (Match(SqlTokenType.Not)) return new UnaryExpr("NOT", ParseNot());

        return ParseComparison();
    }

    private SqlExpression ParseComparison()
    {
        var left = ParseAddition();

        if (Check(SqlTokenType.Equal)) { Advance(); return new BinaryExpr(left, "=", ParseAddition()); }
        if (Check(SqlTokenType.NotEqual)) { Advance(); return new BinaryExpr(left, "<>", ParseAddition()); }
        if (Check(SqlTokenType.LessThan)) { Advance(); return new BinaryExpr(left, "<", ParseAddition()); }
        if (Check(SqlTokenType.GreaterThan)) { Advance(); return new BinaryExpr(left, ">", ParseAddition()); }
        if (Check(SqlTokenType.LessEqual)) { Advance(); return new BinaryExpr(left, "<=", ParseAddition()); }
        if (Check(SqlTokenType.GreaterEqual)) { Advance(); return new BinaryExpr(left, ">=", ParseAddition()); }

        if (Check(SqlTokenType.Is))
        {
            Advance();
            var negated = Match(SqlTokenType.Not);
            Consume(SqlTokenType.Null, "SQL136", "期望 'NULL'");
            return new IsNullExpr(left, negated);
        }

        if (Check(SqlTokenType.In) || (Check(SqlTokenType.Not) && Peek(1) == SqlTokenType.In))
        {
            var negated = Match(SqlTokenType.Not);
            if (!negated) Advance();
            else
            {
                Consume(SqlTokenType.In, "SQL150", "期望 'IN'");
            }

            Consume(SqlTokenType.LeftParen, "SQL137", "期望 '('");

            if (Check(SqlTokenType.Select))
            {
                var subquery = ParseSelectRaw();
                Consume(SqlTokenType.RightParen, "SQL138", "期望 ')'");
                return new InSubqueryExpr(left, subquery, negated);
            }

            var values = ParseExpressionList();
            Consume(SqlTokenType.RightParen, "SQL138", "期望 ')'");
            return new InExpr(left, values, negated);
        }

        if (Check(SqlTokenType.Between) || (Check(SqlTokenType.Not) && Peek(1) == SqlTokenType.Between))
        {
            var negated = Match(SqlTokenType.Not);
            if (!negated) Advance();
            else
            {
                Consume(SqlTokenType.Between, "SQL151", "期望 'BETWEEN'");
            }

            var low = ParseAddition();
            Consume(SqlTokenType.And, "SQL139", "期望 'AND'");
            var high = ParseAddition();
            return new BetweenExpr(left, low, high, negated);
        }

        if (Check(SqlTokenType.Like) || Check(SqlTokenType.ILike) ||
            (Check(SqlTokenType.Not) && Peek(1) == SqlTokenType.Like) ||
            (Check(SqlTokenType.Not) && Peek(1) == SqlTokenType.ILike))
        {
            var negated = Match(SqlTokenType.Not);
            var isCaseInsensitive = Match(SqlTokenType.ILike) || (!negated && Check(SqlTokenType.ILike));
            if (!negated && !isCaseInsensitive) Advance();
            else if (negated && !Check(SqlTokenType.Like) && !Check(SqlTokenType.ILike))
            {
                Consume(SqlTokenType.Like, "SQL152", "期望 'LIKE' 或 'ILIKE'");
            }
            else if (negated && Check(SqlTokenType.ILike))
            {
                Consume(SqlTokenType.ILike, "SQL153", "期望 'ILIKE'");
            }
            else if (isCaseInsensitive)
            {
                Consume(SqlTokenType.ILike, "SQL154", "期望 'ILIKE'");
            }

            return new LikeExpr(left, ParseAddition(), negated, isCaseInsensitive);
        }

        if (
            Check(SqlTokenType.Glob) || (Check(SqlTokenType.Not) && Peek(1) == SqlTokenType.Glob))
        {
            var negated = Match(SqlTokenType.Not);
            if (!negated) Advance();
            else
            {
                Consume(SqlTokenType.Glob, "SQL209", "期望 'GLOB'");
            }

            return new LikeExpr(left, ParseAddition(), negated);
        }

        return left;
    }

    private SqlExpression ParseAddition()
    {
        var left = ParseMultiplication();

        while (Current.Text is "+" or "-" or "||" or "|")
        {
            var op = Advance().Text;
            left = new BinaryExpr(left, op, ParseMultiplication());
        }

        return left;
    }

    private SqlExpression ParseMultiplication()
    {
        var left = ParseUnary();

        while (Current.Text is "*" or "/" or "%" or "&" or "<<" or ">>")
        {
            var op = Advance().Text;
            left = new BinaryExpr(left, op, ParseUnary());
        }

        return left;
    }

    private SqlExpression ParseUnary()
    {
        if (Current.Text == "-")
        {
            Advance();
            return new UnaryExpr("-", ParsePrimary());
        }

        if (Current.Text == "~" || Current.Type == SqlTokenType.Tilde)
        {
            Advance();
            return new UnaryExpr("~", ParsePrimary());
        }

        return ParsePrimary();
    }

    private SqlExpression ParsePrimary()
    {
        if (Match(SqlTokenType.Case)) return ParseCaseExpr();
        if (Match(SqlTokenType.Cast)) return ParseCastExpr();
        if (Match(SqlTokenType.Exists)) return ParseExistsExpr();
        if (Match(SqlTokenType.True)) return new LiteralValue("TRUE", SqlTokenType.True);
        if (Match(SqlTokenType.False)) return new LiteralValue("FALSE", SqlTokenType.False);

        if (Match(SqlTokenType.Not))
        {
            if (Check(SqlTokenType.Exists))
            {
                Advance();
                return ParseExistsExpr(true);
            }

            return new UnaryExpr("NOT", ParsePrimary());
        }

        if (Match(SqlTokenType.LeftParen))
        {
            if (Check(SqlTokenType.Select))
            {
                var query = ParseSelectRaw();
                Consume(SqlTokenType.RightParen, "SQL148", "期望 ')'");
                return new SubqueryExpr(query);
            }

            var expr = ParseExpression();
            Consume(SqlTokenType.RightParen, "SQL149", "期望 ')'");

            if (Check(SqlTokenType.Collate))
            {
                Advance();
                return new CollateExpr(expr, ConsumeIdentifier("SQL153", "期望排序规则名"));
            }

            return expr;
        }

        if (Match(SqlTokenType.Star)) return new StarExpression();

        if (Check(SqlTokenType.Number) || Check(SqlTokenType.String) || Check(SqlTokenType.Null))
        {
            var token = Advance();
            return token.Type switch
            {
                SqlTokenType.Number => new LiteralValue(token.Text, SqlTokenType.Number),
                SqlTokenType.String => new LiteralValue(token.Text, SqlTokenType.String),
                SqlTokenType.Null => new LiteralValue(null, SqlTokenType.Null),
                _ => throw new InvalidOperationException()
            };
        }

        if (Check(SqlTokenType.Identifier))
        {
            var text = Advance().Text;
            if (Peek(0) == SqlTokenType.Dot && Peek(1) == SqlTokenType.Star)
            {
                Advance();
                Advance();
                return new StarExpression(text);
            }

            if (Check(SqlTokenType.Dot))
            {
                Advance();
                var col = Advance().Text;
                return new ColumnRef(col, text);
            }

            if (Match(SqlTokenType.LeftParen))
            {
                return ParseFunctionCall(text);
            }

            if (Check(SqlTokenType.Collate))
            {
                var colRef = new ColumnRef(text);
                Advance();
                return new CollateExpr(colRef, ConsumeIdentifier("SQL153", "期望排序规则名"));
            }

            return new ColumnRef(text);
        }

        if (IsKeyword(Current.Type))
        {
            var text = Current.Text;
            if (Peek(1) == SqlTokenType.LeftParen)
            {
                Advance();
                Advance();
                return ParseFunctionCall(text);
            }

            return new ColumnRef(Advance().Text);
        }

        _diagnostics?.AddError(string.Empty, default,
            "SQL143", $"意外的词法单元：{Current.Type}");
        Advance();
        return new LiteralValue(null, SqlTokenType.Null);
    }

    #endregion

    #region 辅助方法

    private bool IsAtEnd()
    {
        return Current.Type == SqlTokenType.EndOfFile;
    }

    private bool Check(SqlTokenType type)
    {
        return Current.Type == type;
    }

    private bool Match(SqlTokenType type)
    {
        if (Current.Type != type) return false;
        Advance();
        return true;
    }

    private SqlToken Advance()
    {
        var token = Current;
        if (_position < _tokens.Count - 1) _position++;
        return token;
    }

    private SqlToken Consume(SqlTokenType type, string errorCode, string message)
    {
        if (Current.Type == type) return Advance();

        _diagnostics?.AddError(string.Empty, default,
            errorCode, $"{message}，实际遇到 {Current.Type}");

        return Current;
    }

    private string ConsumeIdentifier(string errorCode, string message)
    {
        if (Current.Type == SqlTokenType.Identifier) return Advance().Text;

        if (IsKeyword(Current.Type)) return Advance().Text;

        _diagnostics?.AddError(string.Empty, default,
            errorCode, $"{message}，实际遇到 {Current.Type}");

        return Current.Text;
    }

    private int ParseIntegerValue()
    {
        if (Current.Type == SqlTokenType.Number && int.TryParse(Advance().Text, out var value)) return value;

        _diagnostics?.AddError(string.Empty, default,
            "SQL143", "期望整数值");

        return 0;
    }

    private static bool IsKeyword(SqlTokenType type)
    {
        return type is >= SqlTokenType.Select and <= SqlTokenType.Excluded;
    }

    private int ConsumeNumber()
    {
        if (Current.Type == SqlTokenType.Number && int.TryParse(Advance().Text, out var value)) return value;

        _diagnostics?.AddError(string.Empty, default, "SQL144", "期望数值");

        return 0;
    }

    private SelectStatement ParseSelectRaw()
    {
        Consume(SqlTokenType.Select, "SQL190", "期望 'SELECT'");

        var distinct = Match(SqlTokenType.Distinct);
        var columns = ParseColumnList();

        SqlTableRef? from = null;
        if (Match(SqlTokenType.From)) from = ParseTableRef();

        var joins = new List<SqlTableRef>();
        while (IsJoinKeyword()) joins.Add(ParseJoin());

        SqlExpression? where = null;
        if (Match(SqlTokenType.Where)) where = ParseExpression();

        var groupBy = new List<SqlExpression>();
        if (Match(SqlTokenType.Group))
        {
            Consume(SqlTokenType.By, "SQL191", "期望 'BY'");
            groupBy.Add(ParseExpression());
            while (Match(SqlTokenType.Comma)) groupBy.Add(ParseExpression());
        }

        SqlExpression? having = null;
        if (Match(SqlTokenType.Having)) having = ParseExpression();

        var orderBy = new List<OrderByItem>();
        if (Match(SqlTokenType.Order))
        {
            Consume(SqlTokenType.By, "SQL192", "期望 'BY'");
            orderBy.Add(ParseOrderByItem());
            while (Match(SqlTokenType.Comma)) orderBy.Add(ParseOrderByItem());
        }

        int? limit = null;
        if (Match(SqlTokenType.Limit)) limit = ConsumeNumber();

        int? offset = null;
        if (Match(SqlTokenType.Offset)) offset = ConsumeNumber();

        return new SelectStatement(columns, from, where, joins, groupBy, having, orderBy, limit, offset, distinct);
    }

    private CaseExpr ParseCaseExpr()
    {
        var whenClauses = new List<(SqlExpression When, SqlExpression Then)>();

        Consume(SqlTokenType.When, "SQL193", "期望 'WHEN'");
        var when = ParseExpression();
        Consume(SqlTokenType.Then, "SQL194", "期望 'THEN'");
        var then = ParseExpression();
        whenClauses.Add((when, then));

        while (Match(SqlTokenType.When))
        {
            when = ParseExpression();
            Consume(SqlTokenType.Then, "SQL194", "期望 'THEN'");
            then = ParseExpression();
            whenClauses.Add((when, then));
        }

        SqlExpression? elseExpr = null;
        if (Match(SqlTokenType.Else)) elseExpr = ParseExpression();

        Consume(SqlTokenType.End, "SQL195", "期望 'END'");

        return new CaseExpr(whenClauses, elseExpr);
    }

    private CastExpr ParseCastExpr()
    {
        Consume(SqlTokenType.LeftParen, "SQL196", "期望 '('");
        var expr = ParseExpression();
        Consume(SqlTokenType.As, "SQL197", "期望 'AS'");
        var type = ParseTypeName();
        Consume(SqlTokenType.RightParen, "SQL198", "期望 ')'");

        return new CastExpr(expr, type);
    }

    private ExistsExpr ParseExistsExpr(bool negated = false)
    {
        if (!negated) Consume(SqlTokenType.Exists, "SQL199", "期望 'EXISTS'");
        Consume(SqlTokenType.LeftParen, "SQL200", "期望 '('");
        var subquery = ParseSelectRaw();
        Consume(SqlTokenType.RightParen, "SQL201", "期望 ')'");

        return new ExistsExpr(subquery, negated);
    }

    private FunctionCall ParseFunctionCall(string name)
    {
        var distinct = Match(SqlTokenType.Distinct);
        var args = new List<SqlExpression>();
        if (!Check(SqlTokenType.RightParen))
        {
            args.Add(ParseExpression());
            while (Match(SqlTokenType.Comma)) args.Add(ParseExpression());
        }

        Consume(SqlTokenType.RightParen, "SQL202", "期望 ')'");
        return new FunctionCall(name, args, distinct);
    }

    /// <summary>
    ///     解析类型名，支持带长度参数的类型如 VARCHAR(255)、DECIMAL(10,2)
    /// </summary>
    private string ParseTypeName()
    {
        var sb = new StringBuilder();

        while (true)
        {
            if (!Check(SqlTokenType.Identifier) &&
                !Check(SqlTokenType.Integer) && !Check(SqlTokenType.Real) &&
                !Check(SqlTokenType.Text) && !Check(SqlTokenType.Blob) &&
                !Check(SqlTokenType.Varchar) && !Check(SqlTokenType.Boolean) &&
                !Check(SqlTokenType.Date) && !Check(SqlTokenType.Timestamp) &&
                !Check(SqlTokenType.BigInt) && !Check(SqlTokenType.SmallInt) &&
                !Check(SqlTokenType.TinyInt) && !Check(SqlTokenType.Float) &&
                !Check(SqlTokenType.Double) && !Check(SqlTokenType.Numeric) &&
                !Check(SqlTokenType.Decimal) && !Check(SqlTokenType.Char) &&
                !Check(SqlTokenType.NChar) && !Check(SqlTokenType.Binary))
            {
                break;
            }

            if (sb.Length > 0) sb.Append(' ');
            sb.Append(Advance().Text);
        }

        if (Match(SqlTokenType.LeftParen))
        {
            sb.Append('(');
            while (!Check(SqlTokenType.RightParen) && !IsAtEnd())
            {
                sb.Append(Advance().Text);
            }

            Consume(SqlTokenType.RightParen, "SQL203", "期望 ')'");
            sb.Append(')');
        }

        return sb.ToString();
    }

    #endregion
}
