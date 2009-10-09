namespace roundhouse.sql
{
    public interface Database
    {
        void create_database(string server_name, string database_name);
        void verify_or_create_roundhouse_tables(string server_name, string database_name, string roundhouse_schema_name, string version_table_name,string scripts_run_table_name);
        void delete_database(string server_name, string database_name);
        void run_sql(string server_name, string database_name, string sql_to_run, string script_name,
                bool run_this_script_once);
    }
}