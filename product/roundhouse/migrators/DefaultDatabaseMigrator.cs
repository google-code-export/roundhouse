using System;
using roundhouse.infrastructure.logging;
using roundhouse.sql;

namespace roundhouse.migrators
{
    public class DefaultDatabaseMigrator : DatabaseMigrator
    {
        public Database database { get; set; }
        private readonly bool restoring_database;
        private readonly string restore_path;

        public DefaultDatabaseMigrator(Database database, bool restoring_database, string restore_path)
        {
            Log.bound_to(this).log_a_debug_event_containing(
                "Using an instance of SqlServerDatabase with {0},{1},{2},{3},{4}.",
                 database.server_name, database.database_name, database.roundhouse_schema_name,
                database.version_table_name, database.scripts_run_table_name);
            this.database = database;
            this.restoring_database = restoring_database;
            this.restore_path = restore_path;
        }

        public void create_or_restore_database()
        {
            Log.bound_to(this).log_an_info_event_containing("Creating {0} database on {1} server if it doesn't exist.",
                                                            database.database_name, database.server_name);
            database.run_sql(database.MASTER_DATABASE_NAME,
                             database.create_database_script()
                );

            if (restoring_database)
            {
                restore_database(restore_path);
            } 
        }

        public void restore_database(string restore_from_path)
        {
            string restore_script = database.restore_database_script(restore_from_path);
            Log.bound_to(this).log_an_info_event_containing("Restoring {0} database on {1} server from path {2}. Executing: {3}{4}",
                                                        database.database_name, database.server_name, restore_from_path, Environment.NewLine, restore_script);
            //Log.bound_to(this).log_an_info_event_containing("{0} RESTORING DATABASE ({1} - {2}) {0}",
            //                                                "=".PadRight(9, '='),
            //                                                database.server_name,
            //                                                database.database_name
            //                                                );
            database.run_sql(database.MASTER_DATABASE_NAME,
                             restore_script
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

        public long version_the_database(string repository_path, string repository_version)
        {
            Log.bound_to(this).log_an_info_event_containing("Versioning {0} database with version {1} based on {2}.",
                                                            database.database_name,
                                                            repository_version, repository_path);
            return (long)database.run_sql_scalar(database.database_name,
                             database.insert_version_script(repository_path, repository_version));
        }

        public void run_sql(string sql_to_run, string script_name, bool run_this_script_once, long version_id)
        {
            if (this_script_should_run(script_name, run_this_script_once))
            {
                Log.bound_to(this).log_an_info_event_containing("Running {0} on {1} - {2}.", script_name, database.server_name, database.database_name);
                database.run_sql(database.database_name, sql_to_run);
                record_script_in_scripts_run_table(script_name, sql_to_run, run_this_script_once, version_id);
            }
            else
            {
                Log.bound_to(this).log_an_info_event_containing("Skipped {0}.", script_name);
            }
        }

        public void record_script_in_scripts_run_table(string script_name, string sql_to_run, bool run_this_script_once, long version_id)
        {
            Log.bound_to(this).log_a_debug_event_containing("Recording {0} script ran on {1} - {2}.", script_name,
                                                            database.server_name, database.database_name);
            database.run_sql(database.database_name, database.insert_script_run_script(script_name, sql_to_run, run_this_script_once, version_id));
        }

        private bool this_script_should_run(string script_name, bool run_this_script_once)
        {
            if (!run_this_script_once) return true;

            return !database.has_run_script_already(script_name);
        }
    }
}