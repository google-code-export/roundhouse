namespace roundhouse.sql
{
    public sealed class SqlServerLiteSpeedDatabase : Database
    {
        private readonly Database database;

        public SqlServerLiteSpeedDatabase(Database database)
        {
            this.database = database;
        }

        public string server_name
        {
            get { return database.server_name; }
            set { database.server_name = value; }
        }

        public string database_name
        {
            get { return database.database_name; }
            set { database.database_name = value; }
        }

        public string roundhouse_schema_name
        {
            get { return database.roundhouse_schema_name; }
            set { database.roundhouse_schema_name = value; }
        }

        public string version_table_name
        {
            get { return database.version_table_name; }
            set { database.version_table_name = value; }
        }

        public string scripts_run_table_name
        {
            get { return database.scripts_run_table_name; }
            set { database.scripts_run_table_name = value; }
        }

        public string user_name
        {
            get { return database.user_name; }
            set { database.user_name = value; }
        }

        public string MASTER_DATABASE_NAME
        {
            get { return database.MASTER_DATABASE_NAME; }
        }

        public string create_database_script()
        {
            return database.create_database_script();
        }

        public void backup_database(string output_path_minus_database)
        {
            database.backup_database(output_path_minus_database);
        }

        public string restore_database_script(string restore_from_path)
        {
            return string.Format(
                @"USE Master 
                        ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        ALTER DATABASE [{0}] SET MULTI_USER;

                        exec master.dbo.xp_restore_database @database = N'{0}',
                            @filename = N'{1}',
                            @filenumber = 1, 
                            @with = N'RECOVERY', 
                            @with = N'NOUNLOAD',
                            @with = N'REPLACE',
                            @with = N'STATS = 10';

                        ALTER DATABASE [{0}] SET MULTI_USER;
                        ALTER DATABASE [{0}] SET RECOVERY SIMPLE;
                        --DBCC SHRINKDATABASE ([{0}]);
                        ",
                    database_name, restore_from_path);
        }

        public string delete_database_script()
        {
            return database.delete_database_script();
        }

        public string create_roundhouse_schema_script()
        {
            return database.create_roundhouse_schema_script();
        }

        public string create_roundhouse_version_table_script()
        {
            return database.create_roundhouse_version_table_script();
        }

        public string create_roundhouse_scripts_run_table_script()
        {
            return database.create_roundhouse_scripts_run_table_script();
        }

        public string get_version_script(string repository_path)
        {
            return database.get_version_script(repository_path);
        }

        public string insert_version_script(string repository_path, string repository_version)
        {
            return database.insert_version_script(repository_path, repository_version);
        }

        public string insert_script_run_script(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id)
        {
            return database.insert_script_run_script(script_name, sql_to_run,sql_to_run_hash, run_this_script_once, version_id);
        }

        public string get_current_script_hash_script(string script_name)
        {
            return database.get_current_script_hash_script(script_name);
        }

        public bool has_run_script_already(string script_name)
        {
            return database.has_run_script_already(script_name);
        }

        public void run_sql(string database_name, string sql_to_run)
        {
            database.run_sql(database_name, sql_to_run);
        }

        public object run_sql_scalar(string database_name, string sql_to_run)
        {
            return database.run_sql_scalar(database_name, sql_to_run);
        }
    }
}