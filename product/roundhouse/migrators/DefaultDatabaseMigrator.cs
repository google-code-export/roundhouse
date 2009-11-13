using System;
using roundhouse.infrastructure.logging;
using roundhouse.sql;

namespace roundhouse.migrators
{
    using cryptography;

    public class DefaultDatabaseMigrator : DatabaseMigrator
    {
        public Database database { get; set; }
        private readonly CryptographicService crypto_provider;
        private readonly bool restoring_database;
        private readonly string restore_path;
        private readonly string output_path;
        private readonly bool error_on_one_time_script_changes;

        public DefaultDatabaseMigrator(Database database, CryptographicService crypto_provider, bool restoring_database, string restore_path, string output_path, bool error_on_one_time_script_changes)
        {
            Log.bound_to(this).log_a_debug_event_containing(
                "Using an instance of SqlServerDatabase with {0},{1},{2},{3},{4}.",
                 database.server_name, database.database_name, database.roundhouse_schema_name,
                database.version_table_name, database.scripts_run_table_name);
            this.database = database;
            this.crypto_provider = crypto_provider;
            this.restoring_database = restoring_database;
            this.restore_path = restore_path;
            this.output_path = output_path;
            this.error_on_one_time_script_changes = error_on_one_time_script_changes;
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

        public void backup_database_if_it_exists()
        {
            database.backup_database(output_path);
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

        public string get_current_version(string repository_path)
        {
            string current_version = (string)database.run_sql_scalar(database.database_name,
                             database.get_version_script(repository_path));
            if (string.IsNullOrEmpty(current_version))
            {
                current_version = "0";
            }

            return current_version;
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
            if (this_is_a_one_time_script_that_has_changes_but_has_already_been_run(script_name, sql_to_run, run_this_script_once))
            {
                if (error_on_one_time_script_changes)
                {
                    throw new Exception(string.Format("{0} has changed since the last time it was run. By default this is not allowed - scripts that run once should never change. To change this behavior to a warning, please set warnOnOneTimeScriptChanges to true and run again. Stopping execution.", script_name));
                }
                Log.bound_to(this).log_a_warning_event_containing("{0} is a one time script that has changed since it was run.", script_name);
            }

            if (this_script_should_run(script_name, sql_to_run, run_this_script_once))
            {
                Log.bound_to(this).log_an_info_event_containing("Running {0} on {1} - {2}.", script_name, database.server_name, database.database_name);
                database.run_sql(database.database_name, sql_to_run);
                record_script_in_scripts_run_table(script_name, sql_to_run, run_this_script_once, version_id);
            }
            else
            {
                Log.bound_to(this).log_an_info_event_containing("Skipped {0} either due to being a one time script or finding no changes.", script_name);
            }
        }

        public void record_script_in_scripts_run_table(string script_name, string sql_to_run, bool run_this_script_once, long version_id)
        {
            Log.bound_to(this).log_a_debug_event_containing("Recording {0} script ran on {1} - {2}.", script_name,
                                                            database.server_name, database.database_name);
            database.run_sql(database.database_name, database.insert_script_run_script(script_name, sql_to_run, create_hash(sql_to_run), run_this_script_once, version_id));
        }

        private string create_hash(string sql_to_run)
        {
            return crypto_provider.hash(sql_to_run.Replace(@"'", @"''"));
        }

        private bool this_script_has_run_already(string script_name)
        {
            return database.has_run_script_already(script_name);
        }

        private bool this_is_a_one_time_script_that_has_changes_but_has_already_been_run(string script_name, string sql_to_run, bool run_this_script_once)
        {
            return this_script_has_changed_since_last_run(script_name, sql_to_run) && this_script_has_run_already(script_name) && run_this_script_once;
        }

        private bool this_script_has_changed_since_last_run(string script_name, string sql_to_run)
        {
            bool hash_is_same = false;

            string old_text_hash = string.Empty;
            try
            {
                old_text_hash = (string)database.run_sql_scalar(database.database_name,
                                     database.get_current_script_hash_script(script_name));
            }
            catch (Exception)
            {
                Log.bound_to(this).log_an_info_event_containing("{0} - I didn't find this script executed before.", script_name);
            }

            if (string.IsNullOrEmpty(old_text_hash)) return true;

            string new_text_hash = create_hash(sql_to_run);
            
            if (string.Compare(old_text_hash, new_text_hash, true) == 0)
            {
                hash_is_same = true;
            }

            return !hash_is_same;
        }

        private bool this_script_should_run(string script_name, string sql_to_run, bool run_this_script_once)
        {
            if (this_script_has_run_already(script_name) && run_this_script_once)
            {
                return false;
            }

            return this_script_has_changed_since_last_run(script_name, sql_to_run);
        }
    }
}