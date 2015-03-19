using System.Collections.Generic;
using System.Linq;
using DbHierarchyGenerator.Models;
using DbHierarchyGenerator.Models.IntermediateObjects;

namespace DbHierarchyGenerator.Managers
{
    public class TableTypeManager
    {
        public List<TableType> CreateTableTypes(List<TableTypeDependency> dependencies, List<Definable> definables)
        {
            var tableTypes = dependencies.Select(d => new { d.TypeSchemaName, d.Name }).Distinct().Select(anon => new TableType(anon.TypeSchemaName, anon.Name)).ToList();

            foreach (var tableType in tableTypes)
            {
                tableType.Dependencies = (from dep in dependencies
                                          from def in definables
                                          where def.SchemaName == dep.ObjSchemaName
                                          && def.Name == dep.ObjName
                                          && dep.TypeSchemaName == tableType.SchemaName
                                          && dep.Name == tableType.Name
                                          select def).ToList();
            }

            return tableTypes;
        }
    }
}
