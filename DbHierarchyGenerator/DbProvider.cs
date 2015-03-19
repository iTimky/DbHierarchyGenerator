using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbHierarchyGenerator
{
    public class DbProvider
    {
        private readonly int _commandTimeout = 30;
        private const string ConnectionStringName = "Gen";
        public string ConnectionString { get; private set; }

        public DbProvider(int commandTimeout = 30)
        {
            _commandTimeout = commandTimeout;
            ConnectionString = GetConnectionString();
        }

        private static string GetConnectionString()
        {
            ConnectionStringSettingsCollection cscol = ConfigurationManager.ConnectionStrings;
            if (cscol == null)
                throw new Exception("ConfigurationManager.ConnectionStrings==null");

            ConnectionStringSettings cs = cscol[ConnectionStringName];
            if (cs == null)
                throw new Exception(string.Format("No connection string [{0}]", ConnectionStringName));
            return cs.ConnectionString;
        }

        private SqlCommand PrepareCommand(SqlConnection connection, string query, CommandType commandType = CommandType.Text)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandType = commandType;
            command.CommandTimeout = _commandTimeout;
            command.CommandText = query;
            connection.Open();
            return command;
        }

        public DataTable Exec(string query)
        {
            var results = new DataTable();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = PrepareCommand(connection, query))
            using (var dataAdapter = new SqlDataAdapter(command))
                dataAdapter.Fill(results);

            return results;
        }

        public List<T> Exec<T>(string query, Func<SqlDataReader, T> converter)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = PrepareCommand(connection, query))
            using (var reader = command.ExecuteReader())
            {
                var items = new List<T>();
                while (reader.Read())
                    items.Add(converter(reader));
                return items;
            }
        }
    }
}
