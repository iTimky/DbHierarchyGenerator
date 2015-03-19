using System;
using System.Collections.Generic;

namespace DbHierarchyGenerator.Models
{
    public abstract class DbTypeBase<T> where T : DbTypeBase<T>
    {
        private static readonly Dictionary<string, DbTypeBase<T>> Existing = new Dictionary<string, DbTypeBase<T>>();
        public string Name { get; private set; }

        protected DbTypeBase(string name)
        {
            Name = name.ToLower();

            if (!Existing.ContainsKey(name))
                Existing[name] = this;
        }

        public override string ToString()
        {
            return Name;
        }

        public static explicit operator DbTypeBase<T>(string name)
        {
            DbTypeBase<T> columnType;

            if (!Existing.TryGetValue(name, out columnType))
                throw new InvalidCastException(string.Format("No such column type name: {0}", name));

            return columnType;
        }
    }
}
