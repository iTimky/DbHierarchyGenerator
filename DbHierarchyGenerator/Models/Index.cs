using System.Collections.Generic;

namespace DbHierarchyGenerator.Models
{
    public class Index : DbObject
    {
        public Index(string schemaName, string tableName, string name) : base(schemaName, name)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }
        public IndexType Type { get; set; }
        public bool IsUniquie { get; set; }
        public List<Column> Columns { get; set; }
        public List<Column> IncludeColumns { get; set; }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", SchemaName, TableName, Name);
        }
    }

    public sealed class IndexType : DbTypeBase<IndexType>
    {
        public static readonly IndexType Nonclustered = new IndexType("NONCLUSTERED");
        public static readonly IndexType Clustered = new IndexType("CLUSTERED");

        private IndexType(string name) : base(name) { }
    }
}