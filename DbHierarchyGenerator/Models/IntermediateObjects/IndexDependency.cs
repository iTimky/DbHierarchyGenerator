namespace DbHierarchyGenerator.Models.IntermediateObjects
{
    public class IndexDependency : DbObject
    {
        public IndexDependency(string schemaName, string tableName, string name) : base(schemaName, name)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }
        public string Type { get; set; }
        public string ColumnName { get; set; }
        public bool IsIncluded { get; set; }
        public bool IsUnique { get; set; }
    }

}
