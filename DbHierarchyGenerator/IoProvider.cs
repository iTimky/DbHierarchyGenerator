using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DbHierarchyGenerator
{
    public class IoProvider
    {
        public DirectoryInfo RootDirInfo { get; private set; }
        public List<string> CreatedItemPaths { get; private set; }

        public IoProvider(string rootDir)
        {
            RootDirInfo = Directory.CreateDirectory(rootDir);
            CreatedItemPaths = new List<string>();
        }

        public void CreateFolder(string path, string name, string xmlInside = null)
        {
            if (Directory.Exists(string.Format("{0}\\{1}\\{2}", RootDirInfo.FullName, path, name)))
                return;

            string fullPath = Directory.CreateDirectory(string.Format("{0}\\{1}\\{2}", RootDirInfo.FullName, path, name)).FullName;
            Console.WriteLine(GetLocalPath(fullPath));
            CreatedItemPaths.Add(fullPath);

            if (path != "." && xmlInside != null)
                CreateXmlFileFromString(xmlInside, fullPath, name + "");
        }

        public string CreateXmlFileLocalPath(string xml, string path, string name)
        {
            string fullPath = string.Format("{0}\\{1}", RootDirInfo.FullName, path);
            return CreateXmlFileFromString(xml, fullPath, name);
        }

        public string CreateXmlFileFromString(string xml, string fullPath, string name)
        {
            string fileName = string.Format("{0}\\{1}.xml", fullPath, name);

            if (File.Exists(fileName))
                return fileName;

            using (var file = new StreamWriter(fileName))
            {
                file.WriteLine(xml);
                file.Flush();
                file.Close();
            }

            CreatedItemPaths.Add(fileName);
            Console.WriteLine(GetLocalPath(fileName));

            return fileName;
        }

        public string GetLocalPath(string fullPath)
        {
            return fullPath.Substring(fullPath.IndexOf(RootDirInfo.Name, StringComparison.CurrentCulture));
        }


    }
}
