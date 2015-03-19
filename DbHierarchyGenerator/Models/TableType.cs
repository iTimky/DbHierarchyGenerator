using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbHierarchyGenerator.Models
{
    public class TableType : DbObject
    {
        public List<Definable> Dependencies { get; set; }

        public TableType(string schemaName, string name) : base(schemaName, name)
        {
        }
    }
}
