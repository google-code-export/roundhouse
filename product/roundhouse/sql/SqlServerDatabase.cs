using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using roundhouse.infrastructure.logging;

namespace roundhouse.sql
{
    public class SqlServerDatabase : Database
    {
        public string server_name { get; set; }
        public string database_name { get; set; }
        public string roundhouse_schema_name { get; set; }
        public string version_table_name { get; set; }
        public string scripts_run_table_name { get; set; }

        public SqlServerDatabase(string server_name, string database_name, string roundhouse_schema_name,
                                 string version_table_name,
                                 string scripts_run_table_name)
        {
            this.server_name = server_name;
            this.database_name = database_name;
            this.roundhouse_schema_name = roundhouse_schema_name;
            this.version_table_name = version_table_name;
            this.scripts_run_table_name = scripts_run_table_name;
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
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                        BEGIN 
                            ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
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

        public string insert_version_script(string repository_path, string repository_version)
        {
            return string.Format(
                @"
                    INSERT INTO [{0}].[{1}] 
                    (
                        repository_path
                        ,version
                    )
                    VALUES
                    (
                        '{2}'
                        ,'{3}'
                    )

                    SELECT TOP 1 id 
                    FROM [{0}].[{1}]
                    WHERE 
                        repository_path = '{2}' 
                        AND version = '{3}'
                    ORDER BY modified_date Desc
                ",
                roundhouse_schema_name, version_table_name, repository_path, repository_version);
        }

        public string insert_script_run_script(string script_name, string sql_to_run, bool run_this_script_once, long version_id)
        {
            //todo: get the version going in

            return string.Format(
                @"
                    INSERT INTO [{0}].[{1}] 
                    (
                        version_id
                        ,script_name
                        ,text_of_script
                        ,one_time_script
                    )
                    VALUES
                    (
                        {5}
                        ,'{2}'
                        ,'{3}'
                        ,{4}
                    )
                ",
                roundhouse_schema_name, scripts_run_table_name, script_name, sql_to_run.Replace(@"'", @"''"),
                run_this_script_once ? 1 : 0, version_id);
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
            Server sql_server =
                new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            sql_server.ConnectionContext.ExecuteNonQuery(sql_to_run);
        }

        public object run_sql_scalar(string database_name,string sql_to_run)
        {
            Server sql_server = new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            object return_value = sql_server.ConnectionContext.ExecuteScalar(sql_to_run);
            Log.bound_to(this).log_a_debug_event_containing("Version Id return value was {0}", return_value);

            return return_value;
        }

        private DataTable execute_datatable(string sql_to_run)
        {
            DataSet result = new DataSet();

            using (SqlConnection connection = new SqlConnection(build_connection_string(server_name, database_name)))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "USE " + database_name;
                    command.ExecuteNonQuery();
                    command.CommandText = sql_to_run;
                    SqlDataAdapter da = new SqlDataAdapter(command.CommandText, command.Connection);
                    da.Fill(result);
                    command.Dispose();
                }
                //todo close the connection?
            }

            return result.Tables.Count == 0 ? null : result.Tables[0];
        }
    }
}