using System;

namespace DbHierarchyGenerator.Models
{
    public class DbObject : IEquatable<DbObject>
    {
        public string SchemaName { get; set; }
        public string Name { get; set; }
        public virtual string ShortNames { get { throw new NotImplementedException();} }

        public DbObject(string schemaName, string name)
        {
            SchemaName = schemaName;
            Name = name;
        }

        #region IEquatable
        public override int GetHashCode()
        {
            unchecked
            {
                return ((SchemaName != null ? SchemaName.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        public bool Equals(DbObject other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SchemaName, other.SchemaName) && (Name == "*" || other.Name == "*" || string.Equals(Name, other.Name));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DbObject)obj);
        }

        public static bool operator ==(DbObject first, DbObject second)
        {
            return Equals(first, second);
        }

        public static bool operator !=(DbObject first, DbObject second)
        {
            return !Equals(first, second);
        }
        #endregion

        public override string ToString()
        {
            return SchemaName + "." + Name;
        }
    }
}
