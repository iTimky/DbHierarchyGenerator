using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using DbHierarchyGenerator.Models;
using DbHierarchyGenerator.Models.IntermediateObjects;

namespace DbHierarchyGenerator.Managers
{
    public class TableBuilder
    {
        private readonly DbProvider _dbProvider = new DbProvider();
        private readonly ModelReader _modelReader = new ModelReader();

        public Dictionary<Table, Table> BuildHistoryTables(IEnumerable<Table> tables, HistoryConfig historyConfig)
        {
            var historyTables = historyConfig.HistoryTableList;
            var historyTableByTables = new Dictionary<Table, Table>();

            foreach (var table in tables)
            {
                var historyTable = new Table(table.SchemaName, table.Name + "_History")
                {
                    Columns =
                        new List<Column>(table.Columns.Count + HistoryColumn.HistoryColumnCount) {HistoryColumn.Id}
                };

                var configHistoryTable = historyTables.First(ht => ht.Equals(table));
                if (configHistoryTable.DatePartitioning != null)
                {
                    var partitionFuntion = new DatePartitionFunction(string.Format("PF_{0}_UpdatedDate_History", historyTable.Name), ColumnType.Datetime, configHistoryTable.DatePartitioning.Interval, configHistoryTable.DatePartitioning.Unit);
                    historyTable.PartitionScheme = new PartitionScheme(string.Format("PS_{0}", historyTable.Name), partitionFuntion, historyConfig.PartitionJobName);
                }

                var originPkColumns = table.Constraints.Where(c => c.Type == ConstraintType.PrimaryKey).SelectMany(c => c.Columns).ToList();

                if (originPkColumns.Any())
                {
                    var newCols = originPkColumns.Select(opc => (Column) opc.Clone());

                    foreach (var newCol in newCols)
                    {
                        newCol.IsIdentity = false;
                        historyTable.Columns.Add(newCol);
                    }

                    historyTable.Columns.AddRange(table.Columns.Except(originPkColumns));
                    CreateHistoryTableIndexes(historyTable, originPkColumns,
                        configHistoryTable.DatePartitioning == null ? IndexType.Clustered : IndexType.Nonclustered);

                    if (configHistoryTable.ParentPartitioning != null)
                    {
                        var partitionFuntion = new IntPartitionFunction(string.Format("PF_{0}_{1}", historyTable.Name, originPkColumns.First().Name), ColumnType.Int, configHistoryTable.ParentPartitioning.Interval);
                        historyTable.PartitionScheme = new PartitionScheme(string.Format("PS_{0}", historyTable.Name), partitionFuntion, historyConfig.PartitionJobName);
                    }
                }
                else
                {
                    if (configHistoryTable.ParentPartitioning != null)
                        throw new Exception(string.Format("ParentPartitioning specified for table {0}, but it has no PK", table.Name));
                    historyTable.Columns.AddRange(table.Columns);
                }

                historyTable.Columns.AddRange(new[] {HistoryColumn.Action, HistoryColumn.UserId, HistoryColumn.UserIp, HistoryColumn.UpdatedDate});
                CreateHistoryTableIndexes(historyTable, new List<Column>() { HistoryColumn.UpdatedDate }, historyTable.Indexes.Any(ix => ix.Type == IndexType.Clustered) ? IndexType.Nonclustered : IndexType.Clustered);
                CreateHistoryTableIndexes(historyTable, new List<Column>() { HistoryColumn.UserId });

                historyTableByTables.Add(table, historyTable);
            }

            CreateHistoryTableConstraints(historyTableByTables.Values);

            return historyTableByTables;
        }

        private void CreateHistoryTableConstraints(IEnumerable<Table> historyTables)
        {
            foreach (var historyTable in historyTables)
            {
                if (historyTable.Constraints == null)
                    historyTable.Constraints = new List<Constraint>();

                var constraint = new Constraint(historyTable.SchemaName, historyTable.Name, "PK_" + historyTable.Name);
                constraint.Type = historyTable.Indexes.Any(i => i.Type == IndexType.Clustered) ? ConstraintType.PrimaryKeyNonClustered : ConstraintType.PrimaryKey;
                constraint.Columns = historyTable.Columns.Where(col => col.IsIdentity).ToList();
                historyTable.Constraints.Add(constraint);
            }
        }

        private void CreateHistoryTableIndexes(Table table, List<Column> columns, IndexType indexType = null)
        {
            if (table.Indexes == null)
                table.Indexes = new List<Index>();

            if (!columns.Any())
                return;

            var index = new Index(table.SchemaName, table.Name, string.Format("IX_{0}_{1}", table.Name, string.Join("_", columns.Select(col => col.Name))));
            index.Type = indexType ?? IndexType.Nonclustered;
            index.Columns = columns;
            index.IncludeColumns = new List<Column>();
            table.Indexes.Add(index);
        }

        public IEnumerable<Table> BuildTables()
        {
            var columns = _dbProvider.Exec(Queries.GetAllColumns, _modelReader.ReadColumn);
            var tables = CreateTables(columns);

            var fkDependecies = _dbProvider.Exec(Queries.GetAllFk, _modelReader.ReadFkDependency);
            FillTableDependencies(tables, fkDependecies);
            tables.ForEach(FillLevelsRecursive);

            var indexDependencies = _dbProvider.Exec(Queries.GetAllIndexes, _modelReader.ReadIndexDependency);
            var indexes = BuildIndexes(indexDependencies, columns);
            FillIndexes(tables, indexes);

            var constraintDependencies = _dbProvider.Exec(Queries.GetAllConstraints, _modelReader.ReadConstraintDependency);
            var constraints = BuildConstraints(constraintDependencies, fkDependecies, columns);
            FillConstraints(tables, constraints);

            return tables;
        }

        private void FillTableDependencies(List<Table> tables, IEnumerable<FkDependency> dependencies)
        {
            foreach (var table in tables)
                table.ParentTables = tables.Where(t => dependencies
                    .Where(d => d.SchemaName == table.SchemaName && d.TableName == table.Name)
                    .Any(d => d.RefSchemaName == t.SchemaName && d.RefTableName == t.Name)).ToList();
        }

        private List<Table> CreateTables(IEnumerable<Column> columns)
        {
            var columnsByTableNames = columns.ToLookup(col => new {col.SchemaName, col.TableName});
            var tables = columnsByTableNames.Select(col => new Table(col.Key.SchemaName, col.Key.TableName) { Columns = col.ToList() }).ToList();

            return tables;
        }

        private void FillLevelsRecursive(Table table)
        {
            var exceptTables = table.ParentTables.Where(parent => parent.ParentTables.Contains(table)).ToList();
            exceptTables.Add(table);
            var parents = table.ParentTables.Except(exceptTables).ToList();

            if (table.Level > parents.Select(t => t.Level).DefaultIfEmpty().Max())
                return;

            foreach (var parent in parents)
                FillLevelsRecursive(parent);

            table.Level = parents.Select(t => t.Level).DefaultIfEmpty().Max() + 1;
        }

        private List<Index> BuildIndexes(IEnumerable<IndexDependency> indexDependencies, IEnumerable<Column> columns)
        {
            var indexGrouping = indexDependencies.ToLookup(id => new {id.SchemaName, id.TableName, id.Name, id.Type, id.IsUnique});
            var indexes = new List<Index>();

            foreach (var ig in indexGrouping)
            {
                var index = new Index(ig.Key.SchemaName, ig.Key.TableName, ig.Key.Name);
                index.Type = (IndexType)ig.Key.Type;
                index.IsUniquie = ig.Key.IsUnique;
                index.Columns =
                    ig.Where(id => !id.IsIncluded)
                        .Select(
                            id =>
                                columns.FirstOrDefault(
                                    col =>
                                        col.SchemaName == id.SchemaName && col.TableName == id.TableName &&
                                        col.Name == id.ColumnName))
                        .ToList();

                index.IncludeColumns =
                    ig.Where(id => id.IsIncluded)
                        .Select(
                            id =>
                                columns.FirstOrDefault(
                                    col =>
                                        col.SchemaName == id.SchemaName && col.TableName == id.TableName &&
                                        col.Name == id.ColumnName))
                        .ToList();

                indexes.Add(index);
            }

            return indexes;
        }

        private void FillIndexes(IEnumerable<Table> tables, List<Index> indexes)
        {
            foreach (var table in tables)
                table.Indexes = indexes.Where(ind => ind.SchemaName == table.SchemaName && ind.TableName == table.Name).ToList();
        }

        private List<Constraint> BuildConstraints(IEnumerable<ConstraintDependency> constraintDependencies,
            IEnumerable<FkDependency> fkDependencies, IEnumerable<Column> columns)
        {
            var constraintGrouping = constraintDependencies.ToLookup(cd => new {cd.SchemaName, cd.TableName, cd.Name, cd.Type});
            var fkGrouping = fkDependencies.Where(fk => fk.RefSchemaName != null).ToLookup(fk => new {fk.SchemaName, fk.TableName, fk.ConstraintName, fk.RefSchemaName, fk.RefTableName});
            var constraints = new List<Constraint>();

            foreach (var cg in constraintGrouping)
            {
                var constraint = new Constraint(cg.Key.SchemaName, cg.Key.TableName, cg.Key.Name);
                constraint.Type = (ConstraintType) cg.Key.Type;
                constraint.Columns =
                    cg.Select(
                        cd =>
                            columns.FirstOrDefault(
                                col =>
                                    col.SchemaName == cd.SchemaName && col.TableName == cd.TableName &&
                                    col.Name == cd.ColumnName)).ToList();

                constraints.Add(constraint);
            }

            foreach (var foreignKeyProps in fkGrouping)
            {
                var constraint = new Constraint(foreignKeyProps.Key.SchemaName, foreignKeyProps.Key.TableName, foreignKeyProps.Key.ConstraintName);
                constraint.Type = ConstraintType.ForeignKey;
                constraint.Columns =
                    foreignKeyProps.Select(
                        fk =>
                            columns.FirstOrDefault(
                                col =>
                                    col.SchemaName == fk.SchemaName && col.TableName == fk.TableName &&
                                    col.Name == fk.ColumnName)).ToList();

                constraint.RefSchemaName = foreignKeyProps.Key.RefSchemaName;
                constraint.RefTableName = foreignKeyProps.Key.RefTableName;
                constraint.RefColumns =
                    foreignKeyProps.Select(
                        fk =>
                            columns.FirstOrDefault(
                                col =>
                                    col.SchemaName == fk.RefSchemaName && col.TableName == fk.RefTableName &&
                                    col.Name == fk.RefColumnName)).ToList();

                constraints.Add(constraint);
            }

            return constraints;
        }

        private void FillConstraints(IEnumerable<Table> tables, List<Constraint> constraints)
        {
            foreach (var table in tables)
            {
                table.Constraints =
                    constraints.Where(c => c.SchemaName == table.SchemaName && c.TableName == table.Name).ToList();
            }
        }
    }
}
