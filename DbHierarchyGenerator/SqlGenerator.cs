#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DbHierarchyGenerator.Models;
using DbHierarchyGenerator.Templates;

#endregion

namespace DbHierarchyGenerator
{
    public class SqlGenerator
    {
        public string GetPartitionCreation(Table table)
        {
            if (table.PartitionScheme == null)
                return string.Empty;

            var partitionGenerator = SqlPartitionGenerator.GetGenerator(table.PartitionScheme.Function);
            return partitionGenerator.GeneratePartitionCreation(table.PartitionScheme);
        }

        public string GetPartitionJobStepCreation(Table table)
        {
            if (table.PartitionScheme == null)
                return string.Empty;

            var partitionGenerator = SqlPartitionGenerator.GetGenerator(table.PartitionScheme.Function);
            return partitionGenerator.GenerateJobStepCreationg(table);
        }

        public IEnumerable<string> GetConstraintCreations(Table table)
        {
            return table.Constraints.Where(constr => constr.Type != ConstraintType.ForeignKey).Select(
                constr =>
                    String.Format(SqlTemplates.CreateNonFkConstraintTemplate, constr.SchemaName,
                        constr.TableName, constr.Name, constr.Type,
                        String.Join(", ", constr.Columns.Select(col => col.Name))));
                        //+
                        //(table.PartitionScheme != null && constr.Type == ConstraintType.PrimaryKey ?
                        //string.Format(" on [{0}]", table.PartitionScheme.FileGroups.First())
                        //: string.Empty));
        }

        public IEnumerable<string> GetIndexCreations(Table table)
        {
            var creations = new List<string>();

            foreach (Index index in table.Indexes)
            {
                string creation = string.Format(SqlTemplates.CreateIndexTemplate,
                    index.IsUniquie ? " unique" : string.Empty, index.Type, index.Name, index.SchemaName,
                    index.TableName,
                    string.Join(", ", index.Columns.Select(col => col.Name)));

                if (index.IncludeColumns.Any())
                    creation += string.Format(" include ({0})",
                        string.Join(", ", index.IncludeColumns.Select(col => col.Name)));
                if (table.PartitionScheme != null)
                {
                    if (index.Type == IndexType.Clustered)
                        creation += string.Format(" on [{0}]([{1}])", table.PartitionScheme.Name,
                            index.Columns.First().Name);
                    else
                        creation += string.Format(" on [{0}]", table.PartitionScheme.FileGroups.First());
                }

                creation += ";";

                creations.Add(creation);
            }

            return creations;
        }

        public List<string> GetFkCreations(Table table)
        {
            return
                table.Constraints.Where(constr => constr.Type == ConstraintType.ForeignKey).OrderBy(c => c.Name).Select(
                    fk =>
                        string.Format(SqlTemplates.CreateFkConstraintTemplate, fk.SchemaName,
                            fk.TableName, fk.Name, string.Join(", ", fk.Columns.Select(col => col.Name)),
                            fk.RefSchemaName, fk.RefTableName, string.Join(", ", fk.RefColumns.Select(col => col.Name))))
                    .ToList();
        }

        public IEnumerable<string> GetFkDroppings(Table table)
        {
            return table.Constraints.Where(constr => constr.Type == ConstraintType.ForeignKey)
                .OrderByDescending(c => c.Name)
                .Select(
                    fk =>
                        string.Format(SqlTemplates.DropFkConstraintTemplate, fk.SchemaName, fk.TableName, fk.Name));
        }


        public string GetDefinableAlterQuery(Definable definable)
        {
            string definition = FormatQuery(definable.Definition, 3);
            string alterQuery = CreateToAlter(definable, definition);

            return alterQuery;
        }

        public string GetHistoryTableTriggerAlter(Trigger trigger, Table table, Table historyTable)
        {
            List<Column> pkColumns =
                table.Constraints.Where(c => c.Type == ConstraintType.PrimaryKey).SelectMany(pk => pk.Columns).ToList();
            string pkColDefinitions = string.Join(", ", pkColumns.Select(col => col.GetDefinition()));
            string pkNames = string.Join(", ", pkColumns.Select(col => col.Name));
            string insertColumns = string.Join(", ", table.Columns.Select(col => string.Format("[{0}]", col.Name)));
            string checkColumns = string.Join("," + Environment.NewLine + Tabs(8),
                table.Columns.Select(
                    col =>
                        ColumnType.NotComparable.Contains(col.Type)
                            ? (col.Type == ColumnType.Image
                                ? string.Format("cast([{0}] as varbinary(max)) as [{0}]", col.Name)
                                : string.Format("cast([{0}] as nvarchar(max)) as [{0}]", col.Name))
                            : string.Format("[{0}]", col.Name)));
            string joinPredicate = string.Join(" and ",
                pkColumns.Select(pkCol => string.Format("{0}.[{1}] = {2}.[{1}]", "i", pkCol.Name, "ids")));
            string fromInsertedCols = string.Join("," + Environment.NewLine + Tabs(8),
                table.Columns.Select(col => string.Format("{0}.[{1}]", "i", col.Name)));
            string fromDeletedCols = string.Join("," + Environment.NewLine + Tabs(8),
                table.Columns.Select(col => string.Format("{0}.[{1}]", "d", col.Name)));
            string innerQuery = string.Format(SqlTemplates.AlterHistoryTriggerInnerTemplate, pkColDefinitions, pkNames,
                historyTable, insertColumns, joinPredicate, fromInsertedCols, fromDeletedCols, checkColumns);
            string query = string.Format(SqlTemplates.AlterHistoryTriggerTemplate, trigger.SchemaName, trigger.Name,
                trigger.ParentSchemaName, trigger.ParentName, innerQuery);

            return query;
        }

        private string CreateToAlter(Definable item, string query)
        {
            string type = item.GetType().Name.ToLower();

            var regex =
                new Regex(
                    String.Format(@"create (function|proc|procedure|view|trigger) (\[?{0}]?\.)?\[?{1}]?",
                        item.SchemaName, item.Name), RegexOptions.IgnoreCase);
            string result = regex.Replace(query,
                String.Format("alter {0} [{1}].[{2}]", type, item.SchemaName, item.Name));
            return result;
        }

        private string FormatQuery(string query, uint tabs)
        {
            var queryTabBuilder = new StringBuilder();
            using (var reader = new StringReader(query))
            {
                string line = reader.ReadLine();

                if (line == null)
                    return query;

                line = line.Replace("\t", Tabs(1));
                queryTabBuilder.Append(Tabs(tabs) + line);

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Replace("\t", Tabs(1));
                    queryTabBuilder.Append(Environment.NewLine + Tabs(tabs) + line);
                }
            }

            return queryTabBuilder.ToString();
        }

        public string Tabs(uint tabs)
        {
            var result = new StringBuilder();
            for (int i = 0; i < tabs; i++)
                result.Append("    ");

            return result.ToString();
        }
    }
}