namespace roundhouse.sql
{
    using System.Data.SqlClient;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;

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

        public void run_sql(string server_name, string database_name, string sql_to_run)
        {
            Server sql_server = new Server(new ServerConnection(new SqlConnection(build_connection_string(server_name, database_name))));
            sql_server.ConnectionContext.ExecuteNonQuery(string.Format("USE {0}", database_name));
            sql_server.ConnectionContext.ExecuteNonQuery(sql_to_run);
        }
    }
}