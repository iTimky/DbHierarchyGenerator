using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DbHierarchyGenerator.Models
{
    public abstract class PartitionFunction : DbObject
    {
        public PartitionFunction(string name, ColumnType columnType, int interval) : base(null, name)
        {
            ColumnType = columnType;
            Interval = interval;
        }

        public readonly ColumnType ColumnType;
        public readonly int Interval;
    }

    public class IntPartitionFunction : PartitionFunction
    {
        public IntPartitionFunction(string name, ColumnType columnType, int interval) : base(name, columnType, interval) { }

//        public readonly List<int> Values = new List<int>();
    }

    public class DatePartitionFunction : PartitionFunction
    {
        public DatePartitionFunction(string name, ColumnType columnType, int interval, DateUnit unit)
            : base(name, columnType, interval)
        {
            Unit = unit;
        }

//        public readonly List<DateTime> Values = new List<DateTime>();
        public readonly DateUnit Unit;
    }

    public class PartitionScheme : DbObject
    {
        public PartitionScheme(string name, PartitionFunction function, string jobName = null, List<string> fileGroups = null) : base(null, name)
        {
            Function = function;
            JobName = jobName;

            if (fileGroups == null || !fileGroups.Any())
                FileGroups.Add("primary");
            else
                FileGroups.AddRange(fileGroups);
        }

        public readonly PartitionFunction Function;
        public readonly List<string> FileGroups = new List<string>();
        public readonly string JobName;
    }
}