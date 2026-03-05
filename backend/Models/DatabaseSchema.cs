using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models
{
    public class DatabaseSchema
    {
        public DateTime GeneratedAt { get; set; }
        public string DatabaseType { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<TableSchema> Tables { get; set; } = new List<TableSchema>();
    }

    public class TableSchema
    {
        public string Name { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();
        public List<string> PrimaryKeyColumns { get; set; } = new List<string>();
        public List<ForeignKeySchema> ForeignKeys { get; set; } = new List<ForeignKeySchema>();
    }

    public class ColumnSchema
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public string? DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
    }

    public class ForeignKeySchema
    {
        public string TableName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string ReferencedTableName { get; set; } = string.Empty;
        public string ReferencedColumnName { get; set; } = string.Empty;
        public string ConstraintName { get; set; } = string.Empty;
    }
}
