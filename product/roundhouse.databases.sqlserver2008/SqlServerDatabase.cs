namespace roundhouse.databases.sqlserver2008
{
    using System.Data;
    using System.Data.SqlClient;
    using infrastructure.extensions;
    using infrastructure.logging;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;
    using Database = databases.Database;

    public sealed class SqlServerDatabase : Database
    {
        public string server_name { get; set; }
        public string database_name { get; set; }
        public string connection_string { get; set; }
        public string roundhouse_schema_name { get; set; }
        public string version_table_name { get; set; }
        public string scripts_run_table_name { get; set; }
        public string user_name { get; set; }

        public const string MASTER_DATABASE_NAME = "Master";
        private Server sql_server;
        private bool running_a_transaction = false;

        public void initialize_connection()
        {
            if (!string.IsNullOrEmpty(connection_string))
            {
                string[] parts = connection_string.Split(';');
                foreach (string part in parts)
                {
                    if (string.IsNullOrEmpty(server_name) && (part.to_lower().Contains("server") || part.to_lower().Contains("data source")))
                    {
                        server_name = part.Substring(part.IndexOf("=") + 1);
                    }

                    if (string.IsNullOrEmpty(database_name) && (part.to_lower().Contains("initial catalog") || part.to_lower().Contains("database")))
                    {
                        database_name = part.Substring(part.IndexOf("=") + 1);
                    }
                }
            }

            if (string.IsNullOrEmpty(connection_string) || connection_string.to_lower().Contains(database_name))
            {
                connection_string = build_connection_string(server_name, MASTER_DATABASE_NAME);
            }
        }

        private static string build_connection_string(string server_name, string database_name)
        {
            return string.Format("Server={0};initial catalog={1};Integrated Security=SSPI", server_name, database_name);
        }

        public void open_connection(bool with_transaction)
        {
            sql_server = new Server(new ServerConnection(new SqlConnection(connection_string)));
            sql_server.ConnectionContext.Connect();
            if (with_transaction)
            {
                sql_server.ConnectionContext.BeginTransaction();
                running_a_transaction = true;
            }
        }

        public void close_connection()
        {
            if (running_a_transaction)
            {
                sql_server.ConnectionContext.CommitTransaction();
            }

            sql_server.ConnectionContext.Disconnect();
        }

        public void create_database_if_it_doesnt_exist()
        {
            run_sql(MASTER_DATABASE_NAME, string.Format(
                @"USE Master 
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                         BEGIN 
                            CREATE DATABASE [{0}] 
                         END
                        ",
                database_name));
        }

        public void set_recovery_mode(bool simple)
        {
            run_sql(MASTER_DATABASE_NAME, string.Format(
                @"USE Master 
                   ALTER DATABASE [{0}] SET RECOVERY {1}
                    ",
                     database_name, simple ? "SIMPLE" : "FULL")
                  );
        }

        public void backup_database(string output_path_minus_database)
        {
            //todo: backup database is not a script - it is a command
            //Server sql_server =
            //    new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            //sql_server.BackupDevices.Add(new BackupDevice(sql_server,database_name));
        }

        public void restore_database(string restore_from_path)
        {
            run_sql(MASTER_DATABASE_NAME, string.Format(
                @"USE Master 
                        ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        
                        RESTORE DATABASE [{0}]
                        FROM DISK = N'{1}'
                        WITH NOUNLOAD
                        , STATS = 10
                        , RECOVERY
                        , REPLACE;

                        ALTER DATABASE [{0}] SET MULTI_USER;
                        ALTER DATABASE [{0}] SET RECOVERY SIMPLE;
                        --DBCC SHRINKDATABASE ([{0}]);
                        ",
                database_name, restore_from_path)
                );
        }

        public void delete_database_if_it_exists()
        {
            run_sql(MASTER_DATABASE_NAME, string.Format(
                @"USE Master 
                        IF EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                        BEGIN 
                            ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                            EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = '{0}' 
                            DROP DATABASE [{0}] 
                        END",
                database_name)
                );
        }

        public void create_roundhouse_schema()
        {
            run_sql(string.Format(
                @"
                    USE [{0}]
                    GO

                    IF NOT EXISTS(SELECT * FROM sys.schemas WHERE [name] = '{1}')
                      BEGIN
	                    EXEC('CREATE SCHEMA [{1}]')
                      END

                "
                , database_name, roundhouse_schema_name)
                );
        }

        public void create_roundhouse_version_table()
        {
            run_sql(string.Format(
                @"
                    IF NOT EXISTS(SELECT * FROM sys.tables WHERE [name] = '{1}')
                      BEGIN
                        CREATE TABLE [{0}].[{1}]
                        (
                            id                          BigInt			NOT NULL	IDENTITY(1,1)
                            ,repository_path			VarChar(255)	NULL
                            ,version			        VarChar(35)	    NULL
                            ,entry_date					DateTime        NOT NULL	DEFAULT (GetDate())
                            ,modified_date				DateTime        NOT NULL	DEFAULT (GetDate())
                            ,entered_by                 VarChar(50)     NULL
                            ,CONSTRAINT [PK_{1}_id] PRIMARY KEY CLUSTERED (id) 
                        )
                      END
                ",
                roundhouse_schema_name, version_table_name)
                );
        }

        public void create_roundhouse_scripts_run_table()
        {
            run_sql(string.Format(
                @"
                    IF NOT EXISTS(SELECT * FROM sys.tables WHERE [name] = '{1}')
                      BEGIN
                        CREATE TABLE [{0}].[{1}]
                        (
                            id                          BigInt			NOT NULL	IDENTITY(1,1)
                            ,version_id                 BigInt			NULL
                            ,script_name                VarChar(255)	NULL
                            ,text_of_script             Text        	NULL
                            ,text_hash                  VarChar(512)    NULL
                            ,one_time_script            Bit         	NULL        DEFAULT(0)
                            ,entry_date					DateTime        NOT NULL	DEFAULT (GetDate())
                            ,modified_date				DateTime        NOT NULL	DEFAULT (GetDate())
                            ,entered_by                 VarChar(50)     NULL
                            ,CONSTRAINT [PK_{1}_id] PRIMARY KEY CLUSTERED (id) 
                        )
                        
                        ALTER TABLE [{0}].[{1}] WITH CHECK ADD CONSTRAINT [FK_.{1}_{2}_version_id] FOREIGN KEY(version_id) REFERENCES [{0}].[{2}] (id)

                      END
                ",
                roundhouse_schema_name, scripts_run_table_name, version_table_name)
                );
        }

        public void run_sql(string sql_to_run)
        {
            run_sql(database_name, sql_to_run);
        }

        public void run_sql(string database_name, string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            sql_server.ConnectionContext.ExecuteNonQuery(sql_to_run);
        }

        public void insert_script_run(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id)
        {
            run_sql(string.Format(
                @"
                    INSERT INTO [{0}].[{1}] 
                    (
                        version_id
                        ,script_name
                        ,text_of_script
                        ,text_hash
                        ,one_time_script
                        ,entered_by
                    )
                    VALUES
                    (
                        {2}
                        ,'{3}'
                        ,'{4}'
                        ,'{5}'
                        ,{6}
                        ,'{7}'
                    )
                ",
                roundhouse_schema_name, scripts_run_table_name, version_id,
                script_name, sql_to_run.Replace(@"'", @"''"),
                sql_to_run_hash,
                run_this_script_once ? 1 : 0, user_name)
                );
        }

        public string get_version(string repository_path)
        {
            string version = (string)run_sql_scalar(string.Format(
                @"
                    SELECT TOP 1 version 
                    FROM [{0}].[{1}]
                    WHERE 
                        repository_path = '{2}' 
                    ORDER BY entry_date Desc
                ",
                roundhouse_schema_name, version_table_name, repository_path)
                );

            return version;
        }

        public long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            long version_id = (long)run_sql_scalar(string.Format(
                @"
                    INSERT INTO [{0}].[{1}] 
                    (
                        repository_path
                        ,version
                        ,entered_by
                    )
                    VALUES
                    (
                        '{2}'
                        ,'{3}'
                        ,'{4}'
                    )

                    SELECT TOP 1 id 
                    FROM [{0}].[{1}]
                    WHERE 
                        repository_path = '{2}' 
                        AND version = '{3}'
                    ORDER BY entry_date Desc
                ",
                roundhouse_schema_name, version_table_name, repository_path, repository_version, user_name)
                );

            return version_id;
        }

        public string get_current_script_hash(string script_name)
        {
            string script_hash = (string)run_sql_scalar(string.Format(
                @"
                    SELECT TOP 1
                        text_hash
                    FROM [{0}].[{1}]
                    WHERE script_name = '{2}'
                    ORDER BY entry_date Desc
                ",
                roundhouse_schema_name, scripts_run_table_name, script_name
                )
                );

            return script_hash;
        }

        public bool has_run_script_already(string script_name)
        {
            bool script_has_run = false;

            string sql_to_run = string.Format(
                @"
                    SELECT 
                        script_name
                    FROM [{0}].[{1}]
                    WHERE script_name = '{2}'
                ",
                roundhouse_schema_name, scripts_run_table_name, script_name
                );
            DataTable data_table = execute_datatable(sql_to_run);
            if (data_table.Rows.Count > 0)
            {
                script_has_run = true;
            }

            return script_has_run;
        }

        public object run_sql_scalar(string sql_to_run)
        {
            return run_sql_scalar(database_name, sql_to_run);
        }

        public object run_sql_scalar(string database_name, string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            object return_value = sql_server.ConnectionContext.ExecuteScalar(sql_to_run);

            return return_value;
        }

        private DataTable execute_datatable(string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            DataSet result = sql_server.ConnectionContext.ExecuteWithResults(sql_to_run);

            return result.Tables.Count == 0 ? null : result.Tables[0];
        }

        private bool disposing = false;
        public void Dispose()
        {
            if (!disposing)
            {
                //todo: do we have anything to dispose?
                disposing = true;
            }
        }
    }
}