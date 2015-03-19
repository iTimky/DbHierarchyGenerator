using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbHierarchyGenerator.Models
{
    public class Table : DbObject
    {
        public List<Table> ParentTables { get; set; }

        public uint Level { get; set; }

        public List<Column> Columns { get; set; }
        public List<Column> Fk { get; set; }
        public List<Constraint> Constraints { get; set; }
        public List<Index> Indexes { get; set; }
        public PartitionScheme PartitionScheme { get; set; } 

        public Table(string schemaName, string name) : base(schemaName, name) { }

        public override string ShortNames
        {
            get { return "'U'"; }
        }
    }
}
