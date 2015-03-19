using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace DbHierarchyGenerator.Models
{
    public class HistoryConfig
    {
        [XmlAttribute(AttributeName = "partitionJob")]
        public string PartitionJobName { get; set; }

        [XmlAttribute(AttributeName = "schema")]
        public string SchemaName { get; set; }

        public List<HistoryTable> HistoryTableList { get; set; }
    }

    public class HistoryTable : IEquatable<Table>
    {
        [XmlAttribute(AttributeName = "schema")]
        public string Schema { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        public ParentPartitioning ParentPartitioning { get; set; }
        public DatePartitioning DatePartitioning { get; set; }
        public bool Equals(Table other)
        {
            return other != null && Schema == other.SchemaName && Name == other.Name;
        }
    }

    public class ParentPartitioning
    {
        [XmlAttribute(AttributeName = "interval")]
        public int Interval { get; set; }
    }

    public class DatePartitioning
    {
        public DatePartitioning()
        {
            Interval = 1;
        }

        [XmlAttribute(AttributeName = "interval")]
        public int Interval { get; set; }

        [XmlAttribute(AttributeName = "unit")]
        public DateUnit Unit { get; set; }
    }

    public enum DateUnit
    {
        Day,
        Week,
        Month,
        Year
    }
}
