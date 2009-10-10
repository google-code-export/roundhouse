namespace roundhouse.migrators
{
    using sql;

    public class DefaultDatabaseMigrator : DatabaseMigrator
    {
        private readonly Database database;

        public DefaultDatabaseMigrator(Database database)
        {
            this.database = database;
        }

        public void create_database()
        {
            database.run_sql(database.MASTER_DATABASE_NAME,
                             database.create_database_script()
                );
        }

        public void restore_database(string restore_from_path)
        {
            database.run_sql(database.MASTER_DATABASE_NAME,
                             database.restore_database_script(restore_from_path)
                );
        }

        public void verify_or_create_roundhouse_tables()
        {
            database.run_sql(database.database_name,
                             database.create_roundhouse_schema_script()
                );
            database.run_sql(database.database_name,
                             database.create_roundhouse_version_table_script()
                );
            database.run_sql(database.database_name,
                             database.create_roundhouse_scripts_run_table_script()
                );
        }

        public void delete_database()
        {
            database.run_sql(database.MASTER_DATABASE_NAME,
                             database.delete_database_script()
                );
        }

        public void version_the_database(string repository_path, string repository_version)
        {
            database.run_sql(database.database_name,
                             database.insert_version_script(repository_path, repository_version)
                );
        }

        public void run_sql(string sql_to_run, string script_name, bool run_this_script_once)
        {
            if (this_script_should_run(script_name, run_this_script_once))
            {
                database.run_sql(database.database_name, sql_to_run);
                record_script_in_scripts_run_table(script_name, sql_to_run, run_this_script_once);
            }
        }

        public void record_script_in_scripts_run_table(string script_name, string sql_to_run, bool run_this_script_once)
        {
            database.run_sql(database.database_name,
                             database.insert_script_run_script(script_name, sql_to_run, run_this_script_once)
                );
        }

        private bool this_script_should_run(string script_name, bool run_this_script_once)
        {
            if (!run_this_script_once) return true;
            //todo:check to see if it has already run
            return true;
        }
    }
}