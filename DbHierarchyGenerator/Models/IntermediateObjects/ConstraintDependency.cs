namespace DbHierarchyGenerator.Models.IntermediateObjects
{
    public class ConstraintDependency : DbObject
    {
        public ConstraintDependency(string schemaName, string tableName, string name) : base(schemaName, name)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }
        public string Type { get; set; }
        public string ColumnName { get; set; }
    }
}
