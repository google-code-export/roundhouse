namespace roundhouse.databases.oledb
{
    using System.Data.OleDb;
    using infrastructure.extensions;
    using infrastructure.logging;
    using Database = databases.Database;

    public class OleDbDatabase : Database
    {

        public string server_name { get; set; }
        public string database_name { get; set; }
        public string provider { get; set; }
        public string connection_string { get; set; }
        public string roundhouse_schema_name { get; set; }
        public string version_table_name { get; set; }
        public string scripts_run_table_name { get; set; }
        public string user_name { get; set; }

        private OleDbConnection server_connection;
        private bool running_a_transaction = false;
        private bool disposing = false;
        private const int sixty_seconds = 60;

        public void initialize_connection()
        {
            if (!string.IsNullOrEmpty(connection_string))
            {
                string[] parts = connection_string.Split(';');
                foreach (string part in parts)
                {
                    if (string.IsNullOrEmpty(server_name) && part.to_lower().Contains("server"))
                    {
                        server_name = part.Substring(part.IndexOf("=") + 1);
                    }

                    if (string.IsNullOrEmpty(database_name) && part.to_lower().Contains("database"))
                    {
                        database_name = part.Substring(part.IndexOf("=") + 1);
                    }
                }
            }


            if (string.IsNullOrEmpty(connection_string) || connection_string.to_lower().Contains(database_name))
            {
                connection_string = build_connection_string(server_name, database_name);
            }

        }

        private static string build_connection_string(string server_name, string database_name)
        {
            return string.Format("Provider=SQLNCLI;Server={0};Database={1};Trusted_Connection=yes;", server_name, database_name);
        }

        public void open_connection(bool with_transaction)
        {
            server_connection = new OleDbConnection(connection_string);
            server_connection.Open();

            if (with_transaction)
            {
                server_connection.BeginTransaction();
                running_a_transaction = true;
            }
        }

        public void close_connection()
        {
            if (running_a_transaction)
            {
                //commit?
            }
            server_connection.Close();
        }

        public void create_database_if_it_doesnt_exist()
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for creating a database at this time.");
        }

        public void set_recovery_mode(bool simple)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for setting recovery mode to simple at this time.");
        }

        public void backup_database(string output_path_minus_database)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for backing up a database at this time.");
        }

        public void restore_database(string restore_from_path)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for restoring a database at this time.");
        }

        public void delete_database_if_it_exists()
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for deleting a database at this time.");
        }

        public void create_roundhouse_schema_if_it_doesnt_exist()
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for creating roundhouse schema at this time.");
        }

        public void create_roundhouse_version_table_if_it_doesnt_exist()
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for creating roundhouse version table at this time.");
        }

        public void create_roundhouse_scripts_run_table_if_it_doesnt_exist()
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for creating roundhouse scripts run table at this time.");
        }

        public void run_sql(string sql_to_run)
        {
            run_sql(database_name, sql_to_run);
        }

        public void run_sql(string database_name, string sql_to_run)
        {
            if (string.IsNullOrEmpty(sql_to_run)) return;

            using (OleDbCommand command = server_connection.CreateCommand())
            {
                command.CommandText = sql_to_run;
                command.CommandType = System.Data.CommandType.Text;
                command.CommandTimeout = sixty_seconds;
                command.ExecuteNonQuery();
                command.Dispose();
            }
        }

        public void insert_script_run(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for recording scripts run at this time.");
        }

        public string get_version(string repository_path)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for retrieving versions at this time.");
            return "0";
        }

        public long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for inserting versions at this time.");
            return 0;
        }

        public bool has_run_script_already(string script_name)
        {
            return false;
        }

        public string get_current_script_hash(string script_name)
        {
            Log.bound_to(this).log_a_warning_event_containing("OleDB does not provide a facility for hashing (through recording scripts run) at this time.");
            return string.Empty;
        }

        public object run_sql_scalar(string sql_to_run)
        {
            return run_sql_scalar(database_name, sql_to_run);
        }

        public object run_sql_scalar(string database_name, string sql_to_run)
        {
            object return_value = new object();

            if (string.IsNullOrEmpty(sql_to_run)) return return_value;

            using (OleDbCommand command = server_connection.CreateCommand())
            {
                command.CommandText = sql_to_run;
                command.CommandType = System.Data.CommandType.Text;
                command.CommandTimeout = sixty_seconds;
                return_value = command.ExecuteScalar();
                command.Dispose();
            }

            return return_value;
        }

        public void Dispose()
        {
            if (!disposing)
            {
                server_connection.Dispose();
                disposing = true;
            }
        }
    }
}