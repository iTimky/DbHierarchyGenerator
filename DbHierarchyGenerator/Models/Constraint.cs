using System.Collections.Generic;

namespace DbHierarchyGenerator.Models
{
    public class Constraint : DbObject
    {
        public Constraint(string schemaName, string tableName, string name) : base(schemaName, name)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }
        public ConstraintType Type { get; set; }
        public List<Column> Columns { get; set; }

        public string RefSchemaName { get; set; }
        public string RefTableName { get; set; }
        public List<Column> RefColumns { get; set; }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", SchemaName, TableName, Name);
        }
    }

    public sealed class ConstraintType : DbTypeBase<ConstraintType>
    {
        public static readonly ConstraintType PrimaryKey = new ConstraintType("PRIMARY KEY");
        public static readonly ConstraintType PrimaryKeyNonClustered = new ConstraintType("PRIMARY KEY NONCLUSTERED");
        public static readonly ConstraintType ForeignKey = new ConstraintType("FOREIGN KEY");
        public static readonly ConstraintType Unique = new ConstraintType("UNIQUE");

        private ConstraintType(string name) : base(name) { }
    }
}