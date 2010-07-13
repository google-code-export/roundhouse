namespace roundhouse.infrastructure.persistence
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using app;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using loaders;
    using logging;
    using NHibernate;
    using NHibernate.Cfg;
    using NHibernate.Event;

    public class NHibernateSessionFactoryBuilder
    {
        private readonly ConfigurationPropertyHolder configuration_holder;
        private readonly Dictionary<string, Func<IPersistenceConfigurer>> func_dictionary;

        public NHibernateSessionFactoryBuilder(ConfigurationPropertyHolder config)
        {
            configuration_holder = config;
            func_dictionary = new Dictionary<string, Func<IPersistenceConfigurer>>();
            func_dictionary.Add("roundhouse.databases.sqlserver.SqlServerDatabase, roundhouse.databases.sqlserver", () => MsSqlConfiguration.MsSql2005.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.sqlserver2000.SqlServerDatabase, roundhouse.databases.sqlserver2000", () => MsSqlConfiguration.MsSql2000.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.mysql.MySqlDatabase, roundhouse.databases.mysql", () => MySQLConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.oracle.OracleDatabase, roundhouse.databases.oracle", () => OracleClientConfiguration.Oracle9.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.access.AccessDatabase, roundhouse.databases.access", () => JetDriverConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.sqlite.SQLiteDatabase, roundhouse.databases.sqlite", () => SQLiteConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.postgresql.PostgreSQLDatabase, roundhouse.databases.postgresql", () => PostgreSQLConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.sqlserver.SqlServerDatabase, rh", () => MsSqlConfiguration.MsSql2005.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.sqlserver2000.SqlServerDatabase, rh", () => MsSqlConfiguration.MsSql2000.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.mysql.MySqlDatabase, rh", () => MySQLConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.oracle.OracleDatabase, rh", () => OracleClientConfiguration.Oracle9.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.access.AccessDatabase, rh", () => JetDriverConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.sqlite.SQLiteDatabase, rh", () => SQLiteConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
            func_dictionary.Add("roundhouse.databases.postgresql.PostgreSQLDatabase, rh", () => PostgreSQLConfiguration.Standard.ConnectionString(configuration_holder.ConnectionString));
        }

        public ISessionFactory build_session_factory()
        {
            return build_session_factory(no_operation);
        }

        public ISessionFactory build_session_factory(Action<Configuration> additional_function)
        {
            string top_namespace = configuration_holder.DatabaseType.Substring(0, configuration_holder.DatabaseType.IndexOf(','));
            top_namespace = top_namespace.Substring(0, top_namespace.LastIndexOf('.'));
            string assembly_name = configuration_holder.DatabaseType.Substring(configuration_holder.DatabaseType.IndexOf(',') + 1);
            try
            {
                return build_session_factory(func_dictionary[configuration_holder.DatabaseType](), DefaultAssemblyLoader.load_assembly(assembly_name), top_namespace, additional_function);
            }
            catch (Exception)
            {
                string key = configuration_holder.DatabaseType.Substring(0, configuration_holder.DatabaseType.IndexOf(',')) + ", rh";
                return build_session_factory(func_dictionary[key](), DefaultAssemblyLoader.load_assembly("rh"), top_namespace, additional_function);
            }

        }

        public ISessionFactory build_session_factory(IPersistenceConfigurer db_configuration, Assembly assembly, string top_namespace, Action<Configuration> additional_function)
        {
            //TODO: NHibernate Session Factory - Ignore everyone else in the merged mappings
            Log.bound_to(this).log_a_debug_event_containing("Building Session Factory");
            var config = Fluently.Configure()
                .Database(db_configuration)
                .Mappings(m =>
                              {
                                  m.FluentMappings.Add(assembly.GetType(top_namespace + ".orm.VersionMapping",true,true))
                                      .Add(assembly.GetType(top_namespace + ".orm.ScriptsRunMapping", true, true))
                                      .Add(assembly.GetType(top_namespace + ".orm.ScriptsRunErrorMapping", true, true))
                                  .Conventions.AddAssembly(assembly);
                                  //m.HbmMappings.AddFromAssembly(assembly);
                              })
                .ExposeConfiguration(cfg =>
                    {
                        cfg.SetListener(ListenerType.PreInsert, new AuditEventListener());
                        cfg.SetListener(ListenerType.PreUpdate, new AuditEventListener());
                    })
                .ExposeConfiguration(additional_function);

            return config.BuildSessionFactory();
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