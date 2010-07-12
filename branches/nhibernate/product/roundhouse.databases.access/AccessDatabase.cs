using System;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using roundhouse.infrastructure.extensions;
using roundhouse.infrastructure.logging;
using roundhouse.sql;

namespace roundhouse.databases.access
{
    using connections;
    using infrastructure.app;

    public class AccessDatabase : AdoNetDatabase
    {
        private string connect_options = "Trusted_Connection";

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
                    if (string.IsNullOrEmpty(server_name) && part.to_lower().Contains("server"))
                    {
                        server_name = part.Substring(part.IndexOf("=") + 1);
                    }

                    if (string.IsNullOrEmpty(database_name) && part.to_lower().Contains("database"))
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

            if (connect_options == "Trusted_Connection")
            {
                connect_options = "Trusted_Connection=yes;";
            }

            if (string.IsNullOrEmpty(connection_string))
            {
                connection_string = build_connection_string(server_name, database_name, connect_options);
            }

            if (connection_string.to_lower().Contains("sqlserver") || connection_string.to_lower().Contains("sqlncli"))
            {
                connection_string = build_connection_string(server_name, database_name, connect_options);
            }
            configuration_property_holder.ConnectionString = connection_string;

            set_provider();
            admin_connection_string = connection_string;
            //set_repository(configuration_property_holder);
        }

        public override void open_admin_connection()
        {
            server_connection = new AdoNetConnection(new OleDbConnection(admin_connection_string));
            server_connection.open();
        }

        public override void open_connection(bool with_transaction)
        {
            server_connection = new AdoNetConnection(new OleDbConnection(connection_string));
            server_connection.open();

            if (with_transaction)
            {
                transaction = server_connection.underlying_type().BeginTransaction();
            }
        }

        public override void set_provider()
        {
            provider = ((OleDbConnection)server_connection.underlying_type()).Provider;
            DatabaseTypeSpecifics.sql_scripts_dictionary.TryGetValue(provider, out sql_scripts);
            if (sql_scripts == null)
            {
                sql_scripts = DatabaseTypeSpecifics.t_sql_specific;
            }
        }

        private static string build_connection_string(string server_name, string database_name, string connection_options)
        {
            return string.Format("Provider=SQLNCLI;Server={0};Database={1};{2}", server_name, database_name, connection_options);
        }

        public override void run_database_specific_tasks()
        {
            //TODO: Anything for Access?
        }
  
    }
}