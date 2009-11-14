namespace roundhouse.sql
{
    public interface Database
    {
        string server_name { get; set; }
        string database_name { get; set; }
        string roundhouse_schema_name { get; set; }
        string version_table_name { get; set; }
        string scripts_run_table_name { get; set; }
        string user_name { get; set; }
        string MASTER_DATABASE_NAME { get; }

        string create_database_script();
        void backup_database(string output_path_minus_database);
        string restore_database_script(string restore_from_path);
        string delete_database_script();
        string create_roundhouse_schema_script();
        string create_roundhouse_version_table_script();
        string create_roundhouse_scripts_run_table_script();
        string insert_version_script(string repository_path, string repository_version);
        string insert_script_run_script(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id);
        bool has_run_script_already(string script_name);
        string get_current_script_hash_script(string script_name);
        void run_sql(string database_name, string sql_to_run);
        object run_sql_scalar(string database_name, string sql_to_run);
        string get_version_script(string repository_path);
    }
}