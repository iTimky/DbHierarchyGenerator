namespace DbHierarchyGenerator.Templates
{
    public static class XmlTemplates
    {
        public const string ChangeLogTemplate = @"<?xml version=""1.0"" encoding=""UTF-8""?> 
<databaseChangeLog
  xmlns=""http://www.liquibase.org/xml/ns/dbchangelog""
  xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
  xsi:schemaLocation=""http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-3.1.xsd"">

{0}

</databaseChangeLog>";


        public const string ChangeSetTemplate = @"  <changeSet {0}>
{1}
    </changeSet>";

        public const string AttributeTemplate = @"{0}=""{1}""";

        public const string SqlTemplate = @"        <sql splitStatements=""false""><![CDATA[
{0}
     ]]></sql>";

        public const string RollbackTemplate = @"        <rollback><![CDATA[
{0}
     ]]></rollback>";

        public const string HistoryConfigTemplate = @"<?xml version=""1.0"" encoding=""UTF-8""?> 
<HistoryConfig>
    <HistoryTableList>
    
        <!-- <HistoryTable schema=""dbo"" name=""Employees""/> -->
        
    </HistoryTableList>
</HistoryConfig>";
    }
}
