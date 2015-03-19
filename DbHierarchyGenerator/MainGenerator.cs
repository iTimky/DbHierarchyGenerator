using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using DbHierarchyGenerator.Managers;
using DbHierarchyGenerator.Models;
using DbHierarchyGenerator.Templates;

namespace DbHierarchyGenerator
{
    public class MainGenerator
    {
        private readonly IoProvider _ioProvider = new IoProvider(ConfigurationManager.AppSettings["rootDirectiory"]);
        private readonly DbProvider _dbProvider = new DbProvider();
        private readonly ModelReader _modelReader = new ModelReader();
        private readonly XmlScriptManager _xmlScriptManager = new XmlScriptManager();
        private readonly List<DbObject> _ignoreObjects = new List<DbObject>();

        public MainGenerator()
        {
            var ignoreCsv = ConfigurationManager.AppSettings["ignoreObjects"];
            foreach (var ignoreObj in ignoreCsv.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                _ignoreObjects.Add(new DbObject(ignoreObj.Substring(0, ignoreObj.IndexOf(".", StringComparison.CurrentCulture)),
                                        ignoreObj.Substring(ignoreObj.IndexOf(".", StringComparison.CurrentCulture) + 1)));
            }
        }

        public void Generate(bool buildHierarchy)
        {
            var tables = GenerateTables();

            var functions = GenerateFunctions();
            var storedProcedures = GenerateStoredProcedures();
            var definables = functions.Concat(storedProcedures).ToList();
            GenerateTableTypes(definables);

            var views = GenerateViews();
            GenerateTriggers();

            GenerateHistory(tables);

            if (!_ioProvider.CreatedItemPaths.Any())
            {
                Console.WriteLine("All scripts already exist");
                Console.WriteLine();
            }

            if (buildHierarchy)
            {
                var changeLogGenerator = new ChangeLogHierarchyGenerator(_ioProvider);
                changeLogGenerator.GenerateTableChangelogHierarchy(tables);
                changeLogGenerator.GenerateViewChangelogHierarchy(views);
            }
        }


        private void GenerateHistory(IEnumerable<Table> tables)
        {
            var historyConfig = GetHistoryConfig();

            if (historyConfig == null || historyConfig.HistoryTableList == null || !historyConfig.HistoryTableList.Any())
                return;

            var tablesToBuild = tables.Where(t => historyConfig.HistoryTableList.Any(ht => ht.Equals(t))).ToList();
            var historyTableByTables = new TableBuilder().BuildHistoryTables(tablesToBuild, historyConfig);

            GenerateHistoryTables(historyTableByTables.Values.ToList());
            GenerateHistoryTriggers(historyTableByTables);
        }


        private List<Table> GenerateTables()
        {
            _ioProvider.CreateFolder(".", "Tables");
            var tables = new TableBuilder().BuildTables().Except(_ignoreObjects).Cast<Table>().ToList();
            tables.ForEach(table => _ioProvider.CreateFolder("Tables", table.ToString(), _xmlScriptManager.GetTableXml(table)));

            return tables;
        }

        private void GenerateTableTypes(List<Definable> definables)
        {
            _ioProvider.CreateFolder(".", "User-Defined Table Types");

            var tableTypeDependencies = _dbProvider.Exec(Queries.GetAllTypeDependencies, _modelReader.ReadTableTypeDepencency);
            var tableTypes = new TableTypeManager().CreateTableTypes(tableTypeDependencies, definables).Except(_ignoreObjects).Cast<TableType>().ToList();

            tableTypes.ForEach(tableType => _ioProvider.CreateXmlFileLocalPath(_xmlScriptManager.GetTableTypeXml(tableType), "User-Defined Table Types", tableType.ToString()));
        }

        private List<View> GenerateViews()
        {
            _ioProvider.CreateFolder(".", "Views");

            var views = _dbProvider.Exec(Queries.GetAllViews, _modelReader.ReadView).Except(_ignoreObjects).Cast<View>().ToList();
            views.ForEach(view => _ioProvider.CreateXmlFileLocalPath(_xmlScriptManager.GetViewXml(view), "Views", view.ToString()));

            return views;
        }

        private IEnumerable<Definable> GenerateFunctions()
        {
            _ioProvider.CreateFolder(".", "Functions");

            var functions = _dbProvider.Exec(Queries.GetAllFunctions, _modelReader.ReadFunction).Except(_ignoreObjects).Cast<Function>().ToList();
            functions.ForEach(function => _ioProvider.CreateXmlFileLocalPath(_xmlScriptManager.GetFunctionXml(function), "Functions", function.ToString()));

            return functions;
        }

        private IEnumerable<Definable> GenerateStoredProcedures()
        {
            _ioProvider.CreateFolder(".", "Stored Procedures");

            var storedProcedures = _dbProvider.Exec(Queries.GetAllStoredProcedures, _modelReader.ReadProcedure).Except(_ignoreObjects).Cast<Procedure>().ToList();
            storedProcedures.ForEach(procedure => _ioProvider.CreateXmlFileLocalPath(_xmlScriptManager.GetProcedureXml(procedure), "Stored Procedures", procedure.ToString()));

            return storedProcedures;
        }

        private void GenerateTriggers()
        {
            _ioProvider.CreateFolder(".", "Triggers");

            var triggers = _dbProvider.Exec(Queries.GetAllTriggers, _modelReader.ReadTrigger).Except(_ignoreObjects).Cast<Trigger>().ToList();
            triggers.ForEach(trigger => _ioProvider.CreateXmlFileLocalPath(_xmlScriptManager.GetTriggerXml(trigger), "Triggers", trigger.ToString()));
        }

        private HistoryConfig GetHistoryConfig()
        {
            if (!File.Exists(_ioProvider.RootDirInfo.FullName + "\\db.config-history_tables.xml"))
                _ioProvider.CreateXmlFileLocalPath(XmlTemplates.HistoryConfigTemplate, string.Empty,
                    "db.config-history_tables");

            var xmlSerializer = new XmlSerializer(typeof(HistoryConfig));

            HistoryConfig historyConfig;
            using (var reader = new StreamReader(_ioProvider.RootDirInfo.FullName + "\\db.config-history_tables.xml"))
            {
                historyConfig = (HistoryConfig)xmlSerializer.Deserialize(reader);
            }

            if (historyConfig.HistoryTableList != null)
                foreach (var historyTable in historyConfig.HistoryTableList)
                    if (string.IsNullOrEmpty(historyTable.Schema))
                        historyTable.Schema = string.IsNullOrEmpty(historyConfig.SchemaName)
                            ? "dbo"
                            : historyConfig.SchemaName;

            return historyConfig;
        }

        private void GenerateHistoryTables(List<Table> tables)
        {
            _ioProvider.CreateFolder("Tables", "History");
            tables.ForEach(table => _ioProvider.CreateXmlFileLocalPath(_xmlScriptManager.GetTableXml(table), "Tables\\History", table.ToString()));
        }

        private void GenerateHistoryTriggers(Dictionary<Table, Table> historyTablesByTables)
        {
            _ioProvider.CreateFolder("Triggers", "History");

            foreach (var historyTableByTable in historyTablesByTables)
            {
                var xml = _xmlScriptManager.GetTableHistoryTriggerXml(historyTableByTable.Key, historyTableByTable.Value);
                var fileName = string.Format("{0}.TR_{1}", historyTableByTable.Value.SchemaName, historyTableByTable.Value.Name);
                _ioProvider.CreateXmlFileLocalPath(xml, "Triggers\\History", fileName);
            }
        }
    }
}
