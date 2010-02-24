using System;
using System.Text.RegularExpressions;
using roundhouse.infrastructure.extensions;
using roundhouse.sql;

namespace roundhouse.databases.oracle
{
    public sealed class OracleDatabase : AdoNetDatabase
    {
        private string connect_options = "Integrated Security";

        public override void initialize_connection()
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

                if (!connection_string.to_lower().Contains(connect_options.to_lower()))
                {
                    connect_options = string.Empty;
                    foreach (string part in parts)
                    {
                        if (!part.to_lower().Contains("server") && !part.to_lower().Contains("data source") && !part.to_lower().Contains("initial catalog") &&
                            !part.to_lower().Contains("database"))
                        {
                            connect_options += part + ";";
                        }
                    }
                }
            }

            if (connect_options == "Integrated Security")
            {
                connect_options = "Integrated Security=SSPI;";
            }

            if (string.IsNullOrEmpty(connection_string) || connection_string.to_lower().Contains(database_name.to_lower()))
            {
                connection_string = build_connection_string(server_name, connect_options);
            }

            provider = "System.Data.OracleClient";
            SqlScripts.sql_scripts_dictionary.TryGetValue(provider, out sql_scripts);
            if (sql_scripts == null)
            {
                sql_scripts = SqlScripts.pl_sql_scripts;
            }

            create_connection();
        }

        private static string build_connection_string(string server_name, string connection_options)
        {
            return string.Format("Data Source={0};{1}", server_name, connection_options);
        }

        public override void create_database_if_it_doesnt_exist()
        {
            close_connection();
            string current_connection = connection_string;
            connection_string = build_connection_string(server_name, "User Id=System;Password=Oracle;Persist Security Info=false;");
            create_connection();
            open_connection(false);
            base.create_database_if_it_doesnt_exist();

            connection_string = current_connection;
            create_connection();
        }

        public override void delete_database_if_it_exists()
        {
            close_connection();
            string current_connection = connection_string;
            connection_string = build_connection_string(server_name, "User Id=System;Password=Oracle;Persist Security Info=false;");
            create_connection();
            open_connection(false);
            base.delete_database_if_it_exists();

            connection_string = current_connection;
            create_connection();
        }

        public override long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            run_sql(sql_scripts.insert_version(roundhouse_schema_name, version_table_name, repository_path, repository_version, user_name));
            return Convert.ToInt64((decimal)run_sql_scalar(sql_scripts.get_version_id(roundhouse_schema_name, version_table_name, repository_path)));
        }

        public override void run_sql(string sql_to_run)
        {
            base.run_sql(RemoveSpacesAndComment(sql_to_run));
        }

        public override object run_sql_scalar(string sql_to_run)
        {
            return base.run_sql_scalar(RemoveSpacesAndComment(sql_to_run));
        }

        private string RemoveSpacesAndComment(string query)
        {
            var regexSpaces = new Regex(@"\s{2,}", RegexOptions.IgnoreCase);
            var regexComment = new Regex(@"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)|(^\s*--.*)");

            string cleanQuery = regexComment.Replace(query, " ");
            return regexSpaces.Replace(cleanQuery, " ").Trim();
        }
    }
}