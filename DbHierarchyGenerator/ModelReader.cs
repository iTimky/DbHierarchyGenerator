using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbHierarchyGenerator.Models;
using DbHierarchyGenerator.Models.IntermediateObjects;

namespace DbHierarchyGenerator
{
    public class ModelReader
    {
        public static T GetNullableRef<T>(object o, T def = default(T)) where T : class
        {
            return o == null || o is DBNull ? def : (T)o;
        }

        public static T? GetNullableVal<T>(object o, T? def = default(T?)) where T : struct
        {
            return o == null || o == DBNull.Value ? def : (T)o;
        }

        public void FillDbObject<T>(T dbObject, IDataRecord record) where T : DbObject
        {
            dbObject.SchemaName = (string) record["SchemaName"];
            dbObject.Name = (string) record["Name"];
        }

        public View ReadView(IDataRecord record)
        {
            return new View((string)record["SchemaName"], (string)record["Name"], (string)record["Definition"]);
        }

        public Trigger ReadTrigger(IDataRecord record)
        {
            return new Trigger((string)record["SchemaName"], (string)record["Name"], (string)record["Definition"], (string)record["ParentSchemaName"], (string)record["ParentName"]);
        }

        public FkDependency ReadFkDependency(IDataRecord record)
        {
            var item = new FkDependency
            {
                SchemaName = (string) record["SchemaName"],
                TableName = (string) record["TableName"],
                ColumnName = GetNullableRef<string>(record["ColumnName"]),
                ConstraintName = GetNullableRef<string>(record["ConstraintName"]),
                RefSchemaName = GetNullableRef<string>(record["RefSchemaName"]),
                RefTableName = GetNullableRef<string>(record["RefTableName"]),
                RefColumnName = GetNullableRef<string>(record["RefColumnName"])
            };

            return item;
        }

        public Column ReadColumn(IDataRecord record)
        {
            var item = new Column((string) record["SchemaName"], (string) record["TableName"], (string) record["ColumnName"]);
            item.IsIdentity = (bool) record["IsIdentity"];
            item.IsNullable = (string) record["IsNullable"] == "YES";
            item.DefaultValue = GetNullableRef<string>(record["DefaultValue"]);
            item.Type = ReadColumnType(record);

            if (!item.Type.HasPrecision)
                return item;

            var maximumLength = GetNullableVal<int>(record["MaximumLength"]);
            item.MaximumLength = maximumLength == null ? null : (maximumLength == -1 ? "max" : maximumLength.ToString());
            item.NumericPrecision = GetNullableVal<byte>(record["NumericPrecision"]);
            item.NumericScale = GetNullableVal<int>(record["NumericScale"]);
            item.DateTimePrecision = GetNullableVal<short>(record["DateTimePrecision"]);

            return item;
        }

        public ColumnType ReadColumnType(IDataRecord record)
        {
            var columnType = (ColumnType)(string)record["TypeName"];
            return columnType;
        }

        public IndexDependency ReadIndexDependency(IDataRecord record)
        {
            var indexDependency = new IndexDependency((string) record["SchemaName"], (string) record["TableName"],
                (string) record["IndexName"])
            {
                Type = (string) record["IndexType"],
                ColumnName = (string) record["ColumnName"],
                IsIncluded = (bool) record["IsIncluded"],
                IsUnique = (bool) record["IsUnique"]
            };

            return indexDependency;
        }

        public ConstraintDependency ReadConstraintDependency(IDataRecord record)
        {
            var constraintDependency = new ConstraintDependency((string)record["SchemaName"], (string)record["TableName"], (string)record["ConstraintName"])
            {
                Type = (string)record["ConstraintType"],
                ColumnName = (string)record["ColumnName"]
            };

            return constraintDependency;
        }

        public Procedure ReadProcedure(IDataRecord record)
        {
            return new Procedure((string)record["SchemaName"], (string)record["Name"], (string)record["Definition"]);
        }

        public Function ReadFunction(IDataRecord record)
        {
            return new Function((string)record["SchemaName"], (string)record["Name"], (string)record["Definition"]);
        }

        public TableTypeDependency ReadTableTypeDepencency(IDataRecord record)
        {
            var item = new TableTypeDependency
            {
                TypeSchemaName = (string)record["TypeSchemaName"],
                Name = (string)record["Name"],
                ObjSchemaName = GetNullableRef<string>(record["ObjSchemaName"]),
                ObjName = GetNullableRef<string>(record["ObjName"])
            };

            return item;
        }
    }
}
