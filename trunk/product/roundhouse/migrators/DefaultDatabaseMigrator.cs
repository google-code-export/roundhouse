using roundhouse.infrastructure.logging;
using roundhouse.sql;

namespace roundhouse.migrators
{
    public class DefaultDatabaseMigrator : DatabaseMigrator
    {
        private readonly Database database;

        public DefaultDatabaseMigrator(Database database)
        {
            Log.bound_to(this).log_a_debug_event_containing(
                "Using an instance of SqlServerDatabase with {0},{1},{2},{3},{4}.",
                 database.server_name, database.database_name, database.roundhouse_schema_name,
                database.version_table_name, database.scripts_run_table_name);
            this.database = database;
        }

        public void create_database()
        {
            Log.bound_to(this).log_an_info_event_containing("Creating {0} database on {1} server if it doesn't exist.",
                                                            database.database_name, database.server_name);
            database.run_sql(database.MASTER_DATABASE_NAME, 
                             database.create_database_script()
                );
        }

        public void restore_database(string restore_from_path)
        {
            Log.bound_to(this).log_an_info_event_containing("Restoring {0} database on {1} server.",
                                                            database.database_name, database.server_name);
            database.run_sql(database.MASTER_DATABASE_NAME,
                             database.restore_database_script(restore_from_path)
                );
        }

        public void verify_or_create_roundhouse_tables()
        {
            Log.bound_to(this).log_an_info_event_containing("Creating {0} schema if it doesn't exist.",
                                                            database.roundhouse_schema_name);
            database.run_sql(database.database_name,
                             database.create_roundhouse_schema_script()
                );
            Log.bound_to(this).log_an_info_event_containing("Creating [{0}].[{1}] table if it doesn't exist.",
                                                            database.roundhouse_schema_name,
                                                            database.version_table_name);
            database.run_sql(database.database_name,
                             database.create_roundhouse_version_table_script()
                );
            Log.bound_to(this).log_an_info_event_containing("Creating [{0}].[{1}] table if it doesn't exist.",
                                                            database.roundhouse_schema_name,
                                                            database.scripts_run_table_name);
            database.run_sql(database.database_name,
                             database.create_roundhouse_scripts_run_table_script()
                );
        }

        public void delete_database()
        {
            Log.bound_to(this).log_an_info_event_containing("Deleting {0} database on {1} server.",
                                                            database.database_name, database.server_name);
            database.run_sql(database.MASTER_DATABASE_NAME,
                             database.delete_database_script()
                );
        }

        public void version_the_database(string repository_path, string repository_version)
        {
            Log.bound_to(this).log_an_info_event_containing("Versioning {0} database with {1}-v{2}.",
                                                            database.database_name, repository_path,
                                                            repository_version);
            database.run_sql(database.database_name,
                             database.insert_version_script(repository_path, repository_version));
        }

        public void run_sql(string sql_to_run, string script_name, bool run_this_script_once)
        {
            if (this_script_should_run(script_name, run_this_script_once))
            {
                Log.bound_to(this).log_an_info_event_containing("Running - {0}.", script_name);
                database.run_sql(database.database_name, sql_to_run);
                record_script_in_scripts_run_table(script_name, sql_to_run, run_this_script_once);
            }
            else
            {
                Log.bound_to(this).log_an_info_event_containing("Skipped - {0}.", script_name);
            }
        }

        public void record_script_in_scripts_run_table(string script_name, string sql_to_run, bool run_this_script_once)
        {
            Log.bound_to(this).log_an_info_event_containing("Recording {0} script ran on {1}-{2}.", script_name,
                                                            database.server_name, database.database_name);
            database.run_sql(database.database_name, database.insert_script_run_script(script_name, sql_to_run, run_this_script_once));
        }

        private bool this_script_should_run(string script_name, bool run_this_script_once)
        {
            if (!run_this_script_once) return true;

            return !database.has_run_script_already(script_name);
        }
    }
}