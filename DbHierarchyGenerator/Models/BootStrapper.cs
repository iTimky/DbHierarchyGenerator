namespace DbHierarchyGenerator.Models
{
    public static class Bootstrapper
    {
        // Необходим вызов каждого наследника DbTypeBase<T> для инициализации
        static ColumnType _columnType = ColumnType.Bigint;
        static IndexType _indexType = IndexType.Clustered;
        static ConstraintType _constraintType = ConstraintType.PrimaryKey;

        public static void Initialize() { }
    }
}
