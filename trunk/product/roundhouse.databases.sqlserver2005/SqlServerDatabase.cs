namespace roundhouse.databases.sqlserver2005
{
    using System.Data;
    using System.Data.SqlClient;
    using infrastructure.logging;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;
    using Database = roundhouse.sql.Database;


    public sealed class SqlServerDatabase : Database
    {
        public string server_name { get; set; }
        public string database_name { get; set; }
        public string roundhouse_schema_name { get; set; }
        public string version_table_name { get; set; }
        public string scripts_run_table_name { get; set; }
        public string user_name { get; set; }
        private Server sql_server;
        private bool running_a_transaction = false;

        public SqlServerDatabase()
        {
            MASTER_DATABASE_NAME = "Master";
        }

        public string MASTER_DATABASE_NAME { get; private set; }

        public static string build_connection_string(string server_name, string database_name)
        {
            return string.Format("Server={0};initial catalog={1};Integrated Security=SSPI", server_name, database_name);
        }

        public string create_database_script()
        {
            return string.Format(
                @"USE Master 
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                         BEGIN 
                            CREATE DATABASE [{0}] 
                         END
                        ALTER DATABASE [{0}] SET RECOVERY SIMPLE
                        ",
                database_name);
        }

        public void backup_database(string output_path_minus_database)
        {
            //todo: backup database is not a script - it is a command
            //Server sql_server =
            //    new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            //sql_server.BackupDevices.Add(new BackupDevice(sql_server,database_name));
        }

        public string restore_database_script(string restore_from_path)
        {
            return string.Format(
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
                database_name, restore_from_path);
        }

        public string delete_database_script()
        {
            return string.Format(
                @"USE Master 
                        IF EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                        BEGIN 
                            ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                            EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = '{0}' 
                            DROP DATABASE [{0}] 
                        END",
                database_name);
        }

        public string create_roundhouse_schema_script()
        {
            return string.Format(
                @"
                    USE [{0}]
                    GO

                    IF NOT EXISTS(SELECT * FROM sys.schemas WHERE [name] = '{1}')
                      BEGIN
	                    EXEC('CREATE SCHEMA [{1}]')
                      END

                "
                , database_name, roundhouse_schema_name);
        }

        public string create_roundhouse_version_table_script()
        {
            return string.Format(
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
                roundhouse_schema_name, version_table_name);
        }

        public string create_roundhouse_scripts_run_table_script()
        {
            return string.Format(
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
                roundhouse_schema_name, scripts_run_table_name, version_table_name);
        }

        public string get_version_script(string repository_path)
        {
            return string.Format(
                @"
                    SELECT TOP 1 version 
                    FROM [{0}].[{1}]
                    WHERE 
                        repository_path = '{2}' 
                    ORDER BY entry_date Desc
                ",
                roundhouse_schema_name, version_table_name, repository_path);
        }

        public void open_connection(bool with_transaction)
        {
            sql_server = new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, MASTER_DATABASE_NAME))));
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

        public string insert_version_script(string repository_path, string repository_version)
        {
            return string.Format(
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
                roundhouse_schema_name, version_table_name, repository_path, repository_version, user_name);
        }

        public string insert_script_run_script(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id)
        {
            return string.Format(
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
                run_this_script_once ? 1 : 0, user_name);
        }

        public string get_current_script_hash_script(string script_name)
        {
            return string.Format(
                @"
                    SELECT TOP 1
                        text_hash
                    FROM [{0}].[{1}]
                    WHERE script_name = '{2}'
                    ORDER BY entry_date Desc
                ",
                roundhouse_schema_name, scripts_run_table_name, script_name
                );
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

        public void run_sql(string database_name, string sql_to_run)
        {
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            sql_server.ConnectionContext.ExecuteNonQuery(sql_to_run);
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