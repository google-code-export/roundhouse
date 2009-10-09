using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace roundhouse.sql
{
    public class SqlServerDatabase : Database
    {
        private const string MASTER_DATABASE_NAME = "Master";

        private static string build_connection_string(string server_name, string database_name)
        {
            return string.Format("Server={0};initial catalog={1};Integrated Security=SSPI", server_name, database_name);
        }

        public void create_database(string server_name, string database_name)
        {
            string sql_to_run =
                string.Format(
                    @"USE Master 
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                         BEGIN 
                            CREATE DATABASE [{0}] 
                         END
                        ALTER DATABASE [{0}] SET RECOVERY SIMPLE
                        ",
                    database_name);

            run_sql(server_name, MASTER_DATABASE_NAME, sql_to_run);
        }

        public void verify_or_create_roundhouse_tables(string server_name, string database_name,
                                                       string roundhouse_schema_name, string version_table_name,
                                                       string scripts_run_table_name)
        {
            //todo:create schema
            //todo:create version table
            //todo:create scripts run table
        }

        public void delete_database(string server_name, string database_name)
        {
            string sql_to_run =
                string.Format(
                    @"USE Master 
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                        BEGIN 
                            ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
                            DROP DATABASE [{0}] 
                        END",
                    database_name);

            run_sql(server_name, MASTER_DATABASE_NAME, sql_to_run);
        }

        public void run_sql(string server_name, string database_name, string sql_to_run, string script_name,
                            bool run_this_script_once)
        {
            if (this_script_should_run(script_name, run_this_script_once))
            {
                run_sql(server_name, database_name, sql_to_run);
                //todo:insert into the scripts run table
            }
        }

        private void run_sql(string server_name, string database_name, string sql_to_run)
        {
            Server sql_server =
                new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            sql_server.ConnectionContext.ExecuteNonQuery(sql_to_run);
        }

        private bool this_script_should_run(string script_name, bool run_this_script_once)
        {
            if (!run_this_script_once) return true;
            //todo:check to see if it has already run
            return true;
        }

        private void create_version_table(string server_name, string database_name, string roundhouse_schema_name,
                                          string version_table_name)
        {
            //todo: get table format figured out
            string sql_to_run = string.Format(
                @"
                    IF NOT EXISTS(SELECT * FROM sys.tables WHERE [name] = '{1}'
                      BEGIN
                        CREATE TABLE [{0}].[{1}]
                        (
                            id
                            repository path
                            version
                        )
                      END
                ",
                roundhouse_schema_name, version_table_name);
        }

        private void create_scripts_run_table(string server_name, string database_name, string roundhouse_schema_name,
                                              string scripts_run_table_name)
        {
            //todo: get table format figured out
            string sql_to_run = string.Format(
                @"
                    IF NOT EXISTS(SELECT * FROM sys.tables WHERE [name] = '{1}'
                      BEGIN
                        CREATE TABLE [{0}].[{1}]
                        (
                            id
                            version_table_id
                            script_name
                            text_of_script BIG ENOUGH TO HANDLE A LARGE > 15MB FILE
                        )
                      END
                ",
                roundhouse_schema_name, scripts_run_table_name);
        }
    }
}