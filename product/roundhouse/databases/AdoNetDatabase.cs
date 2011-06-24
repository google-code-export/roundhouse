using roundhouse.infrastructure.app;

namespace roundhouse.databases
{
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using connections;
    using parameters;

    public abstract class AdoNetDatabase : DefaultDatabase<IDbConnection>
    {
        private bool split_batches_in_ado = true;

        public override bool split_batch_statements
        {
            get { return split_batches_in_ado; }
            set { split_batches_in_ado = value; }
        }

        protected IDbTransaction transaction;

        private DbProviderFactory provider_factory;

        private AdoNetConnection GetAdoNetConnection(string conn_string)
        {
            provider_factory = DbProviderFactories.GetFactory(provider);
            IDbConnection connection = provider_factory.CreateConnection();
            connection.ConnectionString = conn_string;
            return new AdoNetConnection(connection);
        }

        public override void open_admin_connection()
        {
            admin_connection = GetAdoNetConnection(admin_connection_string);
            admin_connection.open();
        }

        public override void close_admin_connection()
        {
            admin_connection.close();
        }

        public override void open_connection(bool with_transaction)
        {
            server_connection = GetAdoNetConnection(connection_string);
            server_connection.open();

            set_repository();

            if (with_transaction)
            {
                transaction = server_connection.underlying_type().BeginTransaction();
                repository.start(true);
            }
        }

        public override void close_connection()
        {
            if (transaction != null)
            {
                transaction.Commit();
                transaction = null;
            }

            repository.finish();

            server_connection.close();
        }

        public override void rollback() {
            repository.rollback();
 
            if (transaction != null)
            {
                //rollback previous transaction
                transaction.Rollback();
                server_connection.close();

                //open a new transaction
                server_connection.open();
                //use_database(database_name);
                transaction = server_connection.underlying_type().BeginTransaction();
                repository.start(true);
            }
        }

        protected override void run_sql(string sql_to_run, ConnectionType connection_type, IList<IParameter<IDbDataParameter>> parameters)
        {
            if (string.IsNullOrEmpty(sql_to_run)) return;

            using (IDbCommand command = setup_database_command(sql_to_run,connection_type, parameters))
            {
                command.ExecuteNonQuery();
                command.Dispose();
            }
        }

        protected IDbCommand setup_database_command(string sql_to_run,ConnectionType connection_type, IEnumerable<IParameter<IDbDataParameter>> parameters)
        {
            IDbCommand command = null;
            switch (connection_type)
            {
                case ConnectionType.Default :
                    if (server_connection == null)
                    {
                        open_connection(false);
                    }
                    command  = server_connection.underlying_type().CreateCommand(); 
                    break;
                case ConnectionType.Admin :
                    if (admin_connection == null)
                    {
                        open_admin_connection();
                    }
                    command = admin_connection.underlying_type().CreateCommand();
                    break;
            }
            
            if (parameters != null)
            {
                foreach (IParameter<IDbDataParameter> parameter in parameters)
                {
                    command.Parameters.Add(parameter.underlying_type);
                }
            }
            command.Transaction = transaction;
            command.CommandText = sql_to_run;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = command_timeout;

            return command;
        }
    }
}