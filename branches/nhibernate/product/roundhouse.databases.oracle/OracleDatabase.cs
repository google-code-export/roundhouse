using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using roundhouse.infrastructure.extensions;
using roundhouse.parameters;
using roundhouse.sql;

namespace roundhouse.databases.oracle
{
    using infrastructure.app;
    using infrastructure.persistence;

    public sealed class OracleDatabase : AdoNetDatabase
    {
        private string connect_options = "Integrated Security";

        public override string sql_statement_separator_regex_pattern
        {
            get { return @"(?<KEEP1>^(?:.)*(?:-{2}).*$)|(?<KEEP1>/{1}\*{1}[\S\s]*?\*{1}/{1})|(?<KEEP1>\s)(?<BATCHSPLITTER>;)(?<KEEP2>\s)|(?<KEEP1>\s)(?<BATCHSPLITTER>;)(?<KEEP2>$)"; }
        }

        public override bool supports_ddl_transactions
        {
            get { return false; }
        }

        public override void initialize_connections(ConfigurationPropertyHolder configuration_property_holder)
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
            configuration_property_holder.ConnectionString = connection_string;

            set_provider();
            admin_connection_string = configure_admin_connection_string();
            //set_repository(configuration_property_holder);
        }

        private string configure_admin_connection_string()
        {
            string admin_string = Regex.Replace(connection_string, "User Id=.*?;", "User Id=System;");
            admin_string = Regex.Replace(admin_string, "Password=.*?;", "Password=QAORACLE;");

            return admin_string;
        }

        public override void set_provider()
        {
            provider = "System.Data.OracleClient";
            DatabaseTypeSpecifics.sql_scripts_dictionary.TryGetValue(provider, out sql_scripts);
            if (sql_scripts == null)
            {
                sql_scripts = DatabaseTypeSpecifics.pl_sql_specific;
            }
        }

        public override void run_database_specific_tasks()
        {
            //TODO: Create sequences
        }

        public override long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            var insert_parameters = new List<IParameter<IDbDataParameter>>
                                 {
                                     create_parameter("repository_path", DbType.AnsiString, repository_path, 255), 
                                     create_parameter("repository_version", DbType.AnsiString, repository_version, 35), 
                                     create_parameter("user_name", DbType.AnsiString, user_name, 50)
                                 };
            run_sql(insert_version(), insert_parameters);

            var select_parameters = new List<IParameter<IDbDataParameter>> { create_parameter("repository_path", DbType.AnsiString, repository_path, 255) };
            return Convert.ToInt64((decimal)run_sql_scalar(get_version_id(), select_parameters));
        }

        public string insert_version()
        {
            return string.Format(
                @"
                    INSERT INTO {0}_{1}
                    (
                        id
                        ,repository_path
                        ,version
                        ,entered_by
                    )
                    VALUES
                    (
                        {0}_{1}id.NEXTVAL
                        ,:repository_path
                        ,:repository_version
                        ,:user_name
                    )
                ",
                roundhouse_schema_name, version_table_name);
        }

        public string get_version_id()
        {
            return string.Format(
               @"
                    SELECT id
                    FROM (SELECT * FROM {0}_{1}
                            WHERE 
                                repository_path = :repository_path
                            ORDER BY entry_date DESC)
                    WHERE ROWNUM < 2
                ",
               roundhouse_schema_name, version_table_name);
        }


        public override void run_sql(string sql_to_run)
        {
            // http://www.barrydobson.com/2009/02/17/pls-00103-encountered-the-symbol-when-expecting-one-of-the-following/
            base.run_sql(sql_to_run.Replace("\r\n", "\n"));
        }

        private object run_sql_scalar(string sql_to_run, IList<IParameter<IDbDataParameter>> parameters)
        {
            //http://www.barrydobson.com/2009/02/17/pls-00103-encountered-the-symbol-when-expecting-one-of-the-following/
            sql_to_run = sql_to_run.Replace("\r\n", "\n");
            object return_value = new object();

            if (string.IsNullOrEmpty(sql_to_run)) return return_value;

            using (IDbCommand command = setup_database_command(sql_to_run, parameters))
            {
                return_value = command.ExecuteScalar();
                command.Dispose();
            }

            return return_value;
        }

        private IParameter<IDbDataParameter> create_parameter(string name, DbType type, object value, int? size)
        {
            IDbCommand command = server_connection.underlying_type().CreateCommand();
            var parameter = command.CreateParameter();
            command.Dispose();

            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Value = value;
            if (size != null)
            {
                parameter.Size = size.Value;
            }

            return new AdoNetParameter(parameter);
        }

    }
}