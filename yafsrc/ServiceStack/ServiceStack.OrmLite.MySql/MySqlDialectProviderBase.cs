﻿// ***********************************************************************
// <copyright file="MySqlDialectProviderBase.cs" company="ServiceStack, Inc.">
//     Copyright (c) ServiceStack, Inc. All Rights Reserved.
// </copyright>
// <summary>Fork for YetAnotherForum.NET, Licensed under the Apache License, Version 2.0</summary>
// ***********************************************************************
namespace ServiceStack.OrmLite.MySql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using global::MySql.Data.MySqlClient;

    using ServiceStack.OrmLite.MySql.Converters;
    using ServiceStack.OrmLite.MySql.DataAnnotations;
    using ServiceStack.Script;
    using ServiceStack.Text;

    /// <summary>
    /// Class MySqlDialectProviderBase.
    /// Implements the <see cref="ServiceStack.OrmLite.OrmLiteDialectProviderBase{TDialect}" />
    /// </summary>
    /// <typeparam name="TDialect">The type of the t dialect.</typeparam>
    /// <seealso cref="ServiceStack.OrmLite.OrmLiteDialectProviderBase{TDialect}" />
    public abstract class MySqlDialectProviderBase<TDialect> : OrmLiteDialectProviderBase<TDialect> where TDialect : IOrmLiteDialectProvider
    {

        /// <summary>
        /// The text column definition
        /// </summary>
        private const string TextColumnDefinition = "TEXT";

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlDialectProviderBase{TDialect}"/> class.
        /// </summary>
        public MySqlDialectProviderBase()
        {
            base.AutoIncrementDefinition = "AUTO_INCREMENT";
            base.DefaultValueFormat = " DEFAULT {0}";
            base.SelectIdentitySql = "SELECT LAST_INSERT_ID()";

            base.InitColumnTypeMap();

            base.RegisterConverter<string>(new MySqlStringConverter());
            base.RegisterConverter<char[]>(new MySqlCharArrayConverter());
            base.RegisterConverter<bool>(new MySqlBoolConverter());

            base.RegisterConverter<byte>(new MySqlByteConverter());
            base.RegisterConverter<sbyte>(new MySqlSByteConverter());
            base.RegisterConverter<short>(new MySqlInt16Converter());
            base.RegisterConverter<ushort>(new MySqlUInt16Converter());
            base.RegisterConverter<int>(new MySqlInt32Converter());
            base.RegisterConverter<uint>(new MySqlUInt32Converter());

            base.RegisterConverter<decimal>(new MySqlDecimalConverter());

            base.RegisterConverter<Guid>(new MySqlGuidConverter());
            base.RegisterConverter<DateTimeOffset>(new MySqlDateTimeOffsetConverter());

            this.Variables = new Dictionary<string, string>
            {
                { OrmLiteVariables.SystemUtc, "CURRENT_TIMESTAMP" },
                { OrmLiteVariables.MaxText, "LONGTEXT" },
                { OrmLiteVariables.MaxTextUnicode, "LONGTEXT" },
                { OrmLiteVariables.True, SqlBool(true) },
                { OrmLiteVariables.False, SqlBool(false) },
            };
        }

        /// <summary>
        /// The row version trigger format
        /// </summary>
        public static string RowVersionTriggerFormat = "{0}RowVersionUpdateTrigger";

        /// <summary>
        /// The reserved words
        /// </summary>
        public static HashSet<string> ReservedWords = new(new[]
        {
          "ACCESSIBLE",
          "ADD",
          "ALL",
          "ALTER",
          "ANALYZE",
          "AND",
          "AS",
          "ASC",
          "ASENSITIVE",
          "BEFORE",
          "BETWEEN",
          "BIGINT",
          "BINARY",
          "BLOB",
          "BOTH",
          "BY",
          "CALL",
          "CASCADE",
          "CASE",
          "CHANGE",
          "CHAR",
          "CHARACTER",
          "CHECK",
          "COLLATE",
          "COLUMN",
          "CONDITION",
          "CONSTRAINT",
          "CONTINUE",
          "CONVERT",
          "CREATE",
          "CROSS",
          "CUBE",
          "CUME_DIST",
          "CURRENT_DATE",
          "CURRENT_TIME",
          "CURRENT_TIMESTAMP",
          "CURRENT_USER",
          "CURSOR",
          "DATABASE",
          "DATABASES",
          "DAY_HOUR",
          "DAY_MICROSECOND",
          "DAY_MINUTE",
          "DAY_SECOND",
          "DEC",
          "DECIMAL",
          "DECLARE",
          "DEFAULT",
          "DELAYED",
          "DELETE",
          "DENSE_RANK",
          "DESC",
          "DESCRIBE",
          "DETERMINISTIC",
          "DISTINCT",
          "DISTINCTROW",
          "DIV",
          "DOUBLE",
          "DROP",
          "DUAL",
          "EACH",
          "ELSE",
          "ELSEIF",
          "EMPTY",
          "ENCLOSED",
          "ESCAPED",
          "EXCEPT",
          "EXISTS",
          "EXIT",
          "EXPLAIN",
          "FALSE",
          "FETCH",
          "FIRST_VALUE",
          "FLOAT",
          "FLOAT4",
          "FLOAT8",
          "FOR",
          "FORCE",
          "FOREIGN",
          "FROM",
          "FULLTEXT",
          "FUNCTION",
          "GENERATED",
          "GET",
          "GRANT",
          "GROUP",
          "GROUPING",
          "GROUPS",
          "HAVING",
          "HIGH_PRIORITY",
          "HOUR_MICROSECOND",
          "HOUR_MINUTE",
          "HOUR_SECOND",
          "IF",
          "IGNORE",
          "IN",
          "INDEX",
          "INFILE",
          "INNER",
          "INOUT",
          "INSENSITIVE",
          "INSERT",
          "INT",
          "INT1",
          "INT2",
          "INT3",
          "INT4",
          "INT8",
          "INTEGER",
          "INTERVAL",
          "INTO",
          "IO_AFTER_GTIDS",
          "IO_BEFORE_GTIDS",
          "IS",
          "ITERATE",
          "JOIN",
          "JSON_TABLE",
          "KEY",
          "KEYS",
          "KILL",
          "LAG",
          "LAST_VALUE",
          "LEAD",
          "LEADING",
          "LEAVE",
          "LEFT",
          "LIKE",
          "LIMIT",
          "LINEAR",
          "LINES",
          "LOAD",
          "LOCALTIME",
          "LOCALTIMESTAMP",
          "LOCK",
          "LONG",
          "LONGBLOB",
          "LONGTEXT",
          "LOOP",
          "LOW_PRIORITY",
          "MASTER_BIND",
          "MASTER_SSL_VERIFY_SERVER_CERT",
          "MATCH",
          "MAXVALUE",
          "MEDIUMBLOB",
          "MEDIUMINT",
          "MEDIUMTEXT",
          "MIDDLEINT",
          "MINUTE_MICROSECOND",
          "MINUTE_SECOND",
          "MOD",
          "MODIFIES",
          "NATURAL",
          "NOT",
          "NO_WRITE_TO_BINLOG",
          "NTH_VALUE",
          "NTILE",
          "NULL",
          "NUMERIC",
          "OF",
          "ON",
          "OPTIMIZE",
          "OPTIMIZER_COSTS",
          "OPTION",
          "OPTIONALLY",
          "OR",
          "ORDER",
          "OUT",
          "OUTER",
          "OUTFILE",
          "OVER",
          "PARTITION",
          "PERCENT_RANK",
          "PERSIST",
          "PERSIST_ONLY",
          "PRECISION",
          "PRIMARY",
          "PROCEDURE",
          "PURGE",
          "RANGE",
          "RANK",
          "READ",
          "READS",
          "READ_WRITE",
          "REAL",
          "RECURSIVE",
          "REFERENCES",
          "REGEXP",
          "RELEASE",
          "RENAME",
          "REPEAT",
          "REPLACE",
          "REQUIRE",
          "RESIGNAL",
          "RESTRICT",
          "RETURN",
          "REVOKE",
          "RIGHT",
          "RLIKE",
          "ROW",
          "ROWS",
          "ROW_NUMBER",
          "SCHEMA",
          "SCHEMAS",
          "SECOND_MICROSECOND",
          "SELECT",
          "SENSITIVE",
          "SEPARATOR",
          "SET",
          "SHOW",
          "SIGNAL",
          "SMALLINT",
          "SPATIAL",
          "SPECIFIC",
          "SQL",
          "SQLEXCEPTION",
          "SQLSTATE",
          "SQLWARNING",
          "SQL_BIG_RESULT",
          "SQL_CALC_FOUND_ROWS",
          "SQL_SMALL_RESULT",
          "SSL",
          "STARTING",
          "STORED",
          "STRAIGHT_JOIN",
          "SYSTEM",
          "TABLE",
          "TERMINATED",
          "THEN",
          "TINYBLOB",
          "TINYINT",
          "TINYTEXT",
          "TO",
          "TRAILING",
          "TRIGGER",
          "TRUE",
          "UNDO",
          "UNION",
          "UNIQUE",
          "UNLOCK",
          "UNSIGNED",
          "UPDATE",
          "USAGE",
          "USE",
          "USING",
          "UTC_DATE",
          "UTC_TIME",
          "UTC_TIMESTAMP",
          "VALUES",
          "VARBINARY",
          "VARCHAR",
          "VARCHARACTER",
          "VARYING",
          "VIRTUAL",
          "WHEN",
          "WHERE",
          "WHILE",
          "WINDOW",
          "WITH",
          "WRITE",
          "XOR",
          "YEAR_MONTH",
          "ZEROFILL",
        }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the load children sub select.
        /// </summary>
        /// <typeparam name="From">The type of from.</typeparam>
        /// <param name="expr">The expr.</param>
        /// <returns>System.String.</returns>
        public override string GetLoadChildrenSubSelect<From>(SqlExpression<From> expr)
        {
            return $"SELECT * FROM ({base.GetLoadChildrenSubSelect(expr)}) AS SubQuery";
        }

        /// <summary>
        /// Converts to postdroptablestatement.
        /// </summary>
        /// <param name="modelDef">The model definition.</param>
        /// <returns>System.String.</returns>
        public override string ToPostDropTableStatement(ModelDefinition modelDef)
        {
            if (modelDef.RowVersion == null)
            {
                return null;
            }

            var triggerName = RowVersionTriggerFormat.Fmt(GetTableName(modelDef));
            return "DROP TRIGGER IF EXISTS {0}".Fmt(GetQuotedName(triggerName));
        }

        /// <summary>
        /// Converts to postcreatetablestatement.
        /// </summary>
        /// <param name="modelDef">The model definition.</param>
        /// <returns>System.String.</returns>
        public override string ToPostCreateTableStatement(ModelDefinition modelDef)
        {
            if (modelDef.RowVersion == null)
            {
                return null;
            }

            var triggerName = RowVersionTriggerFormat.Fmt(modelDef.ModelName);
            var triggerBody = "SET NEW.{0} = OLD.{0} + 1;".Fmt(
                modelDef.RowVersion.FieldName.SqlColumn(this));

            var sql = "CREATE TRIGGER {0} BEFORE UPDATE ON {1} FOR EACH ROW BEGIN {2} END;".Fmt(
                triggerName, this.GetTableName(modelDef), triggerBody);

            return sql;

        }

        /// <summary>
        /// Quote the string so that it can be used inside an SQL-expression
        /// Escape quotes inside the string
        /// </summary>
        /// <param name="paramValue">The parameter value.</param>
        /// <returns>System.String.</returns>
        public override string GetQuotedValue(string paramValue)
        {
            return "'" + paramValue.Replace("\\", "\\\\").Replace("'", @"\'") + "'";
        }

        /// <summary>
        /// Gets the quoted value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fieldType">Type of the field.</param>
        /// <returns>System.String.</returns>
        public override string GetQuotedValue(object value, Type fieldType)
        {
            if (value == null)
                return "NULL";

            if (fieldType == typeof(byte[]))
                return "0x" + BitConverter.ToString((byte[])value).Replace("-", "");

            return base.GetQuotedValue(value, fieldType);
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="schema">The schema.</param>
        /// <returns>System.String.</returns>
        public override string GetTableName(string table, string schema = null) =>
            GetTableName(table, schema, useStrategy:true);

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="useStrategy">if set to <c>true</c> [use strategy].</param>
        /// <returns>System.String.</returns>
        public override string GetTableName(string table, string schema, bool useStrategy)
        {
            if (useStrategy)
            {
                return schema != null && !table.StartsWithIgnoreCase(schema + "_")
                    ? QuoteIfRequired(NamingStrategy.GetSchemaName(schema) + "_" + NamingStrategy.GetTableName(table))
                    : QuoteIfRequired(NamingStrategy.GetTableName(table));
            }

            return schema != null && !table.StartsWithIgnoreCase(schema + "_")
                ? QuoteIfRequired(schema + "_" + table)
                : QuoteIfRequired(table);
        }

        /// <summary>
        /// Shoulds the quote.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool ShouldQuote(string name) => name != null &&
            (ReservedWords.Contains(name) || name.IndexOf(' ') >= 0 || name.IndexOf('.') >= 0);

        /// <summary>
        /// Gets the name of the quoted.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        public override string GetQuotedName(string name) => name == null ? null : name.FirstCharEquals('`')
            ? name : '`' + name + '`';

        /// <summary>
        /// Gets the name of the quoted table.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schema">The schema.</param>
        /// <returns>System.String.</returns>
        public override string GetQuotedTableName(string tableName, string schema = null)
        {
            return GetQuotedName(GetTableName(tableName, schema));
        }

        /// <summary>
        /// SQLs the expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>SqlExpression&lt;T&gt;.</returns>
        public override SqlExpression<T> SqlExpression<T>()
        {
            return new MySqlExpression<T>(this);
        }

        /// <summary>
        /// Converts to tablenamesstatement.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <returns>System.String.</returns>
        public override string ToTableNamesStatement(string schema)
        {
            return schema == null
                ? "SELECT table_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE()"
                : "SELECT table_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE() AND table_name LIKE {0}".SqlFmt(this, NamingStrategy.GetSchemaName(schema)  + "\\_%");
        }

        /// <summary>
        /// Return table, row count SQL for listing all tables with their row counts
        /// </summary>
        /// <param name="live">If true returns live current row counts of each table (slower), otherwise returns cached row counts from RDBMS table stats</param>
        /// <param name="schema">The table schema if any</param>
        /// <returns>System.String.</returns>
        public override string ToTableNamesWithRowCountsStatement(bool live, string schema)
        {
            if (live)
                return null;

            return schema == null
                ? "SELECT table_name, table_rows FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE()"
                : "SELECT table_name, table_rows FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE() AND table_name LIKE {0}".SqlFmt(this, NamingStrategy.GetSchemaName(schema)  + "\\_%");
        }

        /// <summary>
        /// Doeses the table exist.
        /// </summary>
        /// <param name="dbCmd">The database command.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schema">The schema.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = {1}"
                .SqlFmt(GetTableName(tableName, schema).StripDbQuotes(), dbCmd.Connection.Database);

            var result = dbCmd.ExecLongScalar(sql);

            return result > 0;
        }

        /// <summary>
        /// Does table exist as an asynchronous operation.
        /// </summary>
        /// <param name="dbCmd">The database command.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;System.Boolean&gt; representing the asynchronous operation.</returns>
        public override async Task<bool> DoesTableExistAsync(IDbCommand dbCmd, string tableName, string schema = null, CancellationToken token=default)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = {1}"
                .SqlFmt(GetTableName(tableName, schema).StripDbQuotes(), dbCmd.Connection.Database);

            var result = await dbCmd.ExecLongScalarAsync(sql, token);

            return result > 0;
        }

        /// <summary>
        /// Doeses the column exist.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schema">The schema.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string schema = null)
        {
            tableName = GetTableName(tableName, schema).StripQuotes();
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS"
                      + " WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName AND TABLE_SCHEMA = @schema"
                          .SqlFmt(GetTableName(tableName, schema).StripDbQuotes(), columnName);

            var result = db.SqlScalar<long>(sql, new { tableName, columnName, schema = db.Database });

            return result > 0;
        }

        /// <summary>
        /// Does column exist as an asynchronous operation.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;System.Boolean&gt; representing the asynchronous operation.</returns>
        public override async Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, string tableName, string schema = null, CancellationToken token=default)
        {
            tableName = GetTableName(tableName, schema).StripQuotes();
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS"
                      + " WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName AND TABLE_SCHEMA = @schema"
                          .SqlFmt(GetTableName(tableName, schema).StripDbQuotes(), columnName);

            var result = await db.SqlScalarAsync<long>(sql, new { tableName, columnName, schema = db.Database }, token);

            return result > 0;
        }

        /// <summary>
        /// Converts to createtablestatement.
        /// </summary>
        /// <param name="tableType">Type of the table.</param>
        /// <returns>System.String.</returns>
        public override string ToCreateTableStatement(Type tableType)
        {
            var sbColumns = StringBuilderCache.Allocate();
            var sbConstraints = StringBuilderCache.Allocate();

            var modelDef = GetModel(tableType);
            foreach (var fieldDef in CreateTableFieldsStrategy(modelDef))
            {
                if (fieldDef.CustomSelect != null || fieldDef.IsComputed && !fieldDef.IsPersisted)
                    continue;

                if (sbColumns.Length != 0) sbColumns.Append(", \n  ");

                sbColumns.Append(GetColumnDefinition(fieldDef, modelDef));

                var sqlConstraint = GetCheckConstraint(modelDef, fieldDef);
                if (sqlConstraint != null)
                {
                    sbConstraints.Append(",\n" + sqlConstraint);
                }

                if (fieldDef.ForeignKey == null || OrmLiteConfig.SkipForeignKeys)
                    continue;

                var refModelDef = GetModel(fieldDef.ForeignKey.ReferenceType);
                sbConstraints.AppendFormat(
                    ", \n\n  CONSTRAINT {0} FOREIGN KEY ({1}) REFERENCES {2} ({3})",
                    GetQuotedName(fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, NamingStrategy, fieldDef)),
                    GetQuotedColumnName(fieldDef.FieldName),
                    GetQuotedTableName(refModelDef),
                    GetQuotedColumnName(refModelDef.PrimaryKey.FieldName));

                if (!string.IsNullOrEmpty(fieldDef.ForeignKey.OnDelete))
                    sbConstraints.AppendFormat(" ON DELETE {0}", fieldDef.ForeignKey.OnDelete);

                if (!string.IsNullOrEmpty(fieldDef.ForeignKey.OnUpdate))
                    sbConstraints.AppendFormat(" ON UPDATE {0}", fieldDef.ForeignKey.OnUpdate);
            }

            var uniqueConstraints = GetUniqueConstraints(modelDef);
            if (uniqueConstraints != null)
            {
                sbConstraints.Append(",\n" + uniqueConstraints);
            }

            if (modelDef.CompositePrimaryKeys.Any())
            {
                sbConstraints.Append(",\n");

                sbConstraints.Append(" PRIMARY KEY (");

                sbConstraints.Append(
                    modelDef.CompositePrimaryKeys.FirstOrDefault().FieldNames.Map(f => modelDef.GetQuotedName(f, this))
                        .Join(","));

                sbConstraints.Append(") ");
            }

            var sql = $"CREATE TABLE {GetQuotedTableName(modelDef)} \n(\n  {StringBuilderCache.ReturnAndFree(sbColumns)}{StringBuilderCacheAlt.ReturnAndFree(sbConstraints)} \n); \n";

            return sql;
        }

        /// <summary>
        /// Doeses the schema exist.
        /// </summary>
        /// <param name="dbCmd">The database command.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName)
        {
            // schema is prefixed to table name
            return true;
        }

        /// <summary>
        /// Converts to createschemastatement.
        /// </summary>
        /// <param name="schemaName">Name of the schema.</param>
        /// <returns>System.String.</returns>
        public override string ToCreateSchemaStatement(string schemaName)
        {
            // https://mariadb.com/kb/en/library/create-database/
            return "SELECT 1";
        }

        /// <summary>
        /// Gets the column definition.
        /// </summary>
        /// <param name="fieldDef">The field definition.</param>
        /// <returns>System.String.</returns>
        public override string GetColumnDefinition(FieldDefinition fieldDef)
        {
            if (fieldDef.PropertyInfo?.HasAttributeCached<TextAttribute>() == true)
            {
                var sql = StringBuilderCache.Allocate();
                sql.AppendFormat("{0} {1}", GetQuotedName(NamingStrategy.GetColumnName(fieldDef.FieldName)), TextColumnDefinition);
                sql.Append(fieldDef.IsNullable ? " NULL" : " NOT NULL");
                return StringBuilderCache.ReturnAndFree(sql);
            }

            var ret = base.GetColumnDefinition(fieldDef);

            return fieldDef.IsRowVersion ? $"{ret} DEFAULT 1" : ret;
        }

        /// <summary>
        /// Gets the column definition.
        /// </summary>
        /// <param name="fieldDef">The field definition.</param>
        /// <param name="modelDef">The model definition.</param>
        /// <returns>System.String.</returns>
        public override string GetColumnDefinition(FieldDefinition fieldDef, ModelDefinition modelDef)
        {
            if (fieldDef.PropertyInfo?.HasAttributeCached<TextAttribute>() == true)
            {
                var sql = StringBuilderCache.Allocate();
                sql.AppendFormat("{0} {1}", this.GetQuotedName(this.NamingStrategy.GetColumnName(fieldDef.FieldName)), TextColumnDefinition);
                sql.Append(fieldDef.IsNullable ? " NULL" : " NOT NULL");
                return StringBuilderCache.ReturnAndFree(sql);
            }

            var ret = base.GetColumnDefinition(fieldDef, modelDef);

            return fieldDef.IsRowVersion ? $"{ret} DEFAULT 1" : ret;
        }

        /// <summary>
        /// SQLs the conflict.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="conflictResolution">The conflict resolution.</param>
        /// <returns>System.String.</returns>
        public override string SqlConflict(string sql, string conflictResolution)
        {
            var parts = sql.SplitOnFirst(' ');
            return $"{parts[0]} {conflictResolution} {parts[1]}";
        }

        /// <summary>
        /// SQLs the currency.
        /// </summary>
        /// <param name="fieldOrValue">The field or value.</param>
        /// <param name="currencySymbol">The currency symbol.</param>
        /// <returns>System.String.</returns>
        public override string SqlCurrency(string fieldOrValue, string currencySymbol) =>
            SqlConcat(new[] {$"'{currencySymbol}'", $"cast({fieldOrValue} as decimal(15,2))"});

        /// <summary>
        /// SQLs the cast.
        /// </summary>
        /// <param name="fieldOrValue">The field or value.</param>
        /// <param name="castAs">The cast as.</param>
        /// <returns>System.String.</returns>
        public override string SqlCast(object fieldOrValue, string castAs) =>
            castAs == Sql.VARCHAR
                ? $"CAST({fieldOrValue} AS CHAR(1000))"
                : $"CAST({fieldOrValue} AS {castAs})";

        /// <summary>
        /// SQLs the bool.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns>System.String.</returns>
        public override string SqlBool(bool value) => value ? "1" : "0";

        /// <summary>
        /// Enables the foreign keys check.
        /// </summary>
        /// <param name="cmd">The command.</param>
        public override void EnableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("SET FOREIGN_KEY_CHECKS=1;");
        /// <summary>
        /// Enables the foreign keys check asynchronous.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task.</returns>
        public override Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) =>
            cmd.ExecNonQueryAsync("SET FOREIGN_KEY_CHECKS=1;", null, token);
        /// <summary>
        /// Disables the foreign keys check.
        /// </summary>
        /// <param name="cmd">The command.</param>
        public override void DisableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("SET FOREIGN_KEY_CHECKS=0;");
        /// <summary>
        /// Disables the foreign keys check asynchronous.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task.</returns>
        public override Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) =>
            cmd.ExecNonQueryAsync("SET FOREIGN_KEY_CHECKS=0;", null, token);

        /// <summary>
        /// Unwraps the specified database.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <returns>DbConnection.</returns>
        protected DbConnection Unwrap(IDbConnection db)
        {
            return (DbConnection)db.ToDbConnection();
        }

        /// <summary>
        /// Unwraps the specified command.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>DbCommand.</returns>
        protected DbCommand Unwrap(IDbCommand cmd)
        {
            return (DbCommand)cmd.ToDbCommand();
        }

        /// <summary>
        /// Unwraps the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>DbDataReader.</returns>
        protected DbDataReader Unwrap(IDataReader reader)
        {
            return (DbDataReader)reader;
        }

#if ASYNC
        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task.</returns>
        public override Task OpenAsync(IDbConnection db, CancellationToken token = default)
        {
            return Unwrap(db).OpenAsync(token);
        }

        /// <summary>
        /// Executes the reader asynchronous.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;IDataReader&gt;.</returns>
        public override Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken token = default)
        {
            return Unwrap(cmd).ExecuteReaderAsync(token).Then(x => (IDataReader)x);
        }

        /// <summary>
        /// Executes the non query asynchronous.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;System.Int32&gt;.</returns>
        public override Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken token = default)
        {
            return Unwrap(cmd).ExecuteNonQueryAsync(token);
        }

        /// <summary>
        /// Executes the scalar asynchronous.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;System.Object&gt;.</returns>
        public override Task<object> ExecuteScalarAsync(IDbCommand cmd, CancellationToken token = default)
        {
            return Unwrap(cmd).ExecuteScalarAsync(token);
        }

        /// <summary>
        /// Reads the asynchronous.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public override Task<bool> ReadAsync(IDataReader reader, CancellationToken token = default)
        {
            return Unwrap(reader).ReadAsync(token);
        }

        /// <summary>
        /// Readers the each.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="fn">The function.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;List&lt;T&gt;&gt;.</returns>
        public override async Task<List<T>> ReaderEach<T>(IDataReader reader, Func<T> fn, CancellationToken token = default)
        {
            try
            {
                var to = new List<T>();
                while (await ReadAsync(reader, token).ConfigureAwait(false))
                {
                    var row = fn();
                    to.Add(row);
                }
                return to;
            }
            finally
            {
                reader.Dispose();
            }
        }

        /// <summary>
        /// Readers the each.
        /// </summary>
        /// <typeparam name="Return">The type of the return.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="fn">The function.</param>
        /// <param name="source">The source.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;Return&gt;.</returns>
        public override async Task<Return> ReaderEach<Return>(IDataReader reader, Action fn, Return source, CancellationToken token = default)
        {
            try
            {
                while (await ReadAsync(reader, token).ConfigureAwait(false))
                {
                    fn();
                }
                return source;
            }
            finally
            {
                reader.Dispose();
            }
        }

        /// <summary>
        /// Readers the read.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="fn">The function.</param>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        public override async Task<T> ReaderRead<T>(IDataReader reader, Func<T> fn, CancellationToken token = default)
        {
            try
            {
                if (await ReadAsync(reader, token).ConfigureAwait(false))
                    return fn();

                return default(T);
            }
            finally
            {
                reader.Dispose();
            }
        }
#endif

        /// <summary>
        /// Gets the drop function.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="functionName">Name of the function.</param>
        /// <returns>System.String.</returns>
        public override string GetDropFunction(string database, string functionName)
        {
            var sb = StringBuilderCache.Allocate();

            var tableName = $"{database}.{base.NamingStrategy.GetTableName(functionName)}";

            sb.Append("DROP FUNCTION IF EXISTS ");
            sb.AppendFormat("{0}", tableName);

            sb.Append(";");

            return StringBuilderCache.ReturnAndFree(sb);
        }

        /// <summary>
        /// Gets the create view.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="modelDef">The model definition.</param>
        /// <param name="selectSql">The select SQL.</param>
        /// <returns>System.String.</returns>
        public override string GetCreateView(string database, ModelDefinition modelDef, StringBuilder selectSql)
        {
            var sb = StringBuilderCache.Allocate();

            var tableName = $"{database}.{base.NamingStrategy.GetTableName(modelDef.ModelName)}";

            sb.AppendFormat("CREATE VIEW {0} as ", tableName);

            sb.Append(selectSql);

            sb.Append(";");

            return StringBuilderCache.ReturnAndFree(sb);
        }

        /// <summary>
        /// Gets the drop view.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="modelDef">The model definition.</param>
        /// <returns>System.String.</returns>
        public override string GetDropView(string database, ModelDefinition modelDef)
        {
            var sb = StringBuilderCache.Allocate();

            var tableName = $"{database}.{base.NamingStrategy.GetTableName(modelDef.ModelName)}";

            sb.Append("DROP VIEW IF EXISTS ");
            sb.AppendFormat("{0}", tableName);

            sb.Append(";");

            return StringBuilderCache.ReturnAndFree(sb);
        }

        /// <summary>
        /// Gets the UTC date function.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUtcDateFunction()
        {
            return "UTC_DATE()";
        }

        /// <summary>
        /// Dates the difference function.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="date1">The date1.</param>
        /// <param name="date2">The date2.</param>
        /// <returns>System.String.</returns>
        public override string DateDiffFunction(string interval, string date1, string date2)
        {
            return $"DATEDIFF({date1}, {date2})";
        }

        /// <summary>
        /// Gets the SQL ISNULL Function
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="alternateValue">The alternate Value.</param>
        /// <returns>The <see cref="string" />.</returns>
        public override string IsNullFunction(string expression, object alternateValue)
        {
            return $"IFNULL(({expression}), {alternateValue})";
        }

        /// <summary>
        /// Converts the flag.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>System.String.</returns>
        public override string ConvertFlag(string expression)
        {
            return $"cast(sign({expression}) as signed)";
        }

        /// <summary>
        /// Databases the fragmentation information.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>System.String.</returns>
        public override string DatabaseFragmentationInfo(string database)
        {
            var sb = new StringBuilder();

            sb.AppendLine("select ENGINE,");
            sb.AppendLine("concat(TABLE_SCHEMA, '.', TABLE_NAME) as table_name, ");
            sb.AppendLine("round(DATA_LENGTH/1024/1024, 2) as data_length,");
            sb.AppendLine("round(INDEX_LENGTH/1024/1024, 2) as index_length,");
            sb.AppendLine("round(DATA_FREE/1024/1024, 2) as data_free,");
            sb.AppendLine("(data_free/(index_length+data_length)) as frag_ratio");
            sb.AppendLine("FROM information_schema.tables");
            sb.AppendLine($"WHERE table_schema = '{database}'");
            sb.AppendLine("ORDER BY frag_ratio DESC;");

            return sb.ToString();
        }

        /// <summary>
        /// Databases the size.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>System.String.</returns>
        public override string DatabaseSize(string database)
        {
            return $"SELECT sum( data_length + index_length ) / 1024 / 1024 FROM information_schema.TABLES WHERE table_schema = '{database}'";
        }

        /// <summary>
        /// SQLs the version.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string SQLVersion()
        {
            return "select @@version";
        }

        /// <summary>
        /// SQLs the name of the server.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string SQLServerName()
        {
            return "MySQL";
        }

        /// <summary>
        /// Shrinks the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>System.String.</returns>
        public override string ShrinkDatabase(string database)
        {
            return ""; //$"optimize table {database}";
        }

        /// <summary>
        /// Res the index database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="objectQualifier">The object qualifier.</param>
        /// <returns>System.String.</returns>
        public override string ReIndexDatabase(string database, string objectQualifier)
        {
            return ""; //$"REINDEX DATABASE {database}";
        }

        /// <summary>
        /// Changes the recovery mode.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>System.String.</returns>
        public override string ChangeRecoveryMode(string database, string mode)
        {
            return "";
        }

        /// <summary>
        /// Just runs the SQL command according to specifications.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>Returns the Results</returns>
        public override string InnerRunSqlExecuteReader(IDbCommand command)
        {
            var sqlCommand = command as MySqlCommand;

            MySqlDataReader reader = null;
            var results = new StringBuilder();

            try
            {
                try
                {
                    reader = sqlCommand.ExecuteReader();

                    if (reader.HasRows)
                    {
                        var rowIndex = 1;
                        var columnNames = reader.GetSchemaTable().Rows.Cast<DataRow>()
                            .Select(r => r["ColumnName"].ToString()).ToList();

                        results.Append("RowNumber");

                        columnNames.ForEach(
                            n =>
                            {
                                results.Append(",");
                                results.Append(n);
                            });

                        results.AppendLine();

                        while (reader.Read())
                        {
                            results.AppendFormat(@"""{0}""", rowIndex++);

                            // dump all columns...
                            columnNames.ForEach(
                                col => results.AppendFormat(
                                    @",""{0}""",
                                    reader[col].ToString().Replace("\"", "\"\"")));

                            results.AppendLine();
                        }
                    }
                    else if (reader.RecordsAffected > 0)
                    {
                        results.AppendFormat("{0} Record(s) Affected", reader.RecordsAffected);
                        results.AppendLine();
                    }
                    else
                    {
                        results.AppendLine("No Results Returned.");
                    }

                    reader.Close();

                    command.Transaction?.Commit();
                }
                finally
                {
                    command.Transaction?.Rollback();
                }
            }
            catch (Exception x)
            {
                reader?.Close();

                results.AppendLine();
                results.AppendFormat("SQL ERROR: {0}", x);
            }

            return results.ToString();
        }
    }
}