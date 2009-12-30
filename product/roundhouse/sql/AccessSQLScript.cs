namespace roundhouse.sql
{
    public class AccessSQLScript : SqlScript
    {
        public string create_database(string database_name)
        {
            throw new System.NotImplementedException();
        }

        public string set_recovery_mode(string database_name, bool simple)
        {
            throw new System.NotImplementedException();
        }

        public string restore_database(string database_name, string restore_from_path)
        {
            throw new System.NotImplementedException();
        }

        public string delete_database(string database_name)
        {
            throw new System.NotImplementedException();
        }

        public string create_roundhouse_schema(string roundhouse_schema_name)
        {
            throw new System.NotImplementedException();
        }

        public string create_roundhouse_version_table(string roundhouse_schema_name, string version_table_name)
        {
            throw new System.NotImplementedException();
        }

        public string create_roundhouse_scripts_run_table(string roundhouse_schema_name, string version_table_name, string scripts_run_table_name)
        {
            throw new System.NotImplementedException();
        }

        public string use_database(string database_name)
        {
            throw new System.NotImplementedException();
        }

        public string get_version(string roundhouse_schema_name, string version_table_name, string repository_path)
        {
            throw new System.NotImplementedException();
        }

        public string insert_version_and_get_version_id(string roundhouse_schema_name, string version_table_name, string repository_path, string repository_version, string user_name)
        {
            throw new System.NotImplementedException();
        }

        public string get_current_script_hash(string roundhouse_schema_name, string scripts_run_table_name, string script_name)
        {
            throw new System.NotImplementedException();
        }

        public string has_script_run(string roundhouse_schema_name, string scripts_run_table_name, string script_name)
        {
            throw new System.NotImplementedException();
        }

        public string insert_script_run(string roundhouse_schema_name, string scripts_run_table_name, long version_id, string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, string user_name)
        {
            throw new System.NotImplementedException();
        }
    }
}