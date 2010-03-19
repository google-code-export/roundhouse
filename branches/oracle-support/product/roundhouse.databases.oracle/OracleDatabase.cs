using System;
using System.Collections.Generic;
using System.Data;
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

            
            if (string.IsNullOrEmpty(connection_string))
            {
				if (connect_options == "Integrated Security")
				{
					connect_options = "Integrated Security=SSPI;";
				}
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
        	throw new NotSupportedException("Has to be tested when an Oracle environment with Integrated Security is available.");            
        }

        public override void delete_database_if_it_exists()
        {
			throw new NotSupportedException("Has to be tested when an Oracle environment with Integrated Security is available.");            
        }

        public override long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            var insert_parameters = new List<IDbDataParameter>
                                 {
                                     create_parameter("repository_path", DbType.AnsiString, repository_path, 255), 
                                     create_parameter("repository_version", DbType.AnsiString, repository_version, 35), 
                                     create_parameter("user_name", DbType.AnsiString, user_name, 50)
                                 };
            run_sql(sql_scripts.insert_version_parameterized(roundhouse_schema_name, version_table_name), insert_parameters);

            var select_parameters = new List<IDbDataParameter> { create_parameter("repository_path", DbType.AnsiString, repository_path, 255) };
            return Convert.ToInt64((decimal)run_sql_scalar(sql_scripts.get_version_id_parameterized(roundhouse_schema_name, version_table_name), select_parameters));
        }

        public override void run_sql(string sql_to_run)
        {
            // http://www.barrydobson.com/2009/02/17/pls-00103-encountered-the-symbol-when-expecting-one-of-the-following/
            base.run_sql(sql_to_run.Replace("\r\n", "\n"));
        }

        public override object run_sql_scalar(string sql_to_run)
        {
            // http://www.barrydobson.com/2009/02/17/pls-00103-encountered-the-symbol-when-expecting-one-of-the-following/
            return base.run_sql_scalar(sql_to_run.Replace("\r\n", "\n"));
        }
    }
}