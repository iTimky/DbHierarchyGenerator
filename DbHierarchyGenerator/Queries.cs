namespace DbHierarchyGenerator
{
    public class Queries
    {
        public const string GetAllColumns = @"select
  t.TABLE_SCHEMA as SchemaName
, t.TABLE_NAME as TableName
, c.COLUMN_NAME as ColumnName
, cast (columnproperty(object_id(t.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as bit) as IsIdentity
, c.COLUMN_DEFAULT as DefaultValue
, c.IS_NULLABLE as IsNullable
, c.DATA_TYPE as TypeName
, c.CHARACTER_MAXIMUM_LENGTH as MaximumLength
, c.NUMERIC_PRECISION as NumericPrecision
, c.NUMERIC_SCALE as NumericScale
, c.DATETIME_PRECISION as DateTimePrecision
from information_schema.tables t
left join information_schema.columns c on c.TABLE_SCHEMA = t.TABLE_SCHEMA and c.TABLE_NAME = t.TABLE_NAME
where t.TABLE_TYPE = 'BASE TABLE'
and t.TABLE_NAME not like '%_History';";

        public const string GetAllConstraints = @"select
  kcu.TABLE_SCHEMA as SchemaName
, kcu.TABLE_NAME as TableName
, kcu.CONSTRAINT_NAME as ConstraintName
, tc.CONSTRAINT_TYPE as ConstraintType
, kcu.COLUMN_NAME as ColumnName
from information_schema.key_column_usage kcu
join information_schema.table_constraints tc on tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA and tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
where tc.CONSTRAINT_TYPE <>'FOREIGN KEY';";

        public const string GetAllIndexes = @"select 
  schema_name(t.schema_id) as SchemaName
, t.name as TableName
, ind.name as IndexName
, ind.type_desc as IndexType
, col.name as ColumnName
, ic.is_included_column as IsIncluded
, ind.is_unique as IsUnique
from sys.indexes ind 
join sys.index_columns ic on ind.object_id = ic.object_id and ind.index_id = ic.index_id 
join sys.columns col on ic.object_id = col.object_id and ic.column_id = col.column_id 
join sys.tables t on ind.object_id = t.object_id 
where ind.is_primary_key = 0
and ind.is_unique_constraint = 0
and t.is_ms_shipped = 0;";

        public const string GetAllFk = @"select
  T.SchemaName
, T.TableName
, T.ColumnName
, T.ConstraintName
, T.RefSchemaName
, T.RefTableName
, T.RefColumnName
from
    (select
        t.TABLE_SCHEMA as SchemaName
    , t.TABLE_NAME as TableName
    , kf.COLUMN_NAME as ColumnName
    , kf.CONSTRAINT_NAME as ConstraintName
    , kp.TABLE_SCHEMA as RefSchemaName
    , kp.TABLE_NAME as RefTableName
    , kp.COLUMN_NAME as RefColumnName
    from information_schema.tables t
    left join information_schema.key_column_usage kf on kf.TABLE_SCHEMA = t.TABLE_SCHEMA and kf.TABLE_NAME = t.TABLE_NAME
    left join information_schema.referential_constraints rc on rc.CONSTRAINT_NAME = kf.CONSTRAINT_NAME
    left join information_schema.key_column_usage kp on kp.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
    where TABLE_TYPE = 'BASE TABLE') T
where T.RefTableName is not null;";

        public const string GetAllTypeDependencies = @"select
  schema_name(t.schema_id) as TypeSchemaName
, t.name as Name
,  SPECIFIC_SCHEMA as ObjSchemaName
, SPECIFIC_NAME as ObjName
from sys.types t
left join information_schema.parameters p on schema_id(p.USER_DEFINED_TYPE_SCHEMA) = t.schema_id
                                        and p.USER_DEFINED_TYPE_NAME = t.name
where t.is_user_defined = 1";

        public const string GetAllFunctions = @"select 
  schema_name(o.schema_id) as SchemaName
, o.name as Name
, sm.definition as [Definition]
from sys.objects o
join sys.sql_modules sm on sm.object_id = o.object_id
where o.type_desc LIKE '%FUNCTION%';";

        public const string GetAllStoredProcedures = @"select
  schema_name(p.schema_id) as SchemaName
, p.name as Name
, m.definition as Definition
from sys.procedures p
join sys.sql_modules m on m.object_id = p.object_id
where schema_name(p.schema_id) <> 'cashdeskuser';";

        public const string GetAllViews = @"select
  schema_name(v.schema_id) as SchemaName
, v.name as Name
, m.definition as Definition
from sys.views v
join sys.sql_modules m on m.object_id = v.object_id;";

        public const string GetAllTriggers = @"select
  schema_name(obj.schema_id) as SchemaName
, obj.name as Name
, m.definition as Definition
, schema_name(parent.schema_id) as ParentSchemaName
, parent.name as ParentName
from sys.objects obj
join sys.triggers tr on tr.object_id = obj.object_id
join sys.objects parent on parent.object_id = tr.parent_id
join sys.sql_modules m on m.object_id = obj.object_id
where obj.type = 'TR'
and obj.name not like '%_History';";
    }
}
