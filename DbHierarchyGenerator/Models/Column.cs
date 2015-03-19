using System;
using System.Collections.Generic;
using System.Text;

namespace DbHierarchyGenerator.Models
{
    public class Column : DbObject, ICloneable, IEquatable<Column>
    {
        public Column(string schemaName, string tableName, string name) : base(schemaName, name)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }
        public bool IsIdentity { get; set; }
        public string DefaultValue { get; set; }
        public bool IsNullable { get; set; }
        public ColumnType Type { get; set; }
        public string MaximumLength { get; set; }
        public byte? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public short? DateTimePrecision { get; set; }

        public List<string> GetPrecList()
        {
            var precs = new List<string>();

            if (MaximumLength != null)
                precs.Add(MaximumLength);

            if (NumericPrecision.HasValue)
                precs.Add(NumericPrecision.ToString());

            if (NumericScale.HasValue)
                precs.Add(NumericScale.ToString());

            if (DateTimePrecision.HasValue)
                precs.Add(DateTimePrecision.ToString());

            return precs;
        }

        public string GetDefinition(bool simple = true)
        {
            var builder = new StringBuilder(String.Format("[{0}] {1}", Name, Type));

            if (Type.HasPrecision)
                builder.Append(string.Format("({0})", string.Join(", ", GetPrecList())));

            if (!simple)
            {
                if (IsIdentity)
                    builder.Append(" identity");

                if (DefaultValue != null)
                    builder.Append(" default " + DefaultValue);
            }

            builder.Append(IsNullable ? " null" : " not null");

            return builder.ToString();
        }

        public override int GetHashCode()
        {
            return SchemaName.GetHashCode() ^ TableName.GetHashCode() ^ Name.GetHashCode();
        }

        public bool Equals(Column other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return SchemaName == other.SchemaName && TableName == other.TableName && Name == other.Name;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", SchemaName, TableName, Name);
        }

        public object Clone()
        {
            var clone = new Column(SchemaName, TableName, Name);
            clone.IsIdentity = IsIdentity;
            clone.DefaultValue = DefaultValue;
            clone.IsNullable = IsNullable;
            clone.Type = Type;
            clone.MaximumLength = MaximumLength;
            clone.NumericPrecision = NumericPrecision;
            clone.NumericScale = NumericScale;
            clone.DateTimePrecision = DateTimePrecision;

            return clone;
        }
    }

    public sealed class ColumnType : DbTypeBase<ColumnType>
    {
        public static readonly ColumnType Bit = new ColumnType("bit");
        public static readonly ColumnType Tinyint = new ColumnType("tinyint");
        public static readonly ColumnType Smallint = new ColumnType("smallint");
        public static readonly ColumnType Int = new ColumnType("int");
        public static readonly ColumnType Bigint = new ColumnType("bigint");
        public static readonly ColumnType Uniqueidentifier = new ColumnType("uniqueidentifier");

        public static readonly ColumnType Char = new ColumnType("char", true);
        public static readonly ColumnType Nchar = new ColumnType("nchar", true);
        public static readonly ColumnType Varchar = new ColumnType("varchar", true);
        public static readonly ColumnType Nvarchar = new ColumnType("nvarchar", true);
        public static readonly ColumnType Text = new ColumnType("text");
        public static readonly ColumnType Ntext = new ColumnType("ntext");
        public static readonly ColumnType Xml = new ColumnType("xml");

        public static readonly ColumnType Date = new ColumnType("date");
        public static readonly ColumnType Datetime = new ColumnType("datetime");
        public static readonly ColumnType Datetime2 = new ColumnType("datetime2", true);
        public static readonly ColumnType Time = new ColumnType("time", true);
        public static readonly ColumnType Timestamp = new ColumnType("timestamp");
        public static readonly ColumnType Smalldatetime = new ColumnType("smalldatetime");
        public static readonly ColumnType Datetimeoffset = new ColumnType("datetimeoffset", true);

        public static readonly ColumnType Real = new ColumnType("real", true);
        public static readonly ColumnType Numeric = new ColumnType("numeric", true);
        public static readonly ColumnType Float = new ColumnType("float");
        public static readonly ColumnType DoublePrecision = new ColumnType("double precision");
        public static readonly ColumnType Decimal = new ColumnType("decimal", true);
        public static readonly ColumnType Money = new ColumnType("money");

        public static readonly ColumnType Binary = new ColumnType("binary");
        public static readonly ColumnType Varbinary = new ColumnType("varbinary", true);
        public static readonly ColumnType Image = new ColumnType("image", true);


        public static readonly IReadOnlyList<ColumnType> NotComparable = new List<ColumnType>() {Xml, Text, Ntext, Image};

        public bool HasPrecision { get; private set; }

        private ColumnType(string name, bool hasPrecision = false) : base(name)
        {
            HasPrecision = hasPrecision;
        }
    }

    public static class HistoryColumn
    {
        public const int HistoryColumnCount = 5;

        public static readonly Column Id = new Column(null, null, "Id_History") { IsIdentity = true, Type = ColumnType.Int };

        public static readonly Column Action = new Column(null, null, "Action_History") { Type = ColumnType.Nchar, MaximumLength = "1" };

        public static readonly Column UserId = new Column(null, null, "UserId_History") { Type = ColumnType.Int };

        public static readonly Column UserIp = new Column(null, null, "UserIp_History")
        {
            IsNullable = true,
            Type = ColumnType.Nvarchar,
            MaximumLength = "32"
        };

        public static readonly Column UpdatedDate = new Column(null, null, "UpdatedDate_History")
        {
            IsNullable = true,
            Type = ColumnType.Datetime,
            DefaultValue = "getdate()"
        };
    }
}
