namespace roundhouse.infrastructure.persistence
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using app;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using logging;
    using NHibernate;
    using NHibernate.Cfg;
    using NHibernate.Event;
    
    public class NHibernateSessionFactory
    {
        private readonly ConfigurationPropertyHolder configuration_holder;
        private readonly Dictionary<string, Func<IPersistenceConfigurer>> func_dictionary;

        public NHibernateSessionFactory(ConfigurationPropertyHolder config)
        {
            configuration_holder = config;
            func_dictionary = new Dictionary<string, Func<IPersistenceConfigurer>>();
            func_dictionary.Add("roundhouse.databases.sqlserver.SqlServerDatabase, roundhouse.databases.sqlserver", () => MsSqlConfiguration.MsSql2005.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.sqlserver2000.SqlServerDatabase, roundhouse.databases.sqlserver2000", () => MsSqlConfiguration.MsSql2000.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.mysql.MySqlDatabase, roundhouse.databases.mysql", () => MySQLConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.oracle.OracleDatabase, roundhouse.databases.oracle", () => OracleClientConfiguration.Oracle9.ConnectionString(configuration_holder.ConnectionString));
            //TODO: Access or OleDB? func_dictionary.Add("roundhouse.databases.oledb.OleDbDatabase, roundhouse.databases.oledb", () => Gen  OracleConfiguration.Oracle9.ConnectionString(configuration_holder.ConnectionString));
        }

        public ISessionFactory build_session_factory()
        {
            return build_session_factory(func_dictionary[configuration_holder.DatabaseType](), Assembly.Load(configuration_holder.DatabaseType), no_operation);
        }

        public ISessionFactory build_session_factory(Action<Configuration> additional_function)
        {
            return build_session_factory(func_dictionary[configuration_holder.DatabaseType](), Assembly.Load(configuration_holder.DatabaseType),additional_function);
        }

        public ISessionFactory build_session_factory(IPersistenceConfigurer db_configuration, Assembly assembly, Action<Configuration> additional_function)
        {
            Log.bound_to(this).log_a_debug_event_containing("Building Session Factory");
            return Fluently.Configure()
                .Database(db_configuration)
                .Mappings(m => {
                                   m.FluentMappings.AddFromAssembly(assembly)
                                       .Conventions.AddAssembly(assembly);
                                   m.HbmMappings.AddFromAssembly(assembly); 
                        })
                .ExposeConfiguration(cfg =>
                    {
                    cfg.SetListener(ListenerType.PreInsert, new AuditEventListener());
                    cfg.SetListener(ListenerType.PreUpdate, new AuditEventListener());
                    })
                .ExposeConfiguration(additional_function)
                .BuildSessionFactory();
        }

        private static void no_operation(Configuration cfg)
        {
        }

        //cfg =>
        //   {
        //         //listeners?
        //         var se = new SchemaExport(cfg);
        //         se.SetOutputFile(Path.Combine(configuration_holder.SqlFilesDirectory,"/up/0001_nhibernate.sql"));
        //         se.Create(true, true);
        //     }
    }
}