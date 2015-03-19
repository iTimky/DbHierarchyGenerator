using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using DbHierarchyGenerator.Managers;
using DbHierarchyGenerator.Models;

namespace DbHierarchyGenerator
{
    public class ChangeLogHierarchyGenerator
    {
        private readonly IoProvider _ioProvider;

        public ChangeLogHierarchyGenerator(IoProvider ioProvider)
        {
            _ioProvider = ioProvider;
        }

        public void GenerateTableChangelogHierarchy(List<Table> tables)
        {
            var tablesByLevel = tables.ToLookup(table => table.Level).OrderBy(t => t.Key).ToList();
            GenerateChangeLog(tablesByLevel, "db.changelog-tables", true, "Tables");
        }

        public void GenerateViewChangelogHierarchy(List<View> views)
        {
            var viewManager = new ViewManager();
            viewManager.FillDependecies(views);

            var viewsByLevel = views.ToLookup(table => table.Level).OrderBy(t => t.Key).ToList();
            GenerateChangeLog(viewsByLevel, "db.changelog-views", false, "Views");
        }

        private void GenerateChangeLog(IEnumerable<IGrouping<uint, DbObject>> objectsByLevel, string changeLogFileName, bool includeAll, string scriptFolder)
        {
            var fileFullPath = string.Format("{0}//{1}.xml", _ioProvider.RootDirInfo.FullName, changeLogFileName);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(XmlScriptManager.EmptyChangeLog);

            string attrValueTemplate;
            string includeEl;
            string includeAttr;

            if (includeAll)
            {
                includeEl = "includeAll";
                includeAttr = "path";
                attrValueTemplate = "{0}/{1}/{2}";
            }
            else
            {
                includeEl = "include";
                includeAttr = "file";
                attrValueTemplate = "{0}/{1}/{2}.xml";
            }

            Console.WriteLine("Generating {0}...", _ioProvider.GetLocalPath(fileFullPath));

            foreach (var objectGroup in objectsByLevel)
            {
                var lvlComment = xmlDocument.CreateComment(string.Format("Level {0}", objectGroup.Key));
                xmlDocument.DocumentElement.AppendChild(lvlComment);

                foreach (var dbObject in objectGroup.OrderBy(t => t.Name))
                {
                    var includeAllNode = xmlDocument.CreateElement(includeEl, xmlDocument.DocumentElement.NamespaceURI);
                    var pathAttribute = xmlDocument.CreateAttribute(includeAttr);
                    pathAttribute.Value = string.Format(attrValueTemplate, _ioProvider.RootDirInfo.Name, scriptFolder, dbObject);
                    includeAllNode.Attributes.Append(pathAttribute);
                    xmlDocument.DocumentElement.AppendChild(includeAllNode);
                }
            }

            xmlDocument.Save(fileFullPath);
        }
    }
}