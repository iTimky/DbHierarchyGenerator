namespace DbHierarchyGenerator.Models.IntermediateObjects
{
    public class FkDependency
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string ConstraintName { get; set; }
        public string RefSchemaName { get; set; }
        public string RefTableName { get; set; }
        public string RefColumnName { get; set; }
    }
}
