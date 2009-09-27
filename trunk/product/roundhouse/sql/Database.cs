namespace roundhouse.infrastructure.sql
{
    public interface Database
    {
        void create_database(string server_name, string database_name);
        void delete_database(string server_name, string database_name);
        void run_sql(string server_name, string database_name, string sql_to_run);
    }
}