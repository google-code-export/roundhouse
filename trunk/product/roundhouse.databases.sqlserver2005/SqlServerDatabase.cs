using roundhouse.sql;

namespace roundhouse.databases.sqlserver2005
{
    using System.Data;
    using System.Data.SqlClient;
    using infrastructure.extensions;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;
    using Database = databases.Database;

    public sealed class SqlServerDatabase : Database
    {
        public string server_name { get; set; }
        public string database_name { get; set; }
        public string provider { get; set; }
        public string connection_string { get; set; }
        public string roundhouse_schema_name { get; set; }
        public string version_table_name { get; set; }
        public string scripts_run_table_name { get; set; }
        public string user_name { get; set; }

        public const string MASTER_DATABASE_NAME = "Master";
        private Server sql_server;
        private bool running_a_transaction = false;

        public void initialize_connection()
        {
            if (!string.IsNullOrEmpty(connection_string))
            {
                string[] parts = connection_string.Split(';');
                foreach (string part in parts)
                {
                    if (string.IsNullOrEmpty(server_name) && (part.to_lower().Contains("server") || part.to_lower().Contains("data source")))
                    {
                        server_name = part.Substring(part.IndexOf("=") + 1);
                    }

                    if (string.IsNullOrEmpty(database_name) && (part.to_lower().Contains("initial catalog") || part.to_lower().Contains("database")))
                    {
                        database_name = part.Substring(part.IndexOf("=") + 1);
                    }
                }
            }

            if (string.IsNullOrEmpty(connection_string) || connection_string.to_lower().Contains(database_name))
            {
                connection_string = build_connection_string(server_name, MASTER_DATABASE_NAME);
            }
        }

        private static string build_connection_string(string server_name, string database_name)
        {
            return string.Format("Server={0};initial catalog={1};Integrated Security=SSPI", server_name, database_name);
        }

        public void open_connection(bool with_transaction)
        {
            sql_server = new Server(new ServerConnection(new SqlConnection(connection_string)));
            sql_server.ConnectionContext.Connect();
            if (with_transaction)
            {
                sql_server.ConnectionContext.BeginTransaction();
                running_a_transaction = true;
            }
        }

        public void close_connection()
        {
            if (running_a_transaction)
            {
                sql_server.ConnectionContext.CommitTransaction();
            }

            sql_server.ConnectionContext.Disconnect();
        }

        public void create_database_if_it_doesnt_exist()
        {
            run_sql(MASTER_DATABASE_NAME, SqlScripts.t_sql_scripts.create_database(database_name));
        }

        public void set_recovery_mode(bool simple)
        {
            run_sql(MASTER_DATABASE_NAME, SqlScripts.t_sql_scripts.set_recovery_mode(database_name, simple));
        }

        public void backup_database(string output_path_minus_database)
        {
            //todo: backup database is not a script - it is a command
            //Server sql_server =
            //    new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            //sql_server.BackupDevices.Add(new BackupDevice(sql_server,database_name));
        }

        public void restore_database(string restore_from_path)
        {
            run_sql(MASTER_DATABASE_NAME, SqlScripts.t_sql_scripts.restore_database(database_name, restore_from_path));
        }

        public void delete_database_if_it_exists()
        {
            run_sql(MASTER_DATABASE_NAME, SqlScripts.t_sql_scripts.delete_database(database_name));
        }

        public void create_roundhouse_schema_if_it_doesnt_exist()
        {
            run_sql(SqlScripts.t_sql_scripts.create_roundhouse_schema(roundhouse_schema_name));
        }

        public void create_roundhouse_version_table_if_it_doesnt_exist()
        {
            run_sql(SqlScripts.t_sql_scripts.create_roundhouse_version_table(roundhouse_schema_name, version_table_name));
        }

        public void create_roundhouse_scripts_run_table_if_it_doesnt_exist()
        {
            run_sql(SqlScripts.t_sql_scripts.create_roundhouse_scripts_run_table(roundhouse_schema_name, version_table_name, scripts_run_table_name));
        }

        public void run_sql(string sql_to_run)
        {
            run_sql(database_name, sql_to_run);
        }

        public void run_sql(string database_name, string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            sql_server.ConnectionContext.ExecuteNonQuery(sql_to_run);
        }

        public void insert_script_run(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id)
        {
            run_sql(SqlScripts.t_sql_scripts.insert_script_run(roundhouse_schema_name, scripts_run_table_name, version_id, script_name, sql_to_run, sql_to_run_hash, run_this_script_once, user_name));
        }

        public string get_version(string repository_path)
        {
            return (string)run_sql_scalar(SqlScripts.t_sql_scripts.get_version(roundhouse_schema_name, version_table_name, repository_path));
        }

        public long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            return (long)run_sql_scalar(SqlScripts.t_sql_scripts.insert_version_and_get_version_id(roundhouse_schema_name, version_table_name, repository_path, repository_version, user_name));
        }

        public string get_current_script_hash(string script_name)
        {
            return (string)run_sql_scalar(SqlScripts.t_sql_scripts.get_current_script_hash(roundhouse_schema_name, scripts_run_table_name, script_name));
        }

        public bool has_run_script_already(string script_name)
        {
            bool script_has_run = false;

            DataTable data_table = execute_datatable(SqlScripts.t_sql_scripts.has_script_run(roundhouse_schema_name, scripts_run_table_name, script_name));
            if (data_table.Rows.Count > 0)
            {
                script_has_run = true;
            }

            return script_has_run;
        }

        public object run_sql_scalar(string sql_to_run)
        {
            return run_sql_scalar(database_name, sql_to_run);
        }

        public object run_sql_scalar(string database_name, string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            object return_value = sql_server.ConnectionContext.ExecuteScalar(sql_to_run);

            return return_value;
        }

        private DataTable execute_datatable(string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            DataSet result = sql_server.ConnectionContext.ExecuteWithResults(sql_to_run);

            return result.Tables.Count == 0 ? null : result.Tables[0];
        }

        private bool disposing = false;
        public void Dispose()
        {
            if (!disposing)
            {
                //todo: do we have anything to dispose?
                disposing = true;
            }
        }
    }
}