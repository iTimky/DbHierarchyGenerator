#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbHierarchyGenerator.Models;

#endregion

namespace DbHierarchyGenerator.Templates
{
    public abstract class SqlPartitionGenerator
    {
        private static readonly Dictionary<Type, SqlPartitionGenerator> Generators =
            new Dictionary<Type, SqlPartitionGenerator>
            {
                {typeof (IntPartitionFunction), new SqlPartitionIntGenerator()},
                {typeof (DatePartitionFunction), new SqlParitionDateGenerator()}
            };

        public static SqlPartitionGenerator GetGenerator(PartitionFunction function)
        {
            return Generators[function.GetType()];
        }

        public string GeneratePartitionCreation(PartitionScheme partitionScheme)
        {
            return string.Format(SqlTemplates.PartitionCreateTemplate + Environment.NewLine, partitionScheme.Function.Name,
                partitionScheme.Function.ColumnType, partitionScheme.Name,
                string.Join(", ", partitionScheme.FileGroups.Select(fg => string.Format("[{0}]", fg))),
                partitionScheme.FileGroups.First(),
                GetFuntionSplitValue(partitionScheme.Function)) + Environment.NewLine;
        }

        public string GenerateJobStepCreationg(Table table)
        {
            var partitionFuntionExec = GetPartitionFuntionExec(table.Name, table.PartitionScheme.Function);
            return string.Format(SqlTemplates.AddHistoryPartitionsJobStepTemplate, table.PartitionScheme.JobName, table.Name, partitionFuntionExec);
        }

        protected abstract string GetFuntionSplitValue(PartitionFunction partitionFunction);
        protected abstract string GetPartitionFuntionExec(string tableName, PartitionFunction partitionFunction);
    }

    public class SqlPartitionIntGenerator : SqlPartitionGenerator
    {
        protected override string GetFuntionSplitValue(PartitionFunction partitionFunction)
        {
            return partitionFunction.Interval.ToString();
        }

        protected override string GetPartitionFuntionExec(string tableName, PartitionFunction partitionFunction)
        {
            return string.Format(SqlTemplates.IntPartitionFunctionExecTemplate, tableName, partitionFunction.Interval);
        }
    }

    public class SqlParitionDateGenerator : SqlPartitionGenerator
    {
        protected override string GetFuntionSplitValue(PartitionFunction partitionFunction)
        {
            var function = ConvertFunction(partitionFunction);
            return string.Format("dateadd({0}, datediff({0}, 0, getdate()), {1})", function.Unit, function.Interval);
        }

        protected override string GetPartitionFuntionExec(string tableName, PartitionFunction partitionFunction)
        {
            var function = ConvertFunction(partitionFunction);
            return string.Format(SqlTemplates.DatePartitionFunctionExecTemplate, tableName, function.Unit, function.Interval);
        }

        private DatePartitionFunction ConvertFunction(PartitionFunction partitionFunction)
        {
            var function = partitionFunction as DatePartitionFunction;
            if (function == null)
                throw new ArgumentException("Wrong partition partitionFunction for SqlParitionDateGenerator");

            return function;
        }
    }
}