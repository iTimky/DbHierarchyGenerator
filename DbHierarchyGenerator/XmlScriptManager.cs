using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DbHierarchyGenerator.Models;
using DbHierarchyGenerator.Templates;

namespace DbHierarchyGenerator
{
    public class XmlScriptManager
    {
        private readonly SqlGenerator _sqlGenerator = new SqlGenerator();
        public string Author { get; private set; }

        public XmlScriptManager()
        {
            Author = ConfigurationManager.AppSettings["author"];
        }

        public string FirstIdAttr { get { return string.Format(XmlTemplates.AttributeTemplate, "id", "1"); } }
        public string SecondIdAttr { get { return string.Format(XmlTemplates.AttributeTemplate, "id", "2"); } }
        public string AuthorAttr { get { return string.Format(XmlTemplates.AttributeTemplate, "author", Author); } }
        public string RunOnChangeAttr { get { return string.Format(XmlTemplates.AttributeTemplate, "runOnChange", "true"); } }
        public string RunAlwaysAttr { get { return string.Format(XmlTemplates.AttributeTemplate, "runAlways", "true"); } }

        public static string EmptyChangeLog { get { return string.Format(XmlTemplates.ChangeLogTemplate, string.Empty); } }

        public string GetTableXml(Table table)
        {
            var columnCreations = string.Join("," + Environment.NewLine, table.Columns.Select(col =>  _sqlGenerator.Tabs(4) + col.GetDefinition(false)));
            var constraints = string.Join(Environment.NewLine, _sqlGenerator.GetConstraintCreations(table));
            var indexes = string.Join(Environment.NewLine, _sqlGenerator.GetIndexCreations(table));
            string constrIndexes = string.Empty;

            if (!string.IsNullOrEmpty(constraints))
                constrIndexes += Environment.NewLine + constraints;
            if (!string.IsNullOrEmpty(indexes))
                constrIndexes += Environment.NewLine + Environment.NewLine + indexes;

            var partitionQuery = _sqlGenerator.GetPartitionCreation(table);
            var createQuery = string.Format(SqlTemplates.CreateTableTemplate, table.SchemaName, table.Name, columnCreations, constrIndexes);
            var mainSql = string.Format(XmlTemplates.SqlTemplate, partitionQuery + createQuery);
            var mainDropQuery = string.Format(SqlTemplates.DropTableTemplate, table.SchemaName, table.Name);
            var mainRollback = string.Format(XmlTemplates.RollbackTemplate, mainDropQuery);
            var mainChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", FirstIdAttr, AuthorAttr), string.Join(Environment.NewLine, mainSql, mainRollback));

            var jobStepChangeSet = GetPartitionJobStepChangeSet(table);

            if (!string.IsNullOrEmpty(jobStepChangeSet))
                return string.Format(XmlTemplates.ChangeLogTemplate, string.Join(Environment.NewLine + Environment.NewLine, mainChangeSet, jobStepChangeSet));

            var fkCreations = _sqlGenerator.GetFkCreations(table);
            if (!fkCreations.Any())
                return string.Format(XmlTemplates.ChangeLogTemplate, mainChangeSet);

            var fkQuery = string.Join(Environment.NewLine, fkCreations);
            var fkSql = string.Format(XmlTemplates.SqlTemplate, fkQuery);
            var fkDropQuery = string.Join(Environment.NewLine, _sqlGenerator.GetFkDroppings(table));
            var fkRollback = string.Format(XmlTemplates.RollbackTemplate, fkDropQuery);
            
            var fkChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", SecondIdAttr, AuthorAttr), string.Join(Environment.NewLine, fkSql, fkRollback));
            var changeLog = string.Format(XmlTemplates.ChangeLogTemplate, string.Join(Environment.NewLine + Environment.NewLine, mainChangeSet, fkChangeSet));

            return changeLog;
        }

        private string GetPartitionJobStepChangeSet(Table table)
        {
            if (table.PartitionScheme == null)
                return string.Empty;

            var stepCreationQuery = _sqlGenerator.GetPartitionJobStepCreation(table);
            if (string.IsNullOrEmpty(stepCreationQuery))
                return string.Empty;

            var stepSql = string.Format(XmlTemplates.SqlTemplate, stepCreationQuery);
            var stepChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", SecondIdAttr, AuthorAttr), stepSql);
            return stepChangeSet;
        }

        public string GetTableTypeXml(TableType tableType)
        {
            var procDrops = tableType.Dependencies.Where(d => d is Procedure).Select(d => string.Format(SqlTemplates.DropProcedureTemplate, d.SchemaName, d.Name));
            var funcDrops = tableType.Dependencies.Where(d => d is Function).Select(d => string.Format(SqlTemplates.DropFunctionTemplate, d.SchemaName, d.Name));
            var drops = string.Join(Environment.NewLine, procDrops.Concat(funcDrops));

            var dropCreateQuery = string.Format(SqlTemplates.DropCreateTableTypeTemplate, tableType.SchemaName, tableType.Name, drops);
            var sql = string.Format(XmlTemplates.SqlTemplate, dropCreateQuery);
            var changeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", FirstIdAttr, AuthorAttr, RunOnChangeAttr), sql);
            var changeLog = string.Format(XmlTemplates.ChangeLogTemplate, changeSet);
            return changeLog;
        }

        public string GetFunctionXml(Definable function)
        {
            var changesetAttrs = new List<string>() { RunOnChangeAttr };
            return GetDefinableXml(function, SqlTemplates.CreateFunctionTemplate, changesetAttrs);
        }

        public string GetProcedureXml(Definable procedure)
        {
            var changesetAttrs = new List<string>() { RunOnChangeAttr };
            return GetDefinableXml(procedure, SqlTemplates.CreateProcedureTemplate, changesetAttrs);
        }

        public string GetViewXml(View view)
        {
            var changesetAttrs = new List<string>() { RunOnChangeAttr };
            return GetDefinableXml(view, SqlTemplates.CreateView, changesetAttrs);
        }

        public string GetTriggerXml(Trigger trigger)
        {
            var changesetAttrs = new List<string>() { RunOnChangeAttr };
            return GetDefinableXml(trigger, SqlTemplates.CreateTrigger, changesetAttrs, trigger.ParentSchemaName, trigger.ParentName);
        }

        public string GetDefinableXml(Definable definable, string queryTemplate, List<string> addAttrs, string add1 = null, string add2 = null)
        {
            var createQuery = string.Format(queryTemplate, definable.SchemaName, definable.Name, add1, add2);
            var createSql = string.Format(XmlTemplates.SqlTemplate, createQuery);
            var createAttrs = new List<string> { FirstIdAttr, AuthorAttr }.Union(addAttrs).ToList();
            var createChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", createAttrs), createSql);

            var alterQuery = _sqlGenerator.GetDefinableAlterQuery(definable);
            var alterSql = string.Format(XmlTemplates.SqlTemplate, alterQuery);
            var alterAttrs = new List<string> { SecondIdAttr, AuthorAttr }.Union(addAttrs).ToList();
            var alterChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", alterAttrs), alterSql);
            var changeLog = string.Format(XmlTemplates.ChangeLogTemplate, string.Join(Environment.NewLine + Environment.NewLine, createChangeSet, alterChangeSet));

            return changeLog;
        }

        public string GetTableHistoryTriggerXml(Table table, Table historyTable)
        {
            var changesetAttrs = new List<string>() { RunOnChangeAttr };
            var trigger = new Trigger(table.SchemaName, "TR_" + historyTable.Name, null, table.SchemaName, table.Name);

            var createQuery = string.Format(SqlTemplates.CreateTrigger, trigger.SchemaName, trigger.Name, trigger.ParentSchemaName, trigger.ParentName);
            var createSql = string.Format(XmlTemplates.SqlTemplate, createQuery);
            var createAttrs = new List<string> { FirstIdAttr, AuthorAttr }.Union(changesetAttrs).ToList();
            var createChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", createAttrs), createSql);

            var alterQuery = _sqlGenerator.GetHistoryTableTriggerAlter(trigger, table, historyTable);
            var alterSql = string.Format(XmlTemplates.SqlTemplate, alterQuery);
            var alterAttrs = new List<string> { SecondIdAttr, AuthorAttr }.Union(changesetAttrs).ToList();
            var alterChangeSet = string.Format(XmlTemplates.ChangeSetTemplate, string.Join(" ", alterAttrs), alterSql);
            var changeLog = string.Format(XmlTemplates.ChangeLogTemplate, string.Join(Environment.NewLine + Environment.NewLine, createChangeSet, alterChangeSet));

            return changeLog;
        }
    }
}
