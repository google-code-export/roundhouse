using System.Collections.Generic;
using System.Data.Common;

namespace roundhouse.databases
{
    using System.Data;
    using sql;

    public abstract class AdoNetDatabase : DefaultDatabase
    {
        private bool split_batches_in_ado = true;
        public override bool split_batch_statements
        {
            get { return split_batches_in_ado; }
            set { split_batches_in_ado = value; }
        }

        private DbProviderFactory provider_factory;
        
        protected void create_connection()
        {
            provider_factory = DbProviderFactories.GetFactory(provider);
            server_connection = provider_factory.CreateConnection();
            server_connection.ConnectionString = connection_string;
        }

    }
}